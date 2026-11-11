using RetroMediaLab.Chips.CPU.Common.Interfaces;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services;

namespace RetroMediaLab.Chips.Tests.CPU.MOS.MCS6500.Chip6502;

/// <summary>Garantie NMI du manuel matériel MOS 6500-10A §1.4.1.2.9 :
/// front actif puis deux périodes basses, sans dépendance au masque IRQ.
/// Aucune attente n'est inventée pour les impulsions plus courtes.</summary>
public sealed class NotifiedNmiRequirementsTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    public void TwoPeriodNotifiedNmiIsRecognizedOnceAndRearmsAfterTheLineReturnsHigh(int initialHalfPeriods)
    {
        var bus = new RamBus();
        var cpu = new Cpu6502(bus, 1_000_000);
        cpu.SetReady(true); cpu.SetReset(false); cpu.SetOverflowInput(false);
        cpu.SetIrq(true); // IRQ active, mais I positionné au RESET.
        cpu.SetNmi(false);
        cpu.Reset();
        cpu.SetPhaseZeroInput(false);
        var clockHigh = false;
        Advance(initialHalfPeriods);
        Assert.Equal(4, cpu.P & 4);

        // asserted=true représente la broche NMI physiquement basse.
        cpu.SetNmi(true);
        Advance(4); // Quatre demi-périodes : deux périodes complètes depuis chaque phase.
        cpu.SetNmi(false);
        Advance(200);
        Assert.Equal(1, bus.Memory[0x10]);
        Assert.Equal(1, bus.NmiVectors);
        Assert.Equal(0, bus.Memory[0x11]);
        Assert.Equal(0, bus.IrqVectors);

        Advance(200); // Aucun autre front : aucune répétition de cette demande.
        Assert.Equal(1, bus.Memory[0x10]);
        Assert.Equal(1, bus.NmiVectors);

        cpu.SetNmi(true);
        Advance(4);
        cpu.SetNmi(false);
        Advance(200);
        Assert.Equal(2, bus.Memory[0x10]);
        Assert.Equal(2, bus.NmiVectors);
        Assert.Equal(0, bus.IrqVectors);
        Assert.Equal(4, cpu.P & 4);

        void Advance(int halfPeriods)
        {
            for (var half = 0; half < halfPeriods; half++)
            {
                clockHigh = !clockHigh;
                cpu.SetPhaseZeroInput(clockHigh);
            }
        }
    }

    private sealed class RamBus : ICpuBus
    {
        internal readonly byte[] Memory = new byte[65536];
        internal int NmiVectors;
        internal int IrqVectors;
        public bool Irq => throw new InvalidOperationException("IRQ doit être reçue par notification.");
        public bool Nmi => throw new InvalidOperationException("NMI doit être reçue par notification.");

        internal RamBus()
        {
            Memory[0xfffd] = 2;
            Memory[0xfffb] = 3;
            Memory[0xffff] = 4;
            new byte[] { 0xea, 0x4c, 0, 2 }.CopyTo(Memory, 0x200); // NOP/JMP : cinq périodes.
            new byte[] { 0xe6, 0x10, 0x40 }.CopyTo(Memory, 0x300); // INC 10, RTI.
            new byte[] { 0xe6, 0x11, 0x40 }.CopyTo(Memory, 0x400); // IRQ indue détectable.
        }

        public byte Read(ushort address)
        {
            if (address == 0xfffa) NmiVectors++;
            if (address == 0xfffe) IrqVectors++;
            return Memory[address];
        }
        public void Write(ushort address, byte value) => Memory[address] = value;
        public void Tick() { }
    }
}
