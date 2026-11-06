using RetroMediaLab.Chips.Timers.Hitachi.HD1468xx.HD146818.Services;
using R = RetroMediaLab.Chips.Timers.Hitachi.HD1468xx.HD146818.Constants.Hd146818Constants;

namespace RetroMediaLab.Chips.Tests.Timers.Hitachi.HD1468xx.HD146818;

public sealed class InterruptTests
{
    [Fact]
    public void EveryCombinationOfMasksWorksWithAllCausesLatchedIndependently()
    {
        for (var mask = 0; mask < 8; mask++)
        {
            var chip = new Hd146818();
            chip.WriteRegister(R.StatusA, 0x26);
            chip.WriteRegister(R.StatusB, R.TwentyFourHour);
            chip.WriteRegister(R.SecondsAlarm, R.AlarmDontCare);
            chip.WriteRegister(R.MinutesAlarm, R.AlarmDontCare);
            chip.WriteRegister(R.HoursAlarm, R.AlarmDontCare);
            chip.AdvanceClocks(32768);
            chip.WriteRegister(R.StatusB, (byte)(R.TwentyFourHour | mask << 4));
            Assert.Equal(mask != 0, chip.Irq);
            chip.WriteRegister(R.StatusC, 0);
            Assert.Equal(mask != 0, chip.Irq);
            var causes = chip.ReadRegister(R.StatusC);
            Assert.Equal(R.InterruptCauses | (mask != 0 ? R.InterruptFlag : 0), causes);
            Assert.False(chip.Irq);
            Assert.Equal(0, chip.ReadRegister(R.StatusC));
        }
    }

    [Fact]
    public void HeldResetSuppressesNewCausesAndTheirEnablesWhileCalendarKeepsRunning()
    {
        var chip = new Hd146818();
        chip.WriteRegister(R.StatusA, 0x26);
        chip.SetReset(true);
        chip.WriteRegister(R.StatusB, R.TwentyFourHour | R.ResetClearedEnables);
        chip.AdvanceClocks(32768);
        Assert.Equal(R.TwentyFourHour, chip.ReadRegister(R.StatusB));
        Assert.Equal(1, chip.ReadRegister(R.Seconds));
        Assert.Equal(0, chip.ReadRegister(R.StatusC));
        Assert.False(chip.SquareWave);
        chip.SetReset(false);
        Assert.NotNull(chip.NextClockEvent);
        chip.AdvanceClocks(32);
        Assert.Equal(R.PeriodicFlag, chip.ReadRegister(R.StatusC));
    }
}
