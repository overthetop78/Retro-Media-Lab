namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS;

internal static class SystemHardware
{
    public const byte Sg1000 = 0x01, Sg1000II = 0x02, Sg1000IIExtendedRam = 0x03;
    public const byte MarkIII = 0x10, MasterSystem = 0x20, MasterSystemII = 0x21;
    public const byte GameGear = 0x40, GameGearMasterSystem = 0x41;
    public const byte MegaDrive = 0x80, PowerBaseConverter = 0x81, Pico = 0x82, MegaCd = 0x84;
    public const byte JapanNtsc = 0, JapanPal = 0x40, Usa = 0x80, Europe = 0xc0;
    public const uint NtscClock = 53_693_175, PalClock = 53_203_424;
    public const int CyclesPerLine = 3420, SmsCycleOffset = 530, PbcCycleOffset = 560;
    public static bool IsSixteenBit(byte hardware) => (hardware & PowerBaseConverter) == MegaDrive;
}
