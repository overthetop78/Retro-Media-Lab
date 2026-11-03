namespace RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Enums;

internal enum Upd765DrivePhase
{
    Idle,
    Select,
    Seek,
    Direction,
    Step,
    Hold,
    Release,
    SenseRead,
    SenseTrackZero,
    SenseTwoSided,
    PollReady,
    PollHold
}
