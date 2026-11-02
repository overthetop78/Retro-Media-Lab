using System.Buffers.Binary;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Hd63463.Constants.Hd63463Constants;
using Phase = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Hd63463.Enums.Hd63463Phase;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Hd63463.Services;

internal sealed partial class Hd63463
{
    private int _formatDmaBytes;
    private int _formatRecordCount;

    private void BeginFormat(int length)
    {
        if (length != C.FormatParameterLength
            || (_specification[C.OperationModeParameter] & C.NonMfmSt506Mode) != 0)
        { Finish(C.InvalidParameter); return; }
        if (!SelectDrive()) return;
        _cylinder = _tracks[_unit];
        _head = _parameters[C.PhysicalHeadParameter];
        _sector = 0;
        _remaining = ParameterWord(C.FormatCountParameter);
        if (_remaining > C.MaximumTrackIdentifiers) { Finish(C.InvalidParameter); return; }
        if (!CheckAddress()) return;
        if (Image!.IsReadOnly) { Finish(C.WriteProtected); return; }
        if (_remaining == 0) { Finish(); return; }
        // A flat sector image cannot preserve a changed number of IDs or an
        // arbitrary physical/logical ID layout. Never silently discard them.
        var records = (long)Image.SectorsPerTrack * Image.SectorSize / RecordBytes;
        if (_remaining != records || _remaining != _sectors[_unit]) { Finish(C.DataError); return; }
        _formatRecordCount = _remaining;
        if (!TrackFitsRotation(_formatRecordCount)) { Finish(C.DataError); return; }
        _formatDmaBytes = 0;
        _buffer = _bufferOffset = 0;
        ResetRecord();
        ResetErrorCode();
        _phase = Phase.ReceivingFormatIdentifiers;
        BeginDelay();
    }

    private void WriteFormatIdentifierWord(ushort value, bool done)
    {
        var buffer = _formatDmaBytes / C.SectorBytes;
        var offset = _formatDmaBytes % C.SectorBytes;
        BinaryPrimitives.WriteUInt16LittleEndian(_buffers[buffer].AsSpan(offset), value);
        _formatDmaBytes += C.ParameterWordBytes;
        if (done || _formatDmaBytes == C.BufferCount * C.SectorBytes) BeginFormatTrack();
    }

    private void BeginFormatTrack()
    {
        SetDrq(false);
        for (var sector = 0; sector < _formatRecordCount; sector++)
        {
            var offset = sector * C.IdentifierBytes;
            var identifier = _buffers[offset / C.SectorBytes].AsSpan(offset % C.SectorBytes, C.IdentifierBytes);
            if (BinaryPrimitives.ReadUInt16BigEndian(identifier) != _cylinder
                || identifier[C.ParameterWordBytes] != _head
                || identifier[C.ParameterWordBytes + 1] != sector)
            { Finish(C.DataError); return; }
        }
        _phase = Phase.WaitingFormatIndex;
        _delay = TicksToIndex();
    }

    private void WriteFormatFragment()
    {
        Span<byte> data = stackalloc byte[C.SectorBytes];
        data.Fill(C.FormatDataPattern);
        StoreFragment(data);
        _recordOffset += C.SectorBytes;
        if (_recordOffset == RecordBytes)
        {
            RecordAccessCompleted();
            _recordOffset = 0;
            _sector++;
            _remaining--;
            BinaryPrimitives.WriteUInt16BigEndian(_parameters.AsSpan(C.FormatCountParameter), (ushort)_remaining);
            if (_remaining == 0)
            {
                _phase = Phase.FinishingFormatTrack;
                _delay = (int)Math.Min(int.MaxValue, (long)TicksToIndex() + ByteTransferTicks(C.FormatTrailingBytes));
                return;
            }
        }
        ScheduleDataFragment();
    }
}
