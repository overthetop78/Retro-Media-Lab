
namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal sealed partial class Sh4Cycles
{
    internal Sh4Cycles(int cpuRatio = 1)
    {
        _cpuRatio = cpuRatio;
    }

    internal void Initialize(Sh4Context context)
    {
        _context = context;
    }

    internal void ExecuteCycles(ushort opcode)
    {
        _context.CycleCounter -= CountCycles(opcode);
    }

    internal void AddCycles(int cycles)
    {
        _context.CycleCounter -= cycles;
    }

    internal void AddReadAccessCycles(uint address, uint size)
    {
        _context.CycleCounter -= ReadAccessCycles(address, size);
    }

    internal void AddWriteAccessCycles(uint address, uint size)
    {
        _context.CycleCounter -= WriteAccessCycles(address, size);
    }

    internal void Reset()
    {
        _lastUnit = Sh4ExecutionUnit.CO;
        _memoryOperations = 0;
    }

    internal ulong Now()
    {
        return unchecked(_context.Scheduler.Now64() + Sh4Scheduler.TimeSlice - (ulong)_context.CycleCounter);
    }

    internal int ReadAccessCycles(uint address, uint size)
    {
        return ReadExternalAccessCycles(address, size) * 2 * _cpuRatio;
    }

    internal int WriteAccessCycles(uint address, uint size)
    {
        return WriteExternalAccessCycles(address, size) * 2 * _cpuRatio;
    }

    private Sh4ExecutionUnit _lastUnit = Sh4ExecutionUnit.CO;
    private readonly int _cpuRatio;
    private int _memoryOperations;
    private Sh4Context _context = null!;
}
