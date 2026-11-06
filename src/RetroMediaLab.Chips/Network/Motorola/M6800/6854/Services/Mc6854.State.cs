using RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Contracts;
using C = RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Constants.Mc6854Constants;

namespace RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Services;

public sealed partial class Mc6854
{
    public Mc6854State CaptureState() => new()
    {
        FormatVersion = C.StateFormatVersion,
        CyclesPerByte = _cyclesPerByte,
        Receive = Array.ConvertAll(_receive, value => new Mc6854ReceiveByteState(value.Value, value.Address, value.Last, value.Valid)),
        Transmit = Array.ConvertAll(_transmit, value => new Mc6854TransmitByteState(value.Value, value.Last)),
        Prefetched = new(_prefetched.Value, _prefetched.Last),
        Outgoing = _outgoing.ToArray(),
        PacketBits = _packetBits?.ToArray(),
        BusEnable = _busEnable,
        BusSelected = _busSelected,
        BusRead = _busRead,
        BusData = _busData,
        BusRegister = _busRegister,
        ReceiveDmaSuppressed = _receiveDmaSuppressed,
        TransmitDmaSuppressed = _transmitDmaSuppressed,
        Control1 = _control1,
        Control2 = _control2,
        Control3 = _control3,
        Control4 = _control4,
        ReceiveStatus = _receiveStatus,
        ReadReceiveStatus = _readReceiveStatus,
        ReadTransmitStatus = _readTransmitStatus,
        AcknowledgedReceive = _acknowledgedReceive,
        AcknowledgedTransmit = _acknowledgedTransmit,
        ClockFraction = _clockFraction,
        StableIdle = _stableIdle,
        ClockPresent = _clockPresent,
        ClearToSend = _clearToSend,
        CarrierPresent = _carrierPresent,
        CtsRaised = _ctsRaised,
        DcdRaised = _dcdRaised,
        ResetAsserted = _resetAsserted,
        LineIdle = _lineIdle,
        RtsAsserted = _rtsAsserted,
        RtsReleaseBits = _rtsReleaseBits,
        LoopOnline = _loopOnline,
        LoopActive = _loopActive,
        LoopPrefixZero = _loopPrefixZero,
        LoopWaitingForData = _loopWaitingForData,
        LoopDelay = _loopDelay,
        LoopOnes = _loopOnes,
        TransmitOutputsDirty = _transmitOutputsDirty,
        PublishedRts = _publishedRts,
        PublishedTransmitting = _publishedTransmitting,
        PacketOffset = _packetOffset,
        PacketLength = _packetLength,
        PacketLineLevel = _packetLineLevel,
        FillBit = _fillBit,
        ReceiveHead = _receiveHead,
        ReceiveCount = _receiveCount,
        ReceiveField = _receiveField,
        FlagDetected = _flagDetected,
        FlagRead = _flagRead,
        FlagOutput = _flagOutput,
        RxClock = _rxClock,
        RxData = _rxData,
        RxLastLevel = _rxLastLevel,
        RxInFrame = _rxInFrame,
        RxDiscard = _rxDiscard,
        RawWindow = _rawWindow,
        RawCount = _rawCount,
        RawOnes = _rawOnes,
        DataOnes = _dataOnes,
        ReceiveShift = _receiveShift,
        ReceiveBits = _receiveBits,
        RxTotalBits = _rxTotalBits,
        ReceiveCrc = _receiveCrc,
        StatusDirty = _statusDirty,
        CachedStatusOne = _cachedStatusOne,
        CachedStatusTwo = _cachedStatusTwo,
        IrqOutput = _irqOutput,
        TransmitHead = _transmitHead,
        TransmitCount = _transmitCount,
        PrefetchedValid = _prefetchedValid,
        TransmitField = _transmitField,
        TransmitPhase = _transmitPhase,
        QueuedPhase = _queuedPhase,
        Transmitting = _transmitting,
        TransmitComplete = _transmitComplete,
        Underrun = _underrun,
        FinishPending = _finishPending,
        AbortRequested = _abortRequested,
        TxClock = _txClock,
        TxData = _txData,
        NrziLevel = _nrziLevel,
        TransmitShift = _transmitShift,
        TransmitBits = _transmitBits,
        TransmitOnes = _transmitOnes,
        TransmitCrc = _transmitCrc,
        LastFlagAvailable = _lastFlagAvailable,
        MarkIdlePrefix = _markIdlePrefix,
    };

    public void RestoreState(Mc6854State state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.FormatVersion != C.StateFormatVersion || state.CyclesPerByte != _cyclesPerByte
            || state.Receive?.Length != _receive.Length || state.Transmit?.Length != _transmit.Length || state.Outgoing is null)
            throw new ArgumentException(nameof(state));
        for (var index = 0; index < _receive.Length; index++)
        {
            var value = state.Receive[index];
            _receive[index] = new(value.Value, value.Address, value.Last, value.Valid);
        }
        for (var index = 0; index < _transmit.Length; index++)
        {
            var value = state.Transmit[index];
            _transmit[index] = new(value.Value, value.Last);
        }
        _prefetched = new(state.Prefetched.Value, state.Prefetched.Last);
        _outgoing.Clear();
        _outgoing.AddRange(state.Outgoing);
        _packetBits = state.PacketBits?.ToArray();
        _busEnable = state.BusEnable;
        _busSelected = state.BusSelected;
        _busRead = state.BusRead;
        _busData = state.BusData;
        _busRegister = state.BusRegister;
        _receiveDmaSuppressed = state.ReceiveDmaSuppressed;
        _transmitDmaSuppressed = state.TransmitDmaSuppressed;
        _control1 = state.Control1;
        _control2 = state.Control2;
        _control3 = state.Control3;
        _control4 = state.Control4;
        _receiveStatus = state.ReceiveStatus;
        _readReceiveStatus = state.ReadReceiveStatus;
        _readTransmitStatus = state.ReadTransmitStatus;
        _acknowledgedReceive = state.AcknowledgedReceive;
        _acknowledgedTransmit = state.AcknowledgedTransmit;
        _clockFraction = state.ClockFraction;
        _stableIdle = state.StableIdle;
        _clockPresent = state.ClockPresent;
        _clearToSend = state.ClearToSend;
        _carrierPresent = state.CarrierPresent;
        _ctsRaised = state.CtsRaised;
        _dcdRaised = state.DcdRaised;
        _resetAsserted = state.ResetAsserted;
        _lineIdle = state.LineIdle;
        _rtsAsserted = state.RtsAsserted;
        _rtsReleaseBits = state.RtsReleaseBits;
        _loopOnline = state.LoopOnline;
        _loopActive = state.LoopActive;
        _loopPrefixZero = state.LoopPrefixZero;
        _loopWaitingForData = state.LoopWaitingForData;
        _loopDelay = state.LoopDelay;
        _loopOnes = state.LoopOnes;
        _transmitOutputsDirty = state.TransmitOutputsDirty;
        _publishedRts = state.PublishedRts;
        _publishedTransmitting = state.PublishedTransmitting;
        _packetOffset = state.PacketOffset;
        _packetLength = state.PacketLength;
        _packetLineLevel = state.PacketLineLevel;
        _fillBit = state.FillBit;
        _receiveHead = state.ReceiveHead;
        _receiveCount = state.ReceiveCount;
        _receiveField = state.ReceiveField;
        _flagDetected = state.FlagDetected;
        _flagRead = state.FlagRead;
        _flagOutput = state.FlagOutput;
        _rxClock = state.RxClock;
        _rxData = state.RxData;
        _rxLastLevel = state.RxLastLevel;
        _rxInFrame = state.RxInFrame;
        _rxDiscard = state.RxDiscard;
        _rawWindow = state.RawWindow;
        _rawCount = state.RawCount;
        _rawOnes = state.RawOnes;
        _dataOnes = state.DataOnes;
        _receiveShift = state.ReceiveShift;
        _receiveBits = state.ReceiveBits;
        _rxTotalBits = state.RxTotalBits;
        _receiveCrc = state.ReceiveCrc;
        _statusDirty = state.StatusDirty;
        _cachedStatusOne = state.CachedStatusOne;
        _cachedStatusTwo = state.CachedStatusTwo;
        _irqOutput = state.IrqOutput;
        _transmitHead = state.TransmitHead;
        _transmitCount = state.TransmitCount;
        _prefetchedValid = state.PrefetchedValid;
        _transmitField = state.TransmitField;
        _transmitPhase = state.TransmitPhase;
        _queuedPhase = state.QueuedPhase;
        _transmitting = state.Transmitting;
        _transmitComplete = state.TransmitComplete;
        _underrun = state.Underrun;
        _finishPending = state.FinishPending;
        _abortRequested = state.AbortRequested;
        _txClock = state.TxClock;
        _txData = state.TxData;
        _nrziLevel = state.NrziLevel;
        _transmitShift = state.TransmitShift;
        _transmitBits = state.TransmitBits;
        _transmitOnes = state.TransmitOnes;
        _transmitCrc = state.TransmitCrc;
        _lastFlagAvailable = state.LastFlagAvailable;
        _markIdlePrefix = state.MarkIdlePrefix;
    }
}