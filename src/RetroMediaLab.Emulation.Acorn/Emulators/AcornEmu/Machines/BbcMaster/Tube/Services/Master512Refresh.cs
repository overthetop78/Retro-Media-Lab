namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMaster.Tube.Services;

/// <summary>IC7 (74LS113), schéma Acorn 0143,200/C : demande de rafraîchissement et retour HLDA.</summary>
internal sealed class Master512Refresh
{
    private bool _requestQ = true;
    private bool _refreshQ = true;
    private bool _timerOutput = true;
    private bool _holdAcknowledged;
    private bool _upperMemorySelected;

    internal bool HoldRequested => !_requestQ;
    internal bool RefreshActive => !_refreshQ;

    internal void Reset()
    {
        _requestQ = _refreshQ = _timerOutput = true;
        _holdAcknowledged = _upperMemorySelected = false;
    }

    internal void TimerOutput(bool level)
    {
        // IC7B : J=0, K=1 ; son Q barré est HOLD. Le preset actif bas est /HLDA.
        if (_timerOutput && !level && !_holdAcknowledged) _requestQ = false;
        _timerOutput = level;
    }

    internal void HoldAcknowledged(bool asserted)
    {
        _holdAcknowledged = asserted;
        if (asserted) _requestQ = true;
    }

    internal void UpperMemorySelected(bool asserted)
    {
        _upperMemorySelected = asserted;
        if (asserted) _refreshQ = true;
    }

    internal void AdvanceClocks(long clocks)
    {
        if (clocks <= 0) return;
        // IC7A : J=/HLDA, K=HLDA, preset=/UCS ; Les entrées HLDA et UCS restent stables pendant ces cycles.
        _refreshQ = _upperMemorySelected || !_holdAcknowledged;
    }
}
