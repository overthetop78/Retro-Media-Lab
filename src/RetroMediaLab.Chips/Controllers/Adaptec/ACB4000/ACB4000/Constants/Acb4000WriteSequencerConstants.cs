namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants;

/// <summary>États et reprogrammation WRITE du firmware 400108-00, routine 1430h.</summary>
internal static class Acb4000WriteSequencerConstants
{
    internal const byte StartState = 0x09;
    internal const byte DataState = 0x04;
    internal const byte EccState = 0x0d;
    internal const byte CompletionState = 0x14;
    internal const byte DataControl = 0x05;
}
