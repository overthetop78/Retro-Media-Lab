using C = RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.SG1000.Constants.Sg1000Constants;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.SG1000.Services;

internal sealed partial class Sg1000Board
{
    public void AdvanceTo(long timeNanoseconds)
        => Advance(timeNanoseconds, false);

    /// <summary>Stops on the VDP's next complete native raster boundary, not a host timer.</summary>
    public void AdvanceFrame() => Advance(long.MaxValue, true);

    private void Advance(long timeNanoseconds, bool stopAtFrame)
    {
        if (_useClockDomains) { AdvanceDomains(timeNanoseconds, stopAtFrame); return; }
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfLessThan(timeNanoseconds, TimeNanoseconds);
        _completedRaster = false;
        while (TimeNanoseconds < timeNanoseconds)
        {
            long edge = _nextMasterTime;
            long next = Math.Min(edge, timeNanoseconds);
            long receiverTime = long.MaxValue;
            foreach (long due in _due) if (due < receiverTime) receiverTime = due;
            next = Math.Min(next, receiverTime);
            if (!_vdpReleased) next = Math.Min(next, C.VdpReleaseNanoseconds);
            if (!_cpuReleased) next = Math.Min(next, C.CpuReleaseNanoseconds);
            if (next < TimeNanoseconds) throw new InvalidOperationException("Stale SG-1000 wire deadline.");
            TimeNanoseconds = next;
            if (receiverTime <= next) AdvanceReceivers(next);
            if (!_vdpReleased && next == C.VdpReleaseNanoseconds) { _vdpReleased = true; _vdp.SetReset(next, false); }
            if (!_cpuReleased && next == C.CpuReleaseNanoseconds) { _cpuReleased = true; _cpu.SetReset(false); }
            if (next == edge)
            {
                _nextMasterIndex++;
                _masterFraction += C.NanosecondsPerSecond % (C.CrystalHertz * 2);
                long increment = C.NanosecondsPerSecond / (C.CrystalHertz * 2);
                if (_masterFraction >= C.CrystalHertz * 2) { _masterFraction -= C.CrystalHertz * 2; increment++; }
                _nextMasterTime = checked(_nextMasterTime + increment);
                _masterHigh = !_masterHigh;
                _vdp.ReceiveMasterClock(next, _masterHigh);
            }
            if (stopAtFrame && _completedRaster) break;
        }
        SynchronizeReceivers(TimeNanoseconds);
        AdvanceAudio(TimeNanoseconds);
    }

    private void AdvanceReceivers(long time)
    {
        // These are the local deadlines received from the physically connected
        // parts. A transaction/clock input also advances its own receiver before
        // changing pins. Stable parts need no repeated call at each quartz edge.
        // Keep the established wire order when deadlines coincide.
        if (_due[3] <= time) _decoder.AdvanceTo(time);
        if (_due[4] <= time) _gates.AdvanceTo(time);
        for (int chip = 0; chip < 2; chip++)
            if (_due[5 + chip] <= time) _joy[chip].AdvanceTo(time);
        for (int chip = 0; chip < 8; chip++)
            if (_due[7 + chip] <= time) _dram[chip].AdvanceTo(time);
        if (_due[15] <= time) _cartridgeLow!.AdvanceTo(time - _cartridgeOrigin);
        if (_due[16] <= time) _cartridgeHigh!.AdvanceTo(time - _cartridgeOrigin);
        if (_due[18] <= time) _cartridgeRam!.AdvanceTo(time - _cartridgeOrigin);
        if (_due[17] <= time) _internalRom!.AdvanceTo(time);
        if (_due[2] <= time) _psg.AdvanceTo(time);
        if (_due[1] <= time) _vdp.AdvanceTo(time);
        if (_due[0] <= time) _cpu.AdvanceTo(time);
    }

    private void SynchronizeReceivers(long time)
    {
        _decoder.AdvanceTo(time);
        _gates.AdvanceTo(time);
        foreach (var mux in _joy) mux.AdvanceTo(time);
        foreach (var memory in _dram) memory.AdvanceTo(time);
        _cartridgeLow?.AdvanceTo(time - _cartridgeOrigin);
        _cartridgeHigh?.AdvanceTo(time - _cartridgeOrigin);
        _cartridgeRam?.AdvanceTo(time - _cartridgeOrigin);
        _internalRom?.AdvanceTo(time);
        _psg.AdvanceTo(time);
        _vdp.AdvanceTo(time);
        _cpu.AdvanceTo(time);
    }
    private void RefreshDeadlines()
    {
        _due[0] = _cpu.NextEventNanoseconds ?? long.MaxValue; _due[1] = _vdp.NextEventNanoseconds ?? long.MaxValue;
        _due[2] = _psg.NextEventNanoseconds ?? long.MaxValue; _due[3] = _decoder.NextEventNanoseconds ?? long.MaxValue;
        _due[4] = _gates.NextEventNanoseconds ?? long.MaxValue;
        for (int chip = 0; chip < 2; chip++) _due[5 + chip] = _joy[chip].NextEventNanoseconds ?? long.MaxValue;
        for (int chip = 0; chip < 8; chip++) _due[7 + chip] = _dram[chip].NextEventNanoseconds ?? long.MaxValue;
        _due[15] = GlobalRomDue(_cartridgeLow?.NextEventNanoseconds) ?? long.MaxValue;
        _due[16] = GlobalRomDue(_cartridgeHigh?.NextEventNanoseconds) ?? long.MaxValue;
        _due[17] = _internalRom?.NextEventNanoseconds ?? long.MaxValue;
        _due[18] = GlobalRomDue(_cartridgeRam?.NextEventNanoseconds) ?? long.MaxValue;
    }
}
