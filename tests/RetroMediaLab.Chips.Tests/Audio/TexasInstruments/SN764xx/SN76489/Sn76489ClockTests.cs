using System.Reflection;
using RetroMediaLab.Chips.Audio.TexasInstruments.SN764xx.SN76489.Services;

namespace RetroMediaLab.Chips.Tests.Audio.TexasInstruments.SN764xx.SN76489;

public sealed class Sn76489ClockTests
{
    private const int InputValues = 256;
    private const int ResetInterval = 29;
    private static readonly FieldInfo[] StateFields = typeof(Sn76489)
        .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
        .Where(field => !typeof(Delegate).IsAssignableFrom(field.FieldType)).ToArray();

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(31)]
    [InlineData(32)]
    [InlineData(33)]
    [InlineData(127)]
    public void GroupedClocksKeepEveryGeneratorBusAndTransferState(int clocks)
    {
        var reference = new Sn76489();
        var grouped = new Sn76489();
        var expected = new List<(int Level, bool Ready)>();
        var actual = new List<(int Level, bool Ready)>();
        reference.OutputChanged = level => expected.Add((level, reference.Ready));
        grouped.OutputChanged = level => actual.Add((level, grouped.Ready));
        for (var value = 0; value < InputValues; value++)
        {
            if (value % ResetInterval == 0)
            {
                reference.Reset();
                grouped.Reset();
            }
            reference.Write((byte)value);
            grouped.Write((byte)value);
            Compare();
            reference.SetBus((byte)~value, (value & 1) != 0, (value & 2) != 0);
            grouped.SetBus((byte)~value, (value & 1) != 0, (value & 2) != 0);
            for (var interval = 0; interval < 5; interval++)
            {
                for (var tick = 0; tick < clocks; tick++) reference.Tick();
                grouped.AdvanceClocks(clocks);
                Compare();
            }
        }

        void Compare()
        {
            Assert.Equal(expected, actual);
            expected.Clear();
            actual.Clear();
            Assert.Equal(reference.Output, grouped.Output);
            Assert.Equal(reference.Ready, grouped.Ready);
            foreach (var field in StateFields)
            {
                var before = field.GetValue(reference);
                var after = field.GetValue(grouped);
                if (before is Array left && after is Array right)
                    Assert.Equal(left.Cast<object>(), right.Cast<object>());
                else Assert.Equal(before, after);
            }
        }
    }

    [Fact]
    public void OutputNotificationsPublishOnlyNewLevelsIncludingWritesAndReset()
    {
        var chip = new Sn76489();
        var previous = chip.Output;
        var count = 0;
        chip.OutputChanged = level =>
        {
            Assert.NotEqual(previous, level);
            Assert.Equal(level, chip.Output);
            previous = level;
            count++;
        };
        Sn76489ToneTests.Mute(chip);
        var muted = count;
        chip.AdvanceClocks(40_000);
        Assert.Equal(muted, count);
        Sn76489ToneTests.Tone(chip, 0, 1);
        chip.Write(0x90);
        chip.AdvanceClocks(40_000);
        Assert.True(count > muted);
        chip.Reset();
        Assert.Equal(chip.Output, previous);
        var reset = count;
        chip.Reset();
        Assert.Equal(reset, count);
    }

    [Fact]
    public void EmptyOrInvalidIntervalsCannotChangeTheState()
    {
        var chip = new Sn76489();
        var reference = new Sn76489();
        chip.AdvanceClocks(0);
        Assert.Throws<ArgumentOutOfRangeException>(() => chip.AdvanceClocks(-1));
        for (var clock = 0; clock < 127; clock++)
        {
            chip.Tick();
            reference.Tick();
            Assert.Equal(reference.Output, chip.Output);
            Assert.Equal(reference.Ready, chip.Ready);
        }
    }
}
