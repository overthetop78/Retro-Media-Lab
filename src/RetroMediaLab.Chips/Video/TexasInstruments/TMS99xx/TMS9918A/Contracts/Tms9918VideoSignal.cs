namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;

/// <summary>Échantillon natif logique du VDP, sans agrandissement ni conversion RGB.
/// Sync désigne l'enveloppe de contrôle, pas une synthèse de tension composite.
/// ColourKnown distingue un vrai index du pipeline d'une donnée encore absente.</summary>
public readonly record struct Tms9918VideoSignal(long TimeNanoseconds, int Dot, int Line,
    long Frame, bool Active, bool HorizontalSync, bool VerticalSync, bool Blanking,
    bool ColorBurst, byte Colour, bool ColourKnown, bool ExternalVideo,
    double? ExternalOutputVoltage = null);
