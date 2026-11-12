namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;

/// <summary>État du récepteur de lecture ; une sélection présente à la création
/// n'est pas supposée avoir commencé par une impulsion valide.</summary>
public readonly record struct Tms9918StatusReadState(
    long TimeNanoseconds, Tms9918CpuPins Pins, bool StatusSelected, bool SelectionQualified);
