using RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Enums;
using C = RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Constants.Mc6854Constants;

namespace RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Services;

public sealed partial class Mc6854
{
    private readonly record struct ReceiveByte(byte Value, bool Address, bool Last, bool Valid);
    private readonly ReceiveByte[] _receive = new ReceiveByte[C.FifoSize];
    private int _receiveHead;
    private int _receiveCount;
    private AdlcFrameField _receiveField;
    private bool _flagDetected;
    private bool _flagRead;
    private bool _flagOutput;
    private bool _rxClock;
    private bool _rxData = true;
    private bool _rxLastLevel = true;
    private bool _rxInFrame;
    private bool _rxDiscard;
    private byte _rawWindow;
    private int _rawCount;
    private int _rawOnes;
    private int _dataOnes;
    private uint _receiveShift;
    private int _receiveBits;
    private int _rxTotalBits;
    private ushort _receiveCrc = C.CrcInitial;

    public bool FlagDetect => _flagOutput;
    public void SetReceiveData(bool level)
    {
        if (_rxData != level)
            _stableIdle = false;
        _rxData = level;
    }

    /// <summary>Le récepteur échantillonne uniquement sur le front montant RxC.</summary>
    public void SetReceiveClock(bool level)
    {
        var rising = level && !_rxClock;
        _rxClock = level;
        if (!rising)
            return;
        _stableIdle = false;
        _flagOutput = false;
        var input = (_control3 & (C.Loop | C.Test)) == C.Test ? _txData : _rxData;
        var bit = (_control4 & C.Nrzi) == 0 ? input : input == _rxLastLevel;
        _rxLastLevel = input;
        if (_resetAsserted || (_control1 & C.ReceiveReset) != 0 || !_carrierPresent || _dcdRaised)
            return;
        ReceiveLoopBit(bit);
        ReceiveBit(bit);
        UpdateStatus();
    }

    private void ResetReceive()
    {
        _receiveHead = _receiveCount = 0;
        _receiveField = AdlcFrameField.Address;
        _packetBits = null;
        _packetOffset = _packetLength = 0;
        _flagDetected = _flagRead = _flagOutput = false;
        _rxInFrame = _rxDiscard = false;
        _rawWindow = 0;
        _rawCount = _rawOnes = _dataOnes = 0;
        _receiveShift = 0;
        _receiveBits = _rxTotalBits = 0;
        _receiveCrc = C.CrcInitial;
    }

    private byte ReadReceive()
    {
        if ((_control1 & C.ReceiveReset) != 0 || _receiveCount == 0)
            return 0;
        _statusDirty = true;
        var result = _receive[_receiveHead].Value;
        _receiveHead = NextFifoIndex(_receiveHead);
        _receiveCount--;
        ObserveReceiveFront();
        return result;
    }

    private void ObserveReceiveFront()
    {
        if (_receiveCount != 0 && _receive[_receiveHead].Last)
        {
            var status = _receive[_receiveHead].Valid ? C.FrameValid : C.FcsError;
            if ((_receiveStatus & status) == 0)
                SetReceiveEvent(status);
        }
    }

    private void AppendReceive(byte value, bool last = false, bool valid = true)
    {
        var address = _receiveField == AdlcFrameField.Address;
        _receiveField = NextField(_receiveField, value);
        if (_rxDiscard || (_receiveStatus & (C.FrameValid | C.FcsError)) != 0)
            return;
        _statusDirty = true;
        var tail = _receiveHead + _receiveCount;
        if (tail >= C.FifoSize)
            tail -= C.FifoSize;
        if (_receiveCount == C.FifoSize)
        {
            // Overrun détruit les données de la FIFO, pas le registre à décalage.
            tail = _receiveHead + C.FifoSize - 1;
            if (tail >= C.FifoSize)
                tail -= C.FifoSize;
            SetReceiveEvent(C.ReceiveOverrun);
        }
        else
            _receiveCount++;
        _receive[tail] = new(value, address, last, valid);
        ObserveReceiveFront();
    }

    private void ReceiveBit(bool bit)
    {
        var previousOnes = _rawOnes;
        if (bit)
        {
            if (_rawOnes < C.IdleOnes)
                _rawOnes++;
            if (_rawOnes == C.AbortOnes)
            {
                if (_rxInFrame && _rxTotalBits + _rawCount >= C.ReportableAbortBits)
                    SetReceiveEvent(C.AbortReceived);
                DiscardCurrentReceive();
            }
            if (_rawOnes == C.IdleOnes && (_receiveStatus & C.Idle) == 0 &&
                (_acknowledgedReceive & C.Idle) == 0)
                SetReceiveEvent(C.Idle);
        }
        else
        {
            _rawOnes = 0;
            _acknowledgedReceive &= unchecked((byte)~(C.Idle | C.AbortReceived));
        }
        if ((previousOnes >= C.AbortOnes) != (_rawOnes >= C.AbortOnes) ||
            (previousOnes >= C.IdleOnes) != (_rawOnes >= C.IdleOnes))
            _statusDirty = true;
        _rawWindow = (byte)((_rawWindow >> 1) | (bit ? C.ExtensionLogicalContinue : 0));
        if (++_rawCount < C.BitsPerOctet)
            return;
        if (_rawWindow == C.Flag)
        {
            ReceiveFlag();
            _rawCount = 0;
            return;
        }
        _rawCount = C.BitsPerOctet - 1;
        if (!_rxInFrame)
            return;
        var data = (_rawWindow & 1) != 0;
        if (!data && _dataOnes == C.StuffingOnes)
        {
            _dataOnes = 0;
            return;
        }
        _dataOnes = data ? _dataOnes + 1 : 0;
        _rxTotalBits++;
        _receiveShift |= (data ? 1U : 0U) << _receiveBits++;
        var width = FieldLength(_receiveField, C.ReceiveWordLengthShift);
        // Le détecteur brut retient déjà sept bits pour reconnaître le flag.
        // Ne pas ajouter une seconde fois ce délai au registre à décalage.
        if (_receiveBits >= width + C.ReceiveLookaheadBits - (C.BitsPerOctet - 1))
            ShiftReceiveWord(width);
    }

    private void ShiftReceiveWord(int width)
    {
        var value = (byte)(_receiveShift & ((1U << width) - 1));
        _receiveCrc = CrcWord(_receiveCrc, value, width);
        _receiveShift >>= width;
        _receiveBits -= width;
        AppendReceive(value);
    }

    private void ReceiveFlag()
    {
        _flagOutput = true;
        if ((_control3 & C.FlagDetectEnable) != 0)
        {
            if (!_flagDetected)
            {
                _statusDirty = true;
                _flagRead = false;
            }
            _flagDetected = true;
        }
        if (_rxInFrame && _rxTotalBits >= C.MinimumFrameBits)
        {
            while (_receiveBits > C.FcsBits)
            {
                var width = Math.Min(FieldLength(_receiveField, C.ReceiveWordLengthShift), _receiveBits - C.FcsBits);
                ShiftReceiveWord(width);
            }
            _receiveCrc = CrcWord(_receiveCrc, _receiveShift & byte.MaxValue, C.BitsPerOctet);
            _receiveCrc = CrcWord(_receiveCrc, (_receiveShift >> C.BitsPerOctet) & byte.MaxValue, C.BitsPerOctet);
            if (!_rxDiscard && _receiveCount != 0 && (_receiveStatus & (C.FrameValid | C.FcsError)) == 0)
            {
                var tail = _receiveHead + _receiveCount - 1;
                if (tail >= C.FifoSize)
                    tail -= C.FifoSize;
                var valid = _rxTotalBits >= C.MinimumValidFrameBits && _receiveCrc == C.CrcResidue &&
                    _receiveField == AdlcFrameField.Information;
                _receive[tail] = _receive[tail] with { Last = true, Valid = valid };
                ObserveReceiveFront();
            }
        }
        _control1 &= unchecked((byte)~C.Discontinue);
        _rxInFrame = true;
        _rxDiscard = false;
        _receiveField = AdlcFrameField.Address;
        _receiveShift = 0;
        _receiveBits = _rxTotalBits = _dataOnes = 0;
        _receiveCrc = C.CrcInitial;
    }

    private void DiscardCurrentReceive()
    {
        _statusDirty = true;
        // Une frontière de la trame précédente protège ses octets encore non lus.
        var keep = 0;
        for (var offset = 0; offset < _receiveCount; offset++)
        {
            var index = _receiveHead + offset;
            if (index >= C.FifoSize)
                index -= C.FifoSize;
            if (_receive[index].Last)
                keep = offset + 1;
        }
        _receiveCount = keep;
        _rxInFrame = false;
        _rxDiscard = false;
        _receiveShift = 0;
        _receiveBits = _rxTotalBits = 0;
        _control1 &= unchecked((byte)~C.Discontinue);
    }

    private void DiscontinueReceive()
    {
        if (!_rxInFrame || _rxTotalBits == 0)
        {
            _control1 &= unchecked((byte)~C.Discontinue);
            return;
        }
        var inFrame = _rxInFrame;
        DiscardCurrentReceive();
        _rxInFrame = inFrame;
        _rxDiscard = true;
        _control1 |= C.Discontinue;
    }

    private static int NextFifoIndex(int index) => index == C.FifoSize - 1 ? 0 : index + 1;
}
