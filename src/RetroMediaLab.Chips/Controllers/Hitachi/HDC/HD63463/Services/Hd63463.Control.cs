using C = RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Constants.Hd63463Constants;
using Command = RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Enums.Hd63463Command;
using Phase = RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Enums.Hd63463Phase;

namespace RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Services;

public sealed partial class Hd63463
{
    private void BeginControl(Phase phase, int cycles)
    {
        _phase = phase;
        _status = C.ParameterReject | C.Busy;
        _parameterRead = _parameterWrite = 0;
        _delay = ControllerCycles(cycles);
        SetDrq(false);
        UpdateInterrupt();
    }

    private void BeginAbort()
    {
        _command = Command.Abort;
        _polling = false;
        Array.Clear(_seekDelays);
        _completedSeeks = 0;
        ResetRecord();
        ResetErrorCode();
        BeginControl(Phase.AbortingOutputs, C.AbortOutputsCycles);
    }

    private bool CompleteControlEvent()
    {
        switch (_phase)
        {
            case Phase.Resetting:
            case Phase.Recalling:
                _status = 0;
                _phase = Phase.Idle;
                UpdateInterrupt();
                return true;
            case Phase.Specifying:
                CompleteSpecify();
                return true;
            case Phase.AbortingOutputs:
                _selected = false;
                _activity = 0;
                _phase = Phase.Aborting;
                // Subtract rounded absolute deadlines, so a non-integral host
                // clock does not lengthen the complete command by one tick.
                _delay = ControllerCycles(C.AbortCycles) - ControllerCycles(C.AbortOutputsCycles);
                if (_delay == 0) Finish(C.AbortAccepted);
                return true;
            case Phase.Aborting:
                Finish(C.AbortAccepted);
                return true;
            default:
                return false;
        }
    }
}
