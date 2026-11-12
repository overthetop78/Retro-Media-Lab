namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;

/// <summary>Signal analogique mémorisé et qualification du port RESET/SYNC.</summary>
public readonly record struct Tms9918ExternalVideoState(double VideoVoltage, bool VideoKnown,
    double ResetSyncVoltage, bool ResetSyncKnown, bool SyncActive, bool SyncPending,
    long SyncStarted, bool VerticalSyncRecognized);
