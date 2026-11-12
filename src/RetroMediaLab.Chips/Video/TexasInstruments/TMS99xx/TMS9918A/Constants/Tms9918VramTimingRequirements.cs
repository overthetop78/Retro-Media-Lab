namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Constants;

/// <summary>TI MP010A §5.4, figure 5-6 ; ns avant CAS montant. Hold = 0 ns.</summary>
internal static class Tms9918VramTimingRequirements
{
    internal const long ReadDataSetup = 60;
}
