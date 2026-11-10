using System.Reflection;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services;
using RetroMediaLab.Chips.Controllers.Acorn.SystemClock.Services;
using RetroMediaLab.Chips.Controllers.Acorn.SystemCpuClock.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Services;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuSystemPrivateClockTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void IsolatedViaReadsRetainKeyboardIrqVideoStateAndControlModeFallback(byte mode)
    {
        using var actual = Create(true, true, mode);
        using var reference = Create(true, true, mode);
        reference.GroupViaReadPeriods = false;
        var via = Field<RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Services.Via6522>(actual, "_via");
        var otherVia = Field<RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Services.Via6522>(reference, "_via");
        var video = Field<SystemVideo>(actual, "_video"); var otherVideo = Field<SystemVideo>(reference, "_video");
        var actualVdu = Field<RetroMediaLab.Chips.Controllers.Acorn.Vdu80CharacterGenerator.Services.Vdu80CharacterGenerator>(video, "_monochrome");
        var expectedVdu = Field<RetroMediaLab.Chips.Controllers.Acorn.Vdu80CharacterGenerator.Services.Vdu80CharacterGenerator>(otherVideo, "_monochrome");
        var videoEvents = new List<(long, bool?)>(); var expectedVideoEvents = new List<(long, bool?)>();
        actualVdu.VideoChanged = level => videoEvents.Add((actualVdu.TimeNanoseconds, level));
        expectedVdu.VideoChanged = level => expectedVideoEvents.Add((expectedVdu.TimeNanoseconds, level));
        for (var field = 0; field < 8; field++)
        {
            var input = field % 2 == 0 ? EmulationInputSnapshot.Empty with
                { Keys = new HashSet<EmulationKey> { EmulationKey.A } } : EmulationInputSnapshot.Empty;
            actual.SetInput(input); reference.SetInput(input);
            CompareFields(reference, actual, 1);
            Assert.Equivalent(otherVia.CaptureState(), via.CaptureState(), strict: true);
            Assert.Equal(expectedVideoEvents, videoEvents);
            expectedVideoEvents.Clear(); videoEvents.Clear();
            var a = Field<RetroMediaLab.Chips.Controllers.Acorn.Vdu80CharacterGenerator.Services.Vdu80CharacterGenerator>(video, "_monochrome");
            var b = Field<RetroMediaLab.Chips.Controllers.Acorn.Vdu80CharacterGenerator.Services.Vdu80CharacterGenerator>(otherVideo, "_monochrome");
            Assert.Equal((b.DotClockCount, b.CharacterClockCount, b.TimeNanoseconds),
                (a.DotClockCount, a.CharacterClockCount, a.TimeNanoseconds));
        }
        Assert.True(actual.RepresentedViaReadPeriods > 0);
        Assert.Equal(0, reference.RepresentedViaReadPeriods);
        var count = actual.RepresentedViaReadPeriods;
        // CA2 pulse output: the ordinary bus/clock route must handle handshakes.
        actual.Write(0x0e0c, 0x0a); reference.Write(0x0e0c, 0x0a);
        CompareFields(reference, actual, 2);
        Assert.Equal(count, actual.RepresentedViaReadPeriods);
        Assert.Equivalent(otherVia.CaptureState(), via.CaptureState(), strict: true);
        actual.Write(0x0e0c, 0); reference.Write(0x0e0c, 0);
        CompareFields(reference, actual, 2);
        Assert.True(actual.RepresentedViaReadPeriods > count);
        Assert.Equivalent(otherVia.CaptureState(), via.CaptureState(), strict: true);
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(3, 0, true)]
    [InlineData(3, 1, true)]
    [InlineData(3, 2, true)]
    [InlineData(0, 3, true)]
    [InlineData(0, 0, false)]
    [InlineData(3, 2, false)]
    public void LinearCharactersPreserveFieldsWritesCrtcAndVduState(byte mode, byte skew, bool constant)
    {
        using var actual = Create(true, true, mode, constant);
        using var reference = Create(true, true, mode, constant);
        var video = Field<SystemVideo>(actual, "_video"); var expected = Field<SystemVideo>(reference, "_video");
        expected.GroupLinearCharacters = false;
        foreach (var machine in new[] { actual, reference })
        {
            machine.Write(0x1840, 8); machine.Write(0x1841, (byte)(mode | skew << 4 | skew << 6));
            // Repeated runs, transitions at their ends, and distinct glyphs.
            for (var address = 0; address < 1024; address++)
                machine.Write((ushort)(0x1000 + address), (byte)((address / 37) % 5));
        }
        for (var field = 0; field < 8; field++)
        {
            CompareFields(reference, actual, 1);
            var crtc = Field<RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Services.Crtc6845>(video, "_crtc");
            var other = Field<RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Services.Crtc6845>(expected, "_crtc");
            Assert.Equivalent(other.CaptureState(), crtc.CaptureState(), strict: true);
            var a = Field<RetroMediaLab.Chips.Controllers.Acorn.Vdu80CharacterGenerator.Services.Vdu80CharacterGenerator>(video, "_monochrome");
            var b = Field<RetroMediaLab.Chips.Controllers.Acorn.Vdu80CharacterGenerator.Services.Vdu80CharacterGenerator>(expected, "_monochrome");
            Assert.Equal((b.DotClockCount, b.CharacterClockCount, b.TimeNanoseconds, b.VideoWhite),
                (a.DotClockCount, a.CharacterClockCount, a.TimeNanoseconds, a.VideoWhite));
            var address = (ushort)(0x1000 + ((field * 83) & 1023));
            actual.Write(address, (byte)(field + 9)); reference.Write(address, (byte)(field + 9));
        }
        if (constant) Assert.True(video.RepresentedLinearCharacters > 0);
        Assert.Equal(0, expected.RepresentedLinearCharacters);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void SettledCharacterPeriodsMatchAllFieldsAndVideoClockTotals(byte mode)
    {
        using var actual = Create(true, true, mode, constantCharacters: true);
        using var reference = Create(true, true, mode, constantCharacters: true);
        var video = Field<SystemVideo>(actual, "_video"); var expected = Field<SystemVideo>(reference, "_video");
        expected.GroupSettledCharacters = false;
        CompareFields(reference, actual, 8);
        Assert.True(video.RepresentedSettledCharacters > 0);
        Assert.Equal(0, expected.RepresentedSettledCharacters);
        var a = Field<RetroMediaLab.Chips.Controllers.Acorn.Vdu80CharacterGenerator.Services.Vdu80CharacterGenerator>(video, "_monochrome");
        var b = Field<RetroMediaLab.Chips.Controllers.Acorn.Vdu80CharacterGenerator.Services.Vdu80CharacterGenerator>(expected, "_monochrome");
        Assert.Equal(b.DotClockCount, a.DotClockCount); Assert.Equal(b.CharacterClockCount, a.CharacterClockCount);
        Assert.Equal(b.TimeNanoseconds, a.TimeNanoseconds);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WaitOrDmaKeepsTheOrdinaryPathAndResumesAtTheSameCpuPhase(bool dma)
    {
        using var actual = Create(true, true, 3); using var reference = Create(true, true, 3);
        reference.GroupPrivateClockPeriods = false;
        foreach (var machine in new[] { reference, actual })
        {
            var control = Field<SystemCpuClockControl>(machine, "_fastCpuClock");
            if (dma) control.SetBusRequest(false); else control.SetMemoryReady(false);
            for (var i = 0; i < 4; i++) machine.Tick();
        }
        var represented = actual.RepresentedPrivateClockPeriods;
        CompareFields(reference, actual, 3);
        Assert.Equal(represented, actual.RepresentedPrivateClockPeriods);
        foreach (var machine in new[] { reference, actual })
        {
            var control = Field<SystemCpuClockControl>(machine, "_fastCpuClock");
            if (dma) control.SetBusRequest(true); else control.SetMemoryReady(true);
        }
        CompareFields(reference, actual, 8);
        Assert.True(actual.RepresentedPrivateClockPeriods > represented);
    }

    [Fact]
    public void PendingDiskCommandAndNmiKeepTheirEffectsBeforePrivateCyclesResume()
    {
        using var actual = Create(true, true, 3); using var reference = Create(true, true, 3);
        reference.GroupPrivateClockPeriods = false;
        foreach (var machine in new[] { reference, actual })
        {
            var controller = Field<RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Services.Intel8271>(machine, "_fdc");
            controller.Write(0, 0x52); controller.Write(1, 0); controller.Write(1, 1);
            Field<Cpu6502>(machine, "_cpu6502").SetNmi(true);
        }
        CompareFields(reference, actual, 8);
        Assert.Equal(Field<RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Services.Intel8271>(reference, "_fdc").Nmi,
            Field<RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Services.Intel8271>(actual, "_fdc").Nmi);
    }

    [Theory]
    [InlineData(false, false, 0)]
    [InlineData(true, false, 0)]
    [InlineData(true, true, 0)]
    [InlineData(false, true, 3)]
    [InlineData(true, false, 3)]
    public void PrivatePrefixesMatchCpuRamPeripheralWritesAudioAndFieldParity(bool disk, bool timer, byte mode)
    {
        using var actual = Create(disk, timer, mode); using var reference = Create(disk, timer, mode);
        reference.GroupPrivateClockPeriods = false;
        CompareFields(reference, actual, 8);
        Assert.True(actual.RepresentedPrivateClockPeriods > 0);
        Assert.Equal(0, reference.RepresentedPrivateClockPeriods);
    }

    private static void CompareFields(AcornSystemMachine reference, AcornSystemMachine actual, int count)
    {
        for (var field = 0; field < count; field++)
        {
            Assert.Equal(reference.RunFrame(), actual.RunFrame());
            Assert.Equal(reference.DrainAudio(), actual.DrainAudio());
            Assert.Equal(reference.VideoFrameDurationSeconds, actual.VideoFrameDurationSeconds);
            Assert.Equal(reference.VideoScanMode, actual.VideoScanMode);
            var expectedCpu = Field<Cpu6502>(reference, "_cpu6502"); var cpu = Field<Cpu6502>(actual, "_cpu6502");
            Assert.Equal((expectedCpu.PC, expectedCpu.A, expectedCpu.X, expectedCpu.Y, expectedCpu.S, expectedCpu.P, expectedCpu.Cycles),
                (cpu.PC, cpu.A, cpu.X, cpu.Y, cpu.S, cpu.P, cpu.Cycles));
            Assert.Equal(Field<byte[]>(reference, "_ram"), Field<byte[]>(actual, "_ram"));
            Assert.Equal(Field<long>(reference, "_ticks"), Field<long>(actual, "_ticks"));
            Assert.Equal(Field<SystemClockGenerator>(reference, "_fastClock").InputClockCount,
                Field<SystemClockGenerator>(actual, "_fastClock").InputClockCount);
            var expectedClock = Field<SystemCpuClockControl>(reference, "_fastCpuClock");
            var clock = Field<SystemCpuClockControl>(actual, "_fastCpuClock");
            Assert.Equal(expectedClock.InputClockCount, clock.InputClockCount);
            Assert.Equal(expectedClock.CpuClockCount, clock.CpuClockCount);
        }
    }

    private static AcornSystemMachine Create(bool disk, bool timer, byte mode, bool constantCharacters = false)
    {
        var rom = new byte[8192]; rom[^4] = 0; rom[^3] = 2; rom[^2] = 0; rom[^1] = 3;
        var characters = constantCharacters ? new byte[2048] : Enumerable.Range(0, 2048).Select(index => (byte)(index * 37 & 63)).ToArray();
        var machine = new AcornSystemMachine(SystemCpuCard.Fast6502A, rom, characters, [(0x2000, 0xc000)], disk);
        try
        {
            byte[] program = [0x58, 0xa2, 0, 0xe8, 0x8e, 0, 0x20, 0xad, 1, 0x0e, 0x8d, 0, 0x10, 0x4c, 3, 2];
            byte[] handler = [0xee, 1, 0x20, 0xad, 4, 0x0e, 0x40];
            for (var i = 0; i < program.Length; i++) machine.Write((ushort)(0x200 + i), program[i]);
            for (var i = 0; i < handler.Length; i++) machine.Write((ushort)(0x300 + i), handler[i]);
            byte[] registers = [83, 80, 81, 0x11, 3, 0, 1, 2, mode, 1, 0x20, 1, 0, 0, 0, 0];
            for (byte register = 0; register < registers.Length; register++)
            { machine.Write(0x1840, register); machine.Write(0x1841, registers[register]); }
            if (timer)
            {
                machine.Write(0x0e0b, 0x40); machine.Write(0x0e0e, 0xc0);
                machine.Write(0x0e04, 0x33); machine.Write(0x0e05, 0);
            }
            return machine;
        }
        catch { machine.Dispose(); throw; }
    }

    private static T Field<T>(object owner, string name) => (T)owner.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;
}
