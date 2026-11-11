using RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Enums;
using RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Services;

namespace RetroMediaLab.Chips.Tests.Video.Motorola.M6847.Chip6847;

public sealed class StableClockTests
{
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    [InlineData(4)] [InlineData(5)] [InlineData(6)] [InlineData(7)]
    [InlineData(8)] [InlineData(9)] [InlineData(10)] [InlineData(11)]
    [InlineData(12)] [InlineData(13)]
    public void PrefixesPreserveAllOutputsTransactionsAndResumeAtEveryBoundary(int mode)
    {
        var unit = new Mc6847(new byte[280]);
        var grouped = new Mc6847(new byte[280]);
        var unitEvents = new List<string>();
        var groupedEvents = new List<string>();
        var inPrefix = false;
        foreach (var (chip, events) in new[] { (unit, unitEvents), (grouped, groupedEvents) })
        {
            chip.Mode = Mode(mode);
            var reset = false;
            chip.DataRequested = () =>
            {
                Assert.False(inPrefix);
                events.Add($"DD:{chip.Field}:{chip.Scanline}:{chip.HalfClock}:{chip.Address}");
                chip.Data = (byte)(chip.Address.GetValueOrDefault() * 17 + chip.Scanline);
                if (!reset && chip.Scanline == 50)
                {
                    reset = true;
                    chip.Reset();
                    chip.Mode = Mode(mode);
                }
            };
            chip.FieldSyncChanged = level =>
            {
                Assert.False(inPrefix);
                events.Add($"FS:{chip.Field}:{chip.Scanline}:{chip.HalfClock}:{level}");
            };
        }
        try
        {
            for (var part = 0; part < 11; part++)
            {
                var pins = Mode((mode + part) % 14);
                if ((part & 1) != 0) pins |= Mc6847Mode.ColourSet;
                unit.Mode = grouped.Mode = pins;
                unit.MemorySelect = grouped.MemorySelect = part % 3 != 1;
                unit.Data = grouped.Data = (byte)(part * 41);
                var remaining = 25001 + part;
                while (remaining > 0)
                {
                    int count;
                    var before = Outputs(unit);
                    inPrefix = true;
                    try { count = grouped.TryAdvanceStableHalfClocks(remaining); }
                    finally { inPrefix = false; }
                    Assert.InRange(count, 0, remaining);
                    if (count == 0)
                    {
                        unit.SetClock(!unit.Clock); grouped.SetClock(!grouped.Clock);
                        remaining--;
                    }
                    else
                    {
                        for (var edge = 0; edge < count; edge++)
                        {
                            unit.SetClock(!unit.Clock);
                            Assert.Equal(before, Outputs(unit));
                        }
                        remaining -= count;
                    }
                    Assert.Equal(Outputs(unit), Outputs(grouped));
                    Assert.Equal((unit.Clock, unit.Field, unit.Scanline, unit.HalfClock, unit.CharacterRow, unit.Mode, unit.Data),
                        (grouped.Clock, grouped.Field, grouped.Scanline, grouped.HalfClock, grouped.CharacterRow, grouped.Mode, grouped.Data));
                }
                Assert.Equal(unitEvents, groupedEvents);
            }
        }
        finally
        {
            unit.DataRequested = grouped.DataRequested = null;
            unit.FieldSyncChanged = grouped.FieldSyncChanged = null;
        }
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(0x10)] [InlineData(0x30)]
    [InlineData(0x50)] [InlineData(0x70)] [InlineData(0x90)] [InlineData(0xb0)]
    [InlineData(0xd0)] [InlineData(0xf0)]
    public void ReceiverPreservesEveryPixelAndAccessAcrossPartialIntervalsAndControlChanges(int mode)
    {
        long unitReads = 0, groupedReads = 0;
        byte Read(int address) => (byte)(address * 17 | (mode == 1 ? 0x40 : 0));
        var unit = new AtomVideo(address => { unitReads++; return Read(address); }, new byte[280]);
        var grouped = new AtomVideo(address => { groupedReads++; return Read(address); }, new byte[280]);
        var unitSync = new List<bool>(); var groupedSync = new List<bool>();
        unit.FrameSyncChanged = unitSync.Add; grouped.FrameSyncChanged = groupedSync.Add;
        try
        {
            for (var part = 0; part < 11; part++)
            {
                unit.SetMode((byte)mode); grouped.SetMode((byte)mode);
                unit.SetColorSet((part & 1) != 0); grouped.SetColorSet((part & 1) != 0);
                var clocks = 11909 + part;
                for (var clock = 0; clock < clocks; clock++) unit.Tick();
                grouped.AdvanceClocks(clocks);
                Assert.Equal((unit.Frame, unit.FrameSync, unitReads), (grouped.Frame, grouped.FrameSync, groupedReads));
                Assert.Equal(unitSync, groupedSync);
                Assert.Equal(unit.CompletedFrame, grouped.CompletedFrame);
            }
            unit.Reset(); grouped.Reset();
            for (var clock = 0; clock < 228 * 262; clock++) unit.Tick();
            grouped.AdvanceClocks(228 * 262);
            Assert.Equal(unit.CompletedFrame, grouped.CompletedFrame);
        }
        finally { unit.FrameSyncChanged = grouped.FrameSyncChanged = null; }
    }

    [Fact]
    public void EmptyAndInvalidRequestsDoNotAdvance()
    {
        var chip = new Mc6847(new byte[280]);
        Assert.Equal(0, chip.TryAdvanceStableHalfClocks(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => chip.TryAdvanceStableHalfClocks(-1));
        Assert.Equal(0, chip.HalfClock);
        var video = new AtomVideo(_ => 0, new byte[280]);
        video.AdvanceClocks(0);
        Assert.Throws<ArgumentOutOfRangeException>(() => video.AdvanceClocks(-1));
        Assert.Equal(0, video.Frame);
    }

    private static Mc6847Mode Mode(int mode) => mode < 8 ? Mc6847Mode.Graphics | (Mc6847Mode)(mode << 4)
        : mode switch { 8 => Mc6847Mode.None, 9 => Mc6847Mode.External,
            10 => Mc6847Mode.Inverse, 11 => Mc6847Mode.External | Mc6847Mode.Inverse,
            12 => Mc6847Mode.Semigraphics, _ => Mc6847Mode.Semigraphics | Mc6847Mode.External };

    private static object Outputs(Mc6847 chip) => (chip.Address, chip.HorizontalSync, chip.FieldSync,
        chip.RowPreset, chip.Active, chip.Colour, chip.LuminanceMillivolts,
        chip.ChromaAMillivolts, chip.ChromaBMillivolts, chip.ChromaBiasMillivolts);
}
