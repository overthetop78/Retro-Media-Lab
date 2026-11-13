namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.SG1000.Services;

internal sealed partial class Sg1000Board
{
    public void AdvanceTo(long timeNanoseconds) => AdvanceRangeBus(timeNanoseconds, false);
    public void AdvanceFrame() => AdvanceRangeBus(long.MaxValue, true);

    private void SynchronizeReceivers(long time)
    {
        // The feed-forward network must deliver pending responses before
        // advancing owner timestamps for an external observation.
        ResolveWiredLogicUntil(time);
        ResolveWiredJoyUntil(0, time); ResolveWiredJoyUntil(1, time);
        _decoder.AdvanceTo(time);
        _gates.AdvanceTo(time);
        foreach (var mux in _joy) mux.AdvanceTo(time);
        foreach (var memory in _dram) memory.AdvanceTo(time);
        long cartridgeTime = time - _cartridgeOrigin;
        if (_cartridgeLow is { } low) AcceptWiredMemory(0, low.AdvanceWiredTime(cartridgeTime));
        if (_cartridgeHigh is { } high) AcceptWiredMemory(1, high.AdvanceWiredTime(cartridgeTime));
        if (_cartridgeRam is { } ram) AcceptWiredMemory(3, ram.AdvanceWiredTime(cartridgeTime));
        if (_internalRom is { } internalRom) AcceptWiredMemory(2, internalRom.AdvanceWiredTime(time));
        _psg.AdvanceTo(time);
        _vdp.AdvanceTo(time);
        _cpu.AdvanceTo(time);
    }
}