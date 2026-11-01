using RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMicro.Constants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants;

/// <summary>Décodage du VIA utilisateur et du convertisseur uPD7002 équipant le Model B.</summary>
internal static class BbcMicroIoConstants
{
    internal const byte UnconnectedBus = 0xfe;
    internal const int UserViaRegisterMask = 15;
    internal const ushort UserViaStart = 0xfe60;
    internal const ushort UserViaEnd = 0xfe80;
    internal const ushort AdcStart = 0xfec0;
    internal const ushort AdcEnd = 0xfee0;
    internal const int RegisterMask = 3;
    internal const int StatusRegister = 0;
    internal const int HighRegister = 1;
    internal const int LowRegister = 2;
    internal const byte CommandMask = 0x0f;
    internal const byte ChannelMask = 3;
    internal const byte TwelveBitMode = 8;
    internal const byte Busy = 0x80;
    internal const byte Complete = 0x40;
    internal const int EightBitCycles = (int)BbcMicroModelConstants.CpuFrequencyHz * 4 / 1000;
    internal const int TwelveBitCycles = (int)BbcMicroModelConstants.CpuFrequencyHz * 10 / 1000;
    internal const int ResultStatusMask = 0xc000;
    internal const int ResultStatusShift = 10;
    internal const int HighShift = 8;
    internal const int LowMask = 0xf0;
    internal const int AxesPerController = 2;
    internal const int ChannelCount = 4;
    internal const int ControllerCount = 2;
    internal const byte FirstFireLine = 0x10;
    internal const byte OpenInputs = 0xff;
    internal const byte Cb1Interrupt = 0x10;
}
