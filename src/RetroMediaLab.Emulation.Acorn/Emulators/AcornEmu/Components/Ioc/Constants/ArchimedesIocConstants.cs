namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Ioc.Constants;

internal static class ArchimedesIocConstants
{
    internal const uint RegisterMask = 0x7c;
    internal const int TimerCount = 4;
    internal const int InterruptTimerCount = 2;
    internal const int BaudTimer = 2;
    internal const int KeyboardTimer = 3;
    internal const int TimerStride = 16;
    internal const int TimerRegisterMask = 12;
    internal const int TimerLow = 0;
    internal const int TimerHigh = 4;
    internal const int TimerGo = 8;
    internal const int TimerLatch = 12;
    internal const int ByteBits = 8;
    internal const int HighByteMask = 0xff00;
    internal const int LowByteMask = 0x00ff;
    internal const byte ForceInterrupt = 0x80;
    internal const byte PowerOnInterrupt = 0x10;
    internal const byte FallingInterrupt = 0x04;
    internal const byte FlybackInterrupt = 0x08;
    internal const byte TimerZeroInterrupt = 0x20;
    internal const byte IrqLatchedMask = 0x7c;
    internal const byte SoundInterrupt = 0x02;
    internal const byte FloppyInterrupt = 0x10;
    internal const byte FloppyDataRequest = 0x01;
    internal const byte KeyboardTransmitInterrupt = 0x40;
    internal const byte KeyboardReceiveInterrupt = 0x80;
    internal const byte ControlPins = 0x3f;
    internal const byte ControlFiqPins = 0x38;
    internal const byte ControlFallingPin = 0x40;
    internal const byte ControlRisingPin = 0x80;
    internal const byte InitialControl = 0xff;
    internal const int SerialFrameHalfBits = 22;
    internal const int ReceiveInterruptHalfBits = 17;
    internal const int SerialTicksPerHalfBit = 16;
    internal const int InternalIoCycles = 2;
}
