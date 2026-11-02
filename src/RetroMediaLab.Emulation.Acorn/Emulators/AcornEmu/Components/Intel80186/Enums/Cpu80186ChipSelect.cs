namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Intel80186.Enums;

[Flags]
internal enum Cpu80186ChipSelect : ushort
{
    None = 0,
    Upper = 0x0001,
    Lower = 0x0002,
    Middle0 = 0x0004,
    Middle1 = 0x0008,
    Middle2 = 0x0010,
    Middle3 = 0x0020,
    Peripheral0 = 0x0040,
    Peripheral1 = 0x0080,
    Peripheral2 = 0x0100,
    Peripheral3 = 0x0200,
    Peripheral4 = 0x0400,
    Peripheral5 = 0x0800,
    Peripheral6 = 0x1000
}
