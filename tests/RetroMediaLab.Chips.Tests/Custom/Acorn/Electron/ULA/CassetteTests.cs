using RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Interfaces;
using RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Services;

namespace RetroMediaLab.Chips.Tests.Custom.Acorn.Electron.ULA;

public sealed partial class CassetteTests
{
    private const int BitClocks = 1664;
    private sealed class Pins : IElectronCassettePins
    {
        public bool InputPresent { get; set; }
        public bool InputLevel { get; set; }
        public bool OutputLevel { get; private set; }
        public bool Motor { get; private set; }
        public void SetOutputLevel(bool value) => OutputLevel = value;
        public void Tick(bool motor) => Motor = motor;
    }

    private static ElectronUla Create(Pins pins) => new(new ElectronVideo(_ => 0), pins);

    private static void Bit(ElectronUla receiver, Pins pins, bool mark)
    {
        var halfPeriod = mark ? 416 : 832;
        for (var clock = 1; clock <= BitClocks; clock++)
        {
            pins.InputLevel = ((clock / halfPeriod) & 1) != 0;
            receiver.Tick();
        }
    }

    [Theory]
    [InlineData(0x00)]
    [InlineData(0xff)]
    [InlineData(0x55)]
    [InlineData(0x80)]
    [InlineData(0xa5)]
    public void TransmitterProducesStartLsbDataStopAndExactHalfWaves(int value)
    {
        var pins = new Pins();
        var ula = Create(pins);
        ula.Write(7, 4);
        ula.Read(4);
        ula.Write(4, (byte)value);
        var counter = 0;
        var bit = -1;
        for (var clock = 0; clock <= 10 * BitClocks; clock++)
        {
            for (var point = 0; point < 8; point++)
            {
                if (counter % 16384 == 0) bit++;
                counter = (counter - (counter % 4096 == 0 ? 769 : 1)) & 65535;
            }
            ula.Tick();
            var mark = bit >= 9 || bit > 0 && ((value >> (bit - 1)) & 1) != 0;
            Assert.Equal((counter & (mark ? 4096 : 8192)) != 0, pins.OutputLevel);
            Assert.Equal(bit >= 9, (ula.Read(0) & 0x20) != 0);
        }
    }

    [Fact]
    public void Fe04ReloadsTheActiveTransmitterInsteadOfQueuingBehindIt()
    {
        var pins = new Pins();
        var ula = Create(pins);
        ula.Write(7, 4);
        ula.Write(4, 0);
        for (var clock = 0; clock < 3 * BitClocks; clock++) ula.Tick();
        ula.Write(4, 0xff);
        Assert.Equal(0, ula.Read(0) & 0x20);
        for (var clock = 0; clock < BitClocks; clock++) ula.Tick();
        var transitions = 0;
        var previous = pins.OutputLevel;
        // The new first data bit is already a mark, while the interrupted old byte was all spaces.
        for (var clock = 0; clock < BitClocks; clock++)
        {
            ula.Tick();
            if (previous != pins.OutputLevel) transitions++;
            previous = pins.OutputLevel;
        }
        Assert.Equal(4, transitions);
        for (var clock = 0; clock <= 7 * BitClocks; clock++) ula.Tick();
        Assert.NotEqual(0, ula.Read(0) & 0x20);
    }

    [Fact]
    public void Fe04PreservesTheCarrierUntilTheSharedCounterStartsTheFrame()
    {
        var pins = new Pins();
        var ula = Create(pins);
        ula.Write(7, 4);
        for (var clock = 0; clock < 100; clock++) ula.Tick();
        Assert.True(pins.OutputLevel);
        ula.Write(4, 0xff);
        ula.Tick();
        Assert.True(pins.OutputLevel);
        for (var clock = 101; clock < 416; clock++) ula.Tick();
        Assert.True(pins.OutputLevel);
        ula.Tick();
        Assert.False(pins.OutputLevel);
        // FE04 does not reset the carrier or invent a separate 1664-clock bit timer.
        for (var clock = 417; clock < BitClocks; clock++) ula.Tick();
        Assert.False(pins.OutputLevel);
        ula.Tick();
        Assert.True(pins.OutputLevel);
    }

    [Fact]
    public void TransmitRegisterLoadedBeforeSelectingOutputIsNotErasedByTheModeLatch()
    {
        var actualPins = new Pins();
        var expectedPins = new Pins();
        var actual = Create(actualPins);
        var expected = Create(expectedPins);
        actual.Write(4, 0xa5);
        actual.Write(7, 4);
        expected.Write(7, 4);
        expected.Write(4, 0xa5);
        for (var clock = 0; clock < 10 * BitClocks; clock++)
        {
            actual.Tick();
            expected.Tick();
            Assert.Equal(expectedPins.OutputLevel, actualPins.OutputLevel);
            Assert.Equal(expected.Read(0), actual.Read(0));
        }
    }

    [Fact]
    public void ReceivedRegisterSurvivesEveryModeSelectionWithoutAClock()
    {
        var pins = new Pins { InputPresent = true };
        var ula = Create(pins);
        ula.Write(7, 0);
        for (var bit = 0; bit < 12; bit++) Bit(ula, pins, true);
        Bit(ula, pins, false);
        for (var bit = 0; bit < 8; bit++) Bit(ula, pins, ((0xa5 >> bit) & 1) != 0);
        Bit(ula, pins, true);
        Assert.Equal(0xa5, ula.Read(4));
        foreach (var control in new byte[] { 2, 4, 6, 0, 6, 4, 2, 0 })
        {
            ula.Write(7, control);
            Assert.Equal(0xa5, ula.Read(4));
        }
    }

    [Fact]
    public void TwoUlasTransferEveryByteUsingOnlyTheirElectricalPins()
    {
        var source = new Pins();
        var destination = new Pins { InputPresent = true };
        var sender = Create(source);
        var receiver = Create(destination);
        sender.Write(7, 4);
        receiver.Write(7, 0x40);
        void Pump(int clocks)
        {
            for (var clock = 0; clock < clocks; clock++)
            {
                sender.Tick();
                destination.InputLevel = source.OutputLevel;
                receiver.Tick();
            }
        }
        Pump(12 * BitClocks);
        Assert.NotEqual(0, receiver.Read(0) & 0x40);
        for (var value = 0; value < 256; value++)
        {
            receiver.Read(4);
            sender.Write(4, (byte)value);
            Pump(10 * BitClocks);
            Assert.NotEqual(0, receiver.Read(0) & 0x10);
            Assert.Equal((byte)value, receiver.Read(4));
            Assert.Equal(0, receiver.Read(0) & 0x10);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConstantInputNeverQualifiesAsCarrier(bool high)
    {
        var pins = new Pins { InputPresent = true, InputLevel = high };
        var receiver = Create(pins);
        receiver.Write(7, 0x40);
        for (var clock = 0; clock < 30 * BitClocks; clock++) receiver.Tick();
        Assert.Equal(0, receiver.Read(0) & 0x40);
    }

    [Fact]
    public void ElectricalInputDoesNotDependOnMotorAndCarrierIsLatchedUntilAcknowledged()
    {
        var pins = new Pins { InputPresent = true };
        var receiver = Create(pins);
        receiver.Write(7, 0);
        for (var bit = 0; bit < 12; bit++) Bit(receiver, pins, true);
        Assert.NotEqual(0, receiver.Read(0) & 0x40);
        Assert.False(pins.Motor);
        receiver.Write(7, 0x40);
        for (var bit = 0; bit < 12; bit++) Bit(receiver, pins, true);
        Assert.NotEqual(0, receiver.Read(0) & 0x40);
        receiver.Write(5, 0x40);
        for (var bit = 0; bit < 12; bit++) Bit(receiver, pins, true);
        Assert.Equal(0, receiver.Read(0) & 0x40);
        pins.InputPresent = false;
        receiver.Tick();
        pins.InputPresent = true;
        for (var bit = 0; bit < 12; bit++) Bit(receiver, pins, true);
        Assert.Equal(0, receiver.Read(0) & 0x40);
        Bit(receiver, pins, false);
        for (var bit = 0; bit < 24; bit++) Bit(receiver, pins, true);
        Assert.NotEqual(0, receiver.Read(0) & 0x40);
    }

    [Fact]
    public void LowStopStartsTheNextFrameWithoutAFramingErrorAndCarrierClearsReceiveFull()
    {
        var pins = new Pins { InputPresent = true };
        var receiver = Create(pins);
        receiver.Write(7, 0x40);
        for (var bit = 0; bit < 12; bit++) Bit(receiver, pins, true);
        for (var bit = 0; bit < 9; bit++) Bit(receiver, pins, false);
        Assert.NotEqual(0, receiver.Read(0) & 0x10);
        Assert.Equal(0, receiver.Read(4));
        Bit(receiver, pins, false);
        Assert.Equal(0, receiver.Read(0) & 0x10);
        Assert.Equal(0, receiver.Read(4));
        pins.InputPresent = false;
        receiver.Tick();
        pins.InputPresent = true;
        receiver.Write(5, 0x40);
        var carrierRemaining = 24;
        while ((receiver.Read(0) & 0x40) == 0 && --carrierRemaining > 0) Bit(receiver, pins, true);
        Assert.True(carrierRemaining > 0);
        Assert.Equal(0, receiver.Read(0) & 0x10);
        Bit(receiver, pins, false);
        for (var bit = 0; bit < 8; bit++) Bit(receiver, pins, true);
        Bit(receiver, pins, true);
        Assert.Equal(0xff, receiver.Read(4));
    }

    [Fact]
    public void ReceiveFullIsAvailableBeforeStopAndNewCarrierClearsUnreadData()
    {
        var pins = new Pins { InputPresent = true };
        var receiver = Create(pins);
        receiver.Write(7, 0);
        for (var bit = 0; bit < 12; bit++) Bit(receiver, pins, true);
        Bit(receiver, pins, false);
        // 0x55: its last data bit is low, so no incidental carrier can clear it.
        for (var bit = 0; bit < 7; bit++) Bit(receiver, pins, (0x55 & (1 << bit)) != 0);
        Assert.Equal(0, receiver.Read(0) & 0x10);
        Bit(receiver, pins, false);
        Assert.NotEqual(0, receiver.Read(0) & 0x10);
        Assert.Equal(0x55, receiver.Read(4));
        Assert.Equal(0, receiver.Read(0) & 0x10);
        Bit(receiver, pins, true);
        Bit(receiver, pins, false);
        for (var bit = 0; bit < 8; bit++) Bit(receiver, pins, false);
        Assert.NotEqual(0, receiver.Read(0) & 0x10);
        Bit(receiver, pins, true);
        for (var bit = 0; bit < 12; bit++) Bit(receiver, pins, true);
        Assert.Equal(0, receiver.Read(0) & 0x10);
        Assert.NotEqual(0, receiver.Read(0) & 0x40);
    }

    [Fact]
    public void CarrierFollowingDisconnectRearmsTheReceiverWithoutAHostReset()
    {
        var pins = new Pins { InputPresent = true };
        var receiver = Create(pins);
        receiver.Write(7, 0);
        for (var bit = 0; bit < 12; bit++) Bit(receiver, pins, true);
        Bit(receiver, pins, false);
        for (var bit = 0; bit < 7; bit++) Bit(receiver, pins, true);
        Assert.Equal(0, receiver.Read(0) & 0x10);
        pins.InputPresent = false;
        receiver.Tick();
        pins.InputPresent = true;
        for (var bit = 0; bit < 12; bit++) Bit(receiver, pins, true);
        Assert.Equal(0, receiver.Read(0) & 0x10);
    }

    [Theory]
    [InlineData(0x00, 0xff)]
    [InlineData(0xff, 0x00)]
    [InlineData(0x55, 0xaa)]
    [InlineData(0x80, 0x01)]
    [InlineData(0xa5, 0x3c)]
    public void DataRegisterExposesEachShiftAndReadOnlyAcknowledgesItsFullFlag(int previous, int value)
    {
        var pins = new Pins { InputPresent = true };
        var receiver = Create(pins);
        receiver.Write(7, 0);
        for (var bit = 0; bit < 12; bit++) Bit(receiver, pins, true);
        Bit(receiver, pins, false);
        for (var bit = 0; bit < 8; bit++) Bit(receiver, pins, (previous & (1 << bit)) != 0);
        Assert.Equal(previous, receiver.Read(4));
        Bit(receiver, pins, true);
        Bit(receiver, pins, false);
        Assert.Equal(previous >> 1, receiver.Read(4));
        for (var bit = 0; bit < 8; bit++)
        {
            Bit(receiver, pins, (value & (1 << bit)) != 0);
            var receivedBits = bit + 1;
            var expected = (previous >> (receivedBits + 1))
                | ((value & ((1 << receivedBits) - 1)) << (8 - receivedBits));
            Assert.Equal(bit == 7, (receiver.Read(0) & 0x10) != 0);
            Assert.Equal(expected, receiver.Read(4));
            Assert.Equal(0, receiver.Read(0) & 0x10);
            Assert.Equal(expected, receiver.Read(4));
        }
    }

    [Fact]
    public void SoundClocksAdvanceTheOutputCounterRatherThanLeavingAnIndependentCarrierPaused()
    {
        var actualPins = new Pins();
        var witnessPins = new Pins();
        var actual = Create(actualPins);
        var witness = Create(witnessPins);
        actual.Write(7, 4);
        witness.Write(7, 4);
        for (var clock = 0; clock < 100; clock++)
        {
            actual.Tick();
            witness.Tick();
        }
        actual.Write(7, 2);
        for (var clock = 0; clock < 400; clock++) actual.Tick();
        actual.Write(7, 4);
        actual.Tick();
        witness.Tick();
        Assert.False(actualPins.OutputLevel);
        Assert.True(witnessPins.OutputLevel);
    }

    [Fact]
    public void Fe04WaitsForTheExistingFrameClockWhenWrittenBetweenBoundaries()
    {
        var pins = new Pins();
        var ula = Create(pins);
        ula.Write(7, 4);
        for (var clock = 0; clock < 100; clock++) ula.Tick();
        ula.Write(4, 0xa5);
        // The next start is at 1665, and TDEmpty follows nine bit clocks later.
        for (var clock = 100; clock < 10 * BitClocks; clock++)
        {
            ula.Tick();
            Assert.Equal(0, ula.Read(0) & 0x20);
        }
        ula.Tick();
        Assert.NotEqual(0, ula.Read(0) & 0x20);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(127)]
    [InlineData(128)]
    [InlineData(255)]
    public void OutputFrequencyDoesNotUseTheSoundDivider(int divider)
    {
        var pins = new Pins();
        var sender = Create(pins);
        sender.Write(6, (byte)divider);
        sender.Write(7, 4);
        for (var clock = 1; clock <= BitClocks * 2; clock++)
        {
            sender.Tick();
            Assert.Equal((((clock - 1) / 416) & 1) == 0, pins.OutputLevel);
        }
    }
}
