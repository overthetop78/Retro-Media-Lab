using RetroMediaLab.Chips.CPU.Common.Interfaces;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services;

namespace RetroMediaLab.Chips.Tests.CPU.MOS.MCS6500.Chip6502;

/// <summary>
/// Observation rapportée sur Apple IIe original : AppleWin #718, tomcw, 2019-11-09.
/// Avec IRQ déjà active, CLI laisse INY porter Y de zéro à un avant le handler.
/// La trace d'entrée IRQ vient du tableau constructeur Synertek 6500-50, chapitre 9.
/// Aucune attente SEI/PLP ni précision demi-cycle déduite de ce signalement.
/// </summary>
public sealed class ReportedCliIrqTests
{
    [Theory]
    [InlineData(0)] // Instructions Step, parcours utilisé par Atom.
    [InlineData(1)] // Cycles complets Clock.
    [InlineData(2)] // Fronts de l'entrée PH0.
    public void PendingIrqAllowsTheInyAfterCliBeforeEnteringTheHandler(int drive)
    {
        var bus = new RamBus();
        var cpu = new Cpu6502(bus, 1_000_000);
        cpu.SetReady(true);
        cpu.SetReset(false);
        cpu.SetOverflowInput(false);
        cpu.SetIrq(true);
        cpu.SetNmi(false);
        cpu.Reset();
        if (drive == 2) cpu.SetPhaseZeroInput(false);
        bus.Accesses.Clear();

        Instruction(); // SEI, IRQ active mais masquée.
        Instruction(); // LDY #0.
        Assert.Equal(0, cpu.Y);
        Assert.Equal(0x203, cpu.PC);
        Instruction(); // CLI : pas encore d'entrée IRQ.
        Assert.Equal(0x204, cpu.PC);
        Assert.Equal(0, bus.VectorReads);
        Instruction(); // INY doit réellement être exécuté.
        Assert.Equal(1, cpu.Y);
        Assert.Equal(0x205, cpu.PC);
        Assert.Equal(0, bus.VectorReads);
        bus.Accesses.Clear();
        var before = cpu.Cycles;

        Instruction(7); // Sept cycles d'entrée, avant le deuxième INY.
        Assert.Equal(7, cpu.Cycles - before);
        Assert.Equal(0x400, cpu.PC);
        Assert.Equal(1, cpu.Y);
        Assert.Equal(1, bus.VectorReads);
        Assert.Equal(new[] { 0x205, 0x205, 0x1fd, 0x1fc, 0x1fb, 0xfffe, 0xffff },
            bus.Accesses.Select(access => (int)access.Address));
        Assert.Equal(new[] { false, false, true, true, true, false, false },
            bus.Accesses.Select(access => access.Write));
        Assert.Equal(2, bus.Memory[0x1fd]);
        Assert.Equal(5, bus.Memory[0x1fc]);
        Assert.Equal(0, bus.Memory[0x1fb] & 0x14); // Ni BRK ni I dans le statut empilé.
        Assert.Equal(4, cpu.P & 4);

        void Instruction(int periods = 2)
        {
            if (drive == 0) { cpu.Step(); return; }
            // PH0 prépare immédiatement le prochain accès après chaque descendant.
            // Ne pas attendre AtInstructionBoundary : la préparation IRQ peut déjà
            // avoir quitté cette phase sans consommer sa première transaction.
            for (var cycle = 0; cycle < periods; cycle++)
            {
                if (drive == 1) cpu.Clock();
                else { cpu.SetPhaseZeroInput(true); cpu.SetPhaseZeroInput(false); }
            }
        }
    }

    private sealed class RamBus : ICpuBus
    {
        internal readonly byte[] Memory = new byte[65536];
        internal readonly List<(ushort Address, bool Write)> Accesses = [];
        internal int VectorReads;
        public bool Irq => throw new InvalidOperationException("IRQ raccordée par notification.");
        public bool Nmi => throw new InvalidOperationException("NMI raccordée par notification.");

        internal RamBus()
        {
            Memory[0xfffd] = 2;
            Memory[0xffff] = 4;
            new byte[] { 0x78, 0xa0, 0, 0x58, 0xc8, 0xc8 }.CopyTo(Memory, 0x200);
            Memory[0x400] = 0xea;
        }

        public byte Read(ushort address)
        {
            Accesses.Add((address, false));
            if (address == 0xfffe) VectorReads++;
            return Memory[address];
        }
        public void Write(ushort address, byte value)
        {
            Accesses.Add((address, true));
            Memory[address] = value;
        }
        public void Tick() { }
    }
}
