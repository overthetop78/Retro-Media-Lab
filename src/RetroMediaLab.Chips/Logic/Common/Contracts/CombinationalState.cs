namespace RetroMediaLab.Chips.Logic.Common.Contracts;

/// <summary>Independent snapshot of a combinatorial package, including pending pin propagation.</summary>
public sealed record CombinationalState(int Version, string Part, long TimeNanoseconds,
    ulong InputLevels, ulong InputKnown, ushort Outputs, ushort Targets,
    long?[] DueNanoseconds, long[] TimingNanoseconds);
