namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;

/// <summary>Prochain pixel à produire ; l'origine (0,0) est le début de la zone active.</summary>
public readonly record struct Tms9918RasterState(int Dot, int Line, long Frame);
