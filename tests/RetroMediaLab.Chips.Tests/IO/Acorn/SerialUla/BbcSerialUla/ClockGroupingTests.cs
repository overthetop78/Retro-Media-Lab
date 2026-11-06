using Ula = RetroMediaLab.Chips.IO.Acorn.SerialUla.BbcSerialUla.Services.BbcSerialUla;

namespace RetroMediaLab.Chips.Tests.IO.Acorn.SerialUla.BbcSerialUla;

public sealed class ClockGroupingTests
{
    [Theory]
    [InlineData(2_000_000)]
    [InlineData(3_000_001)]
    public void ActiveTapeCarrierAndSilenceKeepEveryUnitClockTransition(int frequency)
    {
        var grouped = new Ula(frequency);
        var unit = new Ula(frequency);
        grouped.Write(0x85);
        unit.Write(0x85);
        var carrierSeen = false;
        for (var tick = 0; tick < frequency / 2; tick++)
        {
            bool? level = tick < frequency / 3 ? ((long)tick * 4800 / frequency & 1) != 0 : null;
            grouped.SetCassetteInput(level);
            unit.SetCassetteInput(level);
            grouped.Tick();
            unit.Tick(true, true);
            Assert.Equal((unit.SourceClockPhase, unit.ReceiveData, unit.DataCarrierDetectHigh,
                unit.TransmitClockEdge, unit.ReceiveClockEdge, unit.CassetteOutput),
                (grouped.SourceClockPhase, grouped.ReceiveData, grouped.DataCarrierDetectHigh,
                grouped.TransmitClockEdge, grouped.ReceiveClockEdge, grouped.CassetteOutput));
            carrierSeen |= grouped.DataCarrierDetectHigh;
        }
        Assert.True(carrierSeen);
        Assert.False(grouped.DataCarrierDetectHigh);
        Assert.True(grouped.ReceiveData);
    }

    [Theory]
    [InlineData(1_230_770)]
    [InlineData(2_000_000)]
    [InlineData(3_000_001)]
    public void EveryDividerAndMuxKeepsTheUnitClockTraceAcrossInputChanges(int frequency)
    {
        var grouped = new Ula(frequency);
        var unit = new Ula(frequency);
        var groupedPins = new List<(bool, bool, bool, bool)>();
        var unitPins = new List<(bool, bool, bool, bool)>();
        var groupedClocks = new List<(bool, bool, bool)>();
        var unitClocks = new List<(bool, bool, bool)>();
        grouped.SerialPinsChanged = (a, b, c, d) => groupedPins.Add((a, b, c, d));
        unit.SerialPinsChanged = (a, b, c, d) => unitPins.Add((a, b, c, d));
        grouped.SerialClocksRaised = (a, b, c) => groupedClocks.Add((a, b, c));
        unit.SerialClocksRaised = (a, b, c) => unitClocks.Add((a, b, c));
        var data = true;
        var rts = true;
        void Step()
        {
            grouped.Tick();
            unit.Tick(data, rts);
            Assert.Equal(unit.SourceClockPhase, grouped.SourceClockPhase);
            Assert.Equal((unit.TransmitClockEdge, unit.ReceiveClockEdge, unit.CassetteOutput,
                unit.Rs423TransmitData, unit.Rs423RequestToSendHigh, unit.ReceiveData,
                unit.ClearToSendHigh, unit.DataCarrierDetectHigh, (unit.Control, unit.Motor, unit.Rs423Selected)),
                (grouped.TransmitClockEdge, grouped.ReceiveClockEdge, grouped.CassetteOutput,
                grouped.Rs423TransmitData, grouped.Rs423RequestToSendHigh, grouped.ReceiveData,
                grouped.ClearToSendHigh, grouped.DataCarrierDetectHigh, (grouped.Control, grouped.Motor, grouped.Rs423Selected)));
            Assert.Equal(unitPins, groupedPins);
            Assert.Equal(unitClocks, groupedClocks);
            groupedPins.Clear(); unitPins.Clear();
            groupedClocks.Clear(); unitClocks.Clear();
        }
        for (var tick = 0; tick < frequency / 500; ++tick) Step();
        for (var control = 0; control < 128; ++control)
        {
            grouped.Write((byte)control); unit.Write((byte)control);
            for (var tick = 0; tick < 1200; ++tick)
            {
                if (tick % 137 == 0)
                {
                    data = !data;
                    rts = tick % 274 == 0;
                    grouped.SetTransmitPins(data, rts);
                    unit.SetTransmitPins(data, rts);
                }
                if (tick % 293 == 0)
                {
                    grouped.Rs423ReceiveData = unit.Rs423ReceiveData = !unit.Rs423ReceiveData;
                    grouped.Rs423ClearToSendHigh = unit.Rs423ClearToSendHigh = !unit.Rs423ClearToSendHigh;
                }
                Step();
            }
        }
        // Une entrée cassette réveille le séparateur, puis son silence autorise
        // à nouveau les intervalles groupés. L'expiration reste au même front.
        for (var tick = 0; tick < frequency / 20; ++tick)
        {
            bool? level = tick < frequency / 40 ? ((long)tick * 4800 / frequency & 1) != 0 : null;
            grouped.SetCassetteInput(level); unit.SetCassetteInput(level);
            Step();
        }
        grouped.PowerOn(); unit.PowerOn();
        for (var tick = 0; tick < 1000; ++tick) Step();
    }
}

