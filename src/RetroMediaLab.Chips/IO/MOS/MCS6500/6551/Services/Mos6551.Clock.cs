using C = RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6551.Constants.Mos6551Constants;

namespace RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6551.Services;

public sealed partial class Mos6551
{
    private long _clockPhase;
    private long _clockPeriod;
    private int _divider;
    private bool _internalReceiveClock;
    private bool _internalClockHigh;
    private bool _externalReceiveClock;
    private bool _externalTransmitClock;
    private int _externalTransmitEdges;
    public bool ReceiveClockIsOutput => _internalReceiveClock;
    public bool ReceiveClockOutput { get; private set; } = true;

    public void Advance(int ticks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(ticks);
        if (_resetHeld || (!_enabled && _receiveStep == C.IdleReceive) || _crystalClock == 0) return;
        _clockPhase += (long)ticks * _crystalClock * C.ClockEdgesPerPeriod;
        if (_clockPhase >= _clockPeriod) AdvanceClocks();
    }

    private void AdvanceClocks()
    {
        while (_clockPhase >= _clockPeriod)
        {
            _clockPhase -= _clockPeriod;
            _internalClockHigh = !_internalClockHigh;
            InternalClock(_internalClockHigh);
        }
    }

    /// <summary>Niveau de XTAL1 sans quartz automatique ; le diviseur programmé reste applicable.</summary>
    public void SetTransmitClock(bool high)
    {
        if (_externalTransmitClock == high) return;
        _externalTransmitClock = high;
        if (_resetHeld) return;
        if (++_externalTransmitEdges < _divider) return;
        _externalTransmitEdges = 0;
        _internalClockHigh = !_internalClockHigh;
        InternalClock(_internalClockHigh);
    }

    /// <summary>Niveau physique RxC ; les montées échantillonnent RXD en mode externe.</summary>
    public void SetReceiveClock(bool high)
    {
        if (_externalReceiveClock == high) return;
        _externalReceiveClock = high;
        if (high && !_internalReceiveClock && !_resetHeld) ReceiveClock();
    }

    public void AdvanceReceiveClock(int transitions)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(transitions);
        var rising = transitions / C.ClockEdgesPerPeriod
            + ((transitions & C.LeastSignificantBit) != 0 && !_externalReceiveClock ? C.LeastSignificantBit : 0);
        if (!_internalReceiveClock && !_resetHeld && (_enabled || _receiveStep != C.IdleReceive))
            for (var edge = 0; edge < rising; edge++) ReceiveClock();
        if ((transitions & C.LeastSignificantBit) != 0) _externalReceiveClock = !_externalReceiveClock;
    }

    private void InternalClock(bool high)
    {
        if (_internalReceiveClock)
        {
            SetReceiveClockOutput(high);
            if (high) ReceiveClock();
        }
        if (!high && _enabled) TransmitClock();
    }

    private void SetReceiveClockOutput(bool high)
    {
        if (ReceiveClockOutput == high) return;
        ReceiveClockOutput = high;
        ReceiveClockChanged?.Invoke(high);
    }
}
