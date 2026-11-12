namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Constants;

/// <summary>Exigences minimales en ns, TI MP010A §5.4, figure 5-3.
/// Elles ne sont pas les temps d'accès ni les créneaux VRAM.</summary>
internal static class Tms9918CpuTimingRequirements
{
    internal const long ModeSetup = 30, ModeHold = 30, DataSetup = 100, DataHold = 30, WriteLow = 200;
}
