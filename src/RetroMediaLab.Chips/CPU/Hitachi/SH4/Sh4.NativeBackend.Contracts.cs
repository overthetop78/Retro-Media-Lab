namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

#if !SH4_DYNAREC_NONE
internal enum Sh4CanonicalParameterType
{
    UInt32,
    UInt32Return,
    UInt64LowReturn,
    UInt64HighReturn,
    Float32,
    Float32Return,
    RegisterPointer,
    ContextPointer
}

internal sealed partial class Sh4CodeBuffer
{
    internal nuint ToExecutable(nuint writable)
    {
#if SH4_NO_RWX_PAGES || SH4_SWITCH
        return unchecked(writable + (nuint)ExecutableOffset);
#else
        return writable;
#endif
    }

    internal nuint ToWritable(nuint executable)
    {
#if SH4_NO_RWX_PAGES || SH4_SWITCH
        return unchecked(executable - (nuint)ExecutableOffset);
#else
        return executable;
#endif
    }

    private uint _lastAddress;
    private uint _temporaryLastAddress;
    internal bool UseTemporaryBuffer { get; set; }
}

internal abstract class Sh4NativeBackend
{
    internal abstract void Initialize(Sh4Context context, Sh4CodeBuffer codeBuffer);
    internal abstract void Compile(Sh4RuntimeBlockInfo block, bool blockCheck, bool optimize);
    internal virtual void Reset() { }
    internal abstract void MainLoop(nuint controlBlock);
    internal abstract void HandleException(ref Sh4HostContext context);
    internal abstract bool Rewrite(ref Sh4HostContext context, nuint faultAddress);

    internal virtual Sh4RuntimeBlockInfo AllocateBlock() => new(Sh4RecompilerDriver.Current.BlockManager);

    internal abstract void CanonicalStart(Sh4ShilOpcode opcode);
    internal abstract void CanonicalParameter(Sh4ShilOpcode opcode, Sh4ShilParameter parameter,
        Sh4CanonicalParameterType parameterType);
    internal abstract void CanonicalCall(Sh4ShilOpcode opcode, nuint function);
    internal abstract void CanonicalFinish(Sh4ShilOpcode opcode);

    internal static Sh4NativeBackend? Current { get; set; }
}

internal sealed partial class Sh4RecompilerDriver : Sh4Interpreter
{
    internal static Sh4RecompilerDriver Current { get; private set; } = null!;
}
#endif
