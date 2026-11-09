using RetroMediaLab.Chips.Memory.NationalSemiconductor.MM52116.Interfaces;
using C = RetroMediaLab.Chips.Memory.NationalSemiconductor.MM52116.Constants.Mm52116Constants;

namespace RetroMediaLab.Chips.Memory.NationalSemiconductor.MM52116.Services;

/// <summary>National MM52116/2316E, 2048×8 mask ROM with three programmed select levels.</summary>
/// <remarks>All three digital propagation durations must be supplied explicitly.
/// Data retains its preceding level until the configured address response arrives;
/// selection independently enables/disables the drivers. This is not a measurement
/// of an unspecified physical device, and no oscillator or reset is invented.</remarks>
public sealed class Mm52116 : IMm52116
{
    private readonly byte[] _contents;
    private long _timeNanoseconds;
    private ushort _address;
    private byte _chipSelectLevels;
    private byte? _decodedData;
    private long? _dataReadyAt;
    private long? _driveReadyAt;
    private bool _driveTarget;
    private bool _outputsDriven;
    private byte? _publishedData;
    private bool _publishedDriven;
    private long? _nextEvent;

    public Mm52116(ReadOnlySpan<byte> contents, byte activeSelectLevels,
        long addressAccessNanoseconds, long selectAccessNanoseconds, long outputDisableNanoseconds)
    {
        if (contents.Length != C.Words) throw new ArgumentException(nameof(contents));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(activeSelectLevels, C.SelectMask);
        ValidateDelay(addressAccessNanoseconds, C.MaximumAddressAccessNanoseconds);
        ValidateDelay(selectAccessNanoseconds, C.MaximumSelectAccessNanoseconds);
        ValidateDelay(outputDisableNanoseconds, C.MaximumOutputDisableNanoseconds);
        _contents = contents.ToArray();
        ActiveSelectLevels = activeSelectLevels;
        AddressAccessNanoseconds = addressAccessNanoseconds;
        SelectAccessNanoseconds = selectAccessNanoseconds;
        OutputDisableNanoseconds = outputDisableNanoseconds;
        _dataReadyAt = addressAccessNanoseconds;
        if (ChipSelected)
        {
            _driveTarget = true;
            _driveReadyAt = selectAccessNanoseconds;
        }
        AdvanceTo(0);
    }

    public long TimeNanoseconds => _timeNanoseconds;
    public long AddressAccessNanoseconds { get; }
    public long SelectAccessNanoseconds { get; }
    public long OutputDisableNanoseconds { get; }
    public byte ActiveSelectLevels { get; }
    public ushort Address => _address;
    public byte ChipSelectLevels => _chipSelectLevels;
    public bool ChipSelected => _chipSelectLevels == ActiveSelectLevels;
    public bool OutputsDriven => _outputsDriven;
    public byte? OutputData => _outputsDriven ? _decodedData : null;
    public long? NextEventNanoseconds => _nextEvent;
    public Action<byte?, bool>? OutputsChanged { get; set; }
    public Action<long?>? NextEventNanosecondsChanged { get; set; }

    public void SetAddress(ushort address)
    {
        address = (ushort)(address & C.AddressMask);
        if (_address == address) return;
        _address = address;
        _dataReadyAt = checked(_timeNanoseconds + AddressAccessNanoseconds);
        AdvanceTo(_timeNanoseconds);
    }

    public void SetChipSelects(byte levels)
    {
        levels &= C.SelectMask;
        if (_chipSelectLevels == levels) return;
        var selected = ChipSelected;
        _chipSelectLevels = levels;
        if (selected == ChipSelected) return;
        _driveTarget = ChipSelected;
        _driveReadyAt = _driveTarget == _outputsDriven ? null
            : checked(_timeNanoseconds + (_driveTarget ? SelectAccessNanoseconds : OutputDisableNanoseconds));
        AdvanceTo(_timeNanoseconds);
    }

    /// <summary>Resolve each local deadline at its time before reaching the requested time.</summary>
    public void AdvanceTo(long nanoseconds)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(nanoseconds, _timeNanoseconds);
        var pending = FirstDeadline();
        while (pending is { } next && next <= nanoseconds)
        {
            _timeNanoseconds = next;
            // Both mutations belong to one instant. Do not publish unknown data
            // between a decode and driver enable that finish simultaneously.
            if (_dataReadyAt == next)
            {
                _dataReadyAt = null;
                _decodedData = _contents[_address];
            }
            if (_driveReadyAt == next)
            {
                _driveReadyAt = null;
                _outputsDriven = _driveTarget;
            }
            PublishOutputs();
            PublishNextEvent(FirstDeadline());
            // A recipient can rearm or cancel an access during notification.
            // Continue from the resolved deadline, not the value sent to it.
            pending = _nextEvent;
        }
        _timeNanoseconds = nanoseconds;
        PublishNextEvent(pending);
    }

    private long? FirstDeadline() => _dataReadyAt is not { } data ? _driveReadyAt
        : _driveReadyAt is not { } drive ? data : Math.Min(data, drive);

    private void PublishOutputs()
    {
        var data = _outputsDriven ? _decodedData : null;
        if (_publishedData == data && _publishedDriven == _outputsDriven) return;
        _publishedData = data;
        _publishedDriven = _outputsDriven;
        OutputsChanged?.Invoke(data, _outputsDriven);
    }

    private void PublishNextEvent(long? next)
    {
        if (_nextEvent == next) return;
        _nextEvent = next;
        NextEventNanosecondsChanged?.Invoke(next);
    }

    private static void ValidateDelay(long value, long maximum)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, maximum);
    }
}
