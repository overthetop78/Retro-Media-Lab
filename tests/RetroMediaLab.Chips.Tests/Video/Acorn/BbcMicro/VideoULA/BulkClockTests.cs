using RetroMediaLab.Chips.Video.Acorn.BbcMicro.VideoULA.Services;
using Xunit;

namespace RetroMediaLab.Chips.Tests.Video.Acorn.BbcMicro.VideoULA;

public sealed class BulkClockTests
{
    [Fact]
    public void GroupedCharacterInputsMatchIndividualPinsWithoutAdvancingClocks()
    {
        var pins = new BbcVideoUla();
        var grouped = new BbcVideoUla();
        for (var control = 0; control <= byte.MaxValue; control++)
        {
            pins.Write(0, (byte)control);
            grouped.Write(0, (byte)control);
            for (var data = 0; data <= byte.MaxValue; data++)
            for (var gates = 0; gates < 4; gates++)
            {
                var cursor = (gates & 1) != 0;
                var enabled = (gates & 2) != 0;
                var palette = (byte)(data ^ control);
                pins.Write(1, palette);
                grouped.Write(1, palette);
                pins.Data = (byte)data;
                pins.Cursor = cursor;
                pins.DisplayEnabled = enabled;
                grouped.SetCharacterInputs((byte)data, cursor, enabled);
                Assert.Equal(pins.Data, grouped.Data);
                Assert.Equal(pins.Cursor, grouped.Cursor);
                Assert.Equal(pins.DisplayEnabled, grouped.DisplayEnabled);
                Assert.Equal(pins.Rgb, grouped.Rgb);
                Assert.Equal(pins.Clock8, grouped.Clock8);
                Assert.Equal(pins.Clock4, grouped.Clock4);
                Assert.Equal(pins.Clock2, grouped.Clock2);
                Assert.Equal(pins.Clock1, grouped.Clock1);
                Assert.Equal(pins.CursorActive, grouped.CursorActive);
                var input = (uint)(data * 7919) & 0xffffff;
                Assert.Equal(pins.ClockMasterPeriods(input), grouped.ClockMasterPeriods(input));
                if (data % 31 == 0)
                {
                    pins.SetMasterClock(false); grouped.SetMasterClock(false);
                    pins.SetMasterClock(true); grouped.SetMasterClock(true);
                }
            }
            pins.Reset();
            grouped.Reset();
        }
    }

    [Fact]
    public void PaletteRevisionRolloverCannotReuseAnOldPacket()
    {
        var pins = new BbcVideoUla();
        var bulk = new BbcVideoUla();
        pins.Write(0, 0x1c); bulk.Write(0, 0x1c);
        pins.DisplayEnabled = bulk.DisplayEnabled = true;
        Assert.Equal(0u, bulk.ClockMasterPeriods(0));
        ClockTests.Clock(pins, 8);
        // Force only the host cache generation to its boundary, without billions of register writes.
        typeof(BbcVideoUla).GetField("_paletteRevision", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(bulk, uint.MaxValue);
        pins.Write(1, 0); bulk.Write(1, 0);
        var actual = bulk.ClockMasterPeriods(0);
        Assert.NotEqual(0u, actual);
        for (var dot = 0; dot < 8; dot++)
        {
            ClockTests.Clock(pins);
            Assert.Equal(pins.Rgb, (int)(actual >> (dot * 3)) & 7);
        }
    }

    [Fact]
    public void ReusedPacketsFollowPaletteFlashBlankingAndResetImmediately()
    {
        var pins = new BbcVideoUla();
        var bulk = new BbcVideoUla();
        for (var iteration = 0; iteration < 65536; iteration++)
        {
            if (iteration % 997 == 0) { pins.Reset(); bulk.Reset(); }
            if (iteration % 47 == 0)
            {
                var control = (byte)(iteration / 47);
                pins.Write(0, control); bulk.Write(0, control);
            }
            if (iteration % 31 == 0)
            {
                var palette = (byte)(iteration / 31);
                pins.Write(1, palette); bulk.Write(1, palette);
            }
            pins.Data = bulk.Data = (byte)(iteration & 15);
            pins.DisplayEnabled = bulk.DisplayEnabled = iteration % 137 < 111;
            pins.Inverted = bulk.Inverted = iteration % 107 < 41;
            pins.Cursor = bulk.Cursor = iteration % 89 < 13;
            var input = (uint)iteration & 0xffffff;
            var actual = bulk.ClockMasterPeriods(input);
            for (var dot = 0; dot < 8; dot++)
            {
                pins.TeletextRgb = (int)(input >> (dot * 3)) & 7;
                ClockTests.Clock(pins);
                Assert.Equal(pins.Rgb, (int)(actual >> (dot * 3)) & 7);
            }
            Assert.Equal(pins.Rgb, bulk.Rgb);
            Assert.Equal(pins.CursorActive, bulk.CursorActive);
            Assert.Equal(pins.CrtcClock, bulk.CrtcClock);
        }
    }

    [Fact]
    public void EightPointEmissionMatchesPinTransitionsAcrossAllControlsAndTheirChanges()
    {
        var pins = new BbcVideoUla();
        var bulk = new BbcVideoUla();
        Span<int> inputs = stackalloc int[8];
        for (var iteration = 0; iteration < 65536; iteration++)
        {
            // Faire aussi démarrer certaines émissions entre deux frontières de paquets maître.
            if (iteration % 17 == 0)
            {
                ClockTests.Clock(pins, 3);
                ClockTests.Clock(bulk, 3);
            }
            var control = (byte)(iteration >> 8);
            var data = (byte)iteration;
            var palette = (byte)(iteration * 31);
            pins.Write(0, control); bulk.Write(0, control);
            pins.Write(1, palette); bulk.Write(1, palette);
            pins.Data = bulk.Data = data;
            pins.DisplayEnabled = bulk.DisplayEnabled = (iteration & 2) != 0;
            pins.Inverted = bulk.Inverted = (iteration & 4) != 0;
            pins.Cursor = bulk.Cursor = (iteration & 7) == 0;
            for (var i = 0; i < inputs.Length; i++) inputs[i] = (iteration + i * 3) & 7;
            uint packed = 0;
            for (var i = 0; i < inputs.Length; i++) packed |= (uint)inputs[i] << (i * 3);
            var actual = bulk.ClockMasterPeriods(packed);
            for (var i = 0; i < inputs.Length; i++)
            {
                pins.TeletextRgb = inputs[i];
                ClockTests.Clock(pins);
                Assert.Equal(pins.Rgb, (int)(actual >> (i * 3)) & 7);
            }
            Assert.Equal(pins.Rgb, bulk.Rgb);
            Assert.Equal(pins.CursorActive, bulk.CursorActive);
            Assert.Equal(pins.Clock1, bulk.Clock1); Assert.Equal(pins.Clock2, bulk.Clock2);
            Assert.Equal(pins.Clock4, bulk.Clock4); Assert.Equal(pins.Clock8, bulk.Clock8);
            Assert.Equal(pins.CrtcClock, bulk.CrtcClock);
        }
    }
}
