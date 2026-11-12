namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;

/// <summary>Entrées et réponse CPU encore en cours à la date de capture.</summary>
public readonly record struct Tms9918CpuInterfaceState(Tms9918CpuPins Pins,
    bool ReadMode, bool ReadQualified, bool Reset, long ResetStarted, bool Qualified,
    long? DataDue, bool DataTargetDriven);
