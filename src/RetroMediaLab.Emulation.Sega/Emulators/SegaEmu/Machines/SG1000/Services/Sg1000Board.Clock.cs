namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.SG1000.Services;

internal sealed partial class Sg1000Board
{
    public void AdvanceTo(long timeNanoseconds) => AdvanceRangeBus(timeNanoseconds, false);
    public void AdvanceFrame() => AdvanceRangeBus(long.MaxValue, true);

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
}