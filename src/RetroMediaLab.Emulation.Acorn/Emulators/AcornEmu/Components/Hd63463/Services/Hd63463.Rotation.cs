using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Hd63463.Constants.Hd63463Constants;
using E = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Hd63463.Constants.Hd63463ErrorCodeConstants;
using Command = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Hd63463.Enums.Hd63463Command;
using Stage = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Hd63463.Enums.Hd63463RecordStage;
using Phase = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Hd63463.Enums.Hd63463Phase;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Hd63463.Services;

internal sealed partial class Hd63463
{
    // The spindle belongs to the drive: a controller RESET/Recall does not
    // restart it. Integer angular units retain fractional rotations across
    // Advance calls without iterating over individual magnetic bits.
    private readonly long[] _rotationPositions = new long[C.UnitCount];
    private readonly long[] _rotationRates = Enumerable.Repeat(
        (long)C.DefaultRotationsPerMinute * C.DefaultSpeedPercent, C.UnitCount).ToArray();
    private long RotationPeriod => (long)_clock * C.SecondsPerMinute * C.DefaultSpeedPercent;
    private int SyncBytes => _specification[C.Gap2Parameter] + C.Gap2ExtraBytes;
    private int Gap1Bytes => _specification[C.Gap1Parameter] + C.Gap1ExtraBytes;
    private int PadBytes => (_specification[C.ModeParameter] & E.AddressMarkExcluded) == 0
        ? C.IncludedAddressMarkPadBytes : C.ExcludedAddressMarkPadBytes;
    private int ErrorCodeBytes => (_specification[C.OperationModeParameter] & E.EccMode) == 0
        ? E.CrcBytes : E.FireBytes;
    private int RecordSpanBytes => SyncBytes * C.BufferCount + C.AddressMarkBytes * C.BufferCount
        + C.IdentifierBytes + E.CrcBytes + PadBytes * C.BufferCount + C.DataMarkBytes
        + RecordBytes + ErrorCodeBytes + _specification[C.Gap3Parameter] + C.Gap3ExtraBytes;
    private int PhysicalRecord => _phase == Phase.Formatting ? _sector : _physicalRecord;
    private long DataStartByte => Gap1Bytes + (long)PhysicalRecord * RecordSpanBytes
        + SyncBytes + C.AddressMarkBytes + C.IdentifierBytes + E.CrcBytes + PadBytes
        + SyncBytes + C.AddressMarkBytes + C.DataMarkBytes;

    private void AdvanceRotation(int ticks)
    {
        for (var unit = 0; unit < C.UnitCount; unit++)
        {
            var rate = _rotationRates[unit];
            if (rate == 0 || _disk(unit) is null) continue;
            if (ticks <= (long.MaxValue - _rotationPositions[unit]) / rate)
            {
                var position = _rotationPositions[unit] + ticks * rate;
                _rotationPositions[unit] = position < RotationPeriod ? position : position % RotationPeriod;
            }
            else
                _rotationPositions[unit] = (long)(((Int128)ticks * rate + _rotationPositions[unit]) % RotationPeriod);
        }
    }

    private Int128 ByteAngle(long bytes) => (Int128)bytes * C.ByteBits * _clock
        * _rotationsPerMinute[_unit] * C.DefaultSpeedPercent / C.DataBitsPerSecond;

    private bool TrackFitsRotation(int records) =>
        ByteAngle(Gap1Bytes + (long)records * RecordSpanBytes) < RotationPeriod;

    private int TicksToAngle(long angle)
    {
        if (_speeds[_unit] == 0) return 1;
        var distance = angle - _rotationPositions[_unit];
        if (distance <= 0) distance += RotationPeriod;
        var rate = (Int128)_rotationsPerMinute[_unit] * _speeds[_unit];
        return (int)Int128.Clamp(((Int128)distance + rate - 1) / rate, 1, int.MaxValue);
    }

    private int TicksToIndex() => TicksToAngle(0);
    private int TicksToByte(long bytes) => TicksToAngle((long)ByteAngle(bytes));

    private int ByteTransferTicks(int bytes)
    {
        if (_speeds[_unit] == 0) return 1;
        var numerator = (Int128)bytes * C.ByteBits * _clock * C.DefaultSpeedPercent;
        var denominator = (Int128)C.DataBitsPerSecond * _speeds[_unit];
        return (int)Int128.Clamp((numerator + denominator - 1) / denominator, 1, int.MaxValue);
    }

    private void ScheduleIdentifier()
    {
        var end = Gap1Bytes + (long)_sector * RecordSpanBytes + SyncBytes
            + C.AddressMarkBytes + C.IdentifierBytes + E.CrcBytes;
        _delay = TicksToByte(end);
    }

    private void ScheduleDataFragment()
    {
        var end = DataStartByte + _recordOffset + C.SectorBytes;
        if (_recordOffset + C.SectorBytes == RecordBytes) end += ErrorCodeBytes + PadBytes;
        _delay = TicksToByte(end);
    }

    private void ScheduleRecordSearch()
    {
        if (!CheckAddress(requireIdentifier: false)) return;
        var records = checked(Image!.SectorsPerTrack * Image.SectorSize / RecordBytes);
        StartSearchTimeout();
        if (!TrackFitsRotation(records)
            || (_command == Command.ReadErroneousData ? !IdentifierPresent : !LocateRecord(records)))
        {
            _recordStage = Stage.MissingIdentifier;
            _delay = 0;
            return;
        }
        if (_command == Command.ReadErroneousData)
        {
            var next = 0;
            for (var sector = 0; sector < records; sector++)
            {
                _physicalRecord = sector;
                if (ByteAngle(DataStartByte - C.AddressMarkBytes - C.DataMarkBytes) <= _rotationPositions[_unit]) continue;
                next = sector;
                break;
            }
            _sector = _physicalRecord = next;
            _recordStage = Stage.StartingData;
            _delay = TicksToByte(DataStartByte);
        }
        else
        {
            _recordStage = Stage.Searching;
            var start = Gap1Bytes + (long)_physicalRecord * RecordSpanBytes + SyncBytes;
            var end = start + C.AddressMarkBytes + C.IdentifierBytes + E.CrcBytes;
            var angle = (long)ByteAngle(end);
            if (ByteAngle(start) <= _rotationPositions[_unit]) angle += RotationPeriod;
            _delay = TicksToAngle(angle);
        }
    }
}
