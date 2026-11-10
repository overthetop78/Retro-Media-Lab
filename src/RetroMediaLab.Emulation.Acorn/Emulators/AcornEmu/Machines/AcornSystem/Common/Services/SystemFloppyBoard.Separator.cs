using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Constants.SystemFloppyBoardConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Services;

internal sealed partial class SystemFloppyBoard
{
    internal bool? DataWindow => _ic12.GetOutput(2);
    private bool _separatorClockDescribed;
    private bool _separatorCascadeDescribed;
    private bool _separatorDeferred, _readDataHigh, _separatorRestoring;
    private bool _separatorQdHigh;
    private bool _groupMaskedQb = true;
    // Differential reference transport, never a configurable hardware option.
    internal bool GroupMaskedQb
    {
        get => _groupMaskedQb;
        set
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_groupMaskedQb == value) return;
            if (_advancing) throw new InvalidOperationException("Cannot replace separator observation during an advance.");
            if (!_separatorDeferred)
            {
                _ic7.AdvanceTo(_time); _ic12.AdvanceTo(_time);
                var levels = _ic7.Outputs;
                _separatorQdHigh = (levels & C.Qd) != 0;
                _ic12.SetInputs(1, _separatorQdHigh, (levels & C.Qb) != 0);
            }
            _groupMaskedQb = value;
            ObserveSeparatorOutputs();
        }
    }

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
            _separatorQdHigh = (levels & C.Qd) != 0;
            _ic12.AdvanceTo(_ic7.TimeNanoseconds);
            _ic12.SetInputs(1, _separatorQdHigh, (levels & C.Qb) != 0);
            ObserveSeparatorOutputs();
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
                ObserveSeparatorOutputs();
            }
            else
            {
                _ic7.ConnectPeriodicClock(1, fall, period, high);
                _separatorCascadeDescribed = true;
                ObserveSeparatorOutputs();
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
                case 1:
                    _ic12.SetInputA(2, high); // 6->9.
                    DeferLatchedSeparator();
                    break;
                case 2:
                    _ic12.SetInputA(3, high); // 8->12, latch feedback.
                    // This pin only stores the window level. All consumption
                    // remains at the requested 8271 clock or raw-data edge.
                    if (high.HasValue && !_disposed) _fdc.SetDataWindow(high.Value);
                    DeferLatchedSeparator();
                    break;
                case 3:
                    _ic12.SetInputB(2, high); // 11->10.
                    DeferLatchedSeparator();
                    break;
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
        if (!nReadData) ResumeSeparatorInputs();
        _ic12.AdvanceTo(_time);
        _readDataHigh = nReadData;
        // Raw data reaches pin27 directly; counter resets arrive later through IC12/3.
        SynchronizeController();
        _fdc.SetUnseparatedData(!nReadData);
        _ic12.SetInputs(0, nReadData, nReadData);
        _ic12.SetInputB(3, nReadData);
    }

    private void ObserveSeparatorOutputs()
    {
        // QD low forces NAND pin6 high regardless of QB. On QD's change,
        // its resolved counter word supplies both inputs before unmasking QB.
        var mask = _separatorCascadeDescribed ? 0 : C.Qa;
        if (!_separatorDeferred)
            mask |= C.Qd | (!_groupStableSeparator || !_groupMaskedQb || _separatorQdHigh ? C.Qb : 0);
        _ic7.SetObservedOutputs((byte)mask);
    }

    private void DeferLatchedSeparator()
    {
        if (!_groupStableSeparator || _separatorDeferred || _separatorRestoring || !_readDataHigh
            || _ic12.NextEventNanoseconds.HasValue
            || _ic12.GetOutput(2) != true || _ic12.GetOutput(3) != false) return;
        // Pin11 low forces pin8 high regardless of the periodic QB/QD inputs
        // of gate1. Their counter state remains in IC7, not in this wire owner.
        _separatorDeferred = true;
        ObserveSeparatorOutputs();
    }

    private void ResumeSeparatorInputs()
    {
        if (!_separatorDeferred) return;
        _separatorRestoring = true;
        try
        {
            _ic14.AdvanceTo(_time);
            // A complete real CKB period contains a long settled QB/QD interval.
            // Reconstruct this short tail so gate1's current level AND pending
            // response match the scalar wire before NRDATA unmasks the latch.
            // These are independent local times; board/controller time never moves back.
            var start = Math.Max(Math.Max(_ic7.TimeNanoseconds, _ic12.TimeNanoseconds), _time - C.SeparatorHistoryNanoseconds);
            _ic7.AdvanceTo(start); _ic12.AdvanceTo(start);
            var levels = _ic7.Outputs;
            _separatorQdHigh = (levels & C.Qd) != 0;
            _ic12.SetInputs(1, (levels & C.Qd) != 0, (levels & C.Qb) != 0);
            _separatorDeferred = false;
            ObserveSeparatorOutputs();
            _ic7.AdvanceTo(_time);
            _ic12.AdvanceTo(_time);
        }
        finally
        {
            _separatorRestoring = false;
            RememberDeadline(C.Separator, _ic7.NextOutputEventNanoseconds);
            RememberDeadline(C.Latch, _ic12.NextEventNanoseconds);
        }
    }
}
