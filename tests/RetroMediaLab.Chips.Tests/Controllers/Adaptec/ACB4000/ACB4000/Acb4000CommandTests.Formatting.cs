using System.Buffers.Binary;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Adaptec.ACB4000.ACB4000;

public sealed partial class Acb4000CommandTests
{
    [Fact]
    public void FormatUsesThePatchedIdPreambleAndSuppressesBufferTransfers()
    {
        var disk = new Disk();
        var target = CreateTarget(_ => disk, 2_000_000);
        var selected = Acb4000DriveParameters.Default with { Cylinders = 1, Heads = 1 };
        Assert.Equal(0, Run(target, Short(0x15, count: 22), EncodeParameters(selected)).Status);
        Assert.Equal(0, Run(target, DiagnosticCdb(0x1d, 5), [0x63, 0x30, 0xce, 1, 0x87]).Status);
        Assert.Equal(0, Run(target, [4, 6, 0xe5, 0, 2, 0]).Status);
        var track = Assert.Single(disk.TrackWrites);
        Assert.Equal((ushort)0x4489, track.Words[9 + 8]);
        Assert.Equal(33, track.Ids.Count);
        Assert.Equal(33, disk.Writes);
        Assert.Equal(0, target.BufferController.ReadPointer);
        Assert.All(disk.Bytes, value => Assert.Equal(0xe5, value));
    }

    [Theory]
    [InlineData(256, 1, 32)]
    [InlineData(256, 2, 33)]
    [InlineData(512, 1, 17)]
    [InlineData(512, 0, 18)]
    [InlineData(1024, 1, 9)]
    [InlineData(1024, 2, 9)]
    public void FormatFillsTheSelectedGeometryAndRecordsParametersAfterTheLastBlock(ushort size, byte interleave, int sectors)
    {
        var disk = new Disk();
        var target = CreateTarget(_ => disk, 2_000_000);
        var selected = Acb4000DriveParameters.Default with { Cylinders = 1, Heads = 2, BlockSize = size };
        Assert.Equal(0, Run(target, Short(0x15, count: 22), EncodeParameters(selected)).Status);
        Assert.Equal(0, disk.ParameterWrites);
        Assert.Equal(0, Run(target, [4, 6, 0xe5, 0, interleave, 0]).Status);
        Assert.Equal(1, disk.Preparations);
        Assert.Equal(2 * sectors, disk.Writes);
        Assert.Equal(new[] { (0, 0), (0, 1) }, disk.TrackWrites.Select(track => (track.Cylinder, track.Head)));
        Assert.All(disk.TrackWrites, track => Assert.True(track.Windows > 1));
        Assert.Equal(1, disk.ParameterWrites);
        Assert.Equal(2 * sectors * size, disk.Bytes.Length);
        Assert.All(disk.Bytes, value => Assert.Equal(0xe5, value));
        target.Reset();
        Assert.Equal(EncodeParameters(selected), Run(target, Short(0x1a, count: 22)).Data);
        Assert.Equal(Enumerable.Repeat((byte)0xe5, size), Run(target, Short(8, count: 1)).Data);
    }

    [Fact]
    public void FormatUsesDefaultFillAndTheExistingParametersWithoutModeSelect()
    {
        var disk = new Disk(512);
        var target = CreateTarget(_ => disk, 2_000_000);
        Assert.Equal(0, Run(target, [4, 0, 0, 0, 0, 0]).Status);
        Assert.Equal(512, disk.SectorSize);
        Assert.Equal(18, disk.SectorsPerTrack);
        Assert.All(disk.Bytes, value => Assert.Equal(0x6c, value));
    }

    [Fact]
    public void UnformattedDiskRequiresModeSelectBeforeFormat()
    {
        var disk = new Disk { Formatted = false };
        var target = CreateTarget(_ => disk, 2_000_000);
        Assert.Equal(2, Run(target, [4, 0, 0, 0, 0, 0]).Status);
        Assert.Equal(0, disk.Preparations);
        var selected = Acb4000DriveParameters.Default with { Cylinders = 1, Heads = 1 };
        Assert.Equal(0, Run(target, Short(0x15, count: 22), EncodeParameters(selected)).Status);
        Assert.Equal(0, Run(target, [4, 0, 0, 0, 0, 0]).Status);
        Assert.Equal(33 * 256, disk.Bytes.Length);
    }

    [Fact]
    public void EmptyDefectListIsReceivedBeforeAnyFormatWrite()
    {
        var disk = new Disk();
        var target = CreateTarget(_ => disk, 2_000_000);
        SendCommand(target, [4, 0x1c, 0, 0, 2, 0]);
        for (var i = 0; i < 3; i++)
        {
            WaitRequest(target);
            Transfer(target, 0);
            Assert.Equal(0, disk.Preparations);
        }
        WaitRequest(target);
        Transfer(target, 0);
        Assert.Equal(1, disk.Preparations);
        Assert.Equal(0, disk.Writes);
        WaitRequest(target);
        Assert.Equal(Acb4000BusPhase.Status, target.Bus.Phase);
        Assert.Equal(0, target.Bus.OutputData);
        Transfer(target, 0);
        WaitRequest(target);
        Transfer(target, 0);
        Assert.False(target.Bus.Busy);
        Assert.Equal(1, disk.ParameterWrites);
    }

    [Fact]
    public void ResetStopsWritesAndDoesNotRecordACompletedFormat()
    {
        var disk = new Disk();
        var target = CreateTarget(_ => disk, 2_000_000);
        SendCommand(target, [4, 0, 0, 0, 2, 0]);
        for (var tick = 0; tick < 100_000 && disk.Writes < 7; tick++) Tick(target);
        Assert.Equal(7, disk.Writes);
        for (var tick = 0; tick < 1_000 && !target.WriteGate; tick++) Tick(target);
        Assert.True(target.WriteGate);
        var partialCells = Assert.Single(disk.TrackWrites).Cells;
        target.Reset();
        for (var tick = 0; tick < 50; tick++) Tick(target);
        Assert.Equal(7, disk.Writes);
        Assert.False(target.WriteGate);
        Assert.Equal(partialCells, Assert.Single(disk.TrackWrites).Cells);
        Assert.Equal(0, disk.ParameterWrites);
        Assert.False(target.Bus.Busy);
        Assert.Equal(2, Run(target, Short(8, count: 1)).Status);
        Assert.Equal(0x1c, Run(target, Short(3, count: 4)).Data[0]);
    }

    [Fact]
    public void InvalidOptionsAndReadOnlyDiskDoNotModifyAnyByte()
    {
        foreach (var readOnly in new[] { false, true })
        {
            var disk = new Disk { IsReadOnly = readOnly };
            var bytes = disk.Bytes.ToArray();
            var target = CreateTarget(_ => disk, 2_000_000);
            Assert.Equal(2, Run(target, [4, 0, 0, 0, readOnly ? (byte)2 : (byte)33, 0]).Status);
            Assert.Equal(0, disk.Preparations);
            Assert.Equal(0, disk.ParameterWrites);
            Assert.Equal(bytes, disk.Bytes);
        }
    }

    [Fact]
    public void MalformedDefectsAreRejectedBeforePreparation()
    {
        foreach (var bytes in new byte[][] { [1, 0, 0, 0], [0, 0, 0, 1], [0, 0, 0, 8, 0, 0, 0, 0, 0, 0, 0, 1] })
        {
            var disk = new Disk();
            var target = CreateTarget(_ => disk, 2_000_000);
            Assert.Equal(2, Run(target, [4, 0x1c, 0, 0, 2, 0], bytes).Status);
            Assert.Equal(0, disk.Preparations);
            Assert.Equal(0, disk.Writes);
        }
    }

    [Fact]
    public void NonemptyDefectsProducePhysicalBadSlotsAndCompactLogicalAddresses()
    {
        var disk = new Disk();
        var target = CreateTarget(_ => disk, 2_000_000);
        // Two flaws within slot zero remove just one block; the unused tail removes none.
        var flaws = FormatDefectList((1, 0, 0), (1, 0, 1), (1, 0, 10416));
        Assert.Equal(0, Run(target, [4, 0x1c, 0, 0, 2, 0], flaws).Status);
        Assert.Equal(4, disk.TrackWrites.Count);
        Assert.Equal(131, disk.Writes);
        Assert.Equal(0, target.RemainingBlocks);
        Assert.Equal(1, disk.ParameterWrites);
        var defectiveTrack = disk.TrackWrites.Single(track => track.Cylinder == 1 && track.Head == 0);
        Assert.Equal(33, defectiveTrack.Ids.Count);
        Assert.True(defectiveTrack.Ids[0].IsDefective);
        Assert.Equal(66U, defectiveTrack.Ids[0].LogicalAddress);
        Assert.All(defectiveTrack.Ids, id => Assert.Equal(1, id.DefectCount));
        Assert.Equal(Enumerable.Range(66, 32).Select(value => (uint)value),
            defectiveTrack.Ids.Where(id => !id.IsDefective).Select(id => id.LogicalAddress).Order());
        var nextTrack = disk.TrackWrites.Single(track => track.Cylinder == 1 && track.Head == 1);
        Assert.Equal(98U, nextTrack.Ids.Min(id => id.LogicalAddress));
        Assert.All(nextTrack.Ids, id => Assert.Equal(1, id.DefectCount));
    }

    [Fact]
    public void ACompletelyDefectiveTrackIsRejectedBeforeChangingTheDrive()
    {
        var disk = new Disk();
        var original = disk.Bytes.ToArray();
        var target = CreateTarget(_ => disk, 2_000_000);
        var flaws = FormatDefectList(Enumerable.Range(0, 33).Select(slot => (1, (byte)0, (uint)(slot * 310))).ToArray());
        Assert.Equal(2, Run(target, [4, 0x1c, 0, 0, 2, 0], flaws).Status);
        Assert.Equal(0, disk.Preparations);
        Assert.Empty(disk.TrackWrites);
        Assert.Equal(original, disk.Bytes);
    }

    [Fact]
    public void FormatWaitsForIndexWithoutWritingOrRecordingSuccess()
    {
        var disk = new Disk();
        var target = CreateTarget(_ => disk, 2_000_000);
        Assert.Equal(0, Run(target, Short(0x1a, count: 22)).Status);
        disk.SuppressIndex = true;
        SendCommand(target, [4, 0, 0, 0, 2, 0]);
        for (var tick = 0; tick < 1000; tick++) Tick(target);
        Assert.True(target.Bus.Busy);
        Assert.False(target.Bus.Request);
        Assert.False(target.WriteGate);
        Assert.Empty(disk.TrackWrites);
        Assert.Equal(0, disk.ParameterWrites);
        target.Reset();
        Assert.False(target.WriteGate);
    }

    [Fact]
    public void WriteFaultInputSurvivesControllerResetUntilTheDriveReleasesIt()
    {
        var disk = new Disk();
        var target = CreateTarget(_ => disk, 2_000_000);
        target.SetWriteFault(true);
        Assert.Equal(2, Run(target, [4, 0, 0, 0, 2, 0]).Status);
        Assert.Equal(0, disk.Preparations);
        target.Reset();
        Assert.Equal(2, Run(target, Short(0x0a, count: 1)).Status);
        target.SetWriteFault(false);
        Assert.Equal(0, Run(target, Short(0x0a, count: 1), new byte[256]).Status);
    }

    private static byte[] FormatDefectList(params (int Cylinder, byte Head, uint Position)[] entries)
    {
        var bytes = new byte[4 + entries.Length * 8];
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(2), (ushort)(entries.Length * 8));
        for (var index = 0; index < entries.Length; index++)
        {
            var entry = bytes.AsSpan(4 + index * 8, 8);
            BinaryPrimitives.WriteUInt32BigEndian(entry, (uint)(entries[index].Cylinder << 8) | entries[index].Head);
            BinaryPrimitives.WriteUInt32BigEndian(entry[4..], entries[index].Position);
        }
        return bytes;
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void BackendFailureNeverCommitsSuccessfulFormatParameters(bool failPreparation, bool failParameters)
    {
        var disk = new Disk
        {
            FailPreparation = failPreparation,
            FailParameterWrite = failParameters,
            FailWriteAfter = !failPreparation && !failParameters ? 3 : null
        };
        var target = CreateTarget(_ => disk, 2_000_000);
        Assert.Equal(2, Run(target, [4, 0, 0, 0, 2, 0]).Status);
        Assert.Equal(0, disk.ParameterWrites);
        Assert.Equal(3, Run(target, Short(3, count: 4)).Data[0] & 0x7f);
        if (!failPreparation)
        {
            target.Reset();
            Assert.Equal(2, Run(target, Short(8, count: 1)).Status);
        }
    }
}
