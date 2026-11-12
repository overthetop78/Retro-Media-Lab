namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;

/// <summary>Sorties VRAM physiques : AD est multiplexé ligne/colonne/données.
/// Les niveaux high sont électriques ; R/W haut désigne une lecture.
/// AD est une sortie pilotée ; Known distingue une valeur qualifiée d'une phase indéfinie.</summary>
public readonly record struct Tms9918VramPins(byte AddressData, bool AddressDataKnown,
    bool RasHigh, bool CasHigh, bool ReadWriteHigh);
