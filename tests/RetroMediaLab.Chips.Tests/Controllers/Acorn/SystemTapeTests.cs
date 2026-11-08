using RetroMediaLab.Chips.Controllers.Acorn.SystemTape.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Acorn;

public sealed class SystemTapeTests
{
    [Theory]
    [InlineData(false, 416)]
    [InlineData(true, 208)]
    public void OutputUsesDivider26AndPhysicalCounterTap(bool mark, int halfPeriod)
    {
        var card = new SystemTapeInterface(1_000_000);
        var edges = new List<long>();
        card.CassetteOutputChanged = _ => edges.Add(card.ClockCount);
        card.SetOutputBit(mark, 0);
        card.AdvanceTo(halfPeriod * 4);
        Assert.Equal(new long[] { halfPeriod, halfPeriod * 2, halfPeriod * 3, halfPeriod * 4 }, edges);
    }

    [Fact]
    public void CarrierIsRawCounterQ7AndDataIsLatchedBeforeItsReset()
    {
        var card = new SystemTapeInterface(1_000_000);
        card.AdvanceTo(553);
        Assert.Equal(0x80, card.InputPins);
        card.AdvanceTo(554); // 64th Q3 falling edge: 21*26 + 8.
        Assert.Equal(0xa0, card.InputPins);
        Assert.True(card.CassetteInput); // Q7 alone does not clock the receive flip-flop.
        card.SetComparatorInput(true, 600);
        Assert.False(card.CassetteInput);
        Assert.Equal(0, card.InputPins); // Q7 sampled high, then asynchronously cleared.
        card.AdvanceTo(1151);
        Assert.Equal(0, card.InputPins);
        card.AdvanceTo(1152);
        Assert.Equal(0x20, card.InputPins);
    }

    [Fact]
    public void UnresetCarrierCounterWrapsInsteadOfSaturating()
    {
        var card = new SystemTapeInterface(1_000_000);
        var edges = new List<long>();
        card.InputPinsChanged = _ => edges.Add(card.ClockCount);
        card.AdvanceTo(1662);
        Assert.Equal(new long[] { 554, 1108, 1662 }, edges);
    }

    [Theory]
    [InlineData(416, true)]
    [InlineData(832, false)]
    public void ComparatorPeriodsDecodeTheTwoRecordedTones(int period, bool bit)
    {
        var card = new SystemTapeInterface(1_000_000);
        for (var cycle = 0; cycle < 8; cycle++)
        {
            card.SetComparatorInput(true, cycle * period);
            if (cycle != 0) Assert.Equal(bit, card.CassetteInput);
            Assert.Equal(0, card.InputPins & 0x20);
            card.SetComparatorInput(false, cycle * period + period / 2);
        }
    }

    [Fact]
    public void FallingOutputBitResetsToneCounterButKeepsTheMasterDividerPhase()
    {
        var card = new SystemTapeInterface(1_000_000);
        var edges = new List<long>();
        card.CassetteOutputChanged = _ => edges.Add(card.ClockCount);
        card.SetOutputBit(true, 0);
        card.SetOutputBit(false, 250);
        card.AdvanceTo(650);
        Assert.Equal(new long[] { 208, 250, 650 }, edges);
    }

    [Fact]
    public void ResetMovesTheClockOriginAndPublishesOnlyChangedLevels()
    {
        var card = new SystemTapeInterface(1_008_000);
        card.AdvanceTo(600);
        var changes = 0;
        card.InputPinsChanged = _ => changes++;
        card.Reset(1000);
        Assert.Equal(1, changes);
        card.AdvanceTo(1553);
        Assert.Equal(1, changes);
        card.AdvanceTo(1554);
        Assert.Equal(2, changes);
        Assert.Throws<ArgumentOutOfRangeException>(() => card.AdvanceTo(0));
    }

    [Fact]
    public void GroupedAdvancePreservesEveryOutputEdge()
    {
        static (List<(long, bool)> Audio, List<(long, byte)> Pins) Run(bool grouped)
        {
            var card = new SystemTapeInterface(1_000_000);
            var audio = new List<(long, bool)>();
            var pins = new List<(long, byte)>();
            card.CassetteOutputChanged = level => audio.Add((card.ClockCount, level));
            card.InputPinsChanged = level => pins.Add((card.ClockCount, level));
            for (var clock = 0; clock < 10000; clock++)
            {
                if (!grouped) card.AdvanceTo(clock);
                if (clock % 1000 == 17) card.SetOutputBit(true, clock);
                if (clock % 1000 == 701) card.SetOutputBit(false, clock);
                if (clock % 832 == 0) card.SetComparatorInput(true, clock);
                if (clock % 832 == 416) card.SetComparatorInput(false, clock);
            }
            card.AdvanceTo(10000);
            return (audio, pins);
        }
        var unit = Run(false);
        var grouped = Run(true);
        Assert.Equal(unit.Audio, grouped.Audio);
        Assert.Equal(unit.Pins, grouped.Pins);
    }

    [Fact]
    public void ScheduledCircuitMatchesBinaryCountersAtEveryMasterClock()
    {
        var card = new SystemTapeInterface(1_000_000);
        var divider = 0;
        var transmit = 0;
        var receive = 0;
        var outputBit = false;
        var selectedBit = false;
        var receivedBit = true;
        for (var clock = 1; clock <= 20000; clock++)
        {
            // Independent clock-by-clock wiring reference for IC1, IC2 and IC6.
            var oldQ3 = (divider & 4) != 0;
            divider++;
            if (oldQ3 && (divider & 4) == 0) receive = (receive + 1) & 127;
            if ((divider & 0x1a) == 0x1a)
            {
                divider = 0;
                selectedBit = outputBit;
                transmit = (transmit + 1) & 127;
            }
            card.AdvanceTo(clock);
            if (clock % 701 == 0)
            {
                outputBit = !outputBit;
                if (!outputBit) transmit = 0;
                card.SetOutputBit(outputBit, clock);
            }
            if (clock % 833 == 0)
            {
                receivedBit = (receive & 64) == 0;
                receive = 0;
                card.SetComparatorInput(true, clock);
            }
            if (clock % 833 == 416) card.SetComparatorInput(false, clock);
            Assert.Equal((transmit & (selectedBit ? 8 : 16)) != 0, card.CassetteOutput);
            Assert.Equal(receivedBit, card.CassetteInput);
            Assert.Equal((receivedBit ? 0x80 : 0) | ((receive & 64) != 0 ? 0x20 : 0), card.InputPins);
        }
    }
}
