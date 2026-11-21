namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.ThirtyTwoX.Hardware;

internal sealed partial class Mega32X
{
    // The bank map owns this notification. It is detached with the adapter.
    internal Action? CodeMappingChanged { get; set; }

    internal bool TryGet68kRomAddress(uint address, out uint physical)
    {
        physical = 0;
        if (!_cartridgePresent || !Enabled) return false;
        if (address is >= 0x880000 and < 0x900000) { physical = address & 0x7ffff; return true; }
        if (address is >= 0x900000 and < 0xa00000)
        { physical = (uint)(_romBank * 0x100000) | (address & 0xfffff); return true; }
        return false;
    }
}
