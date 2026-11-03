using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Enums;
using C = RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Constants.Upd765Constants;

namespace RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Services;

public sealed partial class Upd765
{
    private byte ConsumeRead()
    {
        if (_phase != Upd765Phase.Reading || !_request) return C.OpenBus;
        var value = _dataLatch;
        _readBytes[_unit]++;
        _hostOffset++;
        SetRequest(false);
        _terminalCount |= _tcPin;
        return value;
    }

    private void ConsumeWrite(byte value)
    {
        if (_phase != Upd765Phase.Writing || !_request) return;
        if (Scanning)
        {
            _readBytes[_unit]++;
            if (_dataLatch != C.ScanMask && value != C.ScanMask)
            {
                _scanEqual &= _dataLatch == value;
                _scanSatisfied &= _command switch
                {
                    Upd765Command.ScanLowEqual => _dataLatch <= value,
                    Upd765Command.ScanHighEqual => _dataLatch >= value,
                    _ => _dataLatch == value
                };
            }
        }
        else { _hostData = value; _hostDataReady = true; }
        _hostOffset++;
        SetRequest(false);
        if (!Formatting) _terminalCount |= _tcPin;
    }

    private void Overrun(bool finalByte = false)
    {
        var original = _model == Upd765Model.Upd765A;
        if (original && finalByte && !Scanning)
        {
            _stickyDrq = !_drives.NonDma && !_dmaSuppressed;
            SetRequest(false);
            return;
        }
        _stickyDrq = original && !_drives.NonDma && !_dmaSuppressed;
        FinishData(C.Overrun);
    }

    private void FinishSector()
    {
        if (_command == Upd765Command.ReadTrack)
        {
            if (_dataError) { _trackStatus1 |= C.DataError; _trackStatus2 |= C.DataFieldError; }
            _trackFields++;
            _record++;
            if (_terminalCount || _trackFields >= _parameters[C.EndParameter])
                FinishData((byte)(_trackStatus1 | (_terminalCount ? 0 : C.EndOfCylinder)), _trackStatus2);
            else SearchSector(NextSearchGapBytes);
            return;
        }
        if (_dataError) { FinishData(C.DataError, (byte)(C.DataFieldError | (_controlMark ? C.ControlMark : 0))); return; }
        if (_controlMark) { FinishData(detail: C.ControlMark); return; }
        if (Scanning)
        {
            if (_scanSatisfied) { FinishData(detail: _scanEqual ? C.ScanEqualHit : (byte)0); return; }
            if (_terminalCount) { FinishData(detail: C.ScanNotSatisfied); return; }
            if (_record >= _parameters[C.EndParameter])
            {
                if ((_parameters[0] & C.MultiTrack) != 0 && _head == 0)
                { _head = _logicalHead = 1; _record = C.FirstSectorNumber; }
                else { FinishData(C.EndOfCylinder, C.ScanNotSatisfied); return; }
            }
            else _record += _parameters[C.LengthParameter];
            SearchSector(NextSearchGapBytes);
            return;
        }
        var more = AdvanceSectorAddress();
        if (_terminalCount) FinishData();
        else if (!more) FinishData(C.EndOfCylinder);
        else SearchSector(NextSearchGapBytes);
    }

    private int NextSearchGapBytes => Math.Max(0,
        _parameters[C.GapParameter] - (WriteCommand ? C.WriteTailBytes : 0));

    private bool AdvanceSectorAddress()
    {
        var end = _record == _parameters[C.EndParameter];
        var multi = (_parameters[0] & C.MultiTrack) != 0;
        var nextSide = end && multi && _head == 0;
        if (!end) _record++;
        else
        {
            _record = C.FirstSectorNumber;
            if (multi) _logicalHead ^= C.LowBit;
            if (nextSide) _head = 1;
            else _cylinder++;
        }
        return !end || nextSide;
    }
}
