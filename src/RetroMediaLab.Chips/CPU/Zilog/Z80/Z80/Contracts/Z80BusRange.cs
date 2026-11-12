namespace RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Contracts;

/// <summary>Resolved CPU output word at its electrical propagation date.</summary>
public readonly record struct TimedZ80BusPins(long TimeNanoseconds, Z80BusPins Pins);

/// <summary>Borrowed output history through a dependency fence. The receiver
/// consumes all changes and their downstream effects before the CPU samples
/// its memorized DATA, WAIT or INT inputs. No memory-provider shortcut.</summary>
public readonly ref struct Z80BusRange(long startNanoseconds, long endNanoseconds,
    Z80BusPins initialPins, ReadOnlySpan<TimedZ80BusPins> changes)
{
    public long StartNanoseconds { get; } = startNanoseconds;
    public long EndNanoseconds { get; } = endNanoseconds;
    public Z80BusPins InitialPins { get; } = initialPins;
    public ReadOnlySpan<TimedZ80BusPins> Changes { get; } = changes;
}

public delegate void Z80BusRangeHandler(in Z80BusRange range);
