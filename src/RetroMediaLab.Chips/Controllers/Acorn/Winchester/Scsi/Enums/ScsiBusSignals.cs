namespace RetroMediaLab.Chips.Controllers.Acorn.Winchester.Scsi.Enums;

/// <summary>Assertions logiques des sorties cible, après inversion des niveaux électriques actifs bas.</summary>
[Flags]
public enum ScsiBusSignals : byte
{
    None = 0,
    Message = 0x01,
    Busy = 0x02,
    Request = 0x20,
    InputOutput = 0x40,
    ControlData = 0x80
}
