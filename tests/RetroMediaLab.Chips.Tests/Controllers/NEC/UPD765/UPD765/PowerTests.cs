using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Enums;
using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Services;
using static RetroMediaLab.Chips.Tests.Shared.Upd765TestCommands;

namespace RetroMediaLab.Chips.Tests.Controllers.NEC.UPD765.UPD765;

public sealed class PowerTests
{
    [Theory]
    [InlineData(4_000_000)] [InlineData(8_000_000)]
    public void StandbyAndWakeRespectThirtyTwoAndSixteenNativeClocks(int frequency)
    {
        var chip = new Upd765(_ => 0x20, (_, _) => { }, frequency, Upd765Model.Upd72065B);
        Specify(chip, false);
        Auxiliary(chip, 0x35);
        Assert.Equal(Upd765PowerState.EnteringStandby, chip.PowerState); Assert.Equal(32, chip.NextEventTicks);
        chip.Advance(31); Assert.Equal(Upd765PowerState.EnteringStandby, chip.PowerState);
        chip.Advance(1); Assert.Equal(Upd765PowerState.Standby, chip.PowerState);
        chip.Advance(1_000_000); Assert.Equal(Upd765PowerState.Standby, chip.PowerState);
        Assert.Equal(long.MaxValue, chip.NextEventTicks); Assert.False(chip.Irq);
        Auxiliary(chip, 0x34); Assert.Equal(16, chip.NextEventTicks);
        chip.Advance(15); Assert.Equal(Upd765PowerState.Waking, chip.PowerState);
        chip.Advance(1); Assert.Equal(Upd765PowerState.Active, chip.PowerState);
        Send(chip, 0x10); Assert.Equal(0x90, chip.ReadFifo());
    }

    [Theory]
    [InlineData(Upd765Model.Upd765A)] [InlineData(Upd765Model.Upd765B)]
    public void OriginalVariantsIgnoreTheCmosAuxiliaryRegister(Upd765Model model)
    {
        var chip = new Upd765(_ => 0x20, (_, _) => { }, model: model);
        foreach (var command in new byte[] { 0x34, 0x35, 0x36 }) Auxiliary(chip, command);
        chip.Advance(32000); Assert.Equal(Upd765PowerState.Active, chip.PowerState);
        Assert.False(chip.Irq); Assert.Equal(0x80, chip.ReadMainStatus());
    }

    [Fact]
    public void StandbyPreservesTheRemainingPollingDeadlineAndDoesNotSampleReady()
    {
        var ready = true; var samples = 0;
        var chip = new Upd765(unit => { samples++; return unit == 0 && ready ? (byte)0x20 : (byte)0; }, (_, _) => { }, model: Upd765Model.Upd72065B);
        Specify(chip, false); chip.WriteAuxiliaryCommand(0x35); chip.Advance(32);
        var before = samples; ready = false; chip.Advance(1_000_000);
        Assert.Equal(before, samples); Assert.False(chip.Irq);
        chip.WriteAuxiliaryCommand(0x34); chip.Advance(16);
        Assert.Equal(7780, chip.NextEventTicks);
        chip.Advance(7799); Assert.False(chip.Irq);
        chip.Advance(1); Assert.True(chip.Irq);
        Send(chip, 8); Assert.Equal(new byte[] { 0xc8, 0 }, Result(chip, 2));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void HardwareAndSoftwareResetLeaveStandbyAndCancelPendingCommands(bool software)
    {
        var rig = new PinFixture(model: Upd765Model.Upd72065B);
        rig.Command(6); rig.ReadyForTrack(); rig.Chip.WriteAuxiliaryCommand(0x35); rig.Chip.Advance(32);
        Assert.Equal(Upd765PowerState.Standby, rig.Chip.PowerState);
        if (software) Auxiliary(rig.Chip, 0x36);
        else { rig.Chip.SetReset(true); rig.Chip.SetReset(false); }
        Assert.Equal(Upd765PowerState.Active, rig.Chip.PowerState);
        Assert.False(rig.Chip.Active); Assert.False(rig.Chip.Drq); Assert.False(rig.Chip.WriteEnable);
        Assert.Equal(0x80, rig.Chip.ReadMainStatus());
        rig.Chip.Advance(8192); Assert.True(rig.Chip.Irq);
        Send(rig.Chip, 8); Assert.Equal(new byte[] { 0xc0, 0 }, Result(rig.Chip, 2));
    }

    [Fact]
    public void AuxiliaryCommandsAreSampledAtTheEndOfTheStatusRegisterWriteStrobe()
    {
        var chip = new Upd765(_ => 0x20, (_, _) => { }, model: Upd765Model.Upd72065B);
        chip.SetBus(true, false, true, false, value: 0x36);
        Assert.Equal(Upd765PowerState.Active, chip.PowerState);
        chip.SetBus(true, false, true, false, value: 0x35);
        chip.SetBus(true, false, false, false);
        Assert.Equal(Upd765PowerState.EnteringStandby, chip.PowerState);
        chip.Advance(32); Assert.Equal(Upd765PowerState.Standby, chip.PowerState);
        chip.WriteFifo(0x10); chip.WriteDma(0xff); Assert.Equal(0x80, chip.ReadMainStatus());
        Auxiliary(chip, 0x34); chip.Advance(16); Assert.Equal(0x80, chip.ReadMainStatus());
    }

    [Fact]
    public void DataRegisterValuesCannotBeMistakenForAnAuxiliaryReset()
    {
        var chip = new Upd765(_ => 0x20, (_, _) => { }, model: Upd765Model.Upd72065B);
        Send(chip, 0x36); Assert.Equal(0x80, chip.ReadFifo());
        Assert.False(chip.Irq); Assert.Equal(Upd765PowerState.Active, chip.PowerState);
    }

    private static void Auxiliary(Upd765 chip, byte command)
    {
        chip.SetBus(true, false, true, false, value: command);
        chip.SetBus(true, false, false, false);
    }
}
