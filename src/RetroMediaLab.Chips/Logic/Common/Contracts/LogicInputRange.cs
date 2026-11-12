namespace RetroMediaLab.Chips.Logic.Common.Contracts;

/// <summary>A received change on a package's input wires. Unknown levels are
/// carried explicitly, rather than replaced with a guessed binary value.</summary>
public readonly record struct TimedLogicInput(long TimeNanoseconds, ulong Levels, ulong Known);

/// <summary>A finite, externally supplied waveform. Each entry gives all package
/// input levels; between entries they remain stable through KnownUntilNanoseconds.
/// The borrowed span is consumed synchronously, never kept as a hardware FIFO.</summary>
public readonly ref struct LogicInputRange(ReadOnlySpan<TimedLogicInput> changes, long knownUntilNanoseconds)
{
    public const int MaximumChanges = 128;
    public ReadOnlySpan<TimedLogicInput> Changes { get; } = changes;
    public long KnownUntilNanoseconds { get; } = knownUntilNanoseconds;

    internal void Validate(long currentTime, ulong inputMask)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(Changes.Length, MaximumChanges);
        ArgumentOutOfRangeException.ThrowIfLessThan(KnownUntilNanoseconds, currentTime);
        long previous = currentTime;
        foreach (ref readonly var change in Changes)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(change.TimeNanoseconds, previous);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(change.TimeNanoseconds, KnownUntilNanoseconds);
            if (((change.Levels | change.Known) & ~inputMask) != 0)
                throw new ArgumentOutOfRangeException(nameof(Changes));
            previous = change.TimeNanoseconds;
        }
    }
}
