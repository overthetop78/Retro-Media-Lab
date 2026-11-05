using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Adaptec.ACB4000.ACB4000;

public sealed partial class Acb4000CommandTests
{
    [Fact]
    public void NativePatchChangesTheWrittenPreambleInsteadOfARebuiltRecord()
    {
        var disk = new Disk();
        var target = CreateTarget(_ => disk, 2_000_000);
        Assert.Equal(0, Run(target, DiagnosticCdb(0x1d, 5), [0x63, 0x30, 0xc9, 1, 0x83]).Status);
        var payload = Enumerable.Range(0, 256).Select(index => (byte)index).ToArray();
        Assert.Equal(0, Run(target, Short(0x0a, count: 1), payload).Status);
        var capture = Assert.Single(disk.TrackWrites);
        Assert.Equal((4 + 256 + 6) * 16, capture.Cells);
        Assert.Equal((ushort)0x4489, capture.Words[4]);
        Assert.Equal(payload, disk.Bytes.AsSpan(0, 256).ToArray());
    }

    [Fact]
    public void NativeWamPatchRemovesThePhysicalAddressMark()
    {
        var disk = new Disk();
        var before = disk.Bytes.ToArray();
        var target = CreateTarget(_ => disk, 2_000_000);
        Assert.Equal(0, Run(target, DiagnosticCdb(0x1d, 5), [0x63, 0x30, 0x7b, 1, 0]).Status);
        Assert.Equal(0, Run(target, Short(0x0a, count: 1), new byte[256]).Status);
        Assert.Equal((ushort)0x44a9, Assert.Single(disk.TrackWrites).Words[9]);
        Assert.Equal(0, disk.Writes);
        Assert.Equal(before, disk.Bytes);
    }

    [Fact]
    public void ShortNativeDataCountIsNotReplacedByACompleteEncodedPayload()
    {
        var disk = new Disk();
        var before = disk.Bytes.ToArray();
        var target = CreateTarget(_ => disk, 2_000_000);
        Assert.Equal(0, Run(target, DiagnosticCdb(0x1d, 5), [0x63, 0x30, 0xc4, 1, 0]).Status);
        Assert.Equal(0, Run(target, Short(0x0a, count: 1), new byte[256]).Status);
        Assert.Equal((9 + 1 + 6) * 16, Assert.Single(disk.TrackWrites).Cells);
        Assert.Equal(0, disk.Writes);
        Assert.Equal(before, disk.Bytes);
    }

    [Theory]
    [InlineData(256)]
    [InlineData(512)]
    [InlineData(1024)]
    public void WriteReplacesOnlyTheDataFieldThroughThePhysicalOutput(ushort size)
    {
        var disk = new Disk(size);
        var before = disk.Bytes.ToArray();
        var target = CreateTarget(_ => disk, 2_000_000);
        var payload = Enumerable.Range(0, size).Select(index => (byte)(index * 7)).ToArray();
        Assert.Equal(0, Run(target, Short(0x0a, address: 1, count: 1), payload).Status);
        Assert.Equal(1, disk.Writes);
        Assert.True(disk.Reads > 0);
        Assert.Equal(payload, disk.Bytes.AsSpan(size, size).ToArray());
        Assert.Equal(before.AsSpan(0, size).ToArray(), disk.Bytes.AsSpan(0, size).ToArray());
        Assert.Equal(before.AsSpan(2 * size).ToArray(), disk.Bytes.AsSpan(2 * size).ToArray());
        var capture = Assert.Single(disk.TrackWrites);
        Assert.Empty(capture.Ids);
        // State 09 emits nine zero bytes; WG drops three bits after ECC,
        // retaining the final three delayed bits of the ENDEC.
        Assert.Equal((9 + size + 6) * 16, capture.Cells);
        Assert.Equal((ushort)0x4489, capture.Words[9]);
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public void WriteDoesNotOpenItsGateForAWrongDefectiveOrCorruptIdentity(bool wrong, bool defective, bool corrupt)
    {
        var disk = new Disk { PhysicalReadAddress = wrong ? 1u : null, DefectiveReadId = defective, CorruptReadId = corrupt };
        var target = CreateTarget(_ => disk, 2_000_000);
        SendBufferedWrite(target);
        for (var tick = 0; tick < 4000; tick++) Tick(target);
        Assert.True(target.ReadGate);
        Assert.False(target.WriteGate);
        Assert.Equal(0, disk.Writes);
        Assert.Empty(disk.TrackWrites);
        target.Reset();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ResetOrWriteFaultInterruptsThePhysicalSegmentBeforeItsDataIsCommitted(bool fault)
    {
        var disk = new Disk();
        var before = disk.Bytes.ToArray();
        var target = CreateTarget(_ => disk, 2_000_000);
        SendBufferedWrite(target);
        for (var tick = 0; tick < 4000 && !target.WriteGate; tick++) Tick(target);
        Assert.True(target.WriteGate);
        for (var tick = 0; tick < 20; tick++) Tick(target);
        if (fault) target.SetWriteFault(true);
        else target.Reset();
        Tick(target);
        Assert.False(target.WriteGate);
        Assert.Equal(0, disk.Writes);
        Assert.Equal(before, disk.Bytes);
        var partial = Assert.Single(disk.TrackWrites);
        Assert.InRange(partial.Cells, 1L, 279L * 16 - 1);
        if (fault) Assert.Equal(2, ReceiveResult(target).Status);
    }

    private static void SendBufferedWrite(Acb4000 target)
    {
        SendCommand(target, Short(0x0a, count: 1));
        for (var index = 0; index < 256; index++)
        {
            WaitRequest(target);
            Transfer(target, (byte)index);
        }
    }
}
