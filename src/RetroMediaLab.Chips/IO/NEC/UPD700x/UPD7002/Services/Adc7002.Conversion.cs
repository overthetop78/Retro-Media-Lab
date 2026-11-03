using RetroMediaLab.Chips.IO.NEC.UPD700x.UPD7002.Enums;
using C = RetroMediaLab.Chips.IO.NEC.UPD700x.UPD7002.Constants.Adc7002Constants;

namespace RetroMediaLab.Chips.IO.NEC.UPD700x.UPD7002.Services;

public sealed partial class Adc7002
{
    private Adc7002Phase _phase;
    private long _charge;
    private long _integratedTicks;

    private void StartConversion()
    {
        _charge = _integratedTicks = 0;
        if ((_command & C.TwelveBitMode) != 0)
        {
            _phase = Adc7002Phase.Integration;
            SetRemaining(_longIntegrationTicks);
        }
        else
        {
            // Le séquencement du mode court reste à établir ; ne pas lui
            // attribuer arbitrairement les 8192 horloges du mode long.
            _phase = Adc7002Phase.RunDown;
            _sample = (ushort)(_channels[_command & C.ChannelMask] & C.EightBitResultMask);
            SetRemaining(_eightBitTicks);
        }
    }

    private void SetRemaining(long ticks)
    {
        // Compteur négatif : seule Clock peut avancer la conversion.
        // Tick conserve ainsi un seul compteur sur son chemin fréquent.
        _remaining = _externalClock ? -ticks : ticks;
    }

    private void AccumulateInput()
    {
        var elapsed = _longIntegrationTicks - Math.Abs(_remaining);
        var interval = elapsed - _integratedTicks;
        // La multiplication tient dans Int64 même à int.MaxValue Hz côté hôte
        // et 1 Hz côté conversion. Aucun calcul flottant ni allocation par cycle.
        _charge += interval * _channels[_command & C.ChannelMask];
        _integratedTicks = elapsed;
    }

    private void EndPhase()
    {
        if (_phase == Adc7002Phase.Integration)
        {
            AccumulateInput();
            _sample = (ushort)((_charge / _longIntegrationTicks) & C.ResultMask);
            _phase = Adc7002Phase.RunDown;
            // Durée nominale conservée tant que le chronogramme de la rampe
            // n'est pas établi ; il ne faut pas la présenter comme mesurée.
            var runDown = _twelveBitTicks - _longIntegrationTicks;
            SetRemaining(runDown);
            if (runDown != 0) return;
        }
        Complete();
    }
}
