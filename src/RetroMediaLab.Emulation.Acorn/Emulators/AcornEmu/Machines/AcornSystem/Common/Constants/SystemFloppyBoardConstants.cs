namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Constants;

internal static class SystemFloppyBoardConstants
{
    internal const int Clock4MHz = 4_000_000;
    internal const int ControllerClock = 2_000_000;
    internal const int DividerSecondClock = 250_000;
    internal const int ReadyClock = 15_625;
    internal const int IndexClock = 5; // Nominal200 ms per revolution; input edges remain external.
    internal const int Control = 2, Interrupt = 3, Decode = 4, Drive = 5, Write = 6;
    internal const int Separator = 7, Divider = 8, Timeout = 9, Ready1 = 10, Ready0 = 11, Latch = 12, Predivider = 14;
    internal const int DriveConnector = 15;
    internal const int LastPart = DriveConnector;
    internal const ushort CpuPageMask = 0xf000;
    internal const byte AllPinsKnown = 0x3f;
    internal const byte Qa = 1, Qb = 2, Qd = 8;
    internal const ushort ReadyTimeoutOutput = 1 << 12; // CD4020B pin2, Q13.
}
