using System.Numerics;
using C = RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6551.Constants.Mos6551Constants;

namespace RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6551.Services;

public sealed partial class Mos6551
{
    private uint _transmitFrame;
    private int _transmitTicks;
    private int _transmitRemaining;
    private int _transmitStopTicks;
    private int _receiveStep = C.IdleReceive;
    private int _receiveTicks;
    private int _receiveValue;
    private bool _receiveParity;
    private bool _receiveParityError;

    /// <summary>Ticks de l'horloge de la carte, conservés en fraction du quartz réel.</summary>
    public void Advance(int ticks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(ticks);
        if (!Enabled) return;
        var period = (long)_ticksPerSecond * C.BaudDivisors[_control & C.BaudMask];
        _clockPhase += (long)ticks * _crystalClock;
        while (_clockPhase >= period)
        {
            _clockPhase -= period;
            TransmitClock();
            if ((_control & C.InternalReceiveClock) != 0) ReceiveClock();
        }
    }

    /// <summary>Chaque transition du compteur IOC change RxC ; seules les montées sont échantillonnées.</summary>
    public void AdvanceReceiveClock(int transitions)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(transitions);
        if ((_control & C.InternalReceiveClock) == 0 && Enabled)
        {
            var rising = (transitions + (_externalClock ? 0 : 1)) / 2;
            for (var edge = 0; edge < rising; edge++) ReceiveClock();
        }
        if ((transitions & 1) != 0) _externalClock = !_externalClock;
    }

    private void SetTransmit(bool high)
    {
        if (_txd == high) return;
        _txd = high;
        TransmitChanged?.Invoke(high);
    }

    private bool ParityBit(int data)
    {
        var odd = (BitOperations.PopCount((uint)data) & 1) != 0;
        return (_command & C.ParityMask) switch
        {
            C.OddParity => !odd,
            C.EvenParity => odd,
            C.MarkParity => true,
            _ => false
        };
    }

    private void TransmitClock()
    {
        if ((_command & C.Echo) != 0)
        {
            SetTransmit(!_cts && (_status & C.Overrun) == 0 ? _rxd : true);
            return;
        }
        if (_transmitTicks > 0 && --_transmitTicks > 0) return;
        if (_transmitRemaining > 0)
        {
            SetTransmit(_cts || (_transmitFrame & 1) != 0);
            _transmitFrame >>= 1;
            _transmitRemaining--;
            _transmitTicks = _transmitRemaining == 0 ? _transmitStopTicks : C.Oversampling;
            return;
        }
        if (_cts || Rts) { SetTransmit(true); return; }
        if ((_status & C.TransmitEmpty) == 0)
        {
            var bits = DataBits;
            var data = _transmitData & ((1 << bits) - 1);
            _transmitFrame = (uint)data;
            if (HasParity)
            {
                if (ParityBit(data)) _transmitFrame |= 1u << bits;
                bits++;
            }
            _transmitFrame |= 1u << bits;
            _transmitRemaining = bits + 1;
            _transmitStopTicks = StopTicks;
            _transmitTicks = C.Oversampling;
            _status |= C.TransmitEmpty;
            SetTransmit(false);
        }
        else if ((_command & C.TransmitControlMask) == C.TransmitBreak)
        {
            SetTransmit(false);
            return;
        }
        else
        {
            SetTransmit(true);
            _transmitTicks = C.Oversampling * (1 + DataBits + (HasParity ? 1 : 0)) + StopTicks;
        }
        if ((_command & C.TransmitControlMask) == C.TransmitInterrupt) RaiseInterrupt(C.TransmitEmpty);
    }

    private void ReceiveClock()
    {
        if (_dcd)
        {
            _receiveStep = C.IdleReceive;
            _receiveTicks = 0;
            return;
        }
        if (_receiveStep == C.IdleReceive)
        {
            if (_rxd) { _receiveTicks = 0; return; }
            if (++_receiveTicks < C.HalfBit) return;
            _receiveStep = 0;
            _receiveTicks = _receiveValue = 0;
            _receiveParity = _receiveParityError = false;
            return;
        }
        var sampleTicks = _receiveStep < DataBits || HasParity && _receiveStep == DataBits
            ? C.Oversampling : StopTicks;
        if (++_receiveTicks < sampleTicks) return;
        _receiveTicks = 0;
        if (_receiveStep < DataBits)
        {
            if (_rxd) _receiveValue |= 1 << _receiveStep;
            _receiveParity ^= _rxd;
            _receiveStep++;
            return;
        }
        if (HasParity && _receiveStep == DataBits)
        {
            _receiveParity ^= _rxd;
            _receiveParityError = (_command & C.ParityMask) switch
            {
                C.OddParity => !_receiveParity,
                C.EvenParity => _receiveParity,
                _ => false
            };
            _receiveStep++;
            return;
        }
        // Les erreurs sont rattachées au caractère reçu, jamais à une lecture de statut.
        var errors = (_receiveParityError ? C.ParityError : 0) | (!_rxd ? C.FramingError : 0)
            | ((_status & C.ReceiveFull) != 0 ? C.Overrun : 0);
        _receiveData = (byte)_receiveValue;
        _status = (byte)((_status & ~C.ReceiveStatusMask) | C.ReceiveFull | errors);
        if ((_command & C.DisableReceiveInterrupt) == 0) RaiseInterrupt(C.ReceiveFull);
        _receiveStep = C.IdleReceive;
    }
}
