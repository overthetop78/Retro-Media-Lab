using System.Buffers.Binary;
using C = RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Constants.Hd63463Constants;
using Phase = RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Enums.Hd63463Phase;
using Stage = RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Enums.Hd63463RecordStage;

namespace RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Services;

/// <summary>Le champ DATA traverse deux RAM de 256 octets, indépendamment de sa longueur RL.</summary>
public sealed partial class Hd63463
{
    private readonly bool[] _readyBuffers = new bool[C.BufferCount];
    private int _recordOffset;
    private int _diskBuffer;
    private int _hostChunksRemaining;
    private bool _recordComplete;
    private bool _recordDmaDone;
    private bool _betweenRecords;
    private bool _writeReady;
    private bool _waitingForReadBuffer;
    private Stage _recordStage;
    private int RecordBytes => _recordLengths[_unit];
    private bool HasBufferedData => _readyBuffers[0] || _readyBuffers[1];
    private bool DmaRequestPending => Busy && (_phase switch
    {
        Phase.Reading => !_recordDmaDone && _readyBuffers[_buffer],
        Phase.Writing or Phase.Comparing => _writeReady && !_recordDmaDone
            && _hostChunksRemaining > 0 && !_readyBuffers[_buffer],
        Phase.ReadBuffer or Phase.WriteBuffer or Phase.ReadIdentifierBuffer => true,
        Phase.ReceivingFormatIdentifiers => true,
        _ => false
    });

    private void ResetRecord()
    {
        _physicalRecord = 0;
        _recordOffset = _hostChunksRemaining = 0;
        _diskBuffer = _buffer;
        _recordComplete = _recordDmaDone = _betweenRecords = _writeReady = _waitingForReadBuffer = false;
        _recordStage = Stage.Idle;
        _searchTimeout = 0;
        _readFieldStatus = 0;
        Array.Clear(_readyBuffers);
    }

    private void BeginRecordCommand()
    {
        _recordStage = Stage.Preparing;
        _status |= C.Busy;
        _activity = Math.Max(1, _clock / C.MillisecondsPerSecond * C.ActivityMilliseconds);
        var latency = Writing ? _writeLatencies[_unit] : _latencies[_unit];
        _delay = Delay(latency * C.MillisecondsPerSecond);
        UpdateInterrupt();
    }

    private bool AdvanceRecordStage()
    {
        switch (_recordStage)
        {
            case Stage.Preparing:
                if (_phase is Phase.Writing or Phase.Comparing)
                {
                    _writeReady = true;
                    if (DmaMode) SetDrq(DmaRequestPending);
                }
                ScheduleRecordSearch();
                return false;
            case Stage.Searching:
                _searchTimeout = 0;
                _recordStage = Stage.StartingData;
                _delay = TicksToByte(DataStartByte);
                return false;
            case Stage.StartingData:
                var reading = _phase == Phase.Reading;
                var writing = _phase is Phase.Writing or Phase.Comparing;
                if (DmaMode && !_recordDmaDone
                    && ((reading && _readyBuffers[_diskBuffer]) || (writing && !_readyBuffers[_diskBuffer])))
                {
                    _waitingForReadBuffer = reading;
                    ScheduleRecordSearch();
                    if (_speeds[_unit] == 0) _delay = _searchTimeout = 0;
                    return false;
                }
                _waitingForReadBuffer = _betweenRecords = false;
                _searchTimeout = 0;
                _recordStage = Stage.Data;
                ScheduleDataFragment();
                return false;
            case Stage.Data:
                return true;
            default:
                return false;
        }
    }

    private void ReadRecordFragment()
    {
        var buffered = _phase == Phase.Reading;
        var dma = buffered && DmaMode && !_recordDmaDone;
        if (dma && _readyBuffers[_diskBuffer])
        {
            Finish(C.DataOverrun);
            return;
        }
        _waitingForReadBuffer = _betweenRecords = false;
        if (!LoadFragment(_diskBuffer, buffered)) return;
        if (dma) _readyBuffers[_diskBuffer] = true;
        _diskBuffer = (_diskBuffer + 1) % C.BufferCount;
        _recordOffset += C.SectorBytes;
        if (_recordOffset == RecordBytes)
        {
            RecordAccessCompleted();
            // An abnormal DATA field remains the current CHS in the result.
            // Its payload (corrected automatically or left for CKE) still drains
            // through DMA before the command reports its abnormal completion.
            if (_readFieldStatus != 0) _recordComplete = true;
            else AdvanceDiskRecord();
            if (_recordDmaDone || (_recordComplete && (!dma || !HasBufferedData)))
            { Finish(_readFieldStatus); return; }
            if (!_recordComplete) ScheduleRecordSearch();
        }
        else ScheduleDataFragment();
        if (!dma) _buffer = _diskBuffer;
        else SetDrq(_readyBuffers[_buffer]);
    }

    private ushort ReadRecordWord(bool done)
    {
        var value = BinaryPrimitives.ReadUInt16LittleEndian(_buffers[_buffer].AsSpan(_bufferOffset));
        _bufferOffset += C.ParameterWordBytes;
        if (_bufferOffset == C.SectorBytes)
        {
            SetDrq(false);
            _readyBuffers[_buffer] = false;
            _buffer = (_buffer + 1) % C.BufferCount;
            _bufferOffset = 0;
            if (_waitingForReadBuffer && _delay == 0) ScheduleRecordSearch();
        }
        if (done) StopReadDma();
        else if (_recordComplete && !HasBufferedData) Finish(_readFieldStatus);
        else SetDrq(_readyBuffers[_buffer]);
        return value;
    }

    private void StopReadDma()
    {
        _recordDmaDone = true;
        Array.Clear(_readyBuffers);
        SetDrq(false);
        // DONE ends the host transfer, not a DATA field still arriving from disk.
        if (_recordComplete || _betweenRecords) Finish(_readFieldStatus);
    }

    private void QueueWriteBuffer(bool done)
    {
        SetDrq(false);
        _readyBuffers[_buffer] = true;
        _hostChunksRemaining--;
        _buffer = (_buffer + 1) % C.BufferCount;
        _bufferOffset = 0;
        if (done)
        {
            _recordDmaDone = true;
            _hostChunksRemaining = 0;
            SetDrq(false);
            if (_betweenRecords) { Finish(); return; }
        }
        else SetDrq(DmaRequestPending);
        if (_writeReady && _delay == 0) ScheduleRecordSearch();
    }

    private void WriteRecordEvent()
    {
        if (DmaMode && !_recordDmaDone && !_readyBuffers[_diskBuffer])
        {
            Finish(C.DataOverrun);
            return;
        }
        _betweenRecords = false;
        if (!StoreOrCompareFragment(_diskBuffer)) return;
        _readyBuffers[_diskBuffer] = false;
        _diskBuffer = (_diskBuffer + 1) % C.BufferCount;
        _recordOffset += C.SectorBytes;
        if (_recordOffset == RecordBytes)
        {
            RecordAccessCompleted();
            AdvanceDiskRecord();
            if (_recordComplete || _recordDmaDone) { Finish(); return; }
            ScheduleRecordSearch();
        }
        else ScheduleDataFragment();
        if (!DmaMode) _buffer = _diskBuffer;
        else SetDrq(DmaRequestPending);
    }
}
