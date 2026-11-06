using Phase = RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Enums.Intel8271Phase;
using SerialPhase = RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Enums.Intel8271SerialPhase;
using C = RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Constants.Intel8271Constants;

namespace RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Services;

public sealed partial class Intel8271
{
    private SerialPhase _serialPhase;
    private bool _readDataLevel;
    private bool _clockWindow;
    private bool _phaseLockedSeparator;
    private bool _previousPulseWasClock;
    private bool _readPulseSeen;
    private bool _separatorSynchronized;
    private bool _ploClockLevel;
    private long _readPulseGap;
    private int _readFullCellRun;
    private bool _clockPulse;
    private bool _dataPulse;
    private bool _serialClockStarted;
    private long _serialFraction;
    private byte _serialDataShift;
    private byte _serialClockShift;
    private int _serialBits;
    private int _serialZeroBits;
    private bool _serialSyncArmed;
    private bool _inSync;
    private readonly byte[] _serialHeader = new byte[C.FormatIdSize];
    private int _serialFieldOffset;
    private int _serialCrc;
    private int _serialPayloadOffset;
    private bool _serialSkipData;
    private int _serialIndexCount;
    private int _serialRequestTicks;
    private int _seekRetryCount;

    public bool InSync => _inSync;
    private bool SerialReading => _serialPhase != SerialPhase.Inactive;

    /// <summary>Niveau Data Window échantillonné au front Unseparated Data : intervalle entier en single-shot, phase en PLO.</summary>
    public void SetDataWindow(bool high) => _clockWindow = high;

    /// <summary>Niveau de la broche PLO/SS : vrai sélectionne le séparateur à verrouillage de phase.</summary>
    public void SetPhaseLockedSeparator(bool enabled)
    {
        if (_phaseLockedSeparator == enabled) return;
        _phaseLockedSeparator = enabled;
        _separatorSynchronized = _readPulseSeen = false;
    }

    /// <summary>Impulsion active provenant de la tête ; seul son front d’assertion est consommé.</summary>
    public void SetUnseparatedData(bool asserted)
    {
        var rising = asserted && !_readDataLevel;
        _readDataLevel = asserted;
        if (!rising || !SerialReading || _resetAsserted) return;
        var fullCell = _readPulseSeen && _readPulseGap >= (long)_clock * C.ChipCyclesPerBit;
        var synchronizationReached = fullCell && _readFullCellRun == C.SynchronizationZeroBits - 1;
        _readFullCellRun = fullCell ? Math.Min(C.SynchronizationZeroBits, _readFullCellRun + 1) : 1;
        if (!_inSync && synchronizationReached)
        {
            // La synchronisation est une suite de clocks sans données ; elle lève aussi
            // l'ambiguïté d'un premier front reçu au milieu d'un champ ou d'un gap.
            // Une nouvelle plage qualifiée recale aussi une recherche déjà synchronisée,
            // notamment après la jonction d'un champ réécrit ; une seule fois par plage.
            _ploClockLevel = _clockWindow;
            _separatorSynchronized = true;
            _serialFraction = 0;
            _clockPulse = _dataPulse = false;
            _serialDataShift = 0;
            _serialClockShift = C.NormalClocks;
            _serialZeroBits = C.SynchronizationZeroBits - 1;
            _serialSyncArmed = true;
            _previousPulseWasClock = true;
        }
        var clock = _phaseLockedSeparator
            ? _separatorSynchronized ? _clockWindow == _ploClockLevel
                : !_readPulseSeen || (fullCell ? _previousPulseWasClock : !_previousPulseWasClock)
            : !_readPulseSeen || (_clockWindow ? _previousPulseWasClock : !_previousPulseWasClock);
        _previousPulseWasClock = clock;
        _readPulseSeen = true;
        _readPulseGap = 0;
        if (clock)
        {
            if (!_serialClockStarted) { _serialFraction = 0; _serialClockStarted = true; }
            _clockPulse = true;
        }
        else _dataPulse = true;
    }

    private void ResetSerialRead()
    {
        ResetSerialWrite();
        _serialPhase = SerialPhase.Inactive;
        _serialClockStarted = _clockPulse = _dataPulse = _inSync = false;
        _serialFraction = 0;
        _serialBits = _serialZeroBits = _serialFieldOffset = _serialPayloadOffset = _serialIndexCount = _serialRequestTicks = 0;
        _serialDataShift = _serialClockShift = 0;
        _serialSyncArmed = false;
        _previousPulseWasClock = _readPulseSeen = _separatorSynchronized = false;
        _readPulseGap = 0;
        _readFullCellRun = 0;
    }

    private void StopSerialRead()
    {
        ResetSerialWrite();
        _serialPhase = SerialPhase.Inactive;
        _inSync = false;
        _serialRequestTicks = 0;
    }

    private void StartSerialRead()
    {
        ResetSerialRead();
        _serialPhase = _command == C.ReadId ? SerialPhase.WaitIndex : SerialPhase.SearchId;
        SetPhase(IsWriteCommand ? Phase.Write : _command is C.VerifySingle or C.VerifyMultiple ? Phase.Verify : Phase.Read);
        _buffer = [];
    }

    private void SerialIndexPulse()
    {
        if (!SerialReading) return;
        if (_serialPhase == SerialPhase.WaitFormatIndex) { BeginFormatAtIndex(); return; }
        if (_serialPhase == SerialPhase.Writing) { FormatEndIndex(); return; }
        if (_serialPhase == SerialPhase.WaitIndex)
        {
            _serialPhase = SerialPhase.SearchId;
            _serialIndexCount = 0;
        }
        else if (_serialPhase is SerialPhase.SearchId or SerialPhase.SearchData && ++_serialIndexCount >= C.SectorSearchIndexLimit) Complete(C.NotFound);
    }

    private void TickSerialRead()
    {
        if (_readPulseSeen && _readPulseGap < (long)_clock * C.ChipCyclesPerBit) _readPulseGap += _chipClock;
        if (_awaitingData && --_serialRequestTicks <= 0) { Complete(C.LateDma); return; }
        if (_serialPhase == SerialPhase.Writing) { TickSerialWrite(); return; }
        if (!_serialClockStarted) return;
        _serialFraction += _chipClock;
        var bitPeriod = (long)_clock * C.ChipCyclesPerBit;
        while (_serialFraction >= bitPeriod && SerialReading)
        {
            _serialFraction -= bitPeriod;
            ReceiveFmBit(_clockPulse, _dataPulse);
            _clockPulse = _dataPulse = false;
        }
    }

    private void ReceiveFmBit(bool clock, bool data)
    {
        if (_serialPhase == SerialPhase.WriteGap)
        {
            if (--_writeGapBits == 0) BeginSectorWrite();
            return;
        }
        _serialDataShift = (byte)((_serialDataShift << 1) | (data ? 1 : 0));
        _serialClockShift = (byte)((_serialClockShift << 1) | (clock ? 1 : 0));
        if (!_inSync)
        {
            if (clock && !data)
            {
                if (++_serialZeroBits >= C.SynchronizationZeroBits) _serialSyncArmed = true;
            }
            else _serialZeroBits = 0;
            if (_serialSyncArmed && (_serialClockShift == C.AddressMarkClocks && _serialDataShift != C.IndexAddressMark && IsAddressMark(_serialDataShift) || _serialClockShift == C.IndexMarkClocks && _serialDataShift == C.IndexAddressMark))
            {
                _serialBits = 0;
                _serialSyncArmed = false;
                _inSync = true;
                SerialAddressMark(_serialDataShift);
            }
            return;
        }
        if (++_serialBits < C.BitsPerByte) return;
        _serialBits = 0;
        if (_serialClockShift != C.NormalClocks) { Complete(C.ClockError); return; }
        SerialByte(_serialDataShift);
    }

    private static bool IsAddressMark(byte value) =>
        value is C.IdAddressMark or C.IndexAddressMark or C.NormalDataMark or C.DeletedDataMark;

    private void SearchSerial(SerialPhase phase)
    {
        _serialPhase = phase;
        _inSync = false;
        _serialBits = _serialZeroBits = 0;
        _serialSyncArmed = false;
        _separatorSynchronized = false;
        _readFullCellRun = 0;
    }

    private void SerialAddressMark(byte mark)
    {
        if (_serialPhase is SerialPhase.SearchId or SerialPhase.SearchData && mark == C.IdAddressMark)
        {
            _serialPhase = SerialPhase.Header;
            _serialFieldOffset = 0;
            _serialCrc = CrcByte(C.CrcInitial, mark);
            return;
        }
        if (_serialPhase == SerialPhase.SearchData && mark is C.NormalDataMark or C.DeletedDataMark)
        {
            var deleted = mark == C.DeletedDataMark;
            if (deleted) _deletedResult = C.DeletedDataFound;
            _serialSkipData = deleted && _command is C.ReadSingle or C.ReadMultiple or C.ScanData;
            _serialPhase = SerialPhase.Data;
            _serialPayloadOffset = 0;
            _serialCrc = CrcByte(C.CrcInitial, mark);
            SetPhase(_command switch
            {
                C.VerifySingle or C.VerifyMultiple => Phase.Verify,
                C.ScanData or C.ScanIncludingDeleted => Phase.Scan,
                _ => Phase.Read
            });
            if (_phase == Phase.Scan) { _buffer = new byte[_sectorSize]; ResetScanSector(); }
            return;
        }
        SearchSerial(_serialPhase);
    }

    private void SerialByte(byte value)
    {
        _serialCrc = CrcByte(_serialCrc, value);
        switch (_serialPhase)
        {
            case SerialPhase.Header:
                if (_serialFieldOffset < C.FormatIdSize)
                {
                    _serialHeader[_serialFieldOffset] = value;
                    if (_command == C.ReadId) SerialRequest(value);
                }
                if (!SerialReading || ++_serialFieldOffset != C.FormatIdSize + C.CrcByteCount) return;
                if (_serialCrc != 0) { Complete(C.IdCrc); return; }
                if (_command == C.ReadId) { SerialNextSector(); return; }
                if (_serialHeader[0] != _parameters[0])
                {
                    if (_seekRetryCount == C.MaximumTrackRetries) { Complete(C.NotFound); return; }
                    _seekRetryCount++;
                    StopSerialRead();
                    BeginPositioning();
                    return;
                }
                if (_serialHeader[2] != _sector ||
                    _serialHeader[3] != SizeCode(_sectorSize))
                {
                    SearchSerial(SerialPhase.SearchId);
                    return;
                }
                if (IsWriteCommand)
                {
                    _writeGapBits = C.Gap2Bytes * C.BitsPerByte;
                    _serialPhase = SerialPhase.WriteGap;
                    _inSync = false;
                }
                else SearchSerial(SerialPhase.SearchData);
                return;
            case SerialPhase.Data:
                if (!_serialSkipData && _phase != Phase.Verify)
                {
                    if (_phase == Phase.Scan || _phase == Phase.ScanTail)
                    {
                        if (_phase == Phase.Scan)
                        {
                            _offset = _serialPayloadOffset;
                            _buffer[_offset] = value;
                            SerialRequest(value);
                        }
                    }
                    else SerialRequest(value);
                }
                if (++_serialPayloadOffset == _sectorSize)
                {
                    _serialPhase = SerialPhase.DataCrc;
                    _serialFieldOffset = 0;
                }
                return;
            case SerialPhase.DataCrc:
                if (++_serialFieldOffset != C.CrcByteCount) return;
                if (_serialCrc != 0 && !_serialSkipData) { Complete((byte)(C.DataCrc | _deletedResult)); return; }
                if (_scanResult != 0 && _command is C.ScanData or C.ScanIncludingDeleted)
                    Complete((byte)(_scanResult | _deletedResult));
                else SerialNextSector();
                return;
        }
    }

    private void SerialRequest(byte value)
    {
        if (_awaitingData) { Complete(C.LateDma); return; }
        _data = value;
        _awaitingData = _dataRequest = true;
        _serialRequestTicks = _transferDeadlineTicks;
        SetStatus((byte)(C.Busy | ((_mode & C.NonDmaMode) != 0 ? C.Interrupt | C.NonDma : 0)));
    }

    private void SerialNextSector()
    {
        if (--_remaining == 0) { Complete(_deletedResult); return; }
        var step = _command is C.ScanData or C.ScanIncludingDeleted ? _parameters[3] & C.ScanStepMask : 1;
        _sector = (_sector + step) & byte.MaxValue;
        _serialIndexCount = 0;
        SearchSerial(SerialPhase.SearchId);
    }
}
