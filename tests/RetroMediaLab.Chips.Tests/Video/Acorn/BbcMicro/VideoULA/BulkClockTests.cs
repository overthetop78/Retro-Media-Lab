using RetroMediaLab.Chips.Video.Acorn.BbcMicro.VideoULA.Services;
using Xunit;

namespace RetroMediaLab.Chips.Tests.Video.Acorn.BbcMicro.VideoULA;

public sealed class BulkClockTests
{
    [Fact]
    public void EightPointEmissionMatchesPinTransitionsAcrossAllControlsAndTheirChanges()
    {
        var pins = new BbcVideoUla();
        var bulk = new BbcVideoUla();
        Span<int> inputs = stackalloc int[8];
        for (var iteration = 0; iteration < 65536; iteration++)
        {
            // Faire aussi démarrer certaines émissions entre deux frontières CPU.
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
            var actual = bulk.EmitCpuPixels(packed);
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
