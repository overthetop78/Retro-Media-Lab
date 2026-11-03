using Phase = RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Enums.Wd1770Phase;
using C = RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Constants.Wd1770Constants;
using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Enums;

namespace RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Services;

public sealed partial class Wd1770
{
    private void Command(byte value)
    {
        if (_reset) return;
        if ((value & C.CommandMask) == C.ForceInterruptCommand)
        {
            var wasActive = Active;
            _phase = Phase.Idle;
            _timer = 0;
            _writer.SetGate(false);
            _step = _drq = false;
            if (!wasActive)
            {
                _typeOne = true;
                _errors = 0;
                if (_model == Wd1770Model.Wd1772) _motor = true;
            }
            _idleIndexes = 0;
            _forceIndex = (value & C.InterruptOnIndex) != 0;
            if ((value & C.ForceConditionMask) == 0) _forceImmediate = false;
            else if ((value & C.InterruptImmediate) != 0) _forceImmediate = true;
            _irq = _forceImmediate;
            DriveSignalsChanged?.Invoke();
            return;
        }
        if (Active) return;
        _command = value;
        _typeOne = value < C.FirstDataCommand;
        _errors = 0;
        _irq = _forceImmediate;
        _drq = _forceIndex = _verify = false;
        _timer = 0;
        _idleIndexes = _searchIndexes = 0;
        _reader.Reset();
        _writer.Reset();
        _writer.PrecompensationEnabled = (value & C.DisablePrecompensation) == 0;
        if (!_motor)
        {
            _motor = true;
            _spinComplete = false;
            _motorIndexes = 0;
            if ((value & C.DisableSpinUp) == 0)
            {
                _spinIndexes = 0;
                _phase = Phase.SpinUp;
                DriveSignalsChanged?.Invoke();
                return;
            }
        }
        Dispatch();
        DriveSignalsChanged?.Invoke();
    }

    private void Dispatch()
    {
        if (_typeOne)
        {
            var kind = _command & C.CommandMask;
            _seekTarget = _data;
            _restoreSteps = 0;
            if (kind == C.RestoreCommand)
            {
                _track = byte.MaxValue;
                _data = 0;
                _seekTarget = 0;
                _direction = false;
            }
            else if (kind == C.SeekCommand) _direction = _seekTarget > _track;
            else if ((_command & C.CommandGroupMask) == C.StepInCommand) _direction = true;
            else if ((_command & C.CommandGroupMask) == C.StepOutCommand) _direction = false;
            if (kind == C.RestoreCommand && _trackZero || kind == C.SeekCommand && _track == _seekTarget)
            {
                if (kind == C.RestoreCommand) _track = 0;
                Positioned();
                return;
            }
            _phase = Phase.DirectionSetup;
            _timer = (DoubleDensity ? C.MfmDirectionClocks : C.FmDirectionClocks);
            return;
        }
        if ((_command & C.SettleFlag) != 0)
        {
            _phase = Phase.Settle;
            _timer = Milliseconds(_model == Wd1770Model.Wd1772 ? C.Wd1772SettleMilliseconds : C.Wd1770SettleMilliseconds);
        }
        else BeginDataCommand();
    }

    private void EmitStep()
    {
        if (!_direction && _trackZero)
        {
            _track = 0;
            Positioned();
            return;
        }
        _step = true;
        StepPulses++;
        _restoreSteps++;
        if ((_command & C.CommandMask) is C.RestoreCommand or C.SeekCommand || (_command & C.UpdateTrackFlag) != 0)
            _track = unchecked((byte)(_track + (_direction ? 1 : -1)));
        _phase = Phase.StepPulse;
        _timer = DoubleDensity ? C.MfmStepPulseClocks : C.FmStepPulseClocks;
        DriveSignalsChanged?.Invoke();
    }

    private void AfterStepDelay()
    {
        var kind = _command & C.CommandMask;
        if (kind == C.RestoreCommand)
        {
            if (_trackZero) { _track = 0; Positioned(); return; }
            if (_restoreSteps >= C.MaximumRestoreSteps)
            {
                Complete((_command & C.VerifyFlag) != 0 ? C.NotFound : (byte)0);
                return;
            }
        }
        else if (kind != C.SeekCommand || _track == _seekTarget) { Positioned(); return; }
        EmitStep();
    }

    private void Positioned()
    {
        if ((_command & C.VerifyFlag) == 0) { Complete(); return; }
        _verify = true;
        _phase = Phase.Settle;
        _timer = Milliseconds(C.VerifySettleMilliseconds);
    }

    private void BeginDataCommand()
    {
        if (CommandWrites && _writeProtected) { Complete(C.WriteProtected); return; }
        var command = _command & C.CommandMask;
        if (command == C.ReadTrackCommand) { _phase = Phase.WaitReadTrack; return; }
        if (command == C.WriteTrackCommand)
        {
            _phase = Phase.WriteTrackPrime;
            RequestWriteData();
            _timer = ByteClocks * C.WriteTrackPrimeBytes;
            return;
        }
        StartSearch();
    }

    private void StartSearch()
    {
        _phase = Phase.SearchId;
        _searchIndexes = 0;
        _reader.Reset();
    }

    private void TimerFinished()
    {
        switch (_phase)
        {
            case Phase.DirectionSetup: EmitStep(); break;
            case Phase.StepPulse:
                _step = false;
                DriveSignalsChanged?.Invoke();
                _phase = Phase.StepDelay;
                _timer = Math.Max(1, Milliseconds(_stepMilliseconds[_command & C.StepRateMask])
                    - (DoubleDensity ? C.MfmStepPulseClocks : C.FmStepPulseClocks));
                break;
            case Phase.StepDelay: AfterStepDelay(); break;
            case Phase.Settle: if (_verify) StartSearch(); else BeginDataCommand(); break;
            case Phase.AddressTail:
                if (_drq) _errors |= C.LostData;
                Complete();
                break;
            case Phase.WriteTrackPrime:
                if (!_writeLatchFull) Complete(C.LostData);
                else _phase = Phase.WaitWriteTrack;
                break;
            case Phase.WriteGap: AdvanceWriteGap(); break;
            case Phase.WriteFinish: FinishSector(); break;
        }
    }
}
