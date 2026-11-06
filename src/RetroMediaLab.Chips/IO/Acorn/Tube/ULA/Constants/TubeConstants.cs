namespace RetroMediaLab.Chips.IO.Acorn.Tube.ULA.Constants;

public static class TubeConstants
{
    public const int Channels = 4;
    public const int RegisterMask = 7;
    public const int ChannelOneCapacity = 24;
    public const int LatchCapacity = 1;
    public const int SingleByteTransfer = 1;
    public const int TwoByteTransfer = 2;
    public const int DataRegisterMask = 1;
    public const int ChannelThree = 2;
    public const int ChannelThreeCapacity = 2;
    public const int ChannelFour = 3;
    public const byte Available = 0x80;
    public const byte Space = 0x40;
    public const byte HostInterrupt = 0x01;
    public const byte ParasiteInterruptOne = 0x02;
    public const byte ParasiteInterruptFour = 0x04;
    public const byte ParasiteNmi = 0x08;
    public const byte TwoByteTransfers = 0x10;
    public const byte ParasiteReset = 0x20;
    public const byte ResetRegisters = 0x40;
    public const byte SetFlags = 0x80;
    public const byte ControlMask = 0x3f;
    public const byte WritableControlMask = ControlMask | ResetRegisters;
    public const int DmaDataRegister = 5;
    public const int TurboRamBytes = 65536;
    public const int TurboRomBytes = 2048;
    public const int TurboRomStart = 0xf000;
    public const int TurboRegisters = 0xfef8;
    public const int TurboCyclesPerHostCycle = 2;
    public const int Master512CyclesPerHostCycle = 5;
    public const int Master512RamBytes = 524288;
    public const int Master512RomBytes = 16384;
    public const int X86AddressMask = 0xfffff;
    public const int X86RomStart = 0xc0000;
    public const int X86RamMirrorStart = 0x80000;
    public const int X86RamMirrorOffset = 0x40000;
    public const int X86TubePort = 0x80;
}
