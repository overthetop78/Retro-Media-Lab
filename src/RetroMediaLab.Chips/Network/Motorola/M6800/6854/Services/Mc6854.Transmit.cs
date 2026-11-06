using RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Enums;
using C = RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Constants.Mc6854Constants;

namespace RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Services;

public sealed partial class Mc6854
{
    private readonly record struct TransmitByte(byte Value, bool Last);
    private readonly TransmitByte[] _transmit = new TransmitByte[C.FifoSize];
    private readonly List<byte> _outgoing = new();
    private int _transmitHead;
    private int _transmitCount;
    private TransmitByte _prefetched;
    private bool _prefetchedValid;
    private AdlcFrameField _transmitField;
    private AdlcTransmitPhase _transmitPhase;
    private AdlcTransmitPhase _queuedPhase;
    private bool _transmitting;
    private bool _transmitComplete;
    private bool _underrun;
    private bool _finishPending;
    private bool _abortRequested;
    private bool _txClock;
    private bool _txData = true;
    private bool _nrziLevel = true;
    private uint _transmitShift;
    private int _transmitBits;
    private int _transmitOnes;
    private ushort _transmitCrc = C.CrcInitial;
    private bool _lastFlagAvailable;
    private bool _markIdlePrefix;

    public bool TransmitData => _txData;

    /// <summary>Le transmetteur décale sur le front descendant TxC ; CTS ne fige pas le registre chargé.</summary>
    public void SetTransmitClock(bool level)
    {
        var falling = !level && _txClock;
        var rising = level && !_txClock;
        _txClock = level;
        if (rising)
        {
            PrefetchTransmitWord();
            UpdateStatus();
        }
        if (!falling)
            return;
        _stableIdle = false;
        var wasTransmitting = _transmitting;
        var bit = true;
        if (!_resetAsserted && (_control1 & C.TransmitReset) == 0)
        {
            if (TransmitIsMarkIdle)
            {
                _lastFlagAvailable = false;
                _queuedPhase = AdlcTransmitPhase.Idle;
            }
            else
                bit = TransmitBit();
        }
        if ((_control4 & C.Nrzi) != 0)
        {
            // Le codage NRZI ajoute le retard d’un bit décrit par Motorola.
            _txData = _nrziLevel;
            if (!bit)
                _nrziLevel = !_nrziLevel;
        }
        else
            _txData = bit;
        TransmitLoopBit();
        if ((_control2 & C.Rts) == 0 && !_transmitting && !wasTransmitting && _transmitCount == 0)
        {
            if (_rtsAsserted && ++_rtsReleaseBits >= C.RtsReleaseBits)
            {
                _rtsAsserted = false;
                _transmitOutputsDirty = true;
            }
        }
        else
            _rtsReleaseBits = 0;
        UpdateStatus();
    }

    private void ResetTransmit()
    {
        _transmitHead = _transmitCount = 0;
        _prefetchedValid = false;
        _outgoing.Clear();
        _transmitField = AdlcFrameField.Address;
        _transmitPhase = _queuedPhase = AdlcTransmitPhase.Idle;
        _transmitting = _underrun = _finishPending = _transmitComplete = _abortRequested = false;
        _transmitShift = 0;
        _transmitBits = _transmitOnes = 0;
        _transmitCrc = C.CrcInitial;
        _lastFlagAvailable = _markIdlePrefix = false;
        _txData = _nrziLevel = true;
        _transmitOutputsDirty = true;
    }

    private void WriteTransmit(byte value, bool last)
    {
        if ((_control1 & C.TransmitReset) != 0 || _resetAsserted || _transmitCount == C.FifoSize)
            return;
        var tail = _transmitHead + _transmitCount;
        if (tail >= C.FifoSize)
            tail -= C.FifoSize;
        _transmit[tail] = new(value, last);
        _transmitCount++;
        _transmitOutputsDirty = true;
    }

    private void MarkTransmitEnd()
    {
        if (_transmitCount != 0)
        {
            var tail = _transmitHead + _transmitCount - 1;
            if (tail >= C.FifoSize)
                tail -= C.FifoSize;
            _transmit[tail] = _transmit[tail] with { Last = true };
        }
        else if (_prefetchedValid)
            _prefetched = _prefetched with { Last = true };
        else if (_transmitting && _queuedPhase == AdlcTransmitPhase.Data)
            _finishPending = true;
    }

    private void RequestAbort()
    {
        _statusDirty = true;
        _transmitHead = _transmitCount = 0;
        _prefetchedValid = false;
        _abortRequested = true;
        _outgoing.Clear();
        _finishPending = false;
        _transmitOutputsDirty = true;
    }

    // Sans données, flag fill ou boucle, la sortie logique reste 1. Les fronts et RTS
    // continuent d’être traités, sans recréer un registre de décalage d’un bit constant.
    private bool TransmitIsMarkIdle => _transmitBits == 0 && _transmitPhase == AdlcTransmitPhase.Idle &&
        _transmitCount == 0 && !_transmitting && !_abortRequested && !_loopPrefixZero &&
        !_markIdlePrefix && (_control2 & C.FlagIdle) == 0 && (_control3 & C.Loop) == 0;

    private bool TransmitBit()
    {
        if (_loopPrefixZero)
        {
            _loopPrefixZero = false;
            return false;
        }
        if (_abortRequested)
        {
            _abortRequested = false;
            QueueRaw(uint.MaxValue, (_control4 & C.ExtendedAbort) != 0 ? C.LongAbortBits : C.ShortAbortBits,
                AdlcTransmitPhase.Abort);
            _transmitPhase = AdlcTransmitPhase.Idle;
            _transmitting = true;
            _transmitOutputsDirty = true;
            _lastFlagAvailable = false;
        }
        if (_transmitBits == 0)
            RefillTransmit();
        var bit = (_transmitShift & 1) != 0;
        _transmitShift >>= 1;
        if (--_transmitBits == 0)
            FinishQueuedField();
        return bit;
    }

    private void RefillTransmit()
    {
        switch (_transmitPhase)
        {
            case AdlcTransmitPhase.Idle:
                if (_loopActive && _loopWaitingForData && _transmitCount == 0 && (_control2 & C.FlagIdle) == 0)
                {
                    BeginUnderrunAbort();
                    return;
                }
                if (_transmitCount == 0 || ((_control3 & C.Loop) != 0 && !_loopActive))
                {
                    if ((_control2 & C.FlagIdle) != 0)
                        QueueRaw(C.Flag, C.BitsPerOctet, AdlcTransmitPhase.OpeningFlag);
                    else
                    {
                        QueueRaw(_markIdlePrefix ? 0U : 1U, 1, AdlcTransmitPhase.Idle);
                        _markIdlePrefix = false;
                    }
                    return;
                }
                _transmitting = true;
                _transmitOutputsDirty = true;
                _transmitField = AdlcFrameField.Address;
                _transmitCrc = C.CrcInitial;
                _transmitOnes = 0;
                _outgoing.Clear();
                _transmitPhase = AdlcTransmitPhase.Data;
                if (!_lastFlagAvailable || (_control4 & C.DoubleFlag) != 0)
                {
                    QueueRaw(C.Flag, C.BitsPerOctet, AdlcTransmitPhase.OpeningFlag);
                    return;
                }
                LoadTransmitWord();
                return;
            case AdlcTransmitPhase.Data:
                LoadTransmitWord();
                return;
            case AdlcTransmitPhase.FrameCheck:
                QueueStuffed((ushort)~_transmitCrc, C.FcsBits, AdlcTransmitPhase.FrameCheck);
                _transmitPhase = AdlcTransmitPhase.ClosingFlag;
                return;
            case AdlcTransmitPhase.ClosingFlag:
                QueueRaw(C.Flag, C.BitsPerOctet, AdlcTransmitPhase.ClosingFlag);
                _transmitPhase = AdlcTransmitPhase.Idle;
                return;
            default:
                QueueRaw(1, 1, AdlcTransmitPhase.Idle);
                return;
        }
    }

    private void LoadTransmitWord()
    {
        _lastFlagAvailable = false;
        if (_transmitCount == 0 && !_prefetchedValid)
        {
            if (_finishPending)
            {
                _finishPending = false;
                _transmitPhase = AdlcTransmitPhase.FrameCheck;
                RefillTransmit();
                return;
            }
            BeginUnderrunAbort();
            return;
        }
        var next = _prefetchedValid ? _prefetched : PopTransmit();
        _prefetchedValid = false;
        _loopWaitingForData = false;
        var width = FieldLength(_transmitField, C.TransmitWordLengthShift);
        var value = (byte)(next.Value & ((1 << width) - 1));
        _transmitField = NextField(_transmitField, next.Value);
        _outgoing.Add(value);
        _transmitCrc = CrcWord(_transmitCrc, value, width);
        QueueStuffed(value, width, AdlcTransmitPhase.Data);
        if (next.Last)
            _transmitPhase = AdlcTransmitPhase.FrameCheck;
    }

    private TransmitByte PopTransmit()
    {
        _statusDirty = true;
        var next = _transmit[_transmitHead];
        _transmitHead = NextFifoIndex(_transmitHead);
        _transmitCount--;
        _transmitOutputsDirty = true;
        return next;
    }

    private void PrefetchTransmitWord()
    {
        if (_resetAsserted || (_control1 & C.TransmitReset) != 0 || _abortRequested ||
            _queuedPhase != AdlcTransmitPhase.Data || _transmitBits != 1 ||
            _transmitPhase != AdlcTransmitPhase.Data || _finishPending || _prefetchedValid)
            return;
        // Le transfert FIFO a lieu dans la dernière moitié de l’avant-dernier bit du mot.
        _statusDirty = true;
        _stableIdle = false;
        if (_transmitCount != 0)
        {
            _prefetched = PopTransmit();
            _prefetchedValid = true;
        }
        else
        {
            _underrun = true;
            _acknowledgedTransmit &= unchecked((byte)~C.TransmitUnderrun);
            RequestAbort();
        }
    }

    private void BeginUnderrunAbort()
    {
        _statusDirty = true;
        _underrun = true;
        _acknowledgedTransmit &= unchecked((byte)~C.TransmitUnderrun);
        RequestAbort();
        _abortRequested = false;
        QueueRaw(uint.MaxValue, (_control4 & C.ExtendedAbort) != 0 ? C.LongAbortBits : C.ShortAbortBits,
            AdlcTransmitPhase.Abort);
        _transmitPhase = AdlcTransmitPhase.Idle;
        _transmitting = true;
        _transmitOutputsDirty = true;
    }

    private void QueueRaw(uint bits, int count, AdlcTransmitPhase phase)
    {
        _transmitShift = bits;
        _transmitBits = count;
        _queuedPhase = phase;
        _transmitOnes = 0;
    }

    private void QueueStuffed(uint value, int count, AdlcTransmitPhase phase)
    {
        _transmitShift = 0;
        _transmitBits = 0;
        _queuedPhase = phase;
        for (var index = 0; index < count; index++)
        {
            var bit = ((value >> index) & 1) != 0;
            if (bit)
                _transmitShift |= 1U << _transmitBits;
            _transmitBits++;
            _transmitOnes = bit ? _transmitOnes + 1 : 0;
            if (_transmitOnes == C.StuffingOnes)
            {
                _transmitBits++;
                _transmitOnes = 0;
            }
        }
    }

    private void FinishQueuedField()
    {
        if (_queuedPhase == AdlcTransmitPhase.OpeningFlag)
        {
            _lastFlagAvailable = true;
            if (_loopActive && (_control3 & C.Test) == 0)
            {
                _statusDirty = true;
                _loopActive = false;
                _loopWaitingForData = false;
                _transmitComplete = true;
                _transmitting = false;
                _transmitOutputsDirty = true;
            }
            return;
        }
        if (_queuedPhase == AdlcTransmitPhase.Idle)
        {
            _lastFlagAvailable = false;
            return;
        }
        if (_queuedPhase is not (AdlcTransmitPhase.ClosingFlag or AdlcTransmitPhase.Abort))
            return;
        _statusDirty = true;
        _transmitComplete = true;
        _acknowledgedTransmit &= unchecked((byte)~C.TransmitAvailable);
        _transmitting = false;
        _transmitOutputsDirty = true;
        _finishPending = false;
        _lastFlagAvailable = _queuedPhase == AdlcTransmitPhase.ClosingFlag;
        _markIdlePrefix = (_control3 & C.ZeroIdle) != 0;
        if (_queuedPhase == AdlcTransmitPhase.Abort && (_control3 & C.Loop) != 0)
            _control3 &= unchecked((byte)~C.Test);
        if (_loopActive && (_control3 & C.Test) == 0)
            _loopActive = false;
        if (_queuedPhase == AdlcTransmitPhase.ClosingFlag)
        {
            var frame = _outgoing.ToArray();
            _outgoing.Clear();
            if ((_control3 & (C.Loop | C.Test)) != C.Test)
                FrameTransmitted?.Invoke(frame);
        }
    }
}
