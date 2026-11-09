using RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Enums;
using static RetroMediaLab.Chips.Tests.Video.Motorola.M6845.Chip6845.CrtcTestFixture;

namespace RetroMediaLab.Chips.Tests.Video.Motorola.M6845.Chip6845;

public sealed class PinNotificationTests
{
    [Theory]
    [InlineData(Crtc6845Model.Mc6845, 0)]
    [InlineData(Crtc6845Model.Mc6845, 1)]
    [InlineData(Crtc6845Model.Mc6845, 3)]
    [InlineData(Crtc6845Model.Hd6845S, 0)]
    [InlineData(Crtc6845Model.Hd6845S, 1)]
    [InlineData(Crtc6845Model.Hd6845S, 3)]
    public void PinsMatchEveryResolvedTransitionAcrossClockWritesAndReset(Crtc6845Model model, byte mode)
    {
        var crtc = Create(model, mode: mode);
        Write(crtc, 10, 0); Write(crtc, 11, 2);
        Write(crtc, 14, 0x12); Write(crtc, 15, 0x36);
        var actual = new List<(int Operation, string Pin, int Value)>();
        var expected = new List<(int Operation, string Pin, int Value)>();
        var operation = 0;
        crtc.MemoryAddressChanged = value =>
        {
            Assert.Equal(crtc.MemoryAddress, value);
            actual.Add((operation, "MA", value));
        };
        crtc.DisplayEnabledChanged = value =>
        {
            Assert.Equal(crtc.DisplayEnabled, value);
            actual.Add((operation, "DE", value ? 1 : 0));
        };
        crtc.CursorChanged = value =>
        {
            Assert.Equal(crtc.Cursor, value);
            actual.Add((operation, "CU", value ? 1 : 0));
        };
        Assert.Empty(actual);
        void Track(Action action)
        {
            operation++;
            var before = (crtc.MemoryAddress, crtc.DisplayEnabled, crtc.Cursor);
            action();
            if (before.MemoryAddress != crtc.MemoryAddress) expected.Add((operation, "MA", crtc.MemoryAddress));
            if (before.DisplayEnabled != crtc.DisplayEnabled) expected.Add((operation, "DE", crtc.DisplayEnabled ? 1 : 0));
            if (before.Cursor != crtc.Cursor) expected.Add((operation, "CU", crtc.Cursor ? 1 : 0));
        }
        for (var clock = 0; clock < 4096; clock++)
        {
            if (clock == 512) Track(() => Write(crtc, 8, (byte)(mode | 0xf0)));
            if (clock == 540) Track(() => Write(crtc, 8, mode));
            if (clock == 1024) Track(() => crtc.SetReset(true));
            if (clock == 1030) Track(() => crtc.SetReset(false));
            if (clock == 2048) Track(() => { crtc.SetLightPen(true); crtc.SetReset(true); });
            if (clock == 2056) Track(() => { crtc.SetLightPen(false); crtc.SetReset(false); });
            Track(() => crtc.Tick());
        }
        Track(crtc.Reset);
        Track(crtc.Reset);
        Assert.Equal(expected, actual);
        Assert.Contains(actual, item => item.Pin == "MA");
        Assert.Contains(actual, item => item.Pin == "DE");
        Assert.Contains(actual, item => item.Pin == "CU");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void PhysicalMaIsNotTheRendererAddressDelayedByDisplaySkew(int skew)
    {
        var crtc = Create(mode: (byte)(skew << 4));
        var distinct = 0;
        crtc.MemoryAddressChanged = address =>
        {
            Assert.Equal(crtc.MemoryAddress, address);
            if (address != crtc.Address) distinct++;
        };
        Clocks(crtc, 1024);
        Assert.True(distinct > 0);
    }

    [Fact]
    public void ReconnectingAnObserverDoesNotReplayAnOldLevelChange()
    {
        var crtc = Create();
        Clocks(crtc, 20);
        var values = new List<int>();
        crtc.MemoryAddressChanged = values.Add;
        Assert.Empty(values);
        var old = crtc.MemoryAddress;
        crtc.Tick();
        Assert.Equal(old == crtc.MemoryAddress ? 0 : 1, values.Count);
        crtc.MemoryAddressChanged = null;
        Clocks(crtc, 50);
        var previousCount = values.Count;
        crtc.MemoryAddressChanged = values.Add;
        Assert.Equal(previousCount, values.Count);
        Write(crtc, 10, (byte)crtc[10]);
        Assert.Equal(previousCount, values.Count);
    }
}
