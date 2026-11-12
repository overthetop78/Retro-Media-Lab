namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;

/// <summary>Entrées physiques côté CPU du VDP ; CSR/CSW sont les niveaux
/// électriques (false = sélection active). DataKnown distingue les données
/// connues des données non résolues d'un bus partagé.</summary>
public readonly record struct Tms9918CpuPins(byte Data, bool DataKnown, bool Mode,
    bool CsrHigh, bool CswHigh);
