using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

[StructLayout(LayoutKind.Sequential)]
internal struct Sh4PreviousStatusRegister
{
    internal uint Status;

    internal bool Th { readonly get => (Status & (1u << 0)) != 0; set => SetBit(0, value); }
    internal bool S { readonly get => (Status & (1u << 1)) != 0; set => SetBit(1, value); }
    internal bool Q { readonly get => (Status & (1u << 8)) != 0; set => SetBit(8, value); }
    internal bool M { readonly get => (Status & (1u << 9)) != 0; set => SetBit(9, value); }
    internal bool Fd { readonly get => (Status & (1u << 15)) != 0; set => SetBit(15, value); }
    internal bool Bl { readonly get => (Status & (1u << 28)) != 0; set => SetBit(28, value); }
    internal bool Rb { readonly get => (Status & (1u << 29)) != 0; set => SetBit(29, value); }
    internal bool MD { readonly get => (Status & (1u << 30)) != 0; set => SetBit(30, value); }

    internal uint InterruptMask
    {
        readonly get => (Status >> 4) & 15;
        set => Status = (Status & ~0xF0u) | ((value & 15) << 4);
    }

    private void SetBit(int bit, bool enabled)
    {
        uint mask = 1u << bit;
        Status = enabled ? Status | mask : Status & ~mask;
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct Sh4StatusRegister
{
    internal const uint Mask = 0x700083F2;
    internal uint Status;
    private uint _test;

    internal bool T { readonly get => _test != 0; set => _test = value ? 1u : 0; }
    internal bool Th { readonly get => (Status & 1u) != 0; set => SetBit(0, value); }
    internal bool S { readonly get => (Status & (1u << 1)) != 0; set => SetBit(1, value); }
    internal bool Q { readonly get => (Status & (1u << 8)) != 0; set => SetBit(8, value); }
    internal bool M { readonly get => (Status & (1u << 9)) != 0; set => SetBit(9, value); }
    internal bool Fd { readonly get => (Status & (1u << 15)) != 0; set => SetBit(15, value); }
    internal bool Bl { readonly get => (Status & (1u << 28)) != 0; set => SetBit(28, value); }
    internal bool Rb { readonly get => (Status & (1u << 29)) != 0; set => SetBit(29, value); }
    internal bool MD { readonly get => (Status & (1u << 30)) != 0; set => SetBit(30, value); }

    internal uint InterruptMask
    {
        readonly get => (Status >> 4) & 15;
        set => Status = (Status & ~0xF0u) | ((value & 15) << 4);
    }

    internal readonly uint GetFull() => (Status & Mask) | _test;

    internal void SetFull(uint value)
    {
        Status = value & Mask;
        _test = value & 1;
    }

    private void SetBit(int bit, bool enabled)
    {
        uint mask = 1u << bit;
        Status = enabled ? Status | mask : Status & ~mask;
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct Sh4Fpscr
{
    internal uint Full;
    internal uint RoundingMode { readonly get => Full & 3; set => SetField(0, 3, value); }
    internal bool FlagInexact { readonly get => GetBit(2); set => SetBit(2, value); }
    internal bool FlagUnderflow { readonly get => GetBit(3); set => SetBit(3, value); }
    internal bool FlagOverflow { readonly get => GetBit(4); set => SetBit(4, value); }
    internal bool FlagDivideByZero { readonly get => GetBit(5); set => SetBit(5, value); }
    internal bool FlagInvalidOperation { readonly get => GetBit(6); set => SetBit(6, value); }
    internal bool EnableInexact { readonly get => GetBit(7); set => SetBit(7, value); }
    internal bool EnableUnderflow { readonly get => GetBit(8); set => SetBit(8, value); }
    internal bool EnableOverflow { readonly get => GetBit(9); set => SetBit(9, value); }
    internal bool EnableDivideByZero { readonly get => GetBit(10); set => SetBit(10, value); }
    internal bool EnableInvalidOperation { readonly get => GetBit(11); set => SetBit(11, value); }
    internal bool CauseInexact { readonly get => GetBit(12); set => SetBit(12, value); }
    internal bool CauseUnderflow { readonly get => GetBit(13); set => SetBit(13, value); }
    internal bool CauseOverflow { readonly get => GetBit(14); set => SetBit(14, value); }
    internal bool CauseDivideByZero { readonly get => GetBit(15); set => SetBit(15, value); }
    internal bool CauseInvalidOperation { readonly get => GetBit(16); set => SetBit(16, value); }
    internal bool CauseFpuError { readonly get => GetBit(17); set => SetBit(17, value); }
    internal bool Dn { readonly get => (Full & (1u << 18)) != 0; set => SetBit(18, value); }
    internal bool Pr { readonly get => (Full & (1u << 19)) != 0; set => SetBit(19, value); }
    internal bool Sz { readonly get => (Full & (1u << 20)) != 0; set => SetBit(20, value); }
    internal bool Fr { readonly get => (Full & (1u << 21)) != 0; set => SetBit(21, value); }

    private readonly bool GetBit(int bit) => (Full & (1u << bit)) != 0;
    private void SetBit(int bit, bool enabled) => SetField(bit, 1, enabled ? 1u : 0);
    private void SetField(int bit, uint mask, uint value) =>
        Full = (Full & ~(mask << bit)) | ((value & mask) << bit);
}

internal abstract partial class Sh4Executor
{
    internal abstract void Run();
    internal abstract void Start();
    internal abstract void Stop();
    internal abstract void Step();
    internal abstract void Reset(bool hard);
    internal abstract void Initialize();
    internal abstract void Terminate();
    internal abstract void ResetCache();
    internal abstract bool IsCpuRunning { get; }

    internal static partial Sh4Executor GetSh4Interpreter();
    internal static partial Sh4Executor GetSh4Recompiler();
}

internal static class Sh4ContextLayout
{
    internal const int Size = 512;
    internal const int Alignment = 64;
    internal const int StoreQueueSize = 32;
    internal const uint StoreQueueBuffer = 0;
    internal const uint ExtendedFloatingRegisters = 64;
    internal const uint FloatingRegisters = 128;
    internal const uint GeneralRegisters = 192;
    internal const uint Macl = 256;
    internal const uint Mach = 260;
    internal const uint BankedRegisters = 264;
    internal const uint Gbr = 296;
    internal const uint Ssr = 300;
    internal const uint Spc = 304;
    internal const uint Sgr = 308;
    internal const uint Dbr = 312;
    internal const uint Vbr = 316;
    internal const uint Pr = 320;
    internal const uint Fpul = 324;
    internal const uint Pc = 328;
    internal const uint DynamicPc = 332;
    internal const uint SrStatus = 336;
    internal const uint SrT = 340;
    internal const uint Fpscr = 344;
    internal const uint OldStatus = 348;
    internal const uint OldFpscr = 352;
    internal const uint CpuRunning = 356;
    internal const uint SchedulerNext = 360;
    internal const uint InterruptPending = 364;
    internal const uint TemporaryRegister = 368;
    internal const uint CycleCounter = 372;
    internal const uint StoreQueueWriter = 376;

    internal const int FpcbSize = 32 * 1024 * 1024 / 2;
    internal const int FpcbMask = FpcbSize - 1;
#if SH4_HOST_ARM
    internal const int FpcbPadding = 0x100000;
#else
    internal const int FpcbPadding = 0x10000;
#endif
    internal static nuint ControlBlockSize => (nuint)FpcbSize * (nuint)IntPtr.Size + (nuint)FpcbPadding;
}

[InlineArray(Sh4ContextLayout.StoreQueueSize)]
internal struct Sh4StoreQueueBuffer
{
    private byte _byte;
}

[InlineArray(2)]
internal struct Sh4StoreQueueBuffers
{
    private Sh4StoreQueueBuffer _buffer;
}

[InlineArray(16)]
internal struct Sh4FloatingRegisterBank
{
    private float _register;
}

[InlineArray(16)]
internal struct Sh4GeneralRegisterBank
{
    private uint _register;
}

[InlineArray(8)]
internal struct Sh4BankedRegisters
{
    private uint _register;
}

[InlineArray(64)]
internal struct Sh4RawContext
{
    private ulong _register;
}

[StructLayout(LayoutKind.Explicit, Size = Sh4ContextLayout.Size)]
internal struct Sh4ContextData
{
    [FieldOffset((int)Sh4ContextLayout.StoreQueueBuffer)] internal Sh4StoreQueueBuffers StoreQueues;
    [FieldOffset((int)Sh4ContextLayout.ExtendedFloatingRegisters)] internal Sh4FloatingRegisterBank ExtendedFloatingRegisters;
    [FieldOffset((int)Sh4ContextLayout.FloatingRegisters)] internal Sh4FloatingRegisterBank FloatingRegisters;
    [FieldOffset((int)Sh4ContextLayout.GeneralRegisters)] internal Sh4GeneralRegisterBank Registers;
    [FieldOffset((int)Sh4ContextLayout.Macl)] internal ulong Mac;
    [FieldOffset((int)Sh4ContextLayout.Macl)] internal uint Macl;
    [FieldOffset((int)Sh4ContextLayout.Mach)] internal uint Mach;
    [FieldOffset((int)Sh4ContextLayout.BankedRegisters)] internal Sh4BankedRegisters BankedRegisters;
    [FieldOffset((int)Sh4ContextLayout.Gbr)] internal uint Gbr;
    [FieldOffset((int)Sh4ContextLayout.Ssr)] internal uint Ssr;
    [FieldOffset((int)Sh4ContextLayout.Spc)] internal uint Spc;
    [FieldOffset((int)Sh4ContextLayout.Sgr)] internal uint Sgr;
    [FieldOffset((int)Sh4ContextLayout.Dbr)] internal uint Dbr;
    [FieldOffset((int)Sh4ContextLayout.Vbr)] internal uint Vbr;
    [FieldOffset((int)Sh4ContextLayout.Pr)] internal uint ProcedureRegister;
    [FieldOffset((int)Sh4ContextLayout.Fpul)] internal uint Fpul;
    [FieldOffset((int)Sh4ContextLayout.Pc)] internal uint ProgramCounter;
    [FieldOffset((int)Sh4ContextLayout.DynamicPc)] internal uint DynamicPc;
    [FieldOffset((int)Sh4ContextLayout.SrStatus)] internal Sh4StatusRegister StatusRegister;
    [FieldOffset((int)Sh4ContextLayout.Fpscr)] internal Sh4Fpscr Fpscr;
    [FieldOffset((int)Sh4ContextLayout.OldStatus)] internal Sh4PreviousStatusRegister OldStatusRegister;
    [FieldOffset((int)Sh4ContextLayout.OldFpscr)] internal Sh4Fpscr OldFpscr;
    [FieldOffset((int)Sh4ContextLayout.CpuRunning)] internal uint CpuRunning;
    [FieldOffset((int)Sh4ContextLayout.SchedulerNext)] internal int SchedulerNext;
    [FieldOffset((int)Sh4ContextLayout.InterruptPending)] internal uint PendingInterruptMask;
    [FieldOffset((int)Sh4ContextLayout.TemporaryRegister)] internal uint TemporaryRegister;
    [FieldOffset((int)Sh4ContextLayout.CycleCounter)] internal int CycleCounter;
    [FieldOffset((int)Sh4ContextLayout.StoreQueueWriter)] internal nuint StoreQueueWriter;
    [FieldOffset(0)] internal Sh4RawContext Raw;
}

internal readonly unsafe struct Sh4RecompilerControlBlock(nuint address)
{
    internal Span<nuint> BlockPointers => new((void*)address, Sh4ContextLayout.FpcbSize);

    internal Span<byte> Padding =>
        new((void*)(address + (nuint)Sh4ContextLayout.FpcbSize * (nuint)sizeof(nuint)),
            Sh4ContextLayout.FpcbPadding - Sh4ContextLayout.Size);

    internal ref Sh4ContextData Context =>
        ref *(Sh4ContextData*)(address + Sh4ContextLayout.ControlBlockSize - Sh4ContextLayout.Size);
}

internal sealed unsafe partial class Sh4Context(nuint nativeAddress, Sh4AddressSpace addressSpace)
{
    internal const int MainClock = 200_000_000;
    private static Sh4Context? _active;

    internal static Sh4Context Active => _active ?? throw new InvalidOperationException("No SH-4 context is active.");

    internal static Sh4Context? ExchangeActive(Sh4Context? next)
    {
        Sh4Context? previous = _active;
        _active = next;
        return previous;
    }

    internal static Sh4Context FromNative(nint address)
    {
        Sh4Context context = Active;
        if (context.NativeAddress != unchecked((nuint)address))
            throw new InvalidOperationException("The native address does not match the active SH-4 context.");
        return context;
    }

    internal nuint NativeAddress { get; } = nativeAddress;
    internal nuint RecompilerControlBlock => NativeAddress + Sh4ContextLayout.Size - Sh4ContextLayout.ControlBlockSize;
    internal nuint StoreQueueBufferEnd => NativeAddress + Sh4ContextLayout.Size;

    private ref Sh4ContextData Data => ref *(Sh4ContextData*)NativeAddress;

    internal Span<byte> StoreQueues
    {
        get
        {
            Span<Sh4StoreQueueBuffer> buffers = Data.StoreQueues;
            return MemoryMarshal.AsBytes(buffers);
        }
    }

    internal Span<float> ExtendedFloatingRegisters => Data.ExtendedFloatingRegisters;
    internal Span<float> FloatingRegisters => Data.FloatingRegisters;
    internal Span<uint> Registers => Data.Registers;

    internal ref ulong Mac => ref Data.Mac;
    internal ref uint Macl => ref Data.Macl;
    internal ref uint Mach => ref Data.Mach;

    internal Span<uint> BankedRegisters => Data.BankedRegisters;

    internal ref uint Gbr => ref Data.Gbr;
    internal ref uint Ssr => ref Data.Ssr;
    internal ref uint Spc => ref Data.Spc;
    internal ref uint Sgr => ref Data.Sgr;
    internal ref uint Dbr => ref Data.Dbr;
    internal ref uint Vbr => ref Data.Vbr;
    internal ref uint ProcedureRegister => ref Data.ProcedureRegister;
    internal ref uint Fpul => ref Data.Fpul;
    internal ref uint ProgramCounter => ref Data.ProgramCounter;
    internal ref uint DynamicPc => ref Data.DynamicPc;

    internal ref Sh4StatusRegister StatusRegister => ref Data.StatusRegister;
    internal ref Sh4StatusRegister Status => ref Data.StatusRegister;
    internal ref Sh4Fpscr Fpscr => ref Data.Fpscr;
    internal ref Sh4PreviousStatusRegister OldStatusRegister => ref Data.OldStatusRegister;
    internal ref Sh4Fpscr OldFpscr => ref Data.OldFpscr;

    internal bool CpuRunning
    {
        get => Data.CpuRunning != 0;
        set => Data.CpuRunning = value ? 1u : 0;
    }

    internal ref int SchedulerNext => ref Data.SchedulerNext;
    internal ref uint PendingInterruptMask => ref Data.PendingInterruptMask;
    internal bool InterruptPending => PendingInterruptMask != 0;
    internal ref uint TemporaryRegister => ref Data.TemporaryRegister;
    internal ref int CycleCounter => ref Data.CycleCounter;
    internal ref nuint StoreQueueWriter => ref Data.StoreQueueWriter;

    internal Span<ulong> Raw => Data.Raw;

    internal ref uint FloatingBits(int index)
    {
        Debug.Assert(index is >= 0 and <= 15);
        return ref MemoryMarshal.Cast<float, uint>(FloatingRegisters)[index];
    }

    internal ref ulong DoubleBits(int index)
    {
        Debug.Assert(index is >= 0 and <= 7);
        return ref MemoryMarshal.Cast<float, ulong>(FloatingRegisters)[index];
    }

    internal ref ulong ExtendedDoubleBits(int index)
    {
        Debug.Assert(index is >= 0 and <= 7);
        return ref MemoryMarshal.Cast<float, ulong>(ExtendedFloatingRegisters)[index];
    }

    internal double GetDouble(int index)
    {
        Debug.Assert((uint)index <= 7);
        DoubleRegister register = default;
        register.High = FloatingRegisters[index * 2];
        register.Low = FloatingRegisters[index * 2 + 1];
        return register.Value;
    }

    internal void SetDouble(int index, double value)
    {
        Debug.Assert((uint)index <= 7);
        DoubleRegister register = default;
        register.Value = value;
        FloatingRegisters[index * 2] = register.High;
        FloatingRegisters[index * 2 + 1] = register.Low;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct DoubleRegister
    {
        [FieldOffset(0)] internal double Value;
        [FieldOffset(0)] internal float Low;
        [FieldOffset(4)] internal float High;
    }

    internal Sh4AddressSpace AddressSpace => addressSpace;
    internal Sh4Platform Platform => addressSpace.Platform;
    internal Sh4Interrupts Interrupts => addressSpace.Interrupts;
    internal Sh4Mmu Mmu => addressSpace.Mmu;
    internal Sh4Scheduler Scheduler => addressSpace.Scheduler;
    internal Sh4OpcodeDescriptor[] OpcodeTable => addressSpace.OpcodeTable;
    internal Action<Sh4Context, uint> GetInterpreterOpcode(ushort opcode) => addressSpace.InterpreterHandlers[opcode];

    internal void Clear() => Raw.Clear();

    internal byte ReadMemory8(uint address) => addressSpace.ReadGuest8(address);
    internal ushort ReadMemory16(uint address) => addressSpace.ReadGuest16(address);
    internal uint ReadMemory32(uint address) => addressSpace.ReadGuest32(address);
    internal ulong ReadMemory64(uint address) => addressSpace.ReadGuest64(address);

    internal void WriteMemory8(uint address, byte value) => addressSpace.WriteGuest8(address, value);
    internal void WriteMemory16(uint address, ushort value) => addressSpace.WriteGuest16(address, value);
    internal void WriteMemory32(uint address, uint value) => addressSpace.WriteGuest32(address, value);
    internal void WriteMemory64(uint address, ulong value) => addressSpace.WriteGuest64(address, value);

    internal void SetStoreQueueWriteHandler() => addressSpace.SetStoreQueueWriteHandler();
    internal void WriteStoreQueue(uint address)
    {
        if (RuntimeInformation.ProcessArchitecture == Architecture.X86)
            ((delegate* unmanaged[Fastcall]<uint, nint, void>)StoreQueueWriter)(address, (nint)NativeAddress);
        else
            ((delegate* unmanaged<uint, nint, void>)StoreQueueWriter)(address, (nint)NativeAddress);
    }
    internal void RaiseException(uint pc, Sh4ExceptionEvent exception) => Interrupts.RaiseException(pc, exception);
}

internal enum Sh4ExceptionEvent : ushort
{
    PowerOnReset = 0,
    ManualReset = 0x20,
    TlbMissRead = 0x40,
    TlbMissWrite = 0x60,
    TlbInitialPageWrite = 0x80,
    TlbProtectionRead = 0xa0,
    TlbProtectionWrite = 0xc0,
    AddressErrorRead = 0xe0,
    AddressErrorWrite = 0x100,
    FpuError = 0x120,
    FpuDisabled = 0x800,
    SlotFpuDisabled = 0x820,
    TlbMultiHit = 0x140,
    Trap = 0x160,
    IllegalInstruction = 0x180,
    SlotIllegalInstruction = 0x1a0,
    NmiInterrupt = 0x1c0,
    UserBreak = 0x1e0,
    ExtInterrupt0 = 0x200,
    ExtInterrupt1 = 0x220,
    ExtInterrupt2 = 0x240,
    ExtInterrupt3 = 0x260,
    ExtInterrupt4 = 0x280,
    ExtInterrupt5 = 0x2a0,
    ExtInterrupt6 = 0x2c0,
    ExtInterrupt7 = 0x2e0,
    ExtInterrupt8 = 0x300,
    ExtInterrupt9 = 0x320,
    ExtInterruptA = 0x340,
    ExtInterruptB = 0x360,
    ExtInterruptC = 0x380,
    ExtInterruptD = 0x3a0,
    ExtInterruptE = 0x3c0,
    TMU0 = 0x400,
    TMU1 = 0x420,
    TMU2 = 0x440,
    TMU2_TICPI2 = 0x460,
    RTC_ATI = 0x480,
    RTC_PRI = 0x4a0,
    RTC_CUI = 0x4c0,
    SCI_ERI = 0x4e0,
    SCI_RXI = 0x500,
    SCI_TXI = 0x520,
    SCI_TEI = 0x540,
    WDT = 0x560,
    REF_RCMI = 0x580,
    REF_ROVI = 0x5a0,
    HUDI = 0x600,
    GPIO = 0x620,
    DMAC_DTME0 = 0x640,
    DMAC_DTME1 = 0x680,
    DMAC_DTME2 = 0x680,
    DMAC_DTME3 = 0x6a0,
    DMAC_DMAE = 0x6c0,
    SCIF_ERI = 0x700,
    SCIF_RXI = 0x720,
    SCIF_BRI = 0x740,
    SCIF_TXI = 0x760,
}
