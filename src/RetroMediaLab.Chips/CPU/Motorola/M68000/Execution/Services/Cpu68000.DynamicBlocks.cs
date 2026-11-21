using System.Reflection;
using System.Reflection.Emit;

namespace RetroMediaLab.Chips.CPU.Motorola.M68000.Execution;

public sealed partial class Cpu68000<TBus> where TBus : class, ICpu68000Bus
{
    public bool UseDynamicBlocks { get; set; }
    public long DynamicBlockBuildCount { get; private set; }
    private readonly Dictionary<uint, Action<Cpu68000<TBus>, uint>> _dynamicTraces = [];
    private readonly record struct TraceInstruction(uint Address, ushort Opcode);

    public void RunDynamic(uint cycles)
    {
        if (!System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported) { Run(cycles); return; }
        if (unchecked((uint)_state.cycles) >= cycles) return;
        m68ki_check_interrupts();
        if (CPU_STOPPED != 0) { _state.cycles = unchecked((int)cycles); return; }
        _state.cycle_end = cycles;
        bool addressFault = false;
        try
        {
            while (true)
            {
                try
                {
                    if (addressFault) { addressFault = false; m68ki_exception_address_error(); }
                    while (unchecked((uint)_state.cycles) < cycles) ExecuteDynamicTrace(cycles);
                    break;
                }
                catch (AddressFault) { addressFault = true; }
            }
        }
        finally { _state.cycle_end = 0; }
    }

    private void ExecuteDynamicTrace(uint target)
    {
        uint start = REG_PC;
        if (_dynamicTraces.TryGetValue(start, out var compiled))
        {
            compiled(this, target);
            return;
        }
        var trace = new List<TraceInstruction>(16);
        for (int i = 0; i < 16 && unchecked((uint)_state.cycles) < target; i++)
        {
            uint address = REG_PC;
            FetchDynamicInstruction();
            trace.Add(new(address, (ushort)REG_IR));
            ExecuteInstruction();
            FinishDynamicInstruction();
            if (REG_PC == start || CPU_STOPPED != 0) break;
        }
        if (trace.Count == 0) return;
        // Bound owned generated code. Evicted delegates have no unmanaged owner.
        if (_dynamicTraces.Count >= 4096) _dynamicTraces.Clear();
        _dynamicTraces[start] = CompileDynamicTrace(trace);
        DynamicBlockBuildCount++;
    }

    private void FetchDynamicInstruction()
    {
        if (Configuration.TrackInstructionAddress) _state.prev_pc = REG_PC;
        REG_IR = m68ki_read_imm_16();
        int period = Configuration.RefreshPeriod;
        if (period > 0 && _state.cycles >= _state.refresh_cycles)
        {
            _state.refresh_cycles = unchecked(_state.cycles + period);
            _state.cycles = unchecked(_state.cycles + Configuration.RefreshStall);
        }
    }

    private void FinishDynamicInstruction() => USE_CYCLES(_instructionCycles[(int)REG_IR]);

    private bool BeginDynamicInstruction(uint target, uint address, int opcode, uint traceStart)
    {
        if (unchecked((uint)_state.cycles) >= target || REG_PC != address) return false;
        FetchDynamicInstruction();
        if (REG_IR == (uint)opcode) return true;
        // Fetch has already happened, including prefetch and bus side effects.
        // Execute the replacement exactly once before leaving this stale trace.
        // Unrelated traces remain valid and check their own opcodes on entry.
        _dynamicTraces.Remove(traceStart);
        ExecuteInstruction();
        FinishDynamicInstruction();
        return false;
    }

    private static Action<Cpu68000<TBus>, uint> CompileDynamicTrace(List<TraceInstruction> trace)
    {
        var cpu = typeof(Cpu68000<TBus>);
        var method = new DynamicMethod("M68000_" + trace[0].Address.ToString("X8"),
            typeof(void), [cpu, typeof(uint)], cpu, true);
        ILGenerator il = method.GetILGenerator();
        Label exit = il.DefineLabel();
        MethodInfo begin = cpu.GetMethod(nameof(BeginDynamicInstruction), BindingFlags.Instance | BindingFlags.NonPublic)!;
        MethodInfo finish = cpu.GetMethod(nameof(FinishDynamicInstruction), BindingFlags.Instance | BindingFlags.NonPublic)!;
        foreach (var instruction in trace)
        {
            il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldc_I4, unchecked((int)instruction.Address));
            il.Emit(OpCodes.Ldc_I4, (int)instruction.Opcode);
            il.Emit(OpCodes.Ldc_I4, unchecked((int)trace[0].Address));
            il.Emit(OpCodes.Call, begin); il.Emit(OpCodes.Brfalse, exit);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, DynamicInstructionHandlers[InstructionMap[instruction.Opcode]]);
            il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Call, finish);
        }
        il.MarkLabel(exit); il.Emit(OpCodes.Ret);
        return method.CreateDelegate<Action<Cpu68000<TBus>, uint>>();
    }

    // The existing decoder is the authority for opcode-to-handler mapping.
    // Read its direct call targets without duplicating the 68000 opcode table.
    private static readonly Lazy<MethodInfo[]> DynamicHandlers = new(ReadDynamicHandlers);
    private static MethodInfo[] DynamicInstructionHandlers => DynamicHandlers.Value;
    private static MethodInfo[] ReadDynamicHandlers()
    {
        MethodInfo decoder = typeof(Cpu68000<TBus>).GetMethod(nameof(ExecuteInstruction), BindingFlags.Instance | BindingFlags.NonPublic)!;
        byte[] code = decoder.GetMethodBody()!.GetILAsByteArray()!;
        var opcodes = typeof(OpCodes).GetFields(BindingFlags.Static | BindingFlags.Public)
            .Where(f => f.FieldType == typeof(OpCode)).Select(f => (OpCode)f.GetValue(null)!)
            .ToDictionary(op => unchecked((ushort)op.Value));
        var handlers = new List<MethodInfo>();
        for (int offset = 0; offset < code.Length;)
        {
            ushort value = code[offset++];
            if (value == 0xfe) value = (ushort)(0xfe00 | code[offset++]);
            OpCode opcode = opcodes[value];
            if (opcode == OpCodes.Call && decoder.Module.ResolveMethod(BitConverter.ToInt32(code, offset), typeof(Cpu68000<TBus>).GetGenericArguments(), null) is MethodInfo called
                && called.Name.StartsWith("m68k_op_", StringComparison.Ordinal)) handlers.Add(called);
            offset += opcode.OperandType switch
            {
                OperandType.InlineNone => 0,
                OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
                OperandType.InlineVar => 2,
                OperandType.InlineI8 or OperandType.InlineR => 8,
                OperandType.InlineSwitch => checked(4 + 4 * BitConverter.ToInt32(code, offset)),
                _ => 4
            };
        }
        int count = 0;
        foreach (ushort index in InstructionMap) count = Math.Max(count, index + 1);
        if (handlers.Count != count) throw new InvalidOperationException("The 68000 decoder handler map is inconsistent.");
        return handlers.ToArray();
    }
}
