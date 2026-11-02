namespace RetroMediaLab.Chips.IO.Acorn.Archimedes.IOC.Enums;

public enum IocRegister : uint
{
    Control = 0x00,
    Keyboard = 0x04,
    IrqStatusA = 0x10,
    IrqRequestA = 0x14,
    IrqMaskA = 0x18,
    IrqStatusB = 0x20,
    IrqRequestB = 0x24,
    IrqMaskB = 0x28,
    FiqStatus = 0x30,
    FiqRequest = 0x34,
    FiqMask = 0x38,
    TimerBase = 0x40
}
