namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants;

/// <summary>Limites du port SCSI ACB-4000, manuel OEM mars 1984, sections 4.7 et annexe A.</summary>
public static class Acb4000BusConstants
{
    public const byte DefaultTargetId = 0;
    public const byte MaximumTargetId = 7;
    public const int NanosecondsPerSecond = 1_000_000_000;
    public const int BusSettleNanoseconds = 450;
    public const int DeskewNanoseconds = 45;
    public const int MinimumRequestTimeoutNanoseconds = 250_000_000;
    public const int MaximumSelectionResponseNanoseconds = 200_000;
    public const int MaximumBusClearNanoseconds = 650;
    public const int RecommendedResetHoldNanoseconds = 25_000;
    public const int SupportedResetPulseNanoseconds = 50;
    public const byte InputOutputMask = 1;
    public const byte ControlDataMask = 2;
    public const byte MessageMask = 4;
    public const byte InformationPhaseMask = 7;
}
