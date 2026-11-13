using RetroMediaLab.Chips.Memory.Common.Contracts;
using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Contracts;

namespace RetroMediaLab.Chips.EmuChip.Memory.Toshiba.TMM23xx.TMM23256.Services;

public sealed partial class EmuTmm23256
{
    private TimedMemoryOutput<Tmm23256Output>[]? _rangeOutputs;
    private bool _executingRange;

    public MemoryOutputRange<Tmm23256Output> ExecuteInputRange(in MemoryInputRange<Tmm23256Pins> range, bool retainResponseHistory = true)
    {
        range.Validate(_time);
        var changes = range.Changes;
        long end = range.KnownUntilNanoseconds;
        if (_publishing || _executingRange || OutputChanged is not null || NextEventNanosecondsChanged is not null)
            throw new InvalidOperationException("A guaranteed ROM range requires exclusive range delivery.");
        foreach (ref readonly var input in changes)
        {
            if (input.Pins.Address >= _busTiming.Capacity) throw new ArgumentOutOfRangeException(nameof(range));
            _ = checked(input.TimeNanoseconds + _busTiming.MaximumDelay);
        }
        if (retainResponseHistory) _rangeOutputs ??= new TimedMemoryOutput<Tmm23256Output>[(MemoryInputRange<Tmm23256Pins>.MaximumChanges + 1) * 2];
        long start = _time;
        var initial = Output;
        int index = 0, count = 0;
        _executingRange = true;
        try
        {
            // Input capture and pending electrical propagation are consumed in
            // this package's own engine; no public ReceivePins/AdvanceTo chain.
            while (true)
            {
                bool hasInput = index < changes.Length;
                long inputTime = hasInput ? changes[index].TimeNanoseconds : end;
                InitializeThrough(inputTime);
                if (_nextDue >= 0 && _nextDue <= inputTime)
                {
                    if (ResolveOutputAt(_nextDue))
                    {
                        if (retainResponseHistory) _rangeOutputs![count] = new(_time, Output);
                        count++;
                    }
                    continue;
                }
                if (!hasInput) break;
                ref readonly var input = ref changes[index++];
                _time = input.TimeNanoseconds;
                var pins = input.Pins;
                ApplyParallelPins(_time, pins.Address, pins.AddressKnown, pins.CeHigh, pins.OeHigh);
            }
            _time = end;
            return new(start, _time, initial,
                retainResponseHistory ? _rangeOutputs.AsSpan(0, count) : [], _nextDue < 0 ? null : _nextDue,
                Output, retainResponseHistory, count);
        }
        finally { _announcedDue = _nextDue; _executingRange = false; }
    }
}
