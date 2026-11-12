namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;

public readonly record struct Tms9918CpuWriteState(long TimeNanoseconds, Tms9918CpuPins Pins,
    bool WriteMode, bool ModeQualified, long WriteStart, long ModeChanged, long DataChanged,
    long? LastCapture, bool QualificationLost);
