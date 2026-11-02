namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Pcf8583.Enums;

internal enum Pcf8583BusState
{
    Idle,
    Address,
    Pointer,
    Write,
    SlaveAcknowledge,
    Read,
    MasterAcknowledge
}
