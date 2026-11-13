using RetroMediaLab.Chips.Memory.Common.Contracts;
using RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2016.Contracts;

namespace RetroMediaLab.Chips.EmuChip.Memory.Toshiba.TMM20xx.TMM2016.Services;

public sealed partial class EmuTmm2016
{
    private TimedMemoryOutput<Tmm2016Output>[]? _rangeOutputs;
    private bool _executingRange;

    /// <summary>Pin history with a stable supply over the interval. Supply
    /// changes terminate the interval and use ReceiveSupplyMillivolts.</summary>
    public MemoryOutputRange<Tmm2016Output> ExecuteInputRange(in MemoryInputRange<Tmm2016Pins> range, bool retainResponseHistory = true)
    {
        range.Validate(_time);
        var changes = range.Changes;
        long end = range.KnownUntilNanoseconds;
        if (_publishing || _executingRange || OutputChanged is not null || NextEventNanosecondsChanged is not null)
            throw new InvalidOperationException("A guaranteed SRAM range requires exclusive range delivery.");
        ulong previous = _pins;
        Span<ulong> receivedPins = stackalloc ulong[changes.Length];
        int prepared = 0;
        foreach (ref readonly var input in changes)
        {
            if (input.Pins.Address >= _timing.Capacity) throw new ArgumentOutOfRangeException(nameof(range));
            ulong received = PackPins(input.Pins);
            if (received != previous) _ = checked(input.TimeNanoseconds + _maximumDelay);
            previous = received;
            receivedPins[prepared++] = received;
        }
        if (retainResponseHistory) _rangeOutputs ??= new TimedMemoryOutput<Tmm2016Output>[(MemoryInputRange<Tmm2016Pins>.MaximumChanges + 1) * 5];
        long start = _time;
        var initial = Output;
        int index = 0, count = 0;
        _executingRange = true;
        try
        {
            while (true)
            {
                bool hasInput = index < changes.Length;
                long inputTime = hasInput ? changes[index].TimeNanoseconds : end;
                // Include hidden data/hold responses even when no output is
                // currently driven. They remain part of this SRAM's own state.
                if (_internalNext >= 0 && _internalNext <= inputTime)
                {
                    if (ResolveOutputAt(_internalNext))
                    {
                        if (retainResponseHistory) _rangeOutputs![count] = new(_time, Output);
                        count++;
                    }
                    continue;
                }
                if (!hasInput) break;
                ref readonly var input = ref changes[index];
                _time = input.TimeNanoseconds;
                ApplyPackedPins(_time, receivedPins[index++]);
            }
            _time = end;
            return new(start, _time, initial,
                retainResponseHistory ? _rangeOutputs.AsSpan(0, count) : [], _next < 0 ? null : _next,
                Output, retainResponseHistory, count);
        }
        finally { _announced = _next; _executingRange = false; }
    }
}
