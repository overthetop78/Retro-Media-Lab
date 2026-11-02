using System.Buffers.Binary;
using System.IO;
using C = RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Constants.Hd63463Constants;
using Command = RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Enums.Hd63463Command;
using Phase = RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Enums.Hd63463Phase;

namespace RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Services;

public sealed partial class Hd63463
{
    private bool CheckAddress(bool requireIdentifier = true)
    {
        var physicalHead = _parameters[C.PhysicalHeadParameter];
        if (physicalHead >= _heads[_unit]) { Finish(C.InvalidHead); return false; }
        var image = Image;
        if (image is null) { Finish(C.NotReady); return false; }
        var trackBytes = (long)image.SectorsPerTrack * image.SectorSize;
        if (image.SectorSize < C.SectorBytes || image.SectorSize % C.SectorBytes != 0
            || image.Cylinders <= 0 || image.Heads <= 0 || image.SectorsPerTrack <= 0
            || trackBytes % RecordBytes != 0)
        { Finish(C.InvalidParameter); return false; }
        if (requireIdentifier && !IdentifierPresent)
        { Finish(C.Timeout); return false; }
        return true;
    }

    private long FragmentAddress(IDiskImage image) =>
        ((long)_tracks[_unit] * image.Heads + _parameters[C.PhysicalHeadParameter])
        * image.SectorsPerTrack * image.SectorSize + (long)PhysicalRecord * RecordBytes + _recordOffset;

    private void ExecuteEvent()
    {
        if (CompleteControlEvent()) return;
        if (_phase == Phase.WaitingIdentifierIndex)
        {
            _phase = Phase.ReadingIdentifiers;
            ScheduleIdentifier();
            return;
        }
        if (_phase == Phase.WaitingFormatIndex)
        {
            _phase = Phase.Formatting;
            ScheduleDataFragment();
            return;
        }
        if (_phase == Phase.FinishingFormatTrack) { Finish(); return; }
        if (_phase == Phase.EnteringTest)
        {
            _testMode = true;
            _selected = _polling = false;
            _activity = 0;
            _phase = Phase.Idle;
            _status = C.ParameterReject;
            SetDrq(false);
            UpdateInterrupt();
            return;
        }
        if (_phase == Phase.OpeningBuffer) { OpenBuffer(); return; }
        if (_phase == Phase.Positioning) { FinishPositioning(); return; }
        if (_remaining == 0) { Finish(); return; }
        if (!CheckAddress(requireIdentifier: _phase is not (Phase.Reading or Phase.Checking or Phase.Writing or Phase.Comparing))) return;
        try
        {
            if (_phase == Phase.ReadingIdentifiers) { CollectIdentifier(); return; }
            switch (_phase)
            {
                case Phase.Reading:
                case Phase.Checking:
                    if (AdvanceRecordStage()) ReadRecordFragment();
                    break;
                case Phase.Writing:
                case Phase.Comparing:
                    if (AdvanceRecordStage()) WriteRecordEvent();
                    break;
                case Phase.ReceivingFormatIdentifiers:
                    if (DmaMode) SetDrq(true);
                    else BeginFormatTrack();
                    break;
                case Phase.Formatting:
                    WriteFormatFragment();
                    break;
            }
        }
        catch (Exception error) when (MediaFailure(error))
        {
            Finish(Image?.IsReadOnly == true && Writing ? C.WriteProtected : C.DataError);
        }
    }

    private bool LoadFragment(int buffer, bool storeInBuffer)
    {
        var image = Image!;
        var offset = checked(FragmentAddress(image));
        var sector = image.ReadSectorAsync(offset / image.SectorSize).GetAwaiter().GetResult();
        if (sector.IntegrityValid == false && sector.RecordedErrorCheckCode.IsEmpty)
        { Finish(C.DataError); return false; }
        if (storeInBuffer)
            sector.Data.Span.Slice((int)(offset % image.SectorSize), C.SectorBytes).CopyTo(_buffers[buffer]);
        return _recordOffset != 0 || CheckRecordedData(sector, buffer, storeInBuffer);
    }

    private bool StoreOrCompareFragment(int buffer)
    {
        var image = Image!;
        var offset = checked(FragmentAddress(image));
        var index = offset / image.SectorSize;
        if (_phase == Phase.Comparing)
        {
            var original = image.ReadSectorAsync(index).GetAwaiter().GetResult();
            if (original.IntegrityValid == false && original.RecordedErrorCheckCode.IsEmpty)
            { Finish(C.DataError); return false; }
            if (_recordOffset == 0 && !CheckRecordedData(original, buffer, storeInBuffer: false)) return false;
            if (_readFieldStatus != 0) { Finish(_readFieldStatus); return false; }
            if (!original.Data.Span.Slice((int)(offset % image.SectorSize), C.SectorBytes).SequenceEqual(_buffers[buffer]))
            { Finish(C.DataCompareError); return false; }
        }
        else StoreFragment(_buffers[buffer]);
        return true;
    }

    private void StoreFragment(ReadOnlySpan<byte> data)
    {
        var image = Image!;
        var offset = checked(FragmentAddress(image));
        var index = offset / image.SectorSize;
        if (image.SectorSize == C.SectorBytes) image.WriteSector(index, data);
        else
        {
            var original = image.ReadSectorAsync(index).GetAwaiter().GetResult().Data.ToArray();
            data.CopyTo(original.AsSpan((int)(offset % image.SectorSize), C.SectorBytes));
            image.WriteSector(index, original);
        }
    }

    private void AdvanceDiskRecord()
    {
        _remaining--;
        if (++_sector >= _sectors[_unit])
        {
            _sector = 0;
            _head++;
            _parameters[C.PhysicalHeadParameter]++;
        }
        if (_command == Command.ReadErroneousData)
        {
            _parameters[C.PhysicalHeadParameter] = (byte)_head;
            BinaryPrimitives.WriteUInt16BigEndian(_parameters.AsSpan(C.ErroneousCountParameter), (ushort)_remaining);
        }
        else
        {
            _parameters[C.HeadParameter] = (byte)_head;
            _parameters[C.SectorParameter] = (byte)_sector;
            BinaryPrimitives.WriteUInt16BigEndian(_parameters.AsSpan(C.CountParameter), (ushort)_remaining);
        }
        _recordOffset = 0;
        _betweenRecords = true;
        _recordComplete = _remaining == 0;
        _activity = Math.Max(1, _clock / C.MillisecondsPerSecond * C.ActivityMilliseconds);
    }

    private void RecordAccessCompleted()
    {
        if (_phase is Phase.Reading or Phase.Checking) _reads[_unit]++;
        else if (_phase is Phase.Writing or Phase.Formatting) _writes[_unit]++;
        Image!.Mechanics.Seek(_tracks[_unit], _parameters[C.PhysicalHeadParameter], PhysicalRecord);
    }

    private static bool MediaFailure(Exception error) =>
        error is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or NotSupportedException;
}
