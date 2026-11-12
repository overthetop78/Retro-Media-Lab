using RetroMediaLab.Chips.Memory.Common.Contracts;
using RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2009.Contracts;

namespace RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2009.Services;

public sealed partial class Tmm2009
{
    private TimedMemoryOutput<Tmm2009Output>[]? _rangeOutputs;

    public MemoryOutputRange<Tmm2009Output> ExecuteInputRange(in MemoryInputRange<Tmm2009Pins> range, bool retainResponseHistory = true)
    {
        range.Validate(TimeNanoseconds);
        if (_publishing || OutputChanged is not null)
            throw new InvalidOperationException("A guaranteed SRAM range requires exclusive range delivery.");
        foreach (ref readonly var input in range.Changes)
            if (input.Pins.Address > 1023) throw new ArgumentOutOfRangeException(nameof(range));
        if (retainResponseHistory) _rangeOutputs ??= new TimedMemoryOutput<Tmm2009Output>[MemoryInputRange<Tmm2009Pins>.MaximumChanges];
        long start = TimeNanoseconds;
        var initial = _output;
        int count = 0;
        foreach (ref readonly var input in range.Changes)
        {
            TimeNanoseconds = input.TimeNanoseconds;
            if (ApplyPins(input.Pins))
            {
                if (retainResponseHistory) _rangeOutputs![count] = new(TimeNanoseconds, _output);
                count++;
            }
        }
        TimeNanoseconds = range.KnownUntilNanoseconds;
        return new(start, TimeNanoseconds, initial,
            retainResponseHistory ? _rangeOutputs.AsSpan(0, count) : [],
            finalOutput: _output, hasCompleteHistory: retainResponseHistory, responseCount: count);
    }
}
