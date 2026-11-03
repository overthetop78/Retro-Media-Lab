using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Enums;
using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Interfaces;
using C = RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Constants.Upd765Constants;

namespace RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Services;

/// <summary>NEC 765 drive-control section: physical STEP requests, PCN and interrupt causes.</summary>
/// <remarks>The board supplies ST3 input pins and applies STEP to its drives. In particular,
/// resetting the controller does not magically move a physical drive to cylinder zero.</remarks>
public sealed partial class Upd765DriveControl
    : IUpd765DriveControl
{
    private readonly int _boardClock;
    private readonly int _controllerClock;
    private readonly int _recalibrationLimit;
    private readonly int _unitResponseClocks;
    private readonly Func<int, byte> _statusPins;
    private readonly Action<int, int> _step;
    private readonly byte[] _cylinders = new byte[C.DriveCount];
    private readonly byte[] _targets = new byte[C.DriveCount];
    private readonly byte[] _heads = new byte[C.DriveCount];
    private readonly bool[] _seeking = new bool[C.DriveCount];
    private readonly bool[] _recalibrating = new bool[C.DriveCount];
    private readonly bool[] _ready = new bool[C.DriveCount];
    private readonly int[] _remaining = new int[C.DriveCount];
    private readonly long[] _delay = new long[C.DriveCount];
    private readonly Queue<(byte Status, byte Cylinder)> _interrupts = new();
    private long _stepUnits;
    private byte _busyMask;
    private byte _seekingMask;
    private bool _irq;
    private bool _specified;
    private bool _resetPolling;
    private long _pollClockUnits;
    private uint _resetGeneration;

    public Upd765DriveControl(Func<int, byte> statusPins, Action<int, int> step,
        int boardClockHz = C.ReferenceClockHz, int controllerClockHz = C.ReferenceClockHz,
        int recalibrationLimit = C.RecalibrationSteps, int unitResponseClocks = C.UnitResponseClocks)
    {
        ArgumentNullException.ThrowIfNull(statusPins);
        ArgumentNullException.ThrowIfNull(step);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(boardClockHz);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(controllerClockHz);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(recalibrationLimit);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(unitResponseClocks);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(unitResponseClocks, C.ReadyPollUnitClocks - 1);
        _unitResponseClocks = unitResponseClocks;
        _boardClock = boardClockHz;
        _controllerClock = controllerClockHz;
        _recalibrationLimit = recalibrationLimit;
        _statusPins = statusPins;
        _step = step;
        _stepUnits = (long)C.StepRateUnits * C.StepRateClocksPerUnit * _boardClock;
        Reset();
    }

    public Action<bool>? InterruptChanged { get; set; }
    public bool Irq => _irq;
    public byte BusyMask => _busyMask;
    public bool NonDma { get; private set; }
    public long HeadLoadTicks { get; private set; }
    public long HeadUnloadTicks { get; private set; }
    public byte PresentCylinder(int unit) => _cylinders[CheckedUnit(unit)];
    public long NextEventTicks => ToBoardTicks(NextEventUnits);

    public void Reset()
    {
        _resetGeneration++;
        ResetDriveSignals();
        Array.Clear(_seeking);
        Array.Clear(_recalibrating);
        Array.Clear(_delay);
        _interrupts.Clear();
        _busyMask = _seekingMask = 0;
        _specified = PollingEnabled = NonDma = false;
        _resetPolling = false;
        _resetReadyReports = 0;
        _pollClockUnits = PollPeriod - PollScanUnits;
        // NEC 5-5 : RESET ne modifie pas les paramètres SRT, HUT et HLT.
        for (var unit = 0; unit < C.DriveCount; unit++) _ready[unit] = Ready(unit);
        UpdateInterrupt();
    }

    /// <summary>NEC 5-5 : READY haut à la sortie de RESET produit INT dans une période de polling.</summary>
    public void ReleaseReset()
    {
        _resetPolling = _pollingEnabled = true;
        _resetReadyReports = C.AllDrivesMask;
        _pollClockUnits = PollPeriod - PollScanUnits;
    }

    public void Specify(byte rates, byte load)
    {
        _stepUnits = (long)(C.StepRateUnits - (rates >> C.StepRateShift)) * C.StepRateClocksPerUnit * _boardClock;
        var unload = rates & C.HeadUnloadMask;
        HeadUnloadTicks = ClockTicks((unload == 0 ? C.HeadUnloadUnits : unload) * C.HeadUnloadMillisecondsPerUnit);
        var headLoad = load >> C.HeadLoadShift;
        HeadLoadTicks = ClockTicks((headLoad == 0 ? C.HeadLoadUnits : headLoad) * C.HeadLoadMillisecondsPerUnit);
        NonDma = (load & C.NonDma) != 0;
        _specified = PollingEnabled = true;
    }

    public Action<byte>? DriveStatusCompleted { get; set; }

    public void SenseDriveStatus(byte unitHead)
    {
        _sensePending = true;
        _senseUnitHead = (byte)(unitHead & C.UnitHeadMask);
        if (ReadyScanBusy)
        {
            _pollPending = true;
            _signalPhase = Upd765DrivePhase.Idle;
            _signalDelay = 0;
        }
        if (!SignalsBusy) StartDueDrive();
    }

    public bool Seek(byte unitHead, byte cylinder) => Begin(unitHead, cylinder, recalibrate: false);

    public bool Recalibrate(byte unit) => Begin((byte)(unit & C.UnitMask), 0, recalibrate: true);

    private bool Begin(byte unitHead, byte cylinder, bool recalibrate)
    {
        var unit = unitHead & C.UnitMask;
        if ((_busyMask & (1 << unit)) != 0) return false;
        _busyMask |= (byte)(1 << unit);
        _heads[unit] = (byte)(unitHead & C.HeadMask);
        _targets[unit] = cylinder;
        _recalibrating[unit] = recalibrate;
        if (recalibrate) _cylinders[unit] = 0;
        _remaining[unit] = recalibrate ? _recalibrationLimit : Math.Abs(cylinder - _cylinders[unit]);
        _seeking[unit] = true;
        _seekingMask |= (byte)(1 << unit);
        _delay[unit] = _stepUnits;
        var generation = _resetGeneration;
        var pins = _statusPins(unit);
        if (_resetGeneration != generation) return false;
        _ready[unit] = (pins & C.ReadyPin) != 0;
        if (!_ready[unit]) Complete(unit, C.Abnormal | C.NotReady);
        else if ((pins & C.FaultPin) != 0) Complete(unit, C.Abnormal | C.EquipmentCheck);
        else if (recalibrate && (pins & C.TrackZeroPin) != 0)
        {
            _cylinders[unit] = 0;
            Complete(unit);
        }
        else if (!recalibrate && _remaining[unit] == 0) Complete(unit);
        return true;
    }

    public bool TrySenseInterrupt(out byte status, out byte cylinder)
    {
        if (!_interrupts.TryDequeue(out var cause))
        {
            status = C.InvalidCommand;
            cylinder = 0;
            return false;
        }
        status = cause.Status;
        cylinder = cause.Cylinder;
        if ((status & C.SeekEnd) != 0) _busyMask &= unchecked((byte)~(1 << (status & C.UnitMask)));
        UpdateInterrupt();
        return true;
    }

    public void Advance(int ticks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(ticks);
        if (_seekingMask == 0 && !SignalsBusy && !Polling)
        {
            _timeUnits += (long)ticks * _controllerClock;
            return;
        }
        var generation = _resetGeneration;
        if (_seekingMask != 0)
        for (var unit = 0; unit < C.DriveCount; unit++)
            if (_seeking[unit] && !Ready(unit))
            {
                _ready[unit] = false;
                if (SignalsBusy && SelectedUnit == unit) ResetDriveSignals();
                Complete(unit, C.Abnormal | C.NotReady);
                if (_resetGeneration != generation) return;
            }
        var remaining = (long)ticks * _controllerClock;
        while (remaining > 0)
        {
            var polling = Polling;
            var elapsed = Math.Min(remaining, NextEventUnits);
            if (elapsed == long.MaxValue) elapsed = remaining;
            _timeUnits += elapsed;
            remaining -= elapsed;
            if (polling) _pollClockUnits -= elapsed;
            if (_seekingMask != 0)
            for (var unit = 0; unit < C.DriveCount; unit++)
                if (_seeking[unit]) _delay[unit] = Math.Max(0, _delay[unit] - elapsed);
            if (ReadyScanBusy && SeekPreparationDue)
            {
                _pollPending = true;
                _signalPhase = Upd765DrivePhase.Idle;
                _signalDelay = 0;
                StartDueDrive();
            }
            else if (SignalsBusy)
            {
                _signalDelay -= elapsed;
                if (_signalDelay == 0) NextDriveSignal();
            }
            else StartDueDrive();
            if (_resetGeneration != generation) return;
            if (polling && _pollClockUnits <= 0)
            {
                _pollClockUnits += (-_pollClockUnits / PollPeriod + 1) * PollPeriod;
                _pollPending = true;
                if (!SignalsBusy) StartDueDrive();
                if (_resetGeneration != generation) return;
            }
        }
    }

    private void StepDrive(int unit)
    {
        var resetGeneration = _resetGeneration;
        var pins = _statusPins(unit);
        if (resetGeneration != _resetGeneration) return;
        if ((pins & C.ReadyPin) == 0) { Complete(unit, C.Abnormal | C.NotReady); return; }
        if ((pins & C.FaultPin) != 0) { Complete(unit, C.Abnormal | C.EquipmentCheck); return; }
        var direction = _recalibrating[unit] || _targets[unit] < _cylinders[unit]
            ? C.TowardCylinderZero : C.AwayFromCylinderZero;
        _step(unit, direction);
        if (resetGeneration != _resetGeneration) return;
        if (!_recalibrating[unit]) _cylinders[unit] = (byte)(_cylinders[unit] + direction);
        _remaining[unit]--;
        var zero = _recalibrating[unit] && TrackZero(unit);
        if (resetGeneration != _resetGeneration) return;
        if (zero)
        {
            _cylinders[unit] = 0;
            Complete(unit);
        }
        else if (_remaining[unit] == 0)
            Complete(unit, _recalibrating[unit] ? (byte)(C.Abnormal | C.EquipmentCheck) : (byte)0);
        else _delay[unit] = _stepUnits;
    }

    private void Complete(int unit, byte errors = 0)
    {
        _seeking[unit] = false;
        _seekingMask &= unchecked((byte)~(1 << unit));
        _delay[unit] = 0;
        _interrupts.Enqueue(((byte)(unit | _heads[unit] | C.SeekEnd | errors), _cylinders[unit]));
        UpdateInterrupt();
    }

    private bool Ready(int unit) => (_statusPins(unit) & C.ReadyPin) != 0;
    private bool TrackZero(int unit) => (_statusPins(unit) & C.TrackZeroPin) != 0;
    private bool Polling => PollingEnabled && (_resetPolling || _specified);
    private long PollPeriod => (long)_boardClock * C.ReadyPollClocks;
    private long PollTicks => Math.Max(1, (_pollClockUnits + _controllerClock - 1) / _controllerClock);
    private long ClockTicks(int milliseconds) => Math.Max(1,
        (long)_boardClock * milliseconds * C.ReferenceClockHz / C.MillisecondsPerSecond / _controllerClock);

    private void UpdateInterrupt()
    {
        var irq = _interrupts.Count != 0;
        if (_irq == irq) return;
        _irq = irq;
        InterruptChanged?.Invoke(irq);
    }

    private static int CheckedUnit(int unit)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(unit);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(unit, C.DriveCount);
        return unit;
    }
}
