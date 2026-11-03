using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Enums;
using C = RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Constants.Upd765Constants;

namespace RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Services;

public sealed partial class Upd765DriveControl
{
    private Upd765DrivePhase _signalPhase;
    private long _signalDelay;
    private long _timeUnits;
    private int _signalUnit;
    private int _signalHead;
    private bool _seekMode;
    private bool _stepHigh;
    private bool _direction;
    private bool _sensePending;
    private byte _senseUnitHead;
    private byte _senseStatus;

    public Action? OutputsChanged { get; set; }
    public bool SignalsBusy => _signalPhase != Upd765DrivePhase.Idle;
    public int SelectedUnit => _signalUnit;
    public int SelectedHead => _signalHead;
    public bool SeekMode => _seekMode;
    public bool Step => _stepHigh;
    public bool Direction => _direction;
    public long ControllerClocks => _timeUnits / _boardClock;
    public long NextSignalTicks => SignalsBusy ? ToBoardTicks(_signalDelay) : long.MaxValue;

    private long ToBoardTicks(long units) => units == long.MaxValue ? long.MaxValue
        : Math.Max(1, (units + _controllerClock - 1) / _controllerClock);
    private long PreparationUnits => (long)C.SeekPreparationClocks * _boardClock;
    private long NextEventUnits
    {
        get
        {
            var next = Polling ? _pollClockUnits : long.MaxValue;
            if (SignalsBusy)
            {
                next = Math.Min(next, _signalDelay);
                if (ReadyScanBusy)
                    for (var unit = 0; unit < C.DriveCount; unit++)
                        if (_seeking[unit]) next = Math.Min(next, Math.Max(0, _delay[unit] - PreparationUnits));
                return next;
            }
            if (_seekingMask != 0)
            for (var unit = 0; unit < C.DriveCount; unit++)
                if (_seeking[unit]) next = Math.Min(next, Math.Max(0, _delay[unit] - PreparationUnits));
            return next;
        }
    }

    private void ResetDriveSignals()
    {
        var changed = SignalsBusy || _signalUnit != 0 || _signalHead != 0 || _seekMode || _stepHigh || _direction;
        _signalPhase = Upd765DrivePhase.Idle;
        _pollPending = _pollAfterReset = false;
        _sensePending = false;
        _senseStatus = _senseUnitHead = 0;
        _signalDelay = 0;
        _signalUnit = _signalHead = 0;
        _seekMode = _stepHigh = _direction = false;
        if (changed) OutputsChanged?.Invoke();
    }

    private void ScheduleSignal(Upd765DrivePhase phase, int clocks)
    {
        _signalPhase = phase;
        _signalDelay = (long)clocks * _boardClock;
    }

    private void StartDueDrive()
    {
        if (_sensePending)
        {
            _sensePending = false;
            _signalUnit = _senseUnitHead & C.UnitMask;
            _signalHead = (_senseUnitHead & C.HeadMask) == 0 ? 0 : 1;
            _senseStatus = _senseUnitHead;
            _seekMode = false;
            ScheduleSignal(Upd765DrivePhase.SenseRead, SenseSampleClocks);
            OutputsChanged?.Invoke();
            return;
        }
        var selected = -1;
        var earliest = long.MaxValue;
        for (var unit = 0; unit < C.DriveCount; unit++)
            if (_seeking[unit] && _delay[unit] <= PreparationUnits && _delay[unit] < earliest)
            { selected = unit; earliest = _delay[unit]; }
        if (selected < 0)
        {
            if (_pollPending && Polling) BeginReadyScan();
            return;
        }
        _signalUnit = selected;
        _signalHead = (_heads[selected] & C.HeadMask) == 0 ? 0 : 1;
        ScheduleSignal(Upd765DrivePhase.Select, C.UnitSelectSetupClocks);
        OutputsChanged?.Invoke();
    }

    private void NextDriveSignal()
    {
        var generation = _resetGeneration;
        switch (_signalPhase)
        {
            case Upd765DrivePhase.PollHold:
                NextPollUnit();
                break;
            case Upd765DrivePhase.PollReady:
                SamplePollUnit();
                break;
            case Upd765DrivePhase.Select:
                _seekMode = true;
                ScheduleSignal(Upd765DrivePhase.Seek, C.SeekDirectionSetupClocks);
                OutputsChanged?.Invoke();
                break;
            case Upd765DrivePhase.Seek:
                _direction = !_recalibrating[_signalUnit] && _targets[_signalUnit] > _cylinders[_signalUnit];
                ScheduleSignal(Upd765DrivePhase.Direction, C.DirectionStepSetupClocks);
                OutputsChanged?.Invoke();
                break;
            case Upd765DrivePhase.Direction:
                _stepHigh = true;
                ScheduleSignal(Upd765DrivePhase.Step, C.StepPulseClocks);
                OutputsChanged?.Invoke();
                if (generation == _resetGeneration) StepDrive(_signalUnit);
                break;
            case Upd765DrivePhase.Step:
                _stepHigh = false;
                ScheduleSignal(Upd765DrivePhase.Hold, C.DirectionHoldClocks);
                OutputsChanged?.Invoke();
                break;
            case Upd765DrivePhase.Hold:
                _seekMode = false;
                _direction = false;
                ScheduleSignal(Upd765DrivePhase.Release, C.UnitSelectHoldClocks);
                OutputsChanged?.Invoke();
                break;
            case Upd765DrivePhase.SenseRead:
                _senseStatus |= (byte)(_statusPins(_signalUnit) & (C.FaultPin | C.WriteProtectedPin | C.ReadyPin));
                if (generation != _resetGeneration) return;
                _seekMode = true;
                ScheduleSignal(Upd765DrivePhase.SenseTrackZero, SenseSampleClocks);
                OutputsChanged?.Invoke();
                break;
            case Upd765DrivePhase.SenseTrackZero:
                _senseStatus |= (byte)(_statusPins(_signalUnit) & C.TrackZeroPin);
                if (generation != _resetGeneration) return;
                ScheduleSignal(Upd765DrivePhase.SenseTwoSided, SenseSampleClocks);
                break;
            case Upd765DrivePhase.SenseTwoSided:
                _senseStatus |= (byte)(_statusPins(_signalUnit) & C.TwoSidedPin);
                if (generation != _resetGeneration) return;
                _seekMode = false;
                _signalPhase = Upd765DrivePhase.Idle;
                _signalDelay = 0;
                OutputsChanged?.Invoke();
                if (generation == _resetGeneration) DriveStatusCompleted?.Invoke(_senseStatus);
                break;
            case Upd765DrivePhase.Release:
                _signalPhase = Upd765DrivePhase.Idle;
                _signalDelay = 0;
                OutputsChanged?.Invoke();
                if (generation == _resetGeneration) StartDueDrive();
                break;
        }
    }
}
