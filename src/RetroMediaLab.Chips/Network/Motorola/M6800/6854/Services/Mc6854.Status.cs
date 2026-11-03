using C = RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Constants.Mc6854Constants;

namespace RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Services;

public sealed partial class Mc6854
{
    private bool _statusDirty = true;
    private byte _cachedStatusOne;
    private byte _cachedStatusTwo;

    private byte StatusOne()
    {
        UpdateStatus();
        return _cachedStatusOne;
    }

    private byte StatusTwo()
    {
        UpdateStatus();
        return _cachedStatusTwo;
    }

    private void UpdateStatus()
    {
        if (!_statusDirty)
            return;
        _cachedStatusTwo = ComputeStatusTwo();
        _cachedStatusOne = ComputeStatusOne(_cachedStatusTwo);
        _statusDirty = false;
    }

    private bool TransmitRoom => _transmitCount <= ((_control2 & C.TwoByte) != 0 ? 1 : 2);

    private byte RawReceiveStatus()
    {
        var status = _receiveStatus;
        if (!_carrierPresent || _dcdRaised)
            status |= C.DataCarrierDetect;
        if ((_control1 & C.ReceiveReset) == 0)
        {
            if (_receiveCount != 0 && _receive[_receiveHead].Address)
                status |= C.AddressPresent;
            if (_receiveCount >= ((_control2 & C.TwoByte) != 0 ? 2 : 1))
                status |= C.ReceiveAvailable;
            if (_rawOnes >= C.IdleOnes)
                status |= C.Idle;
            else if (_rawOnes >= C.AbortOnes)
                status |= C.AbortReceived;
        }
        return status;
    }

    private byte ComputeStatusTwo()
    {
        var status = RawReceiveStatus();
        if ((_control2 & C.Prioritize) == 0)
            return status;
        if ((status & C.ReceiveErrors) != 0)
            return (byte)(status & C.ReceiveErrors);
        if ((status & C.Idle) != 0)
            return C.Idle;
        if ((status & C.AddressPresent) != 0)
            return C.AddressPresent;
        return status;
    }

    private byte ComputeStatusOne(byte receive)
    {
        var status = 0;
        if ((receive & C.ReceiveAvailable) != 0)
            status |= C.DataAvailable;
        if ((receive & ~C.ReceiveAvailable) != 0)
            status |= C.StatusTwoRequest;
        if (_loopOnline && !_loopActive)
            status |= C.LoopStatus;
        if (_flagDetected && (_control3 & C.FlagDetectEnable) != 0)
            status |= C.FlagDetected;
        if (!_clearToSend || _ctsRaised)
            status |= C.NotClearToSend;
        if (_underrun)
            status |= C.TransmitUnderrun;
        if ((_control1 & C.TransmitReset) == 0 &&
            ((_control2 & C.FrameCompleteMode) != 0 ? _transmitComplete :
                _clearToSend && TransmitRoom && ((_control2 & C.Prioritize) == 0 || !_underrun)))
            status |= C.TransmitAvailable;
        // PSE : CTS supprime les deux états Tx inférieurs ; TU supprime TDRA/FC.
        if ((_control2 & C.Prioritize) != 0)
        {
            if ((status & C.NotClearToSend) != 0)
                status &= ~(C.TransmitUnderrun | C.TransmitAvailable);
            else if ((status & C.TransmitUnderrun) != 0)
                status &= ~C.TransmitAvailable;
        }
        var receiveRequest = receive & ~_acknowledgedReceive & ~C.ReceiveAvailable;
        var rxInterrupt = (_control1 & (C.ReceiveReset | C.ReceiveInterrupt)) == C.ReceiveInterrupt &&
            (receiveRequest != 0 || (status & C.FlagDetected) != 0 ||
                ((_control1 & C.ReceiveDma) == 0 && (receive & C.ReceiveAvailable) != 0));
        var txPending = status & ~_acknowledgedTransmit;
        var txInterrupt = (_control1 & (C.TransmitReset | C.TransmitInterrupt)) == C.TransmitInterrupt &&
            ((txPending & (C.NotClearToSend | C.TransmitUnderrun)) != 0 ||
                ((_control1 & C.TransmitDma) == 0 && (status & C.TransmitAvailable) != 0));
        if (rxInterrupt || txInterrupt)
            status |= C.Interrupt;
        return (byte)status;
    }

    private void ClearReceiveStatus()
    {
        _acknowledgedReceive |= (byte)(_readReceiveStatus & (C.Idle | C.AbortReceived | C.DataCarrierDetect));
        _receiveStatus &= (byte)~_readReceiveStatus;
        if ((_readReceiveStatus & C.DataCarrierDetect) != 0)
            _dcdRaised = false;
        _readReceiveStatus = 0;
        // FD est dans SR1, pas SR2 ; Clear Rx le remet à zéro après lecture SR1.
        if (_flagRead)
            _flagDetected = false;
        _flagRead = false;
    }

    private void ClearTransmitStatus()
    {
        _acknowledgedTransmit |= _readTransmitStatus;
        if ((_readTransmitStatus & C.TransmitUnderrun) != 0)
            _underrun = false;
        if ((_readTransmitStatus & C.NotClearToSend) != 0)
            _ctsRaised = false;
        if ((_readTransmitStatus & C.TransmitAvailable) != 0 && (_control2 & C.FrameCompleteMode) != 0)
            _transmitComplete = false;
        _readTransmitStatus = 0;
    }

    private void SetReceiveEvent(byte status)
    {
        _statusDirty = true;
        _receiveStatus |= status;
        _acknowledgedReceive &= (byte)~status;
    }
}
