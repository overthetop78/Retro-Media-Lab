using System.Buffers.Binary;
using RetroMediaLab.Chips.Controllers.Acorn.Winchester.Scsi.Interfaces;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;
using C = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000Constants;
using S = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000SearchDataConstants;

namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

public sealed partial class Acb4000
{
    private readonly byte[] _searchPattern = new byte[C.BufferSize];
    private bool _searchingData;
    private bool _receivingSearchPattern;
    private bool _searchInverted;
    private IScsiStorage? _searchDataDrive;

    private void BeginSearchData()
    {
        if ((_command[C.UnitOffset] & C.ShortAddressMask & ~S.InvertMask) != 0
            || _command[C.LongReservedOffset] != 0) { Complete(C.BadArgument); return; }
        if (!AddressAvailable()) { Complete(C.IllegalAddress, true); return; }
        _searchInverted = (_command[C.UnitOffset] & S.InvertMask) != 0;
        _receivingSearchPattern = false;
        _searchDataDrive = Drive;
        Receive(S.HeaderLength);
    }

    private void ContinueSearchData()
    {
        if (!ReferenceEquals(Drive, _searchDataDrive) || !_driveReady[Unit])
        { Complete(C.NotReady); return; }
        if (!_receivingSearchPattern)
        {
            var header = _buffer.AsSpan(0, S.HeaderLength);
            var recordSize = BinaryPrimitives.ReadUInt32BigEndian(header[S.RecordSizeOffset..]);
            var count = BinaryPrimitives.ReadUInt32BigEndian(header[S.RecordCountOffset..]);
            if (recordSize != 0 && recordSize != BlockSize
                || BinaryPrimitives.ReadUInt32BigEndian(header[S.FirstRecordOffset..]) != 0
                || count == 0 || count > RemainingBlocks
                || BinaryPrimitives.ReadUInt16BigEndian(header[S.ArgumentLengthOffset..]) != BlockSize + S.ArgumentOverhead
                || BinaryPrimitives.ReadUInt32BigEndian(header[S.DisplacementOffset..]) != 0
                || BinaryPrimitives.ReadUInt16BigEndian(header[S.PatternLengthOffset..]) != BlockSize)
            { Complete(C.BadArgument); return; }
            RemainingBlocks = (int)count;
            _receivingSearchPattern = true;
            Receive(BlockSize);
            return;
        }
        _buffer.AsSpan(0, BlockSize).CopyTo(_searchPattern);
        _receivingSearchPattern = false;
        _searchingData = true;
        ReadBlock();
    }

    private bool CheckSearchDataDrive()
    {
        if (ReferenceEquals(Drive, _searchDataDrive) && _driveReady[Unit]) return true;
        _searchingData = false;
        Complete(C.NotReady);
        return false;
    }

    private void SearchDataBlock(bool valid)
    {
        if (!valid)
        {
            // The SEARCH firmware retries the block three times, without READ's burst correction.
            if (++_readAttempts < S.DataReadAttempts)
            {
                _searchBudget.Begin(Drive!.SectorsPerTrack);
                _readChannel.BeginIdRead();
                return;
            }
            _searchingData = false;
            Complete(C.UncorrectableData, true);
            return;
        }
        var equal = _buffer.AsSpan(0, BlockSize).SequenceEqual(_searchPattern.AsSpan(0, BlockSize));
        if (equal != _searchInverted)
        {
            _searchingData = false;
            // 1737–173E sets valid address and Equal status; the four-byte firmware sense code is zero.
            _sense[Unit] = C.NoSense;
            _senseAddress[Unit] = _address;
            _senseAddressValid[Unit] = _address <= C.MaximumShortAddress;
            CompleteStatus(C.EqualStatus);
            return;
        }
        if (--RemainingBlocks == 0)
        {
            _searchingData = false;
            Complete();
            return;
        }
        if (AdvanceAddress()) SetStage(Acb4000Stage.SearchData);
    }
}
