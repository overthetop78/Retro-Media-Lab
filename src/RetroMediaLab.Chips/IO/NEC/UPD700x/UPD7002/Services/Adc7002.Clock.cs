namespace RetroMediaLab.Chips.IO.NEC.UPD700x.UPD7002.Services;

public sealed partial class Adc7002
{
    private long _inputClockCount;
    private long? _publishedClockEvent;

    public int InputClockFrequency { get; }
    public int ConversionClockFrequency { get; }
    public long InputClockCount => _inputClockCount;
    public long? NextClockEvent => _remaining > 0 ? checked(_inputClockCount + _remaining) : null;
    public Action<long?>? NextClockEventChanged { get; set; }

    /// <summary>Temps exprimé dans les périodes d'entrée configurées pour cette puce.
    /// Une période inactive est conservée sans recalcul des phases de conversion.</summary>
    public void AdvanceTo(long inputClockCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(inputClockCount, _inputClockCount);
        var remaining = inputClockCount - _inputClockCount;
        while (remaining > 0)
        {
            var count = (int)Math.Min(remaining, int.MaxValue);
            Advance(count);
            remaining -= count;
        }
    }

    private void PublishClockEvent()
    {
        var next = NextClockEvent;
        if (_publishedClockEvent == next) return;
        _publishedClockEvent = next;
        NextClockEventChanged?.Invoke(next);
    }
}
