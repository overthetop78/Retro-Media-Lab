using RetroMediaLab.Chips.IO.NationalSemiconductor.INS815x.Chip8154.Services;

namespace RetroMediaLab.Chips.Tests.IO.NationalSemiconductor.INS815x.Chip8154;

public sealed partial class RamIoTests
{
    [Fact]
    public void InputLatchesAtTheEndOfStrobeAndAcknowledgesOnlyAtTheEndOfByteRead()
    {
        var chip = Strobed(0x20);
        byte input = 0x12;
        chip.ReadPortA = () => input;
        Assert.Equal(0, chip.ReadIo(0x21) & 0xc0);
        chip.SetStrobe(false);
        Assert.Equal(0x40, chip.ReadIo(0x21) & 0xc0);
        Assert.False(chip.Irq);
        input = 0xa5;
        Assert.Equal(0x80, chip.ReadIo(0)); // Input latch is transparent while STB is low.
        chip.SetStrobe(true);
        Assert.True(chip.Irq);
        input = 0x5a;
        Assert.Equal((byte?)0xa5, BusRead(chip, 0x20));
        Assert.True(chip.Irq);
        Assert.Equal(0xc0, chip.ReadIo(0x21) & 0xc0);
        Assert.Equal((byte?)0xa5, BusRead(chip, 0x20));
        EndBus(chip);
        Assert.False(chip.Irq);
        Assert.Equal(0, chip.ReadIo(0x21) & 0xc0);
    }

    [Fact]
    public void ASecondPeripheralStrobeOverwritesTheUnreadInputWithoutSelection()
    {
        var chip = Strobed(0x20);
        byte input = 0x39;
        chip.ReadPortA = () => input;
        chip.SetStrobe(false);
        chip.SetStrobe(true);
        input = 0x72;
        chip.SetStrobe(false);
        chip.SetStrobe(true);
        Assert.Equal(0x72, chip.ReadIo(0x20));
        Assert.False(chip.Irq);
    }

    [Fact]
    public void BitOperationsAndOtherPortsDoNotAcknowledgeInputHandshakes()
    {
        var chip = Strobed(0x20);
        chip.ReadPortA = () => 0xa5;
        chip.SetStrobe(false);
        chip.SetStrobe(true);
        chip.WriteIo(0x20, 0xff);
        chip.WriteIo(0x10, 0);
        chip.ReadIo(0);
        chip.ReadIo(0x10);
        chip.WriteIo(0x21, 0);
        Assert.True(chip.Irq);
        Assert.Equal(0xc0, chip.ReadIo(0x21) & 0xc0);
        Assert.Equal(0x80, chip.ReadIo(0x0f)); // PB7 read is INTR, not its external STB level.
        Assert.Equal(0xa5, chip.ReadIo(0x20));
        Assert.False(chip.Irq);
    }

    [Theory]
    [InlineData(0x20)]
    [InlineData(0x60)]
    [InlineData(0xe0)]
    public void IeGatesTheStoredRequestAndByteWritesCannotOverwriteHandshakeLatches(int mode)
    {
        var chip = Strobed((byte)mode, enable: false);
        chip.SetStrobe(false);
        chip.SetStrobe(true);
        Assert.False(chip.Irq);
        chip.WriteIo(0x1f, 0); // Set PB7 IE regardless of data bus.
        Assert.True(chip.Irq);
        chip.WriteIo(0x21, 0);
        Assert.True(chip.Irq);
        Assert.Equal(0xc0, chip.ReadIo(0x21) & 0xc0);
        chip.WriteIo(0x0f, 255);
        Assert.False(chip.Irq);
        chip.WriteIo(0x1f, 0);
        Assert.True(chip.Irq);
        chip.WriteIo(0x0e, 255); // Software may override IBF/OBF separately.
        Assert.Equal(0x80, chip.ReadIo(0x21) & 0xc0);
        Assert.True(chip.Irq);
        chip.WriteIo(0x1e, 0);
        Assert.Equal(0xc0, chip.ReadIo(0x21) & 0xc0);
    }

    [Theory]
    [InlineData(0x60)]
    [InlineData(0xe0)]
    public void OutputDataIsLatchedAtWriteStartButFlagsClearAtWriteEnd(int mode)
    {
        var chip = Strobed((byte)mode);
        Assert.True(chip.Irq);
        Assert.Equal(0xc0, chip.ReadIo(0x21) & 0xc0);
        chip.SetBus(false, true, false, true, false, 0x20, 0xa5);
        Assert.Equal(0xa5, chip.OutputA);
        Assert.True(chip.Irq);
        Assert.Equal(0xc0, chip.ReadIo(0x21) & 0xc0);
        EndBus(chip);
        Assert.False(chip.Irq);
        Assert.Equal(0, chip.ReadIo(0x21) & 0xc0);
        chip.SetStrobe(false);
        Assert.Equal(0x40, chip.ReadIo(0x21) & 0xc0);
        Assert.False(chip.Irq);
        chip.SetStrobe(true);
        Assert.Equal(0xc0, chip.ReadIo(0x21) & 0xc0);
        Assert.True(chip.Irq);
    }

    [Fact]
    public void ThreeStateOutputsAreDrivenOnlyWhileAckIsLowAndRespectOdra()
    {
        var chip = Strobed(0xe0);
        var drives = new List<(byte Value, byte Mask)>();
        chip.PortADriveChanged = (value, mask) => drives.Add((value, mask));
        chip.WriteIo(0x22, 0xf0);
        chip.WriteIo(0x20, 0x5a);
        Assert.Equal(0, chip.PortADriveMask);
        Assert.Empty(drives);
        chip.SetStrobe(false);
        Assert.Equal(0xf0, chip.PortADriveMask);
        Assert.Equal(0x5f, chip.ReadIo(0x20));
        Assert.Equal(((byte)0x5f, (byte)0xf0), drives.Single());
        chip.SetStrobe(true);
        Assert.Equal(0, chip.PortADriveMask);
        Assert.Equal(((byte)255, (byte)0), drives[^1]);
    }

    [Fact]
    public void ModeThreeNeverReleasesItsOutputDataWhenAckChanges()
    {
        var chip = Strobed(0x60);
        chip.WriteIo(0x20, 0x5a);
        Assert.Equal(255, chip.PortADriveMask);
        Assert.Equal(0x5a, chip.ReadIo(0x20));
        chip.SetStrobe(false);
        Assert.Equal(0x5a, chip.ReadIo(0x20));
        chip.SetStrobe(true);
        Assert.Equal(0x5a, chip.ReadIo(0x20));
    }

    [Theory]
    [InlineData(0x60)]
    [InlineData(0xe0)]
    public void OutputReadsAndBitWritesDoNotAcknowledgeOutputHandshakes(int mode)
    {
        var chip = Strobed((byte)mode);
        chip.ReadIo(0x20);
        chip.ReadIo(0);
        chip.WriteIo(0x10, 0);
        chip.WriteIo(0x21, 0xff);
        Assert.True(chip.Irq);
        Assert.Equal(0xc0, chip.ReadIo(0x21) & 0xc0);
        chip.WriteIo(0x20, 0x39);
        Assert.False(chip.Irq);
        Assert.Equal(0, chip.ReadIo(0x21) & 0xc0);
    }

    [Theory]
    [InlineData(0x20, 0)]
    [InlineData(0x60, 0xc0)]
    [InlineData(0xe0, 0xc0)]
    public void EveryModeWriteReinitializesItsHandshakeStateWithoutClearingIe(int mode, int flags)
    {
        var chip = Strobed((byte)mode);
        chip.SetStrobe(false);
        chip.SetStrobe(true);
        chip.WriteIo(0x24, (byte)mode);
        Assert.Equal(flags, chip.ReadIo(0x21) & 0xc0);
        chip.WriteIo(0x24, 0);
        Assert.False(chip.Irq);
        Assert.Equal(0x80, chip.OutputB & 0x80); // IE becomes the ordinary B7 output latch again.
    }

    [Fact]
    public void IrqCallbackSeesTheCompletedStatusAndCanReadTheCapturedByte()
    {
        var chip = Strobed(0x20);
        chip.ReadPortA = () => 0x69;
        var seen = new List<(bool Irq, byte Flags)>();
        chip.InterruptChanged = high => seen.Add((high, (byte)(chip.ReadIo(0x21) & 0xc0)));
        chip.SetStrobe(false);
        chip.SetStrobe(true);
        Assert.Equal((true, (byte)0xc0), seen.Single());
        Assert.Equal(0x69, chip.ReadIo(0x20));
        Assert.Equal((false, (byte)0), seen[^1]);
    }

    private static RamIo8154 Strobed(byte mode, bool enable = true)
    {
        var chip = new RamIo8154();
        chip.WriteIo(0x22, mode == 0x20 ? (byte)0 : (byte)255);
        chip.WriteIo(0x23, 0x40);
        if (enable) chip.WriteIo(0x1f, 0);
        chip.WriteIo(0x24, mode);
        return chip;
    }

    private static byte? BusRead(RamIo8154 chip, int address) => chip.SetBus(false, true, false, false, true, address, 0);
    private static void EndBus(RamIo8154 chip) => chip.SetBus(false, true, false, true, true, 0, 0);
}
