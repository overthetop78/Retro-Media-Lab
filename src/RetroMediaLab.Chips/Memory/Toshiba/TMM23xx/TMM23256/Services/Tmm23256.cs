using System.Security.Cryptography;
using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Enums;
using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Contracts;
using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Interfaces;
using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Models;
using C = RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Constants.Tmm23256Constants;

namespace RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Services;

/// <summary>Shared parallel ROM peripheral: address capture or static tracking is selected by its hardware profile.</summary>
/// <remarks>All dates and pending output propagation are local. No machine, provider callback or host clock is consulted.</remarks>
public sealed class Tmm23256 : ITmm23256
{
    private readonly byte[] _rom;
    private readonly string _hash;
    private readonly Tmm23256Timing _timing;
    private Tmm23256Pins _pins = new(0, false, true, true);
    private ushort _latchedAddress;
    private bool _latchedKnown, _initialized, _initialCeHigh = true, _qualified = true, _publishing;
    private long _ceFall = -1, _ceRise = -1, _oeFall = -1, _accessReady;
    private long? _outputDue, _invalidateDue, _nextDue, _announcedDue;
    private long _addressChanged;
    private Tmm23256Output _target, _output;

    public Tmm23256(ReadOnlySpan<byte> contents, Tmm23256Timing timing)
    {
        ArgumentNullException.ThrowIfNull(timing);
        if (contents.Length != timing.Capacity)
            throw new ArgumentException("ROM contents must match the selected profile capacity.", nameof(contents));
        _rom = contents.ToArray();
        _hash = Convert.ToHexString(SHA256.HashData(_rom));
        _timing = timing;
        _initialized = timing.Startup == 0;
        _accessReady = timing.LatchesAddress ? 0 : timing.Access;
        if (IsOeActive(_pins.OeHigh)) _oeFall = 0;
    }
    public long TimeNanoseconds { get; private set; }
    public long? NextEventNanoseconds => _nextDue;
    public bool IsInitialized => _initialized;
    public bool IsTimingQualified => _qualified && _timing.ElectricallyCharacterized;
    public Tmm23256Output Output => _output;
    public Action<Tmm23256Output>? OutputChanged { get; set; }
    public Action<long?>? NextEventNanosecondsChanged { get; set; }

    public void ReceivePins(long timeNanoseconds, Tmm23256Pins pins)
    {
        if (pins.Address >= _timing.Capacity) throw new ArgumentOutOfRangeException(nameof(pins));
        ArgumentOutOfRangeException.ThrowIfLessThan(timeNanoseconds, TimeNanoseconds);
        _ = checked(timeNanoseconds + Math.Max(_timing.OutputHold,Math.Max(_timing.Access, Math.Max(_timing.OutputEnable, _timing.OutputDisable))));
        AdvanceTo(timeNanoseconds);
        if (_pins == pins) return;
        var previous = _pins;
        bool falling = previous.CeHigh && !pins.CeHigh;
        bool rising = !previous.CeHigh && pins.CeHigh;
        bool addressChanged = previous.Address != pins.Address || previous.AddressKnown != pins.AddressKnown;
        if (addressChanged) _addressChanged = timeNanoseconds;
        if (_timing.LatchesAddress && addressChanged && _ceFall >= 0 && timeNanoseconds - _ceFall < _timing.AddressHold)
        { _qualified = false; _latchedKnown = false; }
        if (falling)
        {
            if (_ceFall >= _timing.Startup) _initialized = true; // method2: a complete post-pause CE cycle
            bool valid = (_ceFall < 0 || timeNanoseconds - _ceFall >= _timing.Cycle)
                && (_ceRise < 0 || timeNanoseconds - _ceRise >= _timing.CeOff)
                && (_ceFall < 0 || timeNanoseconds - _accessReady >= _timing.Precharge);
            _qualified &= valid;
            _initialCeHigh = false;
            _ceFall = timeNanoseconds;
            if (_timing.LatchesAddress)
            {
                _latchedAddress = pins.Address;
                _latchedKnown = pins.AddressKnown && _initialized && valid;
                _accessReady = checked(timeNanoseconds + _timing.Access);
            }
        }
        if (rising)
        {
            if (timeNanoseconds - _ceFall < _timing.CePulse) _qualified = false;
            _ceRise = timeNanoseconds;
        }
        if (!_timing.LatchesAddress && (addressChanged || falling))
        {
            _latchedAddress = pins.Address;
            _latchedKnown = pins.AddressKnown && _initialized;
            _accessReady = checked(_addressChanged + _timing.Access);
            if (_ceFall >= 0) _accessReady = Math.Max(_accessReady,checked(_ceFall + _timing.Access));
            if (addressChanged && _output.Driven && _output.Known)
            {
                long invalidate = checked(timeNanoseconds + _timing.OutputHold);
                _invalidateDue = _invalidateDue is { } pending ? Math.Min(pending,invalidate) : invalidate;
            }
        }
        bool previousOeActive = IsOeActive(previous.OeHigh), oeActive = IsOeActive(pins.OeHigh);
        if (!previousOeActive && oeActive) _oeFall = timeNanoseconds;
        _pins = pins;
        if (!pins.CeHigh && oeActive)
        {
            var target = new Tmm23256Output(_latchedKnown ? _rom[_latchedAddress] : (byte)0, _latchedKnown, true);
            long ready = _timing.OeMode == Tmm23256OutputEnableMode.AlwaysEnabled ? _accessReady
                : Math.Max(_accessReady, checked(_oeFall + _timing.OutputEnable));
            Arm(target, Math.Max(ready, timeNanoseconds),!_timing.LatchesAddress && addressChanged);
        }
        else if (previousOeActive && (rising || !oeActive))
        {
            // The first disabling control starts tOD; another high control cannot postpone it.
            if (_target.Driven || _outputDue is null) Arm(default, checked(timeNanoseconds + _timing.OutputDisable));
        }
        UpdateNext();
        PublishDeadline();
        AdvanceTo(timeNanoseconds); // a deliberately selected zero-delay profile is resolved in this transaction
    }

    private bool IsOeActive(bool high) => _timing.OeMode == Tmm23256OutputEnableMode.AlwaysEnabled
        || high == (_timing.OeMode == Tmm23256OutputEnableMode.ActiveHigh);

    private void Arm(Tmm23256Output target, long due,bool rearm = false)
    {
        if (!rearm && _target == target && _outputDue is not null) return;
        _target = target;
        _outputDue = target != _output || (_invalidateDue.HasValue && target.Driven) ? due : null;
    }

    public void AdvanceTo(long timeNanoseconds)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(timeNanoseconds, TimeNanoseconds);
        if (_publishing)
        {
            if (timeNanoseconds != TimeNanoseconds) throw new InvalidOperationException("Cannot advance ROM from its output callback.");
            return;
        }
        if (!_initialized && _initialCeHigh && _pins.CeHigh && timeNanoseconds >= _timing.Startup) _initialized = true;
        while (_nextDue is { } due && due <= timeNanoseconds)
        {
            TimeNanoseconds = due;
            var previous = _output;
            if (_invalidateDue == due)
            {
                _invalidateDue = null;
                if (_output.Driven) _output = new(0,false,true);
            }
            if (_outputDue == due)
            {
                _outputDue = null; _output = _target;
                if (!_output.Driven) _invalidateDue = null;
            }
            if (_output == _target && _invalidateDue is null) _outputDue = null;
            UpdateNext();
            if (_output != previous)
            {
                _publishing = true;
                try { OutputChanged?.Invoke(_output); }
                finally { _publishing = false; }
            }
            PublishDeadline();
        }
        TimeNanoseconds = timeNanoseconds;
    }

    private void UpdateNext() => _nextDue = _outputDue is null ? _invalidateDue
        : _invalidateDue is null ? _outputDue : Math.Min(_outputDue.Value,_invalidateDue.Value);

    private void PublishDeadline()
    {
        if (_publishing) return;
        while (_announcedDue != _nextDue)
        {
            _announcedDue = _nextDue;
            _publishing = true;
            try { NextEventNanosecondsChanged?.Invoke(_nextDue); }
            finally { _publishing = false; }
        }
    }

    public Tmm23256State CaptureState()
    {
        if (_publishing) throw new InvalidOperationException("Cannot snapshot a partial ROM publication.");
        return new(C.StateVersion, _hash, _timing, TimeNanoseconds, _pins, _latchedAddress,
            _latchedKnown, _initialized, _initialCeHigh, _qualified, _ceFall, _ceRise, _oeFall,
            _accessReady, _outputDue, _target, _output,_addressChanged,_invalidateDue);
    }
    public void RestoreState(Tmm23256State state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (_publishing) throw new InvalidOperationException("Cannot restore during ROM publication.");
        if (state.Version != C.StateVersion || state.RomHash != _hash || state.Timing != _timing
            || state.Time < 0 || state.Pins.Address >= _timing.Capacity || state.LatchedAddress >= _timing.Capacity
            || state.CeFall < -1 || state.CeFall > state.Time || state.CeRise < -1 || state.CeRise > state.Time
            || state.OeFall < -1 || state.OeFall > state.Time || state.AccessReady < 0
            || state.AddressChanged < 0 || state.AddressChanged > state.Time || state.InvalidateDue <= state.Time
            || (_timing.LatchesAddress && state.InvalidateDue.HasValue)
            || state.OutputDue <= state.Time || state.OutputDue.HasValue != (state.Target != state.Output
                || (state.InvalidateDue.HasValue && state.Target.Driven))
            || (state.LatchedKnown && (!state.Initialized || (_timing.LatchesAddress && state.CeFall < 0)))
            || (!state.Output.Driven && (state.Output.Known || state.Output.Data != 0))
            || (!state.Target.Driven && (state.Target.Known || state.Target.Data != 0))
            || (!state.Output.Known && state.Output.Data != 0) || (!state.Target.Known && state.Target.Data != 0)
            || (state.Initialized && state.Time < _timing.Startup) || (state.InitialCeHigh && state.CeFall >= 0)
            || (!state.Initialized && state.InitialCeHigh && state.Time >= _timing.Startup)
            || (!state.Pins.CeHigh && state.CeFall < 0) || (IsOeActive(state.Pins.OeHigh) && state.OeFall < 0)
            || state.Target.Driven != (!state.Pins.CeHigh && IsOeActive(state.Pins.OeHigh))
            || (state.Target.Known && (!state.LatchedKnown || state.Target.Data != _rom[state.LatchedAddress]))
            || state.AccessReady != ExpectedAccessReady(state))
            throw new ArgumentException(nameof(state));
        TimeNanoseconds = state.Time; _pins = state.Pins; _latchedAddress = state.LatchedAddress;
        _latchedKnown = state.LatchedKnown; _initialized = state.Initialized; _initialCeHigh = state.InitialCeHigh;
        _qualified = state.TimingQualified; _ceFall = state.CeFall; _ceRise = state.CeRise; _oeFall = state.OeFall;
        _accessReady = state.AccessReady; _outputDue = state.OutputDue;
        _addressChanged = state.AddressChanged; _invalidateDue = state.InvalidateDue;
        UpdateNext(); _announcedDue = _nextDue;
        _target = state.Target; _output = state.Output;
    }
    private Int128 ExpectedAccessReady(Tmm23256State state)
    {
        if (_timing.LatchesAddress) return state.CeFall < 0 ? 0 : (Int128)state.CeFall + _timing.Access;
        Int128 ready = (Int128)state.AddressChanged + _timing.Access;
        return state.CeFall < 0 ? ready : Int128.Max(ready,(Int128)state.CeFall + _timing.Access);
    }

}
