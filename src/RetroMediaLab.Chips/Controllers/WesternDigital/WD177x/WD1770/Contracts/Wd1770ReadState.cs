namespace RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Contracts;

public sealed record Wd1770ReadState
{
    public required bool DoubleDensity { get; init; }
    public required bool PreviousReadData { get; init; }
    public required bool Sampling { get; init; }
    public required bool Pulse { get; init; }
    public required bool Aligned { get; init; }
    public required bool PreviousDataBit { get; init; }
    public required int SampleCountdown { get; init; }
    public required int WordBits { get; init; }
    public required int A1Run { get; init; }
    public required int C2Run { get; init; }
    public required ushort Shift { get; init; }
    public required uint History { get; init; }
}
