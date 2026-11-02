namespace RetroMediaLab.Chips.Controllers.Generic.ATA.PIO.Enums;

public enum AtaPhase
{
    Idle,
    Reset,
    Command,
    Reading,
    Writing,
    Verifying,
    Sending,
    Receiving,
    Committing
}
