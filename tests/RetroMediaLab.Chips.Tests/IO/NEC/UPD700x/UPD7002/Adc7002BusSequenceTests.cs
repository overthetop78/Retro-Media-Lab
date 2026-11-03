using RetroMediaLab.Chips.IO.NEC.UPD700x.UPD7002.Services;

namespace RetroMediaLab.Chips.Tests.IO.NEC.UPD700x.UPD7002;

public sealed class Adc7002BusSequenceTests
{
    [Fact]
    public void EocObserversSeeThePublishedStatusAndConversionState()
    {
        Adc7002? adc = null;
        var observed = new List<(bool Level, byte Status, bool Converting)>();
        adc = new Adc7002(high => observed.Add((high, adc!.Read(0), adc.Converting)));
        adc.SetChannel(2, 0xc234);
        adc.Write(0, 14);
        adc.Advance(20_000);
        Assert.Equal(new[] { (true, (byte)0x80, true), (false, (byte)0x7e, false) }, observed);
        Assert.Equal(0xc2, adc.Read(1));
        Assert.Equal((true, (byte)0xfe, false), observed[^1]);
    }

    [Fact]
    public void HighByteReturnsTheLatchedResultWhenItsAcknowledgementStartsAnotherConversion()
    {
        Adc7002? adc = null;
        var restart = false;
        adc = new Adc7002(high =>
        {
            if (!high || !restart) return;
            restart = false;
            adc!.Write(0, 9);
        });
        adc.SetChannel(0, 0x1234);
        adc.SetChannel(1, 0xabcd);
        adc.Write(0, 8);
        adc.Advance(20_000);
        restart = true;
        Assert.Equal(0x12, adc.Read(1));
        Assert.True(adc.Converting);
        Assert.Equal(0x88, adc.Read(0));
        adc.Advance(20_000);
        Assert.Equal(0x69, adc.Read(0));
        Assert.Equal(0xab, adc.Read(1));
    }

    [Theory]
    [InlineData(16_383)]
    [InlineData(16_384)]
    [InlineData(16_385)]
    public void ReadingTheOldHighByteAtTheIntegrationBoundaryDoesNotPublishOrCancelTheNewResult(int tick)
    {
        var adc = new Adc7002(_ => { });
        adc.SetChannel(0, 0x1234);
        adc.Write(0, 8);
        adc.Advance(20_000);
        adc.SetChannel(0, 0xabcd);
        adc.Write(0, 12);
        adc.Advance(tick);
        Assert.Equal(0x12, adc.Read(1));
        Assert.Equal(0x88, adc.Read(0));
        adc.Advance(20_000 - tick);
        Assert.Equal(0x6c, adc.Read(0));
        Assert.Equal(0xab, adc.Read(1));
    }
}
