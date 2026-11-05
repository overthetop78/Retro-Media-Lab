using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Adaptec.ACB4000.ACB4000;

public sealed class Acb4000PhysicalParameterTests
{
    [Fact]
    public void RecordedParametersDecodeWithoutReplacingUnrecordedPrecompensation()
    {
        var previous = Acb4000DriveParameters.Default with { WritePrecompensation = 87 };
        Assert.True(Acb4000PhysicalParameters.TryRead(
            [0, 0x48, 0x7f, 0x6f, 0x7f, 0x6f, 0x7f, 0x4f, 0x7f], previous, out var decoded));
        Assert.Equal(new Acb4000DriveParameters(1024, 2048, 16, 2047, 87, 0xff, 2), decoded);
    }

    [Fact]
    public void MissingOrInvalidRecordsAreNotTreatedAsFormattedGeometry()
    {
        var previous = Acb4000DriveParameters.Default;
        Assert.False(Acb4000PhysicalParameters.TryRead([], previous, out var unchanged));
        Assert.Equal(previous, unchanged);
        Assert.False(Acb4000PhysicalParameters.TryRead(new byte[9], previous, out unchanged));
        Assert.Equal(previous, unchanged);
        // Step code 3 is outside the three documented step rates.
        Assert.False(Acb4000PhysicalParameters.TryRead([0, 0x62, 0, 0, 0, 0, 0, 0, 0], previous, out unchanged));
        Assert.Equal(previous, unchanged);
        Assert.False(Acb4000PhysicalParameters.TryRead([0x80, 2, 0, 0, 0, 0, 0, 0, 0], previous, out unchanged));
        Assert.Equal(previous, unchanged);
    }

    [Fact]
    public void NominalParametersMatchTheFirmwarePacking()
    {
        var flags = new byte[9];
        Acb4000PhysicalParameters.Write(flags, Acb4000DriveParameters.Default);
        Assert.Equal(new byte[] { 0, 2, 0x31, 0x22, 0x16, 1, 0x16, 1, 0 }, flags);
    }

    [Fact]
    public void MaximumFieldsKeepAllHeadAndParkingBits()
    {
        var flags = new byte[9];
        Acb4000PhysicalParameters.Write(flags, new(1024, 2048, 16, 2047, 0, 0xff, 2));
        Assert.Equal(new byte[] { 0, 0x48, 0x7f, 0x6f, 0x7f, 0x6f, 0x7f, 0x4f, 0x7f }, flags);
    }

    [Fact]
    public void MinimalGeometryUsesLastCylinderAndHeadRatherThanCounts()
    {
        var flags = new byte[9];
        Acb4000PhysicalParameters.Write(flags, new(256, 1, 1, 0, 0, 0, 0));
        Assert.Equal(new byte[] { 0, 2, 0, 0, 0, 0, 0, 0, 0 }, flags);
    }

    [Fact]
    public void InvalidArgumentsDoNotChangeTheDestination()
    {
        var flags = Enumerable.Repeat((byte)0x55, 10).ToArray();
        Assert.Throws<ArgumentOutOfRangeException>(() => Acb4000PhysicalParameters.Write(flags,
            Acb4000DriveParameters.Default with { Heads = 0 }));
        Assert.All(flags, value => Assert.Equal(0x55, value));
        Assert.Throws<ArgumentException>(() => Acb4000PhysicalParameters.Write(flags.AsSpan(0, 8),
            Acb4000DriveParameters.Default));
        Assert.All(flags, value => Assert.Equal(0x55, value));
        Acb4000PhysicalParameters.Write(flags, Acb4000DriveParameters.Default);
        Assert.Equal(0x55, flags[9]);
    }
}
