using System.Reflection;
using System.Runtime.InteropServices;
using RetroMediaLab.Chips.Controllers.Hitachi.SH7095;
using RetroMediaLab.Chips.CPU.Hitachi.SH2;
using RetroMediaLab.Chips.CPU.Hitachi.SH2.Compilation;
using RetroMediaLab.Chips.CPU.Hitachi.SH2.Contracts;

namespace RetroMediaLab.Chips.Tests.CPU.Hitachi;

public sealed class Sh2NativeCachedReadTests
{
    private static readonly Action<Sh2<Sh7095.CpuBus>, ushort> Interpret = typeof(Sh2<Sh7095.CpuBus>)
        .GetMethod("ExecuteOpcode", BindingFlags.Instance | BindingFlags.NonPublic)!
        .CreateDelegate<Action<Sh2<Sh7095.CpuBus>, ushort>>();

    [Fact]
    public void CacheReadsMatchSizesWaysModesAlignmentAndRequiredCallbacks()
    {
        if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture != Architecture.X64) return;
        Sh7095? a = null, b = null; var busA = new Bus(); var busB = new Bus();
        try
        {
            a = new(busA); b = new(busB); b.Cpu.UseNativeCachedReads = true;
            foreach (int size in new[] { 1, 2, 4 })
            {
                Sh2X64Block? block = null;
                try
                {
                    ushort opcode = (ushort)(0x6100 + (size == 1 ? 0 : size == 2 ? 1 : 2));
                    block = Sh2X64Block.TryCompile([opcode], true, true)!;
                    foreach (uint start in new uint[] { 0x1000, 0x02001000, 0x06001000, 0x4000, 0xc0000200 })
                    foreach (int way in new[] { 0, 1, 2, 3 })
                    foreach (byte control in new byte[] { 0, 1, 9 })
                    foreach (bool direct in new[] { false, true })
                    for (int offset = 0; offset < 16; offset++)
                    {
                        Configure(a, start, way, control, direct); Configure(b, start, way, control, direct);
                        uint address = start + (uint)offset;
                        Prepare(a, address, false); Prepare(b, address, false); b.Cpu.ResetNativeTimingProfile();
                        busA.Reads.Clear(); busB.Reads.Clear();
                        Interpret(a.Cpu, opcode); b.Cpu.ExecuteX64Payload(block);
                        bool ram = start >> 29 == 6;
                        bool pure = start < 0x4000 || start is >= 0x02000000 and < 0x06040000;
                        bool hit = !direct && (ram || pure && (control & 5) == 1 && ((control & 8) == 0 || way >= 2) && offset <= 16 - size);
                        Assert.Equal(hit ? 1UL : 0UL, b.Cpu.GetNativeTimingProfile().CachedReadHits);
                        Assert.Equal(hit ? 0UL : 1UL, b.Cpu.GetNativeTimingProfile().CallbackCalls);
                        Assert.Equal(busA.Reads, busB.Reads);
                        Equal(a, b);
                    }
                }
                finally { block?.Dispose(); }
            }
        }
        finally { try { b?.Cpu.ClearCodeCache(); } finally { a?.Cpu.ClearCodeCache(); } }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DueEventsAndPolicyChangesKeepTheBusPath(bool switchMode)
    {
        if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture != Architecture.X64) return;
        Sh7095? a = null, b = null; Sh2X64Block? block = null; var busA = new Bus(); var busB = new Bus();
        try
        {
            a = new(busA); b = new(busB); b.Cpu.UseNativeCachedReads = true;
            Configure(a, 0x06001000, 3, 1, false); Configure(b, 0x06001000, 3, 1, false);
            if (!switchMode)
            {
                Prepare(a, 0x06001000, true); Prepare(b, 0x06001000, true);
                block = Sh2X64Block.TryCompile([0x6102], true, true)!;
                Interpret(a.Cpu, 0x6102); b.Cpu.ExecuteX64Payload(block);
                Assert.Equal(0UL, b.Cpu.GetNativeTimingProfile().CachedReadHits);
            }
            else
            {
                Prepare(a, 0x10000000, false); Prepare(b, 0x10000000, false);
                a.Cpu.R[2] = b.Cpu.R[2] = 0x06001000;
                busA.OnRead = () => { a.MemoryMode = Sh7095MemoryMode.DirectMapped; busA.OnRead = null; GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); };
                busB.OnRead = () => { b.MemoryMode = Sh7095MemoryMode.DirectMapped; busB.OnRead = null; GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); };
                block = Sh2X64Block.TryCompile([0x6102, 0x6322], true, true)!;
                Interpret(a.Cpu, 0x6102); Interpret(a.Cpu, 0x6322); b.Cpu.ExecuteX64Payload(block);
                Assert.Equal(0UL, b.Cpu.GetNativeTimingProfile().CachedReadHits);
            }
            Equal(a, b);
        }
        finally
        {
            busA.OnRead = busB.OnRead = null;
            try { block?.Dispose(); } finally { try { b?.Cpu.ClearCodeCache(); } finally { a?.Cpu.ClearCodeCache(); } }
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void FullExecutionKeepsPinsMacModeChangesAndFaultRecovery(bool fault, bool live)
    {
        if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture != Architecture.X64) return;
        Sh7095? a = null, b = null; var busA = new Bus(); var busB = new Bus();
        try
        {
            a = new(busA); b = new(busB);
            Configure(a, 0x06001000, 3, 1, false); Configure(b, 0x06001000, 3, 1, false);
            ushort[] words = [0x6102, 0x6321, 0x402f, 0x2011, 0xaffa, 0x0009];
            foreach (var soc in new[] { a, b })
            {
                for (int i = 0; i < words.Length; i++) soc.Write16((uint)(0xc0000800 + i * 2), words[i]);
                soc.Cpu.ProgramCounter = 0xc0000800; soc.Cpu.R[0] = 0xc0000200; soc.Cpu.R[2] = 0x06001000;
                soc.Cpu.AccelerateIdleLoops = false;
            }
            b.Cpu.UseCompiledBlocks = b.Cpu.UseFullX64Blocks = b.Cpu.UseMultiInstructionX64Blocks = true;
            b.Cpu.UseNativeCachedReads = b.Cpu.UseNativeTimingProfile = true;
            b.Cpu.UseLiveX64Context = live;
            for (int pass = 0; pass < 3; pass++)
            {
                if (pass == 1)
                {
                    a.MemoryMode = b.MemoryMode = Sh7095MemoryMode.DirectMapped;
                    if (fault)
                    {
                        busA.OnRead = () => { a.Cpu.ClearCodeCache(); throw new InvalidOperationException("fault"); };
                        busB.OnRead = () => { b.Cpu.ClearCodeCache(); throw new InvalidOperationException("fault"); };
                        Assert.Throws<InvalidOperationException>(() => a.Cpu.Execute(128));
                        Assert.Throws<InvalidOperationException>(() => b.Cpu.Execute(128));
                        busA.OnRead = busB.OnRead = null;
                        Equal(a, b); GC.Collect(); GC.WaitForPendingFinalizers();
                    }
                }
                if (pass == 2) a.MemoryMode = b.MemoryMode = Sh7095MemoryMode.Cached;
                foreach (int budget in new[] { 1, 2, 3, 17, 64, 127, 511 })
                {
                    Assert.Equal(a.Cpu.Execute(budget), b.Cpu.Execute(budget)); Equal(a, b);
                }
            }
            Assert.True(b.Cpu.GetNativeTimingProfile().CachedReadHits > 0);
            Assert.Equal(0, b.Cpu.CompiledBodyBuildCount);
        }
        finally
        {
            busA.OnRead = busB.OnRead = null;
            try { b?.Cpu.ClearCodeCache(); } finally { a?.Cpu.ClearCodeCache(); }
        }
    }

    private static void Configure(Sh7095 soc, uint start, int way, byte control, bool direct)
    {
        soc.Reset(); soc.MemoryMode = Sh7095MemoryMode.Cached;
        soc.Write8(0xfffffe92, (byte)(1 | way << 6));
        soc.Write8(0x60000000 | (start & 0x1ffffff0) | 4, 0);
        int line = way * 64 + (int)(start >> 4 & 63);
        uint cache = start >> 29 == 6 ? start & 4095 : (uint)(line * 16);
        for (int i = 0; i < 16; i++) soc.Write8(0xc0000000 + cache + (uint)i, (byte)(0x80 + i * 7));
        soc.Write8(0xfffffe92, control);
        if (direct) soc.MemoryMode = Sh7095MemoryMode.DirectMapped;
    }
    private static void Prepare(Sh7095 soc, uint address, bool due)
    {
        uint[] state = new uint[Sh2X64Context.Words]; soc.Cpu.CaptureX64State(state);
        state[Sh2X64Context.BusDeadlineLow] = state[Sh2X64Context.BusDeadlineHigh] = due ? 0 : uint.MaxValue;
        soc.Cpu.RestoreX64State(state); soc.Cpu.R[0] = address;
    }
    private static void Equal(Sh7095 a, Sh7095 b)
    {
        uint[] expected = new uint[Sh2X64Context.Words], actual = new uint[expected.Length];
        a.Cpu.CaptureX64State(expected); b.Cpu.CaptureX64State(actual);
        Assert.Equal(a.Cpu.R, b.Cpu.R); Assert.Equal(expected, actual);
        byte[] stateA = new byte[Sh7095.ContextSize], stateB = new byte[stateA.Length];
        a.Cpu.SynchronizeBusClock(); b.Cpu.SynchronizeBusClock(); a.SaveContext(stateA); b.SaveContext(stateB);
        Assert.Equal(stateA, stateB);
    }
    private sealed class Bus : ISh2Bus
    {
        internal readonly List<(uint Address, int Size)> Reads = [];
        internal Action? OnRead;
        private static byte Value(uint a) => (byte)(a * 13 + (a >> 10) + 3);
        public byte Read8(uint address) { Reads.Add((address, 1)); OnRead?.Invoke(); return Value(address); }
        public ushort Read16(uint address) { Reads.Add((address, 2)); OnRead?.Invoke(); return (ushort)(Value(address) << 8 | Value(address + 1)); }
        public uint Read32(uint address) { Reads.Add((address, 4)); OnRead?.Invoke(); return (uint)(Value(address) << 24 | Value(address + 1) << 16 | Value(address + 2) << 8 | Value(address + 3)); }
        public void Write8(uint address, byte value) { } public void Write16(uint address, ushort value) { } public void Write32(uint address, uint value) { }
        public int WaitStates(uint address, bool write) => 2;
    }
}
