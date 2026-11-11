namespace RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Services;

public sealed partial class Via6522
{
    private long _stablePhi2Clocks;
    private long _receivedPhi2Clocks;
    private bool _retryPhi2Prefix = true;
    private bool _receivingPhi2Input;

    /// <inheritdoc />
    public void SetPhi2Input(bool high)
    {
        _receivingPhi2Input = true;
        if (high == _phi2High) return;
        if (_stablePhi2Clocks == long.MaxValue)
        {
            // Aucun compteur temporel ne peut avancer : une qualification avec
            // T1 ou T2 intervalle serait bornée à au plus 65535 périodes.
            _phi2High = high;
            if (!high)
            {
                _timer1JustFired = _timer2JustFired = false;
                _timer2PulseSampled = false;
            }
            return;
        }
        if (!high)
        {
            if (_stablePhi2Clocks > 0)
            {
                _phi2High = false;
                _receivedPhi2Clocks++;
                _stablePhi2Clocks--;
            }
            else Phi2TrailingEdge();
            return;
        }
        if (_receivedPhi2Clocks != 0)
        {
            _phi2High = true;
            if (_stablePhi2Clocks != 0) return;
            // Le dernier montant appartient encore au préfixe sans effet.
            SynchronizePhi2Input();
        }
        else Phi2LeadingEdge();
        if (_retryPhi2Prefix)
            _stablePhi2Clocks = StableClockPrefix(long.MaxValue, out _retryPhi2Prefix);
    }

    /// <inheritdoc />
    public void SynchronizePhi2Input()
    {
        if (!_receivingPhi2Input) return;
        _stablePhi2Clocks = 0;
        _retryPhi2Prefix = true;
        if (_receivedPhi2Clocks == 0) return;
        // Seulement les descendants déjà reçus, précédemment qualifiés sans effet.
        // Le niveau PH2 courant reste celui de l'entrée ; aucun callback à rejouer.
        var count = _receivedPhi2Clocks;
        _receivedPhi2Clocks = 0;
        ApplyStableClockPrefix(count);
    }
}
