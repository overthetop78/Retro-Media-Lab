namespace RetroMediaLab.Chips.IO.NationalSemiconductor.UART.NS16450.Enums;

public enum Ns16450Interrupt : byte
{
    ModemStatus = 0x00,
    None = 0x01,
    TransmitEmpty = 0x02,
    ReceivedData = 0x04,
    LineStatus = 0x06
}
