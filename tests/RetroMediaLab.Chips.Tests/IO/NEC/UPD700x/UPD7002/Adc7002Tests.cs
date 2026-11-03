using RetroMediaLab.Chips.IO.NEC.UPD700x.UPD7002.Services;

namespace RetroMediaLab.Chips.Tests.IO.NEC.UPD700x.UPD7002;

public sealed class Adc7002Tests
{
    [Fact]
    public void AllCommandsSelectTheirChannelAndEchoOnlyLowNibble()
    {
        var adc = new Adc7002(_ => { });
        ushort[] values = [0x1230, 0x4560, 0x7890, 0xabc0];
        for (var channel = 0; channel < values.Length; channel++) adc.SetChannel(channel, values[channel]);
        for (var command = 0; command <= byte.MaxValue; command++)
        {
            var previous = adc.Read(0) & 0x3f;
            adc.Write(0, (byte)command);
            Assert.Equal(0x80 | previous, adc.Read(0));
            Assert.True(adc.EndOfConversionHigh);
            Assert.True(adc.Converting);
            Assert.Equal((previous & 4) != 0, adc.Flag);
            adc.Advance(20_000);
            var result = values[command & 3];
            Assert.Equal(0x40 | (command & 15) | ((result & 0xc000) >> 10), adc.Read(0));
            Assert.Equal((command & 4) != 0, adc.Flag);
            Assert.False(adc.EndOfConversionHigh);
            Assert.Equal(result >> 8, adc.Read(1));
            Assert.Equal((command & 8) != 0 ? result & 0xf0 : 0, adc.Read(2));
            Assert.Equal(adc.Read(2), adc.Read(3));
            Assert.True(adc.EndOfConversionHigh);
            Assert.False(adc.Converting);
        }
    }

    [Fact]
    public void EveryTwelveBitCodeAndEveryDiscardedInputBitHaveTheirSpecifiedPosition()
    {
        var adc = new Adc7002(_ => { });
        for (var value = 0; value <= ushort.MaxValue; value++)
        {
            adc.SetChannel(0, (ushort)value);
            adc.Write(0, 8);
            adc.Advance(20_000);
            Assert.Equal(value >> 8, adc.Read(1));
            Assert.Equal(value & 0xf0, adc.Read(2));
            Assert.Equal((value >> 10) & 0x30, adc.Read(0) & 0x30);
        }
    }

    [Theory]
    [InlineData(0, 8_000)]
    [InlineData(8, 20_000)]
    public void ConversionCompletesAtTypicalNecTimeAndOnlyOnce(int command, int ticks)
    {
        var levels = new List<bool>();
        var adc = new Adc7002(levels.Add);
        adc.SetChannel(0, 0x9876);
        adc.Write(0, (byte)command);
        adc.Advance(ticks - 1);
        Assert.Equal(0x80, adc.Read(0));
        Assert.Equal(0, adc.Read(1));
        Assert.Equal(new[] { true }, levels);
        adc.Tick();
        Assert.Equal(0x60 | command, adc.Read(0));
        Assert.False(adc.EndOfConversionHigh);
        Assert.Equal(0x98, adc.Read(1));
        Assert.Equal(command == 0 ? 0 : 0x70, adc.Read(2));
        Assert.Equal(new[] { true, false, true }, levels);
        adc.Advance(int.MaxValue);
        adc.Tick();
        Assert.Equal(new[] { true, false, true }, levels);
    }

    [Theory]
    [InlineData(1_000_000, 1_000_000, 10_000)]
    [InlineData(2_000_000, 2_000_000, 10_000)]
    [InlineData(2_000_000, 500_000, 40_000)]
    [InlineData(1_000, 3_000_000, 4)]
    [InlineData(int.MaxValue, 1, 21_474_836_470_000L)]
    public void HostAndConversionClocksAreIndependentAndDoNotOverflow(int host, int conversion, long expected)
    {
        var adc = new Adc7002(_ => { }, host, conversion);
        adc.Write(0, 8);
        var remaining = expected - 1;
        while (remaining > 0)
        {
            var batch = (int)Math.Min(remaining, int.MaxValue);
            adc.Advance(batch);
            remaining -= batch;
        }
        Assert.True(adc.Converting);
        adc.Advance(1);
        Assert.False(adc.Converting);
    }

    [Fact]
    public void ExplicitCrystalClockStopsWhenNoEdgesArrive()
    {
        var adc = new Adc7002(_ => { }, conversionClockHz: 0);
        adc.Write(0, 0);
        adc.Advance(int.MaxValue);
        adc.Tick();
        Assert.True(adc.Converting);
        for (var cycle = 0; cycle < 3_999; cycle++) adc.Clock();
        Assert.True(adc.Converting);
        adc.Clock();
        Assert.False(adc.Converting);
    }

    [Fact]
    public void RestartCancelsOldCompletionAndKeepsPreviousDataUntilNewCompletion()
    {
        var levels = new List<bool>();
        var adc = new Adc7002(levels.Add);
        adc.SetChannel(0, 0x1234);
        adc.SetChannel(1, 0xabcd);
        adc.Write(0, 8);
        adc.Advance(20_000);
        adc.Write(0, 0);
        adc.Advance(7_999);
        adc.Write(0, 9);
        adc.Advance(1);
        Assert.True(adc.Converting);
        Assert.Equal(0x12, adc.Read(1));
        adc.Advance(19_999);
        Assert.Equal(0xab, adc.Read(1));
        Assert.Equal(new[] { true, false, true, false, true }, levels);
    }

    [Fact]
    public void LongConversionIntegratesInputChangesUntilTheEndOfItsChargingWindow()
    {
        var adc = new Adc7002(_ => { });
        adc.SetChannel(2, 0x1234);
        adc.Write(0, 10);
        adc.Advance(10_000);
        adc.SetChannel(2, 0xabcd);
        adc.Advance(10_000);
        var average = (0x1234L * 10_000 + 0xabcdL * 6_384) / 16_384;
        Assert.Equal(average >> 8, adc.Read(1));
        Assert.Equal(average & 0xf0, adc.Read(2));
        adc.Write(0, 10);
        adc.Advance(20_000);
        Assert.Equal(0xab, adc.Read(1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(8191)]
    [InlineData(8192)]
    [InlineData(8193)]
    [InlineData(16383)]
    [InlineData(16384)]
    [InlineData(16385)]
    [InlineData(19999)]
    public void InputChangesAtEveryWindowBoundaryHaveTheCorrectWeight(int changeAt)
    {
        var adc = new Adc7002(_ => { });
        adc.SetChannel(0, ushort.MaxValue);
        adc.Write(0, 8);
        adc.Advance(changeAt);
        adc.SetChannel(0, 0);
        adc.Advance(20_000 - changeAt);
        var expected = (long)ushort.MaxValue * Math.Min(changeAt, 16_384) / 16_384;
        Assert.Equal(expected >> 8, adc.Read(1));
        Assert.Equal(expected & 0xf0, adc.Read(2));
    }

    [Fact]
    public void ExplicitClockAndHostClockIntegrateTheSameInputWaveform()
    {
        var external = new Adc7002(_ => { }, conversionClockHz: 0);
        var automatic = new Adc7002(_ => { });
        external.Write(0, 8); automatic.Write(0, 8);
        for (var cycle = 0; cycle < 10_000; cycle++)
        {
            var value = (ushort)(cycle * 19);
            external.SetChannel(0, value); automatic.SetChannel(0, value);
            external.Clock(); automatic.Tick(); automatic.Tick();
        }
        Assert.Equal(automatic.Read(0), external.Read(0));
        Assert.Equal(automatic.Read(1), external.Read(1));
        Assert.Equal(automatic.Read(2), external.Read(2));
    }

    [Fact]
    public void RestartAndOtherChannelsDoNotContributeOldCharge()
    {
        var adc = new Adc7002(_ => { });
        adc.SetChannel(0, ushort.MaxValue);
        adc.SetChannel(1, 0x1234);
        adc.Write(0, 8);
        adc.Advance(8_000);
        adc.SetChannel(1, 0x4567);
        adc.Write(0, 9);
        adc.Advance(16_384);
        adc.SetChannel(1, 0);
        adc.SetChannel(0, 0);
        adc.Advance(3_616);
        Assert.Equal(0x45, adc.Read(1));
        Assert.Equal(0x60, adc.Read(2));
    }

    [Fact]
    public void ChargeAccumulatorDoesNotOverflowAtTheLargestHostToCrystalRatio()
    {
        var adc = new Adc7002(_ => { }, int.MaxValue, 1);
        adc.SetChannel(0, ushort.MaxValue);
        adc.Write(0, 8);
        var remaining = 10_000L * int.MaxValue;
        while (remaining > 0)
        {
            var batch = (int)Math.Min(remaining, int.MaxValue);
            adc.Advance(batch);
            remaining -= batch;
        }
        Assert.Equal(255, adc.Read(1));
        Assert.Equal(0xf0, adc.Read(2));
    }

    [Fact]
    public void UnselectedWriteAddressesDoNotStartOrRestartConversion()
    {
        var adc = new Adc7002(_ => { });
        adc.Write(1, 255);
        adc.Write(2, 255);
        Assert.False(adc.Converting);
        adc.Write(0, 8);
        adc.Advance(19_999);
        adc.Write(1, 255);
        adc.Write(2, 255);
        adc.Tick();
        Assert.False(adc.Converting);
    }

    [Fact]
    public void AddressDecodingUsesOnlyTwoLowBitsAndTestAccessIsExplicit()
    {
        var adc = new Adc7002(_ => { });
        adc.Write(0xfc, 15);
        Assert.Equal(0x80, adc.Read(0x100));
        adc.Advance(20_000);
        Assert.Equal(0x4f, adc.Read(0x100));
        adc.Write(0xff, 0);
        Assert.True(adc.TestMode);
        adc.Write(0, 0);
        Assert.False(adc.TestMode);
    }

    [Fact]
    public void PowerResetCancelsConversionWithoutErasingExternallyDrivenInputs()
    {
        var levels = new List<bool>();
        var adc = new Adc7002(levels.Add);
        adc.SetChannel(0, 0x5678);
        adc.Write(0, 12);
        adc.Advance(19_999);
        adc.Reset();
        adc.Advance(20_000);
        Assert.False(adc.Converting);
        Assert.False(adc.EndOfConversionHigh);
        Assert.False(adc.Flag);
        Assert.Equal(0, adc.Read(1));
        adc.Write(0, 8);
        adc.Advance(20_000);
        Assert.Equal(0x56, adc.Read(1));
        Assert.Equal(new[] { true, false, true, false, true }, levels);
    }

    [Fact]
    public void StatusAndLowReadsKeepEocLowButHighReadAcknowledgesWithoutClearingBusyLatch()
    {
        var levels = new List<bool>();
        var adc = new Adc7002(levels.Add);
        adc.SetChannel(3, 0x9234);
        adc.Write(0, 15);
        adc.Advance(20_000);
        Assert.Equal(0x6f, adc.Read(0));
        Assert.Equal(0x30, adc.Read(2));
        Assert.Equal(0x30, adc.Read(3));
        Assert.False(adc.EndOfConversionHigh);
        Assert.Equal(0x92, adc.Read(1));
        Assert.Equal(0xef, adc.Read(0));
        Assert.False(adc.Converting);
        adc.Read(1);
        Assert.Equal(new[] { true, false, true }, levels);
        adc.Write(0, 0);
        Assert.Equal(0xaf, adc.Read(0)); // Données de la conversion précédente, BUSY actif.
        Assert.True(adc.Flag);
        adc.Read(1); // Acquittement pendant la conversion : ne l'annule pas.
        adc.Advance(8_000);
        Assert.Equal(0x40, adc.Read(0));
        Assert.False(adc.Flag);
    }

    [Fact]
    public void BatchedClocksKeepConversionsStartedByCompletionCallback()
    {
        Adc7002? adc = null;
        var completions = 0;
        adc = new Adc7002(high =>
        {
            if (high) return;
            completions++;
            if (completions < 3) adc!.Write(0, 0);
        });
        adc.Write(0, 0);
        adc.Advance(23_999);
        Assert.Equal(2, completions);
        Assert.True(adc.Converting);
        adc.Tick();
        Assert.Equal(3, completions);
        Assert.False(adc.Converting);
    }

    [Fact]
    public void InvalidIntegrationParametersAreRejectedBeforeExecution()
    {
        Assert.Throws<ArgumentNullException>(() => new Adc7002(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Adc7002(_ => { }, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Adc7002(_ => { }, conversionClockHz: -1));
        var adc = new Adc7002(_ => { });
        Assert.Throws<ArgumentOutOfRangeException>(() => adc.SetChannel(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => adc.SetChannel(4, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => adc.Advance(-1));
    }
}
