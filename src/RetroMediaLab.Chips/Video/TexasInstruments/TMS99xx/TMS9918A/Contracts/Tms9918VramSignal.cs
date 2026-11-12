namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;

/// <summary>Sorties VRAM physiques groupées et datées ; la mémoire raccordée
/// reçoit ces changements et transmet RD par l'entrée du VDP.</summary>
public readonly record struct Tms9918VramSignal(long TimeNanoseconds, Tms9918VramPins Pins);
