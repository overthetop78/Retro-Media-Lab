namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;

/// <summary>Description reçue de la source physique. Date du front i :
/// Origin + floor(i * HalfPeriodNumerator / HalfPeriodDenominator).
/// FirstEdgeIndex conserve la fraction entre deux livraisons. Aucun oscillateur local.</summary>
public readonly record struct Tms9918ClockTrain(long OriginNanoseconds,
    long HalfPeriodNumerator, long HalfPeriodDenominator, long FirstEdgeIndex, int EdgeCount);
