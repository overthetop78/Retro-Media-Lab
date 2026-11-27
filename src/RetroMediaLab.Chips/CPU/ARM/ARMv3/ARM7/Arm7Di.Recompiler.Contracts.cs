using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using RetroMediaLab.Chips.CPU.Hitachi.SH4;

namespace RetroMediaLab.Chips.CPU.ARM.ARMv3.ARM7;

internal struct ArmOp
{
    internal enum OpType
    {
        AND, EOR, SUB, RSB, ADD, ADC, SBC, RSC,
        TST, TEQ, CMP, CMN, ORR, MOV, BIC, MVN,
        LDR, STR, B, BL, MSR, MRS, FALLBACK
    }

    internal enum Condition
    {
        EQ, NE, CS, CC, MI, PL, VS, VC, HI, LS, GE, LT, GT, LE, AL, UC = 15
    }

    internal enum ShiftOp
    {
        LSL, LSR, ASR, ROR, RRX = ROR
    }

    internal const uint OP_READS_FLAGS = 1;
    internal const uint OP_SETS_FLAGS = 4;
    internal const uint OP_SETS_PC = 8;

    private static readonly string[] ConditionLabels =
    [
        "eq", "ne", "cs", "cc", "mi", "pl", "vs", "vc", "hi", "ls", "ge", "lt", "gt", "le", "", "uc"
    ];

    private static readonly string[] OperationLabels =
    [
        "and", "eor", "sub", "rsb", "add", "adc", "sbc", "rsc",
        "tst", "teq", "cmp", "cmn", "orr", "mov", "bic", "mvn",
        "ldr", "str", "b", "bl", "msr", "mrs", "(fallback)"
    ];

    public ArmOp() : this(OpType.FALLBACK, Condition.AL)
    {
    }

    internal ArmOp(OpType type, Condition condition)
    {
        this = default;
        Type = type;
        ConditionCode = condition;
        Rd = new Operand();
        for (int index = 0; index < 3; index++)
            Arg[index] = new Operand();
        Cycles = 6;
        if (condition != Condition.AL)
            Flags |= (byte)OP_READS_FLAGS;
    }

    internal struct Register
    {
        public Register() : this((Arm7Register)0)
        {
        }

        internal Register(Arm7Register register) : this(register, 0)
        {
        }

        internal Register(Arm7Register register, int version)
        {
            ArmReg = register;
            Version = version;
        }

        internal Arm7Register ArmReg;
        internal int Version;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct Operand
    {
        private enum OperandType
        {
            None, Register, Immediate
        }

        [FieldOffset(0)] private OperandType _type;
        [FieldOffset(4)] private uint _immediate;
        [FieldOffset(4)] private Register _register;
        [FieldOffset(12)] internal ShiftOp ShiftType;
        [FieldOffset(16), MarshalAs(UnmanagedType.U1)] internal bool ShiftImmediate;
        [FieldOffset(20)] internal uint ShiftValue;
        [FieldOffset(20)] internal Register ShiftRegister;

        public Operand()
        {
            this = default;
            _type = OperandType.None;
            ShiftType = ShiftOp.LSL;
            ShiftImmediate = true;
        }

        internal Operand(uint value) : this()
        {
            _type = OperandType.Immediate;
            _immediate = value;
        }

        internal Operand(Arm7Register register) : this()
        {
            _type = OperandType.Register;
            _register = new Register(register, 0);
        }

        internal readonly bool IsImmediate() => _type == OperandType.Immediate;

        internal readonly bool IsReg() => _type == OperandType.Register;

        internal readonly bool IsNone() => _type == OperandType.None;

        internal readonly uint GetImmediate()
        {
            Sh4HostRuntime.Verify(IsImmediate());
            return _immediate;
        }

        internal void SetImmediate(uint value)
        {
            _type = OperandType.Immediate;
            _immediate = value;
        }

        [UnscopedRef]
        internal ref Register GetReg()
        {
            Sh4HostRuntime.Verify(IsReg());
            return ref _register;
        }

        [UnscopedRef]
        internal readonly ref readonly Register GetRegReadOnly()
        {
            Sh4HostRuntime.Verify(IsReg());
            return ref _register;
        }

        internal void SetReg(Arm7Register register, int version = 0)
        {
            _type = OperandType.Register;
            _register = new Register(register, version);
        }

        internal readonly bool IsShifted() => !ShiftImmediate || ShiftValue != 0 || ShiftType != ShiftOp.LSL;
    }

    [InlineArray(3)]
    internal struct OperandArray
    {
        private Operand _operand;
    }

    internal OpType Type;
    internal Operand Rd;
    internal OperandArray Arg;
    internal bool PreIndex;
    internal bool AddOffset;
    internal bool ByteTransfer;
    internal bool WriteBack;
    internal Condition ConditionCode;
    internal byte Flags;
    internal byte Cycles;
    internal bool Spsr;
    internal byte PsrMask;

    internal readonly bool IsLogicalOp() =>
        Type is OpType.AND or OpType.EOR or OpType.TST or OpType.TEQ
        || (int)Type >= (int)OpType.ORR && (int)Type <= (int)OpType.MVN;

    internal readonly bool IsCompOp() => (int)Type >= (int)OpType.TST && (int)Type <= (int)OpType.CMN;

    private readonly string ConditionToString() => ConditionLabels[(int)ConditionCode];

    private static string RegisterToString(in Register register) =>
        "r" + ((int)register.ArmReg).ToString(CultureInfo.InvariantCulture) + ":" + register.Version.ToString(CultureInfo.InvariantCulture);

    private static string ShiftToString(ShiftOp type, uint value)
    {
        switch (type)
        {
            case ShiftOp.LSL:
                return "LSL";
            case ShiftOp.LSR:
                return "LSR";
            case ShiftOp.ASR:
                return "ASR";
            case ShiftOp.ROR:
                return value == 0 ? "RRX" : "ROR";
            default:
                return "";
        }
    }

    private static string OperandToString(in Operand operand)
    {
        string text = "";
        if (operand.IsImmediate())
            text = "#" + operand.GetImmediate().ToString(CultureInfo.InvariantCulture);
        else if (operand.IsReg())
            text = RegisterToString(operand.GetRegReadOnly());

        if (operand.ShiftType != ShiftOp.LSL || operand.ShiftValue != 0)
        {
            text += ", " + ShiftToString(operand.ShiftType, operand.ShiftValue);
            if (operand.ShiftImmediate)
                text += " #" + operand.ShiftValue.ToString(CultureInfo.InvariantCulture);
            else
                text += " " + RegisterToString(operand.ShiftRegister);
        }

        return text;
    }

    public override readonly string ToString()
    {
        string text = OperationLabels[(int)Type];
        if ((int)Type <= (int)OpType.MVN)
        {
            if (!IsCompOp() && (Flags & OP_SETS_FLAGS) != 0)
                text += "s";
            text += ConditionToString();
            if (Rd.IsReg())
                text += " " + RegisterToString(Rd.GetRegReadOnly()) + ", ";
            else
                text += " ";
            if (!Arg[0].IsNone())
            {
                text += OperandToString(Arg[0]);
                if (!Arg[1].IsNone())
                {
                    text += ", " + OperandToString(Arg[1]);
                    if (!Arg[2].IsNone())
                        text += ", " + OperandToString(Arg[2]);
                }
            }
        }
        else if ((int)Type <= (int)OpType.STR)
        {
            if (ByteTransfer)
                text += "b";
            text += ConditionToString() + " ";
            if (Rd.IsReg())
                text += RegisterToString(Rd.GetRegReadOnly()) + ", ";
            if (Arg[2].IsReg())
                text += RegisterToString(Arg[2].GetRegReadOnly()) + ", ";
            text += "[" + OperandToString(Arg[0]);
            if (!PreIndex)
                text += "]";
            if (!Arg[1].IsNone())
                text += ", " + OperandToString(Arg[1]);
            if (PreIndex)
                text += "]";
            if (WriteBack)
                text += "!";
        }
        else if ((int)Type <= (int)OpType.BL)
        {
            text += ConditionToString() + " " + OperandToString(Arg[0]);
        }
        else if (Type == OpType.MSR)
        {
            text += ConditionToString() + " CPSR, " + OperandToString(Arg[0]);
        }
        else if (Type == OpType.MRS)
        {
            text += ConditionToString() + " " + OperandToString(Rd) + ", CPSR";
        }

        return text;
    }
}

internal abstract class ArmRegAlloc
{
    private struct RegisterAllocation
    {
        internal int HostRegister;
        internal ushort Version;
        internal bool Dirty;
        internal bool Temporary;
    }

    private readonly RegisterAllocation[] _allocations = new RegisterAllocation[(int)Arm7Register.RN_ARM_REG_COUNT];
    private readonly List<int> _hostRegisters = [];
    private readonly IReadOnlyList<ArmOp> _blockOperations;

    private void AllocateRegister(in ArmOp.Register register, bool write, bool temporary, uint operationIndex)
    {
        ref RegisterAllocation allocation = ref _allocations[(int)register.ArmReg];
        if (allocation.HostRegister == -1 || (allocation.Version != register.Version && !write))
        {
            if (_hostRegisters.Count == 0)
            {
                Arm7Register bestRegister = unchecked((Arm7Register)(-1));
                int bestRegisterUse = -1;
                for (int index = 0; index < _allocations.Length; index++)
                {
                    ref RegisterAllocation candidate = ref _allocations[index];
                    if (candidate.HostRegister == -1)
                        continue;
                    if (!candidate.Dirty)
                    {
                        int nextUse = NextUse((Arm7Register)index, candidate.Version, operationIndex);
                        if (nextUse == -1)
                        {
                            _hostRegisters.Add(candidate.HostRegister);
                            candidate.HostRegister = -1;
                            break;
                        }
                        if (nextUse != (int)operationIndex && nextUse > bestRegisterUse)
                        {
                            bestRegisterUse = nextUse;
                            bestRegister = (Arm7Register)index;
                        }
                    }
                }

                if (_hostRegisters.Count == 0)
                {
                    if (bestRegister == unchecked((Arm7Register)(-1)))
                    {
                        for (int index = 0; index < _allocations.Length; index++)
                        {
                            ref RegisterAllocation candidate = ref _allocations[index];
                            if (candidate.HostRegister == -1)
                                continue;
                            int nextUse = NextUse((Arm7Register)index, candidate.Version, operationIndex);
                            if (nextUse == -1)
                            {
                                bestRegister = (Arm7Register)index;
                                break;
                            }
                            if (nextUse != (int)operationIndex && nextUse > bestRegisterUse)
                            {
                                bestRegisterUse = nextUse;
                                bestRegister = (Arm7Register)index;
                            }
                        }
                        Sh4HostRuntime.Verify(bestRegister != unchecked((Arm7Register)(-1)));
                        Debug.WriteLine($"Flushing dirty register r{(int)bestRegister}");
                    }
                    FlushRegister((int)bestRegister);
                }
                Sh4HostRuntime.Verify(_hostRegisters.Count != 0);
            }
            allocation.HostRegister = _hostRegisters[^1];
            _hostRegisters.RemoveAt(_hostRegisters.Count - 1);
            allocation.Version = unchecked((ushort)register.Version);
            allocation.Dirty = write;
            allocation.Temporary = temporary;
            if (!write)
                LoadReg(allocation.HostRegister, register.ArmReg);
        }
        if (write)
        {
            allocation.Dirty = true;
            allocation.Version = unchecked((ushort)register.Version);
            allocation.Temporary = temporary;
        }
    }

    private bool NeedsWriteback(in ArmOp.Register register, uint operationIndex)
    {
        for (int index = (int)operationIndex; index < _blockOperations.Count; index++)
        {
            ArmOp operation = _blockOperations[index];
            if (operation.Type == ArmOp.OpType.FALLBACK)
                return true;
            if (operation.Rd.IsReg() && operation.Rd.GetReg().ArmReg == register.ArmReg)
            {
                if (operation.ConditionCode == ArmOp.Condition.AL)
                    return false;
                else
                    return true;
            }
        }

        return true;
    }

    private int NextUse(Arm7Register register, int version, uint operationIndex)
    {
        int GetIndexOrUnused(in ArmOp.Register operand, int index)
        {
            if (operand.Version == version)
                return index;
            else
                return -1;
        }

        for (int index = (int)operationIndex; index < _blockOperations.Count; index++)
        {
            ArmOp operation = _blockOperations[index];
            if (operation.Type == ArmOp.OpType.FALLBACK)
                return -1;
            for (int argumentIndex = 0; argumentIndex < 3; argumentIndex++)
            {
                ref readonly ArmOp.Operand argument = ref operation.Arg[argumentIndex];
                if (argument.IsReg() && argument.GetRegReadOnly().ArmReg == register)
                    return GetIndexOrUnused(argument.GetRegReadOnly(), index);
                if (!argument.ShiftImmediate && argument.ShiftRegister.ArmReg == register)
                    return GetIndexOrUnused(argument.ShiftRegister, index);
            }
            if (operation.Rd.IsReg() && operation.Rd.GetReg().ArmReg == register)
                return -1;
        }

        return -1;
    }

    private void FlushRegister(int index)
    {
        ref RegisterAllocation allocation = ref _allocations[index];
        if (allocation.Dirty)
        {
            StoreReg(allocation.HostRegister, (Arm7Register)index);
            allocation.Dirty = false;
        }
        _hostRegisters.Add(allocation.HostRegister);
        allocation.HostRegister = -1;
    }

    private void FlushAllRegisters()
    {
        for (int index = 0; index < _allocations.Length; index++)
            if (_allocations[index].HostRegister != -1)
                FlushRegister(index);
    }

    protected ArmRegAlloc(int maxRegisters, IReadOnlyList<ArmOp> blockOperations)
    {
        _blockOperations = blockOperations;
        for (int index = 0; index < _allocations.Length; index++)
            _allocations[index].HostRegister = -1;
        for (int index = 0; index < maxRegisters; index++)
            _hostRegisters.Add(index);
    }

    internal void Load(uint operationIndex)
    {
        ArmOp operation = _blockOperations[(int)operationIndex];
        if (operation.Type == ArmOp.OpType.FALLBACK)
        {
            FlushAllRegisters();
        }
        else
        {
            bool conditional = operation.ConditionCode != ArmOp.Condition.AL;
            for (int argumentIndex = 0; argumentIndex < 3; argumentIndex++)
            {
                ref readonly ArmOp.Operand argument = ref operation.Arg[argumentIndex];
                if (argument.IsReg())
                    AllocateRegister(argument.GetRegReadOnly(), false, conditional, operationIndex);
                if (!argument.ShiftImmediate)
                    AllocateRegister(argument.ShiftRegister, false, conditional, operationIndex);
            }
            if (operation.Rd.IsReg())
                AllocateRegister(operation.Rd.GetReg(), true, conditional, operationIndex);
        }
    }

    internal void Store(uint operationIndex)
    {
        ArmOp operation = _blockOperations[(int)operationIndex];
        if (operation.Type == ArmOp.OpType.FALLBACK)
            return;
        if (operation.ConditionCode != ArmOp.Condition.AL)
        {
            for (int index = 0; index < _allocations.Length; index++)
                if (_allocations[index].HostRegister != -1 && _allocations[index].Temporary)
                    FlushRegister(index);
        }
        else if (operation.Rd.IsReg() && NeedsWriteback(operation.Rd.GetReg(), operationIndex + 1))
        {
            Arm7Register register = operation.Rd.GetReg().ArmReg;
            ref RegisterAllocation allocation = ref _allocations[(int)register];
            StoreReg(allocation.HostRegister, register);
            allocation.Dirty = false;
        }
    }

    protected int Map(Arm7Register register) => _allocations[(int)register].HostRegister;

    protected abstract void LoadReg(int hostRegister, Arm7Register register);

    protected abstract void StoreReg(int hostRegister, Arm7Register register);
}

internal static unsafe partial class Arm7DiRecompiler
{
    private static byte* IcPtr;
    private static byte* ICache;
    internal const uint ICacheSize = 4 * 1024 * 1024;
    private static nint RxOffset;

    internal static void* CurrentCode() => IcPtr;

    internal static uint SpaceLeft() => unchecked((uint)(ICacheSize - (IcPtr - ICache)));

    internal static bool Empty() => IcPtr == ICache;

    internal static void Advance(uint size) => IcPtr += size;

    internal static void* WriteToExec(void* address) => (byte*)address + RxOffset;

    internal static void* ExecToWrite(void* address) => (byte*)address - RxOffset;
}
