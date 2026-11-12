namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;

public readonly record struct Tms9918VramRead(long TimeNanoseconds, byte Data, bool DataKnown);
