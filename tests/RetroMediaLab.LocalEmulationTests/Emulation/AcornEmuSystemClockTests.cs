using System.Reflection;
using RetroMediaLab.Chips.Controllers.Acorn.SystemClock.Services;
using RetroMediaLab.Chips.Controllers.Acorn.SystemCpuClock.Services;
using Fdc8271 = RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Services.Intel8271;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services;
using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Services;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuSystemClockTests
{
    [Fact]
    public void FastBoardAndCpuReceiveTheirActualSourcePeriodsAndViaGetsTwoMhzPhases()
    {
        var board = Create();
        try
        {
            var source = Field<SystemClockGenerator>(board, "_fastClock");
            var cpu = Field<Cpu6502>(board, "_cpu6502");
            var via = Field<Via6522>(board, "_via");
            var start = source.InputClockCount;
            var cycles = cpu.Cycles;
            var reference = Field<long>(board, "_ticks");
            board.Write(0x0e04, 0); board.Write(0x0e05, 1);
            for (var i = 0; i < 16; i++) board.Tick();
            Assert.Equal(start + 16 * 12, source.InputClockCount);
            Assert.Equal(cycles + 16, cpu.Cycles);
            Assert.Equal(reference + 16, Field<long>(board, "_ticks"));
            Assert.Equal(241, Field<int>(via, "_timer1")); // 256, one load period, fifteen counts.
            Assert.False(cpu.PhaseTwoHigh);
            Assert.False(Field<bool>(via, "_phi2High"));
        }
        finally { board.Reset(true); }
    }

    [Fact]
    public void WaitStopsCpuAndViaButVideoAndDiskReferenceKeepTheirFreeClock()
    {
        var board = Create(disk: true);
        var control = Field<SystemCpuClockControl>(board, "_fastCpuClock");
        try
        {
            var cpu = Field<Cpu6502>(board, "_cpu6502");
            var via = Field<Via6522>(board, "_via");
            var video = Field<SystemVideo>(board, "_video");
            var fdc = Field<Fdc8271>(board, "_fdc");
            board.Write(0x0e04, 0); board.Write(0x0e05, 4);
            board.Read(0x0a00); // Real external transaction synchronizes the idle controller.
            var reference = Field<long>(board, "_ticks");
            var videoClocks = Field<long>(video, "_characterClocks");
            var diskClocks = fdc.InputClockCount;
            var cycles = cpu.Cycles;
            var timer = Field<int>(via, "_timer1");
            control.SetMemoryReady(false);
            for (var i = 0; i < 40; i++) board.Tick();
            Assert.True(cpu.PhaseTwoHigh);
            Assert.True(Field<bool>(via, "_phi2High"));
            Assert.Equal(cycles, cpu.Cycles);
            Assert.Equal(timer, Field<int>(via, "_timer1"));
            Assert.Equal(reference + 40, Field<long>(board, "_ticks"));
            Assert.Equal(videoClocks + 40, Field<long>(video, "_characterClocks"));
            board.Read(0x0a00);
            Assert.Equal(diskClocks + 40, fdc.InputClockCount);
            control.SetMemoryReady(true);
            board.Tick();
            Assert.Equal(cycles + 1, cpu.Cycles);
            Assert.False(cpu.PhaseTwoHigh);
        }
        finally { control.SetMemoryReady(true); board.Reset(true); }
    }

    [Fact]
    public void GrantedBusDoesNotReadThePeripheralUntilTheCpuResumes()
    {
        var board = Create();
        var control = Field<SystemCpuClockControl>(board, "_fastCpuClock");
        var via = Field<Via6522>(board, "_via");
        var originalRead = via.ReadPortA;
        try
        {
            byte[] program = [0xad, 1, 0x0e, 0x4c, 0, 2];
            for (var i = 0; i < program.Length; i++) board.Write((ushort)(0x200 + i), program[i]);
            var reads = 0;
            via.ReadPortA = () => { reads++; return 0x5a; };
            for (var i = 0; i < 20; i++) board.Tick();
            Assert.True(reads > 0);
            var before = reads;
            var cpu = Field<Cpu6502>(board, "_cpu6502");
            var cycles = cpu.Cycles;
            var pc = cpu.PC;
            control.SetBusRequest(false);
            for (var i = 0; i < 30; i++) board.Tick();
            Assert.False(control.BusGrantHigh);
            Assert.Equal(before, reads);
            Assert.Equal(pc, cpu.PC);
            Assert.Equal(cycles + 30, cpu.Cycles);
            control.SetBusRequest(true);
            for (var i = 0; i < 20; i++) board.Tick();
            Assert.True(control.BusGrantHigh);
            Assert.True(reads > before);
        }
        finally
        {
            control.SetBusRequest(true); via.ReadPortA = originalRead; board.Reset(true);
        }
    }

    [Fact]
    public void ResetDoesNotRestartTheFreeRunningClockNetwork()
    {
        var board = Create();
        try
        {
            var source = Field<SystemClockGenerator>(board, "_fastClock");
            var cpu = Field<Cpu6502>(board, "_cpu6502");
            for (var i = 0; i < 11; i++) board.Tick();
            var start = source.InputClockCount;
            var cycles = cpu.Cycles;
            board.Reset(false);
            Assert.Equal(start + 9 * 12, source.InputClockCount);
            Assert.Equal(cycles + 9, cpu.Cycles);
            Assert.Equal(0x200, cpu.PC);
        }
        finally { board.Reset(true); }
    }

    private static AcornSystemMachine Create(bool disk = false)
    {
        var rom = new byte[8192];
        rom[^3] = 2; rom[^1] = 2;
        var board = new AcornSystemMachine(SystemCpuCard.Fast6502A, rom, new byte[2048], [], disk);
        board.Write(0x200, 0x4c); board.Write(0x201, 0); board.Write(0x202, 2);
        return board;
    }

    private static T Field<T>(object owner, string name) =>
        (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;
}
