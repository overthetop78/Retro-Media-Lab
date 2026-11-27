using System.Buffers.Binary;
using System.Diagnostics;
using RetroMediaLab.Chips.Common.Compilation.X64;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

#if !SH4_DYNAREC_NONE
internal sealed unsafe partial class Sh4X86Compiler : Sh4XbyakBaseCompiler
{
    private readonly Sh4X86RegisterAllocator _registers;
    private nuint _currentOperationIndex;
    private uint _canonicalStackSize;

    internal static nuint ExceptionHandlerAddress { get; private set; }

    internal Sh4X86Compiler(Sh4Context context, Sh4CodeBuffer codeBuffer)
        : base(context, codeBuffer, archX64: false)
    {
        _registers = new Sh4X86RegisterAllocator(this, context.Mmu);
    }

    internal Sh4X86Compiler(Sh4Context context, Sh4CodeBuffer codeBuffer, nuint writableCode)
        : base(context, codeBuffer, writableCode, archX64: false)
    {
        _registers = new Sh4X86RegisterAllocator(this, context.Mmu);
    }

    protected override Sh4SsaRegisterAllocator Allocator => _registers;
    protected override X64Register MapRegister(Sh4ShilParameter parameter) => _registers.MapRegister(parameter);
    protected override int MapXRegister(Sh4ShilParameter parameter, int index = 0) => _registers.MapXRegister(parameter, index);

    internal partial void Compile(Sh4RuntimeBlockInfo block, bool forceChecks, bool optimise);

    internal void StartCanonicalCall(Sh4ShilOpcode opcode) => _canonicalStackSize = 0;

    internal partial void AddCanonicalParameter(Sh4ShilOpcode opcode, Sh4ShilParameter parameter,
        Sh4CanonicalParameterType type);

    internal void EmitCanonicalCall(Sh4ShilOpcode opcode, nuint function) => GenerateCdeclCall(function);

    internal partial void FinishCanonicalCall(Sh4ShilOpcode opcode);

    internal void PreloadRegister(Sh4Register guest, X64Register host)
    {
        Debug.WriteLine($"RegPreload reg {(uint)guest} -> {Register32Name(host)}");
        EmitAbsoluteOperand(0x8B, (int)host, guest);
    }

    internal void WriteBackRegister(Sh4Register guest, X64Register host)
    {
        Debug.WriteLine($"RegWriteback reg {(uint)guest} <- {Register32Name(host)}");
        EmitAbsoluteOperand(0x89, (int)host, guest);
    }

    internal void PreloadFloat(Sh4Register guest, int host)
    {
        Debug.WriteLine($"RegPreload_FPU reg {(uint)guest} -> xmm{host}");
        EmitAbsoluteOperand(0x10, host, guest, scalarFloat: true);
    }

    internal void WriteBackFloat(Sh4Register guest, int host)
    {
        Debug.WriteLine($"RegWriteback_FPU reg {(uint)guest} <- xmm{host}");
        EmitAbsoluteOperand(0x11, host, guest, scalarFloat: true);
    }

    private static string Register32Name(X64Register register) => register switch
    {
        X64Register.Rax => "eax",
        X64Register.Rcx => "ecx",
        X64Register.Rdx => "edx",
        X64Register.Rbx => "ebx",
        X64Register.Rsp => "esp",
        X64Register.Rbp => "ebp",
        X64Register.Rsi => "esi",
        X64Register.Rdi => "edi",
        _ => register.ToString().ToLowerInvariant() + "d",
    };

    private void EmitAbsoluteOperand(byte opcode, int host, Sh4Register guest, bool scalarFloat = false)
    {
        uint address = (uint)(Context.NativeAddress + Sh4Shil.GetRegisterOffset(guest));
        if (scalarFloat)
            Emitter.Append([0xF3, 0x0F]);

        Span<byte> operand = stackalloc byte[6];
        operand[0] = opcode;
        operand[1] = (byte)(0x05 | host << 3);
        BinaryPrimitives.WriteUInt32LittleEndian(operand[2..], address);
        Emitter.Append(operand);
    }

    internal partial void GenerateMainLoop();
    internal partial uint RelinkBlock(Sh4RuntimeBlockInfo block);
    internal partial bool RewriteMemoryAccess(ref Sh4HostContext context);

    private partial void GenerateOpcode(Sh4RuntimeBlockInfo block, bool optimise, Sh4ShilOpcode opcode);
    private partial bool GenerateReadMemoryImmediate(Sh4ShilOpcode opcode, Sh4RuntimeBlockInfo block);
    private partial bool GenerateWriteMemoryImmediate(Sh4ShilOpcode opcode, Sh4RuntimeBlockInfo block);
    private partial void GenerateMemoryHandlers();
    private partial void AlignStack(int amount);
    private partial void GenerateMmuLookup(Sh4RuntimeBlockInfo block, Sh4ShilOpcode opcode, uint write);
    private partial void CheckBlock(bool selfModifyingCodeChecks, Sh4RuntimeBlockInfo block);
    private partial void FreezeXmm();
    private partial void ThawXmm();

    private void GenerateCdeclCall(nuint function)
    {
        FreezeXmm();
        CallAbsolute(function);
        ThawXmm();
    }

    private void GenerateCall(nuint function) => GenerateCdeclCall(function);
}
#endif
