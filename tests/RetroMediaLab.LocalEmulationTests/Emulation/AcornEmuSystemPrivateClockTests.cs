using System.Reflection;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services;
using RetroMediaLab.Chips.Controllers.Acorn.SystemClock.Services;
using RetroMediaLab.Chips.Controllers.Acorn.SystemCpuClock.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Services;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuSystemPrivateClockTests
{
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

    private static AcornSystemMachine Create(bool disk, bool timer, byte mode)
    {
        var rom = new byte[8192]; rom[^4] = 0; rom[^3] = 2; rom[^2] = 0; rom[^1] = 3;
        var characters = Enumerable.Range(0, 2048).Select(index => (byte)(index * 37 & 63)).ToArray();
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
