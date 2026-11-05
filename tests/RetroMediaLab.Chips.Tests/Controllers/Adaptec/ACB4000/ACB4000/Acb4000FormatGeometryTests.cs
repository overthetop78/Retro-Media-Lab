using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Adaptec.ACB4000.ACB4000;

public sealed class Acb4000FormatGeometryTests
{
    [Theory]
    [InlineData(256, 1, 32)]
    [InlineData(256, 2, 33)]
    [InlineData(512, 1, 17)]
    [InlineData(512, 2, 18)]
    [InlineData(1024, 1, 9)]
    [InlineData(1024, 2, 9)]
    public void ManufacturerTableDeterminesCapacity(ushort blockSize, byte interleave, int sectors)
    {
        var parameters = Acb4000DriveParameters.Default with { BlockSize = blockSize };
        Assert.True(Acb4000FormatGeometry.TryCreate(parameters, 0, interleave, out var geometry));
        Assert.Equal(sectors, geometry.SectorsPerTrack);
        Assert.Equal(interleave, geometry.Interleave);
        Assert.Equal(306L * 2 * sectors, geometry.BlockCount);
        Assert.Equal(306L * 2 * sectors * blockSize, geometry.CapacityBytes);
    }

    [Theory]
    [InlineData(256, 33)]
    [InlineData(512, 18)]
    [InlineData(1024, 9)]
    public void EveryInterleaveBoundaryAndDefaultIsChecked(ushort blockSize, int sectors)
    {
        var parameters = Acb4000DriveParameters.Default with { BlockSize = blockSize };
        Assert.True(Acb4000FormatGeometry.TryCreate(parameters, 0, 0, out var defaultGeometry));
        Assert.True(Acb4000FormatGeometry.TryCreate(parameters, 0, 2, out var explicitGeometry));
        Assert.Equal(explicitGeometry, defaultGeometry);
        for (var interleave = 0; interleave <= byte.MaxValue; interleave++)
        {
            var valid = Acb4000FormatGeometry.TryCreate(parameters, 0, (byte)interleave, out var geometry);
            Assert.Equal(interleave < sectors, valid);
            if (!valid) Assert.Equal(default, geometry);
        }
        for (var high = 1; high <= byte.MaxValue; high++)
            Assert.False(Acb4000FormatGeometry.TryCreate(parameters, (byte)high, 2, out _));
    }

    [Fact]
    public void MaximumCylinderAndHeadCountsDoNotOverflowCapacity()
    {
        var parameters = Acb4000DriveParameters.Default with { Cylinders = 2048, Heads = 16, BlockSize = 1024 };
        Assert.True(Acb4000FormatGeometry.TryCreate(parameters, 0, 0, out var geometry));
        Assert.Equal(301_989_888L, geometry.CapacityBytes);
        Assert.Equal(294_912L, geometry.BlockCount);
    }

    [Fact]
    public void InvalidDriveParametersCannotProduceAFormat()
    {
        Assert.False(Acb4000FormatGeometry.TryCreate(default, 0, 0, out var geometry));
        Assert.Equal(default, geometry);
    }
}
