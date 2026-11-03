using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Enums;
using C = RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Constants.Upd765Constants;

namespace RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Services;

public sealed partial class Upd765DriveControl
{
    private bool _pollingEnabled;
    private bool _pollPending;
    private bool _pollAfterReset;
    private int _pollUnit;
    private int _resetReadyReports;
    private long PollScanUnits => (long)((C.DriveCount - 1) * C.ReadyPollUnitClocks + _unitResponseClocks) * _boardClock;
    private bool ReadyScanBusy => _signalPhase is Upd765DrivePhase.PollReady or Upd765DrivePhase.PollHold;
    private bool SeekPreparationDue
    {
        get
        {
            for (var unit = 0; unit < C.DriveCount; unit++)
                if (_seeking[unit] && _delay[unit] <= PreparationUnits) return true;
            return false;
        }
    }
    private int SenseSampleClocks => Math.Max(C.SenseSampleClocks, _unitResponseClocks);

    public bool PollingEnabled
    {
        get => _pollingEnabled;
        set
        {
            _pollingEnabled = value;
            if (value)
            {
                if (_pollPending && !SignalsBusy) StartDueDrive();
                return;
            }
            if (!ReadyScanBusy) return;
            _pollPending = true;
            _signalPhase = Upd765DrivePhase.Idle;
            _signalDelay = 0;
            OutputsChanged?.Invoke();
        }
    }

    private void BeginReadyScan()
    {
        _pollPending = false;
        _pollAfterReset = _resetPolling;
        _pollUnit = 0;
        SelectPollUnit();
    }

    private void SelectPollUnit()
    {
        _signalUnit = _pollUnit;
        _signalHead = 0;
        _seekMode = false;
        ScheduleSignal(Upd765DrivePhase.PollReady, _unitResponseClocks);
        OutputsChanged?.Invoke();
    }

    private void SamplePollUnit()
    {
        var generation = _resetGeneration;
        var unit = _pollUnit;
        var ready = Ready(unit);
        if (generation != _resetGeneration || _signalPhase != Upd765DrivePhase.PollReady) return;
        var changed = ready != _ready[unit];
        _ready[unit] = ready;
        var resetReport = _pollAfterReset && (_resetReadyReports & (1 << unit)) != 0;
        if (_pollAfterReset) _resetReadyReports &= ~(1 << unit);
        if (_pollAfterReset ? resetReport && ready : changed && _specified && PollingEnabled && !_seeking[unit])
            _interrupts.Enqueue(((byte)(unit | C.ReadyChanged | (ready ? 0 : C.NotReady)), _cylinders[unit]));
        UpdateInterrupt();
        if (generation != _resetGeneration || _signalPhase != Upd765DrivePhase.PollReady) return;
        ScheduleSignal(Upd765DrivePhase.PollHold, C.ReadyPollUnitClocks - _unitResponseClocks);
    }

    private void NextPollUnit()
    {
        var generation = _resetGeneration;
        if (++_pollUnit < C.DriveCount) { SelectPollUnit(); return; }
        if (_pollAfterReset) _resetPolling = false;
        _signalPhase = Upd765DrivePhase.Idle;
        _signalDelay = 0;
        OutputsChanged?.Invoke();
        if (generation == _resetGeneration) StartDueDrive();
    }
}
