using RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Enums;
using RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Services;

namespace RetroMediaLab.Chips.Tests.Video.Motorola.M6845.Chip6845;

public sealed class LinearCharacterTests
{
    [Theory]
    [InlineData(Crtc6845Model.Mc6845, 0)]
    [InlineData(Crtc6845Model.Mc6845, 1)]
    [InlineData(Crtc6845Model.Mc6845, 3)]
    [InlineData(Crtc6845Model.Hd6845S, 0)]
    [InlineData(Crtc6845Model.Hd6845S, 1)]
    [InlineData(Crtc6845Model.Hd6845S, 3)]
    public void LinearPortionsMatchEveryStateAndResumeAtAllBoundaries(Crtc6845Model model, byte mode)
    {
        var represented = 0;
        for (byte skew = 0; skew < 4; skew++)
        {
            var actual = Create(); var reference = Create();
            var actualSignals = new List<(int, string, int)>();
            var referenceSignals = new List<(int, string, int)>();
            Wire(actual, actualSignals); Wire(reference, referenceSignals);
            for (var time = 0; time < 9000;)
            {
                // Program boundaries, width changes, pointer wrap, LP and reset
                // are entered by the same ordinary operations on both chips.
                if (time % 127 == 0)
                {
                    var index = (byte)((time / 127) % 16);
                    var value = (byte)(time / 13);
                    CrtcTestFixture.Write(actual, index, value);
                    CrtcTestFixture.Write(reference, index, value);
                }
                if (time % 211 == 0) { actual.SetLightPen(true); reference.SetLightPen(true); }
                else { actual.SetLightPen(false); reference.SetLightPen(false); }
                actual.SetReset(time % 997 == 0); reference.SetReset(time % 997 == 0);
                var maximum = Math.Min(17, Math.Min(127 - time % 127,
                    Math.Min(211 - time % 211, 997 - time % 997)));
                // A high LPSTB/reset is an input boundary after one tick.
                if (time % 211 == 0 || time % 997 == 0) maximum = 1;
                var count = actual.AdvanceLinearCharacters(maximum, (first, clocks) =>
                {
                    for (var n = 0; n < clocks; n++)
                    {
                        Assert.False(reference.Tick());
                        Assert.Equal(first + n, reference.MemoryAddress);
                    }
                    return true;
                });
                if (count == 0) { Assert.Equal(reference.Tick(), actual.Tick()); count = 1; }
                else represented += count;
                time += count;
                Assert.Equivalent(reference.CaptureState(), actual.CaptureState(), strict: true);
                Assert.Equal((reference.Address, reference.HSync, reference.VSync, reference.Cursor, reference.DisplayEnabled),
                    (actual.Address, actual.HSync, actual.VSync, actual.Cursor, actual.DisplayEnabled));
                Assert.Equal(referenceSignals, actualSignals);
            }

            Crtc6845 Create()
            {
                var chip = CrtcTestFixture.Create(model, total: 83, displayed: 64, mode: mode);
                CrtcTestFixture.Write(chip, 8, (byte)(mode | skew << 4 | skew << 6));
                CrtcTestFixture.Write(chip, 2, 70);
                CrtcTestFixture.Write(chip, 10, 0);
                CrtcTestFixture.Write(chip, 11, 2);
                CrtcTestFixture.Write(chip, 14, 0x12);
                CrtcTestFixture.Write(chip, 15, 0x38);
                return chip;
            }
        }
        Assert.True(represented > 0);
    }

    [Fact]
    public void ReceiverRefusalDoesNotMutateTheCrtc()
    {
        var chip = CrtcTestFixture.Create(total: 83, displayed: 64);
        CrtcTestFixture.Clocks(chip, 7);
        var before = chip.CaptureState(); var offered = 0;
        Assert.Equal(0, chip.AdvanceLinearCharacters(16, (first, count) => { offered = count; return false; }));
        Assert.True(offered >= 2);
        Assert.Equivalent(before, chip.CaptureState(), strict: true);
    }

    private static void Wire(Crtc6845 chip, List<(int, string, int)> signals)
    {
        // MA's intermediate sequence is consumed by the range receiver.
        chip.MemoryAddressChanged = _ => { };
        chip.DisplayEnabledChanged = level => signals.Add((chip.Scanline * 256 + chip.Column, "DE", level ? 1 : 0));
        chip.CursorChanged = level => signals.Add((chip.Scanline * 256 + chip.Column, "CU", level ? 1 : 0));
        chip.HSyncChanged = level => signals.Add((chip.Scanline * 256 + chip.Column, "HS", level ? 1 : 0));
        chip.VSyncChanged = level => signals.Add((chip.Scanline * 256 + chip.Column, "VS", level ? 1 : 0));
        chip.RasterAddressChanged = value => signals.Add((chip.Scanline * 256 + chip.Column, "RA", value));
    }
}
