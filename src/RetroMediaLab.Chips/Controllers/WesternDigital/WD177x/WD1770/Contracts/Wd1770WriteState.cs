namespace RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Contracts;

public sealed record Wd1770WriteState
{
    public required bool DoubleDensity { get; init; }
    public required bool PrecompensationEnabled { get; init; }
    public required bool WriteData { get; init; }
    public required uint Word { get; init; }
    public required bool Gate { get; init; }
    public required ulong HalfClocks { get; init; }
    public required int RemainingCells { get; init; }
    public required bool PreviousData { get; init; }
    public required int DataHistory { get; init; }
    public required bool NextDataBit { get; init; }
    public required ulong NextCell { get; init; }
    public required ulong RisingEdge { get; init; }
    public required ulong FallingEdge { get; init; }
    public required ushort Crc { get; init; }
}
