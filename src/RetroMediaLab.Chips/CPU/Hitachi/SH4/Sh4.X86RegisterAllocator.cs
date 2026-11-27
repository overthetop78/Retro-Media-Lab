using RetroMediaLab.Chips.Common.Compilation.X64;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

#if !SH4_DYNAREC_NONE
internal sealed partial class Sh4X86RegisterAllocator(Sh4X86Compiler compiler, Sh4Mmu mmu)
    : Sh4SsaRegisterAllocator(mmu, allocateVectorPairs: false)
{
    private readonly Sh4X86Compiler _compiler = compiler;

    protected override partial void Preload(Sh4Register guestRegister, int hostRegister);
    protected override partial void WriteBack(Sh4Register guestRegister, int hostRegister);
    protected override partial void PreloadFloat(Sh4Register guestRegister, int hostRegister);
    protected override partial void WriteBackFloat(Sh4Register guestRegister, int hostRegister);

    internal partial void Allocate(Sh4RuntimeBlockInfo block);

    internal X64Register MapRegister(Sh4ShilParameter parameter)
    {
        int register = MapGeneral(parameter);
        if (register == -1)
            Sh4HostRuntime.Die("Register not allocated");
        return (X64Register)register;
    }

    internal int MapXRegister(Sh4ShilParameter parameter, int index = 0)
    {
        int register = MapFloat(parameter, index);
        if (register == -1)
            Sh4HostRuntime.Die("VRegister not allocated");
        return register;
    }

    internal bool IsMapped(int xmm, int operationIndex) => IsFloatUsed(xmm);
}
#endif
