using Phase = RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Enums.Intel8271Phase;
using SerialPhase = RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Enums.Intel8271SerialPhase;
using WritePhase = RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Enums.Intel8271WritePhase;
using C = RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Constants.Intel8271Constants;

namespace RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Services;

public sealed partial class Intel8271
{
    private WritePhase _writePhase;
    private int _writeGapBits;
    private int _writeRun;
    private ushort _writeWord;
    private int _writeHalfBit;
    private int _writeHalfDelay;
    private long _writeFraction;
    private int _writeCrc;
    private int _writeFieldOffset;
    private byte _writeLatch;
    private bool _writeLatchFull;
    private bool _writeData;
    private readonly int _writeGateDelayCycles;
    private int _writePulseAge;
    private bool _writeStopping;
    private bool _writeFinishRequested;
    private bool _writeFinishingFormat;

    public bool WriteData => _writeData;
    /// <summary>Fronts numériques de Write Data, y compris plusieurs fronts pendant un Tick du propriétaire plus lent.</summary>
    public event Action<bool>? WriteDataChanged;

    private void SetWriteData(bool value)
    {
        if (_writeData == value) return;
        _writeData = value;
        WriteDataChanged?.Invoke(value);
    }

    private void ResetSerialWrite()
    {
        _writePhase = WritePhase.Inactive;
        _writeLatchFull = false;
        _writeHalfBit = C.FmBitsPerWord;
        _writeHalfDelay = _writeGapBits = _writeRun = _writeFieldOffset = 0;
        _writeFraction = 0;
        _writePulseAge = -1;
        _writeStopping = _writeFinishRequested = _writeFinishingFormat = false;
        SetWriteData(false);
    }

    private void StartSerialFormat()
    {
        ResetSerialRead();
        _phase = Phase.Format;
        _serialPhase = SerialPhase.WaitFormatIndex;
        _writePhase = WritePhase.WaitIndex;
        _buffer = [];
    }

    private void BeginFormatAtIndex()
    {
        _serialPhase = SerialPhase.Writing;
        _driveOutput |= C.OutputWriteEnable;
        _writePhase = _parameters[3] == 0 ? WritePhase.Gap1 : WritePhase.Gap5;
        _writeRun = _parameters[3] == 0 ? _parameters[4] : _parameters[3];
        _writeHalfBit = C.FmBitsPerWord;
        _writeHalfDelay = _writeGateDelayCycles;
        _writeFraction = 0;
    }

    private void FormatEndIndex()
    {
        // Le formatage ne coupe pas un secteur long au deuxième index ; seul GAP4 attend l’index final.
        if (_command != C.Format || _writePhase != WritePhase.Gap4) return;
        _writeFinishRequested = _writeFinishingFormat = true;
        if (_writeData) _writeStopping = true;
    }

    private void BeginSectorWrite()
    {
        _serialPhase = SerialPhase.Writing;
        _phase = Phase.Write;
        _driveOutput |= C.OutputWriteEnable;
        _writePhase = WritePhase.DataSync;
        _writeRun = C.FmSyncBytes;
        _writeHalfBit = C.FmBitsPerWord;
        _writeHalfDelay = _writeGateDelayCycles;
        _writePulseAge = -1;
        _writeFraction = 0;
    }

    private void CaptureSerialWrite(byte value)
    {
        if (_serialPhase != SerialPhase.Writing) return;
        _writeLatch = value;
        _writeLatchFull = true;
        _writtenBytes[_drive]++;
    }

    private void RequestSerialWrite() => SerialRequest(0);

    private bool ConsumeSerialWrite(out byte value)
    {
        value = _writeLatch;
        if (_awaitingData || !_writeLatchFull) { Complete(C.LateDma); return false; }
        _writeLatchFull = false;
        return true;
    }

    private void TickSerialWrite()
    {
        _writeFraction += _chipClock;
        while (_writeFraction >= _clock && _serialPhase == SerialPhase.Writing)
        {
            _writeFraction -= _clock;
            if (_writePulseAge >= 0) _writePulseAge++;
            SetWriteData(false);
            if (_writeStopping)
            {
                if (_writePulseAge >= _writeGateDelayCycles) FinishSerialWrite();
                continue;
            }
            if (_writeHalfDelay > 0 && --_writeHalfDelay > 0) continue;
            if (_writeHalfBit == C.FmBitsPerWord)
            {
                if (!LoadSerialWriteWord()) return;
                _writeHalfBit = 0;
            }
            var bit = (_writeWord & (1 << (C.FmBitsPerWord - 1 - _writeHalfBit))) != 0;
            SetWriteData(bit);
            if (bit)
            {
                _writePulseAge = 0;
                if (_writeFinishRequested) _writeStopping = true;
            }
            _writeHalfBit++;
            _writeHalfDelay = C.ChipCyclesPerHalfBit;
            if (_writeHalfBit == C.FmBitsPerWord && _writePhase == WritePhase.FinishSector) _writeStopping = true;
        }
    }

    private void FinishSerialWrite()
    {
        _writeStopping = _writeFinishRequested = false;
        if (_writeFinishingFormat)
        {
            Complete(0);
            return;
        }
        _driveOutput &= unchecked((byte)~C.OutputWriteEnable);
        _writePhase = WritePhase.Inactive;
        _serialClockStarted = _clockPulse = _dataPulse = false;
        _serialFraction = 0;
        SerialNextSector();
    }

    private static ushort EncodeFmWord(byte value, byte clocks)
    {
        ushort word = 0;
        for (var bit = C.BitsPerByte - 1; bit >= 0; bit--)
            word = (ushort)((word << 2) | (((clocks >> bit) & 1) << 1) | ((value >> bit) & 1));
        return word;
    }

    private bool QueueWriteWord(byte value, byte clocks = C.NormalClocks)
    {
        _writeWord = EncodeFmWord(value, clocks);
        return true;
    }

    private bool QueueCrcData(byte value)
    {
        _writeCrc = CrcByte(_writeCrc, value);
        return QueueWriteWord(value);
    }

    private void NextWriteRun(WritePhase phase, int count)
    {
        _writePhase = phase;
        _writeRun = count;
    }

    private bool LoadSerialWriteWord()
    {
        while (_serialPhase == SerialPhase.Writing)
        {
            switch (_writePhase)
            {
                case WritePhase.Gap5:
                    if (_writeRun-- > 0) return QueueWriteWord(C.GapFill);
                    NextWriteRun(WritePhase.IndexSync, C.FmSyncBytes);
                    break;
                case WritePhase.IndexSync:
                    if (_writeRun-- > 0) return QueueWriteWord(0);
                    _writePhase = WritePhase.IndexMark;
                    break;
                case WritePhase.IndexMark:
                    NextWriteRun(WritePhase.Gap1, _parameters[4]);
                    return QueueWriteWord(C.IndexAddressMark, C.IndexMarkClocks);
                case WritePhase.Gap1:
                case WritePhase.Gap3:
                    if (_writeRun-- > 0) return QueueWriteWord(C.GapFill);
                    NextWriteRun(WritePhase.IdSync, C.FmSyncBytes);
                    break;
                case WritePhase.IdSync:
                    if (_writeRun-- > 0) return QueueWriteWord(0);
                    _writePhase = WritePhase.IdMark;
                    break;
                case WritePhase.IdMark:
                    _writeCrc = CrcByte(C.CrcInitial, C.IdAddressMark);
                    _writeFieldOffset = 0;
                    _writePhase = WritePhase.Header;
                    RequestSerialWrite();
                    return QueueWriteWord(C.IdAddressMark, C.AddressMarkClocks);
                case WritePhase.Header:
                    if (!ConsumeSerialWrite(out var header)) return false;
                    if (++_writeFieldOffset == C.FormatIdSize) _writePhase = WritePhase.HeaderCrcHigh;
                    else RequestSerialWrite();
                    return QueueCrcData(header);
                case WritePhase.HeaderCrcHigh:
                    _writePhase = WritePhase.HeaderCrcLow;
                    return QueueWriteWord((byte)(_writeCrc >> C.BitsPerByte));
                case WritePhase.HeaderCrcLow:
                    NextWriteRun(WritePhase.Gap2, C.Gap2Bytes);
                    return QueueWriteWord((byte)_writeCrc);
                case WritePhase.Gap2:
                    if (_writeRun-- > 0) return QueueWriteWord(C.GapFill);
                    NextWriteRun(WritePhase.DataSync, C.FmSyncBytes);
                    break;
                case WritePhase.DataSync:
                    if (_writeRun-- > 0) return QueueWriteWord(0);
                    _writePhase = WritePhase.DataMark;
                    break;
                case WritePhase.DataMark:
                    var mark = _command is C.WriteDeletedSingle or C.WriteDeletedMultiple ? C.DeletedDataMark : C.NormalDataMark;
                    _writeCrc = CrcByte(C.CrcInitial, mark);
                    _writeFieldOffset = 0;
                    _writePhase = WritePhase.Payload;
                    if (_command != C.Format) RequestSerialWrite();
                    return QueueWriteWord(mark, C.AddressMarkClocks);
                case WritePhase.Payload:
                    var data = C.FormatFill;
                    if (_command != C.Format && !ConsumeSerialWrite(out data)) return false;
                    if (++_writeFieldOffset == _sectorSize) _writePhase = WritePhase.DataCrcHigh;
                    else if (_command != C.Format) RequestSerialWrite();
                    return QueueCrcData(data);
                case WritePhase.DataCrcHigh:
                    _writePhase = WritePhase.DataCrcLow;
                    return QueueWriteWord((byte)(_writeCrc >> C.BitsPerByte));
                case WritePhase.DataCrcLow:
                    if (_command == C.Format)
                    {
                        if (--_remaining == 0) _writePhase = WritePhase.Gap4Start;
                        else NextWriteRun(WritePhase.Gap3, _parameters[1]);
                    }
                    else _writePhase = WritePhase.Postamble;
                    return QueueWriteWord((byte)_writeCrc);
                case WritePhase.Postamble:
                    _writePhase = WritePhase.FinishSector;
                    return QueueWriteWord(C.GapFill);
                case WritePhase.FinishSector:
                    FinishSerialWrite();
                    return false;
                case WritePhase.Gap4Start:
                    _writePhase = WritePhase.Gap4;
                    return QueueWriteWord(C.GapFill);
                case WritePhase.Gap4:
                    return QueueWriteWord(C.GapFill);
                default:
                    return false;
            }
        }
        return false;
    }
}
