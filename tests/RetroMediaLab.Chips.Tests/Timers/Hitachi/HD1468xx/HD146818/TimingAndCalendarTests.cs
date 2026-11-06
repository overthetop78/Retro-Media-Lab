using RetroMediaLab.Chips.Timers.Hitachi.HD1468xx.HD146818.Enums;
using RetroMediaLab.Chips.Timers.Hitachi.HD1468xx.HD146818.Services;
using R = RetroMediaLab.Chips.Timers.Hitachi.HD1468xx.HD146818.Constants.Hd146818Constants;

namespace RetroMediaLab.Chips.Tests.Timers.Hitachi.HD1468xx.HD146818;

public sealed class TimingAndCalendarTests
{
    [Theory]
    [InlineData(0, 4194304, 4194176, 2048, 2080)]
    [InlineData(1, 1048576, 1048544, 512, 520)]
    [InlineData(2, 32768, 32767, 16, 130)]
    public void UipWarningRamDisconnectionAndUpdateEndHaveDistinctEdges(int divider, int frequency, long firstUip, int warning, int duration)
    {
        var chip = new Hd146818(frequency, frequency);
        chip.WriteRegister(R.StatusA, (byte)(divider << 4));
        chip.WriteRegister(R.StatusB, R.TwentyFourHour | R.UpdateInterruptEnable);
        chip.AdvanceHalfClocks(firstUip - 1);
        Assert.Equal(0, chip.ReadRegister(R.StatusA) & R.UpdateInProgress);
        chip.AdvanceHalfClocks(1);
        Assert.Equal(R.UpdateInProgress, chip.ReadRegister(R.StatusA) & R.UpdateInProgress);
        Assert.True(chip.CalendarDataAvailable);
        chip.AdvanceHalfClocks(warning - 1);
        Assert.True(chip.CalendarDataAvailable);
        chip.AdvanceHalfClocks(1);
        Assert.False(chip.CalendarDataAvailable);
        chip.WriteRegister(R.Seconds, 0x45);
        chip.WriteRegister(32, 0x5a);
        chip.AdvanceHalfClocks(duration - 1);
        Assert.False(chip.Irq);
        chip.AdvanceHalfClocks(1);
        Assert.True(chip.CalendarDataAvailable);
        Assert.True(chip.Irq);
        Assert.Equal(1, chip.ReadRegister(R.Seconds));
        Assert.Equal(0x5a, chip.ReadRegister(32));
        chip.ReadRegister(R.StatusC);
        var secondUip = firstUip + 2L * frequency;
        chip.AdvanceHalfClocks(secondUip - chip.ClockHalfCount - 1);
        Assert.Equal(0, chip.ReadRegister(R.StatusA) & R.UpdateInProgress);
        chip.AdvanceHalfClocks(1);
        Assert.Equal(R.UpdateInProgress, chip.ReadRegister(R.StatusA) & R.UpdateInProgress);
    }

    [Theory]
    [InlineData(false, 4)]
    [InlineData(true, 1)]
    public void ClockOutPublishesEveryHalfEdgeOnlyWhenConnected(bool sourceClock, int halfPeriod)
    {
        var chip = new Hd146818();
        chip.SetClockFrequencySelect(sourceClock);
        var edges = new List<(long Time, bool High)>();
        chip.ClockOutChanged = high => edges.Add((chip.ClockHalfCount, high));
        chip.AdvanceClocks(8);
        Assert.Equal(16 / halfPeriod, edges.Count);
        for (var i = 0; i < edges.Count; i++)
            Assert.Equal(((i + 1L) * halfPeriod, (i & 1) == 0), edges[i]);
        chip.ClockOutChanged = null;
        chip.WriteRegister(R.StatusA, 0x60);
        Assert.Null(chip.NextClockEvent);
        chip.AdvanceClocks(1000000);
        Assert.False(chip.ClockOut);
    }

    [Fact]
    public void ExternalOscillatorLevelsDoNotCountRepeatedInputsAsEdges()
    {
        var chip = new Hd146818();
        chip.SetClockFrequencySelect(true);
        chip.SetOscillator(true);
        Assert.True(chip.ClockOut);
        Assert.Equal(1, chip.ClockHalfCount);
        chip.SetOscillator(true);
        Assert.Equal(1, chip.ClockHalfCount);
        chip.SetOscillator(false);
        Assert.False(chip.ClockOut);
        Assert.Equal(1, chip.ClockCount);
    }

    [Theory]
    [InlineData(Hd146818Variant.Hd146818, 0)]
    [InlineData(Hd146818Variant.Hd146818X, 0)]
    [InlineData(Hd146818Variant.Hd146818A, 0x10)]
    public void SetAffectsUieAccordingToTheHitachiRevision(Hd146818Variant variant, byte expected)
    {
        var chip = new Hd146818(variant: variant);
        chip.WriteRegister(R.StatusB, R.StopUpdates | R.UpdateInterruptEnable);
        Assert.Equal(expected, chip.ReadRegister(R.StatusB) & R.UpdateInterruptEnable);
    }

    [Fact]
    public void EveryValidDayOfATwoDigitCenturyRollsOverInBinaryMode()
    {
        var chip = new Hd146818();
        chip.WriteRegister(R.StatusA, 0x20);
        chip.WriteRegister(R.StatusB, R.BinaryData | R.TwentyFourHour);
        for (var date = new DateTime(2000, 1, 1); date.Year < 2100; date = date.AddDays(1))
        {
            WriteDate(chip, date.Year % 100, date.Month, date.Day, (int)date.DayOfWeek + 1, 23, 59, 59);
            chip.AdvanceClocks(32768);
            var expected = date.AddDays(1);
            Assert.Equal((byte)(expected.Year % 100), chip.ReadRegister(R.Year));
            Assert.Equal((byte)expected.Month, chip.ReadRegister(R.Month));
            Assert.Equal((byte)expected.Day, chip.ReadRegister(R.Day));
            Assert.Equal((byte)((int)expected.DayOfWeek + 1), chip.ReadRegister(R.DayOfWeek));
            Assert.Equal(0, chip.ReadRegister(R.Hours));
        }
    }

    [Theory]
    [InlineData(88, 3, 29, 59, 4, 1)]
    [InlineData(88, 4, 30, 59, 4, 31)]
    [InlineData(89, 2, 28, 59, 2, 29)]
    [InlineData(88, 2, 28, 58, 3, 1)]
    [InlineData(88, 2, 29, 59, 3, 1)]
    public void FirstCalendarCarryPreservesHitachiInitializationRestrictions(int year, int month, int day, int seconds, byte expectedMonth, byte expectedDay)
    {
        foreach (var variant in Enum.GetValues<Hd146818Variant>())
        {
            var chip = new Hd146818(variant: variant);
            chip.WriteRegister(R.StatusA, 0x70);
            chip.WriteRegister(R.StatusB, R.StopUpdates | R.BinaryData | R.TwentyFourHour);
            WriteDate(chip, year, month, day, 1, 23, 59, seconds);
            chip.WriteRegister(R.StatusA, 0x20);
            chip.WriteRegister(R.StatusB, R.BinaryData | R.TwentyFourHour);
            chip.AdvanceClocks(32768L * (60 - seconds));
            Assert.Equal(expectedMonth, chip.ReadRegister(R.Month));
            Assert.Equal(expectedDay, chip.ReadRegister(R.Day));
        }
    }

    [Theory]
    [InlineData(Hd146818Variant.Hd146818, 3)]
    [InlineData(Hd146818Variant.Hd146818X, 2)]
    [InlineData(Hd146818Variant.Hd146818A, 2)]
    public void DaylightSavingIsNotCopiedIntoPartsForWhichHitachiProhibitsIt(Hd146818Variant variant, byte hour)
    {
        var chip = new Hd146818(variant: variant);
        chip.WriteRegister(R.StatusA, 0x20);
        chip.WriteRegister(R.StatusB, R.BinaryData | R.TwentyFourHour | R.DaylightSaving);
        WriteDate(chip, 88, 4, 24, 1, 1, 59, 59);
        chip.AdvanceClocks(32768);
        Assert.Equal(hour, chip.ReadRegister(R.Hours));
    }

    [Fact]
    public void AutumnHourIsRepeatedOnlyOnceAndAlarmMatchesTheAdjustedTime()
    {
        var chip = new Hd146818();
        chip.WriteRegister(R.StatusA, 0x20);
        chip.WriteRegister(R.StatusB, R.BinaryData | R.TwentyFourHour | R.DaylightSaving);
        WriteDate(chip, 88, 10, 30, 1, 1, 59, 59);
        chip.WriteRegister(R.SecondsAlarm, 0);
        chip.WriteRegister(R.MinutesAlarm, 0);
        chip.WriteRegister(R.HoursAlarm, 1);
        chip.AdvanceClocks(32768);
        Assert.Equal(1, chip.ReadRegister(R.Hours));
        Assert.Equal(R.AlarmFlag | R.UpdateFlag, chip.ReadRegister(R.StatusC));
        chip.AdvanceClocks(32768L * 3600);
        Assert.Equal(2, chip.ReadRegister(R.Hours));
    }

    private static void WriteDate(Hd146818 chip, int year, int month, int day, int weekday, int hour, int minute, int second)
    {
        chip.WriteRegister(R.Year, (byte)year);
        chip.WriteRegister(R.Month, (byte)month);
        chip.WriteRegister(R.Day, (byte)day);
        chip.WriteRegister(R.DayOfWeek, (byte)weekday);
        chip.WriteRegister(R.Hours, (byte)hour);
        chip.WriteRegister(R.Minutes, (byte)minute);
        chip.WriteRegister(R.Seconds, (byte)second);
    }
}
