namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants;

/// <summary>Reprogrammation FORMAT du firmware 400108-00, routines 146Fh–14D2h.</summary>
internal static class Acb4000FormatSequencerConstants
{
    internal const byte WaitForIndexState = 0x15;
    internal const byte GapState = 0x10;
    internal const byte PostDataState = 0x12;
    internal const byte IdEccState = 0x0c;
    internal const byte IdContinuationState = 0x11;
    internal const byte DataEccState = 0x0d;
    internal const byte DataContinuation = 0x94;
    internal const byte TailState = 0x13;
    internal const byte TailGapState = 0x16;
    internal const byte TailContinuation = 0x56;
    internal const byte DataState = 0x04;
    internal const byte DataControl = 0x05;
    internal const byte GapCount = 0x08;
    internal const int TransferChunkBytes = 256;
}
