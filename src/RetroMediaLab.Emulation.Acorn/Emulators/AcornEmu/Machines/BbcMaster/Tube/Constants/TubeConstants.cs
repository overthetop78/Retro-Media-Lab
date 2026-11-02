namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMaster.Tube.Constants;

internal static class TubeConstants
{
    internal const int Channels = 4;
    internal const int RegisterMask = 7;
    internal const int ChannelOneCapacity = 24;
    internal const int ChannelThree = 2;
    internal const int ChannelThreeCapacity = 2;
    internal const int ChannelFour = 3;
    internal const byte Available = 0x80;
    internal const byte Space = 0x40;
    internal const byte HostInterrupt = 0x01;
    internal const byte ParasiteInterruptOne = 0x02;
    internal const byte ParasiteInterruptFour = 0x04;
    internal const byte ParasiteNmi = 0x08;
    internal const byte TwoByteTransfers = 0x10;
    internal const byte ParasiteReset = 0x20;
    internal const byte ResetRegisters = 0x40;
    internal const byte SetFlags = 0x80;
    internal const byte ControlMask = 0x3f;
    internal const int TurboRamBytes = 65536;
    internal const int TurboRomBytes = 2048;
    internal const int TurboRomStart = 0xf000;
    internal const int TurboRegisters = 0xfef8;
    internal const int TurboCyclesPerHostCycle = 2;
    internal const int Master512CyclesPerHostCycle = 5;
    internal const int Master512RamBytes = 524288;
    internal const int Master512RomBytes = 16384;
    internal const int X86AddressMask = 0xfffff;
    internal const int X86RomStart = 0xc0000;
    internal const int X86RamMirrorStart = 0x80000;
    internal const int X86RamMirrorOffset = 0x40000;
    internal const int X86TubePort = 0x80;
}
