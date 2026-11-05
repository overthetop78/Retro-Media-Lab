namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants;

/// <summary>États et écritures du firmware 400108-00 pour les recherches et lectures physiques.</summary>
internal static class Acb4000ReadSequencerConstants
{
    internal const byte ReadIdStart = 0x0f;
    internal const byte ReadDataStart = 0x17;
    internal const byte IdCompletionState = 0x08;
    internal const byte IdEccState = 0x0c;
    internal const byte DataState = 0x04;
    internal const byte RetryBranch = 0x05;
    internal const byte CheckedIdNext = 0x68;
    internal const int DataChunkBytes = 256;
    internal const byte ReadOperationMask = 0xcf;
}
