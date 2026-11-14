namespace RetroMediaLab.Chips.CPU.Zilog.Z80.Instruction;

public struct CpuZ80State
{
    public CpuZ80Pair pc, sp, af, bc, de, hl, ix, iy, wz;
    public CpuZ80Pair af2, bc2, de2, hl2;
    public int r, r2, iff1, iff2, halt, im, i;
    public int nmi_state, nmi_pending, irq_state, after_ei;
    public uint cycles;
    public object? daisy;
    public Func<int, int>? irq_callback;
}

