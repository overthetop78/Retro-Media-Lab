using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Enums;

namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;

/// <summary>Résultat local du décodage, consommé par le cœur dans la transaction
/// courante ; ce n'est ni une commande différée ni une file de transferts.</summary>
internal readonly record struct Tms9918ControlCommand(Tms9918ControlCommandKind Kind,
    byte FirstByte, byte SecondByte, bool IsDocumentedFormat)
{
    internal int Register => SecondByte & 7;
    internal ushort Address => (ushort)(((SecondByte & 0x3f) << 8) | FirstByte);
}
