using System.Reflection;
using System.Reflection.Emit;

namespace RetroMediaLab.Chips.CPU.Zilog.Z80.Instruction;

public sealed partial class CpuZ80<TBus> where TBus : class, ICpuZ80Bus
{
    public bool UseDynamicBlocks { get; set; }
    public long DynamicBlockBuildCount { get; private set; }
    private readonly Dictionary<int, Action<CpuZ80<TBus>, uint>> _dynamicTraces = [];
    private readonly record struct TraceInstruction(int Address, int Opcode);

    public void RunDynamic(uint cycles)
    {
        if (!System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported) { Run(cycles); return; }
        while (_state.cycles < cycles) ExecuteDynamicTrace(cycles);
    }

    private bool PrepareDynamicInstruction(uint target)
    {
        if (_state.cycles >= target) return false;
        if (_state.irq_state != 0 && IFF1 != 0 && _state.after_ei == 0)
        {
            TakeInterrupt();
            if (_state.cycles >= target) return false;
        }
        return true;
    }

    private int FetchDynamicInstruction()
    {
        _state.after_ei = 0;
        R++;
        return ROP();
    }

    private void ExecuteDynamicTrace(uint target)
    {
        if (!PrepareDynamicInstruction(target)) return;
        int start = PC;
        if (_dynamicTraces.TryGetValue(start, out var compiled)) { compiled(this, target); return; }
        var trace = new List<TraceInstruction>(16);
        for (int i = 0; i < 16 && PrepareDynamicInstruction(target); i++)
        {
            int address = PC, opcode = FetchDynamicInstruction();
            trace.Add(new(address, opcode));
            Execute_op(opcode);
            if (PC == start || HALT != 0) break;
        }
        if (trace.Count == 0) return;
        if (_dynamicTraces.Count >= 4096) _dynamicTraces.Clear();
        _dynamicTraces[start] = CompileDynamicTrace(trace);
        DynamicBlockBuildCount++;
    }

    private bool BeginDynamicInstruction(uint target, int address, int opcode, int traceStart)
    {
        if (!PrepareDynamicInstruction(target) || PC != address) return false;
        int actual = FetchDynamicInstruction();
        if (actual != opcode)
        {
            // Other traces still check each opcode before executing it.
            _dynamicTraces.Remove(traceStart);
            Execute_op(actual);
            return false;
        }
        USE_CYCLES(cc_op[opcode]);
        return true;
    }

    private static Action<CpuZ80<TBus>, uint> CompileDynamicTrace(List<TraceInstruction> trace)
    {
        Type cpu = typeof(CpuZ80<TBus>);
        var method = new DynamicMethod("Z80_" + trace[0].Address.ToString("X4"), typeof(void), [cpu, typeof(uint)], cpu, true);
        ILGenerator il = method.GetILGenerator();
        Label exit = il.DefineLabel();
        MethodInfo begin = cpu.GetMethod(nameof(BeginDynamicInstruction), BindingFlags.Instance | BindingFlags.NonPublic)!;
        foreach (var instruction in trace)
        {
            il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldc_I4, instruction.Address); il.Emit(OpCodes.Ldc_I4, instruction.Opcode);
            il.Emit(OpCodes.Ldc_I4, trace[0].Address);
            il.Emit(OpCodes.Call, begin); il.Emit(OpCodes.Brfalse, exit);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, cpu.GetMethod("DynamicOpcode" + instruction.Opcode.ToString("X2"), BindingFlags.Instance | BindingFlags.NonPublic)!);
        }
        il.MarkLabel(exit); il.Emit(OpCodes.Ret);
        return method.CreateDelegate<Action<CpuZ80<TBus>, uint>>();
    }
}
