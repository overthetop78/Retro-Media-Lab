using RetroMediaLab.Chips.IO.Motorola.M6800.Chip6850.Contracts;
using RetroMediaLab.Chips.IO.Acorn.SerialUla.BbcSerialUla.Services;
using RetroMediaLab.Chips.IO.Motorola.M6800.Chip6850.Interfaces;
using RetroMediaLab.Chips.IO.Motorola.M6800.Chip6850.Services;
using BbcSerial = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services.Serial;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuSerialWiringTests
{
    private const int Clock = 2_000_000;

    [Fact]
    public void BusAccessesPreserveInterruptsAndFutureSerialClocks()
    {
        var actual = new Acia6850(Clock, 0);
        var serial = new BbcSerial(actual);
        var reference = new Acia6850(Clock, 0);
        var ula = new BbcSerialUla(Clock);
        actual.Write(false, 0x96);
        reference.Write(false, 0x96);
        serial.Ula.Write(0x64);
        ula.Write(0x64);
        long phase = 0;
        for (var tick = 0; tick < 250_000; tick++)
        {
            // Stable intervals followed by register and data accesses at unaligned clock phases.
            if (tick % 4093 == 0)
            {
                var control = (byte)(tick / 4093);
                actual.Write(false, control);
                reference.Write(false, control);
                actual.Write(true, (byte)tick);
                reference.Write(true, (byte)tick);
            }
            if (tick % 8191 == 0)
            {
                var control = (byte)(0x40 | tick / 8191);
                serial.Ula.Write(control);
                ula.Write(control);
                serial.Ula.Rs423ClearToSendHigh = ula.Rs423ClearToSendHigh = (tick / 8191 & 1) != 0;
                serial.Ula.Rs423ReceiveData = ula.Rs423ReceiveData = (tick / 8191 & 2) != 0;
            }
            serial.Tick(null);
            TickReference(reference, ula, null, 0, ref phase);
            Assert.Equal(reference.Irq, actual.Irq);
            Assert.Equal(reference.TransmitBit, actual.TransmitBit);
            Assert.Equal(reference.RtsHigh, actual.RtsHigh);
            if (tick % 1031 == 0)
            {
                Assert.Equal(reference.Read(false), actual.Read(false));
                Assert.Equal(reference.Read(true), actual.Read(true));
                Assert.Equal(ula.CassetteOutput, serial.Ula.CassetteOutput);
                Assert.Equal(ula.TransmitClockEdge, serial.Ula.TransmitClockEdge);
                Assert.Equal(ula.ReceiveClockEdge, serial.Ula.ReceiveClockEdge);
            }
        }
        // Check all internal phases after the final status access.
        Assert.Equal(reference.Read(false), actual.Read(false));
        foreach (var field in typeof(Acia6850).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic))
            if (!typeof(Delegate).IsAssignableFrom(field.FieldType))
                Assert.Equal(field.GetValue(reference), field.GetValue(actual));
    }

    [Fact]
    public void StablePinsAreSentOnceAndReassertedAfterPowerOn()
    {
        var acia = new ObservedAcia();
        var serial = new BbcSerial(acia);
        Assert.Equal(1, acia.CtsCalls);
        Assert.Equal(1, acia.DcdCalls);
        serial.PowerOn();
        for (var tick = 0; tick < 50_000; tick++) serial.Tick(null);
        Assert.Equal(2, acia.CtsCalls);
        Assert.Equal(2, acia.DcdCalls);
        serial.Ula.Write(0x40);
        serial.Tick(null);
        Assert.Equal(3, acia.CtsCalls);
        serial.Ula.Rs423ClearToSendHigh = false;
        serial.Tick(null);
        Assert.Equal(4, acia.CtsCalls);
        acia.Write(false, 3);
        serial.Tick(null);
        Assert.Equal(4, acia.CtsCalls);
        serial.PowerOn();
        serial.Tick(null);
        Assert.Equal(5, acia.CtsCalls);
        Assert.Equal(3, acia.DcdCalls);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(300)]
    [InlineData(1200)]
    public void FilteredPinsMatchUnfilteredWiringAcrossRegisterChangesAndResets(int baud)
    {
        var actual = new ObservedAcia();
        var serial = new BbcSerial(actual) { BaudRateOverride = baud };
        var reference = new Acia6850(Clock, 0) { BaudRateOverride = baud };
        var ula = new BbcSerialUla(Clock);
        long phase = 0;
        for (var tick = 0; tick < 200_000; tick++)
        {
            if (tick == 100_000)
            {
                serial.PowerOn();
                ula.PowerOn();
                reference.Reset();
                phase = 0;
            }
            if (tick % 127 == 0)
            {
                actual.Write(false, (byte)(tick / 127));
                reference.Write(false, (byte)(tick / 127));
            }
            if (tick % 199 == 0) { actual.Write(true, (byte)tick); reference.Write(true, (byte)tick); }
            if (tick % 1021 == 0) { serial.Ula.Write((byte)tick); ula.Write((byte)tick); }
            serial.Ula.Rs423ReceiveData = ula.Rs423ReceiveData = (tick / 11 & 1) != 0;
            serial.Ula.Rs423ClearToSendHigh = ula.Rs423ClearToSendHigh = (tick / 17 & 1) != 0;
            bool? input = tick % 113 == 0 ? null : (tick / 417 & 1) != 0;
            serial.Tick(input);
            TickReference(reference, ula, input, baud, ref phase);
            Assert.Equal(reference.Irq, actual.Irq);
            Assert.Equal(reference.TransmitBit, actual.TransmitBit);
            Assert.Equal(reference.RtsHigh, actual.RtsHigh);
            if (tick % 13 == 0) Assert.Equal(reference.Read(false), actual.Read(false));
            if (tick % 17 == 0) Assert.Equal(reference.Read(true), actual.Read(true));
        }
        Assert.True(actual.CtsCalls < 10_000);
        Assert.True(actual.DcdCalls < 100);
    }

    [Fact]
    public void CassetteCarrierTransitionsMatchUnfilteredWiring()
    {
        var actual = new ObservedAcia();
        var serial = new BbcSerial(actual);
        var reference = new Acia6850(Clock, 0);
        var ula = new BbcSerialUla(Clock);
        actual.Write(false, 0x95);
        reference.Write(false, 0x95);
        long phase = 0;
        for (var tick = 0; tick < Clock / 2; tick++)
        {
            bool? input = tick < Clock / 3 ? ((long)tick * 4800 / Clock & 1) != 0 : null;
            serial.Tick(input);
            TickReference(reference, ula, input, 0, ref phase);
            Assert.Equal(reference.Irq, actual.Irq);
            if (tick % 13 == 0) Assert.Equal(reference.Read(false), actual.Read(false));
            if (tick % 17 == 0) Assert.Equal(reference.Read(true), actual.Read(true));
        }
        Assert.True(actual.DcdCalls > 2);
        Assert.True(actual.DcdCalls < 10);
    }

    private static void TickReference(Acia6850 acia, BbcSerialUla ula, bool? input, int baud, ref long phase)
    {
        ula.SetCassetteInput(input);
        ula.Tick(acia.TransmitBit, acia.RtsHigh);
        acia.SetCts(ula.ClearToSendHigh);
        acia.SetDcd(ula.DataCarrierDetectHigh);
        var transmit = ula.TransmitClockEdge;
        var receive = ula.ReceiveClockEdge;
        if (!ula.Rs423Selected && baud > 0)
        {
            phase += (long)baud * acia.ClockDivider;
            transmit = receive = phase >= Clock;
            if (transmit) phase -= Clock;
        }
        if (transmit) acia.ClockTransmit();
        if (receive) acia.ClockReceive(ula.ReceiveData);
    }

    private sealed class ObservedAcia : IAcia6850
    {
        private readonly Acia6850 _chip = new(Clock, 0);
        public int CtsCalls { get; private set; }
        public int DcdCalls { get; private set; }
        public bool TransmitBit => _chip.TransmitBit;
        public bool RtsHigh => _chip.RtsHigh;
        public int BaudRateOverride { get => _chip.BaudRateOverride; set => _chip.BaudRateOverride = value; }
        public bool Receiving => _chip.Receiving;
        public int BaudRate => _chip.BaudRate;
        public int ClockDivider => _chip.ClockDivider;
        public int TransmitClockPhase => _chip.TransmitClockPhase;
        public int ReceiveClockPhase => _chip.ReceiveClockPhase;
        public Action<bool, bool>? TransmitPinsChanged { get => _chip.TransmitPinsChanged; set => _chip.TransmitPinsChanged = value; }
        public Action<int>? ClockDividerChanged { get => _chip.ClockDividerChanged; set => _chip.ClockDividerChanged = value; }
        public bool Irq => _chip.Irq;
        public Action<bool>? IrqChanged { get => _chip.IrqChanged; set => _chip.IrqChanged = value; }
        public Acia6850State CaptureState() => _chip.CaptureState();
        public void RestoreState(Acia6850State state) => _chip.RestoreState(state);
        public void Reset() => _chip.Reset();
        public byte Read(bool data) => _chip.Read(data);
        public void Write(bool data, byte value) => _chip.Write(data, value);
        public void SetDcd(bool high) { DcdCalls++; _chip.SetDcd(high); }
        public void SetCts(bool blocked) { CtsCalls++; _chip.SetCts(blocked); }
        public void BeginReceive() => _chip.BeginReceive();
        public void CancelReceive() => _chip.CancelReceive();
        public void ReceiveBit(bool value) => _chip.ReceiveBit(value);
        public void ClockReceive(bool value) => _chip.ClockReceive(value);
        public void ClockTransmit() => _chip.ClockTransmit();
        public void Tick() => _chip.Tick();
    }
}
