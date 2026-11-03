using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Enums;
using C = RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Constants.Upd765Constants;

namespace RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Services;

public sealed partial class Upd765
{
    private Upd765PowerState _powerState;
    private long _powerDelay;
    public Upd765PowerState PowerState => _powerState;
    private bool PowerClockStopped => _powerState is Upd765PowerState.Standby or Upd765PowerState.Waking;

    /// <summary>Registre auxiliaire CMOS A0=0. Les 765A/B n'ont pas ce registre.</summary>
    public void WriteAuxiliaryCommand(byte value)
    {
        if (_resetAsserted || _model != Upd765Model.Upd72065B) return;
        switch ((Upd765AuxiliaryCommand)value)
        {
            case Upd765AuxiliaryCommand.SetStandby when _powerState == Upd765PowerState.Active:
                _powerState = Upd765PowerState.EnteringStandby;
                _powerDelay = C.StandbyHoldClocks;
                break;
            case Upd765AuxiliaryCommand.ResetStandby when _powerState is Upd765PowerState.Standby or Upd765PowerState.EnteringStandby:
                _powerState = Upd765PowerState.Waking;
                _powerDelay = C.StandbyRecoveryClocks;
                break;
            case Upd765AuxiliaryCommand.SoftwareReset:
                Reset();
                _drives.ReleaseReset();
                break;
        }
    }

    private void ResetPower()
    {
        _powerState = Upd765PowerState.Active;
        _powerDelay = 0;
    }
}
