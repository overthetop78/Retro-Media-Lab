namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Upc82C711.Constants;

internal static class Upc82C711Constants
{
    internal const int ConfigurationAddress = 0x3f0;
    internal const int ConfigurationDataAddress = 0x3f1;
    internal const int RegisterCount = 4;
    internal const int RegisterMask = 0x0f;
    internal const byte Unlock = 0x55;
    internal const byte Lock = 0xaa;
    internal const int UnlockWrites = 2;
    internal const byte OpenBus = 0xff;
    internal const int EnableRegister = 0;
    internal const int AddressRegister = 1;
    internal const int SerialRegister = 2;
    internal const byte InitialEnable = 0x3f;
    internal const byte InitialAddress = 0x9f;
    internal const byte InitialSerial = 0xdc;
    internal const int IdeEnable = 0x01;
    internal const int FloppyEnable = 0x18;
    internal const int ParallelEnable = 0x04;
    internal const int ConfigurationReadEnable = 0x80;
    internal const int Serial1Enable = 0x0c;
    internal const int Serial2Enable = 0xc0;
    internal const int IdeBase = 0x1f0;
    internal const int IdeControl = 0x3f6;
    internal const int FloppyBase = 0x3f0;
    internal const int EightRegisterMask = 7;
    internal const int FourRegisterMask = 3;
    internal const int ParallelSelectionMask = 3;
    internal const int AuxiliarySerialShift = 5;
    internal const int Serial2SelectionShift = 4;
    internal const int SerialSelectionMask = 3;
    internal const int AuxiliarySerialFirstSelection = 2;
    internal const int AuxiliarySerialPairSize = 2;
    internal const int SerialCom1 = 0x3f8;
    internal const int SerialCom2 = 0x2f8;
    internal const int ParallelMonochrome = 0x3bc;
    internal const int ParallelPrimary = 0x378;
    internal const int ParallelSecondary = 0x278;

    internal static ReadOnlySpan<int> ParallelAddresses =>
        [0, ParallelMonochrome, ParallelPrimary, ParallelSecondary];

    // CR1[6:5] selects the COM3/COM4 pair; COM1 and COM2 remain fixed.
    internal static ReadOnlySpan<int> AuxiliarySerialAddresses =>
        [0x338, 0x238, 0x3e8, 0x2e8, 0x2e8, 0x2e0, 0x220, 0x228];
}
