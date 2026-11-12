namespace RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Enums;

/// <summary>Actions locales du séquenceur de bus, sans décodage d'instruction.</summary>
[Flags]
internal enum Z80BusStepAction
{
    None = 0, Sample = 1, Wait = 2, Complete = 4, CompleteWrite = 8,
    RefreshAddress = 16, WriteData = 32, BeginAddress = 64,
}
