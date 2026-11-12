namespace RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2016.Constants;

public static class Tmm2016Constants
{
    // Software representation:13address bits,knowledge and controls,then data/knowledge.
    internal const ulong AddressMask = 0x1fff, AddressKnownMask = 0x2000;
    internal const ulong CsMask = 0x4000, OeMask = 0x8000, WeMask = 0x10000, Cs2Mask = 0x20000;
    internal const ulong ControlsMask = CsMask | OeMask | WeMask, DataBitsMask = 0xffffUL << 24;
    internal const int DataShift = 24, DataKnownShift = 32, OutputKnownShift = 8;
    internal const uint OutputDrivenMask = 0x10000;
    public const int Capacity = 2048, StateVersion = 3;
    internal const int MaximumCapacity = 8192, DefaultSupplyMillivolts = 5000;
    public const long AddressHold = 10, AddressSetup = 20;
    public const long CsEnableMinimum = 10, OeEnableMinimum = 5, WeEnableMinimum = 5;
}
