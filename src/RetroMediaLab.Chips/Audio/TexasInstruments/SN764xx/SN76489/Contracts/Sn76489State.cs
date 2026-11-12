namespace RetroMediaLab.Chips.Audio.TexasInstruments.SN764xx.SN76489.Contracts;

/// <summary>Registres, compteurs et bus du générateur, sans récepteur audio hôte.</summary>
public sealed record Sn76489State
{
    public required int FormatVersion { get; init; }
    public required int[] Periods { get; init; }
    public required int[] Counts { get; init; }
    public required int[] Amplitudes { get; init; }
    public required bool[] Levels { get; init; }
    public required int Divider { get; init; }
    public required int Selected { get; init; }
    public required int Noise { get; init; }
    public required int NoisePeriod { get; init; }
    public required int Shift { get; init; }
    public required int TransferClocks { get; init; }
    public required byte BusData { get; init; }
    public required bool ChipSelected { get; init; }
    public required bool WriteEnabled { get; init; }
    public required int Output { get; init; }
    public required int ReadyAssertDelayNanoseconds { get; init; }
    public required bool ReadyOutput { get; init; }
    public required bool DirectTransfer { get; init; }
    public required bool ClockHigh { get; init; }
    public required long InputClockCount { get; init; }
    public required long TimeNanoseconds { get; init; }
    public required long? ReadyDueNanoseconds { get; init; }
}
