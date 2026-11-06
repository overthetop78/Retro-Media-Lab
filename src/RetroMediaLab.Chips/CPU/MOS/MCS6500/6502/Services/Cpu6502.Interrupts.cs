namespace RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services;

/// <summary>Broches d'interruption du NMOS 6502, indépendantes des périphériques raccordés.</summary>
public partial class Cpu6502
{
    private bool _nmiLevel;
    private bool _nmiPending;
    private bool _nmiPrevious;
    private bool _nmiAccepted;
    private bool _overflowLevel;
    private bool _irqPinConnected;
    private bool _nmiPinConnected;
    private bool _irqInput;
    private bool _nmiInputEdge;

    public void SetIrq(bool asserted)
    {
        _irqPinConnected = true;
        _irqInput = asserted;
    }

    public void SetNmi(bool asserted)
    {
        _nmiPinConnected = true;
        if (_nmiLevel == asserted) return;
        _nmiLevel = asserted;
        if (asserted) _nmiInputEdge = true;
    }

    private void ResetNmiInput()
    {
        if (!_nmiPinConnected) _nmiLevel = bus.Nmi;
        _nmiInputEdge = false;
    }

    private void SampleOverflow()
    {
        if (_overflowPinConnected)
        {
            if (_overflowInputEdge) _p |= Overflow;
            _overflowInputEdge = false;
            return;
        }
        var active = bus.SetOverflow;
        if (active && !_overflowLevel) _p |= Overflow;
        _overflowLevel = active;
    }

    /// <summary>La NMI est mémorisée sur front actif ; un niveau maintenu ne déclenche pas plusieurs entrées.</summary>
    private void SampleNmi()
    {
        var active = bus.Nmi;
        if (active && !_nmiLevel) _nmiPending = true;
        _nmiLevel = active;
    }

}
