using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Upd765.Constants.Upd765Constants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Upd765.Services;

/// <summary>NEC 765 drive-control section: physical STEP requests, PCN and interrupt causes.</summary>
/// <remarks>The board supplies ST3 input pins and applies STEP to its drives. In particular,
/// resetting the controller does not magically move a physical drive to cylinder zero.</remarks>
internal sealed class Upd765DriveControl
{
    private readonly int _boardClock;
    private readonly int _controllerClock;
    private readonly int _recalibrationLimit;
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
    private long _stepTicks;
    private byte _busyMask;
    private bool _irq;
    private bool _specified;

    internal Upd765DriveControl(Func<int, byte> statusPins, Action<int, int> step,
        int boardClockHz = C.ReferenceClockHz, int controllerClockHz = C.ReferenceClockHz,
        int recalibrationLimit = C.RecalibrationSteps)
    {
        ArgumentNullException.ThrowIfNull(statusPins);
        ArgumentNullException.ThrowIfNull(step);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(boardClockHz);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(controllerClockHz);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(recalibrationLimit);
        _boardClock = boardClockHz;
        _controllerClock = controllerClockHz;
        _recalibrationLimit = recalibrationLimit;
        _statusPins = statusPins;
        _step = step;
        Reset();
    }

    internal Action<bool>? InterruptChanged { get; set; }
    internal bool Irq => _irq;
    internal byte BusyMask => _busyMask;
    internal bool NonDma { get; private set; }
    internal long HeadLoadTicks { get; private set; }
    internal long HeadUnloadTicks { get; private set; }
    internal bool PollingEnabled { get; set; }
    internal byte PresentCylinder(int unit) => _cylinders[CheckedUnit(unit)];
    internal long NextEventTicks
    {
        get
        {
            var next = long.MaxValue;
            for (var unit = 0; unit < C.DriveCount; unit++)
                if (_seeking[unit]) next = Math.Min(next, _delay[unit]);
            return next;
        }
    }

    internal void Reset()
    {
        Array.Clear(_seeking);
        Array.Clear(_recalibrating);
        Array.Clear(_delay);
        _interrupts.Clear();
        _busyMask = 0;
        _specified = PollingEnabled = NonDma = false;
        _stepTicks = ClockTicks(C.StepRateUnits);
        HeadLoadTicks = HeadUnloadTicks = 0;
        for (var unit = 0; unit < C.DriveCount; unit++) _ready[unit] = Ready(unit);
        UpdateInterrupt();
    }

    internal void Specify(byte rates, byte load)
    {
        _stepTicks = ClockTicks(C.StepRateUnits - (rates >> C.StepRateShift));
        var unload = rates & C.HeadUnloadMask;
        HeadUnloadTicks = ClockTicks((unload == 0 ? C.HeadUnloadUnits : unload) * C.HeadUnloadMillisecondsPerUnit);
        var headLoad = load >> C.HeadLoadShift;
        HeadLoadTicks = ClockTicks((headLoad == 0 ? C.HeadLoadUnits : headLoad) * C.HeadLoadMillisecondsPerUnit);
        NonDma = (load & C.NonDma) != 0;
        _specified = PollingEnabled = true;
    }

    internal byte SenseDriveStatus(byte unitHead) =>
        (byte)((_statusPins(unitHead & C.UnitMask) & C.StatusPinsMask) | (unitHead & C.UnitHeadMask));

    internal bool Seek(byte unitHead, byte cylinder) => Begin(unitHead, cylinder, recalibrate: false);

    internal bool Recalibrate(byte unit) => Begin((byte)(unit & C.UnitMask), 0, recalibrate: true);

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
        _delay[unit] = _stepTicks;
        _ready[unit] = Ready(unit);
        if (!_ready[unit]) Complete(unit, C.Abnormal | C.NotReady);
        else if ((_statusPins(unit) & C.FaultPin) != 0) Complete(unit, C.Abnormal | C.EquipmentCheck);
        else if (recalibrate && TrackZero(unit))
        {
            _cylinders[unit] = 0;
            Complete(unit);
        }
        else if (!recalibrate && _remaining[unit] == 0) Complete(unit);
        return true;
    }

    internal bool TrySenseInterrupt(out byte status, out byte cylinder)
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

    internal void Advance(int ticks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(ticks);
        PollReady();
        for (var unit = 0; unit < C.DriveCount; unit++)
            if (_seeking[unit] && !Ready(unit)) Complete(unit, C.Abnormal | C.NotReady);
        var remainingTicks = (long)ticks;
        while (remainingTicks > 0)
        {
            var next = remainingTicks;
            var active = false;
            for (var unit = 0; unit < C.DriveCount; unit++)
                if (_seeking[unit]) { active = true; next = Math.Min(next, _delay[unit]); }
            if (!active) break;
            remainingTicks -= next;
            for (var unit = 0; unit < C.DriveCount; unit++)
                if (_seeking[unit]) _delay[unit] -= next;
            for (var unit = 0; unit < C.DriveCount; unit++)
                if (_seeking[unit] && _delay[unit] == 0) StepDrive(unit);
        }
    }

    private void StepDrive(int unit)
    {
        if (!Ready(unit)) { Complete(unit, C.Abnormal | C.NotReady); return; }
        if ((_statusPins(unit) & C.FaultPin) != 0)
        { Complete(unit, C.Abnormal | C.EquipmentCheck); return; }
        var direction = _recalibrating[unit] || _targets[unit] < _cylinders[unit]
            ? C.TowardCylinderZero : C.AwayFromCylinderZero;
        _step(unit, direction);
        if (!_recalibrating[unit]) _cylinders[unit] = (byte)(_cylinders[unit] + direction);
        _remaining[unit]--;
        if (_recalibrating[unit] && TrackZero(unit))
        {
            _cylinders[unit] = 0;
            Complete(unit);
        }
        else if (_remaining[unit] == 0)
            Complete(unit, _recalibrating[unit] ? (byte)(C.Abnormal | C.EquipmentCheck) : (byte)0);
        else _delay[unit] = _stepTicks;
    }

    private void Complete(int unit, byte errors = 0)
    {
        _seeking[unit] = false;
        _delay[unit] = 0;
        _interrupts.Enqueue(((byte)(unit | _heads[unit] | C.SeekEnd | errors), _cylinders[unit]));
        UpdateInterrupt();
    }

    private void PollReady()
    {
        for (var unit = 0; unit < C.DriveCount; unit++)
        {
            var ready = Ready(unit);
            if (ready == _ready[unit]) continue;
            _ready[unit] = ready;
            if (_specified && PollingEnabled && !_seeking[unit])
                _interrupts.Enqueue(((byte)(unit | C.ReadyChanged | (ready ? 0 : C.NotReady)), _cylinders[unit]));
        }
        UpdateInterrupt();
    }

    private bool Ready(int unit) => (_statusPins(unit) & C.ReadyPin) != 0;
    private bool TrackZero(int unit) => (_statusPins(unit) & C.TrackZeroPin) != 0;
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
