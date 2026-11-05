using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;
using C = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000Constants;
using O = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000CommandOptionConstants;

namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

public sealed partial class Acb4000
{
    public bool ExpandedCommandsEnabled { get; private set; }

    /// <summary>Entrée matérielle I–J ; la connexion physique est indépendante du RESET SCSI.</summary>
    public void SetExpandedCommandSet(bool enabled) => ExpandedCommandsEnabled = enabled;

    private bool TryExecuteCommandOption()
    {
        switch (_code)
        {
            case Acb4000CommandCode.ExpandedCheckTrack:
            case Acb4000CommandCode.ExpandedRamDiagnostic:
            case Acb4000CommandCode.ExpandedDriveDiagnostic:
            case Acb4000CommandCode.ExpandedControllerDiagnostic:
                // Appendix B explicitly specifies acceptance without repeating these operations.
                Complete(ExpandedCommandsEnabled ? C.NoSense : C.InvalidCommand);
                return true;
            case Acb4000CommandCode.ExpandedInitialize:
                if (!ExpandedCommandsEnabled) { Complete(C.InvalidCommand); return true; }
                // Firmware 103D–104D consumes the SASI parameter block but does not apply it.
                Receive(O.InitializeParameterLength);
                return true;
            case Acb4000CommandCode.ExpandedReadEccBurst:
                if (!ExpandedCommandsEnabled) { Complete(C.InvalidCommand); return true; }
                _buffer[0] = O.MaximumEccBurstLength;
                Reply(O.EccBurstResponseLength);
                return true;
            case Acb4000CommandCode.ExpandedReadBuffer:
                if (!ExpandedCommandsEnabled) { Complete(C.InvalidCommand); return true; }
                Reply(ExpandedBufferLength);
                return true;
            case Acb4000CommandCode.TranslateOrExpandedWriteBuffer:
                if (!ExpandedCommandsEnabled) return false;
                Receive(ExpandedBufferLength);
                return true;
            default:
                return false;
        }
    }

    private int ExpandedBufferLength => _modeSelected[Unit] || _parametersLoaded[Unit]
        ? _modeParameters[Unit].BlockSize : C.DefaultBlockSize;
}
