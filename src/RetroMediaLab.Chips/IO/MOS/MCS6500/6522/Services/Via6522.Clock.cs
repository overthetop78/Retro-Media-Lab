using C = RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Constants.Via6522Constants;

namespace RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Services;

public sealed partial class Via6522
{
    private bool _phi2High = true;
    /// <summary>Avance une période, du front descendant au front montant suivant.
    /// Les cartes disposant d'une horloge à demi-périodes appellent les deux fronts séparément.</summary>
    public void Tick()
    {
        Phi2TrailingEdge();
        Phi2LeadingEdge();
    }

    /// <summary>Termine la phase haute : compteurs et décalage série.
    /// Une écriture de bus de cette phase doit être présentée avant cet appel.</summary>
    public void Phi2TrailingEdge()
    {
        _phi2High = false;
        if ((_ca2Pulse | _cb2Pulse) != 0) TickControlOutputs();
        if (_shiftActive || _externalShiftPending || _internalShiftPending) TickShift();
        if (_timer1Running || _timer2Running) TickTimers();
        else _timer1JustFired = _timer2JustFired = false;
    }

    /// <summary>Commence la phase haute : publication des timeouts et du changement PB7.</summary>
    public void Phi2LeadingEdge()
    {
        _phi2High = true;
        if (_timer2PulseMode && _timer2Running) SampleTimer2Pulse();
        else _timer2PulseSampled = false;
        // Les entrées sont échantillonnées avant les sorties modifiées par ce même front.
        if (_shiftLeadingPending) PublishShiftLeadingEdge();
        if (_timerInterruptsPending != 0) PublishTimerInterrupts();
    }

    private void SampleTimer2Pulse()
    {
        var level = Pb6Level;
        _timer2PulseSampled = _lastPb6 && !level;
        _lastPb6 = level;
    }

    private void PublishTimerInterrupts()
    {
        var interrupts = _timerInterruptsPending;
        _timerInterruptsPending = 0;
        InterruptFlags |= interrupts;
        if ((interrupts & C.Timer1Interrupt) != 0)
        {
            _timerPb7 = !_timerPb7;
            PortBPinsChanged?.Invoke(PortBPins);
        }
    }
}
