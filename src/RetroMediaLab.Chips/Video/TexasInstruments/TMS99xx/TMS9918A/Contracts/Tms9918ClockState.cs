namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;

/// <summary>État canonique des diviseurs : phase modulo 12 demi-périodes maîtres.
/// GROMCLK conserve sa phase modulo 48 demi-périodes, indépendamment du reset
/// du balayage. Formes symétriques nominales ; phase électrique non caractérisée.</summary>
public readonly record struct Tms9918ClockState(
    bool MasterHigh, int HalfEdgePhase, bool CpuClockHigh, bool PixelClockHigh, long HalfEdges,
    int GromHalfEdgePhase = 0, bool GromClockHigh = false);
