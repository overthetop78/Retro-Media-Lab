using RetroMediaLab.Chips.Logic.Common.Contracts;
using C = RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.SG1000.Constants.Sg1000Constants;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.SG1000.Services;

internal sealed partial class Sg1000Board
{
    private bool _clockDomainsConnected, _synchronizingVideo;
    private long _videoCpuDue = long.MaxValue;
    private long _videoMemoryDue = long.MaxValue, _cpuWireDue = long.MaxValue;
    private bool _videoMemoryDueDirty = true, _cpuWireDueDirty = true;

    private void ConnectClockDomains()
    {
        _vdp.CpuClockSourceChanged = source =>
        {
            if (source is not { } clock) return; // Restore detaches transport before rebinding all receivers.
            _cpu.ReceiveClockSource(clock);
            _psg.ReceiveClockSource(clock);
        };
        _vdp.NextCpuEventNanosecondsChanged = next =>
        {
            _videoCpuDue = next ?? long.MaxValue;
            _cpuWireDueDirty = true;
            if (next is { } due) _cpu.LimitClockSourceBefore(due);
        };
        _vdp.ClockInputBoundaryReached = DeliverVideoInputs;
        _vdp.ReceiveMasterClockSource(new RationalClockSource(0, C.NanosecondsPerSecond,
            C.CrystalHertz * 2, _nextMasterIndex, !_masterHigh));
        _videoCpuDue = _vdp.NextCpuEventNanoseconds ?? long.MaxValue;
        _clockDomainsConnected = true;
        _videoMemoryDueDirty = _cpuWireDueDirty = true;
        PublishVideoInputBoundary();
    }

    private void ReceiveDomainDeadline(int slot, long? next)
    {
        long previous = _due[slot], due = next ?? long.MaxValue;
        if (previous == due) return;
        _due[slot] = due;
        if (!_useClockDomains) return;
        if (slot is >= 7 and <= 14)
        {
            if (due < _videoMemoryDue) { _videoMemoryDue = due; _videoMemoryDueDirty = false; }
            else if (previous == _videoMemoryDue) _videoMemoryDueDirty = true;
        }
        else
        {
            _cpuWireDueDirty = true;
            if (next.HasValue) _cpu.LimitClockSourceBefore(due);
        }
    }

    private long NextVideoMemoryDeadline()
    {
        if (!_videoMemoryDueDirty) return _videoMemoryDue;
        long next = long.MaxValue;
        for (int i = 7; i <= 14; i++) next = Math.Min(next, _due[i]);
        _videoMemoryDue = next;
        _videoMemoryDueDirty = false;
        return _videoMemoryDue;
    }

    private long NextCpuWireDeadline()
    {
        if (!_cpuWireDueDirty) return _cpuWireDue;
        long next = _videoCpuDue;
        for (int i = 2; i <= 6; i++) next = Math.Min(next, _due[i]);
        for (int i = 15; i <= 18; i++) next = Math.Min(next, _due[i]);
        if (!_vdpReleased) next = Math.Min(next, C.VdpReleaseNanoseconds);
        if (!_cpuReleased) next = Math.Min(next, C.CpuReleaseNanoseconds);
        _cpuWireDue = next;
        _cpuWireDueDirty = false;
        return _cpuWireDue;
    }

    private void PublishVideoInputBoundary()
    {
        if (!_useClockDomains || !_clockDomainsConnected) return;
        long next = NextVideoMemoryDeadline();
        _vdp.SetClockInputBoundary(next == long.MaxValue ? null : next);
    }

    private void DeliverVideoInputs(long time)
    {
        // Preserve the connected package order. A response can cause new VDP
        // outputs and same-date input deliveries before the next package.
        for (int i = 0; i < 8; i++)
            if (_due[7 + i] <= time) _dram[i].AdvanceTo(time);
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

    private void AdvanceCpuWires(long time)
    {
        if (_due[3] <= time) _decoder.AdvanceTo(time);
        if (_due[4] <= time) _gates.AdvanceTo(time);
        for (int i = 0; i < 2; i++) if (_due[5 + i] <= time) _joy[i].AdvanceTo(time);
        if (_due[15] <= time) _cartridgeLow!.AdvanceTo(time - _cartridgeOrigin);
        if (_due[16] <= time) _cartridgeHigh!.AdvanceTo(time - _cartridgeOrigin);
        if (_due[18] <= time) _cartridgeRam!.AdvanceTo(time - _cartridgeOrigin);
        if (_due[17] <= time) _internalRom!.AdvanceTo(time);
        if (_due[2] <= time) _psg.AdvanceTo(time);
        if (_videoCpuDue <= time) SynchronizeVideoDomain(time);
        if (!_vdpReleased && time == C.VdpReleaseNanoseconds)
        {
            SynchronizeVideoDomain(time);
            _vdpReleased = true;
            _cpuWireDueDirty = true;
            _vdp.SetReset(time, false);
        }
        if (!_cpuReleased && time == C.CpuReleaseNanoseconds)
        {
            _cpuReleased = true;
            _cpuWireDueDirty = true;
            _cpu.SetReset(false);
        }
        _cpu.AdvanceTo(time);
    }

    private void AdvanceDomains(long requestedTime, bool stopAtFrame)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfLessThan(requestedTime, TimeNanoseconds);
        _completedRaster = false;
        while (TimeNanoseconds < requestedTime && !(stopAtFrame && _completedRaster))
        {
            long target = requestedTime;
            if (stopAtFrame && _vdp.NextFrameBoundaryNanoseconds is { } frame) target = Math.Min(target, frame);
            long wire = NextCpuWireDeadline();
            long boundary = Math.Min(wire, checked(target + (target == long.MaxValue ? 0 : 1)));
            _cpu.AdvanceClockSourceUntil(boundary);
            long next = Math.Min(target, Math.Min(NextCpuWireDeadline(), _due[0]));
            if (next < _cpu.TimeNanoseconds || next < TimeNanoseconds)
                throw new InvalidOperationException("CPU crossed an unresolved physical interaction.");
            TimeNanoseconds = next;
            AdvanceCpuWires(next);
            if (next == target)
            {
                SynchronizeVideoDomain(next);
                // A clock at the observation boundary follows the responses at that date.
                _cpu.AdvanceClockSourceUntil(checked(next + 1));
            }
        }
        SynchronizeVideoDomain(TimeNanoseconds);
        SynchronizeReceivers(TimeNanoseconds);
        _videoCpuDue = _vdp.NextCpuEventNanoseconds ?? long.MaxValue;
        _cpuWireDueDirty = true;
        AdvanceAudio(TimeNanoseconds);
    }
}
