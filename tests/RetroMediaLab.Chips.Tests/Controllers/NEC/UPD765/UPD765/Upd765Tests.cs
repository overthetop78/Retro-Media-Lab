using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Enums;
using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Services;
using Disk = RetroMediaLab.Chips.Tests.Controllers.Hitachi.HDC.HD63463.Hd63463Tests.Disk;
using static RetroMediaLab.Chips.Tests.Shared.Upd765TestCommands;
namespace RetroMediaLab.Chips.Tests.Controllers.NEC.UPD765.UPD765;

public sealed class Upd765Tests
{
    [Fact]
    public void CommandAndResultBytesUseTheDocumentedRqmAndDirectionHandshake()
    {
        using var rig = new Rig();
        var device = rig.Device;
        Assert.Equal(0x80, device.ReadMainStatus());
        device.WriteFifo(0xff);
        Assert.Equal(0x10, device.ReadMainStatus());
        device.WriteFifo(3); // Rejected while RQM is low.
        Event(device);
        Assert.Equal(0xd0, device.ReadMainStatus());
        Assert.False(device.Irq);
        Assert.Equal(0x80, device.ReadFifo());
        Assert.Equal(0x80, device.ReadMainStatus());
        Send(device, 4, 0);
        Assert.Equal(0x38, device.ReadFifo());
    }

    [Theory]
    [InlineData(0, 0x80)]
    [InlineData(1, 0x90)]
    public void VersionDistinguishesTheOriginalControllerFromTheIntegrated72065(int model, int version)
    {
        using var rig = new Rig(model: (Upd765Model)model);
        Send(rig.Device, 0x10);
        Assert.Equal(version, rig.Device.ReadFifo());
        Assert.False(rig.Device.Irq);
    }

    [Fact]
    public void SeekReturnsToCommandModeAndOnlySenseInterruptAcknowledgesCompletion()
    {
        using var rig = new Rig();
        var device = rig.Device;
        Specify(device, false);
        Send(device, 0x0f, 0, 2);
        Assert.Equal(0x81, device.ReadMainStatus());
        Wait(device, () => device.Irq);
        Assert.Equal(2, rig.Position[0]);
        Assert.Equal(0x81, device.ReadMainStatus());
        Send(device, 8);
        Assert.Equal(new byte[] { 0x20, 2 }, Result(device, 2));
        Assert.False(device.Irq);
        Assert.Equal(0x80, device.ReadMainStatus());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DmaAndPioReadTheRealSectorAndTerminalCountPreservesTheLastByte(bool pio)
    {
        using var rig = new Rig();
        var device = rig.Device;
        Specify(device, pio);
        ReadCommand(device, 1, 3);
        var data = Read(device, 512, pio, terminalCount: true);
        Assert.Equal(rig.Image.Record(1), data);
        var result = Result(device);
        Assert.Equal(new byte[] { 0, 0, 0, 0, 0, 2, 2 }, result);
        Assert.Equal(512, device.ReadBytes(0));
        Assert.False(device.Drq);
        Assert.False(device.Irq);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DmaAndPioWriteOnlyAfterTheSectorAndCrcClocksHavePassed(bool pio)
    {
        using var rig = new Rig();
        var device = rig.Device;
        Specify(device, pio);
        var payload = Enumerable.Range(0, 512).Select(index => (byte)(index * 7)).ToArray();
        Send(device, 0x45, 0, 0, 0, 1, 2, 3, 0x1b, 0xff);
        for (var offset = 0; offset < payload.Length; offset++)
        {
            WaitByte(device, pio, writing: true);
            if (offset == payload.Length - 1) device.SetTerminalCount(true);
            if (pio) device.WriteFifo(payload[offset]); else device.WriteDma(payload[offset]);
        }
        Assert.False(rig.Image.HasChanges);
        Assert.True(device.Active);
        Assert.Equal(new byte[] { 0, 0, 0, 0, 0, 2, 2 }, Result(device));
        Assert.Equal(payload, rig.Image.Record(1));
        Assert.Equal(512, device.WrittenBytes(0));
    }

    [Fact]
    public void MissingDmaAcknowledgmentTerminatesWithOverrunInsteadOfWaitingForever()
    {
        using var rig = new Rig();
        Specify(rig.Device, false);
        ReadCommand(rig.Device, 0, 3);
        WaitByte(rig.Device, false, false);
        Assert.True(rig.Device.Drq);
        rig.Device.Advance((int)rig.Device.ByteTicks);
        var result = Result(rig.Device);
        Assert.Equal(0x40, result[0]);
        Assert.Equal(0x10, result[1]);
        Assert.False(rig.Device.Drq);
    }

    [Fact]
    public void EotWithoutTerminalCountReturnsEndOfCylinderAndTheNextCylinderAddress()
    {
        using var rig = new Rig();
        Specify(rig.Device, false);
        ReadCommand(rig.Device, 3, 3);
        Assert.Equal(rig.Image.Record(3), Read(rig.Device, 512, false));
        Assert.Equal(new byte[] { 0x40, 0x80, 0, 1, 0, 1, 2 }, Result(rig.Device));
    }

    [Fact]
    public void ReadIdReturnsAnExistingPhysicalHeaderAndAcknowledgesItsResultInterrupt()
    {
        using var rig = new Rig();
        Specify(rig.Device, false);
        Send(rig.Device, 0x4a, 0);
        Wait(rig.Device, () => (rig.Device.ReadMainStatus() & 0xf0) == 0xd0);
        Assert.True(rig.Device.Irq);
        var result = Result(rig.Device);
        Assert.Equal(0, result[0]);
        Assert.Equal(0, result[3]);
        Assert.Equal(0, result[4]);
        Assert.InRange(result[5], (byte)0, (byte)3);
        Assert.Equal(2, result[6]);
        Assert.False(rig.Device.Irq);
    }

    [Fact]
    public void BadDataCrcIsReportedAfterThePayloadHasActuallyBeenTransferred()
    {
        using var image = new Disk(sectorSize: 512, recordSize: 512) { Bad = true };
        using var rig = new Rig(image);
        Specify(rig.Device, false);
        ReadCommand(rig.Device, 1, 3);
        Assert.Equal(image.Record(1), Read(rig.Device, 512, false, terminalCount: true));
        Assert.Equal(new byte[] { 0x40, 0x20, 0x20, 0, 0, 1, 2 }, Result(rig.Device));
    }

    [Fact]
    public void MissingSectorTimesOutAndProtectedMediaCannotEnterTheWriteTransfer()
    {
        using var image = new Disk(sectorSize: 512, recordSize: 512) { IsReadOnly = true };
        using var rig = new Rig(image);
        Specify(rig.Device, false);
        Send(rig.Device, 0x45, 0, 0, 0, 1, 2, 3, 0x1b, 0xff);
        Assert.Equal(2, Result(rig.Device)[1]);
        Assert.False(image.HasChanges);
        ReadCommand(rig.Device, 9, 9);
        Assert.Equal(4, Result(rig.Device)[1]);
        Assert.False(rig.Device.Drq);
    }

    [Fact]
    public void ResetCancelsAPartialWriteWithoutSavingOrDisposingTheBorrowedMedia()
    {
        using var rig = new Rig();
        Specify(rig.Device, false);
        Send(rig.Device, 0x45, 0, 0, 0, 1, 2, 3, 0x1b, 0xff);
        WaitByte(rig.Device, false, true);
        rig.Device.WriteDma(0xa5);
        rig.Device.Reset();
        rig.Device.Advance(1000000);
        Assert.False(rig.Device.Active);
        Assert.False(rig.Device.Irq);
        Assert.False(rig.Device.Drq);
        Assert.False(rig.Image.HasChanges);
        Assert.False(rig.Image.Disposed);
        Assert.Equal(0x80, rig.Device.ReadMainStatus());
    }

    [Fact]
    public void MediaRemovalDuringAnActiveRequestStopsWithoutWritingTheOldImage()
    {
        using var rig = new Rig();
        Specify(rig.Device, false);
        Send(rig.Device, 0x45, 0, 0, 0, 1, 2, 3, 0x1b, 0xff);
        WaitByte(rig.Device, false, true);
        rig.Connected = false;
        rig.Device.Advance(1);
        Assert.Equal(0x48, Result(rig.Device)[0]);
        Assert.False(rig.Image.HasChanges);
        Assert.False(rig.Image.Disposed);
    }

    [Theory]
    [InlineData(0x51, 0x30, 0x30, 8)]
    [InlineData(0x59, 0x20, 0x30, 0)]
    [InlineData(0x5d, 0x40, 0x30, 0)]
    [InlineData(0x59, 0x30, 0x30, 8)]
    [InlineData(0x5d, 0x30, 0x30, 8)]
    [InlineData(0x51, 0xff, 0x30, 8)]
    [InlineData(0x51, 0x30, 0xff, 8)]
    public void ScansCompareDiskAgainstHostAndMaskEitherFfByte(int command, int disk, int host, int status)
    {
        using var image = new Disk(sectorSize: 512, recordSize: 512)
        {
            IsReadOnly = true,
            ReadTransform = sector => sector with { Data = Enumerable.Repeat((byte)disk, 512).ToArray() }
        };
        using var rig = new Rig(image);
        Specify(rig.Device, false);
        Send(rig.Device, (byte)command, 0, 0, 0, 1, 2, 3, 0x1b, 1);
        Assert.True(rig.Device.Reading);
        Assert.False(rig.Device.Writing);
        ScanBytes(rig.Device, Enumerable.Repeat((byte)host, 512));
        var result = Result(rig.Device);
        Assert.Equal(0, result[0]);
        Assert.Equal(0, result[1]);
        Assert.Equal(status, result[2]);
        Assert.Equal(1, result[5]);
        Assert.Equal(512, rig.Device.ReadBytes(0));
        Assert.Equal(0, rig.Device.WrittenBytes(0));
        Assert.False(image.HasChanges);
    }

    [Fact]
    public void ScanStepTwoSkipsTheInterveningSectorAndResetsItsComparisonForEachSector()
    {
        using var rig = new Rig();
        Specify(rig.Device, true);
        Send(rig.Device, 0x51, 0, 0, 0, 1, 2, 3, 0x1b, 2);
        ScanBytes(rig.Device, rig.Image.Record(3), pio: true);
        ScanBytes(rig.Device, rig.Image.Record(3), pio: true);
        var result = Result(rig.Device);
        Assert.Equal(0, result[0]);
        Assert.Equal(8, result[2]);
        Assert.Equal(3, result[5]);
        Assert.Equal(1024, rig.Device.ReadBytes(0));
        Assert.False(rig.Image.HasChanges);
    }

    [Fact]
    public void ScanLowMustSatisfyEveryByteAndReportsNotSatisfiedAtEot()
    {
        using var image = new Disk(sectorSize: 512, recordSize: 512)
        {
            ReadTransform = sector => sector with { Data = Enumerable.Repeat((byte)0x30, 512).ToArray() }
        };
        using var rig = new Rig(image);
        Specify(rig.Device, false);
        Send(rig.Device, 0x59, 0, 0, 0, 3, 2, 3, 0x1b, 1);
        var host = Enumerable.Repeat((byte)0x40, 512).ToArray();
        host[^1] = 0x20;
        ScanBytes(rig.Device, host);
        var result = Result(rig.Device);
        Assert.Equal(0x40, result[0]);
        Assert.Equal(0x80, result[1]);
        Assert.Equal(4, result[2]);
        Assert.Equal(3, result[5]);
    }

    [Fact]
    public void ScanTerminalCountComparesTheAcknowledgedByteAndStopsRequestingHostData()
    {
        using var rig = new Rig();
        Specify(rig.Device, false);
        Send(rig.Device, 0x51, 0, 0, 0, 1, 2, 3, 0x1b, 1);
        WaitByte(rig.Device, false, true);
        rig.Device.SetTerminalCount(true);
        rig.Device.WriteDma(rig.Image.Record(1)[0]);
        Assert.False(rig.Device.Drq);
        var result = Result(rig.Device);
        Assert.Equal(0, result[0]);
        Assert.Equal(8, result[2]);
        Assert.Equal(1, rig.Device.ReadBytes(0));
        Assert.False(rig.Image.HasChanges);
    }

    [Fact]
    public void ScanStillReportsACrcErrorWhenTheHostComparisonMatches()
    {
        using var image = new Disk(sectorSize: 512, recordSize: 512) { Bad = true };
        using var rig = new Rig(image);
        Specify(rig.Device, false);
        Send(rig.Device, 0x51, 0, 0, 0, 1, 2, 3, 0x1b, 1);
        ScanBytes(rig.Device, image.Record(1));
        var result = Result(rig.Device);
        Assert.Equal(0x20, result[1]);
        Assert.Equal(0x20, result[2]);
        Assert.False(image.HasChanges);
    }

    [Theory]
    [InlineData(0x46, false, 0)]
    [InlineData(0x46, true, 0x40)]
    [InlineData(0x4c, true, 0)]
    [InlineData(0x4c, false, 0x40)]
    public void ReadDeletedAndReadDataUseTheActualMarkAndTransferMismatchedDataBeforeReportingCm(int command, bool deleted, int detail)
    {
        using var image = new Disk(sectorSize: 512, recordSize: 512)
        {
            ReadTransform = sector => sector with { DeletedDataMark = deleted }
        };
        using var rig = new Rig(image);
        Specify(rig.Device, false);
        Send(rig.Device, (byte)command, 0, 0, 0, 1, 2, 3, 0x1b, 0xff);
        Assert.Equal(image.Record(1), Read(rig.Device, 512, false, terminalCount: true));
        var result = Result(rig.Device);
        Assert.Equal(detail == 0 ? 0 : 0x40, result[0]);
        Assert.Equal(0, result[1]);
        Assert.Equal(detail, result[2]);
        Assert.Equal(512, rig.Device.ReadBytes(0));
    }

    [Theory]
    [InlineData(0x66, true)]
    [InlineData(0x6c, false)]
    public void SkipDoesNotRequestHostDataForTheWrongMarkAndFindsTheNextMatchingSector(int command, bool firstDeleted)
    {
        using var image = new Disk(sectorSize: 512, recordSize: 512)
        {
            ReadTransform = sector => sector with { DeletedDataMark = sector.Number == 1 ? firstDeleted : !firstDeleted }
        };
        using var rig = new Rig(image);
        Specify(rig.Device, false);
        Send(rig.Device, (byte)command, 0, 0, 0, 1, 2, 3, 0x1b, 0xff);
        Assert.Equal(image.Record(2), Read(rig.Device, 512, false, terminalCount: true));
        var result = Result(rig.Device);
        Assert.Equal(0, result[0]);
        Assert.Equal(0, result[2]);
        Assert.Equal(3, result[5]);
        Assert.Equal(512, rig.Device.ReadBytes(0));
    }

    [Theory]
    [InlineData(0x46)]
    [InlineData(0x4a)]
    public void HeaderCrcFailureIsReportedWithoutADataTransferOrDataFieldError(int command)
    {
        using var image = new Disk(sectorSize: 512, recordSize: 512)
        {
            ReadTransform = sector => sector with { HeaderIntegrityValid = false, DataIntegrityValid = true, IntegrityValid = false }
        };
        using var rig = new Rig(image);
        Specify(rig.Device, false);
        if (command == 0x4a) Send(rig.Device, (byte)command, 0);
        else Send(rig.Device, (byte)command, 0, 0, 0, 1, 2, 3, 0x1b, 0xff);
        var result = Result(rig.Device);
        Assert.Equal(0x40, result[0]);
        Assert.Equal(0x20, result[1]);
        Assert.Equal(0, result[2]);
        Assert.Equal(0, rig.Device.ReadBytes(0));
        Assert.False(rig.Device.Drq);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task WriteDeletedPublishesItsMarkAfterThePhysicalFieldAndANormalWriteClearsIt(bool pio, bool deleted)
    {
        using var rig = new Rig();
        var device = rig.Device;
        Specify(device, pio);
        var payload = Enumerable.Range(0, 512).Select(index => (byte)(index * 3)).ToArray();
        // Start with the opposite mark, so both transitions are exercised.
        rig.Image.WriteSector(1, rig.Image.Record(1), !deleted);
        await rig.Image.FlushAsync();
        Send(device, deleted ? (byte)0x49 : (byte)0x45, 0, 0, 0, 1, 2, 3, 0x1b, 0xff);
        Assert.True(device.Writing);
        ScanBytes(device, payload.Take(payload.Length - 1), pio);
        WaitByte(device, pio, true);
        device.SetTerminalCount(true);
        if (pio) device.WriteFifo(payload[^1]); else device.WriteDma(payload[^1]);
        Assert.False(rig.Image.HasChanges);
        Assert.Equal(!deleted, (await rig.Image.ReadSectorAsync(1)).DeletedDataMark);
        Assert.Equal(0, Result(device)[0]);
        Assert.Equal(deleted, (await rig.Image.ReadSectorAsync(1)).DeletedDataMark);
        device.SetTerminalCount(false);
        Send(device, deleted ? (byte)0x4c : (byte)0x46, 0, 0, 0, 1, 2, 3, 0x1b, 0xff);
        Assert.Equal(payload, Read(device, 512, pio, terminalCount: true));
        Assert.Equal(0, Result(device)[0]);
    }

    [Fact]
    public async Task ResetBeforeTheWriteDeletedCrcDoesNotPublishEitherTheDataOrTheMark()
    {
        using var rig = new Rig();
        var before = rig.Image.Record(1);
        Specify(rig.Device, false);
        Send(rig.Device, 0x49, 0, 0, 0, 1, 2, 3, 0x1b, 0xff);
        WaitByte(rig.Device, false, true);
        rig.Device.SetTerminalCount(true);
        rig.Device.WriteDma(0xa5);
        rig.Device.Reset();
        rig.Device.Advance(1000000);
        Assert.False(rig.Image.HasChanges);
        Assert.Equal(before, rig.Image.Record(1));
        Assert.Null((await rig.Image.ReadSectorAsync(1)).DeletedDataMark);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReadTrackWaitsForIndexAndCountsDataFieldsRatherThanComparingSectorNumberToEot(bool pio)
    {
        using var rig = new Rig();
        Specify(rig.Device, pio);
        Send(rig.Device, 0x42, 0, 0, 0, 0, 2, 2, 0x1b, 0xff);
        rig.Device.Advance(100000);
        Assert.False(rig.Device.Drq);
        Assert.Equal(0, rig.Device.ReadBytes(0));
        var expected = rig.Image.Record(0).Concat(rig.Image.Record(1)).ToArray();
        Assert.Equal(expected, Read(rig.Device, expected.Length, pio));
        var result = Result(rig.Device);
        Assert.Equal(0x80, result[1]);
        Assert.Equal(0, result[2]);
        Assert.Equal(2, result[5]);
        Assert.Equal(expected.Length, rig.Device.ReadBytes(0));
    }

    [Fact]
    public void ReadTrackContinuesPastHeaderAndDataCrcFailuresAndReturnsTheirCombinedStatus()
    {
        using var image = new Disk(sectorSize: 512, recordSize: 512)
        {
            ReadTransform = sector => sector with
            {
                HeaderIntegrityValid = sector.Number != 0,
                DataIntegrityValid = sector.Number != 1,
                IntegrityValid = sector.Number > 1
            }
        };
        using var rig = new Rig(image);
        Specify(rig.Device, false);
        Send(rig.Device, 0x42, 0, 0, 0, 0, 2, 2, 0x1b, 0xff);
        Assert.Equal(image.Record(0).Concat(image.Record(1)), Read(rig.Device, 1024, false));
        var result = Result(rig.Device);
        Assert.Equal(0xa0, result[1]);
        Assert.Equal(0x20, result[2]);
        Assert.Equal(1024, rig.Device.ReadBytes(0));
    }

    [Fact]
    public void ReadTrackDoesNotChangeFaceAfterReadingAllFieldsOnTheSelectedTrack()
    {
        using var rig = new Rig();
        Specify(rig.Device, false);
        Send(rig.Device, 0x42, 0, 0, 0, 0, 2, 4, 0x1b, 0xff);
        var expected = Enumerable.Range(0, 4).SelectMany(rig.Image.Record).ToArray();
        Assert.Equal(expected, Read(rig.Device, expected.Length, false));
        var result = Result(rig.Device);
        Assert.Equal(0, result[0] & 4);
        Assert.Equal(0, result[4]);
        Assert.Equal(0x80, result[1]);
        Assert.Equal(2048, rig.Device.ReadBytes(0));
    }

    [Fact]
    public void ReadTrackReportsIdMismatchAfterDeliveringDataAndAcceptsTerminalCountWithinTheField()
    {
        using var rig = new Rig();
        Specify(rig.Device, false);
        Send(rig.Device, 0x42, 0, 0, 0, 9, 2, 4, 0x1b, 0xff);
        Assert.Equal(rig.Image.Record(0).Take(100), Read(rig.Device, 100, false, terminalCount: true));
        var result = Result(rig.Device);
        Assert.Equal(4, result[1]);
        Assert.Equal(0, result[2]);
        Assert.Equal(100, rig.Device.ReadBytes(0));
        Assert.False(rig.Device.Drq);
    }

    [Fact]
    public void ReadTrackWithoutAnyAddressMarkTimesOutInsteadOfEnteringADataTransfer()
    {
        using var rig = new Rig();
        rig.Position[0] = 9;
        Specify(rig.Device, false);
        Send(rig.Device, 0x42, 0, 9, 0, 0, 2, 4, 0x1b, 0xff);
        Assert.Equal(5, Result(rig.Device)[1]);
        Assert.Equal(0, rig.Device.ReadBytes(0));
        Assert.False(rig.Device.Drq);
    }

    [Fact]
    public void ReadTrackFollowsTheRecordedOrderInsteadOfSortingSectorNumbers()
    {
        using var image = new Disk(sectorSize: 512, recordSize: 512) { TrackOrder = [2, 0, 3, 1] };
        using var rig = new Rig(image);
        Specify(rig.Device, false);
        Send(rig.Device, 0x42, 0, 0, 0, 2, 2, 4, 0x1b, 0xff);
        var expected = image.TrackOrder.SelectMany(image.Record).ToArray();
        Assert.Equal(expected, Read(rig.Device, expected.Length, false));
        Result(rig.Device);
        Assert.Equal(2048, rig.Device.ReadBytes(0));
    }

    [Fact]
    public void RepeatedSectorNumbersSelectTheNextPhysicalOccurrenceAfterTheCurrentRotation()
    {
        using var image = new Disk(sectorSize: 512, recordSize: 512)
        {
            ReadTransform = sector => sector with { Number = sector.Number == 2 ? 1 : sector.Number }
        };
        using var rig = new Rig(image);
        Specify(rig.Device, false);
        rig.Device.Advance(70000);
        ReadCommand(rig.Device, 1, 3);
        Assert.Equal(image.Record(2), Read(rig.Device, 512, false, terminalCount: true));
        Assert.Equal(0, Result(rig.Device)[0]);
    }

    [Theory]
    [InlineData(9, 0x10)]
    [InlineData(0xff, 0x12)]
    public void TheRecordedCylinderIsNotReplacedWithThePhysicalTrackNumber(int recorded, int detail)
    {
        using var image = new Disk(sectorSize: 512, recordSize: 512)
        {
            ReadTransform = sector => sector with { Cylinder = recorded }
        };
        using var rig = new Rig(image);
        Specify(rig.Device, false);
        ReadCommand(rig.Device, 1, 3);
        var result = Result(rig.Device);
        Assert.Equal(4, result[1]);
        Assert.Equal(detail, result[2]);
        Assert.Equal(0, rig.Device.ReadBytes(0));
        Assert.Equal(0, rig.Position[0]);
    }

    [Fact]
    public void ReadIdReturnsTheRecordedSizeCodeWithoutReconstructingItFromTheAvailablePayloadLength()
    {
        using var image = new Disk(sectorSize: 512, recordSize: 512)
        {
            ReadTransform = sector => sector with { RecordedSizeCode = 3 }
        };
        using var rig = new Rig(image);
        Specify(rig.Device, false);
        Send(rig.Device, 0x4a, 0);
        var result = Result(rig.Device);
        Assert.Equal(0, result[0]);
        Assert.Equal(3, result[6]);
        Assert.Equal(0, rig.Device.ReadBytes(0));
    }

    private static void ScanBytes(Upd765 device, IEnumerable<byte> data, bool pio = false)
    {
        foreach (var value in data)
        {
            WaitByte(device, pio, writing: true);
            if (pio) device.WriteFifo(value); else device.WriteDma(value);
        }
    }

    private static void ReadCommand(Upd765 device, byte record, byte end) =>
        Send(device, 0x46, 0, 0, 0, record, 2, end, 0x1b, 0xff);

    private static byte[] Read(Upd765 device, int count, bool pio, bool terminalCount = false)
    {
        var data = new byte[count];
        for (var offset = 0; offset < data.Length; offset++)
        {
            WaitByte(device, pio, false);
            if (terminalCount && offset == data.Length - 1) device.SetTerminalCount(true);
            data[offset] = pio ? device.ReadFifo() : device.ReadDma();
        }
        return data;
    }

    private sealed class Rig : IDisposable
    {
        private readonly bool _ownsImage;
        internal readonly int[] Position = new int[4];
        internal readonly Disk Image;
        internal readonly Upd765 Device;
        internal bool Connected = true;

        internal Rig(Disk? image = null, Upd765Model model = Upd765Model.Upd765A)
        {
            _ownsImage = image is null;
            Image = image ?? new Disk(sectorSize: 512, recordSize: 512);
            Device = new(unit => unit == 0 && Connected ? Image : null, Status,
                (unit, direction) => Position[unit] = Math.Max(0, Position[unit] + direction),
                unit => Position[unit], boardClockHz: 1000000, model: model);
        }

        private byte Status(int unit) => (byte)((unit == 0 && Connected ? 0x20 : 0)
            | (Position[unit] == 0 ? 0x10 : 0) | 0x08 | (Image.IsReadOnly ? 0x40 : 0));

        public void Dispose()
        {
            try { Device.Reset(); }
            finally { if (_ownsImage) Image.Dispose(); }
        }
    }
}
