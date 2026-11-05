using System.Buffers.Binary;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;

namespace RetroMediaLab.Chips.Tests.Controllers.Adaptec.ACB4000.ACB4000;

public sealed partial class Acb4000CommandTests
{
    [Theory]
    [InlineData(256, false)]
    [InlineData(512, false)]
    [InlineData(1024, false)]
    [InlineData(256, true)]
    [InlineData(512, true)]
    [InlineData(1024, true)]
    public void SearchReturnsTheFirstMatchingRecordWithoutTransferringDiskData(ushort size, bool invert)
    {
        var disk = new Disk(size);
        disk.Bytes.AsSpan().Fill(0x11);
        disk.Bytes.AsSpan(size, size).Fill(0x22);
        var before = disk.Bytes.ToArray();
        var target = CreateTarget(_ => disk, 2_000_000);
        var cdb = SearchCdb(count: 3, invert: invert);
        var argument = SearchArgument(size, 3, invert ? (byte)0x11 : (byte)0x22);
        var result = Run(target, cdb, argument);
        Assert.Equal(4, result.Status);
        Assert.Empty(result.Data);
        var sense = Run(target, Short((byte)Acb4000CommandCode.RequestSense, count: 4)).Data;
        Assert.Equal(0x80, sense[0]);
        Assert.Equal(1, sense[3]);
        Assert.Equal(before, disk.Bytes);
        Assert.Equal(0, disk.Writes);
        Assert.Equal(0, Run(target, Short((byte)Acb4000CommandCode.RequestSense, count: 4)).Data[0]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void NoMatchStopsAtTheArgumentRecordCountAndLeavesSenseInvalid(int unit)
    {
        var disk = new Disk();
        disk.Bytes.AsSpan().Fill(0x11);
        var target = CreateTarget(_ => disk, 2_000_000);
        var result = Run(target, SearchCdb(unit: unit, count: 3), SearchArgument(256, 2, 0x22));
        Assert.Equal(0, result.Status);
        Assert.Empty(result.Data);
        Assert.Equal(2, disk.Reads);
        var sense = Run(target, Short((byte)Acb4000CommandCode.RequestSense, unit, count: 4)).Data;
        Assert.Equal(0, sense[0]);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(4, 1)]
    [InlineData(8, 0)]
    [InlineData(8, 3)]
    [InlineData(12, 1)]
    [InlineData(14, 1)]
    [InlineData(18, 1)]
    public void InvalidSearchHeadersAreRejectedBeforeRequestingThePattern(int offset, uint value)
    {
        var target = CreateDiskTarget();
        var header = SearchArgument(256, 2, 0)[..20];
        if (offset is 12 or 18) BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(offset), (ushort)value);
        else BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(offset), value);
        Assert.Equal(2, Run(target, SearchCdb(count: 2), header).Status);
        Assert.Equal(0x24, Run(target, Short((byte)Acb4000CommandCode.RequestSense, count: 4)).Data[0]);
    }

    [Fact]
    public void ZeroRecordSizeUsesThePhysicalBlockSize()
    {
        var disk = new Disk();
        disk.Bytes.AsSpan().Fill(0x44);
        var target = CreateTarget(_ => disk, 2_000_000);
        var argument = SearchArgument(256, 1, 0x44);
        argument.AsSpan(0, 4).Clear();
        Assert.Equal(4, Run(target, SearchCdb(count: 1), argument).Status);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    public void ReservedCdbFieldsAreNotAcceptedAsAnAlternativeSearchMode(int offset)
    {
        var target = CreateDiskTarget();
        var cdb = SearchCdb(count: 1);
        cdb[offset] = 1;
        Assert.Equal(2, Run(target, cdb).Status);
        Assert.Equal(0x24, Run(target, Short((byte)Acb4000CommandCode.RequestSense, count: 4)).Data[0]);
    }

    [Fact]
    public void SearchReportsDataCheckAfterThreeBadReadsInsteadOfComparingUncheckedData()
    {
        var disk = new Disk { BadReads = 3 };
        disk.Bytes.AsSpan().Fill(0x44);
        var target = CreateTarget(_ => disk, 2_000_000);
        var result = Run(target, SearchCdb(count: 1), SearchArgument(256, 1, 0x44));
        Assert.Equal(2, result.Status);
        Assert.Empty(result.Data);
        Assert.Equal(3, disk.Reads);
        Assert.Equal(0x91, Run(target, Short((byte)Acb4000CommandCode.RequestSense, count: 4)).Data[0]);
    }

    [Fact]
    public void SearchCanRecoverOnTheThirdReadWithoutUsingBurstCorrection()
    {
        var disk = new Disk { BadReads = 2 };
        disk.Bytes.AsSpan().Fill(0x44);
        var target = CreateTarget(_ => disk, 2_000_000);
        Assert.Equal(4, Run(target, SearchCdb(count: 1), SearchArgument(256, 1, 0x44)).Status);
        Assert.Equal(3, disk.Reads);
    }

    [Fact]
    public void ResetAbandonsAPartiallyReceivedPattern()
    {
        var target = CreateDiskTarget();
        SendCommand(target, SearchCdb(count: 1));
        var argument = SearchArgument(256, 1, 0x44);
        for (var index = 0; index < 25; index++)
        {
            WaitRequest(target);
            Transfer(target, argument[index]);
        }
        target.Reset();
        Assert.False(target.Bus.Busy);
        Assert.Equal(0, Run(target, Short((byte)Acb4000CommandCode.TestUnitReady)).Status);
    }

    [Fact]
    public void ReplacingTheDriveWhileReceivingTheArgumentDoesNotSearchTheReplacement()
    {
        var first = new Disk();
        var active = first;
        var target = CreateTarget(_ => active, 2_000_000);
        SendCommand(target, SearchCdb(count: 1));
        active = new Disk();
        var result = ReceiveResult(target, SearchArgument(256, 1, 0x44)[..20]);
        Assert.Equal(2, result.Status);
        Assert.Equal(0, first.Reads);
        Assert.Equal(0, active.Reads);
    }

    private static byte[] SearchCdb(int unit = 0, ushort count = 1, bool invert = false)
    {
        var cdb = Long((byte)Acb4000CommandCode.SearchDataEqual, unit, count: count);
        if (invert) cdb[1] |= 0x10;
        return cdb;
    }

    private static byte[] SearchArgument(ushort size, uint records, byte fill)
    {
        var argument = new byte[20 + size];
        BinaryPrimitives.WriteUInt32BigEndian(argument, size);
        BinaryPrimitives.WriteUInt32BigEndian(argument.AsSpan(8), records);
        BinaryPrimitives.WriteUInt16BigEndian(argument.AsSpan(12), (ushort)(size + 6));
        BinaryPrimitives.WriteUInt16BigEndian(argument.AsSpan(18), size);
        argument.AsSpan(20).Fill(fill);
        return argument;
    }
}
