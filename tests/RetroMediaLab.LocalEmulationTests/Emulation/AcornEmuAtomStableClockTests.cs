using System.Reflection;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services;
using RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornAtom.Common.Services;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuAtomStableClockTests
{
    [Theory]
    [InlineData(-1)] [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    [InlineData(4)] [InlineData(5)] [InlineData(6)] [InlineData(7)]
    public void VideoGroupingPreservesTheWholeBoardAcrossVramAndPortTransactions(int mode)
    {
        var disk = (mode & 1) != 0;
        using var unit = new Fixture(false, disk);
        using var grouped = new Fixture(true, disk);
        foreach (var fixture in new[] { unit, grouped })
        {
            var board = fixture.Board;
            var pa = mode < 0 ? (byte)0 : (byte)(0x10 | mode << 5);
            List<byte> program = [0x78, 0xa9, 0x8a, 0x8d, 0x03, 0xb0,
                0xa9, pa, 0x8d, 0x00, 0xb0, 0xa0, 0x00];
            var loop = 0x200 + program.Count;
            program.AddRange([0xc8, 0x98, 0x99, 0x00, 0x80, 0x8d, 0x02, 0xb0,
                0xad, 0x02, 0xb0, 0x85, 0x10, 0xad, 0x04, 0xb8, 0x85, 0x11,
                0x4c, (byte)loop, (byte)(loop >> 8)]);
            for (var i = 0; i < program.Count; i++) board.Write((ushort)(0x200 + i), program[i]);
            board.Write(0x300, 0x40); // RTI pour le NMI synthétique du Disc Pack.
            board.Write(0xb80b, 0x40); board.Write(0xb80e, 0xc0);
            board.Write(0xb804, 0xff); board.Write(0xb805, 0);
        }
        for (var field = 0; field < 4; field++)
        {
            if (field == 1 && disk)
            {
                unit.Board.Write(0x0a00, 0x3f); grouped.Board.Write(0x0a00, 0x3f);
            }
            Assert.Equal(unit.Board.RunFrame(), grouped.Board.RunFrame());
            Assert.Equal(unit.Board.DrainAudio(), grouped.Board.DrainAudio());
            Assert.Equal(unit.Ram, grouped.Ram);
            Assert.Equal((unit.Cpu.PC, unit.Cpu.A, unit.Cpu.X, unit.Cpu.Y, unit.Cpu.S, unit.Cpu.P, unit.Cpu.Cycles),
                (grouped.Cpu.PC, grouped.Cpu.A, grouped.Cpu.X, grouped.Cpu.Y, grouped.Cpu.S, grouped.Cpu.P, grouped.Cpu.Cycles));
            Assert.Equal(unit.Edges, grouped.Edges);
            if (field == 2 && disk)
                Assert.Equal(unit.Board.Read(0x0a01), grouped.Board.Read(0x0a01));
        }
        unit.Board.Reset(false); grouped.Board.Reset(false);
        Assert.Equal(unit.Board.RunFrame(), grouped.Board.RunFrame());
        Assert.Equal(unit.Board.DrainAudio(), grouped.Board.DrainAudio());
        Assert.Equal(unit.Edges, grouped.Edges);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void VideoFetchDuringTheCpuPeriodStillPrecedesItsFinalVramWrite(bool grouped)
    {
        using var fixture = new Fixture(grouped, false);
        var board = fixture.Board;
        byte[] program = [0xa9, 0xaa, 0x8d, 0x00, 0x80, 0xea];
        for (var i = 0; i < program.Length; i++) board.Write((ushort)(0x200 + i), program[i]);
        board.Write(0x8000, 0x55);
        fixture.Cpu.Step(); // LDA.
        fixture.Cpu.Clock(); fixture.Cpu.Clock(); fixture.Cpu.Clock(); // STA avant son transfert.
        var chip = Component<Mc6847>(fixture.Video, "_chip");
        var guard = 0;
        while (!(chip.Scanline == 38 && chip.HalfClock >= 129 && chip.HalfClock < 135))
        {
            Assert.True(++guard < 20000);
            board.Tick();
        }
        var receiver = chip.DataRequested;
        var fetched = new List<byte>();
        chip.DataRequested = () => { receiver!(); fetched.Add(chip.Data); };
        try
        {
            fixture.Cpu.Clock(); // Tick VDG puis écriture finale de AA en RAM.
            Assert.Equal(new byte[] { 0x55 }, fetched);
            Assert.Equal(0xaa, board.Read(0x8000));
        }
        finally { chip.DataRequested = receiver; }
    }

    private static T Component<T>(object source, string name) =>
        (T)source.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(source)!;

    private sealed class Fixture : IDisposable
    {
        internal readonly AtomMachine Board;
        internal readonly Cpu6502 Cpu;
        internal readonly AtomVideo Video;
        internal readonly byte[] Ram;
        internal readonly List<(long Clock, bool High)> Edges = [];
        private readonly Action<bool>? _receiver;
        internal Fixture(bool grouped, bool disk)
        {
            var rom = new byte[8192];
            rom[0x1ffc] = 0; rom[0x1ffd] = 2;
            rom[0x1ffa] = 0; rom[0x1ffb] = 3;
            Board = new AtomMachine(rom, new byte[280], 5120, 6144, dos: disk ? new byte[4096] : []);
            Board.GroupVideoClocks = grouped;
            Cpu = Component<Cpu6502>(Board, "_cpu");
            Video = Component<AtomVideo>(Board, "_video");
            Ram = Component<byte[]>(Board, "_ram");
            _receiver = Video.FrameSyncChanged;
            Video.FrameSyncChanged = high => { _receiver!(high); Edges.Add((Cpu.InputClocks, high)); };
        }
        public void Dispose()
        {
            Video.FrameSyncChanged = _receiver;
            Board.EjectTape(); Board.Reset(true);
        }
    }
}
