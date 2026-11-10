using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Constants.SystemFloppyBoardConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Services;

internal sealed partial class SystemFloppyBoard
{
    internal bool? DataWindow => _ic12.GetOutput(2);
    private bool _separatorClockDescribed;
    private bool _separatorCascadeDescribed;

    private void WireSeparator()
    {
        _ic14.SetObservedOutputs(C.Qa);
        _ic7.SetObservedOutputs(C.Qa | C.Qb | C.Qd);
        _ic14.OutputsChanged = (levels, _) =>
        {
            if (_separatorClockDescribed) return;
            _ic7.AdvanceTo(_time);
            _ic7.SetClock(0, (levels & C.Qa) != 0);
        };
        _ic14.QaPeriodicClockChanged = (fall, period, high) =>
        {
            if (_disposed) return;
            _ic7.AdvanceTo(_time);
            if (period == 0)
            {
                _ic7.DisconnectPeriodicClock(0);
                _separatorClockDescribed = false;
                _ic14.SetObservedOutputs(C.Qa);
            }
            else
            {
                _ic7.ConnectPeriodicClock(0, fall, period, high);
                _separatorClockDescribed = true;
                _ic14.SetObservedOutputs(0);
            }
        };
        _ic7.OutputsChanged = (levels, changed) =>
        {
            if (!_separatorCascadeDescribed && (changed & C.Qa) != 0) _ic7.SetClock(1, (levels & C.Qa) != 0);
            if ((changed & (C.Qb | C.Qd)) == 0) return;
            _ic12.AdvanceTo(_time);
            _ic12.SetInputs(1, (levels & C.Qd) != 0, (levels & C.Qb) != 0);
        };
        // This is the physical external QA->CKB wire, not an implicit cascade
        // inside the counter. During reset its explicit level changes remain.
        _ic7.QaPeriodicClockChanged = (fall, period, high) =>
        {
            if (_disposed) return;
            if (period == 0)
            {
                _ic7.DisconnectPeriodicClock(1);
                _separatorCascadeDescribed = false;
                _ic7.SetObservedOutputs(C.Qa | C.Qb | C.Qd);
            }
            else
            {
                _ic7.ConnectPeriodicClock(1, fall, period, high);
                _separatorCascadeDescribed = true;
                _ic7.SetObservedOutputs(C.Qb | C.Qd);
            }
        };
        _ic12.OutputChanged = (gate, high) =>
        {
            switch (gate)
            {
                case 0:
                    if (!high.HasValue) break;
                    _ic14.AdvanceTo(_time); _ic7.AdvanceTo(_time);
                    _ic14.SetResets(high.Value, high.Value); _ic7.SetResets(high.Value, high.Value);
                    break;
                case 1: _ic12.SetInputA(2, high); break; // 6->9.
                case 2:
                    _ic12.SetInputA(3, high); // 8->12, latch feedback.
                    // This pin only stores the window level. All consumption
                    // remains at the requested 8271 clock or raw-data edge.
                    if (high.HasValue && !_disposed) _fdc.SetDataWindow(high.Value);
                    break;
                case 3: _ic12.SetInputB(2, high); break; // 11->10.
            }
        };
    }

    private void InitializeSeparator()
    {
        _ic7.SetResets(false, false); _ic14.SetResets(false, false);
        _ic12.SetInputs(1, false, false); // Explicit QB/QD phase seed.
        SetReadDataLevel(true);
    }

    internal void SetReadDataLevel(bool nReadData)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _ic12.AdvanceTo(_time);
        // Raw data reaches pin27 directly; counter resets arrive later through IC12/3.
        SynchronizeController();
        _fdc.SetUnseparatedData(!nReadData);
        _ic12.SetInputs(0, nReadData, nReadData);
        _ic12.SetInputB(3, nReadData);
    }
}
