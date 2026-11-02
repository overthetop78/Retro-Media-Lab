using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Hd63463.Services;
using System.Buffers.Binary;
using System.IO;
using RetroMediaLab.Emulation;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Interfaces;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;

namespace RetroMediaLab.Tests.Emulation;

/// <summary>Séquences du port 16 bits, sans ROM, fichier ou processus externe.</summary>
public sealed partial class AcornEmuHd63463Tests
{
    [Fact]
    public void SpecifyDriveCheckAndSeekUseParameterByteOrderAndKeepUnitsIndependent()
    {
        using var first = new Disk();
        using var second = new Disk(0x40);
        var controller = Controller(unit => unit == 0 ? first : second);
        Issue(controller, 0x28, [1, 0]);
        Assert.Equal(0, controller.Status);
        Assert.Equal(new byte[] { 0, 0, 1, 0, 0xe0, 0 }, Results(controller, 6));
        Issue(controller, 0xc0, [2, 0, 0, 2]);
        Assert.True(controller.Busy);
        controller.Advance(10000);
        Assert.Equal(0x70, controller.Status);
        Assert.True(controller.Irq);
        Assert.Equal(new byte[] { 0, 0, 2, 6 }, Results(controller, 4));
        Assert.Equal(1, controller.SeekCount(1));
        Assert.Equal(0, controller.SeekCount(0));
        Issue(controller, 0x28, [2, 0]);
        Assert.Equal(new byte[] { 0, 0, 2, 0, 0xc0, 0 }, Results(controller, 6));
        Issue(controller, 0x28, [0, 0]);
        Assert.False(controller.Active(1));
    }

    [Fact]
    public void MultiSectorReadCrossesHeadBoundaryAndReportsNextChsAndRemainingCount()
    {
        using var disk = new Disk();
        var controller = Controller(_ => disk);
        Issue(controller, 0x40, Transfer(1, 0, 0, 3, 2));
        var data = Read(controller);
        Assert.Equal(disk.Block(3).Concat(disk.Block(4)), data);
        Assert.Equal(0x60, controller.Status);
        Assert.True(controller.Irq);
        Assert.Equal(new byte[] { 0, 0, 1, 1, 0, 0, 1, 1, 0, 0 }, Results(controller, 10));
        Assert.Equal(2, controller.ReadBlocks(0));
        controller.WriteCommand(8);
        controller.Advance(5);
        Assert.False(controller.Irq);
        Assert.Equal(0, controller.Status);
    }

    [Theory]
    [InlineData(256)]
    [InlineData(512)]
    public void WritesPreserveOtherContainerBytesAndOnlyModifyTheSelectedUnit(int sectorSize)
    {
        using var first = new Disk(0x10, sectorSize);
        using var second = new Disk(0x80, sectorSize);
        var controller = Controller(unit => unit == 0 ? first : second);
        var untouched = second.Block(0).ToArray();
        var originalFirst = first.Block(1).ToArray();
        var replacement = Enumerable.Repeat((byte)0x79, 256).ToArray();
        Issue(controller, 0x87, Transfer(2, 0, 0, 1, 1));
        Write(controller, replacement);
        Assert.Equal(replacement, second.Block(1));
        Assert.Equal(untouched, second.Block(0));
        Assert.Equal(originalFirst, first.Block(1));
        Assert.Equal(1, controller.WrittenBlocks(1));
        Issue(controller, 0x40, Transfer(2, 0, 0, 1, 1));
        Assert.Equal(replacement, Read(controller));
    }

    [Theory]
    [InlineData(0, 0x10)] // Pas de Specify.
    [InlineData(1, 0x20)] // Lecteur absent.
    [InlineData(2, 0x3c)] // Tête hors géométrie.
    [InlineData(3, 0x58)] // Identifiant secteur introuvable.
    [InlineData(4, 0x64)] // Protection d'écriture.
    [InlineData(5, 0x44)] // CRC du conteneur.
    public void PhysicalErrorsReturnStatusAndAResultBlockInsteadOfThrowing(int scenario, int error)
    {
        using var disk = new Disk { IsReadOnly = scenario == 4, Bad = scenario == 5 };
        var controller = scenario == 0 ? new Hd63463(_ => disk, 1000000)
            : Controller(_ => scenario == 1 ? null : disk);
        Issue(controller, scenario == 4 ? (byte)0x87 : (byte)0x40,
            Transfer(1, 0, scenario == 2 ? 2 : 0, scenario == 3 ? 4 : 0, 1));
        WaitIdle(controller);
        Assert.False(controller.Busy);
        Assert.False(controller.Drq);
        Assert.True(controller.Irq);
        Assert.Equal(0x64, controller.Status);
        Assert.Equal((ushort)error, controller.ReadParameter());
    }

    [Fact]
    public void AbortCancelsPendingWriteAndResetDoesNotCloseBorrowedImages()
    {
        using var disk = new Disk();
        var controller = Controller(_ => disk);
        Issue(controller, 0x87, Transfer(1, 0, 0, 0, 1));
        controller.Advance(10000);
        Assert.True(controller.Drq);
        controller.WriteDma(0xaaaa);
        controller.WriteCommand(0xf7); // Toute la plage F0-FF est Abort.
        controller.Advance(38);
        Assert.False(controller.Busy);
        Assert.False(controller.Drq);
        Assert.Equal(4, controller.ReadParameter());
        controller.WriteDma(0xffff);
        controller.Advance(100000);
        Assert.False(disk.HasChanges);
        controller.Reset();
        Assert.False(controller.Irq);
        Assert.False(controller.Active(0));
        Assert.False(disk.Disposed);
    }

    [Fact]
    public void BusyRejectsAnotherCommandAndMaskedCompletionDoesNotRaiseIrq()
    {
        using var disk = new Disk();
        var controller = Controller(_ => disk, 0xb0); // CED et SED masqués, DMA activé.
        Issue(controller, 0x40, Transfer(1, 0, 0, 1, 1));
        controller.WriteCommand(0xc8);
        Assert.Equal(disk.Block(1), Read(controller));
        Assert.Equal(0x60, controller.Status);
        Assert.False(controller.Irq);
    }

    [Fact]
    public void VerifyAndCompareReadTheMediaAndReportMismatchingData()
    {
        using var disk = new Disk();
        var controller = Controller(_ => disk);
        Issue(controller, 0x48, Transfer(1, 0, 0, 0, 2));
        controller.Advance(20000);
        Assert.False(controller.Busy);
        Assert.False(controller.Drq);
        Assert.Equal(2, controller.ReadBlocks(0));
        Issue(controller, 0x88, Transfer(1, 0, 0, 0, 1));
        Write(controller, disk.Block(0));
        Assert.Equal(0x60, controller.Status);
        Issue(controller, 0x88, Transfer(1, 0, 0, 0, 1));
        Write(controller, new byte[256]);
        Assert.Equal(0x64, controller.Status);
        Assert.Equal(0x50, controller.ReadParameter());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0x80)]
    public void PioBuffersAreIndependentAndRecallClosesDtrWithoutTouchingTheDisk(byte selection)
    {
        var controller = new Hd63463(_ => null, 1000000);
        Issue(controller, 0x38, [selection, 254]);
        controller.Advance(12);
        Assert.True(controller.Busy);
        controller.Advance(1);
        Assert.False(controller.Busy);
        Assert.False(controller.Drq);
        Assert.False(controller.Irq);
        controller.WriteParameter(0x1937);
        controller.WriteParameter(0xeeee); // Beyond the guaranteed buffer area: never wrap over byte zero.
        Issue(controller, 0x30, [(byte)(selection ^ 0x80), 254]);
        controller.Advance(13);
        Assert.Equal(0, controller.ReadParameter());
        Issue(controller, 0x30, [selection, 254]);
        controller.Advance(13);
        Assert.Equal(0x1937, controller.ReadParameter());
        Assert.Equal(ushort.MaxValue, controller.ReadParameter());
        controller.WriteCommand(8);
        controller.Advance(5);
        Assert.Equal(0, controller.Status);
        Assert.Equal(ushort.MaxValue, controller.ReadDma());
    }

    [Theory]
    [InlineData(0x0100)]
    [InlineData(0x8001)]
    public void BufferPointersRejectReservedBitsAndOddWordAddresses(ushort pointer)
    {
        var controller = new Hd63463(_ => null);
        Issue(controller, 0x30, [(byte)(pointer >> 8), (byte)pointer]);
        Assert.Equal(0x64, controller.Status);
        Assert.Equal(0x0c, controller.ReadParameter());
        Assert.False(controller.Drq);
    }

    [Fact]
    public void StandaloneDmaKeepsRequestActiveUntilDoneAndDoesNotRequireADrive()
    {
        var controller = new Hd63463(_ => null, 1000000);
        Issue(controller, 0x90, [0x80, 252]);
        controller.Advance(18);
        Assert.False(controller.Drq);
        controller.Advance(1);
        Assert.True(controller.Busy);
        Assert.True(controller.Drq);
        controller.WriteDma(0x5678);
        controller.WriteDma(0xabcd);
        Assert.True(controller.Drq);
        controller.DmaDone();
        Assert.False(controller.Busy);
        Assert.False(controller.Drq);
        Assert.Equal(0x60, controller.Status);
        Assert.True(controller.Irq);
        Issue(controller, 0x50, [0x80, 252]);
        controller.Advance(19);
        Assert.Equal(0x5678, controller.ReadDma());
        controller.DmaDone(); // Partial DMA is legal: DONE comes from the host, not the buffer boundary.
        Assert.Equal(ushort.MaxValue, controller.ReadDma());
        Issue(controller, 0x30, [0x80, 254]);
        controller.Advance(13);
        Assert.Equal(0xabcd, controller.ReadParameter());
    }

    [Fact]
    public void PioReadModifyWriteUsesTheInternalBufferWithoutAnyAutomaticMemoryDma()
    {
        using var disk = new Disk();
        var controller = Controller(_ => disk, mode: 0);
        var requests = new List<bool>();
        controller.DataRequestChanged = requests.Add;
        try
        {
            var original = disk.Block(0);
            Issue(controller, 0x40, Transfer(1, 0, 0, 0, 1));
            controller.Advance(20000);
            Assert.False(controller.Busy);
            Assert.Empty(requests);
            Issue(controller, 0x30, [0, 12]);
            controller.Advance(100);
            Assert.Equal(BinaryPrimitives.ReadUInt16LittleEndian(original.AsSpan(12)), controller.ReadParameter());
            Issue(controller, 0x38, [0, 12]);
            controller.Advance(100);
            controller.WriteParameter(0xabcd);
            Issue(controller, 0x87, Transfer(1, 0, 0, 1, 1));
            controller.Advance(20000);
            Assert.False(controller.Busy);
            Assert.Empty(requests);
            original[12] = 0xcd;
            original[13] = 0xab;
            Assert.Equal(original, disk.Block(1));
            Assert.Equal(1, controller.ReadBlocks(0));
            Assert.Equal(1, controller.WrittenBlocks(0));
        }
        finally { controller.DataRequestChanged = null; }
    }

    [Fact]
    public void CheckDataChecksIntegrityWithoutOverwritingTheProgrammedBuffer()
    {
        using var disk = new Disk();
        var controller = Controller(_ => disk, mode: 0);
        Issue(controller, 0x38, [0, 0]);
        controller.Advance(100);
        controller.WriteParameter(0x1937);
        Issue(controller, 0x48, Transfer(1, 0, 0, 0, 1));
        controller.Advance(20000);
        Assert.Equal(0x60, controller.Status);
        Issue(controller, 0x30, [0, 0]);
        controller.Advance(100);
        Assert.Equal(0x1937, controller.ReadParameter());
        Assert.False(controller.Drq);
    }

    [Theory]
    [InlineData(0x60)]
    [InlineData(0x61)]
    public void IdentifiersUseThePhysicalTrackAndWrapSectorsWithoutChangingHeads(byte command)
    {
        using var disk = new Disk();
        var controller = Controller(_ => disk);
        Issue(controller, 0xc0, [1, 0, 0, 1]);
        controller.Advance(20000);
        Issue(controller, command, [1, 1, 0, 2, 0, 4]);
        controller.Advance(7000);
        Assert.False(controller.Drq); // Every requested identifier must be collected first.
        var data = Read(controller);
        Assert.Equal(512, data.Length);
        Assert.Equal(new byte[] { 0, 1, 1, 2, 0, 1, 1, 3, 0, 1, 1, 0, 0, 1, 1, 1 }, data[..16]);
        Assert.Equal(0x60, controller.Status);
        Assert.Equal(0, controller.ReadBlocks(0)); // No DATA field was read.
    }

    [Fact]
    public void PioIdentifiersFillBothBuffersAndDoNotTreatDataCrcAsAnIdCrc()
    {
        using var disk = new Disk { Bad = true };
        var controller = Controller(_ => disk, mode: 0);
        Issue(controller, 0x61, [1, 1, 0, 2, 0, 65]);
        controller.Advance(400000); // Seventeen physical revolutions, not 65 fixed delays.
        Assert.Equal(0x60, controller.Status);
        Assert.False(controller.Drq);
        Issue(controller, 0x30, [0x80, 0]);
        controller.Advance(100);
        Assert.Equal(0, controller.ReadParameter());
        Assert.Equal(0x0201, controller.ReadParameter());
    }

    [Fact]
    public void IdentifierDmaCanFinishEarlyThroughDoneAndReportsTheOriginalHead()
    {
        using var disk = new Disk();
        var controller = Controller(_ => disk);
        Issue(controller, 0x60, [1, 1, 0, 0, 0, 1]);
        controller.Advance(20000);
        Assert.True(controller.Drq);
        Assert.Equal(0, controller.ReadDma());
        controller.DmaDone();
        Assert.Equal(0x60, controller.Status);
        Assert.False(controller.Drq);
        Assert.Equal(new byte[] { 0, 0, 1, 1, 0, 0, 0, 0 }, Results(controller, 8));
        Assert.Equal(ushort.MaxValue, controller.ReadDma());
    }

    [Theory]
    [InlineData(1, 0, 1)]
    [InlineData(0, 1, 1)]
    [InlineData(0, 0, 129)]
    public void IdentifierParametersRejectReservedBytesAndAnOverfullTrackBuffer(byte reservedHigh, byte reservedCount, byte count)
    {
        using var disk = new Disk();
        var controller = Controller(_ => disk);
        Issue(controller, 0x60, [1, 0, reservedHigh, 0, reservedCount, count]);
        Assert.Equal(0x64, controller.Status);
        Assert.Equal(0x0c, controller.ReadParameter());
        Assert.False(controller.Drq);
    }

    [Fact]
    public void TestFloatsTheDriveOutputsAfterSixtyClocksAndOnlyResetRestoresThem()
    {
        var controller = new Hd63463(_ => null, 1000000);
        Issue(controller, 0xe0, []);
        controller.Advance(7);
        Assert.True(controller.Busy);
        Assert.True(controller.DriveOutputsEnabled);
        controller.Advance(1);
        Assert.False(controller.Busy);
        Assert.False(controller.DriveOutputsEnabled);
        Assert.False(controller.Drq);
        Assert.False(controller.Irq);
        Assert.False(controller.Active(0));
        var status = controller.Status;
        controller.WriteCommand(8);
        controller.WriteCommand(0xf0);
        controller.WriteParameter(0x1234);
        Assert.Equal(status, controller.Status);
        Assert.False(controller.DriveOutputsEnabled);
        controller.Reset();
        Assert.True(controller.DriveOutputsEnabled);
        controller.Advance(19);
        Assert.Equal(0, controller.Status);
        Issue(controller, 0x38, [0, 0]);
        controller.Advance(13);
        controller.WriteParameter(0x1234);
        Issue(controller, 0x30, [0, 0]);
        controller.Advance(13);
        Assert.Equal(0x1234, controller.ReadParameter());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void TestAfterSpecifyReportsRtsEvenWhenNoUnitWasConfigured(byte configuredUnits)
    {
        var controller = new Hd63463(_ => null);
        Issue(controller, 0xe8, [0x1f, 0x80, 0, configuredUnits, 16, 2, 1, 3, 0x21, 10, 13, 12, 0, 0, 0, 0]);
        Issue(controller, 0xe0, []);
        Assert.True(controller.DriveOutputsEnabled);
        Assert.False(controller.Busy);
        Assert.Equal(0x64, controller.Status);
        Assert.Equal(0x14, controller.ReadParameter());
        controller.Reset();
        Issue(controller, 0xe0, []);
        controller.Advance(100);
        Assert.False(controller.DriveOutputsEnabled);
    }

    [Theory]
    [InlineData(0, 160)]
    [InlineData(1, 332)]
    [InlineData(2, 492)]
    [InlineData(254, 40812)]
    public void NormalSeekUsesTheProgrammedPulseWidthsInsteadOfAFixedDelay(byte stepLow, int ticks)
    {
        using var disk = new Disk();
        var controller = Controller(_ => disk, stepLow: stepLow);
        controller.Configure(0, 0, 100);
        Issue(controller, 0xc0, [1, 0, 0, 1]);
        controller.Advance(ticks - 1);
        Assert.True(controller.Busy);
        controller.Advance(1);
        Assert.Equal(0x70, controller.Status);
        Assert.Equal(new byte[] { 0, 0, 1, 6 }, Results(controller, 4));
    }

    [Fact]
    public void TheFastestPulseRateIsRejectedInNormalSeekMode()
    {
        using var disk = new Disk();
        var controller = Controller(_ => disk, stepLow: byte.MaxValue);
        Issue(controller, 0xc0, [1, 0, 0, 1]);
        Assert.False(controller.Busy);
        Assert.Equal(0x64, controller.Status);
        Assert.Equal(0x30, controller.ReadParameter());
        Assert.Equal(0, controller.SeekCount(0));
    }

    [Fact]
    public void ParallelSeekReleasesTheControllerAndPollingReportsEachDriveWhenReady()
    {
        using var disk = new Disk();
        var controller = Controller(_ => disk, mode: 0x81);
        Issue(controller, 0xc0, [1, 0, 0, 1]);
        controller.Advance(39);
        Assert.Equal(0x60, controller.Status); // Pulses issued; no seek-end signal yet.
        Assert.Equal(new byte[] { 0, 0, 1, 4 }, Results(controller, 4));
        Issue(controller, 0xc0, [2, 0, 0, 1]);
        controller.Advance(39);
        Assert.Equal(0x60, controller.Status);
        Assert.Equal(new byte[] { 0, 0, 2, 0 }, Results(controller, 4));
        Issue(controller, 0x10, []);
        Assert.Equal(0xc2, controller.Status);
        controller.Advance(1950); // Two 5-tick Recall commands also advance the drive.
        Assert.True(controller.Busy);
        controller.Advance(1);
        Assert.Equal(0x50, controller.Status);
        Assert.True(controller.Irq);
        Assert.Equal(new byte[] { 0, 0, 1, 2 }, Results(controller, 4));
        Issue(controller, 0x10, []);
        controller.Advance(38);
        Assert.True(controller.Busy);
        controller.Advance(1);
        Assert.Equal(0x50, controller.Status);
        Assert.Equal(new byte[] { 0, 0, 2, 6 }, Results(controller, 4));
    }

    [Fact]
    public void PollingIsBusyUntilStoppedOrADriveErrorOccurs()
    {
        using var disk = new Disk();
        IEmulationDiskImage? first = disk;
        var controller = Controller(unit => unit == 0 ? first : disk);
        Issue(controller, 0x10, []);
        controller.Advance(100000);
        Assert.Equal(0xc2, controller.Status);
        controller.WriteCommand(0x18); // POD must be accepted even though POL asserts BSY.
        Assert.False(controller.Busy);
        Assert.False(controller.Irq);
        Issue(controller, 0x10, []);
        first = null;
        controller.MediaChanged(0);
        Assert.Equal(0x4c, controller.Status);
        Assert.True(controller.Irq);
        Assert.Equal(new byte[] { 0, 0x20, 1, 4 }, Results(controller, 4));
        var uninitialized = new Hd63463(_ => disk);
        Issue(uninitialized, 0x10, []);
        Assert.Equal(0x10, uninitialized.ReadParameter());
    }

    [Fact]
    public void ParallelDriveEventsAreIndependentOfAdvanceChunking()
    {
        using var disk = new Disk();
        var single = Controller(_ => disk, mode: 0x81);
        var batched = Controller(_ => disk, mode: 0x81);
        foreach (var controller in new[] { single, batched })
        {
            Issue(controller, 0xc0, [1, 0, 0, 1]);
            controller.Advance(39);
            Issue(controller, 0xc0, [2, 0, 0, 2]);
            controller.Advance(44);
            Issue(controller, 0x10, []);
        }
        for (var tick = 0; tick < 10000; tick++) single.Advance(1);
        batched.Advance(10000);
        Assert.Equal(single.Status, batched.Status);
        Assert.Equal(Results(single, 4), Results(batched, 4));
        Issue(single, 0x10, []);
        Issue(batched, 0x10, []);
        Assert.Equal(single.Status, batched.Status);
        Assert.Equal(Results(single, 4), Results(batched, 4));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void DmaRequestsFollowBurstOrCycleStealForEveryAcknowledgedWord(bool burst, bool write)
    {
        var controller = Controller(_ => null, mode: burst ? (byte)0xc0 : (byte)0x80);
        var requests = new List<bool>();
        controller.DataRequestChanged = requests.Add;
        try
        {
            Issue(controller, write ? (byte)0x90 : (byte)0x50, [0, 0]);
            controller.Advance(10000);
            if (write)
            {
                controller.WriteDma(0x1234);
                controller.WriteDma(0x5678);
            }
            else
            {
                controller.ReadDma();
                controller.ReadDma();
            }
            controller.DmaDone();
            Assert.Equal(burst ? new[] { true, false } : new[] { true, false, true, false, true, false }, requests);
            Assert.Equal(0x60, controller.Status);
        }
        finally { controller.DataRequestChanged = null; }
    }

    [Fact]
    public void DoneWithTheLastReadWordStopsBeforeTheNextRequestedSector()
    {
        using var disk = new Disk();
        var controller = Controller(_ => disk);
        Issue(controller, 0x40, Transfer(1, 0, 0, 0, 2));
        WaitDrq(controller);
        var data = new byte[256];
        for (var word = 0; word < 128; word++)
            BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(word * 2), controller.ReadDma(done: word == 127));
        Assert.Equal(disk.Block(0), data);
        Assert.False(controller.Drq);
        Assert.Equal(0x60, controller.Status);
        Assert.Equal(1, controller.ReadBlocks(0));
        Assert.Equal(new byte[] { 0, 0, 1, 0, 0, 0, 0, 1, 0, 1 }, Results(controller, 10));
        controller.Advance(100000);
        Assert.Equal(1, controller.ReadBlocks(0));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(128)]
    public void DoneWritesTheCurrentSectorWithRetainedBufferBytesAndLeavesTheNextUntouched(int words)
    {
        using var disk = new Disk();
        var controller = Controller(_ => disk);
        var adjacent = disk.Block(1);
        Issue(controller, 0x38, [0, 0]);
        controller.Advance(10000);
        for (var word = 0; word < 128; word++) controller.WriteParameter(0xcccc);
        Issue(controller, 0x87, Transfer(1, 0, 0, 0, 2));
        WaitDrq(controller);
        for (var word = 0; word < words; word++) controller.WriteDma(0x7979, done: word == words - 1);
        Assert.True(controller.Busy);
        WaitIdle(controller);
        var expected = Enumerable.Repeat((byte)0xcc, 256).ToArray();
        expected.AsSpan(0, words * 2).Fill(0x79);
        Assert.Equal(expected, disk.Block(0));
        Assert.Equal(adjacent, disk.Block(1));
        Assert.Equal(0x60, controller.Status);
        Assert.Equal(1, controller.WrittenBlocks(0));
        controller.Advance(100000);
        Assert.Equal(1, controller.WrittenBlocks(0));
    }

    [Fact]
    public void DoneDoesNotTurnACompareFailureIntoASuccessfulCompletion()
    {
        using var disk = new Disk();
        var controller = Controller(_ => disk);
        Issue(controller, 0x88, Transfer(1, 0, 0, 0, 2));
        WaitDrq(controller);
        controller.WriteDma(BinaryPrimitives.ReadUInt16LittleEndian(disk.Block(0)), done: true);
        Assert.True(controller.Busy);
        WaitIdle(controller);
        Assert.False(controller.Busy);
        Assert.False(controller.Drq);
        Assert.Equal(0x64, controller.Status);
        Assert.Equal(0x50, controller.ReadParameter());
    }

    [Fact]
    public void ErroneousReadUsesThePhysicalTrackAndHeadWithItsOwnResultLayout()
    {
        using var disk = new Disk();
        var controller = Controller(_ => disk);
        Issue(controller, 0xc0, [1, 0, 0, 1]);
        controller.Advance(100000);
        Issue(controller, 0x40, Transfer(1, 1, 0, 1, 1));
        Assert.Equal(disk.Block(9), Read(controller));
        Issue(controller, 0x70, [1, 1, 0, 1]);
        Assert.Equal(disk.Block(12), Read(controller));
        Assert.Equal(new byte[] { 0, 0, 1, 1, 0, 0 }, Results(controller, 6));
    }

    [Fact]
    public void ErroneousReadUsesEachSpindleWithoutRetainingTheLastLogicalSector()
    {
        using var first = new Disk();
        using var second = new Disk(7);
        var controller = Controller(unit => unit == 0 ? first : second);
        Issue(controller, 0x40, Transfer(1, 0, 0, 2, 1));
        Read(controller);
        Issue(controller, 0x70, [2, 1, 0, 1]);
        Assert.Equal(second.Block(4), Read(controller));
        Issue(controller, 0x70, [1, 0, 0, 1]);
        Assert.Equal(first.Block(0), Read(controller));
        Assert.Equal(new byte[] { 0, 0, 1, 0, 0, 0 }, Results(controller, 6));
    }

    [Fact]
    public void ErroneousReadInPioStoresDataWithoutRequestingDma()
    {
        using var disk = new Disk();
        var controller = Controller(_ => disk, mode: 0);
        Issue(controller, 0x70, [1, 1, 0, 1]);
        controller.Advance(100000);
        Assert.False(controller.Busy);
        Assert.False(controller.Drq);
        Assert.Equal(new byte[] { 0, 0, 1, 1, 0, 0 }, Results(controller, 6));
        Issue(controller, 0x30, [0, 0]);
        controller.Advance(10000);
        var data = new byte[256];
        for (var word = 0; word < 128; word++)
            BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(word * 2), controller.ReadParameter());
        Assert.Equal(disk.Block(4), data);
    }

    [Fact]
    public void ErroneousReadDoesNotIgnoreDataIntegrityErrors()
    {
        using var disk = new Disk { Bad = true };
        var controller = Controller(_ => disk);
        Issue(controller, 0x70, [1, 0, 0, 1]);
        WaitIdle(controller);
        Assert.False(controller.Drq);
        Assert.Equal(0x64, controller.Status);
        Assert.Equal(new byte[] { 0, 0x44, 1, 0, 0, 1 }, Results(controller, 6));
    }

    [Fact]
    public void ErroneousReadRejectsTheLogicalReadParameterBlock()
    {
        using var disk = new Disk();
        var controller = Controller(_ => disk);
        Issue(controller, 0x70, Transfer(1, 0, 0, 0, 1));
        Assert.Equal(0x64, controller.Status);
        Assert.Equal(0x0c, controller.ReadParameter());
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(0, 255)]
    public void LogicalHeadDoesNotSelectAPhysicalTrack(int physicalHead, int logicalHead)
    {
        using var disk = new Disk();
        var controller = Controller(_ => disk);
        var parameters = Transfer(1, 0, logicalHead, 0, 1);
        parameters[1] = (byte)physicalHead;
        Issue(controller, 0x40, parameters);
        WaitIdle(controller);
        Assert.Equal(0x64, controller.Status);
        Assert.Equal(0x58, controller.ReadParameter());
        Assert.Equal(0, controller.ReadBlocks(0));
    }

    [Theory]
    [InlineData(7, 254, true)]
    [InlineData(8, 3, false)]
    [InlineData(1, 255, false)]
    public void SpecifyRespectsTheSt506HeadAndSectorFields(byte heads, byte sectors, bool valid)
    {
        var controller = new Hd63463(_ => null);
        Issue(controller, 0xe8, [0x1f, 0x80, 0, 6, 16, 2, heads, sectors, 0x21, 10, 13, 12, 0, 0, 0, 0]);
        Assert.Equal(valid ? 0x40 : 0x64, controller.Status);
        if (!valid) Assert.Equal(0x0c, controller.ReadParameter());
    }

    [Theory]
    [InlineData(1, 256)]
    [InlineData(1, 512)]
    [InlineData(2, 256)]
    [InlineData(2, 512)]
    [InlineData(3, 256)]
    [InlineData(3, 512)]
    [InlineData(4, 256)]
    [InlineData(4, 512)]
    [InlineData(5, 256)]
    [InlineData(5, 4096)]
    public void AllRecordLengthsReadThroughTwoBuffersAcrossContainerBoundaries(byte length, int containerBytes)
    {
        var bytes = 256 << (length - 1);
        using var disk = new Disk(17, containerBytes, bytes);
        var controller = Controller(_ => disk, recordLength: length);
        Issue(controller, 0x40, Transfer(1, 0, 1, 1, 2));
        Assert.Equal(disk.Record(5).Concat(disk.Record(6)), ReadClocked(controller));
        Assert.Equal(0x60, controller.Status);
        Assert.Equal(2, controller.ReadBlocks(0));
        Assert.Equal(new byte[] { 0, 0, 1, 1, 0, 0, 1, 3, 0, 0 }, Results(controller, 10));
    }

    [Theory]
    [InlineData(1, 512)]
    [InlineData(2, 256)]
    [InlineData(2, 512)]
    [InlineData(3, 256)]
    [InlineData(4, 512)]
    [InlineData(5, 4096)]
    public void AllRecordLengthsWriteAndCompareWithoutChangingTheAdjacentRecord(byte length, int containerBytes)
    {
        var bytes = 256 << (length - 1);
        using var disk = new Disk(19, containerBytes, bytes);
        var controller = Controller(_ => disk, recordLength: length);
        var before = disk.Record(0);
        var after = disk.Record(2);
        var data = Enumerable.Range(0, bytes).Select(value => (byte)(value * 3 + value / 256)).ToArray();
        Issue(controller, 0x87, Transfer(1, 0, 0, 1, 1));
        WriteClocked(controller, data);
        Assert.Equal(0x60, controller.Status);
        Assert.Equal(1, controller.WrittenBlocks(0));
        Assert.Equal(data, disk.Record(1));
        Assert.Equal(before, disk.Record(0));
        Assert.Equal(after, disk.Record(2));
        Issue(controller, 0x88, Transfer(1, 0, 0, 1, 1));
        WriteClocked(controller, data);
        Assert.Equal(0x60, controller.Status);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void PioKeepsOnlyTheLastTwoHardwareBuffersOfALongRecord(byte length)
    {
        var bytes = 256 << (length - 1);
        using var disk = new Disk(23, 256, bytes);
        var controller = Controller(_ => disk, mode: 0, recordLength: length);
        Issue(controller, 0x40, Transfer(1, 0, 0, 0, 1));
        controller.Advance(100000);
        Assert.Equal(0x60, controller.Status);
        Assert.False(controller.Drq);
        var record = disk.Record(0);
        Assert.Equal(record.AsSpan(bytes - 512, 256).ToArray(), ReadBuffer(controller, 0));
        Assert.Equal(record.AsSpan(bytes - 256, 256).ToArray(), ReadBuffer(controller, 0x80));
        Issue(controller, 0x87, Transfer(1, 0, 0, 1, 1));
        controller.Advance(100000);
        Assert.Equal(0x60, controller.Status);
        var expected = new byte[bytes];
        for (var offset = 0; offset < bytes; offset += 512)
            record.AsSpan(bytes - 512, 512).CopyTo(expected.AsSpan(offset));
        Assert.Equal(expected, disk.Record(1));
    }

    [Fact]
    public void LongReadOverrunsOnlyWhenTheThirdFragmentCannotEnterItsHardwareBuffer()
    {
        using var disk = new Disk(recordSize: 1024);
        var controller = Controller(_ => disk, recordLength: 3);
        Issue(controller, 0x40, Transfer(1, 0, 0, 0, 1));
        WaitDrq(controller);
        controller.Advance(818);
        Assert.True(controller.Drq);
        Assert.True(controller.Busy);
        controller.Advance(1);
        Assert.Equal(0x64, controller.Status);
        Assert.False(controller.Drq);
        Assert.Equal(0x38, controller.ReadParameter());
    }

    [Fact]
    public void DoneStopsDmaButAllowsTheRestOfTheCurrentLongReadToArrive()
    {
        using var disk = new Disk(recordSize: 1024);
        var controller = Controller(_ => disk, recordLength: 3);
        Issue(controller, 0x70, [1, 0, 0, 2]);
        WaitDrq(controller);
        controller.ReadDma(done: true);
        Assert.False(controller.Drq);
        Assert.True(controller.Busy);
        controller.Advance(100000);
        Assert.Equal(0x60, controller.Status);
        Assert.Equal(1, controller.ReadBlocks(0));
        Assert.Equal(new byte[] { 0, 0, 1, 0, 0, 1 }, Results(controller, 6));
    }

    [Fact]
    public void DoneCompletesALongWriteWithTheRetainedBytesOfBothBuffers()
    {
        using var disk = new Disk(recordSize: 1024);
        var controller = Controller(_ => disk, recordLength: 3);
        foreach (var buffer in new byte[] { 0, 0x80 })
        {
            Issue(controller, 0x38, [buffer, 0]);
            controller.Advance(10000);
            for (var word = 0; word < 128; word++)
                controller.WriteParameter(buffer == 0 ? (ushort)0xaaaa : (ushort)0xbbbb);
        }
        var adjacent = disk.Record(1);
        Issue(controller, 0x87, Transfer(1, 0, 0, 0, 2));
        controller.Advance(7000);
        controller.WriteDma(0x7979, done: true);
        Assert.True(controller.Busy);
        Assert.False(controller.Drq);
        controller.Advance(100000);
        Assert.Equal(0x60, controller.Status);
        var expected = new byte[1024];
        for (var offset = 0; offset < expected.Length; offset += 512)
        {
            expected.AsSpan(offset, 256).Fill(0xaa);
            expected[offset] = expected[offset + 1] = 0x79;
            expected.AsSpan(offset + 256, 256).Fill(0xbb);
        }
        Assert.Equal(expected, disk.Record(0));
        Assert.Equal(adjacent, disk.Record(1));
        Assert.Equal(1, controller.WrittenBlocks(0));
    }

    [Theory]
    [InlineData(0, 0x1e)]
    [InlineData(6, 0x1e)]
    [InlineData(7, 0x1e)]
    [InlineData(2, 0x1f)]
    public void SpecifyRejectsReservedRecordLengthsAndInvalidAutomaticCorrection(byte length, byte mode)
    {
        var controller = new Hd63463(_ => null);
        Issue(controller, 0xe8, [mode, 0x80, 0, 6, 16, 2, 1, 3, length, 10, 13, 12, 0, 0, 0, 0]);
        Assert.Equal(0x64, controller.Status);
        Assert.Equal(0x0c, controller.ReadParameter());
    }

    [Theory]
    [InlineData(3600, 16667)]
    [InlineData(6000, 10000)]
    public void SlowReadHostKeepsTwoSectorsAndRetriesTheThirdOnTheNextRevolution(int rpm, int rotationTicks)
    {
        using var disk = new Disk();
        var controller = Controller(_ => disk);
        controller.Configure(0, 2, 100, rotationsPerMinute: rpm);
        Issue(controller, 0x40, Transfer(1, 0, 0, 0, 3));
        WaitReadBlocks(controller, 2);
        controller.Advance(1000); // Miss the next DATA start while both buffers are occupied.
        Assert.Equal(2, controller.ReadBlocks(0));
        Assert.Equal(disk.Block(0).Concat(disk.Block(1)), ReadAvailable(controller, 512));
        Assert.False(controller.Drq);
        controller.Advance(rotationTicks - 500);
        Assert.Equal(2, controller.ReadBlocks(0));
        controller.Advance(32);
        Assert.Equal(3, controller.ReadBlocks(0));
        Assert.Equal(disk.Block(2), ReadAvailable(controller, 256));
        Assert.Equal(0x60, controller.Status);
    }

    [Fact]
    public void FastestReadWaitsForBufferSpaceWithoutSpinningOnDiskEvents()
    {
        using var disk = new Disk();
        var controller = Controller(_ => disk);
        controller.Configure(0, 0, 0);
        Issue(controller, 0x40, Transfer(1, 0, 0, 0, 4));
        controller.Advance(1000000);
        Assert.Equal(2, controller.ReadBlocks(0));
        Assert.Equal(disk.Block(0).Concat(disk.Block(1)), ReadAvailable(controller, 512));
        controller.Advance(6);
        Assert.Equal(4, controller.ReadBlocks(0));
        Assert.Equal(disk.Block(2).Concat(disk.Block(3)), ReadAvailable(controller, 512));
        Assert.False(controller.Busy);
    }

    [Fact]
    public void DoneAfterReadPrefetchReportsTheSectorsAlreadyReadFromTheDisk()
    {
        using var disk = new Disk();
        var controller = Controller(_ => disk);
        Issue(controller, 0x40, Transfer(1, 0, 0, 0, 3));
        WaitReadBlocks(controller, 2);
        controller.Advance(1000);
        controller.ReadDma(done: true);
        Assert.Equal(0x60, controller.Status);
        Assert.Equal(2, controller.ReadBlocks(0));
        Assert.Equal(new byte[] { 0, 0, 1, 0, 0, 0, 0, 2, 0, 1 }, Results(controller, 10));
        controller.Advance(100000);
        Assert.Equal(2, controller.ReadBlocks(0));
    }

    [Fact]
    public void WriteDmaCanFillBothBuffersBeforeTheDiskConsumesTheFirst()
    {
        using var disk = new Disk();
        var controller = Controller(_ => disk);
        var first = Enumerable.Repeat((byte)0x39, 256).ToArray();
        var second = Enumerable.Repeat((byte)0x72, 256).ToArray();
        Issue(controller, 0x87, Transfer(1, 0, 0, 0, 2));
        WaitDrq(controller);
        WriteAvailable(controller, first);
        Assert.True(controller.Drq);
        WriteAvailable(controller, second);
        Assert.False(controller.Drq);
        Assert.Equal(0, controller.WrittenBlocks(0));
        WaitWrittenBlocks(controller, 1);
        Assert.Equal(1, controller.WrittenBlocks(0));
        controller.Advance(515);
        Assert.Equal(2, controller.WrittenBlocks(0));
        Assert.Equal(first, disk.Block(0));
        Assert.Equal(second, disk.Block(1));
        Assert.Equal(0x60, controller.Status);
    }

    [Fact]
    public void LongWriteCannotPauseInsideDataToWaitForTheHost()
    {
        using var disk = new Disk(recordSize: 1024);
        var controller = Controller(_ => disk, recordLength: 3);
        var data = Enumerable.Repeat((byte)0x19, 256).ToArray();
        var next = disk.Block(1);
        Issue(controller, 0x87, Transfer(1, 0, 0, 0, 1));
        WaitDrq(controller);
        WriteAvailable(controller, data);
        for (var tick = 0; tick < 200000 && !disk.HasChanges && controller.Busy; tick++) controller.Advance(1);
        Assert.True(disk.HasChanges);
        controller.Advance(408); // Fractional spindle phase: the next fragment ends 409 ticks later.
        Assert.True(controller.Busy);
        controller.Advance(1);
        Assert.Equal(0x64, controller.Status);
        Assert.Equal(0x38, controller.ReadParameter());
        Assert.Equal(data, disk.Block(0));
        Assert.Equal(next, disk.Block(1));
        Assert.Equal(0, controller.WrittenBlocks(0));
    }

    [Fact]
    public void ShortWriteWaitsOneRevolutionWhenTheNextSectorHasNotStarted()
    {
        using var disk = new Disk();
        var controller = Controller(_ => disk);
        var first = Enumerable.Repeat((byte)0x31, 256).ToArray();
        var second = Enumerable.Repeat((byte)0x62, 256).ToArray();
        Issue(controller, 0x87, Transfer(1, 0, 0, 0, 2));
        WaitDrq(controller);
        WriteAvailable(controller, first);
        WaitWrittenBlocks(controller, 1);
        controller.Advance(200);
        Assert.Equal(1, controller.WrittenBlocks(0));
        WriteAvailable(controller, second);
        controller.Advance(15000);
        Assert.Equal(1, controller.WrittenBlocks(0));
        WaitIdle(controller);
        Assert.Equal(2, controller.WrittenBlocks(0));
        Assert.Equal(0x60, controller.Status);
        Assert.Equal(first, disk.Block(0));
        Assert.Equal(second, disk.Block(1));
    }

    [Fact]
    public void DoneDuringWritePrefetchStopsAtTheCurrentDiskSector()
    {
        using var disk = new Disk();
        var controller = Controller(_ => disk);
        var current = Enumerable.Repeat((byte)0x31, 256).ToArray();
        var untouched = disk.Block(1);
        Issue(controller, 0x87, Transfer(1, 0, 0, 0, 2));
        WaitDrq(controller);
        WriteAvailable(controller, current);
        controller.WriteDma(0x6262, done: true);
        WaitIdle(controller);
        Assert.Equal(0x60, controller.Status);
        Assert.Equal(1, controller.WrittenBlocks(0));
        Assert.Equal(current, disk.Block(0));
        Assert.Equal(untouched, disk.Block(1));
    }

    private static byte[] ReadAvailable(Hd63463 controller, int bytes)
    {
        var data = new byte[bytes];
        for (var offset = 0; offset < bytes; offset += 2)
        {
            Assert.True(controller.Drq);
            BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(offset), controller.ReadDma());
        }
        return data;
    }

    private static void WriteAvailable(Hd63463 controller, ReadOnlySpan<byte> data)
    {
        for (var offset = 0; offset < data.Length; offset += 2)
        {
            Assert.True(controller.Drq);
            controller.WriteDma(BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]));
        }
    }

    private static byte[] ReadBuffer(Hd63463 controller, byte selection)
    {
        Issue(controller, 0x30, [selection, 0]);
        controller.Advance(10000);
        var data = new byte[256];
        for (var word = 0; word < 128; word++)
            BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(word * 2), controller.ReadParameter());
        return data;
    }

    private static byte[] ReadClocked(Hd63463 controller)
    {
        using var data = new MemoryStream();
        for (var tick = 0; tick < 100000 && controller.Busy; tick++)
        {
            controller.Advance(1);
            while (controller.Drq)
            {
                var word = controller.ReadDma();
                data.WriteByte((byte)word);
                data.WriteByte((byte)(word >> 8));
            }
        }
        Assert.False(controller.Busy);
        return data.ToArray();
    }

    private static void WriteClocked(Hd63463 controller, ReadOnlySpan<byte> data)
    {
        var offset = 0;
        for (var tick = 0; tick < 100000 && controller.Busy; tick++)
        {
            controller.Advance(1);
            while (controller.Drq && offset < data.Length)
            {
                controller.WriteDma(BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]));
                offset += 2;
            }
        }
        Assert.False(controller.Busy);
        Assert.Equal(data.Length, offset);
    }

    private static Hd63463 Controller(Func<int, IEmulationDiskImage?> disk, byte mode = 0x80, byte stepLow = 0, byte recordLength = 1, byte? operationMode = null)
    {
        var controller = new Hd63463(disk, 1000000);
        Issue(controller, 0xe8, [operationMode ?? (recordLength == 1 ? (byte)0x1f : (byte)0x1e), mode, stepLow, 6, 16, 2, 1, 3, (byte)(0x20 | recordLength), 10, 13, 12, 0, 0, 0, 0]);
        for (var unit = 0; unit < 2; unit++)
        {
            if (disk(unit) is not { } image) continue;
            var records = (long)image.SectorsPerTrack * image.SectorSize / (256 << (recordLength - 1));
            var rpm = 3600;
            while (records * ((256 << (recordLength - 1)) + 128L) + 64 >= 625000L * 60 / rpm) rpm /= 2;
            controller.Configure(unit, 2, 100, rotationsPerMinute: rpm);
        }
        return controller;
    }

    private static byte[] Transfer(int drive, int cylinder, int head, int sector, int count) =>
        [(byte)drive, (byte)head, (byte)(cylinder >> 8), (byte)cylinder, (byte)head, (byte)sector, (byte)(count >> 8), (byte)count];

    private static void Issue(Hd63463 controller, byte command, byte[] parameters)
    {
        if (controller.Status == 0x80) controller.Advance(100);
        var wasBusy = controller.Busy;
        controller.WriteCommand(8);
        if (!wasBusy)
            for (var tick = 0; tick < 100 && controller.Busy; tick++) controller.Advance(1);
        for (var offset = 0; offset < parameters.Length; offset += 2)
            controller.WriteParameter(BinaryPrimitives.ReadUInt16BigEndian(parameters.AsSpan(offset)));
        controller.WriteCommand(command);
        if (command == 0xe8)
            for (var tick = 0; tick < 100 && controller.Busy; tick++) controller.Advance(1);
    }

    private static byte[] Results(Hd63463 controller, int length)
    {
        var data = new byte[length];
        for (var offset = 0; offset < length; offset += 2)
            BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(offset), controller.ReadParameter());
        return data;
    }

    private static byte[] Read(Hd63463 controller)
    {
        using var data = new MemoryStream();
        for (var attempt = 0; attempt < 20 && controller.Busy; attempt++)
        {
            controller.Advance(10000);
            if (!controller.Drq) continue;
            for (var word = 0; word < 128; word++)
            {
                var value = controller.ReadDma();
                data.WriteByte((byte)value);
                data.WriteByte((byte)(value >> 8));
            }
        }
        Assert.False(controller.Busy);
        return data.ToArray();
    }

    private static void Write(Hd63463 controller, ReadOnlySpan<byte> data)
    {
        WaitDrq(controller);
        Assert.True(controller.Drq);
        for (var offset = 0; offset < data.Length; offset += 2)
            controller.WriteDma(BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]));
        WaitIdle(controller);
    }

    private static void WaitReadBlocks(Hd63463 controller, int blocks)
    {
        for (var tick = 0; tick < 200000 && controller.ReadBlocks(0) < blocks && controller.Busy; tick++) controller.Advance(1);
        Assert.Equal(blocks, controller.ReadBlocks(0));
    }

    private static void WaitWrittenBlocks(Hd63463 controller, int blocks)
    {
        for (var tick = 0; tick < 200000 && controller.WrittenBlocks(0) < blocks && controller.Busy; tick++) controller.Advance(1);
        Assert.Equal(blocks, controller.WrittenBlocks(0));
    }

    private static void WaitDrq(Hd63463 controller)
    {
        for (var tick = 0; tick < 200000 && controller.Busy && !controller.Drq; tick++) controller.Advance(1);
        Assert.True(controller.Drq);
    }

    private static void WaitIdle(Hd63463 controller)
    {
        for (var tick = 0; tick < 200000 && controller.Busy; tick++) controller.Advance(1);
        Assert.False(controller.Busy);
    }

    internal sealed class Disk(byte seed = 0, int sectorSize = 256, int recordSize = 256, int sectorsPerTrack = 4) : IEmulationDiskImage
    {
        private readonly Dictionary<long, bool> _dataMarks = new();
        private readonly byte[] _bytes = Enumerable.Range(0, 3 * 2 * sectorsPerTrack * recordSize).Select(index => (byte)(seed + index * 13 + index / 256 * 7)).ToArray();
        public IEmulationDriveMechanics Mechanics { get; } = EmulationSdk.MediaProcessing.CreateDriveMechanics(RetroMediaLab.Emulation.Constants.EmulationMechanicalSoundConstants.HardDisk);
        public string FormatId => "test.st506";
        public int SectorSize => sectorSize;
        public long SectorCount => _bytes.Length / sectorSize;
        public long Cylinders => 3;
        public int Heads => 2;
        public int SectorsPerTrack => sectorsPerTrack * recordSize / sectorSize;
        public bool IsReadOnly { get; init; }
        public bool HasChanges { get; private set; }
        public IReadOnlyDictionary<string, string> Metadata { get; } = new Dictionary<string, string>();
        public bool Bad { get; init; }
        internal Func<EmulationDiskSector, EmulationDiskSector>? ReadTransform { get; init; }
        internal IReadOnlyList<int>? TrackOrder { get; init; }
        internal bool Disposed { get; private set; }
        internal byte[] Block(int block) => _bytes.AsSpan(block * 256, 256).ToArray();
        internal byte[] Record(int record) => _bytes.AsSpan(record * recordSize, recordSize).ToArray();
        public void UpdateMetadata(IReadOnlyDictionary<string, string> metadata) => throw new NotSupportedException();
        public long? FindSector(int cylinder, int head, int number) =>
            cylinder >= 0 && cylinder < Cylinders && head >= 0 && head < Heads && number >= 0 && number < SectorsPerTrack
                ? (cylinder * Heads + head) * SectorsPerTrack + number : null;
        public IReadOnlyList<long> GetTrackSectors(int cylinder, int head) => cylinder >= 0 && cylinder < Cylinders && head >= 0 && head < Heads
            ? (TrackOrder ?? Enumerable.Range(0, SectorsPerTrack).ToArray())
                .Select(number => ((long)cylinder * Heads + head) * SectorsPerTrack + number).ToArray() : [];
        public ValueTask<EmulationDiskSector> ReadSectorAsync(long index, CancellationToken cancellationToken = default)
        {
            var sector = new EmulationDiskSector(index, _bytes.AsMemory(checked((int)(index * sectorSize)), sectorSize).ToArray(),
                (int)(index / SectorsPerTrack / Heads), (int)(index / SectorsPerTrack % Heads), (int)(index % SectorsPerTrack), !Bad)
            {
                DeletedDataMark = _dataMarks.TryGetValue(index, out var deleted) ? deleted : null
            };
            return ValueTask.FromResult(ReadTransform?.Invoke(sector) ?? sector);
        }
        public void WriteSector(long index, ReadOnlySpan<byte> data)
        {
            if (IsReadOnly) throw new UnauthorizedAccessException();
            if (data.Length != sectorSize) throw new ArgumentException(nameof(data));
            data.CopyTo(_bytes.AsSpan(checked((int)(index * sectorSize)), sectorSize));
            HasChanges = true;
        }
        public void WriteSector(long index, ReadOnlySpan<byte> data, bool deletedDataMark)
        {
            WriteSector(index, data);
            _dataMarks[index] = deletedDataMark;
        }
        public Task FlushAsync(CancellationToken cancellationToken = default) { HasChanges = false; return Task.CompletedTask; }
        public void Dispose() { Mechanics.Reset(); Disposed = true; }
    }
}
