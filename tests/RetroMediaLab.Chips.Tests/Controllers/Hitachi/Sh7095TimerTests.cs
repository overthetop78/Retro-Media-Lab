using System.Buffers.Binary;
using RetroMediaLab.Chips.Controllers.Hitachi.SH7095;
using RetroMediaLab.Chips.CPU.Hitachi.SH2;
using RetroMediaLab.Chips.CPU.Hitachi.SH2.Contracts;

namespace RetroMediaLab.Chips.Tests.Controllers.Hitachi;

public sealed class Sh7095TimerTests
{
    [Theory]
    [InlineData(0, 1, 1, true, 0)]
    [InlineData(0, 1, 65535, true, 65534)]
    [InlineData(0, 0, 0, true, 65535)]
    [InlineData(1, 32, 4, true, 31)]
    [InlineData(2, 65535, 0, false, 12345)]
    [InlineData(0, 4660, 17185, false, 57005)]
    public void CoalescedTimerMatchesMaterializedOriginalAndRearms(int select, int compareA, int compareB, bool clear, int counter)
    {
        Sh7095? a = null, b = null;
        try
        {
            a = new Sh7095(new Bus()); b = new Sh7095(new Bus());
            foreach (var c in new[] { a, b })
            {
                c.Write8(0xfffffe16, (byte)select); c.Write8(0xfffffe60, 3); c.Write8(0xfffffe66, 0x56); c.Write8(0xfffffe10, 0x0e);
                c.Write8(0xfffffe17, 0xe0); c.Write16(0xfffffe14, (ushort)compareA);
                c.Write8(0xfffffe17, 0xf0); c.Write16(0xfffffe14, (ushort)compareB); c.Write8(0xfffffe17, 0xe0);
                c.Write8(0xfffffe11, 0); c.AdvanceCycles(65536 * (select == 0 ? 8 : select == 1 ? 32 : 128));
                Assert.Equal(0x0e, c.Read8(0xfffffe11) & 0x0e);
                c.Write8(0xfffffe11, (byte)(clear ? 0x0f : 0x0e)); c.Write16(0xfffffe12, (ushort)counter);
            }
            b.UseCoalescedLatchedTimerEvents = true;
            byte[] expected = new byte[Sh7095.ContextSize], actual = new byte[expected.Length];
            foreach (int clocks in new[] { 1, 7, 8, 31, 127, 1024, 65537, 1000000 })
            {
                a.AdvanceCycles(clocks); b.AdvanceCycles(clocks);
                a.SaveContext(expected); b.SaveContext(actual); Assert.Equal(expected, actual);
            }
            foreach (byte flags in new byte[] { 1, 3, 5, 9, 0x0f })
            {
                a.Write8(0xfffffe11, flags); b.Write8(0xfffffe11, flags);
                foreach (int clocks in new[] { 1, 8, 64, 65536 })
                {
                    a.AdvanceCycles(clocks); b.AdvanceCycles(clocks);
                    Assert.Equal(a.Read8(0xfffffe11), b.Read8(0xfffffe11)); Assert.Equal(a.Read16(0xfffffe12), b.Read16(0xfffffe12));
                    a.SaveContext(expected); b.SaveContext(actual); Assert.Equal(expected, actual);
                }
            }
            b.LoadContext(actual); b.SaveContext(actual); Assert.Equal(expected, actual);
            b.UseCoalescedLatchedTimerEvents = false; b.SaveContext(actual); Assert.Equal(expected, actual);
        }
        finally { try { b?.Cpu.ClearCodeCache(); } finally { a?.Cpu.ClearCodeCache(); } }
    }

    [Fact]
    public void LatchedClearOnOneStopsEightClockWakeupsUntilFlagsAreCleared()
    {
        var c = new Sh7095(new Bus());
        try
        {
            c.Write8(0xfffffe17, 0xe0); c.Write16(0xfffffe14, 1);
            c.Write8(0xfffffe17, 0xf0); c.Write16(0xfffffe14, 1); c.Write8(0xfffffe17, 0xe0);
            c.Write16(0xfffffe12, 0xffff); c.Write8(0xfffffe11, 1); c.AdvanceCycles(8); c.AdvanceCycles(8);
            Assert.Equal(0x0f, c.Read8(0xfffffe11));
            Assert.Equal(c.Ticks + 8, new Sh7095.CpuBus(c).NextEventCycle);
            c.UseCoalescedLatchedTimerEvents = true;
            Assert.Equal(c.Ticks + 65536 * 8UL, new Sh7095.CpuBus(c).NextEventCycle);
            c.AdvanceCycles(100000); Assert.Equal(0, c.Read16(0xfffffe12));
            c.Write8(0xfffffe11, 1); Assert.Equal(c.Ticks + 8, new Sh7095.CpuBus(c).NextEventCycle);
            c.AdvanceCycles(7); Assert.Equal(1, c.Read8(0xfffffe11)); c.AdvanceCycles(1);
            Assert.Equal(0x0d, c.Read8(0xfffffe11)); // Compare A/B rearmed; overflow stays clear.
        }
        finally { c.Cpu.ClearCodeCache(); }
    }
    private sealed class Bus : ISh2Bus
    {
        public byte Read8(uint address) => 0; public ushort Read16(uint address) => 0; public uint Read32(uint address) => 0;
        public void Write8(uint address, byte value) { } public void Write16(uint address, ushort value) { } public void Write32(uint address, uint value) { }
    }
}

