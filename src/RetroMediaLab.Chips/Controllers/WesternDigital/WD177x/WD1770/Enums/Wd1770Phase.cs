namespace RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Enums;

/// <summary>États du séquenceur physique ; aucun état ne représente un format d’image.</summary>
public enum Wd1770Phase
{
    Idle,
    SpinUp,
    DirectionSetup,
    StepPulse,
    StepDelay,
    Settle,
    SearchId,
    ReadId,
    SearchData,
    ReadData,
    ReadDataCrc,
    AddressTail,
    WaitReadTrack,
    ReadTrack,
    WriteGap,
    WriteLead,
    WriteMark,
    WriteData,
    WriteCrc,
    WriteTail,
    WriteFinish,
    WriteTrackPrime,
    WaitWriteTrack,
    WriteTrack
}
