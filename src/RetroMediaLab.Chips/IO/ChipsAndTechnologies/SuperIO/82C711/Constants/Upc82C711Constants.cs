namespace RetroMediaLab.Chips.IO.ChipsAndTechnologies.SuperIO.Chip82C711.Constants;

public static class Upc82C711Constants
{
    public const int ConfigurationAddress = 0x3f0;
    public const int ConfigurationDataAddress = 0x3f1;
    public const int RegisterCount = 4;
    public const int RegisterMask = 0x0f;
    public const byte Unlock = 0x55;
    public const byte Lock = 0xaa;
    public const int UnlockWrites = 2;
    public const byte OpenBus = 0xff;
    public const int EnableRegister = 0;
    public const int AddressRegister = 1;
    public const int SerialRegister = 2;
    public const byte InitialEnable = 0x3f;
    public const byte InitialAddress = 0x9f;
    public const byte InitialSerial = 0xdc;
    public const int IdeEnable = 0x01;
    public const int FloppyEnable = 0x18;
    public const int ParallelEnable = 0x04;
    public const int ConfigurationReadEnable = 0x80;
    public const int Serial1Enable = 0x0c;
    public const int Serial2Enable = 0xc0;
    public const int IdeBase = 0x1f0;
    public const int IdeControl = 0x3f6;
    public const int FloppyBase = 0x3f0;
    public const int EightRegisterMask = 7;
    public const int FourRegisterMask = 3;
    public const int ParallelSelectionMask = 3;
    public const int AuxiliarySerialShift = 5;
    public const int Serial2SelectionShift = 4;
    public const int SerialSelectionMask = 3;
    public const int AuxiliarySerialFirstSelection = 2;
    public const int AuxiliarySerialPairSize = 2;
    public const int SerialCom1 = 0x3f8;
    public const int SerialCom2 = 0x2f8;
    public const int ParallelMonochrome = 0x3bc;
    public const int ParallelPrimary = 0x378;
    public const int ParallelSecondary = 0x278;
    public static ReadOnlySpan<int> ParallelAddresses =>
        [0, ParallelMonochrome, ParallelPrimary, ParallelSecondary];

    // CR1[6:5] selects the COM3/COM4 pair; COM1 and COM2 remain fixed.
    public static ReadOnlySpan<int> AuxiliarySerialAddresses =>
        [0x338, 0x238, 0x3e8, 0x2e8, 0x2e8, 0x2e0, 0x220, 0x228];
}
