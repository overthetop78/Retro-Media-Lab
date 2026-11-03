using RetroMediaLab.Chips.IO.NEC.UPD700x.UPD7002.Services;

namespace RetroMediaLab.Chips.Tests.IO.NEC.UPD700x.UPD7002;

public sealed class Adc7002PinTests
{
    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, false, false)]
    [InlineData(false, true, true)]
    public void DeselectedOrIdleBusDoesNotDriveDataOrAcknowledgeEoc(bool cs, bool rd, bool wr)
    {
        var adc = Completed();
        Assert.Null(adc.SetBus(cs, rd, wr, 1, 0xff));
        Assert.False(adc.EndOfConversionHigh);
        Assert.False(adc.Converting);
        Assert.Equal(0x48, adc.Read(0));
    }

    [Fact]
    public void ReadSelectionDrivesRegistersAndOnlyHighByteAcknowledgesEoc()
    {
        var adc = Completed();
        Assert.Equal((byte)0x48, adc.SetBus(false, false, true, 0, 0xff));
        Assert.Equal((byte)0x30, adc.SetBus(false, false, true, 2, 0xff));
        Assert.Equal((byte)0x30, adc.SetBus(false, false, true, 3, 0xff));
        Assert.False(adc.EndOfConversionHigh);
        Assert.Equal((byte)0x12, adc.SetBus(false, false, true, 5, 0xff));
        Assert.True(adc.EndOfConversionHigh);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConversionWaitsForWriteRisingEdgeWithEitherClockSource(bool external)
    {
        var levels = new List<bool>();
        var adc = new Adc7002(levels.Add, conversionClockHz: external ? 0 : 1_000_000);
        adc.SetChannel(0, 0x1234);
        Assert.Null(adc.SetBus(false, true, false, 0, 8));
        Assert.True(adc.EndOfConversionHigh);
        Assert.False(adc.Converting);
        adc.Advance(100_000);
        for (var i = 0; i < 20_000; i++) { adc.Tick(); adc.Clock(); }
        Assert.Equal(0x80, adc.Read(0));
        Assert.Equal(new[] { true }, levels);

        adc.SetBus(false, true, true, 0, 8);
        Assert.True(adc.Converting);
        if (external)
        {
            for (var i = 0; i < 9_999; i++) adc.Clock();
        }
        else adc.Advance(19_999);
        Assert.True(adc.Converting);
        if (external) adc.Clock(); else adc.Tick();
        Assert.False(adc.Converting);
        Assert.Equal(0x48, adc.Read(0));
        Assert.Equal(0x12, adc.Read(1));
        Assert.Equal(new[] { true, false, true }, levels);
    }

    [Fact]
    public void DataIsLatchedDuringWriteAndIdleCallsDoNotRestartConversion()
    {
        var adc = new Adc7002(_ => { });
        adc.SetChannel(1, 0xabcd);
        adc.SetBus(false, true, false, 0, 8);
        adc.SetBus(false, true, false, 0, 13);
        adc.SetBus(false, true, true, 0, 13);
        adc.Advance(10_000);
        adc.SetBus(true, true, true, 0, 0);
        adc.Advance(10_000);
        Assert.Equal(0x6d, adc.Read(0));
        Assert.Equal(0xab, adc.Read(1));
    }

    [Fact]
    public void CommandWriteStopsOldConversionUntilItsStrobeEnds()
    {
        var adc = new Adc7002(_ => { });
        adc.SetChannel(0, 0x1234);
        adc.SetChannel(1, 0xabcd);
        adc.Write(0, 8);
        adc.Advance(19_999);
        adc.SetBus(false, true, false, 0, 9);
        adc.Advance(50_000);
        Assert.Equal(0x80, adc.Read(0));
        Assert.Equal(0, adc.Read(1));
        adc.SetBus(false, true, true, 0, 9);
        adc.Advance(20_000);
        Assert.Equal(0xab, adc.Read(1));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void UnselectedWriteAddressesDoNotRestartOrCancelConversion(int register)
    {
        var adc = new Adc7002(_ => { });
        adc.SetChannel(0, 0x1234);
        adc.Write(0, 8);
        adc.Advance(10_000);
        adc.SetBus(false, true, false, register, 0xff);
        adc.Advance(1_000);
        adc.SetBus(false, true, true, register, 0xff);
        adc.Advance(9_000);
        Assert.Equal(0x48, adc.Read(0));
    }

    [Fact]
    public void ResetDiscardsThePendingWriteCycle()
    {
        var adc = new Adc7002(_ => { });
        adc.SetBus(false, true, false, 0, 8);
        adc.Reset();
        adc.SetBus(true, true, true, 0, 8);
        Assert.False(adc.Converting);
        Assert.Equal(0, adc.Read(0));
    }

    private static Adc7002 Completed()
    {
        var adc = new Adc7002(_ => { });
        adc.SetChannel(0, 0x1234);
        adc.Write(0, 8);
        adc.Advance(20_000);
        return adc;
    }
}
