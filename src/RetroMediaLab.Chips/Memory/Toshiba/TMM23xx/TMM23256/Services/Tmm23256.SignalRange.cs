using RetroMediaLab.Chips.Memory.Common.Contracts;
using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Contracts;

namespace RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Services;

public sealed partial class Tmm23256
{
    private TimedMemoryOutput<Tmm23256Output>[]? _rangeOutputs;
    private bool _executingRange;

    public MemoryOutputRange<Tmm23256Output> ExecuteInputRange(in MemoryInputRange<Tmm23256Pins> range, bool retainResponseHistory = true)
    {
        range.Validate(TimeNanoseconds);
        if (_publishing || _executingRange || OutputChanged is not null || NextEventNanosecondsChanged is not null)
            throw new InvalidOperationException("A guaranteed ROM range requires exclusive range delivery.");
        foreach (ref readonly var input in range.Changes)
        {
            if (input.Pins.Address >= _busTiming.Capacity) throw new ArgumentOutOfRangeException(nameof(range));
            _ = checked(input.TimeNanoseconds + _busTiming.MaximumDelay);
        }
        if (retainResponseHistory) _rangeOutputs ??= new TimedMemoryOutput<Tmm23256Output>[(MemoryInputRange<Tmm23256Pins>.MaximumChanges + 1) * 2];
        long start = TimeNanoseconds;
        var initial = Output;
        int index = 0, count = 0;
        _executingRange = true;
        try
        {
            // Input capture and pending electrical propagation are consumed in
            // this package's own engine; no public ReceivePins/AdvanceTo chain.
            while (true)
            {
                bool hasInput = index < range.Changes.Length;
                long inputTime = hasInput ? range.Changes[index].TimeNanoseconds : range.KnownUntilNanoseconds;
                InitializeThrough(inputTime);
                if (_nextDue >= 0 && _nextDue <= inputTime)
                {
                    if (ResolveOutputAt(_nextDue))
                    {
                        if (retainResponseHistory) _rangeOutputs![count] = new(TimeNanoseconds, Output);
                        count++;
                    }
                    continue;
                }
                if (!hasInput) break;
                ref readonly var input = ref range.Changes[index++];
                TimeNanoseconds = input.TimeNanoseconds;
                var pins = input.Pins;
                ApplyParallelPins(TimeNanoseconds, pins.Address, pins.AddressKnown, pins.CeHigh, pins.OeHigh);
            }
            TimeNanoseconds = range.KnownUntilNanoseconds;
            return new(start, TimeNanoseconds, initial,
                retainResponseHistory ? _rangeOutputs.AsSpan(0, count) : [], _nextDue < 0 ? null : _nextDue,
                Output, retainResponseHistory, count);
        }
        finally { _announcedDue = _nextDue; _executingRange = false; }
    }
}
