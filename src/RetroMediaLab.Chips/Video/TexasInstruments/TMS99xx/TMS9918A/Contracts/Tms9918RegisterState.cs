namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;

/// <summary>Valeurs initiales/capturées des huit registres écrits par l'interface CPU.
/// Aucun contenu des registres non initialisés par RESET n'est inventé au montage.</summary>
public readonly record struct Tms9918RegisterState(byte R0, byte R1, byte R2, byte R3,
    byte R4, byte R5, byte R6, byte R7);
