namespace RetroMediaLab.Chips.CPU.Zilog.Z80.Instruction.Compilation;

internal enum Z80X64Field
{
    Pc, Sp, Af, Bc, De, Hl, Ix, Iy, Wz, Af2, Bc2, De2, Hl2,
    R, R2, Iff1, Iff2, Halt, Im, I, NmiState, NmiPending, IrqState, AfterEi, Cycles, Ea
}

/// <summary>
/// Actual CLR offsets from the pinned pc field. CpuZ80State also contains GC
/// references: native code accesses only these primitive fields, never the references.
/// </summary>
internal sealed class Z80X64Layout(int[] offsets)
{
    public int this[Z80X64Field field] => offsets[(int)field];
}
