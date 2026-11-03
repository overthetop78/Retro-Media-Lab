using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Enums;
using C = RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Constants.Upd765Constants;

namespace RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Services;

public sealed partial class Upd765
{
    private int _syncGapCells;
    private int _indexCount;
    private int _idOffset;
    private int _crcOffset;
    private ushort _readCrc;
    private bool _fieldClocksValid;
    private bool _sawId;
    private bool _skipField;
    private byte _wrongCylinder;

    private void SearchSector(int gapBytes = 0)
    {
        _syncGapCells = gapBytes * C.EncodedCellsPerByte;
        _reader.DoubleDensity = Mfm;
        _reader.Reset();
        _writer.DoubleDensity = Mfm;
        _indexCount = _idOffset = _crcOffset = 0;
        _wrongCylinder = 0;
        _sawId = false;
        _phase = Upd765Phase.SearchId;
        _serialPhase = Formatting || _command == Upd765Command.ReadTrack && _trackFields == 0
            ? Upd765SerialPhase.AwaitIndex : Upd765SerialPhase.SearchId;
    }

    private ushort MarkCrc(byte mark)
    {
        var crc = C.CrcInitial;
        if (Mfm)
            for (var sync = 0; sync < C.MfmSyncWords; sync++) crc = Upd765Crc.Add(crc, C.SyncA1);
        return Upd765Crc.Add(crc, mark);
    }

    private void ReceiveSerialByte(byte value, Upd765ReadWordKind kind, bool validClocks)
    {
        if (_syncGapCells > 0) return;
        switch (_serialPhase)
        {
            case Upd765SerialPhase.SearchId:
                if (_indexSkipClocks > 0) return;
                if (kind != Upd765ReadWordKind.AddressMark || value != C.IdAddressMark) return;
                _sawId = true;
                _idOffset = 0;
                _readCrc = MarkCrc(value);
                _fieldClocksValid = true;
                _serialPhase = Upd765SerialPhase.Id;
                break;
            case Upd765SerialPhase.Id:
                _id[_idOffset++] = value;
                _readCrc = Upd765Crc.Add(_readCrc, value);
                _fieldClocksValid &= validClocks;
                if (_idOffset == C.IdFieldBytes) IdentifierReceived();
                break;
            case Upd765SerialPhase.SearchData:
                if (kind != Upd765ReadWordKind.AddressMark) return;
                if (value == C.IdAddressMark) { FinishData(C.MissingAddressMark, C.MissingDataMark); return; }
                if (value is not (C.DataAddressMark or C.DeletedAddressMark)) return;
                _readCrc = MarkCrc(value);
                _fieldClocksValid = true;
                _controlMark = (value == C.DeletedAddressMark) != (_command == Upd765Command.ReadDeleted);
                if (_command == Upd765Command.ReadTrack) _controlMark = false;
                _skipField = _controlMark && (_parameters[0] & C.SkipDeleted) != 0;
                _offset = _hostOffset = _crcOffset = 0;
                _scanEqual = _scanSatisfied = true;
                _length = C.MinimumSectorBytes << _id[C.IdentifierSizeOffset];
                _hostLength = !Scanning && _size == 0 && _parameters[C.LengthParameter] != 0
                    ? Math.Min((int)_parameters[C.LengthParameter], C.MinimumSectorBytes) : _length;
                _phase = Scanning ? Upd765Phase.Writing : Upd765Phase.Reading;
                _serialPhase = Upd765SerialPhase.Data;
                break;
            case Upd765SerialPhase.Data:
                if (_request) { Overrun(); return; }
                _readCrc = Upd765Crc.Add(_readCrc, value);
                _fieldClocksValid &= validClocks;
                _dataLatch = value;
                _offset++;
                if (_offset == _length) _serialPhase = Upd765SerialPhase.Crc;
                if (!_skipField && !_terminalCount && _offset <= _hostLength) SetRequest(true);
                break;
            case Upd765SerialPhase.Crc:
                if (_request)
                {
                    Overrun(finalByte: true);
                    if (!Active) return;
                }
                _readCrc = Upd765Crc.Add(_readCrc, value);
                _fieldClocksValid &= validClocks;
                if (++_crcOffset != C.CrcBytes) return;
                _dataError = !_skipField && (_readCrc != 0 || !_fieldClocksValid);
                if (_skipField)
                {
                    _controlMark = false;
                    if (AdvanceSectorAddress()) SearchSector(_parameters[C.GapParameter]);
                    else FinishData(C.EndOfCylinder);
                }
                else FinishSector();
                break;
        }
    }

    private void IdentifierReceived()
    {
        var cylinder = _id[C.IdentifierCylinderOffset];
        var head = _id[C.IdentifierHeadOffset];
        var record = _id[C.IdentifierRecordOffset];
        var size = _id[C.IdentifierSizeOffset];
        var badCrc = _readCrc != 0 || !_fieldClocksValid;
        if (_command == Upd765Command.ReadId)
        {
            _cylinder = cylinder; _logicalHead = head; _record = record; _size = size;
            FinishData(badCrc ? C.DataError : (byte)0);
            return;
        }
        var match = cylinder == _cylinder && head == _logicalHead && record == _record && size == _size;
        if (_command == Upd765Command.ReadTrack)
        {
            if (!match) _trackStatus1 |= C.NoData;
            if (badCrc) _trackStatus1 |= C.DataError;
            _cylinder = cylinder; _logicalHead = head; _record = record; _size = size;
        }
        else if (!match)
        {
            if (record == _record && head == _logicalHead && size == _size && cylinder != _cylinder)
                _wrongCylinder = (byte)(C.WrongCylinder | (cylinder == C.OpenBus ? C.BadCylinder : 0));
            _serialPhase = Upd765SerialPhase.SearchId;
            return;
        }
        else if (badCrc) { FinishData(C.DataError); return; }
        _wrongCylinder = 0;
        if (size > C.MaximumSizeCode) { FinishData(C.NoData); return; }
        _indexCount = 0;
        if (WriteCommand) BeginWriteField();
        else
        {
            _syncGapCells = (Mfm ? C.ReadBlankMfmBytes : C.ReadBlankFmBytes) * C.EncodedCellsPerByte;
            _serialPhase = Upd765SerialPhase.SearchData;
        }
    }
}
