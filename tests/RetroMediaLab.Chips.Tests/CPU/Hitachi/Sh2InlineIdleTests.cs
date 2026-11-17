using System.Buffers.Binary;
using System.Reflection;
using RetroMediaLab.Chips.Controllers.Hitachi.SH7095;
using RetroMediaLab.Chips.CPU.Hitachi.SH2;
using RetroMediaLab.Chips.CPU.Hitachi.SH2.Compilation;
using RetroMediaLab.Chips.CPU.Hitachi.SH2.Contracts;

namespace RetroMediaLab.Chips.Tests.CPU.Hitachi;

public sealed class Sh2InlineIdleTests
{
    public static IEnumerable<object[]> Scenarios()
    {
        foreach (bool live in new[] { false, true })
        foreach (bool acceleration in new[] { false, true })
        foreach (int period in new[] { 7, 100000 })
        foreach (int kind in Enumerable.Range(0, 5))
            yield return [live, acceleration, period, kind];
    }

    [Theory]
    [MemberData(nameof(Scenarios))]
    public void CertifiedLoopsKeepCpuMemoryAndEventTransitions(bool live, bool acceleration, int period, int kind)
    {
        Sh2<Bus>? reference = null, native = null;
        try
        {
            var a = new Bus(period); var b = new Bus(period);
            ushort[] words = kind switch
            {
                0 => [0xaffe, 9], 1 => [9, 0xaffd, 9],
                2 => [0x4010, 0x8bfd, 0x001b],
                3 => [0xaffe, 0x6113], _ => [0x4010, 9, 0x001b]
            };
            a.Program(words); b.Program(words);
            reference = new(a); native = new(b);
            a.Cpu = reference; b.Cpu = native;
            native.UseCompiledBlocks = native.UseFullX64Blocks = native.UseMultiInstructionX64Blocks = true;
            native.UseLiveX64Context = live; native.UseNativeInlineIdle = true;
            reference.AccelerateIdleLoops = native.AccelerateIdleLoops = acceleration;
            reference.ProgramCounter = native.ProgramCounter = 0x100;
            reference.R[0] = native.R[0] = 37;
            reference.R[15] = native.R[15] = 0x3f00;
            foreach (int budget in new[] { 1, 2, 3, 11, 97, 257 })
            {
                Assert.Equal(reference.Execute(budget), native.Execute(budget));
                Assert.Equal(reference.R, native.R);
                uint[] x = new uint[Sh2X64Context.Words], y = new uint[Sh2X64Context.Words];
                reference.CaptureX64State(x); native.CaptureX64State(y); Assert.Equal(x, y);
                Assert.Equal(a.Memory, b.Memory); Assert.Equal(a.Ticks, b.Ticks);
                Assert.Equal(a.Events, b.Events);
            }
        }
        finally { try { native?.ClearCodeCache(); } finally { reference?.ClearCodeCache(); } }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OnlyCertifiedLoopsRemoveIdleCallbacksAndPolicyCanChange(bool live)
    {
        Sh2<Bus>? cpu = null;
        try
        {
            var bus = new Bus(100000); bus.Program([0xaffe, 9]);
            cpu = new(bus); bus.Cpu = cpu; cpu.ProgramCounter = 0x100;
            cpu.UseCompiledBlocks = cpu.UseFullX64Blocks = cpu.UseMultiInstructionX64Blocks = true;
            cpu.UseNativeTimingProfile = true; cpu.UseLiveX64Context = live;
            cpu.UseNativeInlineIdle = true;
            cpu.Execute(100);
            Assert.Equal(0ul, cpu.GetNativeTimingProfile().Categories.Single(x => x.Category == "idle").Calls);
            Assert.True(cpu.FullNativeInstructions > 0);
            cpu.ResetNativeTimingProfile(); cpu.UseNativeInlineIdle = false;
            cpu.Execute(100);
            Assert.True(cpu.GetNativeTimingProfile().Categories.Single(x => x.Category == "idle").Calls > 0);
            cpu.UseNativeInlineIdle = true; bus.Certified = false; bus.Version++;
            cpu.ResetNativeTimingProfile(); cpu.Execute(100);
            Assert.True(cpu.GetNativeTimingProfile().Categories.Single(x => x.Category == "idle").Calls > 0);
        }
        finally { cpu?.ClearCodeCache(); }
    }

    private sealed class Bus(int period) : ISh2Bus, ISh2NativeIdleBus
    {
        public byte[] Memory { get; } = new byte[0x4000];
        public Sh2<Bus>? Cpu;
        public bool Certified = true;
        public ulong Version = 1, Ticks;
        public int Events;
        public int EventAction;
        public int Wait;
        private ulong next = (uint)period;
        public void StartAt(ulong cycles) { Ticks = cycles; next = cycles + (uint)period; }
        public void EventAt(ulong cycles) => next = cycles;
        public bool DeferClockUpdates => true;
        public ulong NextEventCycle => next;
        public int MaximumWaitStates => Wait;
        public int WaitStates(uint address, bool write) => Wait;
        public bool IsTimedAccess(uint a) => false;
        public long CodeVersion(uint a) => (long)Version;
        public ulong CodeEpoch(uint a) => Version;
        public bool TryGetNativeIdleClocks(uint address, int words, out int clocks)
        { clocks = words + 1 + words * Wait; return Certified && address >= 0x100 && address + words * 2 <= Memory.Length; }
        public bool TryMapFetch(uint address, out ReadOnlyMemory<byte> memory, out uint start, out int wait)
        { memory = Memory; start = 0; wait = Wait; return address + 2 <= Memory.Length; }
        public void AdvanceCycles(int clocks)
        {
            Ticks += (uint)clocks;
            while (Ticks >= next)
            {
                Events++; next += (uint)period;
                if (Events != 1) continue;
                if (EventAction == 1) Cpu!.SetInterrupt(6, 32);
                if (EventAction == 2) { Write16(0x102, 0x001b); }
                if (EventAction == 3) Cpu!.AccelerateIdleLoops = false;
            }
        }
        public byte Read8(uint a) => Memory[a % (uint)Memory.Length];
        public ushort Read16(uint a) => BinaryPrimitives.ReadUInt16BigEndian(Memory.AsSpan((int)a, 2));
        public ushort Fetch16(uint a) => Read16(a);
        public uint Read32(uint a) => BinaryPrimitives.ReadUInt32BigEndian(Memory.AsSpan((int)a, 4));
        public void Write8(uint a, byte v) { Memory[a] = v; Version++; }
        public void Write16(uint a, ushort v) { BinaryPrimitives.WriteUInt16BigEndian(Memory.AsSpan((int)a, 2), v); Version++; }
        public void Write32(uint a, uint v) { BinaryPrimitives.WriteUInt32BigEndian(Memory.AsSpan((int)a, 4), v); Version++; }
        public void Program(ushort[] words)
        { for (int i = 0; i < words.Length; i++) BinaryPrimitives.WriteUInt16BigEndian(Memory.AsSpan(0x100 + i * 2), words[i]); }
    }

    [Theory]
    [InlineData(false, 1)] [InlineData(true, 1)]
    [InlineData(false, 2)] [InlineData(true, 2)]
    [InlineData(false, 3)] [InlineData(true, 3)]
    public void EventsInterruptRewriteOrDisableAnAlreadyTranslatedLoop(bool live, int action)
    {
        CompareSpecial(live, action, 37, false);
    }

    [Theory]
    [InlineData(false, 0u)] [InlineData(true, 0u)]
    [InlineData(false, 1u)] [InlineData(true, 1u)]
    [InlineData(false, 2u)] [InlineData(true, 2u)]
    [InlineData(false, uint.MaxValue)] [InlineData(true, uint.MaxValue)]
    public void DtLimitsAnd64BitClocksMatchTheInterpreter(bool live, uint counter)
    {
        CompareSpecial(live, 0, counter, true);
    }

    private static void CompareSpecial(bool live, int action, uint counter, bool highClock)
    {
        var buses = new[] { new Bus(100000), new Bus(100000) };
        var cpus = buses.Select(b => new Sh2<Bus>(b)).ToArray();
        try
        {
            for (int i = 0; i < 2; i++)
            {
                var b = buses[i]; var c = cpus[i]; b.Cpu = c; b.EventAction = action;
                if (action != 0) b.EventAt(1);
                b.Program(highClock ? [0x4010, 0x8bfd, 0x001b] : [0xaffe, 9]);
                b.Write32(32 * 4, 0x200); b.Write16(0x200, 0x001b);
                c.ProgramCounter = 0x100; c.R[0] = counter; c.R[15] = 0x3f00;
                if (highClock)
                {
                    ulong cycles = (1ul << 32) - 8;
                    typeof(Sh2<Bus>).GetField("<Cycles>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(c, cycles);
                    b.StartAt(cycles);
                }
                if (i == 1)
                {
                    c.UseCompiledBlocks = c.UseFullX64Blocks = c.UseMultiInstructionX64Blocks = true;
                    c.UseNativeInlineIdle = true; c.UseLiveX64Context = live;
                }
            }
            for (int pass = 0; pass < 3; pass++)
            {
                Assert.Equal(cpus[0].Execute(401), cpus[1].Execute(401));
                uint[] a = new uint[Sh2X64Context.Words], b = new uint[Sh2X64Context.Words];
                cpus[0].CaptureX64State(a); cpus[1].CaptureX64State(b);
                Assert.Equal(a, b); Assert.Equal(cpus[0].R, cpus[1].R);
                Assert.Equal(buses[0].Memory, buses[1].Memory);
                Assert.Equal(buses[0].Events, buses[1].Events);
            }
        }
        finally { try { cpus[1].ClearCodeCache(); } finally { cpus[0].ClearCodeCache(); } }
    }

    [Fact]
    public void SocCertificationUsesTheSameWaitsAsTheIdleHelperForEveryCcrMode()
    {
        Sh7095? soc = null;
        try
        {
            var external = new Bus(100000) { Wait = 2 }; external.Program([0xaffe, 9]);
            soc = new(external); var bus = new Sh7095.CpuBus(soc);
            for (int ccr = 0; ccr < 16; ccr++)
            {
                soc.Reset(); soc.Write8(0xfffffe92, 1); soc.Fetch16(0x100);
                soc.Write8(0xfffffe92, (byte)ccr);
                byte[] before = new byte[Sh7095.ContextSize], after = new byte[Sh7095.ContextSize];
                soc.SaveContext(before);
                if (bus.TryGetNativeIdleClocks(0x100, 2, out int clocks))
                    Assert.Equal(3 + bus.WaitStates(0x100, false) + bus.WaitStates(0x102, false), clocks);
                soc.SaveContext(after); Assert.Equal(before, after);
            }
        }
        finally { soc?.Cpu.ClearCodeCache(); }
    }
}
