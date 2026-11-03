using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Constants;
using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Enums;
using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Services;
using static RetroMediaLab.Chips.Tests.Shared.Upd765TestCommands;

namespace RetroMediaLab.Chips.Tests.Controllers.NEC.UPD765.UPD765;

public sealed class BusTests
{
    [Theory]
    [InlineData(Upd765Model.Upd765A, 0x80)]
    [InlineData(Upd765Model.Upd765B, 0x90)]
    [InlineData(Upd765Model.Upd72065B, 0x90)]
    public void VersionCommandTraversesTheSelectedBus(Upd765Model model, byte version)
    {
        var chip = Device(model);
        Write(chip, 0x10);
        Assert.Equal(0x10, chip.ReadMainStatus());
        Event(chip);
        Assert.Equal(0xd0, chip.ReadMainStatus());
        Assert.Equal(version, Read(chip));
        Assert.Null(chip.BusData);
        Assert.Equal(0x80, chip.ReadMainStatus());
    }

    [Fact]
    public void DataIsHighImpedanceOutsideOneValidSelectedRead()
    {
        var chip = Device();
        chip.SetBus(false, true, false, false);
        Assert.Null(chip.BusData);
        chip.SetBus(true, true, true, false);
        Assert.Null(chip.BusData);
        chip.SetBus(true, false, false, false);
        Assert.Null(chip.BusData);
        chip.SetBus(true, true, false, false);
        Assert.Equal((byte)0x80, chip.BusData);
        chip.SetBus(false, true, false, false);
        Assert.Null(chip.BusData);
        Assert.Equal(0x80, chip.ReadMainStatus());
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void UnselectedOrStatusRegisterWritesDoNotAcceptACommand(bool selected, bool data)
    {
        var chip = Device();
        chip.SetBus(selected, false, true, data, value: 0x10);
        chip.SetBus(selected, false, false, data);
        chip.Advance(100);
        Assert.Equal(0x80, chip.ReadMainStatus());
    }

    [Fact]
    public void ExtendedWriteSamplesTheFinalActiveDataOnceAtTheTrailingEdge()
    {
        var chip = Device(Upd765Model.Upd765B);
        chip.SetBus(true, false, true, true, value: 0xff);
        chip.Advance(100);
        Assert.Equal(0x80, chip.ReadMainStatus());
        chip.SetBus(true, false, true, true, value: 0x10);
        chip.SetBus(true, false, true, true, value: 0x10);
        chip.Advance(100);
        Assert.Equal(0x80, chip.ReadMainStatus());
        chip.SetBus(true, false, false, true);
        chip.SetBus(true, false, false, true);
        Event(chip);
        Assert.Equal(0x90, Read(chip));
        Assert.Equal(0x80, chip.ReadMainStatus());
    }

    [Fact]
    public void LosingChipSelectBeforeTheStrobeEndsDoesNotInventAWrite()
    {
        var chip = Device();
        chip.SetBus(true, false, true, true, value: 0x10);
        chip.SetBus(false, false, true, true, value: 0x10);
        chip.SetBus(false, false, false, true);
        Assert.Equal(0x80, chip.ReadMainStatus());
    }

    [Fact]
    public void AStretchedResultReadConsumesOneByteOnlyAtTheTrailingEdge()
    {
        var position = 0;
        var chip = new Upd765(
            _ => (byte)(Upd765Constants.ReadyPin | (position == 0 ? Upd765Constants.TrackZeroPin : 0)),
            (_, direction) => position += direction, clockFrequency: 1_000_000);
        Specify(chip, false);
        Send(chip, 0x0f, 0, 3);
        Wait(chip, () => chip.Irq);
        Send(chip, 8);
        chip.SetBus(true, true, false, true);
        Assert.Equal((byte)0x20, chip.BusData);
        chip.Advance(100);
        chip.SetBus(true, true, false, true);
        Assert.Equal((byte)0x20, chip.BusData);
        Assert.Equal(0xd0, chip.ReadMainStatus());
        chip.SetBus(true, false, false, true);
        Assert.Null(chip.BusData);
        Assert.Equal(0x50, chip.ReadMainStatus());
        Wait(chip, () => (chip.ReadMainStatus() & 0xf0) == 0xd0);
        Assert.Equal(3, Read(chip));
        Assert.Equal(0x80, chip.ReadMainStatus());
    }

    [Fact]
    public void StatusReadDoesNotAcknowledgeTheResultRegister()
    {
        var chip = Device();
        Send(chip, 0xff);
        chip.SetBus(true, true, false, false);
        Assert.Equal((byte)0xd0, chip.BusData);
        chip.SetBus(true, false, false, false);
        Assert.Equal(0x80, Read(chip));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DackForcesTheDataPathAndCannotSubmitACpuCommand(bool chipSelect)
    {
        var chip = Device();
        chip.SetBus(chipSelect, false, true, false, dmaAcknowledge: true, value: 0x10);
        chip.SetBus(chipSelect, false, false, false, dmaAcknowledge: true);
        Assert.Equal(0x80, chip.ReadMainStatus());
        chip.SetBus(chipSelect, true, false, false, dmaAcknowledge: true);
        Assert.Equal((byte)0xff, chip.BusData);
        chip.SetBus(chipSelect, false, false, false, dmaAcknowledge: true);
        Assert.Null(chip.BusData);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MaintainedResetCancelsThePendingStrobeAndBlocksAllAccesses(bool writing)
    {
        var chip = Device();
        Send(chip, 0xff);
        chip.SetBus(true, !writing, writing, true, value: 0x10);
        chip.SetReset(true);
        chip.SetBus(true, false, false, true);
        chip.SetBus(true, true, false, false);
        chip.WriteFifo(0x10);
        chip.WriteDma(0x10);
        chip.SetTerminalCount(true);
        chip.Advance(1_000_000);
        Assert.Null(chip.BusData);
        Assert.Equal(0, chip.ReadMainStatus());
        Assert.Equal(0xff, chip.ReadFifo());
        Assert.Equal(0xff, chip.ReadDma());
        Assert.False(chip.Irq);
        Assert.False(chip.Drq);
        chip.SetReset(false);
        chip.SetBus(true, false, false, true);
        Assert.Equal(0x80, chip.ReadMainStatus());
        Write(chip, 0x10);
        Event(chip);
        Assert.Equal(0x80, Read(chip));
    }

    [Fact]
    public void ResetFromTheInterruptCallbackCannotLeakThePendingWrite()
    {
        var chip = Device();
        chip.InterruptChanged = active => { if (active) chip.SetReset(true); };
        Send(chip, 7, 0);
        Assert.Null(chip.BusData);
        Assert.False(chip.Irq);
        Assert.False(chip.Drq);
        Assert.Equal(0, chip.ReadMainStatus());
        chip.InterruptChanged = null;
        chip.SetReset(false);
        Assert.Equal(0x80, chip.ReadMainStatus());
    }

    [Fact]
    public void PhysicalResetReleaseRaisesReadyInterruptAndSenseInterruptReturnsTheCause()
    {
        var chip = new Upd765(unit => unit == 2 ? (byte)0x20 : (byte)0,
            (_, _) => { });
        chip.SetReset(true);
        chip.Advance(81920);
        Assert.False(chip.Irq);
        chip.SetReset(false);
        Assert.Equal(7828, chip.NextEventTicks);
        chip.Advance(8071);
        Assert.False(chip.Irq);
        chip.Advance(1);
        Assert.True(chip.Irq);
        Send(chip, 8);
        Assert.Equal(new byte[] { 0xc2, 0 }, Result(chip, 2));
        Assert.False(chip.Irq);
    }

    private static Upd765 Device(Upd765Model model = Upd765Model.Upd765A) => new(
        _ => Upd765Constants.ReadyPin | Upd765Constants.TrackZeroPin,
        (_, _) => { }, clockFrequency: 1_000_000, model: model);

    private static void Write(Upd765 chip, byte value)
    {
        chip.SetBus(true, false, true, true, value: value);
        chip.SetBus(true, false, false, true);
    }

    private static byte Read(Upd765 chip)
    {
        chip.SetBus(true, true, false, true);
        var value = Assert.IsType<byte>(chip.BusData);
        chip.SetBus(true, false, false, true);
        return value;
    }
}
