using System.Reflection;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services;
using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornAtom.Common.Services;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuAtomClockTests
{
    [Fact]
    public void CpuWritesLoadTheViaBeforeTheFallingEdgeWithoutASecondTimerTick()
    {
        var board = Create();
        var cpu = Component<Cpu6502>(board, "_cpu");
        try
        {
            byte[] program = [0xa9, 0x88, 0x8d, 0x04, 0xb8, 0xa9, 0x13, 0x8d, 0x05, 0xb8, 0xea];
            for (var i = 0; i < program.Length; i++) board.Write((ushort)(0x200 + i), program[i]);
            Assert.Equal(1_000_000, cpu.ClockFrequency);
            Assert.Equal(1_000_000, cpu.InputClockFrequency);
            Assert.False(cpu.PhaseTwoHigh);
            var start = cpu.Cycles;
            for (var instruction = 0; instruction < 4; instruction++) cpu.Step();
            Assert.Equal(12, cpu.Cycles - start);
            Assert.Equal(5000, Counter(board));
            cpu.Clock();
            Assert.Equal(4999, Counter(board));
            Assert.False(cpu.PhaseTwoHigh);
        }
        finally { board.Reset(true); }
    }

    [Fact]
    public void TimerUnderflowAtTheFallingEdgePublishesIrqAtTheNextRisingEdge()
    {
        var board = Create();
        var cpu = Component<Cpu6502>(board, "_cpu");
        var via = Component<Via6522>(board, "_via");
        var phaseReceiver = cpu.PhaseOutputsChanged;
        var irqReceiver = via.IrqChanged;
        var phases = new List<(bool High, bool Irq)>();
        var irqPhases = new List<bool>();
        Assert.NotNull(phaseReceiver);
        cpu.PhaseOutputsChanged = (one, two) =>
        {
            phaseReceiver!(one, two);
            phases.Add((two, board.Irq));
        };
        via.IrqChanged = level =>
        {
            irqReceiver!(level);
            if (!level) return;
            irqPhases.Add(cpu.PhaseTwoHigh);
            // Lecture du compteur pendant le front de timeout : acquittement inhibé.
            Assert.Equal(0xff, board.Read(0xb804));
            Assert.True(board.Irq);
        };
        try
        {
            board.Write(0x200, 0xea); board.Write(0x201, 0xea);
            board.Write(0xb80e, 0xc0);
            board.Write(0xb804, 0); board.Write(0xb805, 0);
            cpu.Clock(); // Le descendant termine le chargement.
            Assert.False(board.Irq);
            cpu.Clock(); // Le descendant produit FFFF ; IRQ attend le montant.
            Assert.Equal(65535, Counter(board));
            Assert.False(board.Irq);
            cpu.Clock();
            Assert.True(board.Irq);
            Assert.Equal(new[] { true }, irqPhases);
            Assert.Equal(new[] { (true, false), (false, false), (true, false), (false, false),
                (true, true), (false, true) }, phases);
            board.Read(0xb804); // Après le descendant suivant : acquittement permis.
            Assert.False(board.Irq);
        }
        finally
        {
            cpu.PhaseOutputsChanged = phaseReceiver;
            via.IrqChanged = irqReceiver;
            board.Reset(true);
        }
    }

    [Fact]
    public void OtherBoardDomainsDoNotClockTheViaAndResetKeepsThePhaseConnection()
    {
        var board = Create();
        var cpu = Component<Cpu6502>(board, "_cpu");
        try
        {
            for (var reset = 0; reset < 2; reset++)
            {
                board.Reset(reset == 0);
                board.Write(0x200, 0xea);
                board.Write(0xb804, 0x88); board.Write(0xb805, 0x13);
                for (var period = 0; period < 10; period++) board.Tick();
                Assert.Equal(5000, Counter(board));
                cpu.Clock();
                Assert.Equal(5000, Counter(board));
                cpu.Clock();
                Assert.Equal(4999, Counter(board));
            }
        }
        finally { board.Reset(true); }
    }

    private static int Counter(AtomMachine board) => board.Read(0xb804) | board.Read(0xb805) << 8;
    private static T Component<T>(AtomMachine board, string name) =>
        (T)typeof(AtomMachine).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(board)!;
    private static AtomMachine Create()
    {
        var rom = new byte[8192];
        rom[0x1ffc] = 0; rom[0x1ffd] = 2;
        return new AtomMachine(rom, new byte[280], 1024, 1024);
    }
}
