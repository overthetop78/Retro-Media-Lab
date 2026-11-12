using RetroMediaLab.Chips.Logic.Common.Contracts;
using C = RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.SG1000.Constants.Sg1000Constants;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.SG1000.Services;

internal sealed partial class Sg1000Board
{
    private bool _clockDomainsConnected, _synchronizingVideo;
    private long _videoCpuDue = long.MaxValue;

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
            if (next is { } due) _cpu.LimitClockSourceBefore(due);
        };
        _vdp.ReceiveMasterClockSource(new RationalClockSource(0, C.NanosecondsPerSecond,
            C.CrystalHertz * 2, _nextMasterIndex, !_masterHigh));
        _videoCpuDue = _vdp.NextCpuEventNanoseconds ?? long.MaxValue;
        _clockDomainsConnected = true;
    }

    private void ReceiveDomainDeadline(int slot, long? next)
    {
        _due[slot] = next ?? long.MaxValue;
        if (!_useClockDomains || next is not { } due) return;
        if (slot is >= 7 and <= 14) _vdp.LimitMasterClockSourceBefore(due);
        else _cpu.LimitClockSourceBefore(due);
    }

    private long NextVideoMemoryDeadline()
    {
        long next = long.MaxValue;
        for (int i = 7; i <= 14; i++) next = Math.Min(next, _due[i]);
        return next;
    }

    private long NextCpuWireDeadline()
    {
        long next = _videoCpuDue;
        for (int i = 2; i <= 6; i++) next = Math.Min(next, _due[i]);
        for (int i = 15; i <= 18; i++) next = Math.Min(next, _due[i]);
        if (!_vdpReleased) next = Math.Min(next, C.VdpReleaseNanoseconds);
        if (!_cpuReleased) next = Math.Min(next, C.CpuReleaseNanoseconds);
        return next;
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
            while (true)
            {
                long boundary = Math.Min(NextVideoMemoryDeadline(), checked(time + 1));
                int count = _vdp.AdvanceMasterClockSourceUntil(boundary);
                _nextMasterIndex = checked(_nextMasterIndex + count);
                _masterHigh = _vdp.ClockState.MasterHigh;
                long memory = NextVideoMemoryDeadline();
                if (memory > time) break;
                for (int i = 0; i < 8; i++)
                    if (_due[7 + i] <= memory) _dram[i].AdvanceTo(memory);
            }
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
            _vdp.SetReset(time, false);
        }
        if (!_cpuReleased && time == C.CpuReleaseNanoseconds)
        {
            _cpuReleased = true;
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
        AdvanceAudio(TimeNanoseconds);
    }
}
