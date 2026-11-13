using RetroMediaLab.Chips.Logic.Common.Contracts;
using C = RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.SG1000.Constants.Sg1000Constants;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.SG1000.Services;

internal sealed partial class Sg1000Board
{
    private bool _clockDomainsConnected, _synchronizingVideo;
    private long _videoCpuDue = long.MaxValue, _psgReadyDue = long.MaxValue;
    private long _videoMemoryDue = long.MaxValue;
    private readonly long[] _videoMemoryDeadlines = new long[8];
    private bool _videoMemoryDueDirty = true;

    private void ConnectClockDomains()
    {
        _vdp.CpuClockSourceChanged = source =>
        {
            if (source is not { } clock) return;
            _cpu.ReceiveClockSource(clock);
            _psg.ReceiveClockSource(clock);
        };
        _vdp.NextCpuEventNanosecondsChanged = next => _videoCpuDue = next ?? long.MaxValue;
        _vdp.ClockInputBoundaryReached = DeliverVideoInputs;
        _vdp.ReceiveMasterClockSource(new RationalClockSource(0, C.NanosecondsPerSecond,
            C.CrystalHertz * 2, _nextMasterIndex, !_masterHigh));
        _videoCpuDue = _vdp.NextCpuEventNanoseconds ?? long.MaxValue;
        _clockDomainsConnected = true;
        PublishVideoInputBoundary();
    }

    private void ReceiveVideoMemoryDeadline(int bit, long? next)
    {
        long previous = _videoMemoryDeadlines[bit], due = next ?? long.MaxValue;
        if (previous == due) return;
        _videoMemoryDeadlines[bit] = due;
        if (due < _videoMemoryDue) { _videoMemoryDue = due; _videoMemoryDueDirty = false; }
        else if (previous == _videoMemoryDue) _videoMemoryDueDirty = true;
    }

    private void RefreshVideoMemoryBoundaries()
    {
        for (int bit = 0; bit < 8; bit++)
            _videoMemoryDeadlines[bit] = _dram[bit].NextEventNanoseconds ?? long.MaxValue;
        _videoMemoryDueDirty = true;
        _psgReadyDue = _psg.NextEventNanoseconds ?? long.MaxValue;
    }

    private void PublishVideoInputBoundary()
    {
        if (!_clockDomainsConnected) return;
        if (_videoMemoryDueDirty)
        {
            long next = long.MaxValue;
            foreach (long due in _videoMemoryDeadlines) next = Math.Min(next, due);
            _videoMemoryDue = next;
            _videoMemoryDueDirty = false;
        }
        _vdp.SetClockInputBoundary(_videoMemoryDue == long.MaxValue ? null : _videoMemoryDue);
    }

    private void DeliverVideoInputs(long time)
    {
        // Responses retain physical package order, including nested effects.
        for (int bit = 0; bit < 8; bit++)
            if (_videoMemoryDeadlines[bit] <= time) _dram[bit].AdvanceTo(time);
        _vdp.AdvanceTo(time);
        PublishVideoInputBoundary();
    }

    private void SynchronizeVideoDomain(long time)
    {
        if (!_clockDomainsConnected) { _vdp.AdvanceTo(time); return; }
        if (_synchronizingVideo)
        {
            if (time != _vdp.TimeNanoseconds) throw new InvalidOperationException("Recursive video-domain advance.");
            return;
        }
        _synchronizingVideo = true;
        _videoSynchronizationCalls++;
        try
        {
            int count;
            do
            {
                count = _vdp.AdvanceMasterClockSourceUntil(checked(time + 1));
                _nextMasterIndex = checked(_nextMasterIndex + count);
            } while (count == int.MaxValue);
            _masterHigh = _vdp.ClockState.MasterHigh;
            _vdp.AdvanceTo(time);
        }
        finally { _synchronizingVideo = false; }
    }
}
