namespace RetroMediaLab.Chips.Memory.Common.Contracts;

public readonly record struct TimedMemoryInput<TPins>(long TimeNanoseconds, TPins Pins) where TPins : struct;
public readonly record struct TimedMemoryOutput<TOutput>(long TimeNanoseconds, TOutput Output) where TOutput : struct;

/// <summary>Borrowed complete input words, stable between changes and through
/// the supplied bound. Transport metadata only, never a memory's state.</summary>
public readonly ref struct MemoryInputRange<TPins>(ReadOnlySpan<TimedMemoryInput<TPins>> changes,
    long knownUntilNanoseconds) where TPins : struct
{
    public const int MaximumChanges = 128;
    public ReadOnlySpan<TimedMemoryInput<TPins>> Changes { get; } = changes;
    public long KnownUntilNanoseconds { get; } = knownUntilNanoseconds;

    internal void Validate(long currentTime)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(Changes.Length, MaximumChanges);
        ArgumentOutOfRangeException.ThrowIfLessThan(KnownUntilNanoseconds, currentTime);
        long previous = currentTime;
        foreach (ref readonly var change in Changes)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(change.TimeNanoseconds, previous);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(change.TimeNanoseconds, KnownUntilNanoseconds);
            previous = change.TimeNanoseconds;
        }
    }
}

/// <summary>Electrical response history. Storage is owned by the producing
/// component and borrowed only until its next range execution.</summary>
public readonly ref struct MemoryOutputRange<TOutput>(long startNanoseconds, long knownUntilNanoseconds,
    TOutput initialOutput, ReadOnlySpan<TimedMemoryOutput<TOutput>> changes, long? nextEventNanoseconds = null,
    TOutput? finalOutput = null, bool hasCompleteHistory = true, int responseCount = 0) where TOutput : struct
{
    public long StartNanoseconds { get; } = startNanoseconds;
    public long KnownUntilNanoseconds { get; } = knownUntilNanoseconds;
    public TOutput InitialOutput { get; } = initialOutput;
    public ReadOnlySpan<TimedMemoryOutput<TOutput>> Changes { get; } = changes;
    public long? NextEventNanoseconds { get; } = nextEventNanoseconds;
    public TOutput FinalOutput { get; } = finalOutput ?? (changes.IsEmpty ? initialOutput : changes[^1].Output);
    /// <summary>False means only the boundary was observed, not that the
    /// physical output stayed constant. Every propagation was still executed
    /// locally; no observer may infer a stable waveform from an empty span.</summary>
    public bool HasCompleteHistory { get; } = hasCompleteHistory;
    public int ResponseCount { get; } = hasCompleteHistory ? changes.Length : responseCount;
}
