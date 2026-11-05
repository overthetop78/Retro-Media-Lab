namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants;

/// <summary>Paramètres répartis dans les neuf premiers drapeaux ID par le firmware 400108-00.</summary>
public static class Acb4000PhysicalParameterConstants
{
    public const int FlagCount = 9;
    public const int IdCheckAttempts = 4;
    public const int BlockSizeOffset = 0;
    public const int CylinderOffset = 2;
    public const int ReducedCurrentOffset = 4;
    public const int DerivedOffset = 6;
    public const int LandingOffset = 8;
    public const int PackedWordShift = 7;
    public const int AuxiliaryShift = 5;
    public const int HeadUpperShift = 2;
    public const byte FlagMask = 0x7f;
    public const byte WordHighMask = 0x1f;
    public const byte AuxiliaryMask = 0x03;
    public const byte LandingSignFlag = 0x40;
    public const uint MaximumSyncsBetweenIds = 2;
}
