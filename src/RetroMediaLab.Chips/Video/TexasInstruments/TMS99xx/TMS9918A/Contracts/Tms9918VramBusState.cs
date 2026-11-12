namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;

public readonly record struct Tms9918VramBusState(long TimeNanoseconds, Tms9918VramPins Pins,
    byte InputData, bool InputDataKnown, long InputDataChanged, Tms9918VramRead? LastRead);
