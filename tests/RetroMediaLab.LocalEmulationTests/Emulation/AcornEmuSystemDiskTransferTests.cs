using System.Reflection;
using RetroMediaLab.Chips.Controllers.Acorn.SystemClock.Services;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Services;
using RetroMediaLab.Tests.Emulation.Intel8271;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuSystemDiskTransferTests
{
    [Theory]
    [InlineData(1, true)]
    [InlineData(10, true)]
    [InlineData(10, false)]
    public void CpuReadsWritesAndRereadsThroughThePhysicalBoardWithIdenticalSourceModes(int sectors, bool defer)
    {
        using var disk = CreateDisk(sectors);
        using var otherDisk = CreateDisk(sectors);
        using var actual = Create(disk, defer);
        using var reference = Create(otherDisk, defer);
        reference.GroupActiveFloppyPeriods = false;
        var actualNmi = CaptureNmi(actual); var expectedNmi = CaptureNmi(reference);
        for (var field = 0; field < 80; field++)
        {
            Assert.Equal(reference.RunFrame(), actual.RunFrame());
            Assert.Equal(reference.DrainAudio(), actual.DrainAudio());
            Assert.Equal(reference.VideoFrameDurationSeconds, actual.VideoFrameDurationSeconds);
            Assert.Equal(Field<byte[]>(reference, "_ram"), Field<byte[]>(actual, "_ram"));
            var a = Field<Cpu6502>(actual, "_cpu6502"); var b = Field<Cpu6502>(reference, "_cpu6502");
            Assert.Equal((b.PC, b.A, b.X, b.Y, b.S, b.P, b.Cycles), (a.PC, a.A, a.X, a.Y, a.S, a.P, a.Cycles));
            Assert.Equal(reference.FloppyReadBytes(0), actual.FloppyReadBytes(0));
            Assert.Equal(reference.FloppyWrittenBytes(0), actual.FloppyWrittenBytes(0));
            Assert.Equal(expectedNmi, actualNmi); expectedNmi.Clear(); actualNmi.Clear();
        }
        Validate(actual, disk); Validate(reference, otherDisk);
        Assert.Equal(otherDisk.WindowWrites, disk.WindowWrites);
        Assert.True(actual.RepresentedPrivateClockPeriods > reference.RepresentedPrivateClockPeriods);
    }

    private static List<(long Time, long Cycles, long Ticks, bool Phase, bool? Level)> CaptureNmi(AcornSystemMachine machine)
    {
        var trace = new List<(long, long, long, bool, bool?)>();
        var board = Field<SystemFloppyBoard>(machine, "_floppyBoard");
        var cpu = Field<Cpu6502>(machine, "_cpu6502");
        var wire = board.NmiChanged;
        board.NmiChanged = high =>
        {
            trace.Add((board.TimeNanoseconds, cpu.Cycles, Field<long>(machine, "_ticks"), cpu.PhaseTwoHigh, high));
            wire?.Invoke(high);
        };
        return trace;
    }

    internal static void Validate(AcornSystemMachine machine, MemoryTrackDisk disk)
    {
        var ram = Field<byte[]>(machine, "_ram");
        Assert.True(ram[0x70] == 3, $"Stage {ram[0x70]}, PC={Field<Cpu6502>(machine, "_cpu6502").PC:X4}, results={ram[0x71]:X2}/{ram[0x72]:X2}/{ram[0x73]:X2}");
        Assert.Equal(new byte[3], ram[0x71..0x74]);
        Assert.All(ram[0x3000..0x3080], value => Assert.Equal(0x5a, value));
        Assert.All(ram[0x3100..0x3180], value => Assert.Equal(0xa6, value));
        Assert.True(disk.WindowWrites > 0);
        Assert.All(disk.Sectors, sector => Assert.All(sector.Data.ToArray(), value => Assert.Equal(0xa6, value)));
        Assert.Equal(256 * disk.Sectors.Count, machine.FloppyReadBytes(0));
        Assert.Equal(128 * disk.Sectors.Count, machine.FloppyWrittenBytes(0));
    }

    internal static MemoryTrackDisk CreateDisk(int sectors)
    {
        var disk = new MemoryTrackDisk(fill: 0x5a);
        for (var sector = 1; sector < sectors; sector++)
            disk.Sectors.Add(new(sector, Enumerable.Repeat((byte)0x5a, 128).ToArray(), 0, 0, sector + 1));
        return disk;
    }

    internal static AcornSystemMachine Create(MemoryTrackDisk disk, bool defer)
    {
        var rom = new byte[8192]; rom[^6] = 0; rom[^5] = 0xff; rom[^4] = 0; rom[^3] = 2; rom[^2] = 0; rom[^1] = 0xff;
        rom[^256] = 0x40; // RTI in mapped ROM: actual NMI entry/return while the program polls requests.
        var machine = new AcornSystemMachine(SystemCpuCard.Fast6502A, rom, new byte[2048], [(0x2000, 0xc000)], true);
        try
        {
            // Programmable fixture: 128 character clocks/line, 312 lines,
            // progressive 80 x 25 characters; 19.968 ms per field at 2 MHz.
            byte[] video = [127, 80, 98, 0x28, 30, 2, 25, 28, 0x40, 9, 0x20, 9, 0, 0, 0, 0];
            for (byte index = 0; index < video.Length; index++)
            { machine.Write(0x1840, index); machine.Write(0x1841, video[index]); }
            var program = Program(disk.Sectors.Count);
            Assert.True(program.Length <= 0x200); // Remaining on-card RAM ends at 03FF.
            for (var index = 0; index < program.Length; index++) machine.Write((ushort)(0x200 + index), program[index]);
            machine.AttachFloppy(0, disk);
            typeof(SystemClockGenerator).GetProperty("DeferRepresentedResolution", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(Field<SystemClockGenerator>(machine, "_fastClock"), defer);
            return machine;
        }
        catch { machine.Dispose(); throw; }
    }

    private static byte[] Program(int sectors)
    {
        var bytes = new List<byte>(); var labels = new Dictionary<string, int>();
        var branches = new List<(int Offset, string Label)>();
        void Emit(params byte[] data) => bytes.AddRange(data);
        void Label(string name) => labels.Add(name, bytes.Count);
        void Branch(byte opcode, string name) { Emit(opcode, 0); branches.Add((bytes.Count - 1, name)); }
        void Write(ushort address, byte value) => Emit(0xa9, value, 0x8d, (byte)address, (byte)(address >> 8));
        void Read(ushort address) => Emit(0xad, (byte)address, (byte)(address >> 8));
        void Command(byte value, params byte[] parameters)
        { Write(0x0a00, value); foreach (var parameter in parameters) Write(0x0a01, parameter); }
        void Transfer(byte command, ushort destination, byte result, string name, bool write)
        {
            if (sectors == 1) Command(command, 0, 1);
            else Command((byte)(command | 1), 0, 1, (byte)sectors);
            Emit(0xa0, (byte)sectors); Label(name + "Sector"); Emit(0xa2, 0);
            Label(name); Read(0x0a00); Emit(0x29, 4); Branch(0xf0, name);
            if (write) Write(0x0a04, 0xa6);
            else { Read(0x0a04); Emit(0x9d, (byte)destination, (byte)(destination >> 8)); }
            Emit(0xe8, 0xe0, 128); Branch(0xd0, name);
            Emit(0x88); Branch(0xd0, name + "Sector");
            Label(name + "Result"); Read(0x0a00); Emit(0x29, 0x10); Branch(0xf0, name + "Result");
            Read(0x0a01); Emit(0x85, result, 0xe6, 0x70);
        }
        Emit(0x78, 0xd8, 0xa2, 0xff, 0x9a);
        Command(0x3a, 0x17, 1); // Non-DMA request reflected in status.
        Command(0x3a, 0x23, 0x48); // Drive0 + head load.
        Label("Ready"); Command(0x3d, 0x22); Read(0x0a01); Emit(0x29, 4); Branch(0xf0, "Ready");
        Command(0x6c); Read(0x0a01); // Clear power-on not-ready latch.
        Command(0x35, 0x0d, 1, 0, 0x10);
        Transfer(0x52, 0x3000, 0x71, "Read", false);
        Transfer(0x4a, 0, 0x72, "Write", true);
        Transfer(0x52, 0x3100, 0x73, "Reread", false);
        var halt = (ushort)(0x200 + bytes.Count); Emit(0x4c, (byte)halt, (byte)(halt >> 8));
        foreach (var (offset, label) in branches) bytes[offset] = unchecked((byte)checked((sbyte)(labels[label] - offset - 1)));
        return bytes.ToArray();
    }

    internal static T Field<T>(object owner, string name) => (T)owner.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;
}
