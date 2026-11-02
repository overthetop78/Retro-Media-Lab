namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Ata.Enums;

internal enum AtaPhase
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
