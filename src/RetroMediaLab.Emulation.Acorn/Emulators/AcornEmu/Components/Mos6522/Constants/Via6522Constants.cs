namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Mos6522.Constants;

internal static class Via6522Constants
{
    internal const int RegisterCount = 16;
    internal const int RegisterMask = RegisterCount - 1;
    internal const int PortB = 0;
    internal const int PortA = 1;
    internal const int DirectionB = 2;
    internal const int DirectionA = 3;
    internal const int Timer1Low = 4;
    internal const int Timer1High = 5;
    internal const int Latch1Low = 6;
    internal const int Latch1High = 7;
    internal const int Timer2Low = 8;
    internal const int Timer2High = 9;
    internal const int Shift = 10;
    internal const int Auxiliary = 11;
    internal const int Peripheral = 12;
    internal const int Flags = 13;
    internal const int Enable = 14;
    internal const int PortANoHandshake = 15;
    internal const int CounterMask = ushort.MaxValue;
    internal const int HighByteShift = 8;
    internal const int HighByteMask = 0xff00;
    internal const byte Ca2Interrupt = 0x01;
    internal const byte Ca1Interrupt = 0x02;
    internal const byte Cb2Interrupt = 0x08;
    internal const byte Cb1Interrupt = 0x10;
    internal const byte Timer2Interrupt = 0x20;
    internal const byte Timer1Interrupt = 0x40;
    internal const byte InterruptSummary = 0x80;
    internal const byte InterruptMask = 0x7f;
    internal const byte LatchA = 0x01;
    internal const byte LatchB = 0x02;
    internal const byte Timer2PulseCount = 0x20;
    internal const byte Timer1Continuous = 0x40;
    internal const byte Pb6 = 0x40;
    internal const byte Ca1Rising = 0x01;
    internal const byte Cb1Rising = 0x10;
    internal const int Ca2ModeShift = 1;
    internal const int Cb2ModeShift = 5;
    internal const int ControlModeMask = 7;
    internal const int PulseCycles = 1;
}
