using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Enums;

namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;

/// <summary>Compteur et registre de données CPU uniques. Une adresse null est
/// non qualifiée, notamment après une écriture de registre (TI §2.1.2).
/// RequestKnown est une qualification logicielle du modèle de conflit,
/// pas un registre matériel ni une seconde requête en attente.</summary>
public readonly record struct Tms9918CpuMemoryState(ushort? Address, byte Data,
    Tms9918CpuVramOperation PendingOperation, bool DataKnown = true, bool RequestKnown = true);
