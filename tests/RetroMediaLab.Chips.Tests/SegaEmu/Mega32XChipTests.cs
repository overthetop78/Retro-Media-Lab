using RetroMediaLab.Chips.Tests.CPU.Hitachi;
using RetroMediaLab.Chips.Controllers.Hitachi.SH7095;
using RetroMediaLab.Chips.Video.Sega.VDP32X;
using RetroMediaLab.Chips.Audio.Sega.PWM32X;
using RetroMediaLab.Chips.Audio.Common.Resampling;

namespace RetroMediaLab.Chips.Tests.SegaEmu;

public sealed class Mega32XChipTests
{
    [Theory, InlineData(0), InlineData(1), InlineData(2)]
    public void CacheLookupTracksPurgeTagWritesAndWaySelection(int change)
    {
        var memory = new Sh2Tests.MemoryBus(); memory.Program(9);
        memory.Write16(0x300, 0x1234); memory.ReadWaitStates = 3;
        var soc = new Sh7095(memory); soc.Write8(0xfffffe92, 1);
        Assert.Equal(0x1234, soc.Read16(0x300));
        Assert.Equal(0, soc.WaitStates(0x300, false));
        byte[] saved = new byte[Sh7095.ContextSize]; soc.SaveContext(saved);
        memory.Write16(0x300, 0xabcd);
        Assert.Equal(0x1234, soc.Read16(0x300));
        if (change == 0) soc.Write8(0x40000300, 0);
        else if (change == 1) soc.Write8(0x60000300, 0);
        else soc.Write8(0xfffffe92, 9);
        Assert.Equal(3, soc.WaitStates(0x300, false));
        Assert.Equal(0xabcd, soc.Read16(0x300));
        soc.LoadContext(saved);
        Assert.Equal(0x1234, soc.Read16(0x300));
    }

    [Theory, InlineData(false), InlineData(true)]
    public void TimerReadsBeforeDeadlineKeepCompareIrqAndFractionAcrossState(bool clearOnCompare)
    {
        var memory = new Sh2Tests.MemoryBus(); memory.Program(9);
        var soc = new Sh7095(memory);
        soc.Write16(0xfffffe14, 16);
        soc.Write8(0xfffffe60, 6);
        soc.Write8(0xfffffe10, 8);
        soc.Write8(0xfffffe11, clearOnCompare ? (byte)1 : (byte)0);
        soc.Cpu.StatusRegister = 0;
        var bus = new Sh7095.CpuBus(soc);
        Assert.Equal(128UL, bus.NextEventCycle);
        soc.AdvanceCycles(63);
        Assert.Equal(7, soc.Read16(0xfffffe12));
        Assert.Equal(128UL, bus.NextEventCycle);
        soc.AdvanceCycles(64);
        Assert.Equal(15, soc.Read16(0xfffffe12));
        Assert.Equal(0, soc.Read8(0xfffffe11) & 8);
        Assert.False(soc.Cpu.InterruptReady);
        byte[] saved = new byte[Sh7095.ContextSize]; soc.SaveContext(saved);
        soc.AdvanceCycles(1);
        Assert.Equal(clearOnCompare ? 0 : 16, soc.Read16(0xfffffe12));
        Assert.Equal(8, soc.Read8(0xfffffe11) & 8);
        Assert.True(soc.Cpu.InterruptReady);
        byte[] expected = new byte[Sh7095.ContextSize]; soc.SaveContext(expected);
        soc.LoadContext(saved); soc.AdvanceCycles(1);
        byte[] actual = new byte[Sh7095.ContextSize]; soc.SaveContext(actual);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DirectMemoryKeepsP1WaitsCacheRamAndOriginalCacheModel()
    {
        var memory = new Sh2Tests.MemoryBus(); memory.Program(9); memory.ReadWaitStates = 5;
        memory.Write16(0x300, 0x1234);
        var soc = new Sh7095(memory); Assert.Equal(Sh7095MemoryMode.Cached, soc.MemoryMode);
        soc.Write8(0xfffffe92, 1); Assert.Equal(0x1234, soc.Read16(0x300));
        memory.Write16(0x300, 0xabcd);
        Assert.Equal(0x1234, soc.Read16(0x300));
        soc.MemoryMode = Sh7095MemoryMode.DirectMapped;
        Assert.Equal(0xabcd, soc.Read16(0x300)); Assert.Equal(0, soc.WaitStates(0x300, false));
        Assert.Equal(5, soc.WaitStates(0x20000300, false));
        Assert.Equal(0xabcd, soc.Fetch16(0x300, out int cachedWait)); Assert.Equal(0, cachedWait);
        Assert.Equal(0xabcd, soc.Fetch16(0x20000300, out int uncachedWait)); Assert.Equal(5, uncachedWait);
        soc.Write32(0x20000400, 0x12345678); Assert.Equal(0x12345678u, memory.Read32(0x400));
        soc.Write32(0xc0000800, 0xabcdef01); Assert.Equal(0xabcdef01u, soc.Read32(0xc0000800));
        soc.MemoryMode = Sh7095MemoryMode.Cached; Assert.Equal(0x1234, soc.Read16(0x300));
    }
    [Fact]
    public void CompiledCodeFollowsCacheByteWritesAndRefills()
    {
        var memory = new Sh2Tests.MemoryBus(); memory.Program(9);
        memory.Write16(0x300, 0xe001); memory.Write16(0x302, 0xaffe); memory.Write16(0x304, 9);
        var soc = new Sh7095(memory); soc.Write8(0xfffffe92, 1); soc.Cpu.UseCompiledBlocks = true;
        void ExecuteFirst(uint expected)
        {
            soc.Cpu.ProgramCounter = 0x300; soc.Execute(1); Assert.Equal(expected, soc.Cpu.R[0]);
        }
        ExecuteFirst(1);
        soc.Write8(0x301, 2); ExecuteFirst(2);
        memory.Write16(0x300, 0xe003); ExecuteFirst(2);
        foreach (uint address in new uint[] { 0x700, 0xb00, 0xf00, 0x1300 }) soc.Read16(address);
        ExecuteFirst(3);
        soc.Write8(0xfffffe92, 0x11); ExecuteFirst(3);
    }
    [Fact]
    public void SoCCacheReadsWordsAndLongsAcrossLinesAndHonorsCodeDataDisable()
    {
        var memory = new Sh2Tests.MemoryBus();
        for (uint i = 0x300; i < 0x330; i++) memory.Write8(i, (byte)i);
        var soc = new Sh7095(memory); soc.Write8(0xfffffe92, 1);
        Assert.Equal(0x0e0f1011u, soc.Read32(0x30e));
        Assert.Equal(0x0f10, soc.Read16(0x30f));
        memory.Write16(0x300, 0xabcd);
        Assert.Equal(0x0001, soc.Fetch16(0x300));
        soc.Write8(0xfffffe92, 3); // code cache disabled, data cache retained
        Assert.Equal(0xabcd, soc.Fetch16(0x300)); Assert.Equal(0x0001, soc.Read16(0x300));
        soc.Write8(0xfffffe92, 5); // data cache disabled, code cache retained
        Assert.Equal(0x0001, soc.Fetch16(0x300)); Assert.Equal(0xabcd, soc.Read16(0x300));
        soc.Write8(0xfffffe92, 0x11);
        Assert.Equal(0xabcd, soc.Fetch16(0x300));
        soc.Write16(0xfffffe14, 0x1234); soc.Write8(0xfffffe17, 0x10);
        soc.Write16(0xfffffe14, 0x5678);
        Assert.Equal(0x5678, soc.Read16(0xfffffe14));
        soc.Write8(0xfffffe17, 0); Assert.Equal(0x1234, soc.Read16(0xfffffe14));
    }

    [Fact]
    public void SoCNativeRuntimeMatchesReferenceWithInterruptAndStateRestore()
    {
        var a = new Sh2Tests.MemoryBus(); var b = new Sh2Tests.MemoryBus();
        a.Program(0xe005, 0x4010, 0x8bfd, 0x0009, 0xaffe, 0x0009);
        a.Bytes.CopyTo(b.Bytes, 0);
        var native = new Sh7095(a); var reference = new Sh7095(b);
        var original = new RetroMediaLab.Chips.CPU.Hitachi.SH2.Sh2<Sh7095.CpuBus>(new Sh7095.CpuBus(reference));
        original.Reset();
        byte[] actual = new byte[RetroMediaLab.Chips.CPU.Hitachi.SH2.Sh2<Sh7095.CpuBus>.ContextSize];
        byte[] expected = new byte[actual.Length];
        foreach (int budget in new[] { 1, 7, 31, 2, 84, 13 })
        {
            native.Cpu.Execute(budget); original.Execute(budget);
            native.Cpu.SaveContext(actual); original.SaveContext(expected); Assert.Equal(expected, actual);
        }
        native.Cpu.LoadContext(actual); original.LoadContext(expected);
        a.Write32(0x50 * 4, 0x200); b.Write32(0x50 * 4, 0x200);
        a.Write16(0x200, 0xe02a); b.Write16(0x200, 0xe02a);
        native.Cpu.StatusRegister = original.StatusRegister = 0;
        native.Cpu.SetInterrupt(6, 0x50); original.SetInterrupt(6, 0x50);
        native.Cpu.Execute(1); original.Execute(1);
        native.Cpu.SaveContext(actual); original.SaveContext(expected); Assert.Equal(expected, actual);
        Assert.Equal(42u, native.Cpu.R[0]);
    }
    [Theory, InlineData(3, 7), InlineData(7, 3), InlineData(65535, 0)]
    public void SoCPeripheralDeadlinesMatchSingleClockStepping(int compareA, int compareB)
    {
        var fine = new Sh7095(new Sh2Tests.MemoryBus()); var batch = new Sh7095(new Sh2Tests.MemoryBus());
        foreach (var soc in new[] { fine, batch })
        {
            soc.Write16(0xfffffe14, (ushort)compareA); soc.Write8(0xfffffe17, 0x10);
            soc.Write16(0xfffffe14, (ushort)compareB); soc.Write8(0xfffffe11, 1);
            soc.Write8(0xfffffe10, 0x0e);
        }
        for (int i = 0; i < 100003; i++) fine.AdvanceCycles(1);
        batch.AdvanceCycles(100003);
        byte[] expected = new byte[Sh7095.ContextSize], actual = new byte[Sh7095.ContextSize];
        fine.SaveContext(expected); batch.SaveContext(actual); Assert.Equal(expected, actual);
        batch.LoadContext(expected); batch.AdvanceCycles(71); fine.AdvanceCycles(71);
        fine.SaveContext(expected); batch.SaveContext(actual); Assert.Equal(expected, actual);
    }
    [Fact]
    public void SoCDivisionTimerCacheAndDmaAreIndependentOfSega()
    {
        var memory = new Sh2Tests.MemoryBus(); memory.Program(9);
        var soc = new Sh7095(memory);
        soc.Write32(0xffffff00, 3); soc.Write32(0xffffff04, unchecked((uint)-10));
        Assert.Equal(unchecked((uint)-3), soc.Read32(0xffffff04)); Assert.Equal(uint.MaxValue, soc.Read32(0xffffff10));
        soc.Write16(0xfffffe14, 3); soc.Write8(0xfffffe10, 8); soc.AdvanceCycles(24);
        Assert.Equal(8, soc.Read8(0xfffffe11) & 8);
        memory.Write32(0x300, 0x12345678); soc.Write8(0xfffffe92, 1);
        Assert.Equal(0x12345678u, soc.Read32(0x300)); memory.Write32(0x300, 0xaabbccdd);
        Assert.Equal(0x12345678u, soc.Read32(0x300)); Assert.Equal(0xaabbccddu, soc.Read32(0x20000300));
        soc.Write8(0xfffffe92, 0x11); Assert.Equal(0xaabbccddu, soc.Read32(0x300));
        soc.Write32(0xffffff80, 0x300); soc.Write32(0xffffff84, 0x400); soc.Write32(0xffffff88, 2);
        soc.Write32(0xffffff8c, 0x5605); soc.Write32(0xffffffb0, 1); soc.AdvanceCycles(4);
        Assert.Equal(0xaabbccddu, memory.Read32(0x400)); Assert.Equal(0u, soc.Read32(0xffffff88)); Assert.Equal(2u, soc.Read32(0xffffff8c) & 2);
        byte[] saved = new byte[Sh7095.ContextSize]; soc.SaveContext(saved); soc.Write8(0xfffffe92, 0); soc.LoadContext(saved);
        byte[] replay = new byte[saved.Length]; soc.SaveContext(replay); Assert.Equal(saved, replay);
    }

    [Fact]
    public void SoCSerialConnectsToABorrowedReceiver()
    {
        var a = new Sh7095(new Sh2Tests.MemoryBus()); var b = new Sh7095(new Sh2Tests.MemoryBus());
        a.SerialTransmitted = b.ReceiveSerial; a.Write8(0xfffffe02, 0x20); b.Write8(0xfffffe02, 0x10);
        a.Write8(0xfffffe03, 0x5a); a.AdvanceCycles(100000);
        Assert.Equal(0x5a, b.Read8(0xfffffe05)); Assert.Equal(0x40, b.Read8(0xfffffe04) & 0x40);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void VdpDecodesEachModeAndDeferredBankSwitch(int mode)
    {
        var vdp = new Vdp32X(); vdp.WriteRegister(0, (ushort)mode);
        vdp.WritePalette(2, 0x001f); vdp.WritePalette(4, 0x03e0); vdp.WriteFrame16(0, 0x100);
        vdp.WriteFrame16(0x200, mode == 1 ? (ushort)0x0102 : mode == 2 ? (ushort)0x001f : (ushort)0x0301);
        vdp.WriteFrame16(0x202, mode == 2 ? (ushort)0x03e0 : (ushort)0x0002);
        vdp.SetBlank(false, false); vdp.WriteRegister(10, 1); Assert.Equal(0, vdp.ReadRegister(10) & 1);
        vdp.SetBlank(false, true); Assert.Equal(1, vdp.ReadRegister(10) & 1);
        ushort[] pixels = new ushort[320]; Assert.True(vdp.RenderLine(0, pixels));
        Assert.Equal(0x001f, pixels[0]); Assert.Equal(0x03e0, pixels[mode == 3 ? 4 : 1]);
        Assert.Equal(0xffff0000u, Vdp32X.ToXrgb8888(pixels[0]));
        Assert.False(vdp.OverridesMegaDrive(0x1f, false)); Assert.True(vdp.OverridesMegaDrive(0x801f, false));
        byte[] saved = new byte[Vdp32X.ContextSize]; vdp.SaveContext(saved); vdp.Reset(); vdp.LoadContext(saved);
        Assert.True(vdp.RenderLine(0, pixels)); Assert.Equal(0x001f, pixels[0]);
    }

    [Fact]
    public void VdpOverwriteAndAutofillRespectByteMasksAndLowAddressWrap()
    {
        var vdp = new Vdp32X(); vdp.WriteFrame16(0, 0x1234); vdp.WriteFrame16(0x20000, 0x0056); Assert.Equal(0x1256, vdp.ReadFrame16(0));
        vdp.WriteFrame8(0, 0); Assert.Equal(0x1256, vdp.ReadFrame16(0));
        vdp.WriteRegister(4, 1); vdp.WriteRegister(6, 255); vdp.WriteRegister(8, 0xabcd);
        Assert.Equal(0xabcd, vdp.ReadFrame16(510)); Assert.Equal(0xabcd, vdp.ReadFrame16(0)); Assert.True(vdp.Busy);
        vdp.Advance(13); Assert.False(vdp.Busy);
    }

    [Fact]
    public void PwmFifoDrainsProducesAudioAndRestoresItsDeadline()
    {
        var output = new BandLimitedBuffer(4096); output.SetRates(53693175, 44100); var pwm = new Pwm32X(output);
        pwm.Write(0, 0x185, 0); pwm.Write(2, 1001, 0); pwm.Write(8, 250, 0); pwm.Write(8, 750, 0); pwm.Write(8, 500, 0);
        Assert.Equal(0x8000, pwm.Read(8, 0) & 0x8000); pwm.Update(10000); Assert.True(pwm.InterruptPending); Assert.True(pwm.DmaRequest);
        Assert.Equal(0x4000, pwm.Read(8, 10000) & 0x4000); pwm.Acknowledge(); Assert.False(pwm.DmaRequest);
        byte[] saved = new byte[Pwm32X.ContextSize]; pwm.SaveContext(saved); pwm.Update(50000); pwm.LoadContext(saved);
        byte[] replay = new byte[saved.Length]; pwm.SaveContext(replay); Assert.Equal(saved, replay);
        pwm.EndFrame(100000); output.EndFrame(100000); short[] audio = new short[2048]; int samples = output.ReadSamples(audio, Math.Min(output.SamplesAvailable, 1024));
        Assert.True(samples > 0); Assert.Contains(audio, value => value != 0);
    }
}
