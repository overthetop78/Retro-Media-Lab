using RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Services;
using C = RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Constants.Intel8271Constants;

namespace RetroMediaLab.Chips.Tests.Controllers.Intel.FDC.Chip8271;

public sealed class RegisterTests
{
    [Fact]
    public void ResetHasFixedModeBitsAndNoPendingRequest()
    {
        var chip = new Intel8271();
        Assert.Equal(0, chip.Read(0));
        Assert.Equal(0xc0, Special(chip, 0x17));
        Assert.False(chip.Drq);
        Assert.False(chip.Nmi);
    }

    [Theory]
    [InlineData(0, 0xc0)]
    [InlineData(1, 0xc1)]
    [InlineData(2, 0xc2)]
    [InlineData(3, 0xc3)]
    public void ModeFixedBitsSurviveWrites(byte mode, byte expected)
    {
        var chip = new Intel8271();
        Command(chip, 0x3a, 0x17, mode);
        Assert.Equal(expected, Special(chip, 0x17));
    }

    [Theory]
    [InlineData(0x10, 0x12)]
    [InlineData(0x18, 0x1a)]
    public void SpecifyBadTracksLoadsThreeConsecutiveRegisters(byte first, byte track)
    {
        var chip = new Intel8271();
        Command(chip, 0x35, first, 4, 9, 17);
        Assert.Equal(4, Special(chip, first));
        Assert.Equal(9, Special(chip, (byte)(first + 1)));
        Assert.Equal(17, Special(chip, track));
        Assert.Equal(0, chip.Read(0));
    }

    [Fact]
    public void SpecifyTimingIsAlsoReadableAndWritableThroughSpecialRegisters()
    {
        var chip = new Intel8271();
        Command(chip, 0x35, 0x0d, 6, 7, 0xa8);
        Assert.Equal(6, Special(chip, 0x0d));
        Assert.Equal(7, Special(chip, 0x0e));
        Assert.Equal(0xa8, Special(chip, 0x0f));
        Command(chip, 0x3a, 0x0d, 20);
        Assert.Equal(20, Special(chip, 0x0d));
    }

    [Fact]
    public void ResetPinBlocksCommandsAndDmaUntilReleased()
    {
        var chip = new Intel8271();
        chip.Write(2, 1);
        Command(chip, 0x3a, 0x17, 1);
        chip.WriteBus(false, true, 0, 0xa5);
        for (var i = 0; i < 20; i++) chip.Tick();
        Assert.True(chip.ResetAsserted);
        Assert.Equal(0, chip.Read(0));
        Assert.False(chip.Nmi);
        Assert.False(chip.Drq);
        chip.Write(2, 0);
        Assert.False(chip.ResetAsserted);
        Assert.Equal(0xc0, Special(chip, 0x17));
    }

    [Theory]
    [InlineData(2_000_000, 16000)]
    [InlineData(4_000_000, 8000)]
    public void HeadLoadUsesFourMillisecondsAtStandardClockAndEightAtMini(int clock, int ticks)
    {
        var chip = new Intel8271(chipClock: clock);
        chip.SetReady(0, true);
        chip.SetTrackZero(0, true);
        Command(chip, 0x35, 0x0d, 0, 25, 0x11);
        Command(chip, 0x69, 0);
        for (var i = 1; i < ticks; i++) { chip.Tick(); Assert.False(chip.Nmi); }
        chip.Tick();
        Assert.True(chip.Nmi);
        Assert.Equal(0, chip.Read(1));
    }

    [Fact]
    public void BusIsHighImpedanceWithoutChipSelectOrDack()
    {
        var chip = new Intel8271();
        Assert.Null(chip.ReadBus(false, false, 0));
        chip.WriteBus(false, false, 0, 0x2c);
        Assert.Equal(0, chip.Read(0));
        chip.WriteBus(true, false, 0, 0x2c);
        Assert.Equal<byte?>(0xc0, chip.ReadBus(true, false, 0));
        chip.Tick();
        Assert.Equal<byte?>(0x10, chip.ReadBus(true, false, 0));
        Assert.False(chip.Nmi);
    }

    [Fact]
    public void CommandDuringParameterPhaseDoesNotReplaceTheAcceptedCommand()
    {
        var chip = new Intel8271();
        chip.Write(0, 0x3a);
        chip.Write(0, 0x2c);
        chip.Write(1, 0x17);
        chip.Write(1, 1);
        Assert.Equal(0xc1, Special(chip, 0x17));
    }

    private static byte Special(Intel8271 chip, byte address)
    {
        Command(chip, 0x3d, address);
        return chip.Read(1);
    }

    private static void Command(Intel8271 chip, byte command, params byte[] parameters)
    {
        chip.Write(0, command);
        foreach (var parameter in parameters) chip.Write(1, parameter);
    }

}
