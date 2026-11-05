using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Adaptec.ACB4000.ACB4000;

public sealed class Acb4000ClocksTests
{
    [Theory]
    [InlineData(0x00, 2)]
    [InlineData(0x40, 4)]
    [InlineData(0x80, 8)]
    public void SystemClockUsesTheSelectedDivider(byte control, int expectedFalls)
    {
        var clocks = new Acb4000Clocks();
        clocks.WriteControl(control);
        Assert.Equal(expectedFalls, CountFalls(clocks, false, 16));
    }

    [Theory]
    [InlineData(0x00, 2)]
    [InlineData(0x10, 4)]
    public void ReferenceProducesTwoOrFourCyclesPerByte(byte control, int expectedFalls)
    {
        var clocks = new Acb4000Clocks();
        clocks.WriteControl(control);
        clocks.SelectReference(true);
        Assert.Equal(expectedFalls, CountFalls(clocks, true, 16));
    }

    [Theory]
    [InlineData(0x00, false, 4)]
    [InlineData(0x40, false, 2)]
    [InlineData(0x80, false, 1)]
    [InlineData(0x00, true, 4)]
    [InlineData(0x10, true, 2)]
    public void RequestStartsBeforeTheFallingEdgeAndEndsAfterIt(byte control, bool reference, int divider)
    {
        var clocks = new Acb4000Clocks();
        clocks.WriteControl(control);
        clocks.SelectReference(reference);
        Assert.True(clocks.TryRequestTransfer());
        Assert.False(clocks.TryRequestTransfer());
        Advance(clocks, reference, divider - 1);
        // With the direct divider, the pulse begins at the first advancement from phase zero.
        if (divider == 1) Advance(clocks, reference, 1);
        else
        {
            Assert.True(clocks.ClockA);
            Assert.False(clocks.ClockB);
            Advance(clocks, reference, 1);
        }
        Assert.False(clocks.ClockA);
        Assert.False(clocks.ClockB);
        Advance(clocks, reference, 1);
        Assert.True(clocks.ClockB);
        Assert.False(clocks.TransferPending);
        Assert.True(clocks.TryRequestTransfer());
        Advance(clocks, reference, divider * 4);
        Assert.False(clocks.TransferPending);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GroupedAdvancementMatchesIndividualSourceEdges(bool reference)
    {
        for (var count = 0; count < 256; count++)
        {
            var grouped = new Acb4000Clocks();
            var serial = new Acb4000Clocks();
            grouped.SelectReference(reference);
            serial.SelectReference(reference);
            grouped.TryRequestTransfer();
            serial.TryRequestTransfer();
            Advance(grouped, reference, count);
            for (var tick = 0; tick < count; tick++) Advance(serial, reference, 1);
            Assert.Equal(serial.ClockA, grouped.ClockA);
            Assert.Equal(serial.ClockB, grouped.ClockB);
            Assert.Equal(serial.TransferPending, grouped.TransferPending);
        }
    }

    [Fact]
    public void DisabledAndUndefinedOutputsDoNotInventStableLevels()
    {
        var clocks = new Acb4000Clocks();
        clocks.WriteControl(0x08);
        Assert.False(clocks.OutputsEnabled);
        Assert.Null(clocks.ClockA);
        Assert.Null(clocks.ClockB);
        clocks.AdvanceSystemHalfCycles(4);
        clocks.WriteControl(0);
        Assert.False(clocks.ClockA);
        clocks.WriteControl(0xc0);
        Assert.True(clocks.OutputsEnabled);
        Assert.False(clocks.SourceIsDefined);
        Assert.Null(clocks.ClockA);
        clocks.SelectReference(true);
        Assert.True(clocks.SourceIsDefined);
        Assert.NotNull(clocks.ClockA);
    }

    [Fact]
    public void InactiveSourceAndPhaseResetPreserveTheirOwnControl()
    {
        var clocks = new Acb4000Clocks();
        clocks.WriteControl(0x07);
        clocks.TryRequestTransfer();
        clocks.AdvanceReferenceHalfCycles(4);
        Assert.True(clocks.ClockA);
        Assert.True(clocks.TransferPending);
        clocks.SelectReference(true);
        Assert.False(clocks.ClockA);
        clocks.ResetPhase();
        Assert.Equal(0x07, clocks.Control);
        Assert.True(clocks.ClockA);
        Assert.True(clocks.ClockB);
        Assert.False(clocks.TransferPending);
        Assert.Throws<ArgumentOutOfRangeException>(() => clocks.AdvanceSystemHalfCycles(-1));
    }

    private static int CountFalls(Acb4000Clocks clocks, bool reference, int halves)
    {
        var count = 0;
        for (var tick = 0; tick < halves; tick++)
        {
            var before = clocks.ClockA;
            Advance(clocks, reference, 1);
            if (before == true && clocks.ClockA == false) count++;
        }
        return count;
    }

    private static void Advance(Acb4000Clocks clocks, bool reference, int count)
    {
        if (reference) clocks.AdvanceReferenceHalfCycles(count);
        else clocks.AdvanceSystemHalfCycles(count);
    }
}
