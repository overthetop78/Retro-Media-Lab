using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using RetroMediaLab.Chips.Audio.Yamaha.AICA;
using static RetroMediaLab.Chips.CPU.ARM.ARMv3.ARM7.Arm7Register;

namespace RetroMediaLab.Chips.CPU.ARM.ARMv3.ARM7;

internal static unsafe partial class Arm7Di
{
    private static readonly ulong[] RegisterStorage = GC.AllocateArray<ulong>((int)RN_ARM_REG_COUNT / 2, pinned: true);
    internal static Span<Arm7RegisterPair> Reg => MemoryMarshal.Cast<ulong, Arm7RegisterPair>(RegisterStorage);
    internal static ref uint ArmNextPC => ref Reg[(int)R15_ARM_NEXT].I;

    internal static uint* RegisterAddress
    {
        get
        {
            fixed (ulong* registers = RegisterStorage)
                return (uint*)registers;
        }
    }

    private static uint CPUReadMemoryQuick(uint address) => *(uint*)&AicaInterface.Ram[address & AicaInterface.RamMask];
    private static byte CPUReadByte(uint address) => AicaArmMemory.ReadMemory<byte>(address);
    private static uint CPUReadMemory(uint address) => AicaArmMemory.ReadMemory<uint>(address);
    private static ushort CPUReadHalfWord(uint address) => AicaArmMemory.ReadMemory<ushort>(address);
    private static short CPUReadHalfWordSigned(uint address) => unchecked((short)AicaArmMemory.ReadMemory<ushort>(address));
    private static void CPUWriteMemory(uint address, uint data) => AicaArmMemory.WriteMemory(address, data);
    private static void CPUWriteHalfWord(uint address, ushort data) => AicaArmMemory.WriteMemory(address, data);
    private static void CPUWriteByte(uint address, byte data) => AicaArmMemory.WriteMemory(address, data);
    private static int CPUUpdateTicksAccessSeq32(uint address) => 1;
    private static int CPUUpdateTicksAccess32(uint address) => 1;
    private static int CPUUpdateTicksAccess16(uint address) => 1;

    private static void CPUSwap(ref uint a, ref uint b)
    {
        uint c = b;
        b = a;
        a = c;
    }

    internal static bool ArmIrqEnable;
    internal static bool ArmFiqEnable;
    internal static int ArmMode;
    internal static bool Enabled;
    internal static int Arm7ClockTicks;
    internal static readonly byte[] CpuBitsSet = new byte[256];

    static Arm7Di()
    {
        for (int i = 0; i < CpuBitsSet.Length; i++)
        {
            int count = 0;
            for (int j = 0; j < 8; j++)
                if ((i & (1 << j)) != 0)
                    count++;

            CpuBitsSet[i] = (byte)count;
        }
    }

#if ARM7_DYNAREC_NONE
    private static void RunInterpreter(uint cycleCount)
    {
        if (!Enabled)
            return;

        Arm7ClockTicks = unchecked(Arm7ClockTicks - (int)cycleCount);
        while (Arm7ClockTicks < 0)
        {
            if (Reg[(int)INTR_PEND].I != 0)
                CPUFiq();

            Reg[15].I = ArmNextPC + 8;
            ExecuteInstruction(0, ref Arm7ClockTicks, readOpcode: true);
        }
    }

    internal static void AvoidRaceCondition()
    {
        Arm7ClockTicks = Math.Min(Arm7ClockTicks, -50);
    }

    internal static void Run(uint samples)
    {
        for (uint i = 0; i < samples; i++)
        {
            RunInterpreter(ArmCyclesPerSample);
            AicaInterface.TimeStep();
        }
    }
#endif

    internal static void Init()
    {
#if !ARM7_DYNAREC_NONE
        Arm7DiRecompiler.Init();
#endif
        Reset();
    }

    internal static void Term()
    {
#if !ARM7_DYNAREC_NONE
        Arm7DiRecompiler.Term();
#endif
    }

    private static void CPUSwitchMode(int mode, bool saveState)
    {
        CPUUpdateCPSR();

        switch (ArmMode)
        {
            case 0x10:
            case 0x1F:
                Reg[(int)R13_USR].I = Reg[13].I;
                Reg[(int)R14_USR].I = Reg[14].I;
                Reg[(int)RN_SPSR].I = Reg[(int)RN_CPSR].I;
                break;
            case 0x11:
                CPUSwap(ref Reg[(int)R8_FIQ].I, ref Reg[8].I);
                CPUSwap(ref Reg[(int)R9_FIQ].I, ref Reg[9].I);
                CPUSwap(ref Reg[(int)R10_FIQ].I, ref Reg[10].I);
                CPUSwap(ref Reg[(int)R11_FIQ].I, ref Reg[11].I);
                CPUSwap(ref Reg[(int)R12_FIQ].I, ref Reg[12].I);
                Reg[(int)R13_FIQ].I = Reg[13].I;
                Reg[(int)R14_FIQ].I = Reg[14].I;
                Reg[(int)SPSR_FIQ].I = Reg[(int)RN_SPSR].I;
                break;
            case 0x12:
                Reg[(int)R13_IRQ].I = Reg[13].I;
                Reg[(int)R14_IRQ].I = Reg[14].I;
                Reg[(int)SPSR_IRQ].I = Reg[(int)RN_SPSR].I;
                break;
            case 0x13:
                Reg[(int)R13_SVC].I = Reg[13].I;
                Reg[(int)R14_SVC].I = Reg[14].I;
                Reg[(int)SPSR_SVC].I = Reg[(int)RN_SPSR].I;
                break;
            case 0x17:
                Reg[(int)R13_ABT].I = Reg[13].I;
                Reg[(int)R14_ABT].I = Reg[14].I;
                Reg[(int)SPSR_ABT].I = Reg[(int)RN_SPSR].I;
                break;
            case 0x1B:
                Reg[(int)R13_UND].I = Reg[13].I;
                Reg[(int)R14_UND].I = Reg[14].I;
                Reg[(int)SPSR_UND].I = Reg[(int)RN_SPSR].I;
                break;
        }

        uint cpsr = Reg[(int)RN_CPSR].I;
        uint spsr = Reg[(int)RN_SPSR].I;

        switch (mode)
        {
            case 0x10:
            case 0x1F:
                Reg[13].I = Reg[(int)R13_USR].I;
                Reg[14].I = Reg[(int)R14_USR].I;
                Reg[(int)RN_CPSR].I = spsr;
                break;
            case 0x11:
                CPUSwap(ref Reg[8].I, ref Reg[(int)R8_FIQ].I);
                CPUSwap(ref Reg[9].I, ref Reg[(int)R9_FIQ].I);
                CPUSwap(ref Reg[10].I, ref Reg[(int)R10_FIQ].I);
                CPUSwap(ref Reg[11].I, ref Reg[(int)R11_FIQ].I);
                CPUSwap(ref Reg[12].I, ref Reg[(int)R12_FIQ].I);
                Reg[13].I = Reg[(int)R13_FIQ].I;
                Reg[14].I = Reg[(int)R14_FIQ].I;
                if (saveState)
                    Reg[(int)RN_SPSR].I = cpsr;
                else
                    Reg[(int)RN_SPSR].I = Reg[(int)SPSR_FIQ].I;
                break;
            case 0x12:
                Reg[13].I = Reg[(int)R13_IRQ].I;
                Reg[14].I = Reg[(int)R14_IRQ].I;
                Reg[(int)RN_CPSR].I = spsr;
                if (saveState)
                    Reg[(int)RN_SPSR].I = cpsr;
                else
                    Reg[(int)RN_SPSR].I = Reg[(int)SPSR_IRQ].I;
                break;
            case 0x13:
                Reg[13].I = Reg[(int)R13_SVC].I;
                Reg[14].I = Reg[(int)R14_SVC].I;
                Reg[(int)RN_CPSR].I = spsr;
                if (saveState)
                    Reg[(int)RN_SPSR].I = cpsr;
                else
                    Reg[(int)RN_SPSR].I = Reg[(int)SPSR_SVC].I;
                break;
            case 0x17:
                Reg[13].I = Reg[(int)R13_ABT].I;
                Reg[14].I = Reg[(int)R14_ABT].I;
                Reg[(int)RN_CPSR].I = spsr;
                if (saveState)
                    Reg[(int)RN_SPSR].I = cpsr;
                else
                    Reg[(int)RN_SPSR].I = Reg[(int)SPSR_ABT].I;
                break;
            case 0x1B:
                Reg[13].I = Reg[(int)R13_UND].I;
                Reg[14].I = Reg[(int)R14_UND].I;
                Reg[(int)RN_CPSR].I = spsr;
                if (saveState)
                    Reg[(int)RN_SPSR].I = cpsr;
                else
                    Reg[(int)RN_SPSR].I = Reg[(int)SPSR_UND].I;
                break;
            default:
                Trace.TraceError($"Unsupported ARM mode {mode:x2}");
                Enabled = false;
                break;
        }
        ArmMode = mode;
        CPUUpdateFlags();
        CPUUpdateCPSR();
    }

    internal static void CPUUpdateCPSR()
    {
        Arm7RegisterPair cpsr = default;
        cpsr.I = Reg[(int)RN_CPSR].I & 0x40;
        cpsr.PSR.NZCV = Reg[(int)RN_PSR_FLAGS].FLG.NZCV;
        if (!ArmFiqEnable)
            cpsr.I |= 0x40;
        if (!ArmIrqEnable)
            cpsr.I |= 0x80;
        cpsr.PSR.M = (uint)ArmMode;
        Reg[(int)RN_CPSR].I = cpsr.I;
    }

    private static void CPUUpdateFlags()
    {
        uint cpsr = Reg[(int)RN_CPSR].I;
        Reg[(int)RN_PSR_FLAGS].FLG.NZCV = Reg[(int)RN_CPSR].PSR.NZCV;
        ArmIrqEnable = (cpsr & 0x80) == 0;
        ArmFiqEnable = (cpsr & 0x40) == 0;
        UpdateInterruptController();
    }

    private static void CPUSoftwareInterrupt(int comment)
    {
        uint pc = Reg[(int)R15_ARM_NEXT].I + 4;
        CPUSwitchMode(0x13, true);
        Reg[14].I = pc;
        ArmIrqEnable = false;
        ArmNextPC = 0x08;
    }

    private static void CPUUndefinedException()
    {
        Trace.TraceWarning("arm7: CPUUndefinedException(). SOMETHING WENT WRONG");
        uint pc = Reg[(int)R15_ARM_NEXT].I + 4;
        CPUSwitchMode(0x1B, true);
        Reg[14].I = pc;
        ArmIrqEnable = false;
        ArmNextPC = 0x04;
    }

    internal static void Reset()
    {
        Trace.TraceInformation("AICA ARM Reset");
#if !ARM7_DYNAREC_NONE
        Arm7DiRecompiler.Flush();
#endif
        AicaArmMemory.AicaInterrupt = false;
        AicaArmMemory.AicaRegisterL = 0;
        AicaArmMemory.E68kOutput = false;
        AicaArmMemory.E68kRegisterL = 0;
        AicaArmMemory.E68kRegisterM = 0;

        Enabled = false;
        Reg.Clear();
        ArmMode = 0x13;
        Reg[13].I = 0x03007F00;
        Reg[15].I = 0;
        Reg[(int)RN_CPSR].I = 0;
        Reg[(int)R13_IRQ].I = 0x03007FA0;
        Reg[(int)R13_SVC].I = 0x03007FE0;
        ArmIrqEnable = true;
        ArmFiqEnable = false;
        UpdateInterruptController();

        Reg[(int)RN_PSR_FLAGS].FLG.C =
            Reg[(int)RN_PSR_FLAGS].FLG.V =
            Reg[(int)RN_PSR_FLAGS].FLG.N =
            Reg[(int)RN_PSR_FLAGS].FLG.Z = false;

        Reg[(int)RN_CPSR].I |= 0x40;
        CPUUpdateCPSR();
        ArmNextPC = Reg[15].I;
        Reg[15].I += 4;
    }

    internal static void CPUFiq()
    {
        uint pc = Reg[(int)R15_ARM_NEXT].I + 4;
        CPUSwitchMode(0x11, true);
        Reg[14].I = pc;
        ArmIrqEnable = false;
        ArmFiqEnable = false;
        UpdateInterruptController();
        ArmNextPC = 0x1C;
    }

    internal static void Enable(bool enabled)
    {
        if (!Enabled && enabled)
            Reset();
        Enabled = enabled;
    }

    internal static void UpdateInterruptController()
    {
        Reg[(int)INTR_PEND].I = AicaArmMemory.E68kOutput && ArmFiqEnable ? 1u : 0u;
    }

#if !ARM7_DYNAREC_NONE
    internal static delegate* unmanaged<void> UpdateCpsrEntry => &UpdateCpsrNative;
    internal static delegate* unmanaged<void> FiqEntry => &FiqNative;
    internal static delegate* unmanaged<uint, void> InterpretEntry => &InterpretNative;
    internal static delegate* unmanaged<uint, uint, void> MsrDoCpsrEntry => &MsrDoCpsrNative;
    internal static delegate* unmanaged<uint, uint, void> MsrDoSpsrEntry => &MsrDoSpsrNative;

    [UnmanagedCallersOnly]
    private static void UpdateCpsrNative() => CPUUpdateCPSR();

    [UnmanagedCallersOnly]
    private static void FiqNative() => CPUFiq();

    [UnmanagedCallersOnly]
    private static void InterpretNative(uint opcode) => Interpret(opcode);

    [UnmanagedCallersOnly]
    private static void MsrDoCpsrNative(uint value, uint mask) => MsrDo(value, mask, false);

    [UnmanagedCallersOnly]
    private static void MsrDoSpsrNative(uint value, uint mask) => MsrDo(value, mask, true);

    internal static void Interpret(uint opcode)
    {
        int clockTicks = 0;
        ExecuteInstruction(opcode, ref clockTicks, readOpcode: false);
        Reg[(int)CYCL_CNT].I -= unchecked((uint)clockTicks);
    }

    internal static void MsrDo(uint value, uint mask, bool pd)
    {
        if (pd)
        {
            if (ArmMode > 0x10 && ArmMode < 0x1F)
            {
                uint newValue = Reg[(int)RN_SPSR].I;
                if ((mask & 1) != 0)
                    newValue = (newValue & 0xFFFFFF00) | (value & 0x000000FF);
                if ((mask & 2) != 0)
                    newValue = (newValue & 0xFFFF00FF) | (value & 0x0000FF00);
                if ((mask & 4) != 0)
                    newValue = (newValue & 0xFF00FFFF) | (value & 0x00FF0000);
                if ((mask & 8) != 0)
                    newValue = (newValue & 0x00FFFFFF) | (value & 0xFF000000);
                Reg[(int)RN_SPSR].I = newValue;
            }
        }
        else
        {
            CPUUpdateCPSR();
            uint newValue = Reg[(int)RN_CPSR].I;
            if (ArmMode > 0x10)
            {
                if ((mask & 1) != 0)
                    newValue = (newValue & 0xFFFFFF00) | (value & 0x000000FF);
                if ((mask & 2) != 0)
                    newValue = (newValue & 0xFFFF00FF) | (value & 0x0000FF00);
                if ((mask & 4) != 0)
                    newValue = (newValue & 0xFF00FFFF) | (value & 0x00FF0000);
            }
            if ((mask & 8) != 0)
                newValue = (newValue & 0x00FFFFFF) | (value & 0xFF000000);
            newValue |= 0x10;
            if (ArmMode > 0x10)
                CPUSwitchMode((int)(newValue & 0x1F), false);
            Reg[(int)RN_CPSR].I = newValue;
            CPUUpdateFlags();
        }
    }
#endif
}
