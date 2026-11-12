using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Enums;

namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;

/// <summary>Destination et indice de cellule/sprite de la sélection courante.
/// Ce contrat ne fixe ni l'adresse multiplexée ni la phase de capture RD.</summary>
internal readonly record struct Tms9918VramSlot(Tms9918VramSlotKind Kind, int Index);
