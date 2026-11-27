using System.Runtime.InteropServices;
using RetroMediaLab.Chips.Audio.Yamaha.AICA;

namespace RetroMediaLab.Chips.CPU.ARM.ARMv3.ARM7;

internal enum Arm7Register
{
    RN_LR = 14,
    RN_PC = 15,
    RN_CPSR = 16,
    RN_SPSR = 17,

    R13_IRQ = 18,
    R14_IRQ = 19,
    SPSR_IRQ = 20,
    R13_USR = 26,
    R14_USR = 27,
    R13_SVC = 28,
    R14_SVC = 29,
    SPSR_SVC = 30,
    R13_ABT = 31,
    R14_ABT = 32,
    SPSR_ABT = 33,
    R13_UND = 34,
    R14_UND = 35,
    SPSR_UND = 36,
    R8_FIQ = 37,
    R9_FIQ = 38,
    R10_FIQ = 39,
    R11_FIQ = 40,
    R12_FIQ = 41,
    R13_FIQ = 42,
    R14_FIQ = 43,
    SPSR_FIQ = 44,
    RN_PSR_FLAGS = 45,
    R15_ARM_NEXT = 46,
    INTR_PEND = 47,
    CYCL_CNT = 48,
    RN_SCRATCH = 49,

    RN_ARM_REG_COUNT
}

[StructLayout(LayoutKind.Explicit)]
internal struct Arm7RegisterPair
{
    [FieldOffset(0)] internal Arm7RegisterBytes B;
    [FieldOffset(0)] internal Arm7RegisterWords W;
    [FieldOffset(0)] internal Arm7RegisterFlags FLG;
    [FieldOffset(0)] internal Arm7ProgramStatus PSR;
    [FieldOffset(0)] internal uint I;
}

[StructLayout(LayoutKind.Sequential)]
internal struct Arm7RegisterBytes
{
    internal byte B0;
    internal byte B1;
    internal byte B2;
    internal byte B3;
}

[StructLayout(LayoutKind.Sequential)]
internal struct Arm7RegisterWords
{
    internal ushort W0;
    internal ushort W1;
}

[StructLayout(LayoutKind.Sequential)]
internal struct Arm7RegisterFlags
{
    private uint _value;

    internal bool V
    {
        readonly get => (_value & 0x10000000) != 0;
        set => _value = (_value & 0xEFFFFFFF) | (value ? 0x10000000u : 0);
    }

    internal bool C
    {
        readonly get => (_value & 0x20000000) != 0;
        set => _value = (_value & 0xDFFFFFFF) | (value ? 0x20000000u : 0);
    }

    internal bool Z
    {
        readonly get => (_value & 0x40000000) != 0;
        set => _value = (_value & 0xBFFFFFFF) | (value ? 0x40000000u : 0);
    }

    internal bool N
    {
        readonly get => (_value & 0x80000000) != 0;
        set => _value = (_value & 0x7FFFFFFF) | (value ? 0x80000000u : 0);
    }

    internal uint NZCV
    {
        readonly get => _value >> 28;
        set => _value = (_value & 0x0FFFFFFF) | (value << 28);
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct Arm7ProgramStatus
{
    private uint _value;

    internal uint M
    {
        readonly get => _value & 0x1F;
        set => _value = (_value & 0xFFFFFFE0) | (value & 0x1F);
    }

    internal bool F
    {
        readonly get => (_value & 0x40) != 0;
        set => _value = (_value & 0xFFFFFFBF) | (value ? 0x40u : 0);
    }

    internal bool I
    {
        readonly get => (_value & 0x80) != 0;
        set => _value = (_value & 0xFFFFFF7F) | (value ? 0x80u : 0);
    }

    internal uint NZCV
    {
        readonly get => _value >> 28;
        set => _value = (_value & 0x0FFFFFFF) | (value << 28);
    }
}

internal static partial class Arm7Di
{
    internal static uint ArmCyclesPerSample => AicaInterface.ArmClock / 44100;
}
