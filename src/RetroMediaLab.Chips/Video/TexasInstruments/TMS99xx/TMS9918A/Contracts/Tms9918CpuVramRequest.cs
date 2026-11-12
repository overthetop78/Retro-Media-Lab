using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Enums;

namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;

/// <summary>Observation de la requête locale courante, pas une entrée de FIFO.</summary>
internal readonly record struct Tms9918CpuVramRequest(ushort? Address, byte Data,
    Tms9918CpuVramOperation Operation, bool Qualified = true);
