using C = RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Constants.Mc6854Constants;

namespace RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Services;

public sealed partial class Mc6854
{
    private bool _loopOnline;
    private bool _loopActive;
    private bool _loopPrefixZero;
    private bool _loopWaitingForData;
    private bool _loopDelay = true;
    private int _loopOnes;

    private void ResetLoop()
    {
        _loopOnline = _loopActive = _loopPrefixZero = _loopWaitingForData = false;
        _loopDelay = true;
        _loopOnes = 0;
    }

    private void ReceiveLoopBit(bool bit)
    {
        if ((_control3 & C.Loop) == 0)
            return;
        _loopOnes = bit ? Math.Min(_loopOnes + 1, C.BitsPerOctet) : 0;
        if (!_loopOnline)
        {
            if ((_control3 & C.LoopOnline) != 0 && _loopOnes == C.AbortOnes)
            {
                _statusDirty = true;
                _loopOnline = true;
            }
            return;
        }
        if (_loopActive)
            return;
        if ((_control3 & C.LoopOnline) == 0 && _loopOnes == C.BitsPerOctet)
        {
            _statusDirty = true;
            _loopOnline = false;
            return;
        }
        if ((_control3 & C.Test) != 0 && _loopOnes == C.AbortOnes)
        {
            // Le septième 1 du Go Ahead devient 0 : les bits déjà répétés forment un flag.
            _statusDirty = true;
            _loopActive = true;
            _loopPrefixZero = true;
            _loopWaitingForData = true;
            _lastFlagAvailable = true;
        }
    }

    private void TransmitLoopBit()
    {
        if ((_control3 & C.Loop) == 0 || _loopActive)
            return;
        var nrzi = (_control4 & C.Nrzi) != 0;
        if (_loopOnline)
            _txData = nrzi ? _loopDelay : _rxData;
        else
            _txData = nrzi ? _rxData : true;
        _loopDelay = _rxData;
    }
}
