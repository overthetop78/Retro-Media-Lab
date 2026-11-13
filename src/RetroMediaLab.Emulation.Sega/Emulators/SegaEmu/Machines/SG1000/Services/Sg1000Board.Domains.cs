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
    private uint _videoMemoryPending;

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
        if (next.HasValue) _videoMemoryPending |= 1u << bit;
        else _videoMemoryPending &= ~(1u << bit);
        if (due < _videoMemoryDue) { _videoMemoryDue = due; _videoMemoryDueDirty = false; }
        else if (previous == _videoMemoryDue) _videoMemoryDueDirty = true;
    }

    private void RefreshVideoMemoryBoundaries()
    {
        _videoMemoryPending = 0;
        for (int bit = 0; bit < 8; bit++)
        {
            var due = _dram[bit].NextUnrepresentedEventNanoseconds;
            _videoMemoryDeadlines[bit] = due ?? long.MaxValue;
            if (due.HasValue) _videoMemoryPending |= 1u << bit;
        }
        _videoMemoryDueDirty = true;
        _psgReadyDue = _psg.NextEventNanoseconds ?? long.MaxValue;
    }

    private void PublishVideoInputBoundary()
    {
        if (!_clockDomainsConnected) return;
        if (_videoMemoryDueDirty)
        {
            long next = long.MaxValue;
            uint pending = _videoMemoryPending;
            while (pending != 0)
            {
                int bit = System.Numerics.BitOperations.TrailingZeroCount(pending);
                pending &= pending - 1;
                next = Math.Min(next, _videoMemoryDeadlines[bit]);
            }
            _videoMemoryDue = next;
            _videoMemoryDueDirty = false;
        }
        _vdp.SetClockInputBoundary(_videoMemoryDue == long.MaxValue ? null : _videoMemoryDue);
    }

    private void DeliverVideoInputs(long time)
    {
        // Only DRAM responses occur between these deliveries. The first word
        // advances the VDP through this boundary. Following coincident changes
        // can be transported together until the next VDP interaction below.
        _collectingDramData = true;
        _coincidentVramTime = time;
        try
        {
            uint pending = _videoMemoryPending;
            while (pending != 0)
            {
                int bit = System.Numerics.BitOperations.TrailingZeroCount(pending);
                pending &= pending - 1;
                if (_videoMemoryDeadlines[bit] <= time) _dram[bit].AdvanceTo(time);
            }
        }
        finally
        {
            _collectingDramData = false;
            FlushCoincidentVramData();
        }
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
