namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Upd765.Enums;

internal enum Upd765Phase
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
    FormatTail
}
