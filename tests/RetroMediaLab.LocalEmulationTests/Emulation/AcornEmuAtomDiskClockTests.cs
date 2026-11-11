using System.Reflection;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornAtom.Common.Services;
using RetroMediaLab.Tests.Emulation.Intel8271;
using Fdc = RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Services.Intel8271;
using H = RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Constants.Intel8271Constants;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuAtomDiskClockTests
{
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public void DiskDeadlinesPreserveWholeFramesAndReset(int scenario)
    {
        using var scalar = new Fixture(false, scenario == 2);
        using var grouped = new Fixture(true, scenario == 2);
        foreach (var item in new[] { scalar, grouped })
        {
            item.Board.Write(0xb80e, 0xc0);
            item.Board.Write(0xb80b, 0x40);
            item.Board.Write(0xb804, 0xff); item.Board.Write(0xb805, 0);
            if (scenario != 0) item.Command(H.WriteSpecial, H.DriveOutput, H.Drive0Select | H.OutputLoadHead);
        }
        for (var field = 0; field < 15; field++) Frame(scalar, grouped);
        scalar.Board.ConfigureFloppySpeed(0, 75); grouped.Board.ConfigureFloppySpeed(0, 75);
        scalar.Board.AttachFloppy(0, null); grouped.Board.AttachFloppy(0, null);
        Frame(scalar, grouped);
        foreach (var cold in new[] { false, true })
        {
            scalar.Board.Reset(cold); grouped.Board.Reset(cold);
            scalar.LoadProgram(); grouped.LoadProgram();
            Frame(scalar, grouped);
        }
        grouped.Board.GroupDiskClocks = false;
        Frame(scalar, grouped);
        grouped.Board.GroupDiskClocks = true;
        Frame(scalar, grouped);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void TransfersAndDataMirrorsPreserveNmiDates(bool write)
    {
        using var scalar = new Fixture(false, true);
        using var grouped = new Fixture(true, true);
        foreach (var item in new[] { scalar, grouped })
        {
            item.Command(0x35, 0x0d, 1, 0, 0x10);
            item.Command(0x7a, 0x17, 0xc1); // Non-DMA : NMI pour chaque octet.
            item.Command(write ? (byte)0x4a : (byte)0x52, 0, 1);
        }
        var bytes = 0;
        var complete = false;
        for (var block = 0; block < 100_000 && !complete; block++)
        {
            for (var clock = 0; clock < 8; clock++) { scalar.Cpu.Clock(); grouped.Cpu.Clock(); }
            var status = scalar.Board.Read(0x0a00);
            Assert.Equal(status, grouped.Board.Read(0x0a00));
            if ((status & 4) != 0)
            {
                ushort address = (bytes & 1) == 0 ? (ushort)0x0a04 : (ushort)0x0a7c;
                if (write)
                {
                    scalar.Board.Write(address, (byte)(bytes ^ 0xa5));
                    grouped.Board.Write(address, (byte)(bytes ^ 0xa5));
                }
                else
                {
                    Assert.Equal((byte)bytes, scalar.Board.Read(address));
                    Assert.Equal((byte)bytes, grouped.Board.Read(address));
                }
                bytes++;
            }
            if ((status & 0x90) == 0x10)
            {
                Assert.Equal(0, scalar.Board.Read(0x0a01));
                Assert.Equal(0, grouped.Board.Read(0x0a01));
                complete = true;
            }
            Compare(scalar, grouped);
        }
        Assert.True(complete);
        Assert.Equal(128, bytes);
        Assert.True(scalar.Edges.Count >= 256);
        if (write)
        {
            Assert.Equal(scalar.Disk.LastWriteStart, grouped.Disk.LastWriteStart);
            Assert.Equal(scalar.Disk.LastWriteDuration, grouped.Disk.LastWriteDuration);
            Assert.Equal(scalar.Disk.LastWritePulses, grouped.Disk.LastWritePulses);
            Assert.Equal(scalar.Disk.Sectors[0].Data, grouped.Disk.Sectors[0].Data);
        }
        Frame(scalar, grouped);
    }

    [Fact]
    public void MediaChangeClosesTheOldWriteWindowAtTheSourceDate()
    {
        using var scalar = new Fixture(false, true);
        using var grouped = new Fixture(true, true);
        foreach (var item in new[] { scalar, grouped })
            item.Command(H.WriteSpecial, H.DriveOutput, H.Drive0Select | H.OutputLoadHead | H.OutputWriteEnable);
        Frame(scalar, grouped);
        for (var clock = 0; clock < 117; clock++) { scalar.Cpu.Clock(); grouped.Cpu.Clock(); }
        scalar.Board.AttachFloppy(0, null); grouped.Board.AttachFloppy(0, null);
        Assert.True(scalar.Disk.WindowWrites > 0);
        Assert.Equal(scalar.Disk.LastWriteDuration, grouped.Disk.LastWriteDuration);
        Assert.Equal(scalar.Disk.AfterWrite!.FluxIntervalsNanoseconds, grouped.Disk.AfterWrite!.FluxIntervalsNanoseconds);
        Compare(scalar, grouped);
    }

    private static void Frame(Fixture scalar, Fixture grouped)
    {
        Assert.Equal(scalar.Board.RunFrame(), grouped.Board.RunFrame());
        Assert.Equal(scalar.Board.DrainAudio(), grouped.Board.DrainAudio());
        Compare(scalar, grouped);
    }

    private static void Compare(Fixture scalar, Fixture grouped)
    {
        Assert.Equal((scalar.Cpu.PC, scalar.Cpu.A, scalar.Cpu.X, scalar.Cpu.Y, scalar.Cpu.S, scalar.Cpu.P, scalar.Cpu.Cycles),
            (grouped.Cpu.PC, grouped.Cpu.A, grouped.Cpu.X, grouped.Cpu.Y, grouped.Cpu.S, grouped.Cpu.P, grouped.Cpu.Cycles));
        Assert.Equal(scalar.Ram, grouped.Ram);
        Assert.Equal(scalar.Edges, grouped.Edges);
        Assert.Equal((scalar.Board.Irq, scalar.Board.Nmi, scalar.Board.FloppyMotor(0), scalar.Board.FloppyReadBytes(0), scalar.Board.FloppyWrittenBytes(0)),
            (grouped.Board.Irq, grouped.Board.Nmi, grouped.Board.FloppyMotor(0), grouped.Board.FloppyReadBytes(0), grouped.Board.FloppyWrittenBytes(0)));
    }

    private static T Component<T>(object source, string field) =>
        (T)source.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(source)!;

    private sealed class Fixture : IDisposable
    {
        internal readonly AtomMachine Board;
        internal readonly Cpu6502 Cpu;
        internal readonly byte[] Ram;
        internal readonly MemoryTrackDisk Disk = new();
        internal readonly List<(long Clock, bool High)> Edges = [];
        private readonly Fdc _chip;
        private readonly Action<bool>? _nmi;

        internal Fixture(bool grouped, bool media)
        {
            var rom = new byte[8192];
            rom[0x1ffc] = 0; rom[0x1ffd] = 2;
            rom[0x1ffa] = 0; rom[0x1ffb] = 3;
            Board = new(rom, new byte[280], 5120, 6144, dos: new byte[4096]);
            Board.GroupDiskClocks = grouped;
            Cpu = Component<Cpu6502>(Board, "_cpu");
            Ram = Component<byte[]>(Board, "_ram");
            _chip = Component<Fdc>(Board, "_fdc");
            _nmi = _chip.NmiChanged;
            _chip.NmiChanged = level => { _nmi!(level); Edges.Add((Cpu.InputClocks, level)); };
            Board.AttachFloppy(0, media ? Disk : null);
            Command(0x6c); Board.Read(0x0a01);
            LoadProgram();
        }

        internal void LoadProgram()
        {
            byte[] program = [0x78, 0xee, 0x00, 0x80, 0xad, 0x04, 0xb8, 0x85, 0x10, 0x4c, 0x01, 0x02];
            for (var index = 0; index < program.Length; index++) Board.Write((ushort)(0x200 + index), program[index]);
            Board.Write(0x300, 0x40);
        }

        internal void Command(byte command, params int[] parameters)
        {
            Board.Write(0x0a00, command);
            foreach (var value in parameters) Board.Write(0x0a01, (byte)value);
        }

        public void Dispose()
        {
            _chip.NmiChanged = _nmi;
            try { Board.AttachFloppy(0, null); }
            finally { try { Board.Reset(true); } finally { Disk.Dispose(); } }
        }
    }
}
