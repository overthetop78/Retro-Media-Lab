using RetroMediaLab.Chips.Controllers.Acorn.Winchester.Scsi.Interfaces;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Adaptec.ACB4000.ACB4000;

public sealed partial class Acb4000CommandTests
{
    [Fact]
    public void ModeSelectChangesOnlyControllerRamAndLeavesTheExistingFormatReadable()
    {
        var first = new Disk();
        var second = new Disk();
        var target = CreateTarget(unit => unit == 0 ? first : second, 2_000_000);
        var baseline = Run(target, Short(0x1a, count: 22)).Data;
        var selected = new Acb4000DriveParameters(512, 615, 4, 300, 0xffff, 0x85, 2);
        Assert.Equal(0, Run(target, Short(0x15, count: 22), EncodeParameters(selected)).Status);
        Assert.Equal(EncodeParameters(selected), Run(target, Short(0x1a, count: 22)).Data);
        Assert.Equal(baseline, Run(target, Short(0x1a, 1, count: 22)).Data);
        var read = Run(target, Short(8, count: 1));
        Assert.Equal(0, read.Status);
        Assert.Equal(first.Bytes[..256], read.Data);
        Assert.Equal(0, first.ParameterWrites);
        Assert.Equal(0, second.ParameterWrites);
        target.Reset();
        Assert.Equal(baseline, Run(target, Short(0x1a, count: 22)).Data);
    }

    [Fact]
    public void ExtentOnlySelectPreservesTheOptionalDriveParametersAlreadyInRam()
    {
        var disk = new Disk();
        var target = CreateTarget(_ => disk, 2_000_000);
        var selected = new Acb4000DriveParameters(256, 615, 4, 300, 0xffff, 0x85, 2);
        Assert.Equal(0, Run(target, Short(0x15, count: 22), EncodeParameters(selected)).Status);
        var next = selected with { BlockSize = 1024 };
        var extent = new byte[12];
        next.Write(extent, includeDriveParameters: false);
        Assert.Equal(0, Run(target, Short(0x15, count: 12), extent).Status);
        Assert.Equal(EncodeParameters(next), Run(target, Short(0x1a, count: 22)).Data);
        Assert.Equal(0, disk.ParameterWrites);
    }

    [Fact]
    public void ReadOnlyDiskDoesNotPreventChangingVolatileControllerParameters()
    {
        var disk = new Disk { IsReadOnly = true };
        var target = CreateTarget(_ => disk, 2_000_000);
        var selected = Acb4000DriveParameters.Default with { StepRate = 2 };
        Assert.Equal(0, Run(target, Short(0x15, count: 22), EncodeParameters(selected)).Status);
        Assert.Equal(EncodeParameters(selected), Run(target, Short(0x1a, count: 22)).Data);
        Assert.Equal(0, disk.ParameterWrites);
        Assert.Equal(0, disk.Writes);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    [InlineData(13)]
    [InlineData(21)]
    public void ModeSelectRejectsShortOrIncompleteDeclaredListsBeforeRequestingTheirData(byte length)
    {
        var disk = new Disk();
        var target = CreateTarget(_ => disk, 2_000_000);
        Assert.Equal(2, Run(target, Short(0x15, count: length)).Status);
        Assert.Equal(0x24, Run(target, Short(3, count: 4)).Data[0]);
        Assert.Equal(0, disk.ParameterWrites);
    }

    [Fact]
    public void InvalidReceivedParametersLeaveBothRamAndStoredParametersUnchanged()
    {
        var disk = new Disk();
        var target = CreateTarget(_ => disk, 2_000_000);
        var before = Run(target, Short(0x1a, count: 22)).Data;
        var invalid = before.ToArray();
        invalid[0] = 1;
        Assert.Equal(2, Run(target, Short(0x15, count: 22), invalid).Status);
        Assert.Equal(0x24, Run(target, Short(3, count: 4)).Data[0]);
        Assert.Equal(before, Run(target, Short(0x1a, count: 22)).Data);
        Assert.Equal(0, disk.ParameterWrites);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(11)]
    public void ModeSenseRequiresAtLeastAnExtent(byte length)
    {
        var target = CreateDiskTarget();
        Assert.Equal(2, Run(target, Short(0x1a, count: length)).Status);
        Assert.Equal(0x24, Run(target, Short(3, count: 4)).Data[0]);
    }

    [Theory]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(21)]
    [InlineData(22)]
    [InlineData(33)]
    [InlineData(255)]
    public void ModeSenseHonoursTheAllocationAndDoesNotLeakBufferResidue(byte length)
    {
        var disk = new Disk();
        var target = CreateTarget(_ => disk, 2_000_000);
        Assert.Equal(0, Run(target, Short(0x13), Enumerable.Repeat((byte)0xa5, 1024).ToArray()).Status);
        var result = Run(target, Short(0x1a, count: length));
        Assert.Equal(0, result.Status);
        Assert.Equal(length, result.Data.Length);
        var parameters = Acb4000DriveParameters.Default with { Cylinders = 2, Heads = 2 };
        Assert.Equal(EncodeParameters(parameters)[..Math.Min(22, (int)length)], result.Data[..Math.Min(22, (int)length)]);
        Assert.All(result.Data.Skip(22), value => Assert.Equal(0, value));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnformattedOrUnreadableFormatRecordsAreNotInventedByModeSelect(bool corrupt)
    {
        var disk = new Disk { Formatted = false, CorruptParameters = corrupt };
        var target = CreateTarget(_ => disk, 2_000_000);
        Assert.Equal(0, Run(target, Short(0)).Status);
        Assert.Equal(2, Run(target, Short(0x1a, count: 22)).Status);
        Assert.Equal(0x1c, Run(target, Short(3, count: 4)).Data[0]);
        Assert.Equal(2, Run(target, Short(8, count: 1)).Status);
        Assert.Equal(0, disk.Reads);
        Assert.Equal(0, Run(target, Short(0x15, count: 22), EncodeParameters(Acb4000DriveParameters.Default)).Status);
        Assert.Equal(0, disk.ParameterWrites);
        Assert.Equal(2, Run(target, Short(0x1a, count: 22)).Status);
    }

    [Fact]
    public void ParameterCacheIsInvalidatedOnResetAndWhenTheBorrowedSupportChanges()
    {
        var first = new Disk();
        var second = new Disk(512);
        IScsiStorage attached = first;
        var target = CreateTarget(_ => attached, 2_000_000);
        Run(target, Short(0x1a, count: 22));
        Run(target, Short(0x1a, count: 22));
        Assert.Equal(1, first.ParameterAcquisitions);
        target.Reset();
        Run(target, Short(0x1a, count: 22));
        Assert.Equal(2, first.ParameterAcquisitions);
        attached = second;
        var result = Run(target, Short(0x1a, count: 22));
        Assert.True(Acb4000DriveParameters.TryDecode(result.Data, default, out var parameters));
        Assert.Equal(512, parameters.BlockSize);
        Assert.Equal(1, second.ParameterAcquisitions);
        Assert.Equal(0, second.ParameterWrites);
    }

    [Theory]
    [InlineData(256)]
    [InlineData(512)]
    [InlineData(1024)]
    public void RecordedBlockSizeControlsReadWriteTransfersWithoutReinterpretingTheContainer(ushort blockSize)
    {
        var disk = new Disk(blockSize);
        var target = CreateTarget(_ => disk, 2_000_000);
        var data = Enumerable.Range(0, 2 * blockSize).Select(index => (byte)(index * 73 + 5)).ToArray();
        Assert.Equal(0, Run(target, Long(0x2a, address: 1, count: 2), data).Status);
        var read = Run(target, Long(0x28, address: 1, count: 2));
        Assert.Equal(0, read.Status);
        Assert.Equal(data, read.Data);
        // Two identity searches before physical writes, followed by the two requested reads.
        Assert.Equal(4, disk.Reads);
        Assert.Equal(2, disk.Writes);
        Assert.Equal(0, disk.ParameterWrites);
    }

    private static byte[] EncodeParameters(Acb4000DriveParameters parameters)
    {
        var data = new byte[22];
        parameters.Write(data);
        return data;
    }
}
