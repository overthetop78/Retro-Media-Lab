namespace RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2009.Contracts;

public readonly record struct Tmm2009Pins(ushort Address, bool AddressKnown, bool Ce1High,
    bool Ce2High, bool OutputEnableHigh, bool WriteHigh, byte Data, byte DataKnown);

/// <summary>Driven=false denotes Hi-Z; when driven, Known identifies the valid data bits.</summary>
public readonly record struct Tmm2009Output(byte Data, byte Known, bool Driven);

public sealed record Tmm2009State(int Version, bool Ce2ActiveHigh, long Time,
    byte[] Cells, byte[] Known, Tmm2009Pins Pins, bool Writing,
    ushort WriteAddress, bool WriteAddressKnown, byte WriteData, byte WriteKnown);
