using RetroMediaLab.Chips.Common.Compilation.X64;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH2.Compilation;

/// <summary>SH-2 payload emitted directly as Windows x64, with explicit architectural state.</summary>
/// <remarks>Integer operations follow PicoDrive cpu/sh2/compiler.c and cpu/drc/emit_x86.c,
/// revision 9f46ee4e9680d108e548fb427e7c7e1aa1bb9d5a. All 65536 encodings, including
/// illegal/slot exceptions, have native payloads. Bus and clock services are borrowed
/// from the CPU host; no payload calls a managed opcode handler.</remarks>
internal sealed partial class Sh2X64Block : IDisposable
{
    private readonly X64CodeBuffer _buffer;
    internal byte[] Code { get; }
    internal bool RequiresMemory { get; }
    internal bool ExecutesInstructions { get; }
    internal int LinkedEntryFetchWait { get; }
    internal bool TimingInstrumented { get; }
    /// <summary>Borrowed only while the CPU's execution lease defers disposal.</summary>
    internal nint EntryPointer => _buffer.DangerousGetHandle();
    private Sh2X64Block(byte[] code, bool memory, bool execution = false, int linkedEntryFetchWait = -1, bool timing = false)
    {
        Code = code; RequiresMemory = memory; ExecutesInstructions = execution; LinkedEntryFetchWait = linkedEntryFetchWait; TimingInstrumented = timing;
        // UNWIND_INFO v1: 16-byte prologue, SUB RSP,48 at +7, PUSH RDI/RSI/RBX at +3/+2/+1.
        // The table belongs to the executable allocation and is removed before it is freed.
        _buffer = X64CodeBuffer.Create(code, [1, 16, 4, 0, 7, 0x52, 3, 0x70, 2, 0x60, 1, 0x30]);
    }

    internal static Sh2X64Block? TryCompile(ReadOnlySpan<ushort> instructions, bool timing = false, bool cachedReads = false, int[]? contextOffsets = null, bool mappedReads = false)
    {
        if (instructions.IsEmpty || !OperatingSystem.IsWindows()
            || System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture != System.Runtime.InteropServices.Architecture.X64) return null;
        var emitter = new Emitter(timing: timing, cachedReads: cachedReads, contextOffsets: contextOffsets, mappedReads: mappedReads);
        foreach (ushort opcode in instructions) if (!emitter.Instruction(opcode)) return null;
        emitter.Finish();
        return new(emitter.Bytes.ToArray(), emitter.RequiresMemory, timing: timing);
    }

    internal unsafe void Execute(uint[] registers, Span<uint> state, nint memoryAccess = 0)
    {
        if (registers.Length < 16) throw new ArgumentException("Sixteen SH-2 registers are required.", nameof(registers));
        if (state.Length < Sh2X64Context.Words) throw new ArgumentException("Native SH-2 context is too short.", nameof(state));
        if (RequiresMemory && memoryAccess == 0) throw new ArgumentNullException(nameof(memoryAccess));
        fixed (uint* r = registers)
        fixed (uint* s = state) _buffer.Execute((nint)r, (nint)s, memoryAccess);
    }
    public void Dispose() => _buffer.Dispose();

    private sealed partial class Emitter
    {
        internal readonly List<byte> Bytes = [];
        internal bool RequiresMemory;
        private readonly Label _exit = new();
        private readonly bool _execution, _deferred, _timing, _cachedReads, _mappedReads;
        private readonly int[]? _contextOffsets;
        private Label? _instructionEnd;
        private Label? _regionDispatch;
        internal Emitter(bool execution = false, bool deferred = false, bool timing = false, bool cachedReads = false, int[]? contextOffsets = null, bool mappedReads = false)
        {
            _execution = execution; _deferred = deferred; _timing = timing; _cachedReads = cachedReads;
            _mappedReads = mappedReads;
            _contextOffsets = contextOffsets;
            // Preserve ABI nonvolatiles and reserve Windows shadow space. After
            // three pushes RSP is 16-byte aligned for borrowed bus callbacks.
            Bytes.AddRange([0x53, 0x56, 0x57, 0x48, 0x83, 0xec, 0x30]);
            Move(6, 1, true); Move(7, 2, true); Move(3, 8, true);
            if (_timing) { RequiresMemory = true; StartTiming(Sh2NativeTimingLayout.PayloadStart); }
        }
        internal void Finish()
        {
            Mark(_exit);
            if (_timing) EndTiming(Sh2NativeTimingLayout.PayloadStart, Sh2NativeTimingLayout.PayloadTicks, Sh2NativeTimingLayout.PayloadCalls);
            Bytes.AddRange([0x31, 0xc0, 0x48, 0x83, 0xc4, 0x30, 0x5f, 0x5e, 0x5b, 0xc3]);
        }
        // RCX/RDX are borrowed register/state pointers; scratch uses RAX/R8..R11.
        // RBX/RSI/RDI retain borrowed inputs across bus calls and are saved/restored
        // by the prologue/epilogue according to the Windows x64 ABI.
        private void Int32(int value) => Bytes.AddRange(BitConverter.GetBytes(value));
        private void Rex(int reg, int rm, bool wide = false)
        {
            byte rex = (byte)(0x40 | (wide ? 8 : 0) | ((reg >> 3) << 2) | (rm >> 3));
            if (rex != 0x40) Bytes.Add(rex);
        }
        private void Reg(byte op, int destination, int source, bool wide = false)
        { Rex(source, destination, wide); Bytes.Add(op); Bytes.Add((byte)(0xc0 | ((source & 7) << 3) | (destination & 7))); }
        private void Move(int destination, int source, bool wide = false) => Reg(0x89, destination, source, wide);
        private void Load(int destination, int pointer, int offset, bool wide = false)
        {
            bool narrow = pointer == 2 && _contextOffsets is not null && offset / 4 is Sh2X64Context.Sleeping or Sh2X64Context.Yield;
            if (pointer == 2 && _contextOffsets is not null) offset = _contextOffsets[offset / 4];
            Rex(destination, pointer, wide);
            if (narrow) Bytes.AddRange([0x0f, 0xb6]); else Bytes.Add(0x8b);
            Bytes.Add((byte)(0x80 | ((destination & 7) << 3) | (pointer & 7))); if ((pointer & 7) == 4) Bytes.Add(0x24); Int32(offset);
        }
        private void Store(int pointer, int offset, int source, bool wide = false)
        {
            bool narrow = pointer == 2 && _contextOffsets is not null && offset / 4 is Sh2X64Context.Sleeping or Sh2X64Context.Yield;
            if (pointer == 2 && _contextOffsets is not null) offset = _contextOffsets[offset / 4];
            Rex(source, pointer, wide); Bytes.Add(narrow ? (byte)0x88 : (byte)0x89);
            Bytes.Add((byte)(0x80 | ((source & 7) << 3) | (pointer & 7))); if ((pointer & 7) == 4) Bytes.Add(0x24); Int32(offset);
        }

        private void Timestamp()
        {
            Bytes.AddRange([0x0f, 0xae, 0xe8, 0x0f, 0x31]); // LFENCE; RDTSC
            Shift(4, 2, 32, true); Reg(0x09, 0, 2, true);
            Bytes.AddRange([0x0f, 0xae, 0xe8]); // Finish the observation before subsequent work.
            Move(2, 7, true);
        }
        private void StartTiming(int slot)
        {
            Timestamp(); Load(11, 3, 8, true); Store(11, slot * 8, 0, true);
        }
        private void AddTimingValue(int slot, int value)
        {
            Load(10, 11, slot * 8, true); Reg(0x01, 10, value, true); Store(11, slot * 8, 10, true);
        }
        private void AddTimingCount(int slot)
        {
            Load(10, 11, slot * 8, true); Group(0, 10, 1, true); Store(11, slot * 8, 10, true);
        }
        private void EndTiming(int start, int ticks, int calls, int category = -1)
        {
            Timestamp(); Load(11, 3, 8, true); Load(10, 11, start * 8, true); Reg(0x29, 0, 10, true);
            AddTimingValue(ticks, 0); AddTimingCount(calls);
            if (category >= 0) { AddTimingValue(Sh2NativeTimingLayout.Categories + category * 2, 0); AddTimingCount(Sh2NativeTimingLayout.Categories + category * 2 + 1); }
        }
        private void LoadR(int host, int guest) => Load(host, 1, guest * 4);
        private void StoreR(int guest, int host = 0) => Store(1, guest * 4, host);
        private void LoadS(int host, int slot) => Load(host, 2, slot * 4);
        private void StoreS(int slot, int host = 0, bool wide = false) => Store(2, slot * 4, host, wide);
        private void Constant(int host, uint value)
        { Rex(0, host); Bytes.Add((byte)(0xb8 | (host & 7))); Int32(unchecked((int)value)); }
        private void Group(int operation, int host, uint value, bool wide = false)
        { Rex(0, host, wide); Bytes.Add(0x81); Bytes.Add((byte)(0xc0 | (operation << 3) | (host & 7))); Int32(unchecked((int)value)); }
        private void Shift(int operation, int host, byte count, bool wide = false)
        { Rex(0, host, wide); Bytes.Add(0xc1); Bytes.Add((byte)(0xc0 | (operation << 3) | (host & 7))); Bytes.Add(count); }
        private void Unary(int operation, int host)
        { Rex(0, host); Bytes.Add(0xf7); Bytes.Add((byte)(0xc0 | (operation << 3) | (host & 7))); }
        private void Extend(int host, bool sign, bool word)
        { Rex(host, host); Bytes.Add(0x0f); Bytes.Add((byte)(sign ? (word ? 0xbf : 0xbe) : (word ? 0xb7 : 0xb6))); Bytes.Add((byte)(0xc0 | ((host & 7) << 3) | (host & 7))); }
        private void SignExtend64(int host)
        { Rex(host, host, true); Bytes.Add(0x63); Bytes.Add((byte)(0xc0 | ((host & 7) << 3) | (host & 7))); }
        private void Multiply(bool wide)
        { Rex(0, 8, wide); Bytes.AddRange([0x0f, 0xaf, 0xc0]); }
        private void CarryFromT()
        { Bytes.AddRange([0x0f, 0xba, 0xa2]); Int32(_contextOffsets?[Sh2X64Context.Status] ?? Sh2X64Context.Status * 4); Bytes.Add(0); } // BT [RDX+SR],0
        private void T(byte condition)
        {
            Bytes.AddRange([0x41, 0x0f, (byte)(0x90 | condition), 0xc1, 0x45, 0x0f, 0xb6, 0xc9]); // SETcc R9B; MOVZX R9D,R9B
            LoadS(10, Sh2X64Context.Status); Group(4, 10, 0xfffffffe); Reg(0x09, 10, 9); StoreS(Sh2X64Context.Status, 10);
        }
        private void SetStatusBit(bool value)
        { LoadS(0, Sh2X64Context.Status); Group(value ? 1 : 4, 0, value ? 1u : 0xfffffffe); StoreS(Sh2X64Context.Status); }
        private sealed class Label { internal int Position = -1; internal readonly List<int> Sites = []; }
        private void Jump(Label label, byte? condition = null)
        {
            if (condition is { } cc) Bytes.AddRange([0x0f, (byte)(0x80 | cc)]); else Bytes.Add(0xe9);
            label.Sites.Add(Bytes.Count); Int32(0);
        }
        private void Mark(Label label)
        {
            label.Position = Bytes.Count;
            foreach (int site in label.Sites)
            { byte[] delta = BitConverter.GetBytes(label.Position - site - 4); for (int i = 0; i < 4; i++) Bytes[site + i] = delta[i]; }
        }

        internal bool Instruction(ushort opcode)
        {
            int n = opcode >> 8 & 15, m = opcode >> 4 & 15;
            switch (opcode)
            {
                case 0x0009: return true;
                case 0x0008: SetStatusBit(false); return true;
                case 0x0018: SetStatusBit(true); return true;
                case 0x0019: LoadS(0, 0); Group(4, 0, ~0x301u); StoreS(0); return true;
                case 0x0028: Constant(0, 0); StoreS(Sh2X64Context.MacLow, 0, true); return true;
            }
            if ((opcode & 0xf000) == 0xe000)
            { Constant(0, unchecked((uint)(sbyte)opcode)); StoreR(n); return true; }
            if ((opcode & 0xf000) == 0x7000)
            { LoadR(0, n); Group(0, 0, unchecked((uint)(sbyte)opcode)); StoreR(n); return true; }
            if ((opcode >> 8) is 0xc8 or 0xc9 or 0xca or 0xcb or 0x88)
            {
                LoadR(0, 0); uint immediate = (byte)opcode;
                switch (opcode >> 8)
                {
                    case 0xc8: Group(4, 0, immediate); Reg(0x85, 0, 0); T(4); return true;
                    case 0xc9: Group(4, 0, immediate); break;
                    case 0xca: Group(6, 0, immediate); break;
                    case 0xcb: Group(1, 0, immediate); break;
                    case 0x88: Group(7, 0, unchecked((uint)(sbyte)opcode)); T(4); return true;
                }
                StoreR(0); return true;
            }
            int operation = opcode & 0xf00f;
            switch (operation)
            {
                case 0x6003: LoadR(0, m); StoreR(n); return true;
                case 0x300c: case 0x3008: case 0x2009: case 0x200a: case 0x200b:
                    LoadR(0, n); LoadR(8, m);
                    Reg(operation switch { 0x300c => 0x01, 0x3008 => 0x29, 0x2009 => 0x21, 0x200a => 0x31, _ => (byte)0x09 }, 0, 8);
                    StoreR(n); return true;
                case 0x300e: case 0x300a:
                    LoadR(0, n); LoadR(8, m); CarryFromT(); Reg(operation == 0x300e ? (byte)0x11 : (byte)0x19, 0, 8);
                    T(2); StoreR(n); return true;
                case 0x300f: case 0x300b:
                    LoadR(0, n); LoadR(8, m); Reg(operation == 0x300f ? (byte)0x01 : (byte)0x29, 0, 8);
                    T(0); StoreR(n); return true;
                case 0x3000: case 0x3002: case 0x3003: case 0x3006: case 0x3007: case 0x2008:
                    LoadR(0, n); LoadR(8, m); Reg(operation == 0x2008 ? (byte)0x85 : (byte)0x39, 0, 8);
                    T(operation switch { 0x3000 or 0x2008 => 4, 0x3002 => 3, 0x3003 => 13, 0x3006 => 7, _ => (byte)15 }); return true;
                case 0x6007: case 0x600b: case 0x600a:
                    LoadR(0, m);
                    if (operation == 0x6007) Unary(2, 0);
                    else if (operation == 0x600b) Unary(3, 0);
                    else { Move(8, 0); Constant(0, 0); CarryFromT(); Reg(0x19, 0, 8); T(2); }
                    StoreR(n); return true;
                case 0x600c: case 0x600d: case 0x600e: case 0x600f:
                    LoadR(0, m); Extend(0, operation is 0x600e or 0x600f, operation is 0x600d or 0x600f); StoreR(n); return true;
                case 0x6008: case 0x6009:
                    LoadR(0, m);
                    if (operation == 0x6008) Bytes.AddRange([0x66, 0xc1, 0xc8, 8]); else Shift(1, 0, 16);
                    StoreR(n); return true;
                case 0x200d:
                    LoadR(0, n); LoadR(8, m); Shift(5, 0, 16); Shift(4, 8, 16); Reg(0x09, 0, 8); StoreR(n); return true;
                case 0x200c:
                    LoadR(0, n); LoadR(8, m); Reg(0x31, 0, 8); Move(8, 0); Group(5, 0, 0x01010101);
                    Unary(2, 8); Reg(0x21, 0, 8); Group(4, 0, 0x80808080); T(5); return true;
                case 0x0007: case 0x200e: case 0x200f: case 0x3005: case 0x300d:
                    LoadR(0, n); LoadR(8, m);
                    if (operation is 0x200e or 0x200f) { Extend(0, operation == 0x200f, true); Extend(8, operation == 0x200f, true); }
                    if (operation == 0x300d) { SignExtend64(0); SignExtend64(8); }
                    Multiply(operation is 0x3005 or 0x300d); StoreS(Sh2X64Context.MacLow, 0, operation is 0x3005 or 0x300d); return true;
                case 0x2007: DivideInitial(n, m); return true;
                case 0x3004: DivideStep(n, m); return true;
            }
            operation = opcode & 0xf0ff;
            switch (operation)
            {
                case 0x0029: LoadS(0, Sh2X64Context.Status); Group(4, 0, 1); StoreR(n); return true;
                case 0x4010:
                    LoadR(0, n); Group(5, 0, 1); T(4); StoreR(n);
                    LoadS(0, Sh2X64Context.PollingEpoch); Group(0, 0, 1); StoreS(Sh2X64Context.PollingEpoch);
                    IdleCallback(0x300u | (uint)n, n); return true;
                case 0x4011: case 0x4015:
                    LoadR(0, n); Group(7, 0, 0); T(operation == 0x4011 ? (byte)13 : (byte)15); return true;
                case 0x4000: case 0x4020: case 0x4001: case 0x4021: case 0x4004: case 0x4005: case 0x4024: case 0x4025:
                    LoadR(0, n);
                    if (operation is 0x4024 or 0x4025) CarryFromT();
                    Shift(operation switch { 0x4000 or 0x4020 => 4, 0x4001 => 5, 0x4021 => 7, 0x4004 => 0, 0x4005 => 1, 0x4024 => 2, _ => 3 }, 0, 1);
                    T(2); StoreR(n); return true;
                case 0x4008: case 0x4018: case 0x4028: case 0x4009: case 0x4019: case 0x4029:
                    LoadR(0, n); Shift((operation & 1) == 0 ? 4 : 5, 0, (byte)((operation >> 4 & 3) switch { 0 => 2, 1 => 8, _ => 16 })); StoreR(n); return true;
                case 0x0002: case 0x0012: case 0x0022: case 0x000a: case 0x001a: case 0x002a:
                    LoadS(0, operation switch { 0x0002 => 0, 0x0012 => 1, 0x0022 => 2, 0x000a => 5, 0x001a => 4, _ => 3 }); StoreR(n);
                    Constant(0, 1); StoreS(Sh2X64Context.IrqDelay); return true;
                case 0x400e: case 0x401e: case 0x402e: case 0x400a: case 0x401a: case 0x402a:
                    LoadR(0, n); if (operation == 0x400e) Group(4, 0, 0x3f3);
                    StoreS(operation switch { 0x400e => 0, 0x401e => 1, 0x402e => 2, 0x400a => 5, 0x401a => 4, _ => 3 });
                    Constant(0, 1); StoreS(Sh2X64Context.IrqDelay); return true;
            }
            return MemoryInstruction(opcode);
        }

        private void DivideInitial(int n, int m)
        {
            LoadR(0, n); LoadR(8, m); Shift(5, 0, 31); Shift(5, 8, 31);
            LoadS(9, Sh2X64Context.Status); Group(4, 9, ~0x301u);
            Move(10, 0); Shift(4, 10, 8); Reg(0x09, 9, 10);
            Move(11, 8); Shift(4, 11, 9); Reg(0x09, 9, 11);
            Reg(0x31, 0, 8); Reg(0x09, 9, 0); StoreS(Sh2X64Context.Status, 9);
        }
        private void DivideStep(int n, int m)
        {
            LoadR(0, n); Move(10, 0); Shift(5, 10, 31); LoadS(9, Sh2X64Context.Status);
            Move(11, 9); Shift(5, 11, 8); Move(8, 9); Shift(5, 8, 9); Reg(0x31, 11, 8); Group(4, 11, 1);
            Shift(4, 0, 1); Move(8, 9); Group(4, 8, 1); Reg(0x09, 0, 8); LoadR(8, m);
            var add = new Label(); var finish = new Label(); Reg(0x85, 11, 11); Jump(add, 5);
            Reg(0x29, 0, 8, true); Jump(finish); Mark(add); Reg(0x01, 0, 8, true); Mark(finish); StoreR(n);
            Move(11, 0, true); Shift(5, 11, 32, true); Group(4, 11, 1);
            Move(8, 9); Shift(5, 8, 9); Group(4, 8, 1); Reg(0x31, 10, 8); Reg(0x31, 10, 11);
            Reg(0x31, 8, 10); Group(6, 8, 1); Group(4, 9, ~0x101u); Shift(4, 10, 8); Reg(0x09, 9, 10); Reg(0x09, 9, 8);
            StoreS(Sh2X64Context.Status, 9);
        }
    }
}
