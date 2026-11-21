using System.Buffers.Binary;
using RetroMediaLab.Chips.CPU.Hitachi.SH2;
using RetroMediaLab.Chips.CPU.Hitachi.SH2.Contracts;

namespace RetroMediaLab.Chips.Tests.CPU.Hitachi;

public sealed class Sh2Tests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SharedBodiesKeepRelocatedPcBranchesTimingAndCodeChanges(bool pcRelativeRead)
    {
        var a = new TimingMapBus(); var b = new TimingMapBus();
        var reference = new Sh2<TimingMapBus>(a);
        var compiled = Sh2<TimingMapBus>.CreateNative(b);
        ushort[] code = pcRelativeRead ? [0xd101, 0x0009, 0xaffc, 0x0009] : [0xe105, 0x7103, 0xaffc, 0x0009];
        foreach (uint address in new uint[] { 0x100, 0x200, 0x300 })
            foreach (var bus in new[] { a, b })
            {
                for (int i = 0; i < code.Length; i++) bus.Write16(address + (uint)(i * 2), code[i]);
                bus.Write32(address + 8, 0x12340000 + address);
            }
        reference.Reset(); compiled.Reset();
        compiled.UseCompiledBlocks = compiled.UseSharedCompiledBodies = compiled.UseX64RegisterBlocks = true;
        reference.AccelerateIdleLoops = compiled.AccelerateIdleLoops = false;
        void Compare(int budget)
        {
            reference.Execute(budget); compiled.Execute(budget);
            byte[] expected = new byte[Sh2<TimingMapBus>.ContextSize], actual = new byte[expected.Length];
            reference.SaveContext(expected); compiled.SaveContext(actual);
            Assert.Equal(expected, actual); Assert.Equal(a.Clocks, b.Clocks);
        }
        try
        {
            foreach (int wait in new[] { 2, 0, 2 })
            {
                a.SetFetchWaits(wait); b.SetFetchWaits(wait);
                foreach (uint address in new uint[] { 0x100, 0x200, 0x300 })
                {
                    reference.ProgramCounter = compiled.ProgramCounter = address;
                    foreach (int budget in new[] { 37, 1, 5, 17 }) Compare(budget);
                }
            }
            Assert.True(compiled.SharedCompiledBodyReuseCount > 0);
            Assert.True(compiled.CompiledBodyBuildCount < compiled.CompiledBlockBuildCount);
            a.Write16(0x100, 0xe10b); b.Write16(0x100, 0xe10b);
            a.SetFetchWaits(1); b.SetFetchWaits(1);
            reference.ProgramCounter = compiled.ProgramCounter = 0x100;
            Compare(47);
            long before = compiled.CompiledBodyBuildCount;
            compiled.ClearCodeCache();
            reference.ProgramCounter = compiled.ProgramCounter = 0x100;
            Compare(47);
            Assert.True(compiled.CompiledBodyBuildCount > before);
        }
        finally { try { compiled.ClearCodeCache(); } finally { reference.ClearCodeCache(); } }
    }

    [Theory]
    [InlineData(2, 8)]
    [InlineData(8, 4)]
    public void CompiledCacheCapacityControlsEvictionWithoutChangingExecution(int capacity, int builds)
    {
        var reference = new MemoryBus().Program(0xe001, 0xe002, 0xe003, 0xe004);
        var compiled = new MemoryBus().Program(0xe001, 0xe002, 0xe003, 0xe004);
        compiled.UseCompiledBlocks = true;
        compiled.CompiledBlockCacheCapacity = capacity;
        try
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => compiled.CompiledBlockCacheCapacity = 0);
            for (int pass = 0; pass < 2; pass++)
                for (uint index = 0; index < 4; index++)
                {
                    reference.ProgramCounter = compiled.ProgramCounter = 0x100 + index * 2;
                    reference.Execute(1); compiled.Execute(1);
                    byte[] expected = new byte[Sh2<MemoryBus>.ContextSize], actual = new byte[expected.Length];
                    reference.SaveContext(expected); compiled.SaveContext(actual);
                    Assert.Equal(expected, actual);
                }
            Assert.Equal(builds, compiled.CompiledBlockBuildCount);
            compiled.CompiledBlockCacheCapacity = capacity; // Same value keeps cached code.
            long before = compiled.CompiledBlockBuildCount;
            compiled.ProgramCounter = 0x106; compiled.Execute(1);
            Assert.Equal(before, compiled.CompiledBlockBuildCount);
            compiled.CompiledBlockCacheCapacity = capacity + 1; // Changing capacity releases all links/code.
            compiled.ProgramCounter = 0x106; compiled.Execute(1);
            Assert.Equal(before + 1, compiled.CompiledBlockBuildCount);
        }
        finally { try { compiled.ClearCodeCache(); } finally { reference.ClearCodeCache(); } }
    }

    [Fact]
    public void CompiledBlocksRevalidateFetchTimingsWhenCodeBytesStayIdentical()
    {
        var a = new TimingMapBus(); var b = new TimingMapBus();
        var reference = new Sh2<TimingMapBus>(a); var compiled = Sh2<TimingMapBus>.CreateNative(b);
        reference.Reset(); compiled.Reset(); compiled.UseCompiledBlocks = true;
        reference.AccelerateIdleLoops = compiled.AccelerateIdleLoops = false;
        try
        {
            foreach (int waits in new[] { 2, 0, 2, 1, 0 })
            {
                a.SetFetchWaits(waits); b.SetFetchWaits(waits);
                foreach (int budget in new[] { 47, 3, 8, 1, 17, 31, 5, 127 })
                {
                    reference.Execute(budget); compiled.Execute(budget);
                    byte[] expected = new byte[Sh2<TimingMapBus>.ContextSize], actual = new byte[expected.Length];
                    reference.SaveContext(expected); compiled.SaveContext(actual);
                    Assert.Equal(expected, actual); Assert.Equal(a.Clocks, b.Clocks);
                }
            }
            Assert.True(compiled.CompiledBlockBuildCount > 0);
        }
        finally { try { compiled.ClearCodeCache(); } finally { reference.ClearCodeCache(); } }
    }

    private sealed class TimingMapBus : ISh2Bus
    {
        private readonly byte[] _memory = new byte[65536];
        private int _waits;
        private uint _version;
        public int Clocks;
        public TimingMapBus()
        {
            Write32(0, 0x100); Write32(4, 0x8000);
            ushort[] code = [0xe101, 0x7101, 0x7201, 0xaffb, 0x0009];
            for (int i = 0; i < code.Length; i++) Write16((uint)(0x100 + i * 2), code[i]);
        }
        public void SetFetchWaits(int waits) { _waits = waits; _version++; }
        public uint FetchMapVersion => _version;
        public long CodeVersion(uint address) => _version;
        public ulong CodeEpoch(uint address) => (ulong)_version + 1;
        public bool DeferClockUpdates => true;
        public int MaximumWaitStates => 2;
        public int WaitStates(uint address, bool write) => write ? 0 : _waits;
        public bool TryMapFetch(uint address, out ReadOnlyMemory<byte> memory, out uint start, out int waitStates)
        { memory = _memory; start = 0; waitStates = _waits; return address < _memory.Length - 1; }
        public byte Read8(uint address) => _memory[address & 65535];
        public ushort Read16(uint address) => (ushort)(Read8(address) << 8 | Read8(address + 1));
        public uint Read32(uint address) => (uint)Read16(address) << 16 | Read16(address + 2);
        public void Write8(uint address, byte value) => _memory[address & 65535] = value;
        public void Write16(uint address, ushort value) { Write8(address, (byte)(value >> 8)); Write8(address + 1, (byte)value); }
        public void Write32(uint address, uint value) { Write16(address, (ushort)(value >> 16)); Write16(address + 2, (ushort)value); }
        public void AdvanceCycles(int cycles) => Clocks += cycles;
    }

    [Theory, InlineData(0), InlineData(2), InlineData(5)]
    public void LinkedBlocksKeepFetchWaitsAcrossBudgets(int waits)
    {
        var a = new MemoryBus { ReadWaitStates = waits, DeferClockUpdates = true };
        var b = new MemoryBus { ReadWaitStates = waits, MaximumWaitStates = waits, DeferClockUpdates = true, TrackCodeEpoch = true };
        var reference = a.Program(0xe001, 0x7101, 0xaffd, 9); b.Program(0xe001, 0x7101, 0xaffd, 9);
        var compiled = Sh2<MemoryBus>.CreateNative(b); compiled.Reset(); compiled.UseCompiledBlocks = true;
        foreach (int budget in new[] { 1, 3, 7, 19, 252, 997 })
        {
            reference.Execute(budget); compiled.Execute(budget);
            byte[] expected = new byte[Sh2<MemoryBus>.ContextSize], actual = new byte[expected.Length];
            reference.SaveContext(expected); compiled.SaveContext(actual); Assert.Equal(expected, actual); Assert.Equal(a.Clocks, b.Clocks);
        }
    }

    [Fact]
    public void FastPayloadStopsAtShortenedMmioDeadline()
    {
        var a = new MemoryBus { ReadWaitStates = 2, DeferClockUpdates = true, NextEventCycle = ulong.MaxValue };
        var b = new MemoryBus { ReadWaitStates = 2, MaximumWaitStates = 6, DeferClockUpdates = true, NextEventCycle = ulong.MaxValue, TrackCodeEpoch = true };
        var reference = a.Program(0x6012, 0x7001, 0xaffe, 0x0009);
        var compiled = b.Program(0x6012, 0x7001, 0xaffe, 0x0009);
        compiled.UseCompiledBlocks = true;
        foreach (MemoryBus memory in new[] { a, b })
        {
            memory.Write32(0x50 * 4, 0x200);
            memory.Write16(0x200, 0xe22a);
            memory.Write16(0x202, 0x0009);
            memory.Write32(0x500, 0x12345678);
        }
        reference.R[1] = compiled.R[1] = 0x500;
        reference.StatusRegister = compiled.StatusRegister = 0;
        int referenceEvents = 0, compiledEvents = 0;
        a.BeforeRead16 = address => { if (address == 0x500) a.NextEventCycle = (ulong)a.Clocks + 1; };
        b.BeforeRead16 = address => { if (address == 0x500) b.NextEventCycle = (ulong)b.Clocks + 1; };
        a.EventReached = () => { referenceEvents++; reference.SetInterrupt(6, 0x50); a.NextEventCycle = ulong.MaxValue; };
        b.EventReached = () => { compiledEvents++; compiled.SetInterrupt(6, 0x50); b.NextEventCycle = ulong.MaxValue; };

        reference.Execute(96);
        compiled.Execute(96);

        byte[] expected = new byte[Sh2<MemoryBus>.ContextSize], actual = new byte[expected.Length];
        reference.SaveContext(expected); compiled.SaveContext(actual);
        Assert.Equal(expected, actual);
        Assert.Equal(a.Bytes, b.Bytes);
        Assert.Equal(a.Clocks, b.Clocks);
        Assert.Equal(1, referenceEvents);
        Assert.Equal(referenceEvents, compiledEvents);
        Assert.Equal(42u, compiled.R[2]);
    }

    [Fact]
    public void CompiledBlockReadsUpdatedDataOutsideItsCodeRangeWithoutRebuild()
    {
        var a = new MemoryBus { TrackCodeEpoch = true };
        var b = new MemoryBus { TrackCodeEpoch = true };
        var reference = a.Program(0x6012, 0xaffe, 0x0009);
        var compiled = b.Program(0x6012, 0xaffe, 0x0009);
        compiled.UseCompiledBlocks = true;
        a.Write32(0x110, 0x11223344); b.Write32(0x110, 0x11223344);
        reference.R[1] = compiled.R[1] = 0x110;

        reference.Execute(1);
        compiled.Execute(1);
        Assert.Equal(0x11223344u, compiled.R[0]);
        long builtBlocks = compiled.CompiledBlockBuildCount;
        Assert.True(builtBlocks > 0);
        long codeVersion = b.CodeVersion(0x100);
        ulong codeEpoch = b.CodeEpoch(0x100);

        a.Write32(0x110, 0x55667788);
        b.Write32(0x110, 0x55667788);
        Assert.NotEqual(codeVersion, b.CodeVersion(0x100));
        Assert.Equal(codeEpoch, b.CodeEpoch(0x100));
        reference.ProgramCounter = compiled.ProgramCounter = 0x100;
        reference.R[0] = compiled.R[0] = 0;
        reference.Execute(1);
        compiled.Execute(1);

        byte[] expected = new byte[Sh2<MemoryBus>.ContextSize], actual = new byte[expected.Length];
        reference.SaveContext(expected); compiled.SaveContext(actual);
        Assert.Equal(expected, actual);
        Assert.Equal(a.Bytes, b.Bytes);
        Assert.Equal(a.Clocks, b.Clocks);
        Assert.Equal(0x55667788u, compiled.R[0]);
        Assert.Equal(builtBlocks, compiled.CompiledBlockBuildCount);
    }

    [Fact]
    public void CodeWriteBarrierStopsBlockAfterReadThatChangesNextInstruction()
    {
        var a = new MemoryBus(); var b = new MemoryBus { TrackCodeEpoch = true };
        var reference = a.Program(0x6012, 0xe207, 0xaffe, 9); b.Program(0x6012, 0xe207, 0xaffe, 9);
        var compiled = Sh2<MemoryBus>.CreateNative(b); compiled.Reset(); compiled.UseCompiledBlocks = true;
        a.BeforeRead16 = address => { if (address == 0x500) a.Write16(0x102, 0xe22a); };
        b.BeforeRead16 = address => { if (address == 0x500) b.Write16(0x102, 0xe22a); };
        reference.R[1] = compiled.R[1] = 0x500;
        reference.Execute(2); compiled.Execute(2);
        byte[] expected = new byte[Sh2<MemoryBus>.ContextSize], actual = new byte[expected.Length];
        reference.SaveContext(expected); compiled.SaveContext(actual); Assert.Equal(expected, actual); Assert.Equal(42u, compiled.R[2]);
    }
    [Theory, InlineData(17), InlineData(1000)]
    public void CompiledArithmeticPrefixesRespectDeferredClockInterrupts(int deadline)
    {
        ushort[] code = [..Enumerable.Repeat((ushort)0x7001, 64), 0xaffe, 9];
        var a = new MemoryBus { DeferClockUpdates = true, NextEventCycle = (ulong)deadline };
        var b = new MemoryBus { DeferClockUpdates = true, NextEventCycle = (ulong)deadline };
        var original = a.Program(code); b.Program(code);
        var compiled = Sh2<MemoryBus>.CreateNative(b); compiled.Reset(); compiled.UseCompiledBlocks = true;
        foreach (MemoryBus memory in new[] { a, b }) { memory.Write32(0x50 * 4, 0x600); memory.Write16(0x600, 0xe12a); memory.Write16(0x602, 0x001b); }
        original.StatusRegister = compiled.StatusRegister = 0;
        a.EventReached = () => { original.SetInterrupt(6, 0x50); a.NextEventCycle = ulong.MaxValue; };
        b.EventReached = () => { compiled.SetInterrupt(6, 0x50); b.NextEventCycle = ulong.MaxValue; };
        original.Execute(128); compiled.Execute(128);
        byte[] expected = new byte[Sh2<MemoryBus>.ContextSize], actual = new byte[expected.Length];
        original.SaveContext(expected); compiled.SaveContext(actual); Assert.Equal(expected, actual); Assert.Equal(a.Clocks, b.Clocks);
        Assert.Equal(a.Bytes, b.Bytes);
    }
    [Theory]
    [InlineData(Sh2NativeDispatch.Switch)]
    [InlineData(Sh2NativeDispatch.DelegateTable)]
    [InlineData(Sh2NativeDispatch.IndirectTable)]
    public void NativeDispatchMatchesReferenceForEveryOpcode(Sh2NativeDispatch dispatch)
    {
        var a = new MemoryBus(); var b = new MemoryBus();
        var reference = new Sh2<MemoryBus>(a); var candidate = Sh2<MemoryBus>.CreateNative(b, dispatch: dispatch);
        byte[] expected = new byte[Sh2<MemoryBus>.ContextSize], actual = new byte[expected.Length];
        for (int opcode = 0; opcode <= ushort.MaxValue; opcode++)
        {
            foreach (MemoryBus memory in new[] { a, b })
            { memory.Write32(0, 0x100); memory.Write32(4, 0x8000); memory.Write16(0x100, (ushort)opcode); memory.Write16(0x102, 9); }
            reference.Reset(); candidate.Reset(); reference.Execute(1); candidate.Execute(1);
            reference.SaveContext(expected); candidate.SaveContext(actual);
            if (!expected.AsSpan().SequenceEqual(actual)) Assert.Fail($"Opcode {opcode:X4}, dispatch {dispatch}");
        }
        Assert.Equal(a.Bytes, b.Bytes);
    }

    [Theory]
    [InlineData(Sh2NativeDispatch.Switch)]
    [InlineData(Sh2NativeDispatch.DelegateTable)]
    [InlineData(Sh2NativeDispatch.IndirectTable)]
    public void NativeDispatchPreservesInterruptsAndRestore(Sh2NativeDispatch dispatch)
    {
        var a = new MemoryBus(); var b = new MemoryBus();
        var reference = a.Program(0xe063, 0x4010, 0x8bfd, 0xaffe, 9);
        b.Bytes.AsSpan().Clear(); a.Bytes.CopyTo(b.Bytes, 0);
        var candidate = Sh2<MemoryBus>.CreateNative(b, dispatch: dispatch); candidate.Reset();
        byte[] expected = new byte[Sh2<MemoryBus>.ContextSize], actual = new byte[expected.Length];
        foreach (int budget in new[] { 1, 2, 13, 31, 7, 64 })
        {
            reference.Execute(budget); candidate.Execute(budget); reference.SaveContext(expected); candidate.SaveContext(actual); Assert.Equal(expected, actual);
        }
        reference.LoadContext(expected); candidate.LoadContext(actual);
        foreach (MemoryBus memory in new[] { a, b }) { memory.Write32(0x50 * 4, 0x200); memory.Write16(0x200, 0xe02a); }
        reference.StatusRegister = candidate.StatusRegister = 0; reference.SetInterrupt(6, 0x50); candidate.SetInterrupt(6, 0x50);
        reference.Execute(1); candidate.Execute(1); reference.SaveContext(expected); candidate.SaveContext(actual);
        Assert.Equal(expected, actual); Assert.Equal(42u, candidate.R[0]);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MappedFetchHonorsMappingChangesAndSelfModification(bool native)
    {
        var bus = new FetchMapBus();
        bus.Write32(0, 0x100); bus.Write32(4, 0x8000);
        bus.Write16(0x100, 0x2011); bus.Write16(0x102, 0xe007);
        bus.WriteAlternate16(0x102, 0xe02a);
        var cpu = native ? Sh2<FetchMapBus>.CreateNative(bus) : new Sh2<FetchMapBus>(bus);
        cpu.Reset(); cpu.R[0] = 0x300; cpu.R[1] = 0x1234; bus.SwitchOnWrite = true;
        cpu.Execute(2); Assert.Equal(42u, cpu.R[0]); Assert.Equal(0, bus.FallbackFetches);

        bus = new FetchMapBus();
        bus.Write32(0, 0x100); bus.Write32(4, 0x8000);
        bus.Write16(0x100, 0x2011); bus.Write16(0x102, 0xe007);
        cpu = native ? Sh2<FetchMapBus>.CreateNative(bus) : new Sh2<FetchMapBus>(bus);
        cpu.Reset(); cpu.R[0] = 0x102; cpu.R[1] = 0xe02a;
        cpu.Execute(2); Assert.Equal(42u, cpu.R[0]); Assert.Equal(0, bus.FallbackFetches);
    }

    private sealed class FetchMapBus : ISh2Bus
    {
        private byte[] _active = new byte[65536];
        private readonly byte[] _alternate = new byte[65536];
        public bool SwitchOnWrite;
        public int FallbackFetches;
        public uint FetchMapVersion { get; private set; }
        public bool TryMapFetch(uint address, out ReadOnlyMemory<byte> memory, out uint start, out int waitStates)
        { memory = _active; start = 0; waitStates = 0; return true; }
        public byte Read8(uint address) => _active[address & 65535];
        public ushort Read16(uint address) => (ushort)(Read8(address) << 8 | Read8(address + 1));
        public uint Read32(uint address) => (uint)Read16(address) << 16 | Read16(address + 2);
        public ushort Fetch16(uint address, out int waitStates) { FallbackFetches++; waitStates = 0; return Read16(address); }
        public void Write8(uint address, byte value) => _active[address & 65535] = value;
        public void Write16(uint address, ushort value)
        {
            Write8(address, (byte)(value >> 8)); Write8(address + 1, (byte)value);
            if (SwitchOnWrite && address == 0x300) { _active = _alternate; FetchMapVersion++; }
        }
        public void WriteAlternate16(uint address, ushort value)
        {
            _alternate[address] = (byte)(value >> 8);
            _alternate[address + 1] = (byte)value;
        }
        public void Write32(uint address, uint value) { Write16(address, (ushort)(value >> 16)); Write16(address + 2, (ushort)value); }
    }
    [Theory, InlineData(false), InlineData(true)]
    public void IdleLoopAccelerationPreservesInstructionBoundariesAndCounts(bool decrement)
    {
        ushort[] code = decrement ? [0xe063, 0x4010, 0x8bfd, 0x0009, 0xaffe, 0x0009] : [0xaffe, 0x0009];
        var original = new MemoryBus().Program(code); original.AccelerateIdleLoops = false;
        var accelerated = new MemoryBus().Program(code);
        foreach (int budget in new[] { 1, 2, 3, 13, 64, 55, 7, 41, 64, 64, 64, 64 })
        {
            original.Execute(budget); accelerated.Execute(budget);
            byte[] expected = new byte[Sh2<MemoryBus>.ContextSize], actual = new byte[Sh2<MemoryBus>.ContextSize];
            original.SaveContext(expected); accelerated.SaveContext(actual); Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void ThreeInstructionNopBranchLoopMatchesReferenceOnIrregularBudgets()
    {
        var reference = new MemoryBus().Program(0x0009, 0xaffd, 0x0009); reference.AccelerateIdleLoops = false;
        var accelerated = new MemoryBus().Program(0x0009, 0xaffd, 0x0009);
        foreach (int budget in new[] { 1, 2, 3, 4, 17, 64, 55, 7, 252, 61 })
        {
            reference.Execute(budget); accelerated.Execute(budget);
            byte[] expected = new byte[Sh2<MemoryBus>.ContextSize], actual = new byte[expected.Length];
            reference.SaveContext(expected); accelerated.SaveContext(actual); Assert.Equal(expected, actual);
        }
    }
    public sealed class MemoryBus : ISh2Bus
    {
        public byte[] Bytes { get; } = new byte[65536];
        public int Clocks;
        public int ReadWaitStates;
        public int MaximumWaitStates { get; set; } = -1;
        int ISh2Bus.MaximumWaitStates => MaximumWaitStates;
        public int WaitStates(uint address, bool write) => write ? 0 : ReadWaitStates;
        public bool TrackCodeEpoch;
        private ulong _codeEpoch;
        private readonly uint[] _compiledCodeWords = new uint[1024];
        public Action<uint>? BeforeRead16;
        public ulong CodeEpoch(uint address) => TrackCodeEpoch ? _codeEpoch + 1 : 0;
        public void MarkCode(uint address, int length)
        {
            if (!TrackCodeEpoch) return;
            for (int offset = 0; offset < length; offset += 2)
            { uint word = unchecked(address + (uint)offset) >> 1 & 32767; _compiledCodeWords[word >> 5] |= 1u << (int)(word & 31); }
        }
        public bool DeferClockUpdates { get; set; }
        public ulong NextEventCycle { get; set; } = ulong.MaxValue;
        public Action? EventReached;
        private readonly uint[] _versions = new uint[256];
        public long CodeVersion(uint address) => _versions[(address & 65535) >> 8];
        public byte Read8(uint a) => Bytes[a & 65535];
        public ushort Read16(uint a) { BeforeRead16?.Invoke(a); return (ushort)((Read8(a) << 8) | Read8(a + 1)); }
        public uint Read32(uint a) => (uint)Read16(a) << 16 | Read16(a + 2);
        public void Write8(uint a, byte v)
        {
            uint word = a >> 1 & 32767;
            if (TrackCodeEpoch && Bytes[a & 65535] != v && (_compiledCodeWords[word >> 5] & (1u << (int)(word & 31))) != 0) _codeEpoch++;
            _versions[(a & 65535) >> 8]++; Bytes[a & 65535] = v;
        }
        public void Write16(uint a, ushort v) { Write8(a, (byte)(v >> 8)); Write8(a + 1, (byte)v); }
        public void Write32(uint a, uint v) { Write16(a, (ushort)(v >> 16)); Write16(a + 2, (ushort)v); }
        public void AdvanceCycles(int n) { Clocks += n; if ((ulong)Clocks >= NextEventCycle) EventReached?.Invoke(); }
        public Sh2<MemoryBus> Program(params ushort[] words)
        {
            Write32(0, 0x100); Write32(4, 0x8000);
            for (int i = 0; i < words.Length; i++) Write16((uint)(0x100 + i * 2), words[i]);
            var cpu = new Sh2<MemoryBus>(this); cpu.Reset(); return cpu;
        }
    }

    [Fact]
    public void CompiledBlocksMatchTheInterpreterAndInvalidateModifiedCode()
    {
        ushort[] code = [0xe063, 0x4010, 0x8bfd, 0xa001, 0x7001, 0x7064, 0x7101, 0xaffe, 0x0009];
        var memoryA = new MemoryBus(); var original = memoryA.Program(code);
        var memoryB = new MemoryBus(); var compiled = memoryB.Program(code); compiled.UseCompiledBlocks = true;
        void Compare(int budget)
        {
            original.Execute(budget); compiled.Execute(budget);
            byte[] a = new byte[Sh2<MemoryBus>.ContextSize], b = new byte[Sh2<MemoryBus>.ContextSize];
            original.SaveContext(a); compiled.SaveContext(b); Assert.Equal(a, b);
        }
        foreach (int budget in new[] { 1, 13, 64, 55, 8, 64, 64, 64, 64, 64, 64, 64 }) Compare(budget);
        memoryA.Write16(0x10e, 0x7102); memoryB.Write16(0x10e, 0x7102);
        original.ProgramCounter = compiled.ProgramCounter = 0x10c;
        foreach (int budget in new[] { 1, 2, 13, 64 }) Compare(budget);
        byte[] state = new byte[Sh2<MemoryBus>.ContextSize]; compiled.SaveContext(state);
        Compare(64); compiled.LoadContext(state); original.LoadContext(state); Compare(64);
    }

    [Fact]
    public void ImmediateArithmeticAndDelaySlotSurviveStateRestore()
    {
        var bus = new MemoryBus(); var cpu = bus.Program(0xe005, 0x7003, 0xa001, 0x7001, 0x7064, 0x7002);
        cpu.Execute(1); cpu.Execute(1); cpu.Execute(1);
        byte[] saved = new byte[Sh2<MemoryBus>.ContextSize]; cpu.SaveContext(saved);
        cpu.Execute(1); cpu.Execute(1); Assert.Equal(11u, cpu.R[0]);
        byte[] result = new byte[saved.Length]; cpu.SaveContext(result);
        cpu.LoadContext(saved); cpu.Execute(1); cpu.Execute(1);
        byte[] replay = new byte[saved.Length]; cpu.SaveContext(replay); Assert.Equal(result, replay);
    }

    [Theory]
    [InlineData(0x301c, 0xffffffffu, 1u, 0u, false)]
    [InlineData(0x301e, 0xffffffffu, 0u, 0u, true)]
    [InlineData(0x301f, 0x7fffffffu, 1u, 0x80000000u, true)]
    [InlineData(0x301b, 0x80000000u, 1u, 0x7fffffffu, true)]
    [InlineData(0x301a, 0u, 0u, 0xffffffffu, true)]
    public void ArithmeticWrapsAndSetsCarryOrSignedOverflow(ushort opcode, uint a, uint b, uint expected, bool t)
    {
        var bus = new MemoryBus(); var cpu = bus.Program(opcode); cpu.R[0] = a; cpu.R[1] = b;
        cpu.StatusRegister = opcode is 0x301e or 0x301a ? 1u : 0u;
        cpu.Execute(1); Assert.Equal(expected, cpu.R[0]); Assert.Equal(t ? 1u : 0u, cpu.StatusRegister & 1);
    }

    [Theory]
    [InlineData(0x6010, 0xffffff80u)]
    [InlineData(0x6011, 0xffff8001u)]
    [InlineData(0x6012, 0x80010203u)]
    public void MemoryLoadsAreBigEndianAndSignExtend(ushort opcode, uint expected)
    {
        var bus = new MemoryBus(); var cpu = bus.Program(opcode); cpu.R[1] = 0x300; bus.Write32(0x300, 0x80010203);
        cpu.Execute(1); Assert.Equal(expected, cpu.R[0]);
    }

    [Fact]
    public void BsrAndRtsExecuteTheirOwnDelaySlots()
    {
        var bus = new MemoryBus(); var cpu = bus.Program(0xb002, 0xe001, 0x7004, 0x0009, 0x000b, 0x7002);
        for (int i = 0; i < 4; i++) cpu.Execute(1);
        Assert.Equal(3u, cpu.R[0]); Assert.Equal(0x104u, cpu.ProgramCounter);
        cpu.Execute(1); Assert.Equal(7u, cpu.R[0]); Assert.Equal(0x104u, cpu.ProcedureRegister);
    }

    [Fact]
    public void InterruptStacksStatusAndPcAndRteRestoresThem()
    {
        var bus = new MemoryBus(); var cpu = bus.Program(0x0009); bus.Write32(0x50 * 4, 0x200);
        bus.Write16(0x200, 0xe02a); bus.Write16(0x202, 0x002b); bus.Write16(0x204, 0x0009);
        cpu.StatusRegister = 0; cpu.SetInterrupt(6, 0x50); cpu.Execute(1);
        Assert.Equal(42u, cpu.R[0]); Assert.Equal(0x7ff8u, cpu.R[15]); Assert.Equal(0x100u, bus.Read32(0x7ff8));
        Assert.Equal(0u, bus.Read32(0x7ffc)); cpu.SetInterrupt(0, 0); cpu.Execute(1); cpu.Execute(1);
        Assert.Equal(0x100u, cpu.ProgramCounter); Assert.Equal(0u, cpu.StatusRegister); Assert.Equal(0x8000u, cpu.R[15]);
    }

    [Fact]
    public void IllegalInstructionUsesVectorFourAndInvalidContextDoesNotMutateCpu()
    {
        var bus = new MemoryBus(); var cpu = bus.Program(0xffff); bus.Write32(16, 0x200);
        cpu.Execute(1); Assert.Equal(0x200u, cpu.ProgramCounter);
        byte[] state = new byte[Sh2<MemoryBus>.ContextSize]; cpu.SaveContext(state);
        byte[] invalid = state.ToArray(); BinaryPrimitives.WriteUInt32LittleEndian(invalid.AsSpan(100), 3);
        Assert.False(Sh2<MemoryBus>.CanLoadContext(invalid)); Assert.Throws<ArgumentException>(() => cpu.LoadContext(invalid));
        byte[] after = new byte[state.Length]; cpu.SaveContext(after); Assert.Equal(state, after);
    }

    [Fact]
    public void SleepStopsFetchingUntilAnUnmaskedInterrupt()
    {
        var bus = new MemoryBus(); var cpu = bus.Program(0x001b, 0xe001); cpu.StatusRegister = 0;
        cpu.Execute(100); Assert.True(cpu.Sleeping); Assert.Equal(0u, cpu.R[0]);
        bus.Write32(0x50 * 4, 0x200); bus.Write16(0x200, 0xe002); cpu.SetInterrupt(6, 0x50); cpu.Execute(1);
        Assert.False(cpu.Sleeping); Assert.Equal(2u, cpu.R[0]);
    }
}
