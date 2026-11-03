using RetroMediaLab.Chips.IO.NationalSemiconductor.INS815x.Chip8154.Services;

namespace RetroMediaLab.Chips.Tests.IO.NationalSemiconductor.INS815x.Chip8154;

public sealed partial class RamIoTests
{
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    public void ChipSelectsAndMemoryIoSelectMatchTheTruthTable(bool cs0, bool cs1, bool selected)
    {
        var chip = new RamIo8154();
        chip.WriteRam(0x20, 0xa5);
        Assert.Equal(selected ? (byte?)0xa5 : null, chip.SetBus(cs0, cs1, true, false, true, 0x20, 0));
        chip.SetBus(cs0, cs1, true, true, true, 0x20, 0);
        Assert.Equal(selected ? (byte?)255 : null, chip.SetBus(cs0, cs1, false, false, true, 0x20, 0));
    }

    [Fact]
    public void BasicReadCapturesThePadsOnceUntilNrdsRises()
    {
        var chip = new RamIo8154();
        byte input = 0x39;
        chip.ReadPortA = () => input;
        Assert.Equal((byte?)0x39, BusRead(chip, 0x20));
        input = 0xa5;
        Assert.Equal((byte?)0x39, BusRead(chip, 0x20));
        EndBus(chip);
        Assert.Equal((byte?)0xa5, BusRead(chip, 0x20));
    }

    [Fact]
    public void BasicOutputIsLatchedAtTheLeadingWriteEdgeAndNotRepeatedDuringThePulse()
    {
        var chip = new RamIo8154();
        chip.WriteIo(0x22, 255);
        var seen = new List<byte>();
        chip.PortAPinsChanged = seen.Add;
        chip.SetBus(false, true, false, true, false, 0x20, 0x55);
        Assert.Equal(0x55, chip.OutputA);
        Assert.Equal(new byte[] { 0x55 }, seen);
        chip.SetBus(false, true, false, true, false, 0x20, 0xa5);
        Assert.Equal(0x55, chip.OutputA);
        EndBus(chip);
        Assert.Equal(new byte[] { 0x55 }, seen);
    }

    [Fact]
    public void ResetMidReadCancelsTheCycleWithoutAStaleTrailingAcknowledgment()
    {
        var chip = Strobed(0x20);
        chip.ReadPortA = () => 0x55;
        chip.SetStrobe(false);
        chip.SetStrobe(true);
        Assert.Equal((byte?)0x55, BusRead(chip, 0x20));
        chip.SetReset(false);
        Assert.Null(BusRead(chip, 0x20));
        chip.SetReset(true);
        EndBus(chip);
        Assert.False(chip.Irq);
        Assert.Equal(0, chip.Mode);
    }

    [Fact]
    public void DeselectedOrUndefinedBusOperationsDoNotModifyRamOrPorts()
    {
        var chip = new RamIo8154();
        chip.WriteRam(0, 0x39);
        Assert.Null(chip.SetBus(true, true, true, true, false, 0, 0x55));
        EndBus(chip);
        Assert.Null(chip.SetBus(false, true, true, false, false, 0, 0xa5));
        EndBus(chip);
        Assert.Equal(0x39, chip.ReadRam(0));
        Assert.Equal(0, chip.OutputA);
    }
}
