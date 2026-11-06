namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Enums;

[Flags]
internal enum BbcInterruptSources
{
    None = 0,
    SystemVia = 1,
    UserVia = 2,
    Serial = 4,
    Winchester = 8,
    RealTimeClock = 16,
    AccessControl = 32,
    Tube = 64
}
