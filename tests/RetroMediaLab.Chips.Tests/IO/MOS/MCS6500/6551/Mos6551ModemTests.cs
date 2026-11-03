using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6551.Enums;
using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6551.Services;
using static RetroMediaLab.Chips.Tests.IO.MOS.MCS6500.Chip6551.Mos6551TestSignals;

namespace RetroMediaLab.Chips.Tests.IO.MOS.MCS6500.Chip6551;

public sealed class Mos6551ModemTests
{
    [Theory]
    [InlineData(Mos6551Variant.Mos6551, 2)]
    [InlineData(Mos6551Variant.Rockwell65C51, 0)]
    public void ResetIdentitiesAreManufacturerSpecific(Mos6551Variant variant, int reset)
    {
        var chip = new Mos6551(1, 0, variant);
        Assert.Equal(reset, chip.Read(2));
        Assert.Equal(0, chip.Read(3));
        Assert.True(chip.Dtr);
        Assert.True(chip.Rts);
        chip.Write(2, 0xff);
        chip.Write(3, 0xc5);
        chip.Write(1, 0);
        Assert.Equal(0xe0 | reset, chip.Read(2));
        Assert.Equal(0xc5, chip.Read(3));
        chip.Reset();
        Assert.Equal(reset, chip.Read(2));
    }

    [Theory]
    [InlineData(false, 0x20)]
    [InlineData(true, 0x40)]
    public void FirstModemTransitionIsFrozenUntilStatusReadAndTheSecondThenInterrupts(bool dsr, int mask)
    {
        var chip = Device();
        void Set(bool value) { if (dsr) chip.SetDsr(value); else chip.SetDcd(value); }
        Set(true);
        Set(false);
        Assert.True(chip.Irq);
        Assert.Equal(0x80 | mask, chip.Read(1) & (0x80 | mask));
        Assert.True(chip.Irq);
        Assert.Equal(0x80, chip.Read(1) & (0x80 | mask));
        Assert.False(chip.Irq);
    }

    [Fact]
    public void DisabledModemPinsFollowTheirLevelsWithoutInterruptingOrDiscardingUnreadData()
    {
        var chip = Device();
        Send(chip, 0x37);
        chip.Write(1, 0); // Program reset leaves the RX interrupt latched until status is read.
        Assert.True(chip.Irq);
        chip.SetDsr(true);
        chip.SetDcd(true);
        Assert.Equal(0xe8, chip.Read(1) & 0xef);
        Assert.False(chip.Irq);
        Assert.Equal(0x37, chip.Read(0));
        chip.SetDsr(false);
        chip.SetDcd(false);
        Assert.Equal(0, chip.Read(1) & 0xe0);
    }

    [Fact]
    public void AllRegisterMirrorsAndCommandBitsRoundTripWithoutAliasingTheDataRegister()
    {
        var chip = Device();
        for (var value = 0; value <= byte.MaxValue; value++)
        {
            chip.Write(6, (byte)value);
            chip.Write(7, (byte)~value);
            Assert.Equal(value, chip.Read(2));
            Assert.Equal((byte)~value, chip.Read(3));
            Assert.Equal((value & 1) == 0, chip.Dtr);
            Assert.Equal((value & 0x1c) == 0, chip.Rts);
        }
    }

    [Fact]
    public void HeldResetIgnoresWritesAndClockEdgesUntilReleased()
    {
        var chip = Device();
        chip.Write(0, 0x55);
        Tx(chip, 20);
        chip.SetReset(false);
        chip.Write(2, 9);
        chip.Write(3, 15);
        chip.Write(0, 0x53);
        Tx(chip, 200);
        Send(chip, 0x77);
        Assert.Equal(2, chip.Read(2));
        Assert.Equal(0, chip.Read(3));
        Assert.Equal(0x10, chip.Read(1));
        Assert.True(chip.Txd);
        chip.SetReset(true);
        chip.Write(2, 9);
        chip.Write(0, 0);
        Tx(chip);
        Assert.False(chip.Txd);
    }

    [Fact]
    public void ReceiveAndTransmitInterruptsAreLatchedUntilStatusAndResetRemovesAllCauses()
    {
        var chip = Device(0, 5);
        Tx(chip, 2);
        Send(chip, 0x53);
        chip.SetDsr(true);
        Assert.True(chip.Irq);
        Assert.Equal(0x53, chip.Read(0));
        Assert.True(chip.Irq);
        Assert.Equal(0xd0, chip.Read(1) & 0xd8);
        Assert.False(chip.Irq);
        chip.Reset();
        Tx(chip, 2);
        Assert.False(chip.Irq);
    }
}
