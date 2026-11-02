namespace RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Constants;

public static class Timer80186Constants
{
    public const int Count = 3;
    public const int ExternalCount = 2;
    public const int PrescalerTimer = 2;
    public const int RegisterBase = 0x50;
    public const int RegisterStride = 8;
    public const int CounterRegister = 0;
    public const int MaximumARegister = 2;
    public const int MaximumBRegister = 4;
    public const int ControlRegister = 6;
    public const int LastRegister = 0x66;
    public const int ClockDivider = 4;
    public const int PulseDelay = 1;
    public const ushort Enabled = 0x8000;
    public const ushort ChangeEnable = 0x4000;
    public const ushort InterruptEnabled = 0x2000;
    public const ushort RegisterInUse = 0x1000;
    public const ushort MaximumReached = 0x0020;
    public const ushort Retrigger = 0x0010;
    public const ushort Prescaled = 0x0008;
    public const ushort ExternalClock = 0x0004;
    public const ushort Alternate = 0x0002;
    public const ushort Continuous = 0x0001;
    public const ushort PreservedBits = 0x1fc0;
    public const ushort Timer2PreservedBits = 0x1fde;
}
