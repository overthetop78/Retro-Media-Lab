using System.Buffers.Binary;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Hd63463.Constants.Hd63463Constants;
using Phase = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Hd63463.Enums.Hd63463Phase;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Hd63463.Services;

internal sealed partial class Hd63463
{
    private int _identifierIndex;
    private int _identifierDmaBytes;
    private int _identifierSectors;

    private void BeginIdentifierRead(int length)
    {
        if (length != C.IdentifierParameterLength || _parameters[C.CylinderParameter] != 0
            || _parameters[C.HeadParameter] != 0 || _parameters[C.IdentifierCountParameter] > C.MaximumTrackIdentifiers)
        { Finish(C.InvalidParameter); return; }
        if (!SelectDrive()) return;
        _cylinder = _tracks[_unit];
        _head = _parameters[C.PhysicalHeadParameter];
        _sector = 0;
        if (!CheckAddress()) return;
        var image = Image!;
        _identifierSectors = checked(image.SectorsPerTrack * image.SectorSize / RecordBytes);
        if (!TrackFitsRotation(_identifierSectors)) { Finish(C.Timeout); return; }
        _sector = _parameters[C.IdentifierOffsetParameter] % _identifierSectors;
        _remaining = _parameters[C.IdentifierCountParameter];
        _identifierIndex = _identifierDmaBytes = 0;
        if (_remaining == 0) { Finish(); return; }
        _phase = Phase.WaitingIdentifierIndex;
        StartSearchTimeout();
        _status |= C.Busy;
        _delay = TicksToIndex();
        UpdateInterrupt();
    }

    private void CollectIdentifier()
    {
        if (Image is not { } image) { Finish(C.NotReady); return; }
        // A sector container can retain recorded identifiers. Block images,
        // split records and combined records expose only canonical geometry.
        // DATA integrity is deliberately not interpreted as an ID CRC error.
        var cylinder = _cylinder;
        var head = _head;
        var number = _sector;
        if (image.SectorSize == RecordBytes)
        {
            var index = ((long)_tracks[_unit] * image.Heads + _parameters[C.PhysicalHeadParameter])
                * image.SectorsPerTrack + _sector;
            var recorded = image.ReadSectorAsync(index).GetAwaiter().GetResult();
            cylinder = recorded.Cylinder;
            head = recorded.Head;
            number = recorded.Number;
            if (cylinder is < ushort.MinValue or > ushort.MaxValue
                || head is < byte.MinValue or > byte.MaxValue
                || number is < byte.MinValue or > byte.MaxValue)
            { Finish(C.DataError); return; }
        }
        var offset = _identifierIndex * C.IdentifierBytes;
        var identifier = _buffers[offset / C.SectorBytes].AsSpan(offset % C.SectorBytes, C.IdentifierBytes);
        BinaryPrimitives.WriteUInt16BigEndian(identifier, (ushort)cylinder);
        identifier[C.ParameterWordBytes] = (byte)head;
        identifier[C.ParameterWordBytes + 1] = (byte)number;
        image.Mechanics.Seek(_cylinder, _head, _sector);
        _identifierIndex++;
        _searchTimeout = 0;
        _remaining--;
        _parameters[C.IdentifierCountParameter] = (byte)_remaining;
        _sector = (_sector + 1) % _identifierSectors;
        if (_remaining != 0)
        {
            StartSearchTimeout();
            ScheduleIdentifier();
            return;
        }
        if (!DmaMode) { Finish(); return; }
        _phase = Phase.ReadIdentifierBuffer;
        SetDrq(true);
    }

    private ushort ReadIdentifierWord()
    {
        var buffer = _identifierDmaBytes / C.SectorBytes;
        var offset = _identifierDmaBytes % C.SectorBytes;
        var value = BinaryPrimitives.ReadUInt16LittleEndian(_buffers[buffer].AsSpan(offset));
        _identifierDmaBytes += C.ParameterWordBytes;
        if (_identifierDmaBytes == C.BufferCount * C.SectorBytes) Finish();
        return value;
    }
}
