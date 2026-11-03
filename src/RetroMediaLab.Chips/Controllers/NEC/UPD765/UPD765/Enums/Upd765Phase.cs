namespace RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Enums;

public enum Upd765Phase
{
    Idle,
    Command,
    HeadLoad,
    SearchId,
    Reading,
    Writing,
    DataEnd,
    Result,
    FormatLead,
    FormatGap,
    FormatTail,
    DrivePrepare,
    SenseDrive
}
