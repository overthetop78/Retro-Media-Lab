namespace RetroMediaLab.Chips.IO.Acorn.Archimedes.IOC.Constants;

public static class ArchimedesIocConstants
{
    public const uint RegisterMask = 0x7c;
    public const int TimerCount = 4;
    public const int InterruptTimerCount = 2;
    public const int BaudTimer = 2;
    public const int KeyboardTimer = 3;
    public const int TimerStride = 16;
    public const int TimerRegisterMask = 12;
    public const int TimerLow = 0;
    public const int TimerHigh = 4;
    public const int TimerGo = 8;
    public const int TimerLatch = 12;
    public const int ByteBits = 8;
    public const int HighByteMask = 0xff00;
    public const int LowByteMask = 0x00ff;
    public const byte ForceInterrupt = 0x80;
    public const byte PowerOnInterrupt = 0x10;
    public const byte FallingInterrupt = 0x04;
    public const byte FlybackInterrupt = 0x08;
    public const byte TimerZeroInterrupt = 0x20;
    public const byte IrqLatchedMask = 0x7c;
    public const byte SoundInterrupt = 0x02;
    public const byte FloppyInterrupt = 0x10;
    public const byte FloppyDataRequest = 0x01;
    public const byte KeyboardTransmitInterrupt = 0x40;
    public const byte KeyboardReceiveInterrupt = 0x80;
    public const byte ControlPins = 0x3f;
    public const byte ControlFiqPins = 0x38;
    public const byte ControlFallingPin = 0x40;
    public const byte ControlRisingPin = 0x80;
    public const byte InitialControl = 0xff;
    public const int SerialFrameHalfBits = 22;
    public const int ReceiveInterruptHalfBits = 17;
    public const int SerialTicksPerHalfBit = 16;
    public const int InternalIoCycles = 2;
}
