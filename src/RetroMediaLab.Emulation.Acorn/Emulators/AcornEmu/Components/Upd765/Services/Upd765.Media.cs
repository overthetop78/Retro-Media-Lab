using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Upd765.Enums;
using System.IO;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Enums;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Upd765.Constants.Upd765Constants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Upd765.Services;

internal sealed partial class Upd765
{
    private void SearchSector()
    {
        try
        {
            var cylinder = _physicalCylinder(_unit);
            if (_trackCylinder != cylinder || _trackHead != _head)
            {
                _trackRecords = _image!.GetTrackSectors(cylinder, _head).ToArray();
                _trackSectors = _trackRecords.Select(index => _image.ReadSectorAsync(index).GetAwaiter().GetResult()).ToArray();
                _trackCylinder = cylinder;
                _trackHead = _head;
            }
            _recordIndex = -1;
            if (Formatting)
            {
                _formatSectors.AddRange(_trackSectors);
                _formatCylinder = cylinder;
                _phase = Upd765Phase.SearchId;
                _delay = RevolutionTicks - _rotation[_unit];
                return;
            }
            var slot = -1;
            if (_command == Upd765Command.ReadTrack && _trackRecords.Length > 0)
            {
                slot = _trackFields % _trackRecords.Length;
                _recordIndex = _trackRecords[slot];
            }
            else if (_command == Upd765Command.ReadId && _trackRecords.Length > 0)
            {
                slot = (int)((_rotation[_unit] * _trackRecords.Length + RevolutionTicks - 1) / RevolutionTicks)
                    % _trackRecords.Length;
                _recordIndex = _trackRecords[slot];
            }
            else
            {
                var nearest = long.MaxValue;
                for (var candidate = 0; candidate < _trackSectors.Length; candidate++)
                {
                    if (_trackSectors[candidate].Number != _record) continue;
                    var distance = AngularDelay(candidate);
                    if (distance >= nearest) continue;
                    nearest = distance;
                    slot = candidate;
                }
                if (slot >= 0) _recordIndex = _trackRecords[slot];
            }
            _recordSlot = slot;
            _phase = Upd765Phase.SearchId;
            _delay = _command == Upd765Command.ReadTrack && _trackFields == 0 && slot >= 0
                ? RevolutionTicks - _rotation[_unit]
                : slot < 0 ? RevolutionTicks * C.SearchRevolutions
                : AngularDelay(slot);
        }
        catch (Exception error) when (MediaFailure(error)) { FinishData(C.NoData); }
    }

    private void SectorFound()
    {
        if (Formatting)
        {
            _phase = Upd765Phase.FormatLead;
            _delay = ((_parameters[0] & C.Mfm) == 0 ? C.FormatLeadFmBytes : C.FormatLeadMfmBytes) * ByteTicks;
            return;
        }
        if (_recordIndex < 0)
        {
            FinishData((byte)(C.NoData | (_command is Upd765Command.ReadId or Upd765Command.ReadTrack ? C.MissingAddressMark : 0)));
            return;
        }
        try
        {
            var sector = _trackSectors[_recordSlot];
            var size = sector.RecordedSizeCode is { } recorded ? recorded : SizeCode(sector.Data.Length);
            if (_command == Upd765Command.ReadId)
            {
                if (size < 0) { FinishData(C.NoData); return; }
                _cylinder = (byte)sector.Cylinder;
                _logicalHead = (byte)sector.Head;
                _record = (byte)sector.Number;
                _size = (byte)size;
                FinishData(sector.HeaderIntegrityValid == false ? C.DataError : (byte)0);
                return;
            }
            if (_command == Upd765Command.ReadTrack)
            {
                if (sector.HeaderIntegrityValid == false) _trackStatus1 |= C.DataError;
                if (sector.Cylinder != _parameters[C.CylinderParameter] || sector.Head != _parameters[C.HeadParameter]
                    || sector.Number != (byte)(_parameters[C.RecordParameter] + _trackFields)
                    || size != _parameters[C.SizeParameter]) _trackStatus1 |= C.NoData;
                _cylinder = (byte)sector.Cylinder;
                _logicalHead = (byte)sector.Head;
                _record = (byte)sector.Number;
                _size = size < 0 ? _parameters[C.SizeParameter] : (byte)size;
            }
            else
            {
                if (sector.HeaderIntegrityValid == false) { FinishData(C.DataError); return; }
                if (sector.Cylinder != _cylinder)
                {
                    FinishData(C.NoData, (byte)(C.WrongCylinder | (sector.Cylinder == C.OpenBus ? C.BadCylinder : 0)));
                    return;
                }
                if (sector.Head != _logicalHead || sector.Number != _record || size != _size)
                { FinishData(C.NoData); return; }
                _controlMark = !WriteCommand
                    && (sector.DeletedDataMark == true) != (_command == Upd765Command.ReadDeleted);
                if (_controlMark && (_parameters[0] & C.SkipDeleted) != 0)
                {
                    _controlMark = false;
                    if (AdvanceSectorAddress()) SearchSector();
                    else FinishData(C.EndOfCylinder);
                    return;
                }
            }
            _buffer = WriteCommand ? new byte[sector.Data.Length] : sector.Data.ToArray();
            _dataError = !WriteCommand && (sector.DataIntegrityValid == false
                || sector.DataIntegrityValid is null && sector.IntegrityValid == false && sector.HeaderIntegrityValid != false);
            _length = !Scanning && _size == 0 && _parameters[C.LengthParameter] != 0
                ? Math.Min((int)_parameters[C.LengthParameter], C.MinimumSectorBytes) : _buffer.Length;
            _offset = 0;
            _scanEqual = _scanSatisfied = true;
            _phase = WriteCommand || Scanning ? Upd765Phase.Writing : Upd765Phase.Reading;
            _delay = ByteTicks;
            _image!.Mechanics.Seek(_physicalCylinder(_unit), _head, _record, _recordIndex);
        }
        catch (Exception error) when (MediaFailure(error)) { FinishData(C.NoData); }
    }

    private static int SizeCode(int length)
    {
        for (var code = 0; code <= C.MaximumSizeCode; code++)
            if (length == C.MinimumSectorBytes << code) return code;
        return -1;
    }

    private long AngularDelay(int slot) => Math.Max(1,
        (slot * RevolutionTicks / _trackRecords.Length - _rotation[_unit] + RevolutionTicks) % RevolutionTicks);

    private static bool MediaFailure(Exception error) =>
        error is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException;
}
