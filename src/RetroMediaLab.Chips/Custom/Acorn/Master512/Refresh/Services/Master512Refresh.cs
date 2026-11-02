namespace RetroMediaLab.Chips.Custom.Acorn.Master512.Refresh.Services;

/// <summary>IC7 (74LS113), schéma Acorn 0143,200/C : demande de rafraîchissement et retour HLDA.</summary>
public sealed class Master512Refresh
:RetroMediaLab.Chips.Custom.Acorn.Master512.Refresh.Interfaces.IMaster512Refresh{
    private bool _requestQ = true;
    private bool _refreshQ = true;
    private bool _timerOutput = true;
    private bool _holdAcknowledged;
    private bool _upperMemorySelected;

    public bool HoldRequested => !_requestQ;
    public bool RefreshActive => !_refreshQ;

    public void Reset()
    {
        _requestQ = _refreshQ = _timerOutput = true;
        _holdAcknowledged = _upperMemorySelected = false;
    }

    public void TimerOutput(bool level)
    {
        // IC7B : J=0, K=1 ; son Q barré est HOLD. Le preset actif bas est /HLDA.
        if (_timerOutput && !level && !_holdAcknowledged) _requestQ = false;
        _timerOutput = level;
    }

    public void HoldAcknowledged(bool asserted)
    {
        _holdAcknowledged = asserted;
        if (asserted) _requestQ = true;
    }

    public void UpperMemorySelected(bool asserted)
    {
        _upperMemorySelected = asserted;
        if (asserted) _refreshQ = true;
    }

    public void AdvanceClocks(long clocks)
    {
        if (clocks <= 0) return;
        // IC7A : J=/HLDA, K=HLDA, preset=/UCS ; Les entrées HLDA et UCS restent stables pendant ces cycles.
        _refreshQ = _upperMemorySelected || !_holdAcknowledged;
    }
}
