using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Adaptec.ACB4000.ACB4000;

public sealed class Acb4000ParameterChannelTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void LayoutComesFromThePhysicalOrderRatherThanParameterFlags(bool sequential)
    {
        var flags = new byte[9];
        Acb4000PhysicalParameters.Write(flags, Acb4000DriveParameters.Default);
        var channel = new Acb4000ParameterChannel();
        channel.Begin(Acb4000DriveParameters.Default);
        channel.SetIndex(true);
        for (var index = 0; index < flags.Length; index++)
            FeedId(channel, (uint)(sequential ? index : index * 2), flags[index]);
        Assert.True(channel.FormatValid);
        Assert.Equal(sequential, channel.SequentialLayout);
        channel.Reset();
        Assert.False(channel.SequentialLayout);
    }

    [Theory]
    [InlineData(256)]
    [InlineData(512)]
    [InlineData(1024)]
    public void OnlyThePhysicalIdFlagsAfterIndexCanSupplyTheParameters(ushort size)
    {
        var expected = Acb4000DriveParameters.Default with
        {
            BlockSize = size, Cylinders = 2048, Heads = 16, ReducedWriteCurrent = 2047,
            LandingZone = 0xff, StepRate = 2
        };
        var flags = new byte[9];
        Acb4000PhysicalParameters.Write(flags, expected);
        var channel = new Acb4000ParameterChannel();
        channel.Begin(Acb4000DriveParameters.Default);
        FeedParameters(channel, flags);
        Assert.False(channel.Complete);
        channel.SetIndex(true);
        channel.SetIndex(true);
        FeedParameters(channel, flags);
        Assert.True(channel.Complete);
        Assert.True(channel.FormatValid);
        Assert.Equal(expected, channel.Parameters);
        channel.SetIndex(false);
        channel.SetIndex(true);
        FeedParameters(channel, new byte[9]);
        Assert.Equal(expected, channel.Parameters);
    }

    [Fact]
    public void BadIdCheckCannotShiftTheParameterPositions()
    {
        var flags = new byte[9];
        Acb4000PhysicalParameters.Write(flags, Acb4000DriveParameters.Default);
        var channel = new Acb4000ParameterChannel();
        channel.Begin(Acb4000DriveParameters.Default);
        channel.SetIndex(true);
        FeedId(channel, 0, flags[0]);
        FeedId(channel, 2, flags[1], corrupt: true);
        for (var index = 2; index < 9; index++) FeedId(channel, (uint)(index * 2), flags[index]);
        Assert.False(channel.Complete);
        Assert.False(channel.FormatValid);
        channel.SetIndex(false);
        channel.SetIndex(true);
        FeedParameters(channel, flags);
        Assert.True(channel.FormatValid);
    }

    [Fact]
    public void PhysicalOrdinalsDoNotUseLogicalAddressesOrTheDefectiveAddressBitToSelectParameters()
    {
        var flags = new byte[9];
        Acb4000PhysicalParameters.Write(flags, Acb4000DriveParameters.Default);
        var channel = new Acb4000ParameterChannel();
        channel.Begin(Acb4000DriveParameters.Default);
        channel.SetIndex(true);
        for (var index = 0; index < flags.Length; index++) FeedId(channel, 0x7fffff, flags[index], defective: true);
        Assert.True(channel.Complete);
        Assert.True(channel.FormatValid);
        Assert.Equal(Acb4000DriveParameters.Default, channel.Parameters);
    }

    [Fact]
    public void ANewIndexDiscardsAnIncompleteGroupAndExpiryDoesNotInventAFormat()
    {
        var flags = new byte[9];
        Acb4000PhysicalParameters.Write(flags, Acb4000DriveParameters.Default);
        var channel = new Acb4000ParameterChannel();
        channel.Begin(Acb4000DriveParameters.Default);
        channel.SetIndex(true);
        FeedId(channel, 0, flags[0]);
        channel.SetIndex(false);
        channel.SetIndex(true);
        for (var index = 1; index < 8; index++) FeedId(channel, (uint)(index * 2), flags[index]);
        Assert.False(channel.Complete);
        channel.Expire();
        Assert.True(channel.Complete);
        Assert.False(channel.FormatValid);
        FeedParameters(channel, flags);
        Assert.False(channel.FormatValid);
        channel.Reset();
        Assert.False(channel.Complete);
    }

    [Fact]
    public void ValidIdsWithInvalidParameterValuesRemainAnInvalidFormat()
    {
        var channel = new Acb4000ParameterChannel();
        channel.Begin(Acb4000DriveParameters.Default);
        channel.SetIndex(true);
        FeedParameters(channel, new byte[9]);
        Assert.True(channel.Complete);
        Assert.False(channel.FormatValid);
    }

    [Fact]
    public void FourthBadCheckOfTheRequestedOrdinalEndsAcquisition()
    {
        var channel = new Acb4000ParameterChannel();
        channel.Begin(Acb4000DriveParameters.Default);
        for (var attempt = 0; attempt < 4; attempt++)
        {
            channel.SetIndex(false);
            channel.SetIndex(true);
            FeedId(channel, 0, 0, corrupt: true);
            Assert.Equal(attempt == 3, channel.Complete);
            Assert.False(channel.FormatValid);
        }
        var flags = new byte[9];
        Acb4000PhysicalParameters.Write(flags, Acb4000DriveParameters.Default);
        channel.SetIndex(false);
        channel.SetIndex(true);
        FeedParameters(channel, flags);
        Assert.False(channel.FormatValid);
        channel.Begin(Acb4000DriveParameters.Default);
        channel.SetIndex(true);
        FeedParameters(channel, flags);
        Assert.True(channel.FormatValid);
    }

    [Fact]
    public void EachOrdinalHasFourChecksAndEarlierIdCrcDoesNotInvalidateItsRetry()
    {
        var expected = Acb4000DriveParameters.Default with { Cylinders = 306, Heads = 4, LandingZone = 100 };
        var flags = new byte[9];
        Acb4000PhysicalParameters.Write(flags, expected);
        var channel = new Acb4000ParameterChannel();
        channel.Begin(Acb4000DriveParameters.Default);
        for (var ordinal = 0; ordinal < flags.Length; ordinal++)
        {
            for (var attempt = 0; attempt < 4; attempt++)
            {
                channel.SetIndex(false);
                channel.SetIndex(true);
                for (var preceding = 0; preceding < ordinal; preceding++)
                    FeedId(channel, (uint)preceding, 0, corrupt: true);
                FeedId(channel, (uint)ordinal, flags[ordinal], corrupt: attempt < 3);
                Assert.Equal(attempt < 3 ? ordinal : ordinal + 1, channel.ParametersRead);
                Assert.Equal(ordinal == flags.Length - 1 && attempt == 3, channel.Complete);
            }
        }
        Assert.True(channel.FormatValid);
        Assert.Equal(expected, channel.Parameters);
    }

    private static void FeedParameters(Acb4000ParameterChannel channel, byte[] flags)
    {
        for (var index = 0; index < flags.Length; index++) FeedId(channel, (uint)(index * 2), flags[index]);
    }

    private static void FeedId(Acb4000ParameterChannel channel, uint address, byte flags,
        bool defective = false, bool corrupt = false)
    {
        Span<byte> id = stackalloc byte[10];
        Span<ushort> words = stackalloc ushort[10];
        Acb4000FieldCodec.WriteId(id, address, flags, defective);
        if (corrupt) id[^1] ^= 1;
        var previous = false;
        Acb4000MfmEncoding.WriteField(id, words, ref previous);
        foreach (var word in words)
            for (var bit = 15; bit >= 0; bit--) channel.ClockCell((word & (1 << bit)) != 0);
    }
}
