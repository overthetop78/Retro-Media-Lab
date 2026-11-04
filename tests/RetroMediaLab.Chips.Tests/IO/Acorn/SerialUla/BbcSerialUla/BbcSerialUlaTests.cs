using RetroMediaLab.Chips.IO.Acorn.SerialUla.BbcSerialUla.Constants;
using RetroMediaLab.Chips.IO.Motorola.M6800.Chip6850.Services;
using Ula = RetroMediaLab.Chips.IO.Acorn.SerialUla.BbcSerialUla.Services.BbcSerialUla;

namespace RetroMediaLab.Chips.Tests.IO.Acorn.SerialUla.BbcSerialUla;

public sealed class BbcSerialUlaTests
{
    private const int Clock = 2_000_000;
    private const int Window = 104_001;
    // Independent table transcribed from AUG 20.9, not from the production constants.
    public static IEnumerable<object[]> Rates()
    {
        int[] dividers = [1, 16, 4, 128, 2, 64, 8, 256];
        for (var tx = 0; tx < dividers.Length; tx++)
            for (var rx = 0; rx < dividers.Length; rx++)
                yield return [tx, rx, dividers[tx], dividers[rx]];
    }

    public static IEnumerable<object[]> SerialFrames()
    {
        for (var selection = 0; selection < 8; selection++)
            foreach (var control in new byte[] { 0x94, 0x95, 0x96 })
                yield return [selection, control];
    }

    [Theory]
    [MemberData(nameof(Rates))]
    public void IndependentClockSelectionsFollowTheDocumentedFractionalSource(int tx, int rx, int txDivider, int rxDivider)
    {
        var ula = new Ula(Clock);
        ula.Write((byte)(BbcSerialUlaConstants.Rs423Mask | tx | (rx << 3)));
        var transmit = 0;
        var receive = 0;
        for (var tick = 0; tick < Window; tick++)
        {
            ula.Tick(true, true);
            if (ula.TransmitClockEdge) transmit++;
            if (ula.ReceiveClockEdge) receive++;
        }
        var sourceEdges = (long)Window * 16_000_000 / (Clock * 13L);
        Assert.Equal(sourceEdges / txDivider, transmit);
        Assert.Equal(sourceEdges / rxDivider, receive);
    }

    [Fact]
    public void RegisterWritesDoNotRestartTheClockAndReadAppliesFe()
    {
        var changed = new Ula(Clock);
        var reference = new Ula(Clock);
        reference.Write(0xff);
        for (var tick = 0; tick < 2000; tick++)
        {
            if (tick == 437) changed.Write(0xff);
            changed.Tick(true, true);
            reference.Tick(true, true);
            if (tick >= 437)
            {
                Assert.Equal(reference.TransmitClockEdge, changed.TransmitClockEdge);
                Assert.Equal(reference.ReceiveClockEdge, changed.ReceiveClockEdge);
            }
        }
        Assert.Equal(0, changed.Read());
        Assert.Equal(0xfe, changed.Control);
        Assert.True(changed.Motor);
        Assert.True(changed.Rs423Selected);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void MuxAndMotorDoNotRequireAMediaOrRewriteTheInputPins(bool data, bool cts)
    {
        var ula = new Ula(Clock) { Rs423ReceiveData = data, Rs423ClearToSendHigh = cts };
        ula.Write(0x80);
        ula.Tick(false, false);
        Assert.True(ula.Motor);
        Assert.True(ula.ReceiveData);
        Assert.False(ula.ClearToSendHigh);
        Assert.True(ula.Rs423TransmitData);
        Assert.True(ula.Rs423RequestToSendHigh);
        ula.Write(0xc0);
        ula.Tick(false, false);
        Assert.Equal(data, ula.ReceiveData);
        Assert.Equal(cts, ula.ClearToSendHigh);
        Assert.False(ula.DataCarrierDetectHigh);
        Assert.False(ula.Rs423TransmitData);
        Assert.False(ula.Rs423RequestToSendHigh);
        Assert.Equal(0, ula.CassetteOutput);
    }

    [Theory]
    [InlineData(false, 1024)]
    [InlineData(true, 512)]
    public void SynthesisHasThePhysicalToneFrequencyAndRtsGatesItsOutput(bool data, int divider)
    {
        var ula = new Ula(Clock);
        var crossings = 0;
        short previous = 0;
        for (var tick = 0; tick < Window; tick++)
        {
            ula.Tick(data, false);
            if (previous <= 0 && ula.CassetteOutput > 0) crossings++;
            previous = ula.CassetteOutput;
        }
        Assert.InRange(crossings, (int)((long)Window * 16_000_000 / (Clock * 13L * divider)), (int)((long)Window * 16_000_000 / (Clock * 13L * divider)) + 1);
        ula.Tick(data, true);
        Assert.Equal(0, ula.CassetteOutput);
    }

    [Fact]
    public void CassetteReceiveClockIsIndependentOfTheRs423ReceiveSelection()
    {
        var ula = new Ula(Clock);
        var edges = 0;
        for (var tick = 0; tick < Window; tick++)
        {
            ula.Write((byte)((tick & 7) << 3));
            ula.Tick(true, false);
            if (ula.ReceiveClockEdge) edges++;
        }
        Assert.Equal((long)Window * 16_000_000 / (Clock * 13L * 64), edges);
    }

    [Theory]
    [MemberData(nameof(SerialFrames))]
    public void RealAciaLoopbackUsesPinsAndProgrammedDividers(int selector, byte control)
    {
        var ula = new Ula(Clock) { Rs423ClearToSendHigh = false };
        var acia = new Acia6850(Clock, 0);
        ula.Write((byte)(0x40 | selector | (selector << 3)));
        acia.Write(false, control);
        acia.Write(true, 0xa5);
        for (var tick = 0; tick < Clock / 5; tick++)
        {
            ula.Rs423ReceiveData = acia.TransmitBit;
            ula.Tick(acia.TransmitBit, acia.RtsHigh);
            acia.SetCts(ula.ClearToSendHigh);
            acia.SetDcd(ula.DataCarrierDetectHigh);
            if (ula.ReceiveClockEdge) acia.ClockReceive(ula.ReceiveData);
            if (ula.TransmitClockEdge) acia.ClockTransmit();
            if ((acia.Read(false) & 1) != 0)
            {
                Assert.Equal(0, acia.Read(false) & 0x70);
                Assert.Equal(0xa5, acia.Read(true));
                return;
            }
        }
        Assert.Fail("No byte received through the real ACIA pins.");
    }

    [Fact]
    public void PowerOnRestoresTheDigitalStateWithoutChangingExternalInputPins()
    {
        var ula = new Ula(Clock) { Rs423ReceiveData = false, Rs423ClearToSendHigh = false };
        ula.Write(0xff);
        for (var tick = 0; tick < 10000; tick++) ula.Tick(false, false);
        ula.PowerOn();
        var reference = new Ula(Clock);
        Assert.Equal(0, ula.Control);
        Assert.False(ula.Motor);
        Assert.False(ula.Rs423Selected);
        Assert.False(ula.Rs423ReceiveData);
        Assert.False(ula.Rs423ClearToSendHigh);
        Assert.True(ula.ReceiveData);
        Assert.False(ula.DataCarrierDetectHigh);
        for (var tick = 0; tick < 2000; tick++)
        {
            ula.Tick(true, false);
            reference.Tick(true, false);
            Assert.Equal(reference.TransmitClockEdge, ula.TransmitClockEdge);
            Assert.Equal(reference.ReceiveClockEdge, ula.ReceiveClockEdge);
            Assert.Equal(reference.CassetteOutput, ula.CassetteOutput);
        }
    }

    [Fact]
    public void ExternalCtsReleasesThePendingAciaByteOnlyInRs423Mode()
    {
        var ula = new Ula(Clock);
        var acia = new Acia6850(Clock, 0);
        ula.Write(0x49);
        acia.Write(false, 0x95);
        acia.Write(true, 0x5a);
        void Tick()
        {
            ula.Tick(acia.TransmitBit, acia.RtsHigh);
            acia.SetCts(ula.ClearToSendHigh);
            if (ula.TransmitClockEdge) acia.ClockTransmit();
        }
        for (var tick = 0; tick < 20000; tick++) { Tick(); Assert.True(acia.TransmitBit); }
        Assert.Equal(0, acia.Read(false) & 2);
        ula.Rs423ClearToSendHigh = false;
        var start = false;
        for (var tick = 0; tick < 20000; tick++) { Tick(); start |= !acia.TransmitBit; }
        Assert.True(start);
        Assert.Equal(2, acia.Read(false) & 2);
        ula.Write(0x85);
        ula.Rs423ClearToSendHigh = true;
        Tick();
        Assert.False(ula.ClearToSendHigh);
    }

    [Fact]
    public void CarrierRequiresAContinuousHighToneAndIsClearedBySilence()
    {
        var ula = new Ula(Clock);
        var carrier = false;
        for (var tick = 0; tick < Clock / 4; tick++)
        {
            ula.SetCassetteInput((tick / 833 & 1) != 0); // low tone cannot qualify carrier
            ula.Tick(true, false);
            Assert.False(ula.DataCarrierDetectHigh);
        }
        for (var tick = 0; tick < Clock / 4; tick++)
        {
            ula.SetCassetteInput((tick / 417 & 1) != 0);
            ula.Tick(true, false);
            carrier |= ula.DataCarrierDetectHigh;
        }
        Assert.True(carrier);
        ula.SetCassetteInput(null);
        for (var tick = 0; tick < Clock / 500; tick++) ula.Tick(true, false);
        Assert.False(ula.DataCarrierDetectHigh);
        Assert.True(ula.ReceiveData);
    }
}
