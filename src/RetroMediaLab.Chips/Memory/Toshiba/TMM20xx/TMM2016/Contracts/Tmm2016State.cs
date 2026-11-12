using RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2016.Models;

namespace RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2016.Contracts;

public readonly record struct Tmm2016Pins(ushort Address, bool AddressKnown, bool CsHigh,
    bool OeHigh, bool WeHigh, byte Data, byte DataKnown, bool Cs2High = true);
public readonly record struct Tmm2016Output(byte Data, byte Known, bool Driven);

/// <summary>Memory and timing phases, not delegates, output observers or any host resource.</summary>
public sealed record Tmm2016State(int Version, Tmm2016Timing Timing, long Time,
    byte[] Cells, byte[] Known, Tmm2016Pins Pins, long AddressChanged, long DataChanged,
    long CsStarted, long OeStarted, long WriteStarted, ushort WriteAddress, bool WriteAddressKnown,
    bool Writing, bool Qualified, bool PostWriteUnknown, long LastWrite, ushort LastWriteAddress,
    long LastWriteAddressStarted, bool LastWriteAddressKnown, byte ReadData, byte ReadKnown,
    byte TargetData, byte TargetKnown, bool Driven, bool DriveTarget,
    long? DataDue, long? InvalidateDue, long? DriveDue, long? WriteEnableDue, long? WriteDisableDue,
    bool WriteOutputSelected, int SupplyMillivolts, long SupplyRecoveryEnd);
