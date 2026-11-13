namespace RetroMediaLab.Chips.EmuChip.Audio.TexasInstruments.SN764xx.SN76489.Services;

public sealed partial class EmuSn76489
{
    private readonly int _readyAssertDelay;
    private bool _readyOutput, _directTransfer, _clockHigh, _publishing;
    private bool _publishedReadyOutput;
    private long _inputClockCount;
    private long? _readyDue, _announcedReadyDue;

    public Action<bool>? ReadyChanged { get; set; }
    public Action<long?>? NextEventNanosecondsChanged { get; set; }
    public long InputClockCount => _inputClockCount;
    public long TimeNanoseconds { get; private set; }
    public long? NextEventNanoseconds => NextReadyEvent;
    private bool ReadyTarget => _transferClocks == 0 || (!_chipSelected && !_directTransfer);

    public void SetBus(long timeNanoseconds, byte value, bool chipSelected, bool writeEnabled)
    {
        AdvanceTo(timeNanoseconds);
        SetBus(value, chipSelected, writeEnabled);
    }

    public void ReceiveClock(long timeNanoseconds, bool high)
    {
        if (_hasClockSource)
        {
            if (_advancingClockSource)
            {
                if (timeNanoseconds == TimeNanoseconds && high == _clockHigh) return;
                throw new InvalidOperationException("Recursive PSG clock transition.");
            }
            AdvancePeriodicClockTo(timeNanoseconds, false);
            DetachClockSource();
        }
        AdvanceTo(timeNanoseconds);
        if (_clockHigh == high) return;
        if (_publishing) throw new InvalidOperationException("Recursive PSG clock transition.");
        if (high) _ = checked(_inputClockCount + 1);
        _clockHigh = high;
        if (high) Tick();
    }

    public void AdvanceTo(long timeNanoseconds)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(timeNanoseconds, TimeNanoseconds);
        if (_publishing && timeNanoseconds != TimeNanoseconds)
            throw new InvalidOperationException("Cannot advance PSG time from an output callback.");
        if (_hasClockSource && !_advancingClockSource)
        {
            AdvancePeriodicClockTo(timeNanoseconds, true);
            return;
        }
        while (_readyDue is { } due && due <= timeNanoseconds)
        {
            TimeNanoseconds = due;
            _readyDue = null;
            _readyOutput = ReadyTarget;
            PublishReady();
            PublishReadyDeadline();
        }
        TimeNanoseconds = timeNanoseconds;
    }

    private void ValidateReadyAssertion()
    {
        if (_readyOutput && _readyDue is null && _readyAssertDelay != 0)
            _ = checked(TimeNanoseconds + _readyAssertDelay);
    }

    private void ResolveReady()
    {
        if (ReadyTarget)
        {
            _readyDue = null;
            _readyOutput = true;
        }
        else if (_readyOutput && _readyDue is null)
        {
            if (_readyAssertDelay == 0) _readyOutput = false;
            else _readyDue = checked(TimeNanoseconds + _readyAssertDelay);
        }
    }

    private void PublishReady()
    {
        if (_publishedReadyOutput == _readyOutput) return;
        _publishedReadyOutput = _readyOutput;
        if (ReadyChanged is not { } listener) return;
        bool wasPublishing = _publishing;
        _publishing = true;
        try { listener(_readyOutput); }
        finally { _publishing = wasPublishing; }
    }

    private void PublishReadyDeadline()
    {
        long? next = NextReadyEvent;
        if (_announcedReadyDue == next) return;
        _announcedReadyDue = next;
        bool wasPublishing = _publishing;
        _publishing = true;
        try { NextEventNanosecondsChanged?.Invoke(next); }
        finally { _publishing = wasPublishing; }
    }
}
