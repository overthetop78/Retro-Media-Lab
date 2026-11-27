using RetroMediaLab.Chips.Common.Compilation.X64;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

#if !SH4_DYNAREC_NONE
internal sealed partial class Sh4X64RegisterAllocator(Sh4X64BlockCompiler compiler, Sh4Mmu mmu)
    : Sh4SsaRegisterAllocator(mmu, allocateVectorPairs: OperatingSystem.IsWindows())
{
    private static readonly int[] WindowsGeneralRegisters =
    [
        (int)X64Register.Rbx, (int)X64Register.Rbp, (int)X64Register.Rdi, (int)X64Register.Rsi,
        (int)X64Register.R12, (int)X64Register.R13, (int)X64Register.R14, (int)X64Register.R15, -1
    ];
    private static readonly int[] SystemVGeneralRegisters =
    [
        (int)X64Register.Rbx, (int)X64Register.Rbp, (int)X64Register.R12,
        (int)X64Register.R13, (int)X64Register.R14, (int)X64Register.R15, -1
    ];
    private static readonly int[] WindowsFloatRegisters = [6, 7, 8, 9, 10, 11, 12, 13, 14, 15, -1];
    private static readonly int[] SystemVFloatRegisters = [8, 9, 10, 11, -1];

    private readonly Sh4X64BlockCompiler _compiler = compiler;

    internal void Allocate(Sh4RuntimeBlockInfo block) => Allocate(block,
        OperatingSystem.IsWindows() ? WindowsGeneralRegisters : SystemVGeneralRegisters,
        OperatingSystem.IsWindows() ? WindowsFloatRegisters : SystemVFloatRegisters);

    protected override partial void Preload(Sh4Register guestRegister, int hostRegister);
    protected override partial void WriteBack(Sh4Register guestRegister, int hostRegister);
    protected override partial void PreloadFloat(Sh4Register guestRegister, int hostRegister);
    protected override partial void WriteBackFloat(Sh4Register guestRegister, int hostRegister);

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
