namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;

/// <summary>Sortie CPUCLK réellement fournie par le VDP, avec son front daté.</summary>
public readonly record struct Tms9918ClockOutput(long TimeNanoseconds, bool High);
