using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Enums;
using C = RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Constants.Upd765Constants;

namespace RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Services;

public sealed partial class Upd765
{
    private byte ConsumeRead()
    {
        if (_phase != Upd765Phase.Reading || !_request) return C.OpenBus;
        var value = _buffer[_offset++];
        _readBytes[_unit]++;
        SetRequest(false);
        if (_offset == _length || _tcPin && !Formatting)
        {
            _terminalCount |= _tcPin;
            EndHostTransfer();
        }
        return value;
    }

    private void ConsumeWrite(byte value)
    {
        if (_phase != Upd765Phase.Writing || !_request) return;
        if (Scanning)
        {
            var data = _buffer[_offset++];
            _readBytes[_unit]++;
            if (data != C.ScanMask && value != C.ScanMask)
            {
                _scanEqual &= data == value;
                _scanSatisfied &= _command switch
                {
                    Upd765Command.ScanLowEqual => data <= value,
                    Upd765Command.ScanHighEqual => data >= value,
                    _ => data == value
                };
            }
        }
        else _buffer[_offset++] = value;
        SetRequest(false);
        if (_offset == _length || _tcPin && !Formatting)
        {
            _terminalCount |= _tcPin;
            EndHostTransfer();
        }
    }

    private void EndHostTransfer()
    {
        SetRequest(false);
        _phase = Upd765Phase.DataEnd;
        if (Formatting)
        {
            var lead = (_parameters[0] & C.Mfm) == 0 ? C.FormatAfterIdFmBytes : C.FormatAfterIdMfmBytes;
            var dataLength = C.MinimumSectorBytes << _parameters[C.FormatSizeParameter];
            _delay = Math.Max(1, _delay) + (lead + dataLength + C.CrcBytes - 1) * ByteTicks;
            return;
        }
        _delay = Math.Max(1, _delay) + (_buffer.Length - _offset + C.CrcBytes - 1) * ByteTicks;
    }

    private void FinishSector()
    {
        if (Formatting) { FinishFormatSector(); return; }
        if (_command == Upd765Command.ReadTrack)
        {
            if (_dataError) { _trackStatus1 |= C.DataError; _trackStatus2 |= C.DataFieldError; }
            _trackFields++;
            _record++;
            if (_terminalCount || _trackFields >= _parameters[C.EndParameter])
                FinishData((byte)(_trackStatus1 | (_terminalCount ? 0 : C.EndOfCylinder)), _trackStatus2);
            else SearchSector();
            return;
        }
        if (WriteCommand)
        {
            try
            {
                if (_image!.IsReadOnly || (_statusPins(_unit) & C.WriteProtectedPin) != 0)
                { FinishData(C.WriteProtected); return; }
                _image.WriteSector(_recordIndex, _buffer, _command == Upd765Command.WriteDeleted);
                _writtenBytes[_unit] += _buffer.Length;
            }
            catch (Exception error) when (MediaFailure(error)) { FinishData(C.NoData); return; }
        }
        if (_dataError) { FinishData(C.DataError, (byte)(C.DataFieldError | (_controlMark ? C.ControlMark : 0))); return; }
        if (_controlMark) { FinishData(detail: C.ControlMark); return; }
        if (Scanning)
        {
            if (_scanSatisfied)
            {
                FinishData(detail: _scanEqual ? C.ScanEqualHit : (byte)0);
                return;
            }
            if (_terminalCount) { FinishData(detail: C.ScanNotSatisfied); return; }
            if (_record >= _parameters[C.EndParameter])
            {
                if ((_parameters[0] & C.MultiTrack) != 0 && _head == 0)
                { _head = _logicalHead = 1; _record = C.FirstSectorNumber; }
                else { FinishData(C.EndOfCylinder, C.ScanNotSatisfied); return; }
            }
            else _record += _parameters[C.LengthParameter];
            SearchSector();
            return;
        }
        var more = AdvanceSectorAddress();
        if (_terminalCount) FinishData();
        else if (!more) FinishData(C.EndOfCylinder);
        else SearchSector();
    }

    private bool AdvanceSectorAddress()
    {
        var end = _record == _parameters[C.EndParameter];
        var nextSide = end && (_parameters[0] & C.MultiTrack) != 0 && _head == 0;
        if (!end) _record++;
        else
        {
            _record = 1;
            if (nextSide) { _head = 1; _logicalHead = 1; }
            else
            {
                _cylinder++;
                if ((_parameters[0] & C.MultiTrack) != 0) _logicalHead = 0;
            }
        }
        return !end || nextSide;
    }
}
