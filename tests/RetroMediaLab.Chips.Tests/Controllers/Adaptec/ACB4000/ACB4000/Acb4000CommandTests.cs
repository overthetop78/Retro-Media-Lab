using System.Buffers.Binary;
using RetroMediaLab.Chips.Controllers.Acorn.Winchester.Scsi.Interfaces;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Adaptec.ACB4000.ACB4000;

/// <summary>Commandes à travers les vrais cycles REQ/ACK du port cible, avec supports numériques empruntés.</summary>
public sealed partial class Acb4000CommandTests
{
    [Fact]
    public void UnitReadyReportsExternalWriteFaultUntilTheDriveReleasesItIncludingAfterReset()
    {
        var disk = new Disk();
        var target = CreateTarget(_ => disk, 2_000_000);
        target.SetWriteFault(true);
        Assert.Equal(2, Run(target, Short(0)).Status);
        Assert.Equal(3, Run(target, Short(3, count: 4)).Data[0]);
        target.Reset();
        Assert.Equal(2, Run(target, Short(0)).Status);
        Assert.Equal(3, Run(target, Short(3, count: 4)).Data[0]);
        target.SetWriteFault(false);
        Assert.Equal(0, Run(target, Short(0)).Status);
        Assert.Equal(new byte[4], Run(target, Short(3, count: 4)).Data);
    }

    [Fact]
    public void ReadOnlyMediaDoesNotInventADriveWriteFaultDuringUnitReady()
    {
        var target = CreateTarget(_ => new Disk { IsReadOnly = true }, 2_000_000);
        Assert.Equal(0, Run(target, Short(0)).Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void BothPhysicalPortsReturnStatusWithoutLogicalUnitBits(int unit)
    {
        var disk = new Disk();
        var target = CreateTarget(_ => disk, 2_000_000);
        var result = Run(target, Short(0, unit));
        Assert.Equal(0, result.Status);
        Assert.Empty(result.Data);
        Assert.False(target.Bus.Busy);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void OtherLunsDoNotCreateFictitiousPorts(int unit)
    {
        var target = CreateTarget(index => { Assert.InRange(index, 0, 1); return new Disk(); }, 2_000_000);
        Assert.Equal(2, Run(target, Short(0, unit)).Status);
        var sense = Run(target, Short(3, unit, count: 4));
        Assert.Equal(0, sense.Status);
        Assert.Equal(new byte[] { 0x25, (byte)(unit << 5), 0, 0 }, sense.Data);
    }

    [Fact]
    public void MissingDriveReturnsNotReadyRatherThanCompletingSuccessfully()
    {
        var target = CreateTarget(_ => null, 2_000_000);
        Assert.Equal(2, Run(target, Short(0)).Status);
        Assert.Equal(4, Run(target, Short(3, count: 4)).Data[0]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(255)]
    public void SenseAllocationsBelowFourReturnFourAndNeverCheck(int allocation)
    {
        var target = CreateTarget(_ => null, 2_000_000);
        Run(target, Short(0));
        var command = Short(3, count: allocation);
        command[5] = 0xff;
        var sense = Run(target, command);
        Assert.Equal(0, sense.Status);
        Assert.Equal(Math.Max(4, allocation), sense.Data.Length);
        Assert.Equal(4, sense.Data[0]);
        Assert.All(sense.Data.Skip(4), value => Assert.Equal(0, value));
        Assert.Equal(0, Run(target, Short(3, count: 4)).Data[0]);
    }

    [Fact]
    public void SubsequentNonSenseCommandClearsThePreviousCheck()
    {
        var disk = new Disk();
        var target = CreateTarget(_ => disk, 2_000_000);
        Assert.Equal(2, Run(target, Short(8, address: 132, count: 1)).Status);
        Assert.Equal(0, Run(target, Short(0)).Status);
        Assert.Equal(new byte[4], Run(target, Short(3, count: 4)).Data);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReadAndWriteTransferAcrossBlockBoundaries(bool extended)
    {
        var disk = new Disk();
        var target = CreateTarget(_ => disk, 2_000_000);
        var original = disk.Bytes.ToArray();
        var data = Enumerable.Range(0, 512).Select(index => (byte)(index * 73 + 5)).ToArray();
        var write = extended ? Long(0x2a, 1, 3, 2) : Short(0x0a, 1, 3, 2);
        Assert.Equal(0, Run(target, write, data).Status);
        Assert.Equal(data, disk.Bytes.AsSpan(3 * 256, 512).ToArray());
        Assert.Equal(original[..(3 * 256)], disk.Bytes[..(3 * 256)]);
        Assert.Equal(original[(5 * 256)..], disk.Bytes[(5 * 256)..]);
        var read = extended ? Long(0x28, 1, 3, 2) : Short(8, 1, 3, 2);
        var result = Run(target, read);
        Assert.Equal(0, result.Status);
        Assert.Equal(data, result.Data);
        Assert.Equal(2, disk.Writes);
    }

    [Theory]
    [InlineData(false, 256)]
    [InlineData(true, 65536)]
    public void ZeroBlockCountMeansTheDocumentedMaximumWithoutAllocatingTheWholeTransfer(bool extended, int maximum)
    {
        var target = CreateDiskTarget();
        SendCommand(target, extended ? Long(0x28, count: 0) : Short(8, count: 0));
        Assert.Equal(maximum, target.RemainingBlocks);
        for (var tick = 0; tick < 8 && !target.ReadGate; tick++) Tick(target);
        Assert.True(target.ReadGate);
        WaitRequest(target);
        Assert.Equal(Acb4000BusPhase.DataIn, target.Bus.Phase);
        target.Reset();
        Assert.False(target.Bus.Busy);
    }

    [Fact]
    public void UncorrectableDataIsSentBeforeCheckAndSenseRetainsItsAddress()
    {
        var disk = new Disk { BadReads = int.MaxValue };
        var target = CreateTarget(_ => disk, 2_000_000);
        var result = Run(target, Short(8, 1, 2, 1));
        Assert.Equal(2, result.Status);
        Assert.Equal(disk.Bytes.AsSpan(512, 256).ToArray(), result.Data);
        Assert.Equal(6, disk.Reads);
        var sense = Run(target, Short(3, 1, count: 4));
        Assert.Equal(new byte[] { 0x91, 0x20, 0, 2 }, sense.Data);
    }

    [Fact]
    public void SuccessfulRereadAvoidsEndingCheck()
    {
        var disk = new Disk { BadReads = 2 };
        var target = CreateTarget(_ => disk, 2_000_000);
        var result = Run(target, Short(8, count: 1));
        Assert.Equal(0, result.Status);
        Assert.Equal(3, disk.Reads);
        Assert.Equal(disk.Bytes[..256], result.Data);
    }

    [Fact]
    public void TransferReachesLastValidBlockBeforeAddressFailure()
    {
        var disk = new Disk();
        var target = CreateTarget(_ => disk, 2_000_000);
        var result = Run(target, Short(8, address: 131, count: 2));
        Assert.Equal(2, result.Status);
        Assert.Equal(disk.Bytes[(131 * 256)..], result.Data);
        Assert.Equal(new byte[] { 0xa1, 0, 0, 132 }, Run(target, Short(3, count: 4)).Data);
    }

    [Fact]
    public void WriteAndVerifyWritesAllBlocksBeforeEccVerification()
    {
        var disk = new Disk();
        var target = CreateTarget(_ => disk, 2_000_000);
        Assert.Equal(0, Run(target, Long(0x2e, address: 3, count: 2), new byte[512]).Status);
        Assert.Equal(new[] { "R3", "W3", "R4", "W4", "R3", "R4" }, disk.Transfers);
    }

    [Fact]
    public void VerifyChecksExistingDataWithoutRequestingHostBytesOrRetryingAnError()
    {
        var disk = new Disk { BadReads = int.MaxValue };
        var target = CreateTarget(_ => disk, 2_000_000);
        var result = Run(target, Long(0x2f, address: 5, count: 2));
        Assert.Equal(2, result.Status);
        Assert.Empty(result.Data);
        Assert.Equal(1, disk.Reads);
        Assert.Equal(0x99, Run(target, Short(3, count: 4)).Data[0]);
    }

    [Fact]
    public void BufferDiagnosticsTransferTheEntireOneKilobyteWithoutRequiringADrive()
    {
        var target = CreateTarget(_ => null, 2_000_000);
        var data = Enumerable.Range(0, 1024).Select(index => (byte)(index * 17)).ToArray();
        Assert.Equal(0, Run(target, Short(0x13), data).Status);
        Assert.Equal(data, Run(target, Short(0x14)).Data);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CapacityIncludesBlockSizeAndTheRequestedBoundary(bool partialMedium)
    {
        var target = CreateDiskTarget();
        var cdb = Long(0x25);
        cdb[8] = partialMedium ? (byte)1 : (byte)0;
        var result = Run(target, cdb);
        Assert.Equal(0, result.Status);
        Assert.Equal(8, result.Data.Length);
        Assert.Equal(partialMedium ? 66u : 131u, BinaryPrimitives.ReadUInt32BigEndian(result.Data));
        Assert.Equal(256u, BinaryPrimitives.ReadUInt32BigEndian(result.Data.AsSpan(4)));
    }

    [Fact]
    public void ReadOnlyDriveCannotReceiveDataForAWrite()
    {
        var disk = new Disk { IsReadOnly = true };
        var target = CreateTarget(_ => disk, 2_000_000);
        Assert.Equal(2, Run(target, Short(0x0a, count: 1)).Status);
        Assert.Equal(0, disk.Writes);
        Assert.Equal(0x83, Run(target, Short(3, count: 4)).Data[0]);
    }

    [Theory]
    [InlineData(0x12)]
    [InlineData(0x1e)]
    [InlineData(0x35)]
    public void GenericScsiCommandsAreNotInventedForThisController(byte opcode)
    {
        var target = CreateDiskTarget();
        var cdb = opcode == 0x35 ? Long(opcode) : Short(opcode);
        Assert.Equal(2, Run(target, cdb).Status);
        Assert.Equal(0x20, Run(target, Short(3, count: 4)).Data[0]);
    }

    [Fact]
    public void ResetAbortsAnIncompleteWriteWithoutPublishingItsPartialBuffer()
    {
        var disk = new Disk();
        var original = disk.Bytes.ToArray();
        var target = CreateTarget(_ => disk, 2_000_000);
        SendCommand(target, Short(0x0a, count: 1));
        for (var index = 0; index < 100; index++)
        {
            WaitRequest(target);
            Transfer(target, 0xa5);
        }
        target.Reset();
        Assert.Equal(original, disk.Bytes);
        Assert.Equal(0, disk.Writes);
        Assert.Equal(0, Run(target, Short(0)).Status);
    }

    [Fact]
    public void ReservedControlAndLongCommandBitsAreRejected()
    {
        var target = CreateDiskTarget();
        var control = Short(0);
        control[5] = 1;
        Assert.Equal(2, Run(target, control).Status);
        Assert.Equal(0x24, Run(target, Short(3, count: 4)).Data[0]);
        var read = Long(0x28, count: 1);
        read[6] = 1;
        Assert.Equal(2, Run(target, read).Status);
        Assert.Equal(0x24, Run(target, Short(3, count: 4)).Data[0]);
    }

}
