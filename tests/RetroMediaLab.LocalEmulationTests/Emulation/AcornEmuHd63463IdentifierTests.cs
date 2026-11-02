using System.IO;

namespace RetroMediaLab.Tests.Emulation;

public sealed partial class AcornEmuHd63463Tests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DataCommandsUseThePhysicalPositionOfTheMatchingRecordedIdentifier(bool write)
    {
        using var disk = new Disk
        {
            ReadTransform = sector => sector with { Cylinder = 0x4321, Head = 7, Number = 3 - sector.Number }
        };
        var controller = Controller(_ => disk);
        var parameters = Transfer(1, 0x4321, 7, 1, 1);
        parameters[1] = 0; // Physical head zero; logical head seven is recorded in its IDs.
        var untouched = disk.Block(1);
        var expected = write ? Enumerable.Repeat((byte)0xa5, 256).ToArray() : disk.Block(2);
        Issue(controller, write ? (byte)0x87 : (byte)0x40, parameters);
        if (write) Write(controller, expected);
        else Assert.Equal(expected, Read(controller));
        Assert.Equal(0x60, controller.Status);
        Assert.Equal(expected, disk.Block(2));
        Assert.Equal(untouched, disk.Block(1));
        Assert.Equal(0, controller.SeekCount(0));
    }

    [Fact]
    public void CanonicalGeometryCannotSubstituteForAnAbsentRecordedIdentifier()
    {
        using var disk = new Disk { ReadTransform = sector => sector with { Number = 3 } };
        var controller = SearchController(disk, 1000000, 1);
        Issue(controller, 0x40, Transfer(1, 0, 0, 1, 1));
        WaitIdle(controller);
        Assert.False(controller.Drq);
        Assert.Equal(0x58, controller.ReadParameter());
        Assert.Equal(0, controller.ReadBlocks(0));
    }

    [Theory]
    [InlineData(0x60)]
    [InlineData(0x61)]
    public void IdentifierCommandsReturnRecordedLabelsDespiteBadData(byte command)
    {
        using var disk = new Disk
        {
            ReadTransform = sector => sector with
            {
                Cylinder = 0x4321,
                Head = 7,
                Number = 0x90 + sector.Number,
                IntegrityValid = false
            }
        };
        var controller = Controller(_ => disk);
        Issue(controller, command, [1, 0, 0, 1, 0, 2]);
        WaitDrq(controller);
        var identifiers = ReadAvailable(controller, 512);
        Assert.Equal(new byte[] { 0x43, 0x21, 7, 0x91, 0x43, 0x21, 7, 0x92 }, identifiers[..8]);
        Assert.Equal(0x60, controller.Status);
        Assert.False(disk.Disposed);
    }

    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(65536, 0, 0)]
    [InlineData(0, 256, 0)]
    [InlineData(0, 0, 256)]
    public void IdentifierReadRejectsUnrepresentableLabelsInsteadOfTruncating(int cylinder, int head, int number)
    {
        using var disk = new Disk
        {
            ReadTransform = sector => sector with { Cylinder = cylinder, Head = head, Number = number }
        };
        var controller = Controller(_ => disk);
        Issue(controller, 0x60, [1, 0, 0, 0, 0, 1]);
        WaitIdle(controller);
        Assert.Equal(0x64, controller.Status);
        Assert.Equal(0x44, Results(controller, 2)[1]);
        Assert.False(controller.Drq);
    }

    [Theory]
    [InlineData(0x60)]
    [InlineData(0x61)]
    public void IdentifierMediaFailureBecomesControllerError(byte command)
    {
        using var disk = new Disk { ReadTransform = _ => throw new IOException() };
        var controller = Controller(_ => disk);
        Issue(controller, command, [1, 0, 0, 0, 0, 1]);
        WaitIdle(controller);
        Assert.Equal(0x64, controller.Status);
        Assert.Equal(0x44, Results(controller, 2)[1]);
        Assert.False(controller.Drq);
        Assert.False(disk.Disposed);
    }
}
