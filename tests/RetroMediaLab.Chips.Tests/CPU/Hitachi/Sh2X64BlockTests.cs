using System.Reflection;
using System.Runtime.InteropServices;
using RetroMediaLab.Chips.CPU.Hitachi.SH2;
using RetroMediaLab.Chips.CPU.Hitachi.SH2.Compilation;
using RetroMediaLab.Chips.CPU.Hitachi.SH2.Contracts;
using Xunit.Abstractions;

namespace RetroMediaLab.Chips.Tests.CPU.Hitachi;

public sealed class Sh2X64BlockTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(false, false, false, false, false, false, false)]
    [InlineData(true, true, false, false, false, false, false)]
    [InlineData(true, true, true, false, false, false, false)]
    [InlineData(true, true, true, true, false, false, false)]
    [InlineData(true, false, false, false, true, false, false)]
    [InlineData(true, true, false, false, true, false, false)]
    [InlineData(true, true, true, false, true, false, false)]
    [InlineData(true, true, true, true, true, false, false)]
    [InlineData(true, false, false, false, true, true, false)]
    [InlineData(true, true, false, false, true, true, false)]
    [InlineData(true, true, true, false, true, true, false)]
    [InlineData(true, true, true, true, true, true, false)]
    [InlineData(false, false, false, false, false, false, true)]
    [InlineData(true, true, true, true, true, false, true)]
    [InlineData(true, true, true, true, true, true, true)]
    [InlineData(true, false, false, false, true, true, true)]
    public void FullNativeExecutionKeepsBudgetsSelfModificationIdleAndIrq(bool mapped, bool deferred, bool idle, bool optimized, bool multi, bool linked, bool timing)
    {
        if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture != Architecture.X64) return;
        var a = new AuditMemoryBus { Mapped = mapped, Deferred = deferred };
        var b = new AuditMemoryBus { Mapped = mapped, Deferred = deferred };
        var reference = new Sh2<AuditMemoryBus>(a); var native = optimized ? Sh2<AuditMemoryBus>.CreateNative(b) : new Sh2<AuditMemoryBus>(b);
        a.Cycles = () => reference.Cycles; b.Cycles = () => native.Cycles;
        reference.AccelerateIdleLoops = native.AccelerateIdleLoops = idle;
        uint[] registers = new uint[16], state = new uint[Sh2X64Context.Words]; Initialize(registers, state, 0xe005, 0);
        registers[5] = 0x400; registers[15] = 0x8000; state[Sh2X64Context.VectorBase] = 0;
        state[Sh2X64Context.Pc] = 0x104; state[Sh2X64Context.PreviousPc] = 0x204;
        ushort[] code = [0xe005, 0xe103, 0x301e, 0x310a, 0x6203, 0x7220, 0x2522, 0x6352, 0x4310, 0x8bfd, 0xaff4, 0x0009];
        foreach (var bus in new[] { a, b })
        {
            for (int i = 0; i < code.Length; i++) bus.Write16((uint)(0x100 + i * 2), code[i]);
            bus.Write32(0x140, 0x200); bus.Write16(0x200, 0xe92a); bus.Write16(0x202, 0x002b); bus.Write16(0x204, 0x0009);
        }
        registers.CopyTo(reference.R, 0); registers.CopyTo(native.R, 0);
        reference.RestoreX64State(state); native.RestoreX64State(state);
        native.UseCompiledBlocks = native.UseFullX64Blocks = true;
        native.UseMultiInstructionX64Blocks = multi;
        native.UseLinkedX64Regions = linked;
        native.UseNativeTimingProfile = timing;
        try
        {
            for (int pass = 0; pass < 10; pass++)
            {
                if (pass == 3) { a.Write16(0x100, 0xe009); b.Write16(0x100, 0xe009); }
                if (pass == 5) { a.SetReadWait(0); b.SetReadWait(0); }
                if (pass == 7)
                {
                    reference.StatusRegister = native.StatusRegister = 0;
                    a.EventCycle = (ulong)a.Clocks + 31; b.EventCycle = (ulong)b.Clocks + 31;
                    a.OnEvent = () => reference.SetInterrupt(6, 0x50); b.OnEvent = () => native.SetInterrupt(6, 0x50);
                    a.OnRead = address => { if (address == 0x140) reference.SetInterrupt(0, 0); };
                    b.OnRead = address => { if (address == 0x140) native.SetInterrupt(0, 0); };
                }
                if (pass == 8)
                {
                    a.OnRead = address => { if (address == 0x400) reference.ClearCodeCache(); };
                    b.OnRead = address => { if (address == 0x400) native.ClearCodeCache(); };
                }
                foreach (int budget in new[] { 1, 2, 3, 7, 13, 64, 127, 17, 1024, 4 })
                {
                    a.Events.Clear(); b.Events.Clear();
                    int expectedClocks = reference.Execute(budget), actualClocks = native.Execute(budget);
                    uint[] expected = new uint[Sh2X64Context.Words], actual = new uint[expected.Length];
                    reference.CaptureX64State(expected); native.CaptureX64State(actual);
                    if (expectedClocks != actualClocks || !reference.R.AsSpan().SequenceEqual(native.R)
                        || !expected.AsSpan().SequenceEqual(actual) || !a.Events.SequenceEqual(b.Events) || a.Clocks != b.Clocks)
                        Assert.Fail($"Full native pass {pass}, budget {budget}; clocks {actualClocks}/{expectedClocks}; R {string.Join(',', native.R.Select(x => x.ToString("X8")))} expected {string.Join(',', reference.R.Select(x => x.ToString("X8")))}; state {string.Join(',', actual.Select(x => x.ToString("X8")))} expected {string.Join(',', expected.Select(x => x.ToString("X8")))}; events {string.Join(';', b.Events)} expected {string.Join(';', a.Events)}");
                }
            }
            Assert.True(native.FullNativeInstructions > 0);
            if (multi) Assert.True(native.FullNativeBlockRuns > 0); else Assert.True(native.FullNativeOpcodeBuilds > 0);
            Assert.Equal(0, native.CompiledBodyBuildCount); // No CIL block generation on this route.
            if (mapped && deferred && !multi) Assert.True(native.FullNativePureRuns > 0);
            var profile = native.GetNativeTimingProfile();
            if (timing)
            {
                Assert.True(profile.PayloadTscTicks > 0); Assert.True(profile.CallbackTscTicks > 0);
                Assert.True(profile.PayloadTscTicks >= profile.CallbackTscTicks);
                Assert.Equal(profile.CallbackTscTicks, profile.Categories.Aggregate(0UL, (total, c) => total + c.TscTicks));
                Assert.Equal(profile.CallbackCalls, profile.Categories.Aggregate(0UL, (total, c) => total + c.Calls));
            }
            else Assert.Equal(0UL, profile.PayloadCalls);
            native.ResetNativeTimingProfile(); Assert.Equal(0UL, native.GetNativeTimingProfile().PayloadCalls);
        }
        finally { try { native.ClearCodeCache(); } finally { reference.ClearCodeCache(); } }
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public void FullNativeFaultCanRetireActiveCodeAndResume(bool multi, bool linked, bool timing)
    {
        if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture != Architecture.X64) return;
        var a = new AuditMemoryBus { Mapped = true, Deferred = true };
        var b = new AuditMemoryBus { Mapped = true, Deferred = true };
        var reference = new Sh2<AuditMemoryBus>(a); var native = Sh2<AuditMemoryBus>.CreateNative(b);
        a.Cycles = () => reference.Cycles; b.Cycles = () => native.Cycles;
        foreach (var bus in new[] { a, b }) { bus.Write16(0x100, 0x6102); bus.Write32(0x400, 0x12345678); }
        reference.ProgramCounter = native.ProgramCounter = 0x100;
        reference.R[0] = native.R[0] = 0x400; native.UseCompiledBlocks = native.UseFullX64Blocks = true;
        native.UseMultiInstructionX64Blocks = multi;
        native.UseLinkedX64Regions = linked;
        native.UseNativeTimingProfile = timing;
        a.OnRead = address => { if (address == 0x400) { reference.ClearCodeCache(); throw new InvalidOperationException("bus fault"); } };
        b.OnRead = address => { if (address == 0x400) { native.ClearCodeCache(); throw new InvalidOperationException("bus fault"); } };
        try
        {
            Assert.Throws<InvalidOperationException>(() => reference.Execute(1));
            Assert.Throws<InvalidOperationException>(() => native.Execute(1));
            a.OnRead = b.OnRead = null;
            uint[] expected = new uint[Sh2X64Context.Words], actual = new uint[expected.Length];
            reference.CaptureX64State(expected); native.CaptureX64State(actual);
            Assert.Equal(expected, actual); Assert.Equal(reference.R, native.R);
            Assert.Equal(reference.Execute(1), native.Execute(1));
            reference.CaptureX64State(expected); native.CaptureX64State(actual);
            Assert.Equal(expected, actual); Assert.Equal(reference.R, native.R);
        }
        finally { try { native.ClearCodeCache(); } finally { reference.ClearCodeCache(); } }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void NativeExecutionLeavesChangedCodeAfterReadOrWrite(bool read, bool linked)
    {
        if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture != Architecture.X64) return;
        var a = new AuditMemoryBus { Mapped = true, Deferred = true };
        var b = new AuditMemoryBus { Mapped = true, Deferred = true };
        var reference = new Sh2<AuditMemoryBus>(a); var native = Sh2<AuditMemoryBus>.CreateNative(b);
        a.Cycles = () => reference.Cycles; b.Cycles = () => native.Cycles;
        reference.AccelerateIdleLoops = native.AccelerateIdleLoops = false;
        foreach (var bus in new[] { a, b })
        { bus.Write16(0x100, read ? (ushort)0x6102 : (ushort)0x2011); bus.Write16(0x102, 0xe205); bus.Write16(0x104, 0x0009); }
        reference.ProgramCounter = native.ProgramCounter = 0x100;
        reference.R[0] = native.R[0] = read ? 0x400u : 0x102u;
        reference.R[1] = native.R[1] = 0xe22a;
        if (read) { a.OnRead = address => { if (address == 0x400) a.Write16(0x102, 0xe22a); }; b.OnRead = address => { if (address == 0x400) b.Write16(0x102, 0xe22a); }; }
        native.UseCompiledBlocks = native.UseFullX64Blocks = native.UseMultiInstructionX64Blocks = true;
        native.UseLinkedX64Regions = linked;
        try
        {
            a.Events.Clear(); b.Events.Clear();
            Assert.Equal(reference.Execute(read ? 8 : 7), native.Execute(read ? 8 : 7));
            Assert.Equal(42u, native.R[2]); Assert.Equal(reference.R, native.R);
            uint[] expected = new uint[Sh2X64Context.Words], actual = new uint[expected.Length];
            reference.CaptureX64State(expected); native.CaptureX64State(actual);
            Assert.Equal(expected, actual); Assert.Equal(a.Events, b.Events);
        }
        finally { try { native.ClearCodeCache(); } finally { reference.ClearCodeCache(); } }
    }
    private static readonly uint[] Edges = [0, 1, 0x7fffffff, 0x80000000, 0xffffffff, 0xff00ff00, 0x12345678, 0x80000001];

    [Fact]
    public void InstrumentedPayloadsPreserveMacScratchAndBusSemantics()
    {
        if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture != Architecture.X64) return;
        var a = new AuditMemoryBus(); var b = new AuditMemoryBus();
        var reference = new Sh2<AuditMemoryBus>(a); var native = new Sh2<AuditMemoryBus>(b);
        a.Cycles = () => reference.Cycles; b.Cycles = () => native.Cycles;
        reference.AccelerateIdleLoops = native.AccelerateIdleLoops = false;
        var interpret = typeof(Sh2<AuditMemoryBus>).GetMethod("ExecuteOpcode", BindingFlags.Instance | BindingFlags.NonPublic)!
            .CreateDelegate<Action<Sh2<AuditMemoryBus>, ushort>>();
        uint[] registers = new uint[16], state = new uint[Sh2X64Context.Words], expected = new uint[state.Length], actual = new uint[state.Length];
        try
        {
            foreach (ushort opcode in new ushort[] { 0xe123, 0x012f, 0x412f, 0x2126, 0x6126, 0x4107, 0xc301, 0xaffe, 0x002b })
            {
                Sh2X64Block? block = null;
                try
                {
                    block = Sh2X64Block.TryCompile([opcode], true)!;
                    for (int sample = 0; sample < 64; sample++)
                    {
                        Initialize(registers, state, opcode, sample); state[Sh2X64Context.Branch] = (uint)(sample % 3);
                        registers.CopyTo(reference.R, 0); registers.CopyTo(native.R, 0);
                        reference.RestoreX64State(state); native.RestoreX64State(state);
                        a.Events.Clear(); b.Events.Clear(); a.Clocks = b.Clocks = 0;
                        interpret(reference, opcode); native.ExecuteX64Payload(block);
                        reference.CaptureX64State(expected); native.CaptureX64State(actual);
                        Assert.Equal(reference.R, native.R); Assert.Equal(expected, actual); Assert.Equal(a.Events, b.Events); Assert.Equal(a.Clocks, b.Clocks);
                    }
                }
                finally { block?.Dispose(); }
            }
            var profile = native.GetNativeTimingProfile();
            Assert.Equal(576UL, profile.PayloadCalls); Assert.True(profile.CallbackCalls > 0);
            Assert.True(profile.PayloadTscTicks >= profile.CallbackTscTicks);
        }
        finally { try { native.ClearCodeCache(); } finally { reference.ClearCodeCache(); } }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LinkedRegionsUseMappedTimingAfterAColdFetchAndAfterTimingChanges(bool optimized)
    {
        if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture != Architecture.X64) return;
        var a = new AuditMemoryBus { Mapped = false, Deferred = true };
        var b = new AuditMemoryBus { Mapped = false, Deferred = true };
        var reference = new Sh2<AuditMemoryBus>(a);
        var native = optimized ? Sh2<AuditMemoryBus>.CreateNative(b) : new Sh2<AuditMemoryBus>(b);
        a.Cycles = () => reference.Cycles; b.Cycles = () => native.Cycles;
        reference.AccelerateIdleLoops = native.AccelerateIdleLoops = false;
        foreach (var bus in new[] { a, b })
        { bus.Write16(0x100, 0x7001); bus.Write16(0x102, 0xaffd); bus.Write16(0x104, 0x0009); }
        a.OnRead = address => { if (address == 0x100) { a.Mapped = true; a.SetReadWait(0); } };
        b.OnRead = address => { if (address == 0x100) { b.Mapped = true; b.SetReadWait(0); } };
        reference.ProgramCounter = native.ProgramCounter = 0x100;
        native.UseCompiledBlocks = native.UseFullX64Blocks = native.UseMultiInstructionX64Blocks = native.UseLinkedX64Regions = true;
        try
        {
            for (int pass = 0; pass < 4; pass++)
            {
                if (pass == 1) { a.SetReadWait(2); b.SetReadWait(2); }
                if (pass == 2) { a.SetReadWait(0); b.SetReadWait(0); }
                a.Events.Clear(); b.Events.Clear();
                Assert.Equal(reference.Execute(128), native.Execute(128)); Assert.Equal(reference.R, native.R);
                uint[] expected = new uint[Sh2X64Context.Words], actual = new uint[expected.Length];
                reference.CaptureX64State(expected); native.CaptureX64State(actual);
                Assert.Equal(expected, actual); Assert.Equal(a.Events, b.Events);
            }
            Assert.True(native.FullNativeLinkedHops > 0);
        }
        finally { try { native.ClearCodeCache(); } finally { reference.ClearCodeCache(); } }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LinkedRegionsKeepBackwardForwardBranchesAndEntryFetchChanges(bool changeTiming)
    {
        if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture != Architecture.X64) return;
        var a = new AuditMemoryBus { Mapped = true, Deferred = true };
        var b = new AuditMemoryBus { Mapped = true, Deferred = true };
        var reference = new Sh2<AuditMemoryBus>(a); var native = Sh2<AuditMemoryBus>.CreateNative(b);
        a.Cycles = () => reference.Cycles; b.Cycles = () => native.Cycles;
        reference.AccelerateIdleLoops = native.AccelerateIdleLoops = false;
        ushort[] words = changeTiming ? [0x6102, 0xaffd, 0x0009]
            : [0xe003, 0x4010, 0x8bfd, 0xa001, 0xe12a, 0xffff, 0x0009, 0xaffe, 0x0009];
        foreach (var bus in new[] { a, b })
        { for (int i = 0; i < words.Length; i++) bus.Write16((uint)(0x100 + i * 2), words[i]); }
        reference.ProgramCounter = native.ProgramCounter = 0x100;
        if (changeTiming)
        {
            reference.R[0] = native.R[0] = 0x400;
            int readsA = 0, readsB = 0;
            a.OnRead = address => { if (address == 0x400 && ++readsA == 4) a.SetReadWait(0); };
            b.OnRead = address => { if (address == 0x400 && ++readsB == 4) b.SetReadWait(0); };
        }
        native.UseCompiledBlocks = native.UseFullX64Blocks = native.UseMultiInstructionX64Blocks = native.UseLinkedX64Regions = true;
        try
        {
            foreach (int budget in new[] { 1, 2, 5, 13, 64, 257, 1000 })
            {
                a.Events.Clear(); b.Events.Clear();
                Assert.Equal(reference.Execute(budget), native.Execute(budget)); Assert.Equal(reference.R, native.R);
                uint[] expected = new uint[Sh2X64Context.Words], actual = new uint[expected.Length];
                reference.CaptureX64State(expected); native.CaptureX64State(actual);
                Assert.Equal(expected, actual); Assert.Equal(a.Events, b.Events);
            }
            Assert.True(native.FullNativeLinkedHops > 0);
            Assert.Equal(0, native.CompiledBodyBuildCount);
            if (!changeTiming) Assert.Equal(42u, native.R[1]);
        }
        finally { try { native.ClearCodeCache(); } finally { reference.ClearCodeCache(); } }
    }

    [Theory]
    [InlineData(0xa000, false)]
    [InlineData(0x0009, false)]
    [InlineData(0xa000, true)]
    [InlineData(0x0009, true)]
    public void NativeExecutionCompletesDelayedAndIllegalSlotBranches(int slot, bool linked)
    {
        if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture != Architecture.X64) return;
        var a = new AuditMemoryBus { Mapped = true, Deferred = true };
        var b = new AuditMemoryBus { Mapped = true, Deferred = true };
        var reference = new Sh2<AuditMemoryBus>(a); var native = Sh2<AuditMemoryBus>.CreateNative(b);
        a.Cycles = () => reference.Cycles; b.Cycles = () => native.Cycles;
        reference.AccelerateIdleLoops = native.AccelerateIdleLoops = false;
        foreach (var bus in new[] { a, b })
        { bus.Write16(0x100, 0xa000); bus.Write16(0x102, (ushort)slot); bus.Write32(24, 0x200); bus.Write16(0x104, 0x0009); bus.Write16(0x200, 0x0009); }
        reference.ProgramCounter = native.ProgramCounter = 0x100;
        reference.R[15] = native.R[15] = 0x8000;
        native.UseCompiledBlocks = native.UseFullX64Blocks = native.UseMultiInstructionX64Blocks = true;
        native.UseLinkedX64Regions = linked;
        try
        {
            a.Events.Clear(); b.Events.Clear();
            Assert.Equal(reference.Execute(slot == 0xa000 ? 11 : 7), native.Execute(slot == 0xa000 ? 11 : 7));
            Assert.Equal(reference.R, native.R);
            uint[] expected = new uint[Sh2X64Context.Words], actual = new uint[expected.Length];
            reference.CaptureX64State(expected); native.CaptureX64State(actual);
            Assert.Equal(expected, actual); Assert.Equal(a.Events, b.Events);
            Assert.True(native.FullNativeBlockRuns > 0);
        }
        finally { try { native.ClearCodeCache(); } finally { reference.ClearCodeCache(); } }
    }
    private static readonly Action<Sh2<NoAccessBus>, ushort> Interpret = typeof(Sh2<NoAccessBus>)
        .GetMethod("ExecuteOpcode", BindingFlags.Instance | BindingFlags.NonPublic)!
        .CreateDelegate<Action<Sh2<NoAccessBus>, ushort>>();

    [Fact]
    public void EverySupportedEncodingMatchesReferenceIntegerSemantics()
    {
        if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture != Architecture.X64) return;
        var cpu = new Sh2<NoAccessBus>(new());
        cpu.AccelerateIdleLoops = false;
        uint[] initial = new uint[16], actual = new uint[16], context = new uint[Sh2X64Context.Words], expected = new uint[Sh2X64Context.Words];
        int supported = 0, cases = 0;
        try
        {
            for (int encoding = 0; encoding <= ushort.MaxValue; encoding++)
            {
                Sh2X64Block? block = null;
                try
                {
                    ushort opcode = (ushort)encoding;
                    block = Sh2X64Block.TryCompile([opcode]);
                    if (block is null || block.RequiresMemory) continue;
                    supported++;
                    for (int sample = 0; sample < 512; sample++)
                    {
                        Initialize(initial, context, opcode, sample);
                        initial.CopyTo(cpu.R, 0); initial.CopyTo(actual, 0);
                        cpu.RestoreX64State(context);
                        Interpret(cpu, opcode); cpu.CaptureX64State(expected);
                        block.Execute(actual, context);
                        if (!actual.AsSpan().SequenceEqual(cpu.R) || !context.AsSpan().SequenceEqual(expected))
                            Assert.Fail($"SH2 {opcode:X4}, sample {sample}: registers {string.Join(',', actual.Select(x => x.ToString("X8")))} expected {string.Join(',', cpu.R.Select(x => x.ToString("X8")))}; state {string.Join(',', context.Select(x => x.ToString("X8")))} expected {string.Join(',', expected.Select(x => x.ToString("X8")))}");
                        cases++;
                    }
                }
                finally { block?.Dispose(); }
            }
            Assert.True(supported > 14000, $"Unexpected instruction coverage: {supported}");
            Assert.True(supported < 65536); // Memory/control-flow cannot silently pass as native yet.
            output.WriteLine($"Native integer encodings: {supported}; value/state cases: {cases}.");
        }
        finally { cpu.ClearCodeCache(); }
    }

    [Fact]
    public void EveryMemoryEncodingMatchesBusTraceRegistersAndWaits()
    {
        if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture != Architecture.X64) return;
        var a = new AuditMemoryBus(); var b = new AuditMemoryBus();
        var reference = new Sh2<AuditMemoryBus>(a); var native = new Sh2<AuditMemoryBus>(b);
        a.Cycles = () => reference.Cycles; b.Cycles = () => native.Cycles;
        reference.AccelerateIdleLoops = native.AccelerateIdleLoops = false;
        var interpret = typeof(Sh2<AuditMemoryBus>).GetMethod("ExecuteOpcode", BindingFlags.Instance | BindingFlags.NonPublic)!
            .CreateDelegate<Action<Sh2<AuditMemoryBus>, ushort>>();
        uint[] registers = new uint[16], context = new uint[Sh2X64Context.Words], expected = new uint[Sh2X64Context.Words], actual = new uint[Sh2X64Context.Words];
        int supported = 0, cases = 0, covered = 0;
        try
        {
            for (int encoding = 0; encoding <= ushort.MaxValue; encoding++)
            {
                Sh2X64Block? block = null;
                try
                {
                    ushort opcode = (ushort)encoding; block = Sh2X64Block.TryCompile([opcode]);
                    if (block is null) continue;
                    covered++;
                    if (!block.RequiresMemory) continue;
                    supported++;
                    for (int sample = 0; sample < 64; sample++)
                    {
                        Initialize(registers, context, opcode, sample);
                        context[Sh2X64Context.Branch] = (uint)(sample % 3); // PC-relative accesses in delay slots too.
                        registers.CopyTo(reference.R, 0); registers.CopyTo(native.R, 0);
                        reference.RestoreX64State(context); native.RestoreX64State(context);
                        a.Events.Clear(); b.Events.Clear(); a.Clocks = b.Clocks = 0;
                        interpret(reference, opcode); native.ExecuteX64Payload(block);
                        reference.CaptureX64State(expected); native.CaptureX64State(actual);
                        if (!reference.R.AsSpan().SequenceEqual(native.R) || !expected.AsSpan().SequenceEqual(actual)
                            || !a.Events.SequenceEqual(b.Events) || a.Clocks != b.Clocks)
                            Assert.Fail($"Memory SH2 {opcode:X4}, sample {sample}: registers {string.Join(',', native.R.Select(x => x.ToString("X8")))} expected {string.Join(',', reference.R.Select(x => x.ToString("X8")))}; state {string.Join(',', actual.Select(x => x.ToString("X8")))} expected {string.Join(',', expected.Select(x => x.ToString("X8")))}; accesses {string.Join(';', b.Events)} expected {string.Join(';', a.Events)}");
                        cases++;
                    }
                }
                finally { block?.Dispose(); }
            }
            Assert.True(supported > 20000, $"Unexpected memory instruction coverage: {supported}");
            Assert.Equal(65536, covered);
            output.WriteLine($"Native memory encodings: {supported}; value/state/trace cases: {cases}.");
        }
        finally { try { native.ClearCodeCache(); } finally { reference.ClearCodeCache(); } }
    }

    [Fact]
    public void BusFailureReturnsSafelyBeforeFollowingInstruction()
    {
        if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture != Architecture.X64) return;
        var cpu = new Sh2<NoAccessBus>(new()); cpu.R[15] = 0x8000; cpu.R[1] = 7;
        Sh2X64Block? block = null;
        try
        {
            block = Sh2X64Block.TryCompile([0x2f06, 0xe12a]); Assert.NotNull(block);
            Assert.Throws<InvalidOperationException>(() => cpu.ExecuteX64Payload(block));
            Assert.Equal(0x7ffcu, cpu.R[15]); Assert.Equal(7u, cpu.R[1]);
        }
        finally { try { block?.Dispose(); } finally { cpu.ClearCodeCache(); } }
    }

    [Fact]
    public void MixedPayloadCarriesFlagsAndAllSixteenRegistersAcrossInstructions()
    {
        if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture != Architecture.X64) return;
        var cpu = new Sh2<NoAccessBus>(new()); cpu.AccelerateIdleLoops = false;
        var candidate = new Sh2<NoAccessBus>(new()); candidate.AccelerateIdleLoops = false;
        ushort[] patterns = [0x300e, 0x300a, 0x300f, 0x300b, 0x3004, 0x2007, 0x200c, 0x6008, 0x600e, 0x0029, 0x4024, 0x4025, 0x4010, 0x300d, 0x000a];
        ushort[] code = Enumerable.Range(0, 256).Select(i => (ushort)(patterns[i % patterns.Length] | ((i & 15) << 8) | ((i * 7 & 15) << 4))).ToArray();
        // Unary encodings have fixed low-byte bits; choose their nibbles from valid encodings instead.
        for (int i = 0; i < code.Length; i++)
            if ((patterns[i % patterns.Length] & 0xf000) is 0x0000 or 0x4000)
                code[i] = (ushort)(patterns[i % patterns.Length] | ((i & 15) << 8));
        Sh2X64Block? block = null;
        try
        {
            block = Sh2X64Block.TryCompile(code); Assert.NotNull(block);
            for (int sample = 0; sample < 32; sample++)
            {
                uint[] registers = new uint[16], state = new uint[Sh2X64Context.Words], expected = new uint[state.Length];
                Initialize(registers, state, 0x312e, sample); registers.CopyTo(cpu.R, 0); cpu.RestoreX64State(state);
                foreach (ushort opcode in code) Interpret(cpu, opcode);
                // Use an independent payload owner for the borrowed idle/bus protocol.
                uint[] initialRegisters = new uint[16], initialState = new uint[state.Length];
                Initialize(initialRegisters, initialState, 0x312e, sample);
                initialRegisters.CopyTo(candidate.R, 0); candidate.RestoreX64State(initialState);
                cpu.CaptureX64State(expected); candidate.ExecuteX64Payload(block); candidate.CaptureX64State(state);
                Assert.Equal(cpu.R, candidate.R); Assert.Equal(expected, state);
            }
        }
        finally { try { block?.Dispose(); } finally { try { candidate.ClearCodeCache(); } finally { cpu.ClearCodeCache(); } } }
    }

    private static void Initialize(uint[] registers, uint[] state, ushort opcode, int sample)
    {
        Array.Clear(state);
        for (int i = 0; i < registers.Length; i++) registers[i] = unchecked(0x9e3779b9u * (uint)(i + sample + 1));
        registers[opcode >> 8 & 15] = Edges[sample & 7];
        registers[opcode >> 4 & 15] = Edges[(sample >> 3) & 7];
        state[Sh2X64Context.Status] = (uint)(((sample >> 6) & 1) | ((sample >> 7 & 1) << 8) | ((sample >> 8 & 1) << 9) | 0xf0 | ((sample >> 2 & 1) << 1));
        state[Sh2X64Context.GlobalBase] = 0xaabbccdd; state[Sh2X64Context.VectorBase] = 0x12345678;
        state[Sh2X64Context.Procedure] = 0x87654321; state[Sh2X64Context.MacLow] = 0x11223344; state[Sh2X64Context.MacHigh] = 0x55667788;
        state[Sh2X64Context.Pc] = 0x06000104; state[Sh2X64Context.PreviousPc] = 0x06000204;
        state[Sh2X64Context.PollingEpoch] = unchecked((uint)(sample - 1));
    }

    private sealed class NoAccessBus : ISh2Bus
    {
        public byte Read8(uint address) => throw new InvalidOperationException("Integer payload accessed memory.");
        public ushort Read16(uint address) => throw new InvalidOperationException("Integer payload accessed memory.");
        public uint Read32(uint address) => throw new InvalidOperationException("Integer payload accessed memory.");
        public void Write8(uint address, byte value) => throw new InvalidOperationException("Integer payload accessed memory.");
        public void Write16(uint address, ushort value) => throw new InvalidOperationException("Integer payload accessed memory.");
        public void Write32(uint address, uint value) => throw new InvalidOperationException("Integer payload accessed memory.");
    }

    private sealed class AuditMemoryBus : ISh2Bus
    {
        private readonly byte[] _memory = Enumerable.Range(0, 65536).Select(i => (byte)(i * 29 + (i >> 8))).ToArray();
        internal readonly List<(uint Address, uint Value, int Kind, ulong Cycle)> Events = [];
        internal Func<ulong> Cycles = () => 0;
        internal int Clocks;
        internal bool Mapped, Deferred;
        private uint _version;
        private int _readWait = 2;
        internal ulong EventCycle = ulong.MaxValue;
        internal Action? OnEvent;
        internal Action<uint>? OnRead;
        internal void SetReadWait(int value) { _readWait = value; _version++; }
        public bool DeferClockUpdates => Deferred;
        public ulong NextEventCycle => EventCycle;
        public long CodeVersion(uint address) => _version;
        public ulong CodeEpoch(uint address) => (ulong)_version + 1;
        public uint FetchMapVersion => _version;
        public bool TryMapFetch(uint address, out ReadOnlyMemory<byte> memory, out uint start, out int waitStates)
        { memory = _memory; start = 0; waitStates = _readWait; return Mapped && address < 65535; }
        private byte Byte(uint address) => _memory[address & 65535];
        private uint Read(uint address, int size)
        {
            uint value = 0; for (int i = 0; i < size; i++) value = value << 8 | Byte(unchecked(address + (uint)i));
            Events.Add((address, value, size, Cycles())); OnRead?.Invoke(address); return value;
        }
        private void Write(uint address, uint value, int size)
        {
            for (int i = 0; i < size; i++) _memory[unchecked(address + (uint)i) & 65535] = (byte)(value >> ((size - 1 - i) * 8));
            Events.Add((address, value, 0x100 | size, Cycles()));
            _version++;
        }
        public byte Read8(uint address) => (byte)Read(address, 1);
        public ushort Read16(uint address) => (ushort)Read(address, 2);
        public uint Read32(uint address) => Read(address, 4);
        public void Write8(uint address, byte value) => Write(address, value, 1);
        public void Write16(uint address, ushort value) => Write(address, value, 2);
        public void Write32(uint address, uint value) => Write(address, value, 4);
        public int WaitStates(uint address, bool write) => write ? 1 : _readWait;
        public void AdvanceCycles(int clocks)
        { Clocks += clocks; if ((ulong)Clocks >= EventCycle) { EventCycle = ulong.MaxValue; OnEvent?.Invoke(); } }
    }
}


