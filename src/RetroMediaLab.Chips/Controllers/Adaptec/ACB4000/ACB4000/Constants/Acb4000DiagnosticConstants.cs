namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants;

/// <summary>Liste SEND DIAGNOSTIC du contrôleur ACB-4000.</summary>
public static class Acb4000DiagnosticConstants
{
    public const byte DumpHardwareOption = 0x61;
    public const byte DumpRamOption = 0x62;
    public const byte PatchHardwareOption = 0x63;
    public const byte PatchRamOption = 0x64;
    public const int AddressOffset = 2;
    public const int PatchCountOffset = 3;
    public const int MaximumPatchLength = 256;
    public const ushort HardwareBase = 0x4000;
    public const ushort RamBase = 0x8000;
    public const int DumpHeaderLength = 4;
    public const int DumpLengthHighOffset = 0;
    public const int DumpLengthLowOffset = 1;
    public const int DumpAddressHighOffset = 2;
    public const int DumpAddressLowOffset = 3;
    public const ushort DeclaredDumpLength = 0x0104;
    // Firmware 400108-00, 1B5F-1B69: DCR D / RZ precedes the first read, hence 255 bytes.
    public const int DumpMemoryLength = byte.MaxValue;

    public const byte ReadErrorHandlingOption = 0x65;
    public const byte ReinitializeDriveOption = 0x60;
    public const byte FirmwareQualifier = 0x30;
    public const byte ReinitializeFailure = 0x1d;
    public const int ParameterLength = 4;
    public const int OptionOffset = 0;
    public const int QualifierOffset = 1;
    public const int ReadModeOffset = 2;
    public const int CdbReservedOffset = 2;
    public const int CdbLengthOffset = 3;
}
