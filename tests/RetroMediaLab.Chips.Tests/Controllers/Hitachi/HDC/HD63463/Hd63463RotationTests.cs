using RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Services;
using System.Buffers.Binary;
namespace RetroMediaLab.Chips.Tests.Controllers.Hitachi.HDC.HD63463;

public sealed partial class Hd63463Tests
{
    [Theory]
    [InlineData(0x40, 0, 164109)]
    [InlineData(0x40, 1, 8205)]
    [InlineData(0x48, 0, 164109)]
    public void OrdinaryDataWaitsForACompleteIdThenTheDataField(byte command, byte sector, int completion)
    {
        using var disk = new Disk();
        var controller = RotatingController(disk);
        RawRotationCommand(controller, command, Transfer(1, 0, 0, sector, 1));
        controller.Advance(completion - 440 - 1);
        Assert.True(controller.Busy);
        Assert.False(controller.Drq);
        Assert.Equal(0, controller.ReadBlocks(0));
        controller.Advance(1);
        Assert.Equal(1, controller.ReadBlocks(0));
        if (command == 0x40)
        {
            Assert.True(controller.Drq);
            Assert.Equal(disk.Block(sector), ReadAvailable(controller, 256));
        }
        Assert.Equal(0x60, controller.Status);
    }

    [Fact]
    public void DataWriteUsesTheSamePhysicalRecordCadenceAsReadAndFormat()
    {
        using var disk = new Disk();
        var controller = RotatingController(disk);
        RawRotationCommand(controller, 0x87, Transfer(1, 0, 0, 0, 2));
        controller.Advance(1);
        Assert.True(controller.Drq);
        WriteAvailable(controller, Enumerable.Repeat((byte)0x79, 512).ToArray());
        controller.Advance(164108 - 441);
        Assert.Equal(0, controller.WrittenBlocks(0));
        controller.Advance(1);
        Assert.Equal(1, controller.WrittenBlocks(0));
        controller.Advance(4095);
        Assert.Equal(1, controller.WrittenBlocks(0));
        controller.Advance(1);
        Assert.Equal(2, controller.WrittenBlocks(0));
        Assert.Equal(0x60, controller.Status);
        Assert.All(disk.Block(0).Concat(disk.Block(1)), value => Assert.Equal(0x79, value));
    }

    [Fact]
    public void SlowReadDmaRetriesTheMissedDataFieldOnTheNextRevolution()
    {
        using var disk = new Disk();
        var controller = RotatingController(disk);
        RawRotationCommand(controller, 0x40, Transfer(1, 0, 0, 0, 3));
        controller.Advance(173000 - 440);
        Assert.Equal(2, controller.ReadBlocks(0));
        Assert.Equal(disk.Block(0).Concat(disk.Block(1)), ReadAvailable(controller, 512));
        controller.Advance(332300 - 173000);
        Assert.Equal(2, controller.ReadBlocks(0));
        Assert.False(controller.Drq);
        controller.Advance(1);
        Assert.Equal(3, controller.ReadBlocks(0));
        Assert.Equal(disk.Block(2), ReadAvailable(controller, 256));
        Assert.Equal(0x60, controller.Status);
    }

    [Fact]
    public void SlowWriteDmaDoesNotStartADataFieldWithoutItsFirstBuffer()
    {
        using var disk = new Disk();
        var untouched = disk.Block(1);
        var controller = RotatingController(disk);
        RawRotationCommand(controller, 0x87, Transfer(1, 0, 0, 0, 2));
        controller.Advance(1);
        WriteAvailable(controller, Enumerable.Repeat((byte)0x31, 256).ToArray());
        controller.Advance(170000 - 441);
        Assert.Equal(1, controller.WrittenBlocks(0));
        Assert.Equal(untouched, disk.Block(1));
        WriteAvailable(controller, Enumerable.Repeat((byte)0x62, 256).ToArray());
        controller.Advance(328204 - 170000);
        Assert.Equal(1, controller.WrittenBlocks(0));
        controller.Advance(1);
        Assert.Equal(2, controller.WrittenBlocks(0));
        Assert.All(disk.Block(1), value => Assert.Equal(0x62, value));
    }

    [Fact]
    public void ErroneousReadCanCatchDataAfterItsIdHasAlreadyPassed()
    {
        using var disk = new Disk();
        var controller = RotatingController(disk);
        RawRotationCommand(controller, 0x70, [1, 0, 0, 1]);
        controller.Advance(4108 - 440);
        Assert.False(controller.Drq);
        controller.Advance(1);
        Assert.Equal(disk.Block(0), ReadAvailable(controller, 256));
        Assert.Equal(new byte[] { 0, 0, 1, 0, 0, 0 }, Results(controller, 6));
    }

    [Fact]
    public void CrossingToTheNextHeadWaitsForItsFirstPhysicalId()
    {
        using var disk = new Disk();
        var controller = RotatingController(disk);
        RawRotationCommand(controller, 0x40, Transfer(1, 0, 0, 3, 2));
        controller.Advance(16397 - 440);
        Assert.Equal(disk.Block(3), ReadAvailable(controller, 256));
        controller.Advance(164108 - 16397);
        Assert.False(controller.Drq);
        controller.Advance(1);
        Assert.Equal(disk.Block(4), ReadAvailable(controller, 256));
        Assert.Equal(0x60, controller.Status);
    }

    [Theory]
    [InlineData(0x60, 0, 160500)]
    [InlineData(0x61, 2, 168692)]
    public void IdentifierReadWaitsForIndexAndTheCompleteIdField(byte command, byte offset, int completion)
    {
        using var disk = new Disk();
        var controller = RotatingController(disk);
        RawRotationCommand(controller, command, [1, 0, 0, offset, 0, 1]);
        controller.Advance(completion - 440 - 1);
        Assert.True(controller.Busy);
        Assert.False(controller.Drq);
        controller.Advance(1);
        Assert.True(controller.Drq);
        Assert.Equal(0, controller.ReadDma());
        Assert.Equal((ushort)(offset << 8), controller.ReadDma(done: true));
        Assert.Equal(0x60, controller.Status);
    }

    [Fact]
    public void FormatWaitsForIndexAndFinishesAfterGap4AtTheFollowingIndex()
    {
        using var disk = new Disk();
        var controller = RotatingController(disk);
        RawRotationCommand(controller, 0xa3, [1, 0, 0, 4]);
        controller.Advance(40000);
        var identifiers = FormatIdentifiers(0, 0, 4);
        for (var offset = 0; offset < 16; offset += 2)
            controller.WriteDma(BinaryPrimitives.ReadUInt16LittleEndian(identifiers.AsSpan(offset)), offset == 14);
        controller.Advance(160000 - 40440);
        Assert.Equal(0, controller.WrittenBlocks(0));
        controller.Advance(4108);
        Assert.Equal(0, controller.WrittenBlocks(0));
        controller.Advance(1); // 321 decoded bytes from index through the first DATA PAD.
        Assert.Equal(1, controller.WrittenBlocks(0));
        controller.Advance(320038 - 164109);
        Assert.Equal(4, controller.WrittenBlocks(0));
        Assert.True(controller.Busy);
        controller.Advance(1);
        Assert.Equal(0x60, controller.Status);
    }

    [Fact]
    public void ControllerResetDoesNotRestartThePhysicalSpindle()
    {
        using var disk = new Disk();
        var controller = RotatingController(disk);
        controller.Advance(100000);
        controller.Reset();
        controller.Advance(150);
        SpecifyRotatingController(controller);
        RawRotationCommand(controller, 0x60, [1, 0, 0, 0, 0, 1]);
        controller.Advance(160499 - 100840);
        Assert.False(controller.Drq);
        controller.Advance(1);
        Assert.True(controller.Drq);
    }

    [Fact]
    public void DrivesKeepIndependentSpindleRates()
    {
        using var disk = new Disk();
        var controller = RotatingController(disk);
        controller.Configure(1, 0, 100, rotationsPerMinute: 1500);
        RawRotationCommand(controller, 0x60, [1, 0, 0, 0, 0, 1]);
        controller.Advance(160500 - 440);
        Assert.True(controller.Drq);
        controller.DmaDone();
        RawRotationCommand(controller, 0x60, [2, 0, 0, 0, 0, 1]);
        // Changing RPM preserves angle; unit 1 had already rotated 400 cycles
        // at 3000 RPM before its rate was changed, so its index is at 319600.
        controller.Advance(320099 - 160540);
        Assert.False(controller.Drq);
        controller.Advance(1);
        Assert.True(controller.Drq);
    }

    [Fact]
    public void BulkAdvanceAndIndividualClocksProduceTheSameFormattedTrackAndNextId()
    {
        using var first = new Disk();
        using var second = new Disk();
        var bulk = RotatingController(first);
        var clocked = RotatingController(second);
        foreach (var controller in new[] { bulk, clocked })
        {
            RawRotationCommand(controller, 0xa3, [1, 0, 0, 4]);
            controller.Advance(40000);
            var identifiers = FormatIdentifiers(0, 0, 4);
            for (var offset = 0; offset < 16; offset += 2)
                controller.WriteDma(BinaryPrimitives.ReadUInt16LittleEndian(identifiers.AsSpan(offset)), offset == 14);
        }
        bulk.Advance(280000);
        for (var tick = 0; tick < 280000; tick++) clocked.Advance(1);
        Assert.Equal(bulk.Status, clocked.Status);
        Assert.Equal(4, bulk.WrittenBlocks(0));
        Assert.Equal(bulk.WrittenBlocks(0), clocked.WrittenBlocks(0));
        for (var sector = 0; sector < 4; sector++) Assert.Equal(first.Record(sector), second.Record(sector));
        RawRotationCommand(bulk, 0x60, [1, 0, 0, 0, 0, 1]);
        RawRotationCommand(clocked, 0x60, [1, 0, 0, 0, 0, 1]);
        bulk.Advance(160019);
        for (var tick = 0; tick < 160019; tick++) clocked.Advance(1);
        Assert.Equal(bulk.Drq, clocked.Drq);
        Assert.Equal(bulk.ReadDma(done: true), clocked.ReadDma(done: true));
    }

    private static Hd63463 RotatingController(Disk disk)
    {
        var controller = new Hd63463(_ => disk, 8000000);
        controller.Configure(0, 0, 100, rotationsPerMinute: 3000);
        controller.Configure(1, 0, 100, rotationsPerMinute: 3000);
        controller.Advance(150);
        SpecifyRotatingController(controller);
        return controller;
    }

    private static void SpecifyRotatingController(Hd63463 controller)
    {
        byte[] specification = [0x1f, 0x82, 0, 6, 16, 2, 1, 3, 0x21, 10, 13, 12, 0, 0, 0, 0];
        for (var offset = 0; offset < specification.Length; offset += 2)
            controller.WriteParameter(BinaryPrimitives.ReadUInt16BigEndian(specification.AsSpan(offset)));
        controller.WriteCommand(0xe8);
        controller.Advance(250);
    }

    private static void RawRotationCommand(Hd63463 controller, byte command, byte[] parameters)
    {
        controller.WriteCommand(8);
        controller.Advance(40);
        for (var offset = 0; offset < parameters.Length; offset += 2)
            controller.WriteParameter(BinaryPrimitives.ReadUInt16BigEndian(parameters.AsSpan(offset)));
        controller.WriteCommand(command);
    }
}
