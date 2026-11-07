using System.Buffers.Binary;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;
using C = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000Constants;
using M = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000PositioningConstants;
using O = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000CommandOptionConstants;

namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

public sealed partial class Acb4000
{
    private void ExecuteCommand()
    {
        _code = (Acb4000CommandCode)_command[C.OpcodeOffset];
        Unit = _command[C.UnitOffset] >> C.LunShift;
        SelectMemoryUnit();
        var receiveDump = Unit < C.DriveCount && DiagnosticDumpReady;
        if (Unit < C.DriveCount) DiagnosticDumpReady = false;
        _readingDisk = _writingDisk = false;
        _translating = false;
        _searchingData = _receivingSearchPattern = false;
        _reinitializing = _completeAfterParameterAcquisition = false;
        _readError = 0;
        _address = _commandLength == C.ShortCdbLength
            ? (uint)((_command[C.UnitOffset] & C.ShortAddressMask) << C.AddressHighShift | _command[C.ShortAddressMiddleOffset] << C.AddressMiddleShift | _command[C.ShortAddressLowOffset])
            : BinaryPrimitives.ReadUInt32BigEndian(_command.AsSpan(C.LongAddressOffset));
        RemainingBlocks = _commandLength == C.ShortCdbLength ? _command[C.ShortCountOffset]
            : BinaryPrimitives.ReadUInt16BigEndian(_command.AsSpan(C.LongCountOffset));
        if (RemainingBlocks == 0) RemainingBlocks = _commandLength == C.ShortCdbLength
            ? C.MaximumShortBlockCount : C.MaximumLongBlockCount;
        _firstAddress = _address;
        _commandBlocks = RemainingBlocks;

        // REQUEST SENSE never returns CHECK, including a short allocation or a nonexistent LUN.
        if (_code == Acb4000CommandCode.RequestSense) { SenseReply(); return; }
        _sense[Unit] = C.NoSense;
        _senseAddress[Unit] = 0;
        _senseAddressValid[Unit] = false;
        if (Unit >= C.DriveCount) { Complete(C.InvalidUnit); return; }
        if (_driveMoving[Unit]) { CompleteStatus(C.BusyStatus); return; }
        var controlMask = ExpandedCommandsEnabled ? O.LinkedControlMask : byte.MaxValue;
        if ((_command[_commandLength - 1] & controlMask) != 0) { Complete(C.BadArgument); return; }
        if (TryExecuteCommandOption()) return;

        switch (_code)
        {
            case Acb4000CommandCode.ReceiveDiagnostic:
                // Firmware 1B34/175A: without a preceding dump, reject before disk access.
                BeginReceiveDiagnostic(receiveDump);
                return;
            case Acb4000CommandCode.SendDiagnostic:
                BeginSendDiagnostic();
                return;
            case Acb4000CommandCode.ReadBuffer:
                if (!ReservedShort()) { Complete(C.BadArgument); return; }
                Reply(C.BufferSize);
                return;
            case Acb4000CommandCode.WriteBuffer:
                if (!ReservedShort()) { Complete(C.BadArgument); return; }
                Receive(C.BufferSize);
                return;
        }

        if (Drive is not { } drive || !_driveReady[Unit]) { Complete(C.NotReady); return; }
        if (_code == Acb4000CommandCode.StartStop)
        {
            if (!ReservedShort(allocation: true) || _command[C.ShortCountOffset] > M.StartUnitMask)
            { Complete(C.BadArgument); return; }
            // START performs calibration and reacquires the physical drive table, even when cached.
            if ((_command[C.ShortCountOffset] & M.StartUnitMask) != 0)
            {
                _parametersLoaded[Unit] = false;
                _completeAfterParameterAcquisition = true;
            }
        }
        if (_code != Acb4000CommandCode.TestUnitReady && !LoadParameters(drive)) return;
        if (_code is Acb4000CommandCode.Read or Acb4000CommandCode.ReadLong
            or Acb4000CommandCode.Write or Acb4000CommandCode.WriteLong or Acb4000CommandCode.WriteAndVerify
            or Acb4000CommandCode.Verify or Acb4000CommandCode.ReadCapacity
            or Acb4000CommandCode.TranslateOrExpandedWriteBuffer or Acb4000CommandCode.SearchDataEqual)
        {
            if (!_hasFormat[Unit]) { Complete(C.BlownFormat); return; }
        }
        switch (_code)
        {
            case Acb4000CommandCode.Rezero:
                BeginRezeroCommand();
                break;
            case Acb4000CommandCode.Seek:
                BeginSeekCommand();
                break;
            case Acb4000CommandCode.StartStop:
                BeginParkingCommand();
                break;
            case Acb4000CommandCode.TranslateOrExpandedWriteBuffer:
                BeginTranslation();
                break;
            case Acb4000CommandCode.SearchDataEqual:
                BeginSearchData();
                break;
            case Acb4000CommandCode.TestUnitReady:
                Complete(!ReservedShort() ? C.BadArgument : _driveWriteFault ? C.WriteFault : C.NoSense);
                break;
            case Acb4000CommandCode.ModeSelect:
                BeginModeSelect();
                break;
            case Acb4000CommandCode.ModeSense:
                ModeSenseReply();
                break;
            case Acb4000CommandCode.Format:
                BeginFormat();
                break;
            case Acb4000CommandCode.Read:
            case Acb4000CommandCode.ReadLong:
                if (!ValidBlockCommand()) { Complete(C.BadArgument); return; }
                _readingDisk = true;
                ReadBlock();
                break;
            case Acb4000CommandCode.Write:
            case Acb4000CommandCode.WriteLong:
            case Acb4000CommandCode.WriteAndVerify:
                if (!ValidBlockCommand()) { Complete(C.BadArgument); return; }
                if (!AddressAvailable()) { Complete(C.IllegalAddress, true); return; }
                if (drive.IsReadOnly || _driveWriteFault) { Complete(C.WriteFault, true); return; }
                _writingDisk = true;
                Receive(BlockSize);
                break;
            case Acb4000CommandCode.Verify:
                if (!ValidBlockCommand()) { Complete(C.BadArgument); return; }
                SetStage(Acb4000Stage.Verify);
                break;
            case Acb4000CommandCode.ReadCapacity:
                CapacityReply();
                break;
            default:
                Complete(C.InvalidCommand);
                break;
        }
    }

    private bool ReservedShort(bool allocation = false) => (_command[C.UnitOffset] & C.ShortAddressMask) == 0
        && _command[C.ShortAddressMiddleOffset] == 0 && _command[C.ShortAddressLowOffset] == 0 && (allocation || _command[C.ShortCountOffset] == 0);

    private bool ValidBlockCommand() => _commandLength == C.ShortCdbLength
        || (_command[C.UnitOffset] & C.ShortAddressMask) == 0 && _command[C.LongReservedOffset] == 0;

    private void SenseReply()
    {
        var length = Math.Max(C.MinimumSenseLength, (int)_command[C.ShortCountOffset]);
        _buffer.AsSpan(0, length).Clear();
        _buffer[C.SenseCodeOffset] = (byte)(_sense[Unit] | (_senseAddressValid[Unit] ? C.SenseAddressValid : 0));
        _buffer[C.SenseUnitAddressOffset] = (byte)(((uint)Unit << C.LunShift) | ((_senseAddress[Unit] >> C.AddressHighShift) & C.ShortAddressMask));
        _buffer[C.SenseAddressMiddleOffset] = (byte)(_senseAddress[Unit] >> C.AddressMiddleShift);
        _buffer[C.SenseAddressLowOffset] = (byte)_senseAddress[Unit];
        _sense[Unit] = C.NoSense;
        _senseAddress[Unit] = 0;
        _senseAddressValid[Unit] = false;
        Reply(length);
    }

    private void CapacityReply()
    {
        var drive = Drive!;
        if ((_command[C.UnitOffset] & C.ShortAddressMask) != 0 || _command[C.LongReservedOffset] != 0 || _command[C.LongCountOffset] != 0 || _command[C.CapacityPmiOffset] > 1)
        { Complete(C.BadArgument); return; }
        var blocks = drive.CapacityBytes / BlockSize;
        if (blocks <= 0) { Complete(C.NotReady); return; }
        var last = blocks - 1;
        if (_command[C.CapacityPmiOffset] != 0)
        {
            if (!AddressAvailable()) { Complete(C.IllegalAddress, true); return; }
            var cylinderBlocks = (long)drive.Heads * drive.SectorsPerTrack * drive.SectorSize / BlockSize;
            if (cylinderBlocks <= 0) { Complete(C.BadArgument); return; }
            last = Math.Min(last, (_address / cylinderBlocks + 1) * cylinderBlocks);
        }
        BinaryPrimitives.WriteUInt32BigEndian(_buffer, (uint)Math.Min(last, uint.MaxValue));
        BinaryPrimitives.WriteUInt32BigEndian(_buffer.AsSpan(C.CapacityBlockSizeOffset), (uint)BlockSize);
        Reply(C.CapacityResponseLength);
    }
}
