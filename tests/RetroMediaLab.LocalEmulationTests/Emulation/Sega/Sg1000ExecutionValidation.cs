using System.Text.Json;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.SG1000.Contracts;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.SG1000.Services;

namespace RetroMediaLab.Tests.Emulation.Sega;

public sealed class Sg1000ExecutionValidation
{
    [Fact]
    public void NativeVideoConversionPreservesEveryPaletteIndexQualificationAndOutputByte()
    {
        var palette=(uint[])typeof(RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Services.EmulatorCore)
            .GetField("Palette",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static)!.GetValue(null)!;
        var indices=Enumerable.Range(0,256).Select(i=>(byte)i).ToArray();
        var known=Enumerable.Range(0,256).Select(i=>(i%3)!=0).ToArray();
        var expected=new byte[indices.Length*4+4];Array.Fill(expected,(byte)0xa5);
        var actual=(byte[])expected.Clone();
        for(int i=0;i<indices.Length;i++)System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(
            expected.AsSpan(i*4),known[i]?palette[indices[i]&15]:0U);
        RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Services.EmulatorCore.ConvertVideoPixels(indices,known,actual);
        Assert.Equal(expected,actual);
        RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Services.EmulatorCore.ConvertVideoPixels([],[],actual);
        Assert.Equal(expected,actual);
    }

    [Fact]
    public void ProfilingRepeatabilityMeasuresBetweenRunsRatherThanTreatingFramesAsIndependent()
    {
        var stable=Sg1000Profiling.SummarizeRepetitions([100,100,100,100,100],[99,100,101,102,98]);
        Assert.Equal(5,stable.Repetitions);Assert.Equal(0,stable.StandardDeviationMs);
        Assert.Equal(0,stable.CoefficientVariationPercent);Assert.Equal(0,stable.MedianAbsoluteDeviationMs);
        Assert.Equal(new double[]{99,100,101,102,98},stable.RunMediansMs);
        var spread=Sg1000Profiling.SummarizeRepetitions([98,99,100,101,102],[98,99,100,101,102]);
        Assert.Equal(100,spread.MeanMs);Assert.Equal(98,spread.MinimumRunMeanMs);Assert.Equal(102,spread.MaximumRunMeanMs);
        Assert.Equal(Math.Sqrt(2.5),spread.StandardDeviationMs,10);Assert.Equal(1,spread.MedianAbsoluteDeviationMs);
        Assert.Throws<ArgumentException>(()=>Sg1000Profiling.SummarizeRepetitions([],[]));
        Assert.Throws<ArgumentException>(()=>Sg1000Profiling.SummarizeRepetitions([double.NaN],[1]));
    }

    [Fact]
    public void ProfilingComparisonCannotDeclareCausalGainFromSingleRunsOrSameBinary()
    {
        var runs=Sg1000Profiling.SummarizeRepetitions([100,100,100,100,100],[100,100,100,100,100]);
        Assert.Equal("IncomparableTimingPolicy",Sg1000Profiling.AssessPerformance(false,"default-tiering","tiering-disabled",runs,runs));
        Assert.Equal("SameBinaryRepeatabilityControl",Sg1000Profiling.AssessPerformance(true,"x","x",runs,runs));
        Assert.Equal("IncompleteBinaryIdentity",Sg1000Profiling.AssessPerformance(true,"x","x",runs,runs,false));
        Assert.Equal("InsufficientRepetitions",Sg1000Profiling.AssessPerformance(false,"x","x",null,runs));
        Assert.Equal("UnpairedSeriesNoCausalConclusion",Sg1000Profiling.AssessPerformance(false,"x","x",runs,runs));
    }

    [Fact]
    public void CpuProgramUsesRealRamVramSoundControllersAndPauseWiring()
    {
        Sg1000Board? board = null;
        try
        {
            board = new(BuildCartridge());
            board.SetController(0, 33); // up + button2
            board.SetController(1, 22); // down + left + button1
            board.AdvanceTo(40_000_000);
            Assert.True(board.Cpu.IsHalted);
            Assert.False(board.Cpu.IsFaulted);
            var state = board.CaptureState();
            AssertVram(0, 0xaa);
            AssertVram(0x1800, 0);
            AssertVram(0x2000, 0x23);
            AssertVram(0x3f00, 0xd0);
            Assert.Equal(0xe9, state.Ram.Cells[0x10]); // C000/C400 mirror
            Assert.Equal(0x5e, state.Ram.Cells[0x11]); // controller low port
            Assert.Equal(0xfa, state.Ram.Cells[0x12]); // controller high port
            Assert.True(board.FramesCompleted >= 2);
            Assert.True(board.Vdp.IsCpuInterfaceQualified, JsonSerializer.Serialize(new
                { state.Vdp.CpuInterface.Qualified, state.Vdp.CpuWriteBus, state.Vdp.Registers }));
            var pixels = board.ColourIndices.Span;
            var known = board.PixelKnowledge.Span;
            Assert.True(known[3 * 256], JsonSerializer.Serialize(new
            {
                Pixels = pixels.Slice(3 * 256, 8).ToArray(), Known = known.Slice(3 * 256, 8).ToArray(),
                state.Vdp.Background, state.Vdp.Sprites, state.Vdp.VramBus,
                Dram = state.Dram.Select(memory => new { memory.TimingQualified, memory.Row, memory.Column }).ToArray()
            }));
            Assert.Equal(new byte[] { 2, 3, 2, 3, 2, 3, 2, 3 }, pixels.Slice(3 * 256, 8).ToArray());
            var audio = board.DrainAudio();
            Assert.InRange(audio.Length, 1919, 1921);
            Assert.Contains(audio, sample => sample > 100);
            Assert.Contains(audio, sample => sample < -100);
            board.SetPause(true);
            board.AdvanceTo(41_000_000);
            Assert.Equal(0x42, board.CaptureState().Ram.Cells[0x13]);
            board.SetPause(false);
            void AssertVram(int address, byte expected)
            {
                byte actual = 0;
                for (int bit = 0; bit < 8; bit++)
                {
                    byte cell = state.Dram[bit].Cells[address];
                    Assert.NotEqual(0, cell);
                    if (cell == 2) actual |= (byte)(1 << bit);
                }
                Assert.Equal(expected, actual);
            }
        }
        finally { board?.Dispose(); }
    }

    [Fact]
    public void SnapshotDuringExecutionContinuesEveryChipPixelAndAudioPhase()
    {
        Sg1000Board? original = null, restored = null;
        try
        {
            var rom = BuildCartridge();
            original = new(rom);
            restored = new(rom, false);
            original.SetController(0, 19);
            original.SetController(1, 44);
            original.AdvanceTo(391_237);
            var state = JsonSerializer.Deserialize<Sg1000BoardState>(Snapshot(original))!;
            restored.RestoreState(state);
            Assert.Equal(Snapshot(original), Snapshot(restored));
            foreach (long time in new long[] { 501_901, 1_000_071, 4_000_003 })
            {
                original.AdvanceTo(time); restored.AdvanceTo(time);
                Assert.Equal(Snapshot(original), Snapshot(restored));
            }
            original.SetController(1, 7); restored.SetController(1, 7);
            original.SetPause(true); restored.SetPause(true);
            original.AdvanceTo(5_000_007); restored.AdvanceTo(5_000_007);
            Assert.Equal(Snapshot(original), Snapshot(restored));
            Assert.Equal(original.DrainAudio(), restored.DrainAudio());
        }
        finally { try { original?.Dispose(); } finally { restored?.Dispose(); } }
    }

    [Fact]
    public void MalformedStateCannotPartiallyReplaceAWorkingBoard()
    {
        Sg1000Board? board = null;
        try
        {
            board = new(BuildCartridge());
            board.AdvanceTo(451_337);
            var state = board.CaptureState();
            byte[] before = Snapshot(board);
            foreach (var invalid in new[]
            {
                state with { Version = 1 }, state with { NextMasterIndex = 1 },
                state with { Pixels = [] }, state with { CartridgeHash = [] },
                state with { Psg = state.Psg with { TransferClocks = 33 } },
                state with { Dram = state.Dram.Select((memory, index) => index == 7 ? memory with { Cells = [] } : memory).ToArray() }
            })
            {
                Assert.Throws<ArgumentException>(() => board.RestoreState(invalid));
                Assert.Equal(before, Snapshot(board));
            }
        }
        finally { board?.Dispose(); }
    }

    private static byte[] Snapshot(Sg1000Board board) => JsonSerializer.SerializeToUtf8Bytes(board.CaptureState());
    private static byte[] BuildCartridge()
    {
        var rom = Enumerable.Repeat((byte)0xff, 8192).ToArray();
        new byte[] { 0xc3, 0x00, 0x01 }.CopyTo(rom, 0);
        new byte[] { 0x3e, 0x42, 0x32, 0x13, 0xc0, 0xed, 0x45 }.CopyTo(rom, 0x66);
        var code = new List<byte> { 0xf3, 0x31, 0xff, 0xc3 };
        Load(0x16); Store(0xc000); Load(0xe9); Store(0xc400);
        code.AddRange([0x3a, 0x00, 0xc0]); Store(0xc010);
        code.AddRange([0xdb, 0xdc]); Store(0xc011);
        code.AddRange([0xdb, 0xdd]); Store(0xc012);
        byte[] registers = [0, 0xc0, 6, 0x80, 0, 0x7e, 7, 0xe3];
        for (int register = 0; register < 8; register++)
        { Out(0xbf, registers[register]); Out(0xbf, (byte)(0x80 | register)); }
        Address(0x0000);
        for (int row = 0; row < 8; row++) Out(0xbe, 0xaa);
        Address(0x2000); Out(0xbe, 0x23);
        Address(0x1800); Out(0xbe, 0);
        Address(0x3f00); Out(0xbe, 0xd0);
        foreach (byte data in new byte[] { 0x84, 0, 0x90, 0xbf, 0xdf, 0xff }) Out(0x7f, data);
        ushort halt = (ushort)(0x100 + code.Count);
        code.AddRange([0x76, 0xc3, (byte)halt, (byte)(halt >> 8)]);
        code.CopyTo(rom, 0x100);
        return rom;
        void Load(byte value) => code.AddRange([0x3e, value]);
        void Store(ushort address) => code.AddRange([0x32, (byte)address, (byte)(address >> 8)]);
        void Out(byte port, byte value)
        {
            Load(value); code.AddRange([0xd3, port]);
            // The real VDP has one CPU transfer register, not a FIFO. Respect its worst access window.
            if (port == 0xbe) code.AddRange([0, 0, 0]);
        }
        void Address(ushort address) { Out(0xbf, (byte)address); Out(0xbf, (byte)(0x40 | (address >> 8))); }
    }
}
