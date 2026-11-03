using RetroMediaLab.Chips.IO.NationalSemiconductor.INS815x.Chip8154.Services;
using RetroMediaLab.Chips.IO.NationalSemiconductor.INS815x.Chip8154.Enums;

namespace RetroMediaLab.Chips.Tests.IO.NationalSemiconductor.INS815x.Chip8154;

public sealed partial class RamIoTests
{
    [Fact]
    public void EveryRamCellRetainsEveryByteAndHasOnlySevenAddressInputs()
    {
        var chip = new RamIo8154();
        for (var value = 0; value < 256; value++)
            for (var address = 0; address < 128; address++)
            {
                chip.WriteRam(address, (byte)value);
                Assert.Equal(value, chip.ReadRam(address));
                Assert.Equal(value, chip.ReadRam(address + 128));
            }
        chip.Reset();
        for (var address = 0; address < 128; address++) Assert.Equal(255, chip.ReadRam(address));
    }

    [Theory]
    [InlineData(0x20, 0x22, 0)]
    [InlineData(0x21, 0x23, 8)]
    public void EveryBitOperationUsesItsAddressAndIgnoresTheWrittenData(int port, int direction, int bitBase)
    {
        var chip = new RamIo8154();
        chip.WriteIo(direction, 255);
        for (var bit = 0; bit < 8; bit++)
        {
            chip.WriteIo(port, 0);
            chip.WriteIo(0x10 + bitBase + bit, 0);
            Assert.Equal(1 << bit, chip.ReadIo(port));
            Assert.Equal(0x80, chip.ReadIo(bitBase + bit));
            Assert.Equal(0x80, chip.ReadIo(0x10 + bitBase + bit));
            chip.WriteIo(bitBase + bit, 255);
            Assert.Equal(0, chip.ReadIo(port));
            Assert.Equal(0, chip.ReadIo(bitBase + bit));
        }
    }

    [Fact]
    public void AllDirectionsCombinePadsAndLatchesWithoutLosingWritesToInputs()
    {
        var chip = new RamIo8154();
        byte pins = 255;
        chip.ReadPortA = () => pins;
        for (var direction = 0; direction < 256; direction++)
        {
            chip.WriteIo(0x22, (byte)direction);
            for (var output = 0; output < 256; output++)
            {
                chip.WriteIo(0x20, (byte)output);
                pins = (byte)(output ^ 0x5a);
                Assert.Equal((output | (~direction & 255)) & pins, chip.ReadIo(0x20));
                Assert.Equal(output, chip.OutputA);
                Assert.Equal(direction, chip.PortADriveMask);
            }
        }
    }

    [Fact]
    public void DirectionAndModeReadsAreHighImpedanceAndDoNotExposeInternalLatches()
    {
        var chip = new RamIo8154();
        foreach (var address in new[] { 0x22, 0x23, 0x24, 0x25, 0x40, 0x7f })
        {
            chip.WriteIo(address, 0x7f);
            Assert.Equal(255, chip.ReadIo(address));
            Assert.Null(chip.SetBus(false, true, false, false, true, address, 0));
            chip.SetBus(false, true, false, true, true, address, 0);
        }
    }

    [Fact]
    public void AllModeBytesDecodeOnlyTheThreeConnectedControlBits()
    {
        var chip = new RamIo8154();
        for (var value = 0; value < 256; value++)
        {
            chip.WriteIo(0x24, (byte)value);
            var expected = (value & 0x20) == 0 ? RamIoMode.Basic
                : (value & 0x40) == 0 ? RamIoMode.StrobedInput
                : (value & 0x80) == 0 ? RamIoMode.StrobedOutput : RamIoMode.StrobedOutputThreeState;
            Assert.Equal(expected, chip.OperatingMode);
            Assert.Equal(value, chip.Mode);
        }
    }

    [Fact]
    public void ResetMaintainsRamAndInputLevelsWhileInhibitingEveryBusAccess()
    {
        var chip = new RamIo8154();
        chip.WriteRam(127, 0xa5);
        chip.WriteIo(0x22, 255);
        chip.WriteIo(0x20, 0x5a);
        chip.SetStrobe(false);
        chip.SetReset(false);
        chip.WriteRam(127, 0x11);
        chip.WriteIo(0x22, 255);
        chip.WriteIo(0x24, 0x60);
        Assert.Equal(255, chip.ReadRam(127));
        Assert.Null(chip.SetBus(false, true, true, false, true, 127, 0));
        Assert.Equal(0, chip.PortADriveMask);
        Assert.Equal(0, chip.PortBDriveMask);
        Assert.Equal(0, chip.OutputA);
        Assert.Equal(0, chip.OutputB);
        Assert.Equal(RamIoMode.Basic, chip.OperatingMode);
        Assert.False(chip.StrobeHigh);
        Assert.False(chip.Irq);
        chip.SetReset(true);
        Assert.Equal(0xa5, chip.ReadRam(127));
        Assert.Equal(0x7f, chip.ReadIo(0x21));
    }

    [Fact]
    public void PortObserversPreserveRepeatedWritesWhileDriveCallbacksReportOnlyTransitions()
    {
        var chip = new RamIo8154();
        var levels = new List<byte>();
        var drives = new List<(byte Value, byte Mask)>();
        chip.PortAPinsChanged = levels.Add;
        chip.PortADriveChanged = (value, mask) => drives.Add((value, mask));
        chip.WriteIo(0x20, 0xff);
        Assert.Equal(new byte[] { 255 }, levels);
        Assert.Empty(drives);
        chip.WriteIo(0x22, 255);
        Assert.Equal(new byte[] { 255, 255 }, levels); // Direction changed, level did not.
        chip.WriteIo(0x20, 0x55);
        chip.WriteIo(0x20, 0x55);
        Assert.Equal(new byte[] { 255, 255, 0x55, 0x55 }, levels);
        Assert.Equal(new[] { ((byte)255, (byte)255), ((byte)0x55, (byte)255) }, drives);
        chip.Reset();
        Assert.Equal(((byte)255, (byte)0), drives[^1]);
    }

    [Fact]
    public void ResetObserversReadBothPortsWithTheCompletedInputDirections()
    {
        var chip = new RamIo8154();
        chip.WriteIo(0x22, 255);
        chip.WriteIo(0x23, 255);
        chip.WriteIo(0x20, 0);
        chip.WriteIo(0x21, 0);
        var observed = new List<(byte A, byte B)>();
        chip.PortADriveChanged = (_, _) => observed.Add((chip.ReadIo(0x20), chip.ReadIo(0x21)));
        chip.Reset();
        Assert.Equal(((byte)255, (byte)255), observed.Single());
    }
}
