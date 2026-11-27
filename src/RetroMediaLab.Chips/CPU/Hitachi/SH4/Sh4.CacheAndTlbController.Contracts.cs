namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal sealed partial class Sh4CacheAndTlbController(
    Sh4Context context, Sh4Mmu mmu, Sh4Executor executor, Sh4Configuration configuration,
    Sh4InstructionCache instructionCache, Sh4OperandCache operandCache)
    : Sh4RegisterBank(18)
{
    private readonly Sh4Context _context = context;
    private readonly Sh4Mmu _mmu = mmu;
    private readonly Sh4Executor _executor = executor;
    private readonly Sh4Configuration _configuration = configuration;
    private readonly Sh4InstructionCache _instructionCache = instructionCache;
    private readonly Sh4OperandCache _operandCache = operandCache;

    internal partial void WriteQueueAddress(uint address, uint value);
}
