namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants;

/// <summary>Listes de paramètres de l'ACB-4000, OEM sections 6.3.1.11–13.</summary>
public static class Acb4000ParameterConstants
{
    public const int ExtentParameterLength = 12;
    public const int DriveParameterLength = 22;
    public const int ExtentLengthOffset = 3;
    public const byte ExtentLength = 8;
    public const int DensityOffset = 4;
    public const int BlockSizeHighOffset = 9;
    public const int BlockSizeMiddleOffset = 10;
    public const int BlockSizeLowOffset = 11;
    public const int ListCodeOffset = 12;
    public const byte ListCode = 1;
    public const int CylinderOffset = 13;
    public const int HeadOffset = 15;
    public const int ReducedCurrentOffset = 16;
    public const int PrecompensationOffset = 18;
    public const int LandingOffset = 20;
    public const int StepRateOffset = 21;
    public const ushort MinimumCylinders = 1;
    public const ushort MaximumCylinders = 2048;
    public const byte MinimumHeads = 1;
    public const byte MaximumHeads = 16;
    public const ushort MaximumReducedCurrentCylinder = 2047;
    public const ushort DefaultCylinders = 306;
    public const byte DefaultHeads = 2;
    public const ushort DefaultReducedCurrentCylinder = 150;
    public const ushort MinimumBlockSize = 256;
    public const ushort MediumBlockSize = 512;
    public const ushort MaximumBlockSize = 1024;
    public const byte LandingBeforeTrackZeroMask = 0x80;
    public const byte LandingDistanceMask = 0x7f;
    public const byte NonBufferedStepCode = 0;
    public const byte BufferedStepCode = 1;
    public const byte FastBufferedStepCode = 2;
    public const int NonBufferedStepNanoseconds = 3_000_000;
    public const int BufferedStepNanoseconds = 28_000;
    public const int FastBufferedStepNanoseconds = 12_000;
}
