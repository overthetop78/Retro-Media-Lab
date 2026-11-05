using System.Buffers.Binary;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Adaptec.ACB4000.ACB4000;

public sealed class Acb4000ParameterTests
{
    [Fact]
    public void ManufacturerDefaultVectorHasTheDocumentedHeaderAndDriveValues()
    {
        var expected = new byte[] { 0, 0, 0, 8, 0, 0, 0, 0, 0, 0, 1, 0, 1, 1, 0x32, 2, 0, 0x96, 0, 0x96, 0, 0 };
        var bytes = new byte[22];
        Acb4000DriveParameters.Default.Write(bytes);
        Assert.Equal(expected, bytes);
        Assert.True(Acb4000DriveParameters.TryDecode(expected, default, out var value));
        Assert.Equal(Acb4000DriveParameters.Default, value);
    }

    [Theory]
    [InlineData(256)]
    [InlineData(512)]
    [InlineData(1024)]
    public void ExtentOnlyChangesBlockSizeAndPreservesExistingDriveParameters(ushort blockSize)
    {
        var previous = new Acb4000DriveParameters(256, 615, 4, 300, 0xffff, 0x85, 2);
        var bytes = new byte[12];
        (previous with { BlockSize = blockSize }).Write(bytes, includeDriveParameters: false);
        Assert.True(Acb4000DriveParameters.TryDecode(bytes, previous, out var decoded));
        Assert.Equal(previous with { BlockSize = blockSize }, decoded);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(128)]
    [InlineData(255)]
    [InlineData(257)]
    [InlineData(768)]
    [InlineData(2048)]
    [InlineData(0x100100)]
    public void InvalidBlockSizesCannotTruncateIntoAValidSize(int blockSize)
    {
        var bytes = Valid();
        bytes[9] = (byte)(blockSize >> 16);
        bytes[10] = (byte)(blockSize >> 8);
        bytes[11] = (byte)blockSize;
        Assert.False(Acb4000DriveParameters.TryDecode(bytes, Acb4000DriveParameters.Default, out var result));
        Assert.Equal(Acb4000DriveParameters.Default, result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void ReservedHeaderAndDensityBytesMustBeZero(int offset)
    {
        var bytes = Valid();
        bytes[offset] = 1;
        Assert.False(Acb4000DriveParameters.TryDecode(bytes, Acb4000DriveParameters.Default, out var result));
        Assert.Equal(Acb4000DriveParameters.Default, result);
    }

    [Theory]
    [InlineData(3, 7)]
    [InlineData(3, 9)]
    [InlineData(12, 0)]
    [InlineData(12, 2)]
    [InlineData(15, 0)]
    [InlineData(15, 17)]
    [InlineData(21, 3)]
    [InlineData(21, 255)]
    public void DescriptorLengthListCodeHeadsAndStepRateRespectLimits(int offset, byte value)
    {
        var bytes = Valid();
        bytes[offset] = value;
        Assert.False(Acb4000DriveParameters.TryDecode(bytes, Acb4000DriveParameters.Default, out _));
    }

    [Theory]
    [InlineData(1, 1, 0)]
    [InlineData(2048, 16, 2047)]
    public void DocumentedGeometryAndReducedCurrentBoundaryValuesAreAccepted(ushort cylinders, byte heads, ushort reduced)
    {
        var parameters = new Acb4000DriveParameters(1024, cylinders, heads, reduced, 0xffff, 0xff, 2);
        var bytes = new byte[22];
        parameters.Write(bytes);
        Assert.True(Acb4000DriveParameters.TryDecode(bytes, default, out var result));
        Assert.Equal(parameters, result);
    }

    [Theory]
    [InlineData(13, 0)]
    [InlineData(13, 2049)]
    [InlineData(16, 2048)]
    public void CylinderAndReducedCurrentOutOfRangeValuesDoNotModifyPreviousParameters(int offset, ushort value)
    {
        var bytes = Valid();
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(offset), value);
        Assert.False(Acb4000DriveParameters.TryDecode(bytes, Acb4000DriveParameters.Default, out var result));
        Assert.Equal(Acb4000DriveParameters.Default, result);
    }

    [Fact]
    public void EveryTruncatedOptionalListIsRejected()
    {
        var bytes = Valid();
        for (var length = 0; length < 22; length++)
        {
            if (length == 12) continue;
            Assert.False(Acb4000DriveParameters.TryDecode(bytes.AsSpan(0, length), Acb4000DriveParameters.Default, out var result));
            Assert.Equal(Acb4000DriveParameters.Default, result);
        }
    }

    [Theory]
    [InlineData(0, 3000000)]
    [InlineData(1, 28000)]
    [InlineData(2, 12000)]
    public void StepTimingIsPhysicalNanosecondsNotAHostExecutionDuration(byte code, int nanoseconds)
    {
        var parameters = Acb4000DriveParameters.Default with { StepRate = code };
        Assert.Equal(nanoseconds, parameters.StepPulseNanoseconds);
    }

    [Theory]
    [InlineData(0, 305)]
    [InlineData(7, 312)]
    [InlineData(0x80, 0)]
    [InlineData(0x85, -5)]
    public void LandingZoneDisplacementUsesTheSpecifiedSideOfTheDataCylinders(byte zone, int cylinder)
    {
        Assert.Equal(cylinder, (Acb4000DriveParameters.Default with { LandingZone = zone }).LandingCylinder);
    }

    [Fact]
    public void SerializationLeavesBytesOutsideTheParameterListUntouched()
    {
        var bytes = Enumerable.Repeat((byte)0xa5, 33).ToArray();
        Acb4000DriveParameters.Default.Write(bytes);
        Assert.All(bytes.Skip(22), value => Assert.Equal(0xa5, value));
    }

    private static byte[] Valid()
    {
        var result = new byte[22];
        Acb4000DriveParameters.Default.Write(result);
        return result;
    }
}
