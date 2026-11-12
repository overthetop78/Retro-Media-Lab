namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Constants;

/// <summary>TI MP010A et notes originales Macourek/Guttag. La chute MOS est
/// typique, non une caractérisation de chaque exemplaire.</summary>
internal static class Tms9918ExternalVideoRequirements
{
    internal const double ResetLowVolts = 0.8, SyncHighVolts = 9.0, NominalMosDropVolts = 0.7;
    internal const long VerticalSyncMinimumNanoseconds = 7200;
}
