using RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMicro.Constants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants;

/// <summary>Décodage du VIA utilisateur et du convertisseur uPD7002 équipant le Model B.</summary>
internal static class BbcMicroIoConstants
{
    internal const int ViaPhaseMask = 1;
    internal const int ViaTrailingPhase = 0;
    internal const byte UnconnectedBus = 0xfe;
    internal const int UserViaRegisterMask = 15;
    internal const ushort UserViaStart = 0xfe60;
    internal const ushort UserViaEnd = 0xfe80;
    internal const ushort AdcStart = 0xfec0;
    internal const ushort AdcEnd = 0xfee0;
    internal const int RegisterMask = RetroMediaLab.Chips.IO.NEC.UPD700x.UPD7002.Constants.Adc7002Constants.RegisterMask;
    internal const int StatusRegister = RetroMediaLab.Chips.IO.NEC.UPD700x.UPD7002.Constants.Adc7002Constants.StatusRegister;
    internal const int HighRegister = RetroMediaLab.Chips.IO.NEC.UPD700x.UPD7002.Constants.Adc7002Constants.HighRegister;
    internal const int LowRegister = 2;
    internal const byte CommandMask = RetroMediaLab.Chips.IO.NEC.UPD700x.UPD7002.Constants.Adc7002Constants.CommandMask;
    internal const byte ChannelMask = RetroMediaLab.Chips.IO.NEC.UPD700x.UPD7002.Constants.Adc7002Constants.ChannelMask;
    internal const byte TwelveBitMode = RetroMediaLab.Chips.IO.NEC.UPD700x.UPD7002.Constants.Adc7002Constants.TwelveBitMode;
    internal const byte Busy = RetroMediaLab.Chips.IO.NEC.UPD700x.UPD7002.Constants.Adc7002Constants.Busy;
    internal const byte Complete = RetroMediaLab.Chips.IO.NEC.UPD700x.UPD7002.Constants.Adc7002Constants.Complete;
    internal const int EightBitCycles = (int)BbcMicroModelConstants.CpuFrequencyHz * 4 / 1000;
    internal const int TwelveBitCycles = (int)BbcMicroModelConstants.CpuFrequencyHz * 10 / 1000;
    internal const int ResultStatusMask = RetroMediaLab.Chips.IO.NEC.UPD700x.UPD7002.Constants.Adc7002Constants.ResultStatusMask;
    internal const int ResultStatusShift = RetroMediaLab.Chips.IO.NEC.UPD700x.UPD7002.Constants.Adc7002Constants.ResultStatusShift;
    internal const int HighShift = RetroMediaLab.Chips.IO.NEC.UPD700x.UPD7002.Constants.Adc7002Constants.HighShift;
    internal const int LowMask = RetroMediaLab.Chips.IO.NEC.UPD700x.UPD7002.Constants.Adc7002Constants.LowMask;
    internal const int AxesPerController = 2;
    internal const int ChannelCount = RetroMediaLab.Chips.IO.NEC.UPD700x.UPD7002.Constants.Adc7002Constants.ChannelCount;
    internal const int ControllerCount = 2;
    internal const byte FirstFireLine = 0x10;
    internal const byte OpenInputs = 0xff;
    internal const byte Cb1Interrupt = 0x10;
}
