using C = RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Constants.Hd63463Constants;
using E = RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Constants.Hd63463ErrorCodeConstants;

namespace RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Services;

public sealed partial class Hd63463
{
    private int _physicalRecord;
    private int _searchTimeout;
    private long _timeoutClockPhase;
    private long TimeoutClockPeriod => (long)_clock * C.SearchTimeoutQuantumCycles;

    private bool IdentifierPresent => Image is { } image
        && _head == _parameters[C.PhysicalHeadParameter]
        && _head < image.Heads && _cylinder < image.Cylinders
        && _tracks[_unit] == _cylinder && _sector < _sectors[_unit]
        && _sector < (long)image.SectorsPerTrack * image.SectorSize / RecordBytes;

    /// <summary>Compare les adresses logiques des ID de la piste sélectionnée, sans déplacer les têtes.</summary>
    private bool LocateRecord(int records)
    {
        var image = Image!;
        if (_tracks[_unit] >= image.Cylinders || _parameters[C.PhysicalHeadParameter] >= image.Heads
            || _sector >= _sectors[_unit]) return false;
        if (image.SectorSize != RecordBytes)
        {
            _physicalRecord = _sector;
            return IdentifierPresent;
        }

        var trackStart = ((long)_tracks[_unit] * image.Heads + _parameters[C.PhysicalHeadParameter])
            * image.SectorsPerTrack;
        var found = false;
        var soonest = long.MaxValue;
        for (var record = 0; record < records; record++)
        {
            var identifier = image.ReadSectorAsync(trackStart + record).GetAwaiter().GetResult();
            if (identifier.Cylinder != _cylinder || identifier.Head != _head || identifier.Number != _sector) continue;
            var start = Gap1Bytes + (long)record * RecordSpanBytes + SyncBytes;
            var end = start + C.AddressMarkBytes + C.IdentifierBytes + E.CrcBytes;
            var angle = (long)ByteAngle(end);
            if (ByteAngle(start) <= _rotationPositions[_unit]) angle += RotationPeriod;
            var distance = angle - _rotationPositions[_unit];
            if (distance >= soonest) continue;
            soonest = distance;
            _physicalRecord = record;
            found = true;
        }
        return found;
    }

    private void AdvanceSearchClock(int ticks)
    {
        var phase = _timeoutClockPhase + (long)ticks * C.ControllerClockHz;
        _timeoutClockPhase = phase < TimeoutClockPeriod ? phase : phase % TimeoutClockPeriod;
    }

    private void StartSearchTimeout()
    {
        // Hitachi specifies a one-quantum uncertainty, not an exact microcode
        // phase: TO*80000 <= cycles <= (TO+1)*80000. A free-running prescaler
        // models that interval without coupling it to spindle speed.
        var count = _specification[C.SpecifyCylinder] >> C.SearchTimeoutShift;
        var numerator = (long)(count + 1) * TimeoutClockPeriod - _timeoutClockPhase;
        _searchTimeout = (int)Math.Clamp((numerator + C.ControllerClockHz - 1) / C.ControllerClockHz, 1, int.MaxValue);
    }
}
