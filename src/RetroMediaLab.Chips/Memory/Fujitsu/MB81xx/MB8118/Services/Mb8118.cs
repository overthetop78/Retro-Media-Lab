using RetroMediaLab.Chips.Logic.Common.Enums;
using RetroMediaLab.Chips.Memory.Fujitsu.MB81xx.MB8118.Contracts;
using RetroMediaLab.Chips.Memory.Fujitsu.MB81xx.MB8118.Interfaces;
using RetroMediaLab.Chips.Memory.Fujitsu.MB81xx.MB8118.Models;

namespace RetroMediaLab.Chips.Memory.Fujitsu.MB81xx.MB8118.Services;

/// <summary>16K×1 dynamic memory. RAS and CAS latch addresses; CAS controls the output latch.</summary>
public sealed class Mb8118 : IMb8118
{
    private const long Retention = 2_000_000, Startup = 200_000, MaximumPulse = 10_000;
    private readonly Mb8118Timing _timing;
    private readonly byte[] _cells = new byte[16384];
    private readonly long[] _refreshed = new long[128];
    private Mb8118Pins _pins = new(0, false, true, true, true, null);
    private int _row, _column, _startupCycles, _writeAddress;
    private bool _rowKnown, _columnKnown, _rowActive, _cycleWrite, _readCycle;
    private bool _qualified = true, _publishing;
    private long _rasFall = -1, _casFall = -1, _rasRise = -1, _casRise = -1;
    private long _addressChanged = -1, _dataChanged = -1, _writeFall = -1, _writeAt = -1;
    private long _writeHoldUntil = -1;
    private long? _outputDue, _announcedDue;
    private LogicPinState _output = LogicPinState.HighImpedance, _target = LogicPinState.HighImpedance;
    public Mb8118(Mb8118Timing timing)
    {
        ArgumentNullException.ThrowIfNull(timing);
        _timing = timing;
        Array.Fill(_refreshed, -1);
    }
    public long TimeNanoseconds { get; private set; }
    public long? NextEventNanoseconds => _outputDue;
    public bool IsTimingQualified => _qualified;
    public LogicPinState Output => _output;
    public Action<LogicPinState>? OutputChanged { get; set; }
    public Action<long?>? NextEventNanosecondsChanged { get; set; }

    public void ReceivePins(long timeNanoseconds, Mb8118Pins pins)
    {
        if (pins.Address > 127) throw new ArgumentOutOfRangeException(nameof(pins));
        AdvanceTo(timeNanoseconds);
        if (pins == _pins) return;
        _ = checked(timeNanoseconds + Math.Max(_timing.RasAccess, _timing.OutputOff));
        var old = _pins;
        long now = TimeNanoseconds;
        bool addressChanged = pins.Address != old.Address || pins.AddressKnown != old.AddressKnown;
        if (addressChanged)
        {
            if (_rasFall >= 0 && now - _rasFall < 15) { _qualified = false; _rowKnown = false; }
            if (_casFall >= 0 && now - _casFall < 15) { _qualified = false; _columnKnown = false; }
            if (_rowActive && _rasFall >= 0 && now - _rasFall < _timing.ColumnFromRas && !old.CasHigh)
                _qualified = false;
            _addressChanged = now;
        }
        if (pins.Data != old.Data)
        {
            if (_writeAt >= 0 && now < _writeHoldUntil)
            { _cells[_writeAddress] = 0; _qualified = false; }
            _dataChanged = now;
        }
        _pins = pins;
        if (old.RasHigh && !pins.RasHigh) BeginRow(now, pins);
        if (old.CasHigh && !pins.CasHigh) BeginColumn(now, pins);
        if (old.WriteHigh && !pins.WriteHigh)
        {
            if (_readCycle && _rasRise >= 0 && now - _rasRise < _timing.ReadHoldFromRas) _qualified = false;
            _writeFall = now;
            if (!pins.CasHigh && _rowActive)
            {
                if (_readCycle && (now - _casFall < _timing.CasToWrite || now - _rasFall < 120))
                    _qualified = false;
                if (_writeAt != now) Store(now, pins.Data);
            }
        }
        if (!old.WriteHigh && pins.WriteHigh && _cycleWrite
            && (now - _writeFall < _timing.WriteHold || now - _casFall < _timing.WriteHold
                || now - _rasFall < _timing.WriteHoldFromRas)) _qualified = false;
        if (!old.RasHigh && pins.RasHigh) EndRow(now);
        if (!old.CasHigh && pins.CasHigh)
        {
            if (_casFall >= 0 && (now - _casFall < _timing.CasPulse || now - _casFall > MaximumPulse
                || now - _rasFall < _timing.CasHold || (_cycleWrite && now - _writeAt < _timing.WriteToCas)))
                _qualified = false;
            _casRise = now;
            Arm(checked(now + _timing.OutputOff), LogicPinState.HighImpedance);
        }
        PublishDeadline();
    }

    private void BeginRow(long now, Mb8118Pins pins)
    {
        if (now < Startup || (_rasRise >= 0 && now - _rasRise < _timing.RasPrecharge)
            || (_rasFall >= 0 && now - _rasFall < (_readCycle && _cycleWrite ? _timing.ReadWriteCycle : _timing.Cycle)))
            _qualified = false;
        _rasFall = now;
        _row = pins.Address;
        _rowKnown = pins.AddressKnown;
        _rowActive = true;
        _cycleWrite = false;
        _columnKnown = false;
        // Hidden refresh deliberately leaves the previous DOUT latch and CAS phase intact.
        if (_rowKnown)
        {
            long previous = _refreshed[_row];
            if (previous < 0 || now - previous > Retention)
                for (int column = 0; column < 128; column++) _cells[_row | (column << 7)] = 0;
            _refreshed[_row] = now;
        }
    }

    private void BeginColumn(long now, Mb8118Pins pins)
    {
        bool page = _casFall >= _rasFall && _rasFall >= 0;
        if (_casRise >= 0 && now - _casRise < (page ? _timing.PagePrecharge : _timing.CasPrecharge)) _qualified = false;
        if (page && now - _casFall < _timing.PageCycle) _qualified = false;
        _casFall = now;
        if (!_rowActive) return;
        if (!page && now - _rasFall < _timing.RasCasMinimum) _qualified = false;
        _column = pins.Address;
        _columnKnown = pins.AddressKnown;
        _readCycle = pins.WriteHigh;
        if (!pins.WriteHigh)
        {
            Store(now, pins.Data);
            return;
        }
        int address = _row | (_column << 7);
        var value = _rowKnown && _columnKnown && _startupCycles >= 8 ? (LogicPinState)_cells[address] : LogicPinState.Unknown;
        Arm(Math.Max(checked(_rasFall + _timing.RasAccess), checked(now + _timing.CasAccess)), value);
    }

    private void Store(long now, bool? data)
    {
        _cycleWrite = true;
        if (!_rowKnown || !_columnKnown) return;
        _writeAddress = _row | (_column << 7);
        _writeAt = now;
        _writeHoldUntil = Math.Max(checked(now + _timing.WriteHold), checked(_rasFall + _timing.WriteHoldFromRas));
        _cells[_writeAddress] = _startupCycles < 8 || !data.HasValue ? (byte)0 : data.Value ? (byte)2 : (byte)1;
    }

    private void EndRow(long now)
    {
        if (now - _rasFall > MaximumPulse && _rowKnown)
            for (int column = 0; column < 128; column++) _cells[_row | (column << 7)] = 0;
        if (now - _rasFall < _timing.RasPulse || now - _rasFall > MaximumPulse
            || (_casFall >= _rasFall && now - _casFall < _timing.RasHold)
            || (_cycleWrite && now - _writeAt < _timing.WriteToRas)) _qualified = false;
        _rasRise = now;
        _rowActive = false;
        if (now >= Startup && _startupCycles < 8) _startupCycles++;
    }

    private void Arm(long due, LogicPinState target)
    {
        _target = target;
        _outputDue = target == _output ? null : due;
    }

    public void AdvanceTo(long timeNanoseconds)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(timeNanoseconds, TimeNanoseconds);
        if (_publishing)
        {
            if (timeNanoseconds != TimeNanoseconds) throw new InvalidOperationException("Cannot advance DRAM from an output callback.");
            return;
        }
        while (_outputDue is { } due && due <= timeNanoseconds)
        {
            TimeNanoseconds = due;
            _outputDue = null;
            _output = _target;
            _publishing = true;
            try { OutputChanged?.Invoke(_output); }
            finally { _publishing = false; PublishDeadline(); }
        }
        TimeNanoseconds = timeNanoseconds;
    }

    private void PublishDeadline()
    {
        if (_publishing) return;
        while (_announcedDue != _outputDue)
        {
            _announcedDue = _outputDue;
            _publishing = true;
            try { NextEventNanosecondsChanged?.Invoke(_outputDue); }
            finally { _publishing = false; }
        }
    }

    public Mb8118State CaptureState()
    {
        if (_publishing) throw new InvalidOperationException("Cannot snapshot during DRAM output publication.");
        return new(1, _timing.Signature, (byte[])_cells.Clone(), (long[])_refreshed.Clone(),
            TimeNanoseconds, _pins, _row, _column, _rowKnown, _columnKnown, _rowActive,
            _cycleWrite, _readCycle, _qualified, _startupCycles, _rasFall, _casFall,
            _rasRise, _casRise, _addressChanged, _dataChanged, _writeFall, _writeAt,
            _writeHoldUntil, _writeAddress, _outputDue, _target, _output);
    }

    public void RestoreState(Mb8118State state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (_publishing) throw new InvalidOperationException("Cannot restore during DRAM output publication.");
        if (state.Version != 1 || state.Timing is null || !state.Timing.SequenceEqual(_timing.Signature)
            || state.Cells?.Length != 16384 || state.Refreshed?.Length != 128 || state.Time < 0
            || state.Row is < 0 or > 127 || state.Column is < 0 or > 127 || state.Pins.Address > 127
            || state.WriteAddress is < 0 or >= 16384 || state.StartupCycles is < 0 or > 8
            || state.Cells.Any(value => value > 2) || state.Refreshed.Any(time => time < -1 || time > state.Time)
            || new[] { state.RasFall, state.CasFall, state.RasRise, state.CasRise, state.AddressChanged,
                state.DataChanged, state.WriteFall, state.WriteAt }.Any(time => time < -1 || time > state.Time)
            || (uint)state.Output > 3 || (uint)state.OutputTarget > 3 || state.OutputDue <= state.Time
            || state.WriteHoldUntil < -1
            || state.OutputDue.HasValue != (state.Output != state.OutputTarget)
            || state.RowActive == state.Pins.RasHigh)
            throw new ArgumentException(nameof(state));
        state.Cells.CopyTo(_cells, 0); state.Refreshed.CopyTo(_refreshed, 0);
        TimeNanoseconds = state.Time; _pins = state.Pins; _row = state.Row; _column = state.Column;
        _rowKnown = state.RowKnown; _columnKnown = state.ColumnKnown; _rowActive = state.RowActive;
        _cycleWrite = state.CycleWrite; _readCycle = state.ReadCycle; _qualified = state.TimingQualified;
        _startupCycles = state.StartupCycles; _rasFall = state.RasFall; _casFall = state.CasFall;
        _rasRise = state.RasRise; _casRise = state.CasRise; _addressChanged = state.AddressChanged;
        _dataChanged = state.DataChanged; _writeFall = state.WriteFall; _writeAt = state.WriteAt;
        _writeHoldUntil = state.WriteHoldUntil;
        _writeAddress = state.WriteAddress; _outputDue = _announcedDue = state.OutputDue;
        _target = state.OutputTarget; _output = state.Output;
    }
}
