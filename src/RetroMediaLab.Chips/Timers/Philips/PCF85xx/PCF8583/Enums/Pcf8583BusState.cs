namespace RetroMediaLab.Chips.Timers.Philips.PCF85xx.PCF8583.Enums;

public enum Pcf8583BusState
{
    Idle,
    Address,
    Pointer,
    Write,
    SlaveAcknowledge,
    Read,
    MasterAcknowledge
}
