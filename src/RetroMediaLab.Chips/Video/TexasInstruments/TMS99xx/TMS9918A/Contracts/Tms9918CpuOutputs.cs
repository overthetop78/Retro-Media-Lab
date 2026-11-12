namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;

/// <summary>CD piloté ou relâché et sortie INT active à zéro ; la date est celle
/// de la notification. DataKnown n'est pas une résistance de rappel du bus.</summary>
public readonly record struct Tms9918CpuOutputs(
    long TimeNanoseconds, byte Data, bool DataDriven, bool DataKnown, bool IntHigh);
