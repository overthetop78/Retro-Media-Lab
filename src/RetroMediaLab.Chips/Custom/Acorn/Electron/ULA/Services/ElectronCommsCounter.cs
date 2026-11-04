using C = RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Constants.ElectronUlaConstants;

namespace RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Services;

/// <summary>Compteur numérique partagé son/cassette, avancé entre ses frontières.</summary>
/// <remarks>FE06 change PD sans effacer CNT ; FE07 choisit sa source sans réinitialisation.
/// Le modèle TX reprend les sauts du compteur ElectronFpga ; le son conserve PD+1.</remarks>
internal sealed class ElectronCommsCounter
{
    private int _inputPhase;
    private byte _divider;
    private int _value;
    private bool _soundLevel;
    private bool _receiveClockEdge;

    internal byte Divider => _divider;
    internal int Value => _value;
    internal bool SoundLevel => _soundLevel;
    internal bool ReceiveClockEdge => _receiveClockEdge;
    internal bool MarkLevel => (_value & C.CommsMarkBit) != 0;
    internal bool SpaceLevel => (_value & C.CommsSpaceBit) != 0;

    internal void WriteDivider(byte value) => _divider = value;

    internal bool TickSound()
    {
        var clocks = C.MasterClocksPerCpuClock;
        var frame = (_value & C.CommsFrameMask) < clocks;
        _receiveClockEdge = (_value & C.CommsLowMask) < clocks;
        if (_value < clocks)
        {
            clocks -= _value + C.CommsCounterStep;
            _value = (_divider << C.SoundDividerShift) | C.SoundReloadMask;
            _soundLevel = !_soundLevel;
        }
        _value -= clocks;
        return frame;
    }

    internal bool TickOutput()
    {
        var frame = false;
        _receiveClockEdge = false;
        var clocks = C.MasterClocksPerCpuClock;
        while (clocks != 0)
        {
            var remaining = _value & C.CommsLowMask;
            if (remaining == 0)
            {
                frame |= (_value & C.CommsFrameMask) == 0;
                _receiveClockEdge = true;
                _value = (_value - C.CommsTransmitSkip) & C.CommsCounterMask;
                clocks--;
            }
            else
            {
                var count = Math.Min(remaining, clocks);
                _value -= count;
                clocks -= count;
            }
        }
        return frame;
    }

    internal bool TickInput()
    {
        // Keep the duration invalid after timeout, while FRAMECK continues to run.
        if (_value < C.CommsCounterMask)
            _value = Math.Min(_value + C.MasterClocksPerCpuClock, C.CommsCounterMask);
        if (++_inputPhase < C.TapeMarkHalfPeriod) return false;
        _inputPhase = 0;
        // Valid half-waves supply their own phase edges. Continue them only once
        // the longest nominal half-wave has elapsed without a new input edge.
        return _value > C.TapeSpaceHalfPeriod * C.MasterClocksPerCpuClock;
    }

    internal int TakeInputPeriod()
    {
        var period = _value / C.MasterClocksPerCpuClock;
        _value = 0;
        _inputPhase = 0;
        return period;
    }

    internal void Reset()
    {
        _inputPhase = 0;
        _divider = 0;
        _value = 0;
        _soundLevel = false;
        _receiveClockEdge = false;
    }
}
