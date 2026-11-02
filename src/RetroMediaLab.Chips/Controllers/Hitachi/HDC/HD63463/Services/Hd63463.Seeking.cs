using C = RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Constants.Hd63463Constants;
using Command = RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Enums.Hd63463Command;
using Phase = RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Enums.Hd63463Phase;

namespace RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Services;

public sealed partial class Hd63463
{
    private bool ParallelSeek => (_specification[C.ModeParameter] & C.ParallelSeek) != 0;
    private static byte UnitBit(int unit) => (byte)(1 << (unit + C.UnitMaskFirstBit));

    private void BeginSeek(int length)
    {
        if (length != (_command == Command.Seek ? C.SeekParameterLength : C.DriveParameterLength))
        { Finish(C.InvalidParameter); return; }
        if (!SelectDrive()) return;
        var low = _specification[C.StepLowParameter];
        if (!ParallelSeek && low == byte.MaxValue) { Finish(C.InvalidStepRate); return; }
        _cylinder = _command == Command.Recalibrate ? 0 : ParameterWord(C.CylinderParameter);
        if (_cylinder >= _cylinders[_unit]) { Finish(C.InvalidCylinder); return; }
        var steps = _command == Command.Recalibrate ? _cylinders[_unit] + C.RecalibrateExtraPulses
            : Math.Abs(_tracks[_unit] - _cylinder);
        var lowCycles = ParallelSeek ? low switch
        {
            0 => C.DefaultParallelStepLowCycles,
            byte.MaxValue => C.FastestStepLowCycles,
            _ => low * C.ParallelStepLowMultiplier + C.ParallelStepLowOffset
        } : low == 0 ? C.DefaultNormalStepLowCycles : (low - 1) * C.NormalStepLowMultiplier + C.NormalStepLowOffset;
        var high = _specification[C.SpecifySectorSize] >> C.StepHighShift;
        var highCycles = ParallelSeek && low == byte.MaxValue ? C.FastestStepHighCycles
            : high == 0 ? C.DefaultStepHighCycles : high * C.StepHighMultiplier + C.StepHighOffset;
        var cycles = C.DirectionSetupCycles + steps * (lowCycles + highCycles);
        var speed = _speeds[_unit];
        var ticks = speed == 0 ? 1 : (int)Math.Clamp((long)ControllerCycles(cycles) * C.DefaultSpeedPercent / speed, 1, int.MaxValue);
        if (!ParallelSeek) ticks = (int)Math.Min(int.MaxValue, (long)ticks + SeekSettlingTicks(_unit));
        _validUnits &= (byte)~UnitBit(_unit);
        _completedSeeks &= (byte)~UnitBit(_unit);
        _phase = Phase.Positioning;
        _status |= C.Busy;
        _delay = ticks;
        UpdateInterrupt();
    }

    private int SeekSettlingTicks(int unit)
    {
        var speed = _speeds[unit];
        return speed == 0 ? 1 : (int)Math.Clamp((long)_clock * _latencies[unit] * C.DefaultSpeedPercent
            / C.MillisecondsPerSecond / speed, 1, int.MaxValue);
    }

    private void FinishPositioning()
    {
        if (Image is not { } image) { Finish(C.NotReady); return; }
        if (_cylinder >= image.Cylinders) { Finish(C.SeekNotComplete); return; }
        if (_tracks[_unit] != _cylinder) _seeks[_unit]++;
        _tracks[_unit] = _cylinder;
        image.Mechanics.Seek(_cylinder, 0, 0);
        Array.Clear(_parameters);
        _parameters[C.DriveResultSelection] = (byte)(_unit + 1);
        _phase = Phase.Idle;
        _status = (byte)(C.ParameterReject | (_command == Command.Seek ? C.CommandEnd : 0));
        if (ParallelSeek) _seekDelays[_unit] = SeekSettlingTicks(_unit);
        else
        {
            _validUnits |= UnitBit(_unit);
            _status |= C.SeekEnd;
        }
        _parameters[C.SeekResultUnits] = _validUnits;
        _activity = Math.Max(1, _clock / C.MillisecondsPerSecond * C.ActivityMilliseconds);
        UpdateInterrupt();
    }

    private void DriveSeekCompleted(int unit)
    {
        if (_disk(unit) is null)
        {
            _validUnits &= (byte)~UnitBit(unit);
            if (_polling) EndPolling(unit, C.NotReady);
            return;
        }
        _validUnits |= UnitBit(unit);
        _completedSeeks |= UnitBit(unit);
        if (_polling) EndPolling(unit);
    }

    private void BeginPolling(int length)
    {
        if (length != 0) { Finish(C.InvalidParameter); return; }
        if (!_initialized) { Finish(C.NotInitialized); return; }
        _polling = true;
        _selected = false;
        _phase = Phase.Polling;
        _status = C.ParameterReject | C.Busy | C.Polling;
        _delay = 0;
        for (var unit = 0; unit < C.UnitCount; unit++)
        {
            if (!_specified[unit]) continue;
            if (_disk(unit) is null) { EndPolling(unit, C.NotReady); return; }
            if ((_completedSeeks & UnitBit(unit)) != 0) { EndPolling(unit); return; }
        }
    }

    private void EndPolling(int unit, byte error = 0)
    {
        _polling = false;
        _phase = Phase.Idle;
        _delay = 0;
        _completedSeeks &= (byte)~UnitBit(unit);
        if (error != 0) _validUnits &= (byte)~UnitBit(unit);
        Array.Clear(_parameters);
        _parameters[C.DriveResultSelection] = (byte)(unit + 1);
        _parameters[C.SeekResultUnits] = _validUnits;
        _parameters[1] = error;
        _parameterRead = _parameterWrite = 0;
        _status = (byte)(C.ParameterReject | (error == 0 ? C.SeekEnd : C.DriveError | C.AbnormalEnd));
        UpdateInterrupt();
    }

    private void DisablePolling()
    {
        _polling = false;
        _phase = Phase.Idle;
        _delay = 0;
        _status = C.ParameterReject;
        UpdateInterrupt();
    }
}
