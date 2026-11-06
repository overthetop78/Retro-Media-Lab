using RetroMediaLab.Chips.CPU.Common.Interfaces;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip65C12.Services;

namespace RetroMediaLab.Chips.Tests.CPU.MOS.MCS6500.Chip65C12;

public sealed class ArithmeticTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void AllBinaryAndValidPackedDecimalPairsHaveTheExpectedResultCarryNzAndDuration(bool decimalMode, bool subtract)
    {
        var bus = new Bus();
        bus.Memory[0xfffd] = 2;
        // SED/CLD ; CLC/SEC ; LDA #left ; ADC/SBC #right ; JMP back to CLC/SEC.
        byte[] program = [decimalMode ? (byte)0xf8 : (byte)0xd8, 0x18, 0xa9, 0, subtract ? (byte)0xe9 : (byte)0x69, 0, 0x4c, 1, 2];
        program.CopyTo(bus.Memory, 0x200);
        var cpu = new Cpu65C12(bus, 2_000_000);
        cpu.Reset();
        cpu.Step();
        var modulus = decimalMode ? 100 : 256;
        for (var left = 0; left < modulus; left++)
        for (var right = 0; right < modulus; right++)
        for (var carry = 0; carry < 2; carry++)
        {
            bus.Memory[0x201] = carry == 0 ? (byte)0x18 : (byte)0x38;
            var a = decimalMode ? Packed(left) : (byte)left;
            var b = decimalMode ? Packed(right) : (byte)right;
            bus.Memory[0x203] = a;
            bus.Memory[0x205] = b;
            cpu.Step(); cpu.Step();
            var before = cpu.Cycles;
            cpu.Step();
            var sum = subtract ? left - right - (1 - carry) : left + right + carry;
            var wrapped = (sum + modulus) % modulus;
            var expected = decimalMode ? Packed(wrapped) : (byte)wrapped;
            Assert.Equal(expected, cpu.A);
            var expectedCarry = subtract ? sum >= 0 : sum >= modulus;
            Assert.Equal(expectedCarry ? 1 : 0, cpu.P & 1);
            Assert.Equal(expected == 0 ? 2 : 0, cpu.P & 2);
            Assert.Equal(expected & 0x80, cpu.P & 0x80);
            Assert.Equal(decimalMode ? 3 : 2, cpu.Cycles - before);
            if (!decimalMode)
            {
                // Overflow follows the representable signed result, independently of carry.
                var signed = subtract ? (sbyte)a - (sbyte)b - (1 - carry) : (sbyte)a + (sbyte)b + carry;
                Assert.Equal(signed is < sbyte.MinValue or > sbyte.MaxValue ? 0x40 : 0, cpu.P & 0x40);
            }
            cpu.Step();
        }
    }

    private static byte Packed(int number) => (byte)((number / 10 << 4) | number % 10);

    private sealed class Bus : ICpuBus
    {
        internal byte[] Memory { get; } = new byte[ushort.MaxValue + 1];
        public bool Irq => false;
        public bool Nmi => false;
        public void Tick() { }
        public byte Read(ushort address) => Memory[address];
        public void Write(ushort address, byte value) => Memory[address] = value;
    }
}
