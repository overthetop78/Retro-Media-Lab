using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Enums;
using Phase = RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Enums.Wd1770Phase;
using C = RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Constants.Wd1770Constants;

namespace RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Services;

public sealed partial class Wd1770
{
    private ushort FieldCrc(byte mark) => Wd1770Crc.Add(DoubleDensity ? C.MfmCrcInitial : C.CrcInitial, mark);
    private void ReceiveData(byte value)
    {
        if (_drq) _errors |= C.LostData;
        _data = value;
        SetInterrupts(_irq, true);
    }
    private void ReceiveByte(byte value, Wd1770ReadWordKind kind, bool validClocks)
    {
        var mark = kind == Wd1770ReadWordKind.AddressMark;
        if (_phase == Phase.ReadTrack) { ReceiveData(value); return; }
        if (_phase == Phase.SearchId)
        {
            if (!mark || value != C.IdAddressMark) return;
            _idOffset = 0;
            _crc = FieldCrc(value);
            _phase = Phase.ReadId;
            return;
        }
        if (_phase == Phase.ReadId)
        {
            if (_idOffset < C.IdBytes) _id[_idOffset] = value;
            _crc = Wd1770Crc.Add(_crc, value);
            if (!_verify && (_command & C.CommandMask) == C.ReadAddressCommand) ReceiveData(value);
            if (++_idOffset < C.IdBytes + C.CrcBytes) return;
            if (!_verify && (_command & C.CommandMask) == C.ReadAddressCommand)
            {
                _sector = _id[C.IdCylinder];
                if (_crc != 0) _errors |= C.CrcError;
                _phase = Phase.AddressTail;
                _timer = ByteClocks;
                return;
            }
            if (_id[C.IdCylinder] != _track || !_verify && _id[C.IdSector] != _sector)
            { _phase = Phase.SearchId; return; }
            if (_crc != 0)
            {
                if (_verify) Complete(C.CrcError | C.NotFound);
                else { _errors |= C.CrcError; _phase = Phase.SearchId; }
                return;
            }
            _errors &= unchecked((byte)~C.CrcError);
            if (_verify) { Complete(); return; }
            _remaining = C.SectorSizeBase << (_id[C.IdSize] & C.SectorSizeMask);
            if (CommandWrites)
            {
                _phase = Phase.WriteGap;
                _writeGapBytes = 0;
                _timer = ByteClocks;
            }
            else
            {
                _phase = Phase.SearchData;
                _searchBytes = DoubleDensity ? C.MfmDataSearchBytes : C.FmDataSearchBytes;
            }
            return;
        }
        if (_phase == Phase.SearchData)
        {
            if (mark && value is >= C.FirstDataAddressMark and <= C.LastDataAddressMark)
            {
                _crc = FieldCrc(value);
                _errors = (byte)((_errors & ~C.RecordType) | ((value & C.DeletedMarkMask) == 0 ? C.RecordType : 0));
                _phase = Phase.ReadData;
            }
            else if (--_searchBytes == 0) _phase = Phase.SearchId;
            return;
        }
        if (_phase == Phase.ReadData)
        {
            ReceiveData(value);
            _crc = Wd1770Crc.Add(_crc, value);
            if (--_remaining != 0) return;
            _phase = Phase.ReadDataCrc;
            _remaining = C.CrcBytes;
            return;
        }
        if (_phase != Phase.ReadDataCrc) return;
        if (_drq) _errors |= C.LostData;
        _crc = Wd1770Crc.Add(_crc, value);
        if (--_remaining != 0) return;
        if (_crc != 0) { Complete(C.CrcError); return; }
        FinishSector();
    }

    private void FinishSector()
    {
        _writer.SetGate(false);
        PublishDriveSignals();
        if ((_command & C.MultiSectorFlag) != 0)
        {
            _sector = unchecked((byte)(_sector + 1));
            StartSearch();
        }
        else Complete();
    }

    private void RequestWriteData()
    {
        _writeLatchFull = false;
        SetInterrupts(_irq, true);
    }

    private void AdvanceWriteGap()
    {
        _writeGapBytes++;
        if (_writeGapBytes == C.InitialWriteRequestByte) RequestWriteData();
        if (_writeGapBytes == C.InitialWriteDeadlineByte && !_writeLatchFull) { Complete(C.LostData); return; }
        if (_writeGapBytes < (DoubleDensity ? C.MfmWriteGateByte : C.FmWriteGateByte))
        { _timer = ByteClocks; return; }
        _writer.SetGate(true);
        _writeRun = DoubleDensity ? C.MfmLeadBytes : C.FmLeadBytes;
        _phase = Phase.WriteLead;
        QueueWriteWord();
        PublishDriveSignals();
    }

    private byte TakeWriteData()
    {
        if (!_writeLatchFull) { _errors |= C.LostData; return 0; }
        var data = _writeLatch;
        _writeLatchFull = false;
        SetInterrupts(_irq, false);
        return data;
    }

    private byte FormatLookAhead(byte value) => !DoubleDensity ? value : value switch
    {
        C.FormatA1 => C.SyncA1,
        C.FormatC2 => C.SyncC2,
        C.FormatCrc => (byte)(_writer.PredictCrc() >> C.BitsPerByte),
        _ => value
    };

    private void QueueWriteWord()
    {
        switch (_phase)
        {
            case Phase.WriteLead:
                if (_writeRun-- > 0)
                {
                    _writer.TryLoadData(0);
                    _writer.SetLookAhead(_writeRun == 0 && DoubleDensity ? C.SyncA1 : (byte)0);
                    return;
                }
                _phase = Phase.WriteMark;
                _writeRun = DoubleDensity ? C.MfmSyncWords : 0;
                QueueWriteWord();
                break;
            case Phase.WriteMark:
                if (_writeRun-- > 0)
                {
                    _writer.TryLoadFormat(C.FormatA1);
                    _writer.SetLookAhead(C.SyncA1);
                    return;
                }
                var mark = (_command & C.DeletedDataFlag) != 0 ? C.DeletedDataMark : C.DataAddressMark;
                _writer.TryLoadFormat(mark);
                _writer.SetLookAhead(_writeLatchFull ? _writeLatch : (byte)0);
                _phase = Phase.WriteData;
                break;
            case Phase.WriteData:
                if (_remaining-- > 0)
                {
                    _writer.TryLoadData(TakeWriteData());
                    if (_remaining != 0) RequestWriteData();
                    else _writer.SetLookAhead((byte)(_writer.PredictCrc() >> C.BitsPerByte));
                    return;
                }
                _phase = Phase.WriteCrc;
                QueueWriteWord();
                break;
            case Phase.WriteCrc:
                _writer.TryLoadFormat(C.FormatCrc);
                _writer.SetLookAhead(C.FmOrdinaryClock);
                _phase = Phase.WriteTail;
                break;
            case Phase.WriteTail:
                _writer.TryLoadData(C.FmOrdinaryClock);
                _phase = Phase.WriteFinish;
                break;
            case Phase.WriteFinish:
                _timer = CellClocks;
                break;
            case Phase.WriteTrack:
                var value = TakeWriteData();
                _writer.TryLoadFormat(value);
                _formatCrc = value == C.FormatCrc;
                _formatCrcRequested = false;
                if (!_formatCrc) RequestWriteData();
                break;
        }
    }
}
