using System.Diagnostics;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

using static Sh4Register;
using static Sh4ShilOperation;

#if !SH4_DYNAREC_NONE
internal sealed partial class Sh4SsaOptimizer(Sh4RuntimeBlockInfo block, Sh4RecompilerDriver? driver = null)
{
    private readonly Sh4RuntimeBlockInfo _block = block;
    private readonly SortedSet<RegisterValue> _writeBackValues = [];
    private Statistics _stats;
    private readonly uint[] _registerVersions = new uint[(int)sh4_reg_count];
    private readonly SortedDictionary<RegisterValue, uint> _constantValues = [];
    private int _operationIndex;
    private Sh4RecompilerDriver Driver => driver ?? throw new InvalidOperationException("SH-4 optimization requires a driver.");

    private readonly record struct RegisterValue(Sh4Register Register, uint Version) : IComparable<RegisterValue>
    {
        internal RegisterValue(Sh4ShilParameter operand, int index = 0)
            : this(operand.Register + index, operand.GetVersion(index))
        {
            Sh4HostRuntime.Verify(operand.IsRegister);
            Sh4HostRuntime.Verify(index >= 0 && index < operand.Count);
        }

        public int CompareTo(RegisterValue other)
        {
            int registerOrder = ((int)Register).CompareTo((int)other.Register);
            return registerOrder != 0 ? registerOrder : Version.CompareTo(other.Version);
        }
    }

    private void PrintBlock()
    {
        foreach (var opcode in _block.Opcodes)
            Trace.TraceInformation($"{_block.VirtualAddress + (uint)opcode.GuestOffset:x8}  {opcode.Disassemble()}");
    }

    private void AddVersion(ref Sh4ShilParameter operand, bool define)
    {
        if (!operand.IsRegister)
            return;

        if (define)
        {
            for (int i = 0; i < operand.Count; i++)
                _registerVersions[(int)operand.Register + i]++;
        }
        for (int i = 0; i < operand.Count; i++)
            operand.SetVersion(i, unchecked((ushort)_registerVersions[(int)operand.Register + i]));
    }

    private void ReplaceByMove(Sh4ShilOpcode opcode, uint value)
    {
        Sh4HostRuntime.Verify(opcode.Destination2.IsNull);
        opcode.Operation = shop_mov32;
        opcode.Source1 = new Sh4ShilParameter(value);
        if (!opcode.Source2.IsNull)
            opcode.Source2.ReplaceValue(Sh4ShilParameterType.FMT_NULL, opcode.Source2.Immediate);
        if (!opcode.Source3.IsNull)
            opcode.Source3.ReplaceValue(Sh4ShilParameterType.FMT_NULL, opcode.Source3.Immediate);
        _stats.ConstantOperationsReplaced++;
    }

    private static void ReplaceByMove(Sh4ShilOpcode opcode)
    {
        Sh4HostRuntime.Verify(opcode.Destination2.IsNull);
        opcode.Operation = shop_mov32;
        if (!opcode.Source2.IsNull)
            opcode.Source2.ReplaceValue(Sh4ShilParameterType.FMT_NULL, opcode.Source2.Immediate);
        if (!opcode.Source3.IsNull)
            opcode.Source3.ReplaceValue(Sh4ShilParameterType.FMT_NULL, opcode.Source3.Immediate);
    }

    private void PropagateOperand(ref Sh4ShilParameter operand)
    {
        if (operand.Is32 && _constantValues.TryGetValue(new RegisterValue(operand), out uint value))
        {
            operand.ReplaceValue(Sh4ShilParameterType.FMT_IMM, value);
            _stats.PropagatedConstants++;
        }
    }

    private static bool DefinesHigherVersion(Sh4ShilParameter operand, RegisterValue value) =>
        operand.IsRegister && value.Register >= operand.Register && value.Register < operand.Register + operand.Count
        && operand.GetVersion(value.Register - operand.Register) > value.Version;

    private static bool UsesRegisterValue(Sh4ShilParameter operand, RegisterValue value) =>
        operand.IsRegister && value.Register >= operand.Register && value.Register < operand.Register + operand.Count
        && operand.GetVersion(value.Register - operand.Register) == value.Version;

    private static void ReplaceByAlias(ref Sh4ShilParameter operand, RegisterValue from, RegisterValue to)
    {
        if (operand.Is32 && operand.Register == from.Register)
        {
            Sh4HostRuntime.Verify(operand.GetVersion(0) == from.Version);
            operand.ReplaceValue(operand.Type, unchecked((uint)to.Register));
            operand.SetVersion(0, (ushort)to.Version);
        }
    }

    private static Sh4ShilParameter ChangeFormat(Sh4ShilParameter operand, Sh4ShilParameterType type)
    {
        operand.ReplaceValue(type, operand.Immediate);
        return operand;
    }

    private void SingleBranchTargetPass()
    {
        if (!_block.ReadOnly)
            return;

        uint branch = _block.BranchAddress;
        bool updateCycles = !SkipSingleBranchTarget(ref branch, updateCycles: true);
        _block.BranchAddress = branch;
        uint next = _block.NextAddress;
        SkipSingleBranchTarget(ref next, updateCycles);
        _block.NextAddress = next;
    }

    private struct Statistics
    {
        internal uint PropagatedConstants;
        internal uint ConstantOperationsReplaced;
        internal uint DeadCodeOperations;
        internal uint DeadRegisters;
        internal uint DynamicToStaticBlocks;
        internal uint WriteAfterWriteBlocks;
        internal uint CombinedShifts;
    }
}
#endif
