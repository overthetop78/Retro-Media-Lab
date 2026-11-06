using System.Text.Json;
using RetroMediaLab.Chips.CPU.Common.Interfaces;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Contracts;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Enums;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip65C12.Services;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip65C102.Services;

namespace RetroMediaLab.Tests.Chips.CPU.MOS.MCS6500.Chip6502;

public sealed class StateTests
{
    [Theory]
    [InlineData(Cpu6502Variant.Nmos6502)]
    [InlineData(Cpu6502Variant.Gte65Sc12)]
    [InlineData(Cpu6502Variant.Gte65Sc102)]
    public void SerializedStateContinuesEveryInputPhaseWithTheSameBusTransfersAndPendingInterrupt(Cpu6502Variant variant)
    {
        for (var offset = 0; offset < 48; offset++)
        {
            var bus = new Bus();
            var cpu = Create(bus, variant);
            cpu.Reset();
            cpu.AdvanceInputClocks(offset);
            cpu.SetIrq(true);
            cpu.SetNmi(true);
            if (variant != Cpu6502Variant.Gte65Sc12) cpu.SetReady(false);
            var snapshot = cpu.CaptureState();
            var serialized = JsonSerializer.SerializeToUtf8Bytes(snapshot);
            var restored = JsonSerializer.Deserialize<Cpu6502State>(serialized)!;
            var resumedBus = bus.Copy();
            var resumed = Create(resumedBus, variant);
            resumed.RestoreState(restored);
            Assert.Empty(resumedBus.Trace);
            Assert.Equal(snapshot, resumed.CaptureState());
            bus.Trace.Clear();
            for (var step = 0; step < 160; step++)
            {
                if (step == 5 && variant != Cpu6502Variant.Gte65Sc12)
                {
                    cpu.SetReady(true);
                    resumed.SetReady(true);
                }
                cpu.AdvanceInputClocks(1);
                resumed.AdvanceInputClocks(1);
                Assert.Equal(cpu.CaptureState(), resumed.CaptureState());
            }
            Assert.Equal(bus.Trace, resumedBus.Trace);
            Assert.Equal(bus.Memory, resumedBus.Memory);
        }
    }

    [Fact]
    public void IncompatibleStateIsRejectedBeforeChangingTheCpu()
    {
        var cpu = Create(new Bus(), Cpu6502Variant.Nmos6502);
        cpu.Reset();
        var original = cpu.CaptureState();
        Assert.Throws<ArgumentException>(() => cpu.RestoreState(original with { Variant = Cpu6502Variant.Gte65Sc12 }));
        Assert.Throws<ArgumentException>(() => cpu.RestoreState(original with { ClockFrequency = 1 }));
        Assert.Equal(original, cpu.CaptureState());
    }

    private static Cpu6502 Create(Bus bus, Cpu6502Variant variant)
    {
        if (variant == Cpu6502Variant.Nmos6502) return new Cpu6502(bus, 2_000_000);
        if (variant == Cpu6502Variant.Gte65Sc12) return new Cpu65C12(bus, 2_000_000);
        var advanced = new Cpu65C102(bus, 4_000_000);
        advanced.ClockOutputsChanged = (phase2, phase4) => bus.Trace.Add((bus.Ticks, phase2, phase4, 0, 0));
        return advanced;
    }

    private sealed class Bus : ICpuBus
    {
        internal byte[] Memory { get; } = new byte[65536];
        internal List<(long Clock, bool Read, bool Write, ushort Address, byte Value)> Trace { get; } = [];
        internal long Ticks;
        public bool Irq => false;
        public bool Nmi => false;

        internal Bus()
        {
            byte[] program = [0xa2, 0x03, 0xa9, 0x45, 0x8d, 0x00, 0x30, 0xee, 0x00, 0x30, 0xca, 0xd0, 0xf5, 0x4c, 0x00, 0x20];
            program.CopyTo(Memory, 0x2000);
            Memory[0xfffd] = 0x20;
            Memory[0xfffb] = Memory[0xffff] = 0x40;
            Memory[0x4000] = 0x40;
        }

        internal Bus Copy()
        {
            var copy = new Bus { Ticks = Ticks };
            Memory.CopyTo(copy.Memory, 0);
            return copy;
        }

        public byte Read(ushort address)
        {
            var value = Memory[address];
            Trace.Add((Ticks, true, false, address, value));
            return value;
        }

        public void Write(ushort address, byte value)
        {
            Memory[address] = value;
            Trace.Add((Ticks, false, true, address, value));
        }

        public void Tick() => Ticks++;
    }
}
