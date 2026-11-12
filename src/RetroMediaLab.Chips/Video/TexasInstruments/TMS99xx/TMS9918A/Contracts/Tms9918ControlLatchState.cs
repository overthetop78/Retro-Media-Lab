namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;

/// <summary>Verrou du premier octet de contrôle et phase de sa transaction.</summary>
public readonly record struct Tms9918ControlLatchState(byte FirstByte, bool AwaitingSecondByte);
