using System.Buffers.Binary;
using C = RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Constants.Hd63463Constants;
using Command = RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Enums.Hd63463Command;
using Phase = RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Enums.Hd63463Phase;

namespace RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Services;

public sealed partial class Hd63463
{
    public void WriteCommand(byte value)
    {
        if (_testMode || _phase == Phase.Resetting) return;
        if ((value & C.AbortMask) == (byte)Command.Abort)
        {
            BeginAbort();
            return;
        }
        if (_polling && value == (byte)Command.DisablePolling)
        {
            DisablePolling();
            return;
        }
        if (Busy) return;
        if (value == (byte)Command.Recall)
        {
            BeginControl(Phase.Recalling, C.RecallCycles);
            return;
        }
        var length = _parameterWrite;
        _parameterRead = _parameterWrite = 0;
        _command = (Command)value;
        _status = C.ParameterReject;
        _phase = Phase.Executing;
        SetDrq(false);
        UpdateInterrupt();
        switch (_command)
        {
            case Command.Test:
                if (length != 0) { Finish(C.InvalidParameter); break; }
                if (_initialized) { Finish(C.TestAfterSpecify); break; }
                _phase = Phase.EnteringTest;
                _status |= C.Busy;
                _delay = ControllerCycles(C.TestCycles);
                break;
            case Command.Specify:
                Specify(length);
                break;
            case Command.OpenBufferRead:
            case Command.OpenBufferWrite:
            case Command.BufferToMemory:
            case Command.MemoryToBuffer:
                BeginBufferAccess(length);
                break;
            case Command.CheckEcc:
                CheckEcc(length);
                break;
            case Command.CheckDrive:
                CheckDrive(length);
                break;
            case Command.Polling:
                BeginPolling(length);
                break;
            case Command.DisablePolling:
                DisablePolling();
                break;
            case Command.Recalibrate:
            case Command.Seek:
                BeginSeek(length);
                break;
            case Command.ReadId:
            case Command.FindId:
                BeginIdentifierRead(length);
                break;
            case Command.WriteFormat:
                BeginFormat(length);
                break;
            case Command.ReadData:
            case Command.ReadErroneousData:
            case Command.WriteData:
            case Command.CheckData:
            case Command.CompareData:
                BeginTransfer(length);
                break;
            default:
                // Les autres commandes seront raccordées après confrontation au manuel complet.
                // Le contrôleur n'est pas encore enregistré dans les modèles utilisables.
                Finish(C.InvalidCommand);
                break;
        }
    }

    private int ParameterWord(int offset) => BinaryPrimitives.ReadUInt16BigEndian(_parameters.AsSpan(offset));

    private void Specify(int length)
    {
        var recordLength = _parameters[C.SpecifySectorSize] & C.SectorLengthMask;
        if (length != C.SpecifyParameterLength
            || (_parameters[C.SpecifyCylinder] >> C.SearchTimeoutShift) == 0
            || recordLength is < C.SectorLength256 or > C.MaximumSectorLength
            || (recordLength != C.SectorLength256
                && (_parameters[C.OperationModeParameter] & C.AutomaticCorrection) != 0)
            || _parameters[C.SpecifyHeads] >= C.MaximumHeads
            || _parameters[C.SpecifySectors] >= C.MaximumSectors)
        {
            Finish(C.InvalidParameter);
            return;
        }
        BeginControl(Phase.Specifying, C.SpecifyCycles);
    }

    private void CompleteSpecify()
    {
        var recordLength = _parameters[C.SpecifySectorSize] & C.SectorLengthMask;
        _initialized = true;
        _parameters.CopyTo(_specification, 0);
        _interruptMode = _specification[C.ModeParameter];
        for (var unit = 0; unit < C.UnitCount; unit++)
        {
            if ((_specification[C.SpecifyUnits] & (1 << (unit + C.UnitMaskFirstBit))) == 0) continue;
            _cylinders[unit] = ((_specification[C.SpecifyCylinder] & C.SpecifyCylinderMask) << C.ByteBits
                | _specification[C.SpecifyCylinder + 1]) + 1;
            _heads[unit] = _specification[C.SpecifyHeads] + 1;
            _sectors[unit] = _specification[C.SpecifySectors] + 1;
            _recordLengths[unit] = C.SectorBytes << (recordLength - C.SectorLength256);
            _specified[unit] = true;
        }
        _validUnits = _specification[C.SpecifyUnits];
        for (var unit = 0; unit < C.UnitCount; unit++)
            if (_seekDelays[unit] > 0) _validUnits &= (byte)~UnitBit(unit);
        _completedSeeks = 0;
        Array.Clear(_parameters);
        _status = C.ParameterReject;
        _phase = Phase.Idle;
        UpdateInterrupt();
    }

    private bool SelectDrive()
    {
        var selected = _parameters[C.DriveParameter];
        if (selected is < 1 or > C.UnitCount)
        {
            _selected = false;
            Finish(C.NotReady);
            return false;
        }
        _unit = selected - 1;
        _selected = true;
        if (!_specified[_unit]) { Finish(C.NotInitialized); return false; }
        if (Image is null) { Finish(C.NotReady); return false; }
        if (_command != Command.CheckDrive && _seekDelays[_unit] > 0) { Finish(C.InSeek); return false; }
        return true;
    }

    private void CheckDrive(int length)
    {
        if (length < C.DriveParameterLength) { Finish(C.InvalidParameter); return; }
        if (_parameters[C.DriveParameter] == 0)
        {
            _selected = false;
            Array.Clear(_parameters);
            _status = 0;
            _phase = Phase.Idle;
            return;
        }
        if (!SelectDrive()) return;
        Array.Clear(_parameters);
        _parameters[C.DriveResultSelection] = (byte)(_unit + 1);
        _parameters[C.DriveResultSignals] = (byte)(C.DriveReady
            | (_seekDelays[_unit] == 0 ? C.DriveSeekComplete : 0)
            | (_tracks[_unit] == 0 ? C.TrackZero : 0));
        _status = 0;
        _phase = Phase.Idle;
    }

    private void BeginTransfer(int length)
    {
        var physicalRead = _command == Command.ReadErroneousData;
        if (length != (physicalRead ? C.ErroneousParameterLength : C.TransferParameterLength))
        { Finish(C.InvalidParameter); return; }
        if (!SelectDrive()) return;
        if (physicalRead)
        {
            _cylinder = _tracks[_unit];
            _head = _parameters[C.PhysicalHeadParameter];
            _sector = 0; // RED selects the next physical DATA field after command preparation.
            _remaining = ParameterWord(C.ErroneousCountParameter);
        }
        else
        {
            _cylinder = ParameterWord(C.CylinderParameter);
            _head = _parameters[C.HeadParameter];
            _sector = _parameters[C.SectorParameter];
            _remaining = ParameterWord(C.CountParameter);
        }
        _phase = _command switch
        {
            Command.WriteData => Phase.Writing,
            Command.CompareData => Phase.Comparing,
            Command.CheckData => Phase.Checking,
            _ => Phase.Reading
        };
        if (!CheckAddress(requireIdentifier: false)) return;
        if (_phase == Phase.Writing && Image!.IsReadOnly) { Finish(C.WriteProtected); return; }
        _buffer = _bufferOffset = 0;
        ResetRecord();
        ResetErrorCode();
        _hostChunksRemaining = _remaining * (RecordBytes / C.SectorBytes);
        if (_remaining == 0) { Finish(); return; }
        BeginRecordCommand();
    }

    private void BeginDelay()
    {
        _status |= C.Busy;
        _activity = Math.Max(1, _clock / C.MillisecondsPerSecond * C.ActivityMilliseconds);
        var latency = Writing ? _writeLatencies[_unit] : _latencies[_unit];
        _delay = Delay(C.CommandMicroseconds + latency * C.MillisecondsPerSecond);
        UpdateInterrupt();
    }

    private void Finish(byte error = 0)
    {
        SetDrq(false);
        ResetRecord();
        _polling = false;
        _delay = 0;
        _phase = Phase.Idle;
        _status = (byte)(C.ParameterReject | C.CommandEnd | (error != 0 ? C.AbnormalEnd : 0));
        Array.Copy(_parameters, 0, _parameters, C.ResultPrefixBytes, C.TransferResultBytes);
        _parameters[0] = 0;
        _parameters[1] = error;
        _parameterRead = _parameterWrite = 0;
        UpdateInterrupt();
    }

}
