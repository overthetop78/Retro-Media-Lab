using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornElectron.Common.Constants.ElectronConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornElectron.Common.Services;

internal sealed partial class ElectronUla
{
    private byte _received;
    private byte _pendingByte;
    private bool _transmitPending;
    private int _transmitBits;
    private int _transmitPhase;
    private int _transmitFrame;
    private bool _lastInput = true;
    private int _receiveStep = -1;
    private int _receivePhase;
    private byte _receiveByte;
    private int _carrierPhase;
    private int _carrierBits;
    private bool _carrierReported;

    private void ResetSerial()
    {
        _received = _pendingByte = _receiveByte = 0;
        _transmitPending = false;
        _transmitBits = _transmitPhase = _transmitFrame = 0;
        _receiveStep = -1;
        _receivePhase = _carrierPhase = _carrierBits = 0;
        _carrierReported = false;
        _lastInput = true;
    }

    private void TickSerial()
    {
        if (_comms == C.TapeOutputMode)
        {
            if (_transmitBits == 0 && !_transmitPending) _status |= C.TransmitEmptyInterrupt;
            if (_transmitBits == 0 && _transmitPending)
            {
                _transmitFrame = (_pendingByte << C.TapeFirstDataPosition) | (1 << C.TapeStopBitPosition);
                _transmitBits = C.TapeFrameBits;
                _transmitPhase = 0;
                _transmitPending = false;
            }
            _cassette.SetOutputBit(_transmitBits == 0 || (_transmitFrame & 1) != 0);
            if (_transmitBits != 0 && (_transmitPhase += C.TapeBaud) >= C.CpuClock)
            {
                _transmitPhase -= C.CpuClock;
                _transmitFrame >>= 1;
                _transmitBits--;
                if (_transmitBits == C.TapeTransmitReadyBits) _status |= C.TransmitEmptyInterrupt;
            }
            return;
        }
        _cassette.SetOutputBit(true);
        if (_comms != C.TapeInputMode || !Motor || !_cassette.InputPresent)
        {
            _receiveStep = -1;
            _carrierBits = _carrierPhase = 0;
            _carrierReported = false;
            _lastInput = true;
            return;
        }
        var bit = _cassette.InputBit;
        if (!bit)
        {
            _carrierBits = _carrierPhase = 0;
            _carrierReported = false;
        }
        else if (!_carrierReported && (_carrierPhase += C.TapeBaud) >= C.CpuClock)
        {
            _carrierPhase -= C.CpuClock;
            if (++_carrierBits >= C.TapeCarrierBits)
            {
                _carrierReported = true;
                _status |= C.HighToneInterrupt;
            }
        }
        if (_receiveStep < 0 && _lastInput && !bit)
        {
            _receiveStep = 0;
            _receiveByte = 0;
            _receivePhase = -C.CpuClock / C.TapeReceivePhaseDelay;
        }
        _lastInput = bit;
        if (_receiveStep < 0 || (_receivePhase += C.TapeBaud) < C.CpuClock) return;
        _receivePhase -= C.CpuClock;
        if (_receiveStep < C.TapeDataBits)
        {
            if (bit) _receiveByte |= (byte)(1 << _receiveStep);
            _receiveStep++;
            return;
        }
        if (bit)
        {
            _received = _receiveByte;
            _status |= C.ReceiveFullInterrupt;
        }
        _receiveStep = -1;
    }
}
