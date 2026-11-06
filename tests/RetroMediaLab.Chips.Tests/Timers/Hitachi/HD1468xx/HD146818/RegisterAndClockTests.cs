using RetroMediaLab.Chips.Timers.Hitachi.HD1468xx.HD146818.Services;
using R = RetroMediaLab.Chips.Timers.Hitachi.HD1468xx.HD146818.Constants.Hd146818Constants;

namespace RetroMediaLab.Chips.Tests.Timers.Hitachi.HD1468xx.HD146818;

public sealed class RegisterAndClockTests
{
    private static Hd146818 Clock(bool binary = false, bool twentyFourHours = true)
    {
        var chip = new Hd146818();
        chip.WriteRegister(R.StatusA, 0x20);
        chip.WriteRegister(R.StatusB, (byte)((binary ? R.BinaryData : 0) | (twentyFourHours ? R.TwentyFourHour : 0)));
        chip.WriteRegister(R.Day, 1);
        chip.WriteRegister(R.Month, 1);
        chip.WriteRegister(R.DayOfWeek, 1);
        return chip;
    }

    [Fact]
    public void UserRamStoresAllBytesAndControlReadOnlyBitsCannotBeWritten()
    {
        var chip = Clock();
        for (var address = R.ConfigurationStart; address < R.RegisterCount; address++)
            for (var value = 0; value <= byte.MaxValue; value++)
            {
                chip.WriteRegister(address, (byte)value);
                Assert.Equal((byte)value, chip.ReadRegister(address));
            }
        chip.WriteRegister(R.Seconds, byte.MaxValue);
        Assert.Equal(R.SevenBits, chip.ReadRegister(R.Seconds));
        chip.WriteRegister(R.StatusC, byte.MaxValue);
        chip.WriteRegister(R.StatusD, byte.MaxValue);
        Assert.Equal(0, chip.ReadRegister(R.StatusC));
        Assert.Equal(0, chip.ReadRegister(R.StatusD));
        chip.SetPowerSense(true);
        Assert.Equal(R.ValidRam, chip.ReadRegister(R.StatusD));
        chip.SetReset(true);
        Assert.Equal(R.ValidRam, chip.ReadRegister(R.StatusD));
        chip.SetPowerSense(false);
        Assert.Equal(0, chip.ReadRegister(R.StatusD));
    }

    [Theory]
    [InlineData(false, 0x59, 0x13)]
    [InlineData(true, 59, 13)]
    public void CalendarAdvancesByOscillatorClocksInEitherDataMode(bool binary, byte seconds, byte hours)
    {
        var chip = Clock(binary);
        chip.WriteRegister(R.Seconds, seconds);
        chip.WriteRegister(R.Minutes, seconds);
        chip.WriteRegister(R.Hours, (byte)(hours - 1));
        chip.AdvanceClocks(R.LowFrequency);
        Assert.Equal(0, chip.ReadRegister(R.Seconds));
        Assert.Equal(0, chip.ReadRegister(R.Minutes));
        Assert.Equal(hours, chip.ReadRegister(R.Hours));
        Assert.Equal(R.UpdateFlag, chip.ReadRegister(R.StatusC));
    }

    [Theory]
    [InlineData(0x11, 0x92, 1)]
    [InlineData(0x91, 0x12, 2)]
    [InlineData(0x12, 0x01, 1)]
    [InlineData(0x92, 0x81, 1)]
    public void TwelveHourCalendarTogglesPmOnlyAtElevenToTwelve(byte hours, byte expected, byte day)
    {
        var chip = Clock(twentyFourHours: false);
        chip.WriteRegister(R.Seconds, 0x59);
        chip.WriteRegister(R.Minutes, 0x59);
        chip.WriteRegister(R.Hours, hours);
        chip.AdvanceClocks(R.LowFrequency);
        Assert.Equal(expected, chip.ReadRegister(R.Hours));
        Assert.Equal(day, chip.ReadRegister(R.Day));
    }

    [Fact]
    public void SetPreservesWritesAndDoesNotImplicitlyConvertDataWhenModeChanges()
    {
        var chip = Clock();
        chip.WriteRegister(R.StatusB, R.StopUpdates | R.UpdateFlag | R.TwentyFourHour);
        chip.WriteRegister(R.Seconds, 0x23);
        chip.AdvanceClocks(R.LowFrequency * 3L);
        Assert.Equal(0x23, chip.ReadRegister(R.Seconds));
        Assert.Equal(R.StopUpdates | R.TwentyFourHour, chip.ReadRegister(R.StatusB));
        Assert.Equal(0, chip.ReadRegister(R.StatusA) & R.UpdateInProgress);
        chip.WriteRegister(R.StatusB, R.StopUpdates | R.BinaryData);
        Assert.Equal(0x23, chip.ReadRegister(R.Seconds));
    }

    [Fact]
    public void AlarmCauseExistsWithoutMaskAndMaskChangeImmediatelyAssertsIrq()
    {
        var chip = Clock();
        chip.WriteRegister(R.SecondsAlarm, 1);
        chip.WriteRegister(R.MinutesAlarm, R.AlarmDontCare);
        chip.WriteRegister(R.HoursAlarm, byte.MaxValue);
        var edges = new List<bool>();
        chip.IrqChanged = edges.Add;
        chip.AdvanceClocks(R.LowFrequency);
        Assert.False(chip.Irq);
        chip.WriteRegister(R.StatusB, R.AlarmFlag | R.TwentyFourHour);
        Assert.True(chip.Irq);
        Assert.Equal(R.InterruptFlag | R.AlarmFlag | R.UpdateFlag, chip.ReadRegister(R.StatusC));
        Assert.False(chip.Irq);
        Assert.Equal(new[] { true, false }, edges);
        Assert.Equal(0, chip.ReadRegister(R.StatusC));
    }

    [Theory]
    [InlineData(0, 4194304)]
    [InlineData(1, 1048576)]
    [InlineData(2, 32768)]
    public void EveryRateSelectHasItsDocumentedPeriod(int divider, int frequency)
    {
        for (var rate = 1; rate <= 15; rate++)
        {
            var chip = new Hd146818(frequency, frequency);
            chip.WriteRegister(R.StatusA, (byte)((divider << 4) | rate));
            chip.WriteRegister(R.StatusB, R.StopUpdates);
            var effectiveRate = divider == 2 && rate <= 2 ? rate + 7 : rate;
            var period = (long)frequency * (1L << (effectiveRate - 1)) / 32768;
            chip.AdvanceClocks(period - 1);
            Assert.Equal(0, chip.ReadRegister(R.StatusC));
            chip.Clock();
            Assert.Equal(R.PeriodicFlag, chip.ReadRegister(R.StatusC));
        }
    }

    [Fact]
    public void ReferenceConversionKeepsTheFractionAndMatchesIndividualClockEdges()
    {
        var chip = new Hd146818(2000000);
        var other = new Hd146818(2000000);
        chip.WriteRegister(R.StatusA, 0x26);
        other.WriteRegister(R.StatusA, 0x26);
        chip.WriteRegister(R.StatusB, R.SquareWaveEnable);
        other.WriteRegister(R.StatusB, R.SquareWaveEnable);
        var edges = new List<(long Clock, bool Level)>();
        var otherEdges = new List<(long Clock, bool Level)>();
        chip.SquareWaveChanged = level => edges.Add((chip.ClockCount, level));
        other.SquareWaveChanged = level => otherEdges.Add((other.ClockCount, level));
        for (var time = 1; time <= 2000000; time++) chip.AdvanceTo(time);
        other.AdvanceTo(2000000);
        Assert.Equal(32768, chip.ClockCount);
        Assert.Equal(edges, otherEdges);
        Assert.Equal(chip.Export(), other.Export());
        Assert.Throws<ArgumentOutOfRangeException>(() => chip.AdvanceTo(1999999));
    }

    [Fact]
    public void DividerResetStopsUpdatesWithoutErasingRamAndResetDoesNotStopCalendar()
    {
        var chip = Clock();
        chip.WriteRegister(R.StatusA, 0x66);
        chip.WriteRegister(32, 0x5a);
        chip.AdvanceClocks(R.LowFrequency * 2L);
        Assert.Equal(0, chip.ReadRegister(R.Seconds));
        Assert.Equal(0, chip.ReadRegister(R.StatusC));
        chip.WriteRegister(R.StatusA, 0x20);
        chip.SetReset(true);
        chip.AdvanceClocks(R.LowFrequency);
        Assert.Equal(1, chip.ReadRegister(R.Seconds));
        Assert.Equal(0, chip.ReadRegister(R.StatusC));
        Assert.Equal(0x5a, chip.ReadRegister(32));
    }
}
