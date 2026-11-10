using RetroMediaLab.Chips.Logic.TexasInstruments.CD4000.CD4013B.Services;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Constants.SystemFloppyBoardConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Services;

internal sealed partial class SystemFloppyBoard
{
    private long _controllerClockOrigin, _controllerClockCount;
    private bool _synchronizingController;

    private void SynchronizeController(bool includeCurrentTime = false)
    {
        if (_synchronizingController) return;
        // A wire callback must not consume another package's response at this
        // same date ahead of the board's existing tie order.
        var limit = includeCurrentTime ? _time : _time - 1;
        if (_ic8.TimeNanoseconds < limit) _ic8.AdvanceTo(limit);
        var count = checked(_controllerClockOrigin + _ic8.GetQaRisingClockCount(0));
        if (count == _controllerClockCount) return;
        _synchronizingController = true;
        try { _fdc.AdvanceTo(count); _controllerClockCount = count; }
        finally { _synchronizingController = false; }
    }

    private void ControllerClockEventChanged(long? next)
        => _ic8.RequestQaClock(0, next.HasValue ? checked(next.Value - _controllerClockOrigin) : null);

    private void WireReady()
    {
        _controllerClockOrigin = _controllerClockCount = _fdc.InputClockCount;
        _ic8.SetObservedOutputs(0, C.Qd);
        _ic8.SetObservedOutputs(1, C.Qd);
        _ic8.FirstQaClockReached = _ => SynchronizeController(includeCurrentTime: true);
        _fdc.NextClockEventChanged += ControllerClockEventChanged;
        ControllerClockEventChanged(_fdc.NextClockEvent);
        _ic8.FirstOutputsChanged = (levels, changed) =>
        {
            if ((changed & C.Qd) != 0) _ic8.SetClock(1, (levels & C.Qd) != 0);
        };
        _ic8.SecondOutputsChanged = (levels, _) =>
        {
            _ic9.AdvanceTo(_time);
            _ic9.SetClock((levels & C.Qd) != 0);
        };
        _ic9.SetObservedOutputs(C.ReadyTimeoutOutput);
        _ic9.OutputsChanged = (levels, _) =>
        {
            var high = (levels & C.ReadyTimeoutOutput) != 0;
            _ic10.AdvanceTo(_time); _ic11.AdvanceTo(_time);
            _ic10.SetData(0, high); _ic11.SetData(0, high);
        };
        WireReadyFlipFlops(_ic10, 1);
        WireReadyFlipFlops(_ic11, 0);
    }

    private void WireReadyFlipFlops(Cd4013B chip, int drive)
    {
        chip.FirstOutputsChanged = (q, notQ) =>
        {
            // The board really wires Q1->RESET2 and /Q1->D2.
            if (q.HasValue) chip.SetReset(1, q.Value);
            chip.SetData(1, notQ);
        };
        chip.SecondOutputsChanged = (_, notQ) =>
        {
            if (!_disposed)
            {
                SynchronizeController();
                _fdc.SetReady(drive, notQ.HasValue ? !notQ.Value : null);
            }
        };
    }

    private void InitializeReady()
    {
        _ic8.SetClear(0, false); _ic8.SetClear(1, false);
        _ic9.SetReset(false);
        _ic10.SetData(0, false); _ic11.SetData(0, false); // Explicit counter phase seed0.
        _ic10.SetAsynchronousInputs(0, false, false); _ic11.SetAsynchronousInputs(0, false, false);
        _ic10.SetSet(1, false); _ic11.SetSet(1, false);
        _ic2.SetInputB(1, true); // IC2/5 is +5 V.
        SetIndexLevel(true);
    }

    internal void SetIndexLevel(bool nIndex)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        // IC1 has one physical INDEX input. The existing controller API associates
        // it with each selected drive; both retained views receive this same wire.
        SynchronizeController();
        _fdc.SetIndex(0, !nIndex); _fdc.SetIndex(1, !nIndex);
        _ic2.AdvanceTo(_time);
        _ic2.SetInputA(1, nIndex);
    }

    private void IndexClockChanged(bool high)
    {
        _ic9.AdvanceTo(_time); _ic10.AdvanceTo(_time); _ic11.AdvanceTo(_time);
        _ic9.SetReset(high);
        _ic10.SetClocks(high, high); _ic11.SetClocks(high, high);
    }

    internal bool? Ready0 => Invert(_ic11.GetNotQ(1));
    internal bool? Ready1 => Invert(_ic10.GetNotQ(1));
    private static bool? Invert(bool? high) => high.HasValue ? !high.Value : null;
}
