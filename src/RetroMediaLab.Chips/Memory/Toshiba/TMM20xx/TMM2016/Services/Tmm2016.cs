using RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2016.Contracts;
using RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2016.Interfaces;
using RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2016.Models;
using C = RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2016.Constants.Tmm2016Constants;

namespace RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2016.Services;

/// <summary>One asynchronous SRAM mechanism for Toshiba2016 grades; no reset, clock or machine dependency.</summary>
public sealed partial class Tmm2016 : ITmm2016
{
    private readonly Tmm2016Timing _timing;
    private readonly long _maximumDelay;
    private readonly byte[] _cells, _known;
    private readonly ulong _selectMask, _selectValue, _readMask, _readValue, _controlsMask;
    private int _supplyMillivolts = C.DefaultSupplyMillivolts;
    private long _supplyRecoveryEnd;
    private ulong _pins = C.ControlsMask | C.Cs2Mask;
    private long _addressChanged, _dataChanged, _csStarted = -1, _oeStarted = -1, _writeStarted = -1;
    private ushort _writeAddress, _lastWriteAddress;
    private bool _writeAddressKnown, _lastWriteAddressKnown, _writing, _qualified = true, _postWriteUnknown;
    private long _lastWrite = -1, _lastWriteAddressStarted;
    private byte _readData, _readKnown, _targetData, _targetKnown;
    private bool _driven, _driveTarget, _publishing;
    private long _dataDue = -1, _invalidateDue = -1, _driveDue = -1, _internalNext = -1, _next = -1, _announced = -1;
    private long _writeEnableDue = -1, _writeDisableDue = -1;
    private bool _writeOutputSelected;
    private uint _published;

    public Tmm2016(Tmm2016Timing timing)
    {
        ArgumentNullException.ThrowIfNull(timing);
        _maximumDelay = timing.Validate(); _timing = timing;
        _cells = new byte[timing.Capacity]; _known = new byte[timing.Capacity];
        _selectMask = C.CsMask | (timing.HasSecondChipSelect ? C.Cs2Mask : 0);
        _selectValue = timing.HasSecondChipSelect ? C.Cs2Mask : 0;
        _readMask = _selectMask | C.OeMask | C.WeMask; _readValue = _selectValue | C.WeMask;
        _controlsMask = C.ControlsMask | (timing.HasSecondChipSelect ? C.Cs2Mask : 0);
    }
    public long TimeNanoseconds { get; private set; }
    public long? NextEventNanoseconds => _next < 0 ? null : _next;
    public bool IsTimingQualified => _qualified;
    public Tmm2016Output Output => _driven ? new((byte)(_readData & _readKnown),_readKnown,true) : default;
    public Action<Tmm2016Output>? OutputChanged { get; set; }
    public Action<long?>? NextEventNanosecondsChanged { get; set; }

    private uint OutputBits => _driven
        ? (uint)(_readData & _readKnown) | ((uint)_readKnown << C.OutputKnownShift) | C.OutputDrivenMask : 0;

    private static ulong PackPins(Tmm2016Pins pins) => (ulong)pins.Address
        | (pins.AddressKnown ? C.AddressKnownMask : 0) | (pins.CsHigh ? C.CsMask : 0)
        | (pins.OeHigh ? C.OeMask : 0) | (pins.WeHigh ? C.WeMask : 0)
        | (pins.Cs2High ? C.Cs2Mask : 0)
        | ((ulong)(pins.Data & pins.DataKnown) << C.DataShift) | ((ulong)pins.DataKnown << C.DataKnownShift);
    private static Tmm2016Pins UnpackPins(ulong pins) => new((ushort)(pins & C.AddressMask),
        (pins & C.AddressKnownMask) != 0,(pins & C.CsMask) != 0,(pins & C.OeMask) != 0,
        (pins & C.WeMask) != 0,(byte)(pins >> C.DataShift),(byte)(pins >> C.DataKnownShift),(pins & C.Cs2Mask) != 0);

    private bool IsSelected(ulong pins) => (pins & _selectMask) == _selectValue;
    private bool IsNormalSupply => _supplyMillivolts >= _timing.MinimumOperatingMillivolts
        && _supplyMillivolts <= _timing.MaximumOperatingMillivolts;
    private bool IsSupplyReady => _timing.MinimumRetentionMillivolts == 0
        || (IsNormalSupply && TimeNanoseconds >= _supplyRecoveryEnd);

    private void GuardRetention(ulong pins)
    {
        if (_timing.MinimumRetentionMillivolts == 0 || IsSupplyReady) return;
        if (_supplyMillivolts < _timing.MinimumRetentionMillivolts
            || _supplyMillivolts > _timing.MaximumOperatingMillivolts || IsSelected(pins))
        { Array.Clear(_known); _qualified = false; }
    }

    /// <summary>Stable supply levels; numeric retention guarantees, not an analog rail/current simulation.</summary>
    public void ReceiveSupplyMillivolts(long timeNanoseconds,int millivolts)
    {
        if (_timing.MinimumRetentionMillivolts == 0)
            throw new NotSupportedException("No supply-retention behavior is qualified for this profile.");
        ArgumentOutOfRangeException.ThrowIfNegative(millivolts);
        _ = checked(timeNanoseconds + _maximumDelay);
        AdvanceTo(timeNanoseconds);
        if (millivolts == _supplyMillivolts) return;
        bool wasNormal = IsNormalSupply;
        _supplyMillivolts = millivolts;
        if (wasNormal && IsNormalSupply) return;
        if (!wasNormal && IsNormalSupply) _supplyRecoveryEnd = checked(timeNanoseconds + _timing.Access);
        GuardRetention(_pins);
        // No known digital data is guaranteed outside operating/recovery conditions.
        _targetData = _targetKnown = 0; _dataDue = -1; _invalidateDue = timeNanoseconds;
        FindNext(); AdvanceTo(timeNanoseconds); PublishDeadline();
    }

    public void ReceivePins(long timeNanoseconds, Tmm2016Pins pins)
    {
        if (pins.Address >= _timing.Capacity) throw new ArgumentOutOfRangeException(nameof(pins));
        AdvanceTo(timeNanoseconds);
        ulong received = PackPins(pins);
        if (received == _pins) return;
        _ = checked(timeNanoseconds + _maximumDelay);
        GuardRetention(received);
        var old = _pins;
        bool selected = IsSelected(received), oldSelected = IsSelected(old);
        bool address = ((received ^ old) & (C.AddressMask | C.AddressKnownMask)) != 0;
        bool data = ((received ^ old) & C.DataBitsMask) != 0;
        bool csFall = !oldSelected && selected, oeFall = ((old & C.OeMask) != 0) && (received & C.OeMask) == 0;
        bool writing = selected && (received & C.WeMask) == 0;
        bool ended = _writing && !writing;

        if (ended) CommitWrite(timeNanoseconds, old);
        if (_lastWrite >= 0 && _lastWriteAddressKnown
            && ((address && (timeNanoseconds - _lastWrite < _timing.WriteRecovery
                || timeNanoseconds - _lastWriteAddressStarted < _timing.WriteCycle))
                || (data && timeNanoseconds - _lastWrite < _timing.DataHold)))
        { _known[_lastWriteAddress] = 0; _qualified = false; }
        if (address)
        {
            if (oldSelected && ((old & C.WeMask) != 0) && timeNanoseconds - _addressChanged < _timing.Access)
                _qualified = false;
            if (_writing && writing)
            {
                _qualified = false;
                if (_writeAddressKnown) _known[_writeAddress] = 0;
                if ((received & C.AddressKnownMask) != 0) _known[(ushort)(received & C.AddressMask)] = 0;
                _writeAddressKnown = false;
            }
            _addressChanged = timeNanoseconds;
        }
        if (data) _dataChanged = timeNanoseconds;
        if (csFall) _csStarted = timeNanoseconds;
        if (oeFall) _oeStarted = timeNanoseconds;
        if (writing && !_writing)
        {
            _writeStarted = timeNanoseconds; _writeAddress = (ushort)(received & C.AddressMask);
            _writeOutputSelected = oldSelected && (old & C.OeMask) == 0;
            _writeAddressKnown = ((received & C.AddressKnownMask) != 0);
            if (timeNanoseconds - _addressChanged < _timing.AddressSetup) _qualified = false;
        }
        _writing = writing;
        _pins = received;

        if (writing)
        {
            _postWriteUnknown = true; _dataDue = -1; _invalidateDue = -1;
        }
        else if (((received & C.WeMask) != 0) && (address || csFall))
        {
            _postWriteUnknown = false;
            if (csFall && !_driven) _readData = _readKnown = 0;
            if (address && _readKnown != 0)
                _invalidateDue = Earlier(_invalidateDue, checked(timeNanoseconds + _timing.AddressHold));
            PrepareRead(timeNanoseconds);
        }
        else if (ended)
        {
            // Toshiba does not specify a maximum WE-high-to-valid-data delay.
            _postWriteUnknown = true; _targetData = _targetKnown = 0;
            _dataDue = -1; _invalidateDue = timeNanoseconds;
        }
        else if (oeFall && selected && ((received & C.WeMask) != 0) && !_postWriteUnknown)
        {
            if (!_driven) { _readData = _readKnown = 0; }
            PrepareRead(timeNanoseconds);
        }
        bool controlsChanged = ((received ^ old) & _controlsMask) != 0;
        if (controlsChanged)
        {
            if (!selected || (received & (C.OeMask | C.WeMask)) != 0)
            {
                _writeEnableDue = -1;
                if (!_driven || (received & _readMask) == _readValue) _writeDisableDue = -1;
            }
            UpdateDrive(timeNanoseconds, old, received);
            if (csFall && writing && (received & C.OeMask) == 0 && _timing.SelectionDuringWriteCanDrive
                && _timing.CsEnable < _timing.WeDisable)
            {
                // Explicit profile difference: NEC's CS-controlled write shows a low-Z,
                // undefined interval; Toshiba's note4 guarantees continuous high-Z here.
                _writeEnableDue = checked(timeNanoseconds + _timing.CsEnable);
                _writeDisableDue = checked(timeNanoseconds + _timing.WeDisable);
            }
        }
        FindNext();
        if (_internalNext >= 0 && _internalNext <= timeNanoseconds) AdvanceTo(timeNanoseconds);
        PublishDeadline();
    }

    private void CommitWrite(long time, ulong previous)
    {
        bool valid = IsSupplyReady && _writeAddressKnown && ((previous & C.AddressKnownMask) != 0) && (ushort)(previous & C.AddressMask) == _writeAddress
            && _writeStarted - _addressChanged >= _timing.AddressSetup
            && time - _addressChanged >= _timing.AddressToWriteEnd
            && time - _writeStarted >= _timing.WritePulse
            && (!_timing.ActiveOutputWriteNeedsDisable || !_writeOutputSelected
                || time - _writeStarted >= _timing.WeDisable + _timing.DataSetup)
            && time - _csStarted >= _timing.CsWrite && time - _dataChanged >= _timing.DataSetup;
        _qualified &= valid;
        if (!_writeAddressKnown || (previous & C.AddressKnownMask) == 0) Array.Clear(_known);
        else
        {
            _cells[_writeAddress] = (byte)(previous >> C.DataShift);
            _known[_writeAddress] = valid ? (byte)(previous >> C.DataKnownShift) : (byte)0;
        }
        _lastWrite = time; _lastWriteAddress = _writeAddress;
        _lastWriteAddressStarted = _addressChanged; _lastWriteAddressKnown = _writeAddressKnown;
    }

    private void PrepareRead(long time)
    {
        _targetKnown = (IsSupplyReady && (_pins & C.AddressKnownMask) != 0) ? _known[(ushort)(_pins & C.AddressMask)] : (byte)0;
        _targetData = (byte)(((_pins & C.AddressKnownMask) != 0) ? _cells[(ushort)(_pins & C.AddressMask)] & _targetKnown : 0);
        long ready = checked(_addressChanged + _timing.Access);
        if (IsSelected(_pins)) ready = Math.Max(ready, checked(_csStarted + _timing.Access));
        if ((_pins & C.OeMask) == 0) ready = Math.Max(ready, checked(_oeStarted + _timing.OeAccess));
        _dataDue = Math.Max(time,ready);
        if (_invalidateDue >= 0 && _invalidateDue >= _dataDue) _invalidateDue = -1;
    }

    private void UpdateDrive(long time, ulong old, ulong pins)
    {
        bool target = (pins & _readMask) == _readValue;
        if (target == _driveTarget && (_driveDue >= 0 || target == _driven)) return;
        _driveTarget = target;
        if (target == _driven) { _driveDue = -1; return; }
        long delay = target ? 0 : long.MaxValue;
        if (target)
        {
            if (!IsSelected(old)) delay = Math.Max(delay,_timing.CsEnable);
            if (((old & C.OeMask) != 0)) delay = Math.Max(delay,_timing.OeEnable);
            if ((old & C.WeMask) == 0) delay = Math.Max(delay,_timing.WeEnable);
        }
        else
        {
            if (!IsSelected(pins) && IsSelected(old)) delay = Math.Min(delay,_timing.CsDisable);
            if (((pins & C.OeMask) != 0) && (old & C.OeMask) == 0) delay = Math.Min(delay,_timing.OeDisable);
            if ((pins & C.WeMask) == 0 && ((old & C.WeMask) != 0)) delay = Math.Min(delay,_timing.WeDisable);
        }
        _driveDue = checked(time + delay);
    }

    // No event uses a negative hardware timestamp. Keep nullable conversion at the
    // public/state boundaries rather than repeating it throughout the resolver.
    private static long Earlier(long first, long second) => first < 0 ? second : second < 0 ? first : Math.Min(first,second);
    private void FindNext()
    {
        long visible = _driveDue;
        if (_writeEnableDue >= 0 && (visible < 0 || _writeEnableDue < visible)) visible = _writeEnableDue;
        if (_writeDisableDue >= 0 && (visible < 0 || _writeDisableDue < visible)) visible = _writeDisableDue;
        long next = visible;
        if (_dataDue >= 0 && (next < 0 || _dataDue < next)) next = _dataDue;
        if (_invalidateDue >= 0 && (next < 0 || _invalidateDue < next)) next = _invalidateDue;
        _internalNext = next;
        _next = _driven || (_driveDue >= 0 && _driveTarget) ? next : visible;
    }

    public void AdvanceTo(long timeNanoseconds)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(timeNanoseconds,TimeNanoseconds);
        if (_publishing)
        {
            if (timeNanoseconds != TimeNanoseconds) throw new InvalidOperationException("Cannot advance SRAM from an output callback.");
            return;
        }
        while (_internalNext >= 0 && _internalNext <= timeNanoseconds)
        {
            long due = _internalNext;
            TimeNanoseconds = due;
            if (_invalidateDue == due) { _invalidateDue = -1; _readData = _readKnown = 0; }
            if (_dataDue == due) { _dataDue = -1; _readData = _targetData; _readKnown = _targetKnown; }
            if (_writeEnableDue == due)
            { _writeEnableDue = -1; _readData = _readKnown = 0; _driven = true; }
            if (_writeDisableDue == due)
            {
                _writeDisableDue = -1; _driven = false;
                if (!_driveTarget) _driveDue = -1;
            }
            if (_driveDue == due)
            {
                _driveDue = -1; _driven = _driveTarget;
                if (!_driven) _writeEnableDue = _writeDisableDue = -1;
            }
            FindNext();
            uint output = OutputBits;
            if (output != _published)
            {
                _published = output;
                _publishing = true;
                try { OutputChanged?.Invoke(Output); }
                finally { _publishing = false; }
            }
            PublishDeadline();
        }
        TimeNanoseconds = timeNanoseconds;
    }

    private void PublishDeadline()
    {
        if (_publishing) return;
        while (_announced != _next)
        {
            _announced = _next; _publishing = true;
            try { NextEventNanosecondsChanged?.Invoke(_next < 0 ? null : _next); }
            finally { _publishing = false; }
        }
    }
}
