using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;
using C = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000Constants;

namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

public sealed partial class Acb4000
{
    private bool AddressAvailable() => Drive is { } drive && (long)_address < drive.CapacityBytes / BlockSize;

    private void ReadBlock() => BeginPhysicalRead(verify: false);

    private void BeginPhysicalRead(bool verify)
    {
        if (!AddressAvailable()) { Complete(C.IllegalAddress, true); return; }
        try
        {
            var offset = (long)_address * BlockSize;
            BeginPositioning(offset, verify ? Acb4000PositioningPurpose.Verify : Acb4000PositioningPurpose.Read);
        }
        catch (Exception error) when (StorageFailure(error)) { Complete(C.UncorrectableData, true); }
    }

    private void StartReadChannel(bool verify)
    {
        BeginNativeReadBuffer();
        _readChannel.Configure(BlockSize);
        _searchBudget.Begin(Drive!.SectorsPerTrack);
        _readAttempts = 0;
        _matchingReadId = false;
        _verifying = verify;
        _readError = C.NoSense;
        _stage = Acb4000Stage.Read;
        if (_translating) BeginTranslationRead();
        else _searchDeadline.BeginFollowingFieldWait();
    }

    private void EndReadTransfer()
    {
        if (!_readingDisk) { Complete(); return; }
        if (_readError != C.NoSense) { Complete(_readError, true); return; }
        if (--RemainingBlocks == 0) { Complete(); return; }
        if (!AdvanceAddress()) return;
        ReadBlock();
    }

    private void EndWriteTransfer()
    {
        EndHostBufferDma();
        if (_code == Acb4000CommandCode.SearchDataEqual) { ContinueSearchData(); return; }
        if (_code == Acb4000CommandCode.SendDiagnostic) { EndSendDiagnostic(); return; }
        if (_code == Acb4000CommandCode.ModeSelect) { EndModeSelect(); return; }
        if (_code == Acb4000CommandCode.Format) { ContinueFormatDefects(); return; }
        if (!_writingDisk) { Complete(); return; }
        if (_driveWriteFault) { Complete(C.WriteFault, true); return; }
        BeginPositioning((long)_address * BlockSize, Acb4000PositioningPurpose.Write);
    }

    private void WritePositionedBlock()
    {
        _writeDrive = Drive;
        _writeChannel.Reset();
        _readChannel.Configure(BlockSize);
        _nativeReadOperation = true;
        _searchBudget.Begin(Drive!.SectorsPerTrack);
        var drive = _writeDrive!;
        var physicalBlock = (long)_address * BlockSize / drive.SectorSize;
        FormatCylinder = checked((int)(physicalBlock / drive.SectorsPerTrack / drive.Heads));
        FormatHead = (int)(physicalBlock / drive.SectorsPerTrack % drive.Heads);
        _stage = Acb4000Stage.WriteSearch;
        _matchingReadId = false;
        _searchDeadline.BeginFollowingFieldWait();
    }

    private bool CheckWriteDrive()
    {
        if (!ReferenceEquals(Drive, _writeDrive) || !_driveReady[Unit])
        { _writeChannel.Reset(); Complete(C.NotReady); return false; }
        if (_driveWriteFault || _writeDrive!.IsReadOnly)
        { _writeChannel.Reset(); Complete(C.WriteFault, true); return false; }
        return true;
    }

    private bool ClockBlockWrite()
    {
        if (!CheckWriteDrive()) return false;
        var cell = _writeChannel.ClockCell();
        if (!_writeChannel.Complete) return cell;
        _emittedBlocks[Unit]++;
        _writeDrive = null;
        if (--RemainingBlocks == 0)
        {
            if (_code == Acb4000CommandCode.WriteAndVerify)
            {
                _address = _firstAddress;
                RemainingBlocks = _commandBlocks;
                _stage = Acb4000Stage.Verify;
                return false;
            }
            Complete();
            return false;
        }
        if (!AdvanceAddress()) return false;
        if (!AddressAvailable()) { Complete(C.IllegalAddress, true); return false; }
        Receive(BlockSize);
        return false;
    }

    private void VerifyBlock() => BeginPhysicalRead(verify: true);

    private bool AdvanceAddress()
    {
        if (_address == uint.MaxValue) { Complete(C.IllegalAddress); return false; }
        _address++;
        return true;
    }
}
