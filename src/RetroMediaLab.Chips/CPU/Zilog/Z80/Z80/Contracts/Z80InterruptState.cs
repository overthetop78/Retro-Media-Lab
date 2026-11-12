using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Enums;

namespace RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Contracts;

/// <summary>Bascules d'interruption uniquement ; les phases et transactions appartiennent au CPU.</summary>
public readonly record struct Z80InterruptState(bool Iff1, bool Iff2, Z80InterruptMode Mode,
    byte EiInhibitRemaining, bool IntAsserted, bool NmiAsserted, bool NmiPending);
