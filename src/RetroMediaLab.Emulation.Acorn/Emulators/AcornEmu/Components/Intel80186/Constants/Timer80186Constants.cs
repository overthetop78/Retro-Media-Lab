namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Intel80186.Constants;

internal static class Timer80186Constants
{
    internal const int Count = 3;
    internal const int ExternalCount = 2;
    internal const int PrescalerTimer = 2;
    internal const int RegisterBase = 0x50;
    internal const int RegisterStride = 8;
    internal const int CounterRegister = 0;
    internal const int MaximumARegister = 2;
    internal const int MaximumBRegister = 4;
    internal const int ControlRegister = 6;
    internal const int LastRegister = 0x66;
    internal const int ClockDivider = 4;
    internal const int PulseDelay = 1;
    internal const ushort Enabled = 0x8000;
    internal const ushort ChangeEnable = 0x4000;
    internal const ushort InterruptEnabled = 0x2000;
    internal const ushort RegisterInUse = 0x1000;
    internal const ushort MaximumReached = 0x0020;
    internal const ushort Retrigger = 0x0010;
    internal const ushort Prescaled = 0x0008;
    internal const ushort ExternalClock = 0x0004;
    internal const ushort Alternate = 0x0002;
    internal const ushort Continuous = 0x0001;
    internal const ushort PreservedBits = 0x1fc0;
    internal const ushort Timer2PreservedBits = 0x1fde;
}
