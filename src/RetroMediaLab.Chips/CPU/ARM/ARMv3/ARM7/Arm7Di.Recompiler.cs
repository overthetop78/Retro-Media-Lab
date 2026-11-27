using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using RetroMediaLab.Chips.Audio.Yamaha.AICA;
using RetroMediaLab.Chips.CPU.Hitachi.SH4;

namespace RetroMediaLab.Chips.CPU.ARM.ARMv3.ARM7;

using static Arm7Register;
using static ArmOp;

#if !ARM7_DYNAREC_NONE
internal static unsafe partial class Arm7DiRecompiler
{
    internal static nuint ArmCompileCode;
    internal static delegate* unmanaged<nint, nint, void> ArmMainLoop;
    internal static readonly nuint[] EntryPoints = GC.AllocateArray<nuint>((int)(AicaInterface.AramSizeMax / 4), pinned: true);
    private static readonly Lazy<nuint> Arm7Tcb = new(() => Sh4VirtualMemory.DeclareCodeCache(ICacheSize));
    private static readonly List<ArmOp> BlockOperations = [];

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct ArmOpBits(uint full)
    {
        internal uint Full = full;
        internal readonly uint Immediate12 => Full & 0xfff;
        internal uint Rd
        {
            readonly get => (Full >> 12) & 0xf;
            set => Full = (Full & ~0xf000u) | ((value & 0xf) << 12);
        }
        internal uint Rn
        {
            readonly get => (Full >> 16) & 0xf;
            set => Full = (Full & ~0xf0000u) | ((value & 0xf) << 16);
        }
        internal readonly bool SetFlags => (Full & (1u << 20)) != 0;
        internal readonly uint Operation => (Full >> 21) & 0xf;
        internal readonly bool ImmediateOperation => (Full & (1u << 25)) != 0;
        internal readonly uint Group => (Full >> 26) & 3;
        internal readonly uint Condition => Full >> 28;
        internal readonly uint Rm => Full & 0xf;
        internal readonly bool ShiftByRegister => (Full & (1u << 4)) != 0;
        internal readonly uint ShiftType => (Full >> 5) & 3;
        internal readonly uint ShiftImmediate => (Full >> 7) & 0x1f;
        internal readonly uint Immediate8 => Full & 0xff;
        internal readonly uint Rotate => (Full >> 8) & 0xf;
        internal readonly bool Zero => (Full & (1u << 7)) != 0;
        internal readonly uint ShiftRegister => (Full >> 8) & 0xf;
        internal bool Load
        {
            readonly get => (Full & (1u << 20)) != 0;
            set => Full = (Full & ~(1u << 20)) | (value ? 1u << 20 : 0);
        }
        internal bool WriteBack
        {
            readonly get => (Full & (1u << 21)) != 0;
            set => Full = (Full & ~(1u << 21)) | (value ? 1u << 21 : 0);
        }
        internal readonly bool Byte => (Full & (1u << 22)) != 0;
        internal bool Up
        {
            readonly get => (Full & (1u << 23)) != 0;
            set => Full = (Full & ~(1u << 23)) | (value ? 1u << 23 : 0);
        }
        internal bool PreIndex
        {
            readonly get => (Full & (1u << 24)) != 0;
            set => Full = (Full & ~(1u << 24)) | (value ? 1u << 24 : 0);
        }
    }

    private static uint FindFirstSet(uint value) =>
        value == 0 ? 0u : (uint)BitOperations.TrailingZeroCount(value) + 1;

    private static ArmOp DecodeArmOp(uint opcode, uint armPc)
    {
        var bits = new ArmOpBits(opcode);
        var op = new ArmOp();
        op.ConditionCode = (Condition)bits.Condition;
        if (op.ConditionCode == Condition.UC)
        {
            op.Type = OpType.FALLBACK;
            op.Arg[0] = new Operand(opcode);
            op.Cycles = 0;
            return op;
        }
        if (op.ConditionCode != Condition.AL)
            op.Flags |= (byte)OP_READS_FLAGS;

        switch (bits.Group)
        {
            case 0:
                if ((bits.Operation & 4) == 0 && !bits.ImmediateOperation && bits.ShiftByRegister && bits.Zero)
                {
                    op.Type = OpType.FALLBACK;
                    op.Arg[0] = new Operand(opcode);
                    op.Cycles = 0;
                    return op;
                }
                if (bits.Operation >= (uint)OpType.TST && bits.Operation <= (uint)OpType.CMN && !bits.SetFlags)
                {
                    op.Spsr = (bits.Operation & 2) != 0;
                    if ((bits.Full & 0x0fbf0fff) == 0x010f0000)
                    {
                        op.Type = OpType.MRS;
                        op.Rd = new Operand((Arm7Register)bits.Rd);
                        Sh4HostRuntime.Verify(bits.Rd != 15);
                    }
                    else if ((bits.Full & 0x0fb0fff0) == 0x0120f000)
                    {
                        op.Type = OpType.MSR;
                        op.PsrMask = (byte)((bits.Full >> 16) & 0xf);
                        op.Arg[0] = new Operand((Arm7Register)bits.Rm);
                        op.Cycles++;
                    }
                    else if ((bits.Full & 0x0db0f000) == 0x0120f000)
                    {
                        op.Type = OpType.MSR;
                        op.PsrMask = (byte)((bits.Full >> 16) & 0xf);
                        if (!bits.ImmediateOperation)
                        {
                            op.Arg[0] = new Operand((Arm7Register)bits.Rm);
                            Sh4HostRuntime.Verify(bits.Rm != 15);
                        }
                        else
                        {
                            int rotate = (int)bits.Rotate * 2;
                            op.Arg[0] = new Operand((bits.Immediate8 >> rotate) | (bits.Immediate8 << (32 - rotate)));
                        }
                    }
                    else
                    {
                        op.Type = OpType.MOV;
                        op.ConditionCode = Condition.AL;
                        op.Flags = 0;
                        op.Rd = new Operand((Arm7Register)0);
                        op.Arg[0] = op.Rd;
                    }
                    return op;
                }
                {
                    op.Type = (OpType)bits.Operation;
                    if (!op.IsCompOp())
                        op.Rd = new Operand((Arm7Register)bits.Rd);
                    int argumentIndex = 0;
                    if (op.Type != OpType.MOV && op.Type != OpType.MVN)
                    {
                        op.Arg[0] = new Operand((Arm7Register)bits.Rn);
                        if (op.Arg[0].GetReg().ArmReg == RN_PC)
                            op.Arg[0] = new Operand(armPc + (!bits.ImmediateOperation && bits.ShiftByRegister ? 12u : 8u));
                        argumentIndex++;
                    }
                    if (bits.SetFlags)
                        op.Flags |= (byte)OP_SETS_FLAGS;
                    if (bits.ImmediateOperation)
                    {
                        int rotate = (int)bits.Rotate * 2;
                        op.Arg[argumentIndex] = new Operand((bits.Immediate8 >> rotate) | (bits.Immediate8 << (32 - rotate)));
                    }
                    else
                    {
                        ref Operand argument = ref op.Arg[argumentIndex];
                        argument = new Operand((Arm7Register)bits.Rm);
                        argument.ShiftType = (ShiftOp)bits.ShiftType;
                        argument.ShiftImmediate = !bits.ShiftByRegister;
                        if (argument.ShiftImmediate)
                        {
                            argument.ShiftValue = bits.ShiftImmediate;
                            if (argument.ShiftType == ShiftOp.RRX && argument.ShiftValue == 0)
                                op.Flags |= (byte)OP_READS_FLAGS;
                        }
                        else
                        {
                            argument.ShiftRegister = new Register((Arm7Register)bits.ShiftRegister);
                            op.Cycles++;
                        }
                        if (argument.GetReg().ArmReg == RN_PC)
                        {
                            if (argument.ShiftImmediate && !((op.Flags & OP_SETS_FLAGS) != 0 && op.IsLogicalOp())
                                && argument.ShiftType != ShiftOp.ROR)
                            {
                                uint nextPc = armPc + 8;
                                switch (argument.ShiftType)
                                {
                                    case ShiftOp.LSL:
                                        argument = new Operand(nextPc << (int)argument.ShiftValue);
                                        break;
                                    case ShiftOp.LSR:
                                        argument = new Operand(nextPc >> (int)argument.ShiftValue);
                                        break;
                                    case ShiftOp.ASR:
                                        argument = new Operand(unchecked((uint)((int)nextPc >> (int)argument.ShiftValue)));
                                        break;
                                    default:
                                        break;
                                }
                            }
                            else
                            {
                                argument.SetImmediate(armPc + (argument.ShiftImmediate ? 8u : 12u));
                            }
                        }
                    }
                    if (op.Rd.IsReg() && op.Rd.GetReg().ArmReg == RN_PC)
                    {
                        if (op.Type == OpType.MOV && op.Arg[0].IsReg() && !bits.SetFlags && !op.Arg[0].IsShifted())
                        {
                            op.Type = OpType.B;
                            op.Flags |= (byte)OP_SETS_PC;
                            op.Rd = new Operand();
                            op.Cycles += 3;
                            return op;
                        }
                        if (op.ConditionCode != Condition.AL || (op.Flags & OP_SETS_FLAGS) != 0)
                        {
                            op.Type = OpType.FALLBACK;
                            op.Arg[0] = new Operand(opcode);
                            op.Arg[1] = new Operand();
                            op.Arg[2] = new Operand();
                            op.Rd = new Operand();
                            op.Cycles = 0;
                        }
                        else
                        {
                            op.Rd.GetReg().ArmReg = R15_ARM_NEXT;
                            op.Cycles++;
                        }
                        op.Flags |= (byte)OP_SETS_PC;
                    }
                    if (op.Type == OpType.ADC || op.Type == OpType.SBC || op.Type == OpType.RSC)
                        op.Flags |= (byte)OP_READS_FLAGS;
                }
                break;

            case 1:
            {
                op.AddOffset = bits.Up;
                op.ByteTransfer = bits.Byte;
                op.PreIndex = bits.PreIndex;
                op.WriteBack = (bits.WriteBack || !bits.PreIndex) && bits.Rd != bits.Rn
                    && (bits.ImmediateOperation || bits.Immediate12 != 0);
                if (bits.Load)
                {
                    op.Type = OpType.LDR;
                    op.Rd = new Operand((Arm7Register)bits.Rd);
                    if (op.Rd.GetReg().ArmReg == RN_PC)
                    {
                        op.Flags |= (byte)OP_SETS_PC;
                        if (op.ConditionCode != Condition.AL)
                        {
                            op.Type = OpType.FALLBACK;
                            op.Arg[0] = new Operand(opcode);
                            op.Cycles = 0;
                            return op;
                        }
                        op.Rd.SetReg(R15_ARM_NEXT);
                        op.Cycles++;
                    }
                    op.Cycles += 4;
                }
                else
                {
                    op.Type = OpType.STR;
                    op.Arg[2] = new Operand((Arm7Register)bits.Rd);
                    if (op.Arg[2].GetReg().ArmReg == RN_PC)
                        op.Arg[2] = new Operand(armPc + 12);
                    op.Cycles += 3;
                }
                op.Arg[0] = new Operand((Arm7Register)bits.Rn);
                if (op.Arg[0].GetReg().ArmReg == RN_PC && op.WriteBack)
                {
                    op.Flags |= (byte)OP_SETS_PC;
                    op.Type = OpType.FALLBACK;
                    op.Arg[0] = new Operand(opcode);
                    op.Arg[1] = new Operand();
                    op.Arg[2] = new Operand();
                    op.Cycles = 0;
                    return op;
                }
                if (!bits.ImmediateOperation)
                {
                    if (op.Arg[0].GetReg().ArmReg == RN_PC)
                        op.Arg[0] = new Operand(unchecked(armPc + 8 + (uint)(op.AddOffset ? (int)bits.Immediate12 : -(int)bits.Immediate12)));
                    else
                        op.Arg[1] = new Operand(bits.Immediate12);
                }
                else
                {
                    if (op.Arg[0].GetReg().ArmReg == RN_PC)
                        op.Arg[0] = new Operand(armPc + 8);
                    op.Arg[1] = new Operand((Arm7Register)bits.Rm);
                    op.Arg[1].ShiftType = (ShiftOp)bits.ShiftType;
                    op.Arg[1].ShiftImmediate = true;
                    op.Arg[1].ShiftValue = bits.ShiftImmediate;
                    if (op.Arg[1].GetReg().ArmReg == RN_PC)
                    {
                        op.Type = OpType.FALLBACK;
                        op.Arg[0] = new Operand(opcode);
                        op.Arg[1] = new Operand();
                        op.Arg[2] = new Operand();
                        op.Cycles = 0;
                        return op;
                    }
                    if (op.Arg[1].ShiftType == ShiftOp.RRX && op.Arg[1].ShiftValue == 0)
                        op.Flags |= (byte)OP_READS_FLAGS;
                }
                break;
            }

            case 2:
                if (bits.ImmediateOperation)
                {
                    op.Type = (bits.Operation & 8) != 0 ? OpType.BL : OpType.B;
                    op.Arg[0] = new Operand(unchecked(armPc + 8 + (uint)(((int)(opcode & 0xffffff) << 8) >> 6)));
                    op.Flags |= (byte)OP_SETS_PC;
                    op.Cycles += 3;
                }
                else
                {
                    uint registerList = opcode & 0xffff;
                    if ((opcode & (1u << 22)) == 0
                        && Arm7Di.CpuBitsSet[registerList & 255] + Arm7Di.CpuBitsSet[(registerList >> 8) & 255] == 1)
                    {
                        if ((opcode & (1u << 20)) != 0)
                        {
                            var newBits = new ArmOpBits(0x04000000 | (opcode & 0xf0000000))
                            {
                                PreIndex = bits.PreIndex,
                                Up = bits.Up,
                                WriteBack = bits.WriteBack & bits.PreIndex,
                                Load = true,
                                Rn = bits.Rn,
                                Rd = FindFirstSet(registerList) - 1
                            };
                            newBits.Full |= 4;
                            Debug.WriteLine($"ARM: MEM TFX R {opcode:X8} -> {newBits.Full:X8}");
                            op = DecodeArmOp(newBits.Full, armPc);
                            op.Cycles += 4;
                            return op;
                        }
                        else
                        {
                            var newBits = new ArmOpBits(0x04000000 | (opcode & 0xf0000000))
                            {
                                PreIndex = bits.PreIndex,
                                Up = bits.Up,
                                WriteBack = bits.PreIndex,
                                Load = false,
                                Rn = bits.Rn,
                                Rd = FindFirstSet(registerList) - 1
                            };
                            newBits.Full |= 4;
                            Debug.WriteLine($"ARM: MEM TFX W {opcode:X8} -> {newBits.Full:X8}");
                            op = DecodeArmOp(newBits.Full, armPc);
                            op.Cycles += 4;
                            return op;
                        }
                    }
                    op.Type = OpType.FALLBACK;
                    op.Arg[0] = new Operand(opcode);
                    op.Cycles = 0;
                    if ((opcode & 0x8000) != 0 && bits.Load)
                        op.Flags |= (byte)OP_SETS_PC;
                }
                break;

            case 3:
                op.Type = OpType.FALLBACK;
                op.Arg[0] = new Operand(opcode);
                op.Cycles = 0;
                if (bits.ImmediateOperation && (bits.Operation & 8) != 0)
                    op.Flags |= (byte)OP_SETS_PC;
                break;
        }
        return op;
    }

    private static void BlockSsaPass()
    {
        Span<uint> versions = stackalloc uint[(int)RN_ARM_REG_COUNT];
        versions.Clear();
        for (int index = 0; index < BlockOperations.Count; index++)
        {
            ArmOp op = BlockOperations[index];
            if (op.Type == OpType.FALLBACK)
            {
                foreach (ref uint version in versions)
                    version++;
            }
            else
            {
                if ((op.Type == OpType.STR || op.Type == OpType.LDR) && op.WriteBack)
                {
                    if (op.Type == OpType.LDR && !op.PreIndex && op.Arg[1].IsReg()
                        && op.Arg[1].GetReg().ArmReg == op.Rd.GetReg().ArmReg)
                    {
                        var newOp = new ArmOp(OpType.MOV, op.ConditionCode);
                        newOp.Rd = new Operand(RN_SCRATCH);
                        newOp.Arg[0] = op.Rd;
                        BlockOperations.Insert(index, newOp);
                        index++;
                        var newOp2 = new ArmOp(op.AddOffset ? OpType.ADD : OpType.SUB, op.ConditionCode);
                        newOp2.Rd = op.Arg[0];
                        newOp2.Arg[0] = newOp2.Rd;
                        newOp2.Arg[1] = op.Arg[1];
                        newOp2.Arg[1].SetReg(RN_SCRATCH);
                        if (op.Arg[1].ShiftType == ShiftOp.RRX && op.Arg[1].ShiftValue == 0)
                            newOp2.Flags |= (byte)OP_READS_FLAGS;
                        op.Flags &= unchecked((byte)~OP_READS_FLAGS);
                        op.WriteBack = false;
                        op.Arg[1] = new Operand();
                        BlockOperations[index] = op;
                        BlockOperations.Insert(index + 1, newOp2);
                        index--;
                        op = BlockOperations[index];
                    }
                    else
                    {
                        var newOp = new ArmOp(op.AddOffset ? OpType.ADD : OpType.SUB, op.ConditionCode);
                        newOp.Rd = op.Arg[0];
                        newOp.Arg[0] = newOp.Rd;
                        newOp.Arg[1] = op.Arg[1];
                        if (op.Arg[1].ShiftType == ShiftOp.RRX && op.Arg[1].ShiftValue == 0)
                            newOp.Flags |= (byte)OP_READS_FLAGS;
                        if (op.ConditionCode == Condition.AL)
                            op.Flags &= unchecked((byte)~OP_READS_FLAGS);
                        op.WriteBack = false;
                        op.Arg[1] = new Operand();
                        BlockOperations[index] = op;
                        if (op.PreIndex)
                            BlockOperations.Insert(index, newOp);
                        else
                            BlockOperations.Insert(index + 1, newOp);
                        op = BlockOperations[index];
                    }
                }
                for (int argumentIndex = 0; argumentIndex < 3; argumentIndex++)
                {
                    ref Operand argument = ref op.Arg[argumentIndex];
                    if (argument.IsReg())
                    {
                        ref Register register = ref argument.GetReg();
                        register.Version = (int)versions[(int)register.ArmReg];
                    }
                    if (!argument.ShiftImmediate)
                        argument.ShiftRegister.Version = (int)versions[(int)argument.ShiftRegister.ArmReg];
                }
                if (op.Rd.IsReg())
                {
                    ref Register register = ref op.Rd.GetReg();
                    register.Version = (int)++versions[(int)register.ArmReg];
                }
                BlockOperations[index] = op;
            }
        }
    }

    internal static nuint CompileEntry => (nuint)(delegate* unmanaged<void>)&CompileCallback;

    [UnmanagedCallersOnly]
    private static void CompileCallback() => Compile();

    internal static void Compile()
    {
        byte* result = IcPtr;
        uint pc = Arm7Di.Reg[(int)R15_ARM_NEXT].I;
        EntryPoints[(pc & (AicaInterface.AramSizeMax - 1)) / 4] = (nuint)WriteToExec(result);
        BlockOperations.Clear();
        uint cycles = 0;
        Debug.WriteLine($"ARM7 Block {pc:x}");
        for (uint operations = 0; operations < 32; operations++)
        {
            uint opcode = *(uint*)(AicaInterface.Ram + (pc & AicaInterface.RamMask));
#if false
            using var output = new System.IO.StringWriter();
            var disassembler = new Aarch32Disassembler(output, pc);
            disassembler.DecodeA32(opcode);
            Debug.WriteLine(output.ToString());
#endif
            ArmOp lastOp = DecodeArmOp(opcode, pc);
            cycles += lastOp.Cycles;
            pc += 4;

            if (opcode != 0 && opcode != 0x41474553)
            {
                if (lastOp.Type == OpType.FALLBACK)
                {
                    var armOp = new ArmOp(OpType.MOV, Condition.AL);
                    armOp.Rd = new Operand(RN_PC);
                    armOp.Arg[0] = new Operand(pc + 4);
                    BlockOperations.Add(armOp);
                }
                if ((lastOp.Flags & OP_SETS_PC) != 0)
                {
                    if (lastOp.ConditionCode != Condition.AL)
                    {
                        var armOp = new ArmOp(OpType.MOV, Condition.AL);
                        armOp.Rd = new Operand(R15_ARM_NEXT);
                        armOp.Arg[0] = new Operand(pc);
                        BlockOperations.Add(armOp);
                    }
                    if (lastOp.Type == OpType.BL)
                    {
                        var armOp = new ArmOp(OpType.MOV, lastOp.ConditionCode);
                        armOp.Rd = new Operand(RN_LR);
                        armOp.Arg[0] = new Operand(pc);
                        BlockOperations.Add(armOp);
                    }
                    BlockOperations.Add(lastOp);
                    Debug.WriteLine($"ARM: {pc:X6}: Block End {operations}");
                    break;
                }
                BlockOperations.Add(lastOp);
            }

            if (operations == 31)
            {
                var armOp = new ArmOp(OpType.MOV, Condition.AL);
                armOp.Rd = new Operand(R15_ARM_NEXT);
                armOp.Arg[0] = new Operand(pc);
                BlockOperations.Add(armOp);
                Debug.WriteLine($"ARM: {pc:X6}: Block split");
            }
        }

        BlockSsaPass();
        CompileX64Backend(BlockOperations, cycles);
        Debug.WriteLine($"arm7rec_compile done: 0x{(nuint)result:x},0x{(nuint)IcPtr:x}");
    }

    internal static void Flush()
    {
        IcPtr = ICache;
        FlushX64Backend();
        Sh4HostRuntime.Verify(ArmCompileCode != 0);
        for (int i = 0; i < EntryPoints.Length; i++)
            EntryPoints[i] = ArmCompileCode;
    }

    internal static void Init()
    {
#if SH4_NO_RWX_PAGES
        bool result = Sh4VirtualMemory.PrepareJitBlock(Arm7Tcb.Value, ICacheSize, out nuint address, out RxOffset);
#else
        bool result = Sh4VirtualMemory.PrepareJitBlock(Arm7Tcb.Value, ICacheSize, out nuint address);
#endif
        ICache = (byte*)address;
        Sh4HostRuntime.Verify(result);
        Flush();
    }

    internal static void Term()
    {
        try
        {
#if SH4_NO_RWX_PAGES
            if (ICache != null)
                Sh4VirtualMemory.ReleaseJitBlock((nuint)ICache, (nuint)WriteToExec(ICache), ICacheSize);
#else
            if (ICache != null && (nuint)ICache != Arm7Tcb.Value)
                Sh4VirtualMemory.ReleaseJitBlock((nuint)ICache, ICacheSize);
#endif
        }
        finally
        {
            ICache = null;
        }
    }

    private static uint DoMemOp(uint address, uint data, bool load, bool byteTransfer)
    {
        uint result = 0;
        if (load)
        {
            if (byteTransfer)
                result = AicaArmMemory.ReadMemory<byte>(address);
            else
                result = AicaArmMemory.ReadMemory<uint>(address);
        }
        else
        {
            if (byteTransfer)
                AicaArmMemory.WriteMemory(address, (byte)data);
            else
                AicaArmMemory.WriteMemory(address, data);
        }
        return result;
    }

    [UnmanagedCallersOnly]
    private static uint ReadByte(uint address, uint data) => DoMemOp(address, data, load: true, byteTransfer: true);

    [UnmanagedCallersOnly]
    private static uint ReadWord(uint address, uint data) => DoMemOp(address, data, load: true, byteTransfer: false);

    [UnmanagedCallersOnly]
    private static uint WriteByte(uint address, uint data) => DoMemOp(address, data, load: false, byteTransfer: true);

    [UnmanagedCallersOnly]
    private static uint WriteWord(uint address, uint data) => DoMemOp(address, data, load: false, byteTransfer: false);

    internal static nuint GetMemOp(bool load, bool byteTransfer)
    {
        if (load)
        {
            if (byteTransfer)
                return (nuint)(delegate* unmanaged<uint, uint, uint>)&ReadByte;
            return (nuint)(delegate* unmanaged<uint, uint, uint>)&ReadWord;
        }
        if (byteTransfer)
            return (nuint)(delegate* unmanaged<uint, uint, uint>)&WriteByte;
        return (nuint)(delegate* unmanaged<uint, uint, uint>)&WriteWord;
    }

}

internal static unsafe partial class Arm7Di
{
    internal static void Run(uint samples)
    {
        for (uint i = 0; i < samples; i++)
        {
            if (Enabled)
            {
                Reg[(int)CYCL_CNT].I += ArmCyclesPerSample;
                fixed (nuint* entryPoints = Arm7DiRecompiler.EntryPoints)
                    Arm7DiRecompiler.ArmMainLoop((nint)RegisterAddress, (nint)entryPoints);
            }
            AicaInterface.TimeStep();
        }
    }

    internal static void AvoidRaceCondition() =>
        Reg[(int)CYCL_CNT].I = (uint)Math.Max(unchecked((int)Reg[(int)CYCL_CNT].I), 50);
}
#endif
