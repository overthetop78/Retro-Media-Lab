namespace RetroMediaLab.Chips.IO.Motorola.M6800.Chip6850.Enums;

public enum Acia6850TransmitControl : byte
{
    RequestToSend = 0,
    RequestToSendWithInterrupt = 1,
    ReleaseRequestToSend = 2,
    SendBreak = 3
}
