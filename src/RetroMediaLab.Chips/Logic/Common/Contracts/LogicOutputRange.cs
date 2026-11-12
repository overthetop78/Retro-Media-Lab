namespace RetroMediaLab.Chips.Logic.Common.Contracts;

/// <summary>All package outputs have settled for this date before the word is
/// emitted. Each two-bit field uses LogicPinState; ChangedPins preserves which
/// distinct outputs changed, including transitions to and from high impedance.</summary>
public readonly record struct TimedLogicOutput(long TimeNanoseconds, ushort Levels, byte ChangedPins);

/// <summary>Delivered electrical history, not a second package state. The
/// changes borrow the component's reusable transport storage and remain valid
/// until its next ExecuteInputRange call. Copy only when retaining a trace.</summary>
public readonly ref struct LogicOutputRange(long startNanoseconds, long knownUntilNanoseconds,
    ushort initialLevels, ReadOnlySpan<TimedLogicOutput> changes, long? nextEventNanoseconds = null)
{
    public long StartNanoseconds { get; } = startNanoseconds;
    public long KnownUntilNanoseconds { get; } = knownUntilNanoseconds;
    public ushort InitialLevels { get; } = initialLevels;
    public ReadOnlySpan<TimedLogicOutput> Changes { get; } = changes;
    public long? NextEventNanoseconds { get; } = nextEventNanoseconds;
}
