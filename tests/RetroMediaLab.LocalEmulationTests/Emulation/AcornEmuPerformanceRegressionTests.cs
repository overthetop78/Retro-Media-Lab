using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.Chips.CPU.Common.Enums;
using RetroMediaLab.Chips.CPU.Common.Interfaces;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services;
using BbcKeyboard = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services.Keyboard;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuPerformanceRegressionTests
{
    [Fact]
    public void KeyboardColumnSummaryMatchesEveryPhysicalContactAfterEachInputChange()
    {
        var keyboard = new BbcKeyboard();
        foreach (var key in Enum.GetValues<EmulationKey>())
        {
            keyboard.SetInput(new HashSet<EmulationKey> { key });
            Check();
            keyboard.SetInput(new HashSet<EmulationKey>());
            Check();
        }
        void Check()
        {
            for (var column = 0; column < 16; column++)
                Assert.Equal(Enumerable.Range(1, 7).Any(row => (keyboard.Read((byte)(row * 16 + column)) & 128) != 0), keyboard.ColumnPressed(column));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FixedControlWiringAvoidsUnconnectedCallbacksWithoutChangingCycles(bool cmos)
    {
        var bus = new FixedBus();
        var cpu = new Cpu6502(bus, cmos);
        cpu.Reset();
        cpu.Step();
        Assert.Equal(cmos ? 8 : 9, cpu.Cycles);
        Assert.Equal(0x201, cpu.PC);
    }

    private sealed class FixedBus : ICpuBus
    {
        public CpuBusControlSignals ControlSignals => CpuBusControlSignals.None;
        public bool Irq => false;
        public bool Nmi => false;
        public bool Ready => throw new InvalidOperationException();
        public bool ResetRequested => throw new InvalidOperationException();
        public bool SetOverflow => throw new InvalidOperationException();
        public bool DataBusEnabled => throw new InvalidOperationException();
        public void SetSync(bool asserted) => throw new InvalidOperationException();
        public byte Read(ushort address) => address switch { 0xfffc => 0, 0xfffd => 2, _ => 0xea };
        public void Write(ushort address, byte value) { }
        public void Tick() { }
    }
}
