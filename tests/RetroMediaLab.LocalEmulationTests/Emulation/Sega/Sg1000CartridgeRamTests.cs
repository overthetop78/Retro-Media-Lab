using System.Text.Json;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.SG1000.Contracts;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.SG1000.Enums;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.SG1000.Services;

namespace RetroMediaLab.Tests.Emulation.Sega;

public sealed class Sg1000CartridgeRamTests
{
    [Theory]
    [InlineData((int)Sg1000CartridgeLayout.TheCastle, 8192)]
    [InlineData((int)Sg1000CartridgeLayout.Othello, 2048)]
    public void AllCellsUseTheCpuBusWithMirrorsTimedStateAndVolatileReplacement(int layoutId, int capacity)
    {
        var layout = (Sg1000CartridgeLayout)layoutId;
        Sg1000Board? board = null, restored = null;
        try
        {
            var rom = Program(capacity);
            var configuration = new Sg1000RomConfiguration(CartridgeLayout: layout);
            board = new(rom, romConfiguration: configuration);
            // Find the initial real SRAM read while its guaranteed data/drive response is pending.
            Sg1000BoardState? pending = null;
            for (int step = 0; step < 1000; step++)
            {
                board.AdvanceTo(board.TimeNanoseconds + 50);
                var candidate = board.CaptureState();
                if (candidate.CartridgeRam is { } ram && (ram.DataDue is not null || ram.DriveDue is not null))
                { pending = candidate; break; }
            }
            Assert.NotNull(pending);
            restored = new(rom, false, configuration);
            restored.RestoreState(JsonSerializer.Deserialize<Sg1000BoardState>(JsonSerializer.SerializeToUtf8Bytes(pending))!);
            foreach (long delta in new long[] { 50, 200, 1000 })
            {
                long time = board.TimeNanoseconds + delta;
                board.AdvanceTo(time); restored.AdvanceTo(time);
                Assert.Equal(Snapshot(board), Snapshot(restored));
            }
            for (int step = 0; step < 350 && !board.Cpu.IsHalted; step++)
                board.AdvanceTo(board.TimeNanoseconds + 1_000_000);
            Assert.False(board.Cpu.IsFaulted);
            Assert.True(board.Cpu.IsHalted);
            var state = board.CaptureState();
            Assert.Equal(0x5a, state.Ram.Cells[0x10]);
            var memory = Assert.IsType<RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2016.Contracts.Tmm2016State>(state.CartridgeRam);
            Assert.True(memory.Qualified, JsonSerializer.Serialize(memory with { Cells = [], Known = [] }));
            Assert.Equal(capacity, memory.Cells.Length);
            for (int address = 0; address < capacity; address++)
            {
                Assert.Equal(255, memory.Known[address]);
                Assert.Equal((byte)((address & 255) ^ ((0x8000 + address) >> 8)), memory.Cells[address]);
            }
            Assert.Equal(0x7a, state.Ram.Cells[0x20]); // Internal RAM was not aliased to cartridge RAM.
            Assert.False(state.CartridgeLow!.Pins.CeHigh && layout == Sg1000CartridgeLayout.TheCastle);
            if (layout == Sg1000CartridgeLayout.TheCastle) Assert.False(state.CartridgeLow.Timing.ElectricallyCharacterized);
            byte[] before = Snapshot(board);
            foreach (var invalid in new[]
            {
                state with { CartridgeRam = null },
                state with { CartridgeRam = memory with { Cells = [] } },
                state with { CartridgeRam = memory with { Time = memory.Time + 1 } },
                state with { RomConfiguration = configuration with { CartridgeLayout = Sg1000CartridgeLayout.Standard } }
            })
            {
                Assert.Throws<ArgumentException>(() => board.RestoreState(invalid));
                Assert.Equal(before, Snapshot(board));
            }
            Assert.Throws<ArgumentException>(() => board.SetCartridge(new byte[8192]));
            Assert.Equal(before, Snapshot(board));
            board.SetCartridge([]);
            Assert.Null(board.CaptureState().CartridgeRam);
            board.SetCartridge(rom);
            var replacement = board.CaptureState().CartridgeRam!;
            Assert.All(replacement.Known, known => Assert.Equal(0, known));
            Assert.Equal(0, replacement.Time);
            Assert.Equal(0x7a, board.CaptureState().Ram.Cells[0x20]);
        }
        finally { try { board?.Dispose(); } finally { restored?.Dispose(); } }
    }

    private static byte[] Snapshot(Sg1000Board board) => JsonSerializer.SerializeToUtf8Bytes(board.CaptureState());

    private static byte[] Program(int capacity)
    {
        byte[] image = Enumerable.Repeat((byte)0xff, 32768).ToArray();
        var code = new List<byte> { 0xf3, 0x31, 0xff, 0xc3, 0x3a, 0x00, 0x80,
            0x3e, 0x7a, 0x32, 0x20, 0xc0, 0x21, 0x00, 0x80 };
        Count();
        int fill = code.Count;
        code.AddRange([0x7d, 0xac, 0x77, 0x23, 0x0b, 0x78, 0xb1]);
        Relative(0x20, fill);
        code.AddRange([0x21, 0x00, 0x80]); Count();
        int verify = code.Count;
        code.AddRange([0x7d, 0xac, 0x57, 0x7e, 0xba]);
        int mismatch = code.Count; code.AddRange([0x20, 0]);
        code.AddRange([0x23, 0x0b, 0x78, 0xb1]); Relative(0x20, verify);
        // Highest physical cell observed through the highest mirror.
        code.AddRange([0x3a, 0xff, 0xbf, 0xfe, (byte)(255 ^ ((0x8000 + capacity - 1) >> 8))]);
        int mirrorFailure = code.Count; code.AddRange([0x20, 0]);
        // Attempt a ROM write; only the selected SRAM may accept writes.
        code.AddRange([0x3e, 0, 0x32, 0, 0, 0x3a, 0, 0, 0xfe, 0xf3]);
        int romFailure = code.Count; code.AddRange([0x20, 0]);
        code.AddRange([0x3e, 0x5a, 0x32, 0x10, 0xc0, 0x76]);
        int fail = code.Count;
        code.AddRange([0x3e, 0xee, 0x32, 0x10, 0xc0, 0x76]);
        foreach (int branch in new[] { mismatch, mirrorFailure, romFailure }) code[branch + 1] = unchecked((byte)(fail - branch - 2));
        code.CopyTo(image);
        return image;
        void Count() => code.AddRange([0x01, (byte)capacity, (byte)(capacity >> 8)]);
        void Relative(byte instruction, int target)
        { int at = code.Count; code.AddRange([instruction, unchecked((byte)(target - at - 2))]); }
    }
}
