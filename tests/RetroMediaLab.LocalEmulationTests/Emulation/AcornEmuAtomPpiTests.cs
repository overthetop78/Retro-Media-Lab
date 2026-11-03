using System.Reflection;
using RetroMediaLab.Chips.IO.Intel.MCS80.Chip8255.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornAtom.Common.Services;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornAtom.Common.Constants.AtomConstants;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuAtomPpiTests
{
    [Fact]
    public void TheNativeCassetteClockReachesPc4WhenItIsProgrammedAsStrobeA()
    {
        var board = new AtomMachine(new byte[C.CombinedBasicOsBytes], new byte[280], C.RamBlockBytes, C.RamBlockBytes);
        try
        {
            var ppi = (Ppi8255)typeof(AtomMachine).GetField("_ppi", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(board)!;
            board.Write(C.PpiStart + 3, 0xbb);
            board.Write(C.PpiStart + 3, 0x09);
            Assert.False(ppi.InterruptA);
            for (var cycle = 0; cycle < C.CassetteHalfPeriod * 3; cycle++) board.Tick();
            Assert.True(ppi.InterruptA);
            Assert.Equal(0x28, board.Read(C.PpiStart + 2) & 0x28);
            Assert.False(board.Irq); // No INTR-to-CPU wire exists on the ordinary Atom board.
            board.Read(C.PpiStart);
            Assert.False(ppi.InterruptA);
        }
        finally { board.Reset(true); }
    }
}
