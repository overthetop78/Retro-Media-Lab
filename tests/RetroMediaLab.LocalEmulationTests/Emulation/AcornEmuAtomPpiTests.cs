using System.Reflection;
using RetroMediaLab.Chips.IO.Intel.MCS80.Chip8255.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornAtom.Common.Services;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornAtom.Common.Constants.AtomConstants;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuAtomPpiTests
{
    [Fact]
    public void TheNativeTapeClockReachesPc4WhenItIsProgrammedAsStrobeA()
    {
        var board = new AtomMachine(new byte[C.CombinedBasicOsBytes], new byte[280], C.RamBlockBytes, C.RamBlockBytes);
        try
        {
            var ppi = (Ppi8255)typeof(AtomMachine).GetField("_ppi", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(board)!;
            board.Write(C.PpiStart + 3, 0xbb);
            board.Write(C.PpiStart + 3, 0x09);
            Assert.False(ppi.InterruptA);
            for (var cycle = 0; cycle < C.TapeHalfPeriod * 3; cycle++) board.Tick();
            Assert.True(ppi.InterruptA);
            Assert.Equal(0x28, board.Read(C.PpiStart + 2) & 0x28);
            Assert.False(board.Irq); // No INTR-to-CPU wire exists on the ordinary Atom board.
            board.Read(C.PpiStart);
            Assert.False(ppi.InterruptA);
        }
        finally { board.Reset(true); }
    }

    [Fact]
    public void NativePc7ReadsTheThirtyTwoLineFieldSyncPulseFromTheVdg()
    {
        var board = new AtomMachine(new byte[C.CombinedBasicOsBytes], new byte[280], C.RamBlockBytes, C.RamBlockBytes);
        try
        {
            board.Write(C.PpiStart + 3, 0x8a);
            var previous = (board.Read(C.PpiStart + 2) & 0x80) != 0;
            Assert.True(previous);
            var edges = new List<(int Cycle, bool High)>();
            for (var cycle = 1; cycle <= 40000; cycle++)
            {
                board.Tick();
                var high = (board.Read(C.PpiStart + 2) & 0x80) != 0;
                if (high != previous) { edges.Add((cycle, high)); previous = high; }
            }
            Assert.True(edges.Count >= 4);
            Assert.False(edges[0].High);
            Assert.True(edges[1].High);
            Assert.InRange(edges[1].Cycle - edges[0].Cycle, 2038, 2040);
            Assert.InRange(edges[2].Cycle - edges[1].Cycle, 14649, 14651);
        }
        finally { board.Reset(true); }
    }

    [Fact]
    public void NativePaGraphicsAndPcCssReachTheVdgWithoutASecondPixelDecoder()
    {
        var board = new AtomMachine(new byte[C.CombinedBasicOsBytes], new byte[280], C.RamBlockBytes, 6144);
        try
        {
            board.Write(C.PpiStart + 3, 0x8a);
            for (var address = 0x8000; address < 0x9800; address++) board.Write((ushort)address, 0x1b);
            board.Write(C.PpiStart, 0x10);
            var video = (RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Services.AtomVideo)typeof(AtomMachine)
                .GetField("_video", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(board)!;
            for (var cycle = 0; cycle < 50000; cycle++) board.Tick();
            int[] lower = [0x30d200, 0xc1e500, 0x4c3ab4, 0x9a3236];
            AssertRow(video.CompletedFrame, lower);
            board.Write(C.PpiStart + 2, 8);
            for (var cycle = 0; cycle < 50000; cycle++) board.Tick();
            int[] upper = [0xbfc8ad, 0x41af71, 0xc84ef0, 0xd47f00];
            AssertRow(video.CompletedFrame, upper);
        }
        finally { board.Reset(true); }
    }

    private static void AssertRow(byte[] frame, int[] colours)
    {
        for (var x = 0; x < 256; x++)
        {
            var offset = x * 4;
            var colour = frame[offset] | (frame[offset + 1] << 8) | (frame[offset + 2] << 16);
            Assert.Equal(colours[x / 4 % 4], colour);
        }
    }
}
