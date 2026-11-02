namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Ioc.Enums;

internal enum IocRegister : uint
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
