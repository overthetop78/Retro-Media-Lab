using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Constants;
using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Enums;
using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.WesternDigital.WD177x.WD1770;

public sealed class OutputNotificationTests
{
    private readonly record struct Outputs(bool Motor, bool Step, bool Direction, bool Gate,
        bool Active, bool Reading, bool Writing, bool Density);

    private static Outputs ReadOutputs(Wd1770 chip) => new(chip.Motor, chip.Step, chip.Direction,
        chip.WriteGate, chip.Active, chip.Reading, chip.Writing, chip.DoubleDensity);

    [Theory]
    [InlineData(Wd1770Model.Wd1770)]
    [InlineData(Wd1770Model.Wd1772)]
    public void NotificationsAlwaysCarryANewOutputState(Wd1770Model model)
    {
        var chip = new Wd1770(model);
        var previous = ReadOutputs(chip);
        var notifications = 0;
        chip.DriveSignalsChanged += () =>
        {
            var current = ReadOutputs(chip);
            Assert.NotEqual(previous, current);
            previous = current;
            ++notifications;
        };
        chip.Reset();
        chip.Reset();
        Assert.Equal(0, notifications);
        chip.Write(Wd1770Constants.StatusCommandRegister, 0xd0);
        var afterInterrupt = notifications;
        chip.Write(Wd1770Constants.StatusCommandRegister, 0xd0);
        Assert.Equal(afterInterrupt, notifications);
        chip.Reset();
        var afterReset = notifications;
        chip.Reset();
        Assert.Equal(afterReset, notifications);
        chip.DoubleDensity = !chip.DoubleDensity;
        Assert.Equal(afterReset + 1, notifications);
        chip.DoubleDensity = chip.DoubleDensity;
        Assert.Equal(afterReset + 1, notifications);
        Assert.Equal(ReadOutputs(chip), previous);
    }

    [Theory]
    [InlineData(Wd1770Model.Wd1770)]
    [InlineData(Wd1770Model.Wd1772)]
    public void SpinUpCompletionPublishesDirectionBeforeTheStep(Wd1770Model model)
    {
        var chip = new Wd1770(model);
        var edges = new List<Outputs>();
        chip.DriveSignalsChanged += () => edges.Add(ReadOutputs(chip));
        chip.Write(Wd1770Constants.StatusCommandRegister, 0x40);
        for (var index = 0; index < Wd1770Constants.SpinUpIndexes; ++index)
        {
            chip.SetIndex(false);
            chip.SetIndex(true);
        }
        Assert.True(chip.Direction);
        Assert.False(chip.Step);
        Assert.True(edges[^1].Direction);
        Assert.False(edges[^1].Step);
        chip.AdvanceTo(chip.ClockCount + 1);
        Assert.Equal(ReadOutputs(chip), edges[^1]);
    }
}
