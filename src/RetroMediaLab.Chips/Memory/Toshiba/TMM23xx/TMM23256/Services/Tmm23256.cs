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
    private readonly Tmm23256LocalTiming _busTiming;
    private ushort _address;
    private bool _addressKnown, _ceHigh = true, _oeHigh = true;
    private ushort _latchedAddress;
    private bool _latchedKnown, _initialized, _initialCeHigh = true, _qualified = true, _publishing;
    private long _ceFall = -1, _ceRise = -1, _oeFall = -1, _accessReady;
    private long _outputDue = -1, _invalidateDue = -1, _nextDue = -1, _announcedDue = -1;
    private long _addressChanged;
    private const ushort Known = 0x100, Driven = 0x200;
    private ushort _target, _output;

    public Tmm23256(ReadOnlySpan<byte> contents, Tmm23256Timing timing)
    {
        ArgumentNullException.ThrowIfNull(timing);
        if (contents.Length != timing.Capacity)
            throw new ArgumentException("ROM contents must match the selected profile capacity.", nameof(contents));
        _rom = contents.ToArray();
        _hash = Convert.ToHexString(SHA256.HashData(_rom));
        _timing = timing;
        _busTiming = new(timing);
        _initialized = timing.Startup == 0;
        _accessReady = timing.LatchesAddress ? 0 : timing.Access;
        if (IsOeActive(_oeHigh)) _oeFall = 0;
    }
    public long TimeNanoseconds { get; private set; }
    public long? NextEventNanoseconds => _nextDue < 0 ? null : _nextDue;
    public bool IsInitialized => _initialized;
    public bool IsTimingQualified => _qualified && _timing.ElectricallyCharacterized;
    public Tmm23256Output Output => new((byte)_output, (_output & Known) != 0, (_output & Driven) != 0);
    public Action<Tmm23256Output>? OutputChanged { get; set; }
    public Action<long?>? NextEventNanosecondsChanged { get; set; }

    public void ReceivePins(long timeNanoseconds, Tmm23256Pins pins)
        => ReceiveParallelPins(timeNanoseconds, pins.Address, pins.AddressKnown, pins.CeHigh, pins.OeHigh);

    public void ReceiveParallelPins(long timeNanoseconds, ushort address, bool addressKnown, bool ceHigh, bool oeHigh)
    {
        if (address >= _busTiming.Capacity) throw new ArgumentOutOfRangeException(nameof(address));
        ArgumentOutOfRangeException.ThrowIfLessThan(timeNanoseconds, TimeNanoseconds);
        _ = checked(timeNanoseconds + _busTiming.MaximumDelay);
        AdvanceTo(timeNanoseconds);
        if (_address == address && _addressKnown == addressKnown && _ceHigh == ceHigh && _oeHigh == oeHigh) return;
        bool falling = _ceHigh && !ceHigh;
        bool rising = !_ceHigh && ceHigh;
        bool addressChanged = _address != address || _addressKnown != addressKnown;
        if (addressChanged) _addressChanged = timeNanoseconds;
        if (_busTiming.LatchesAddress && addressChanged && _ceFall >= 0 && timeNanoseconds - _ceFall < _busTiming.AddressHold)
        { _qualified = false; _latchedKnown = false; }
        if (falling)
        {
            if (_ceFall >= _busTiming.Startup) _initialized = true; // method2: a complete post-pause CE cycle
            bool valid = (_ceFall < 0 || timeNanoseconds - _ceFall >= _busTiming.Cycle)
                && (_ceRise < 0 || timeNanoseconds - _ceRise >= _busTiming.CeOff)
                && (_ceFall < 0 || timeNanoseconds - _accessReady >= _busTiming.Precharge);
            _qualified &= valid;
            _initialCeHigh = false;
            _ceFall = timeNanoseconds;
            if (_busTiming.LatchesAddress)
            {
                _latchedAddress = address;
                _latchedKnown = addressKnown && _initialized && valid;
                _accessReady = checked(timeNanoseconds + _busTiming.Access);
            }
        }
        if (rising)
        {
            if (timeNanoseconds - _ceFall < _busTiming.CePulse) _qualified = false;
            _ceRise = timeNanoseconds;
        }
        if (!_busTiming.LatchesAddress && (addressChanged || falling))
        {
            _latchedAddress = address;
            _latchedKnown = addressKnown && _initialized;
            _accessReady = checked(_addressChanged + _busTiming.Access);
            if (_ceFall >= 0) _accessReady = Math.Max(_accessReady,checked(_ceFall + _busTiming.Access));
            if (addressChanged && (_output & (Driven | Known)) == (Driven | Known))
            {
                long invalidate = checked(timeNanoseconds + _busTiming.OutputHold);
                _invalidateDue = _invalidateDue >= 0 ? Math.Min(_invalidateDue, invalidate) : invalidate;
            }
        }
        bool previousOeActive = IsOeActive(_oeHigh), oeActive = IsOeActive(oeHigh);
        if (!previousOeActive && oeActive) _oeFall = timeNanoseconds;
        _address = address; _addressKnown = addressKnown; _ceHigh = ceHigh; _oeHigh = oeHigh;
        if (!ceHigh && oeActive)
        {
            ushort target = _latchedKnown ? (ushort)(_rom[_latchedAddress] | Known | Driven) : Driven;
            long ready = _busTiming.AlwaysEnabled ? _accessReady
                : Math.Max(_accessReady, checked(_oeFall + _busTiming.OutputEnable));
            Arm(target, Math.Max(ready, timeNanoseconds), !_busTiming.LatchesAddress && addressChanged);
        }
        else if (previousOeActive && (rising || !oeActive))
        {
            // The first disabling control starts tOD; another high control cannot postpone it.
            if ((_target & Driven) != 0 || _outputDue < 0) Arm(0, checked(timeNanoseconds + _busTiming.OutputDisable));
        }
        UpdateNext();
        PublishDeadline();
        if (_nextDue >= 0 && _nextDue <= timeNanoseconds)
            AdvanceTo(timeNanoseconds); // Resolve a selected zero-delay profile in this same transaction.
    }

    private bool IsOeActive(bool high) => _busTiming.AlwaysEnabled || high == _busTiming.ActiveHigh;

    private void Arm(ushort target, long due, bool rearm = false)
    {
        if (!rearm && _target == target && _outputDue >= 0) return;
        _target = target;
        _outputDue = target != _output || (_invalidateDue >= 0 && (target & Driven) != 0) ? due : -1;
    }

    public void AdvanceTo(long timeNanoseconds)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(timeNanoseconds, TimeNanoseconds);
        if (_publishing)
        {
            if (timeNanoseconds != TimeNanoseconds) throw new InvalidOperationException("Cannot advance ROM from its output callback.");
            return;
        }
        if (!_initialized && _initialCeHigh && _ceHigh && timeNanoseconds >= _busTiming.Startup) _initialized = true;
        while (_nextDue >= 0 && _nextDue <= timeNanoseconds)
        {
            long due = _nextDue;
            TimeNanoseconds = due;
            var previous = _output;
            if (_invalidateDue == due)
            {
                _invalidateDue = -1;
                if ((_output & Driven) != 0) _output = Driven;
            }
            if (_outputDue == due)
            {
                _outputDue = -1; _output = _target;
                if ((_output & Driven) == 0) _invalidateDue = -1;
            }
            if (_output == _target && _invalidateDue < 0) _outputDue = -1;
            UpdateNext();
            if (_output != previous)
            {
                _publishing = true;
                try { OutputChanged?.Invoke(Output); }
                finally { _publishing = false; }
            }
            PublishDeadline();
        }
        TimeNanoseconds = timeNanoseconds;
    }

    private void UpdateNext() => _nextDue = _outputDue < 0 ? _invalidateDue
        : _invalidateDue < 0 ? _outputDue : Math.Min(_outputDue, _invalidateDue);

    private void PublishDeadline()
    {
        if (_publishing) return;
        while (_announcedDue != _nextDue)
        {
            _announcedDue = _nextDue;
            _publishing = true;
            try { NextEventNanosecondsChanged?.Invoke(_nextDue < 0 ? null : _nextDue); }
            finally { _publishing = false; }
        }
    }

    public Tmm23256State CaptureState()
    {
        if (_publishing) throw new InvalidOperationException("Cannot snapshot a partial ROM publication.");
        return new(C.StateVersion, _hash, _timing, TimeNanoseconds, new(_address, _addressKnown, _ceHigh, _oeHigh), _latchedAddress,
            _latchedKnown, _initialized, _initialCeHigh, _qualified, _ceFall, _ceRise, _oeFall,
            _accessReady, _outputDue < 0 ? null : _outputDue,
            new((byte)_target, (_target & Known) != 0, (_target & Driven) != 0), Output,
            _addressChanged, _invalidateDue < 0 ? null : _invalidateDue);
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
        TimeNanoseconds = state.Time;
        _address = state.Pins.Address; _addressKnown = state.Pins.AddressKnown;
        _ceHigh = state.Pins.CeHigh; _oeHigh = state.Pins.OeHigh; _latchedAddress = state.LatchedAddress;
        _latchedKnown = state.LatchedKnown; _initialized = state.Initialized; _initialCeHigh = state.InitialCeHigh;
        _qualified = state.TimingQualified; _ceFall = state.CeFall; _ceRise = state.CeRise; _oeFall = state.OeFall;
        _accessReady = state.AccessReady; _outputDue = state.OutputDue ?? -1;
        _addressChanged = state.AddressChanged; _invalidateDue = state.InvalidateDue ?? -1;
        UpdateNext(); _announcedDue = _nextDue;
        _target = (ushort)(state.Target.Data | (state.Target.Known ? Known : 0) | (state.Target.Driven ? Driven : 0));
        _output = (ushort)(state.Output.Data | (state.Output.Known ? Known : 0) | (state.Output.Driven ? Driven : 0));
    }
    private Int128 ExpectedAccessReady(Tmm23256State state)
    {
        if (_timing.LatchesAddress) return state.CeFall < 0 ? 0 : (Int128)state.CeFall + _timing.Access;
        Int128 ready = (Int128)state.AddressChanged + _timing.Access;
        return state.CeFall < 0 ? ready : Int128.Max(ready,(Int128)state.CeFall + _timing.Access);
    }

}
