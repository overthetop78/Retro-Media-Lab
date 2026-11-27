namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal partial class Sh4Interpreter(Sh4AddressSpace addressSpace, Sh4Debugger debugger) : Sh4Executor
{
    internal override void ResetCache()
    {
    }

    internal Sh4Context Context
    {
        get => _context;
        private protected set => _context = value;
    }

    internal static Sh4Interpreter? Instance;

    private protected Sh4Context _context = null!;

    private readonly Sh4Cycles _cycles = new(InterpreterCpuRatio);

#if SH4_STRICT_MODE
    private const int InterpreterCpuRatio = 1;
#else
    private const int InterpreterCpuRatio = 8;
#endif

    private readonly Sh4AddressSpace _addressSpace = addressSpace;
    private readonly Sh4Debugger _debugger = debugger;
    private Action<Sh4Context, uint>[] _handlers => _addressSpace.InterpreterHandlers;
    private Sh4Interrupts _interrupts => _addressSpace.Interrupts;
    private Sh4InstructionCache _instructionCache => _addressSpace.InstructionCache;
    private Sh4OperandCache _operandCache => _addressSpace.OperandCache;

    internal Sh4Mmu Mmu => _addressSpace.Mmu;
    internal Sh4Scheduler Scheduler => _addressSpace.Scheduler;
    internal Sh4OpcodeDescriptor[] OpcodeTable => Sh4OpcodeTable.OpcodeDescriptors;
    internal float Sh4Clock => Sh4Context.MainClock;

    private ushort ReadInstruction16(uint address) => _addressSpace.ReadGuestInstruction(address);

    private void RaiseException(uint pc, Sh4ExceptionEvent exception) => _interrupts.RaiseException(pc, exception);
}
