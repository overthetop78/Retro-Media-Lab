using RetroMediaLab.Chips.CPU.Common.Interfaces;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services;

namespace RetroMediaLab.Chips.Tests.CPU.MOS.MCS6500.Chip6502;

/// <summary>Attentes arithmétiques issues du manuel constructeur Synertek 6500-50,
/// sections 2.2.1/2.2.2 et annexe B. Aucun corpus externe ni accès à l'état interne.</summary>
public sealed class ArithmeticSpecificationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EveryBinaryOperandPairAndInputCarryMatchSignedAndUnsignedArithmetic(bool subtract)
    {
        var harness = new Harness(subtract);
        for (var left = 0; left < 256; left++)
        for (var right = 0; right < 256; right++)
        for (var carry = 0; carry < 2; carry++)
        {
            var wide = subtract ? left - right - (1 - carry) : left + right + carry;
            var signed = subtract
                ? (sbyte)left - (sbyte)right - (1 - carry)
                : (sbyte)left + (sbyte)right + carry;
            var expected = unchecked((byte)wide);
            var expectedFlags = (subtract ? wide >= 0 : wide > 255) ? 1 : 0;
            if (expected == 0) expectedFlags |= 2;
            if (signed < -128 || signed > 127) expectedFlags |= 0x40;
            if (expected >= 128) expectedFlags |= 0x80;

            var result = harness.Evaluate((byte)left, (byte)right, carry, decimalMode: false);
            Assert.Equal(expected, result.Value);
            Assert.Equal(expectedFlags, result.Status & 0xc3);
            Assert.Equal(0x24, result.Status & 0x2c); // I et bit réservé conservés, D reste nul.
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EveryValidPackedDecimalPairAndInputCarryMatchBaseTenArithmetic(bool subtract)
    {
        var harness = new Harness(subtract);
        for (var left = 0; left < 100; left++)
        for (var right = 0; right < 100; right++)
        for (var carry = 0; carry < 2; carry++)
        {
            var wide = subtract ? left - right - (1 - carry) : left + right + carry;
            var residue = (wide % 100 + 100) % 100;
            var expectedCarry = subtract ? wide >= 0 : wide > 99;
            var result = harness.Evaluate(Packed(left), Packed(right), carry, decimalMode: true);
            Assert.Equal(Packed(residue), result.Value);
            Assert.Equal(expectedCarry, (result.Status & 1) != 0);
            Assert.Equal(0x2c, result.Status & 0x2c); // D/I inchangés par ADC/SBC.
            // N/V/Z décimaux et chiffres A–F : aucune attente déduite du résultat corrigé.
        }
    }

    private static byte Packed(int number) => (byte)(number / 10 * 16 + number % 10);

    private sealed class Harness
    {
        private readonly RamBus _bus = new();
        private readonly Cpu6502 _cpu;

        internal Harness(bool subtract)
        {
            // LDA #P, PHA, PLP, LDA #A, ADC/SBC #M, JMP 0200.
            // Chaque échantillon initialise le CPU par ses véritables instructions.
            byte[] program = [0xa9, 0, 0x48, 0x28, 0xa9, 0, subtract ? (byte)0xe9 : (byte)0x69, 0, 0x4c, 0, 2];
            program.CopyTo(_bus.Memory, 0x200);
            _bus.Memory[0xfffd] = 2;
            _cpu = new Cpu6502(_bus, 1_000_000);
            _cpu.SetIrq(false); _cpu.SetNmi(false); _cpu.SetReady(true);
            _cpu.SetReset(false); _cpu.SetOverflowInput(false);
            _cpu.Reset();
        }

        internal (byte Value, byte Status) Evaluate(byte left, byte right, int carry, bool decimalMode)
        {
            _bus.Memory[0x201] = (byte)(0x24 | carry | (decimalMode ? 8 : 0));
            _bus.Memory[0x205] = left;
            _bus.Memory[0x207] = right;
            for (var instruction = 0; instruction < 4; instruction++) _cpu.Step();
            var start = _cpu.Cycles;
            _cpu.Step();
            Assert.Equal(start + 2, _cpu.Cycles);
            Assert.Equal(0x208, _cpu.PC);
            var result = (_cpu.A, _cpu.P);
            _cpu.Step(); // Le saut ne modifie pas les indicateurs.
            Assert.Equal(0x200, _cpu.PC);
            Assert.Equal(0xfd, _cpu.S);
            return result;
        }
    }

    private sealed class RamBus : ICpuBus
    {
        internal readonly byte[] Memory = new byte[65536];
        public bool Irq => false;
        public bool Nmi => false;
        public byte Read(ushort address) => Memory[address];
        public void Write(ushort address, byte value) => Memory[address] = value;
        public void Tick() { }
    }
}
