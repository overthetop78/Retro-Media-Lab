using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Upd765.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Enums;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Upd765.Constants.Upd765Constants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Upd765.Services;

internal sealed partial class Upd765
{
    private void BeginFormatIdentifier()
    {
        _buffer = new byte[C.IdentifierBytes];
        _length = C.IdentifierBytes;
        _offset = 0;
        _phase = Upd765Phase.Writing;
        _delay = ByteTicks;
    }

    private void FinishFormatSector()
    {
        if (_image!.IsReadOnly || (_statusPins(_unit) & C.WriteProtectedPin) != 0)
        { FinishData(C.WriteProtected); return; }
        if ((_statusPins(_unit) & C.FaultPin) != 0)
        { FinishData(status: C.Abnormal | C.EquipmentCheck); return; }
        _cylinder = _buffer[C.IdentifierCylinderOffset];
        _logicalHead = _buffer[C.IdentifierHeadOffset];
        _record = _buffer[C.IdentifierRecordOffset];
        _size = _buffer[C.IdentifierSizeOffset];
        var data = new byte[C.MinimumSectorBytes << _parameters[C.FormatSizeParameter]];
        Array.Fill(data, _parameters[C.FormatFillParameter]);
        var sector = new EmulationDiskSector(_trackFields, data, _cylinder, _logicalHead, _record, true)
        {
            HeaderIntegrityValid = true,
            DataIntegrityValid = true,
            DeletedDataMark = false,
            RecordedSizeCode = _size
        };
        if (_trackFields < _formatSectors.Count) _formatSectors[_trackFields] = sector;
        else _formatSectors.Add(sector);
        var count = _trackFields + 1;
        var last = count >= _parameters[C.FormatCountParameter];
        if (last && _formatSectors.Count > count) _formatSectors.RemoveRange(count, _formatSectors.Count - count);
        try { _image.FormatTrack(_formatCylinder, _head, _formatSectors, (int)_rate, (_parameters[0] & C.Mfm) != 0); }
        catch (Exception error) when (MediaFailure(error)) { FinishData(C.NoData); return; }
        _trackFields = count;
        _writtenBytes[_unit] += data.Length;
        _record++;
        if (last)
        {
            _phase = Upd765Phase.FormatTail;
            _delay = RevolutionTicks - _rotation[_unit];
        }
        else
        {
            _phase = Upd765Phase.FormatGap;
            var lead = (_parameters[0] & C.Mfm) == 0 ? C.FormatHeaderFmBytes : C.FormatHeaderMfmBytes;
            _delay = Math.Max(1, _parameters[C.FormatGapParameter] + lead) * ByteTicks;
        }
    }
}
