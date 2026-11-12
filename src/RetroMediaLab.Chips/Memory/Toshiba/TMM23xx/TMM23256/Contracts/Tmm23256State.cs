using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Models;

namespace RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Contracts;

public readonly record struct Tmm23256Pins(ushort Address, bool AddressKnown, bool CeHigh, bool OeHigh);
public readonly record struct Tmm23256Output(byte Data, bool Known, bool Driven);

/// <summary>Immutable ROM contents belong to the package; the hash prevents restoring against another mask.</summary>
public sealed record Tmm23256State(int Version, string RomHash, Tmm23256Timing Timing,
    long Time, Tmm23256Pins Pins, ushort LatchedAddress, bool LatchedKnown, bool Initialized,
    bool InitialCeHigh, bool TimingQualified, long CeFall, long CeRise, long OeFall,
    long AccessReady, long? OutputDue, Tmm23256Output Target, Tmm23256Output Output,
    long AddressChanged, long? InvalidateDue);
