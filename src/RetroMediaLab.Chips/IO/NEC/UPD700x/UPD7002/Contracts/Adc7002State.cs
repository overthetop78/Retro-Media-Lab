using RetroMediaLab.Chips.IO.NEC.UPD700x.UPD7002.Enums;

namespace RetroMediaLab.Chips.IO.NEC.UPD700x.UPD7002.Contracts;

/// <summary>État numérique du convertisseur, canaux et charge accumulée ; aucun récepteur hôte.</summary>
public sealed record Adc7002State
{
    public required int FormatVersion { get; init; }
    public required int InputClockFrequency { get; init; }
    public required int ConversionClockFrequency { get; init; }
    public required ushort[] Channels { get; init; }
    public required bool TestMode { get; init; }
    public required bool WriteActive { get; init; }
    public required int WriteRegister { get; init; }
    public required byte WriteData { get; init; }
    public required long InputClockCount { get; init; }
    public required long? PublishedClockEvent { get; init; }
    public required Adc7002Phase Phase { get; init; }
    public required long Charge { get; init; }
    public required long IntegratedTicks { get; init; }
    public required long Remaining { get; init; }
    public required ushort Sample { get; init; }
    public required ushort Result { get; init; }
    public required byte Status { get; init; }
    public required byte Command { get; init; }
}
