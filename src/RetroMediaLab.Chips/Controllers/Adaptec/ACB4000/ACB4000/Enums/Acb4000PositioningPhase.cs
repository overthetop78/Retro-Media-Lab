namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;

public enum Acb4000PositioningPhase
{
    Idle,
    InitialCheck,
    SeekSteps,
    LeaveTrackZero,
    WaitForExcursion,
    SearchTrackZero,
    WaitForTrackZeroStep,
    Complete
}
