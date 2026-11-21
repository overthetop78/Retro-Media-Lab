namespace RetroMediaLab.Chips.CPU.Motorola.M68000.Execution;

/// <summary>Clock and bus behaviour supplied by the owning board, in its clock units.</summary>
public sealed record Cpu68000Configuration(
    int ClockMultiplier = 1,
    bool Prefetch = true,
    bool AddressErrors = true,
    bool TrackInstructionAddress = true,
    bool WriteTasMemory = true,
    int RefreshPeriod = 0,
    int RefreshStall = 0,
    int RefreshSkipAdvance = 0,
    int ResetRefreshAdvance = 0,
    uint? PrefetchResetAddress = 0x1000);
