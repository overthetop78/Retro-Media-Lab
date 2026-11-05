using RetroMediaLab.Chips.Video.Philips.SAA50xx.SAA5050.Constants;
using RetroMediaLab.Chips.Video.Philips.SAA50xx.SAA5050.Enums;
using RetroMediaLab.Chips.Video.Philips.SAA50xx.SAA5050.Services;
using Xunit;
using System.Reflection;

namespace RetroMediaLab.Chips.Tests.Video.Philips.SAA50xx.SAA5050;

public sealed class BatchedClockTests
{
    [Theory]
    [InlineData(0UL)]
    [InlineData(ulong.MaxValue - 100)]
    public void QueueOverflowAndClockWrapPreserveScalarEvents(ulong origin)
    {
        var reference = new SignalFixture(enable: false);
        var grouped = new SignalFixture(enable: false);
        var clock = typeof(TeletextSaa5050).GetField("_halfDotTime", BindingFlags.Instance | BindingFlags.NonPublic)!;
        clock.SetValue(reference.Chip, origin);
        clock.SetValue(grouped.Chip, origin);
        var fields = new[] { "_halfDotTime", "_pipelineHead", "_pipelineCount", "_windowHead", "_windowCount", "_serialPixel" }
            .Select(name => typeof(TeletextSaa5050).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!).ToArray();
        for (var run = 0; run < 128; run++)
        {
            // More entries than either queue can retain, including equal deadlines.
            for (var item = 0; item < Saa5050Constants.PipelineCapacity * 2; item++)
            {
                var enabled = (item & 1) != 0;
                reference.Chip.SetLoadOutputShiftEnable(enabled);
                grouped.Chip.SetLoadOutputShiftEnable(enabled);
                reference.Sample((byte)(run + item));
                grouped.Sample((byte)(run + item));
            }
            var count = 1 + run % Saa5050Constants.MaximumPackedHalfDots;
            ulong expected = 0;
            for (var dot = 0; dot < count; dot++)
            {
                reference.HalfDots(1);
                expected |= (ulong)reference.Chip.Rgb << (dot * Saa5050Constants.ColourBits);
            }
            Assert.Equal(expected, grouped.Chip.ClockHalfDots(count));
            Assert.Equal(reference.Chip.Monochrome, grouped.Chip.Monochrome);
            Assert.Equal(reference.Chip.Blanking, grouped.Chip.Blanking);
            foreach (var field in fields) Assert.Equal(field.GetValue(reference.Chip), field.GetValue(grouped.Chip));
        }
        grouped.Chip.Reset();
        Assert.Equal(0UL, clock.GetValue(grouped.Chip));
        Assert.Equal(0, fields.Single(field => field.Name == "_pipelineCount").GetValue(grouped.Chip));
        Assert.Equal(0, fields.Single(field => field.Name == "_windowCount").GetValue(grouped.Chip));
    }

    [Fact]
    public void ParallelPackingMatchesEveryTwelvePointMaskAndAllPacketLengths()
    {
        for (var mask = 0; mask <= 0xfff; mask++)
        {
            var reversed = 0;
            for (var bit = 0; bit < 12; bit++) reversed |= ((mask >> (11 - bit)) & 1) << bit;
            Assert.Equal(reversed, TeletextSaa5050.ReverseSerialMask(mask));
            for (var count = 0; count <= 21; count++)
            {
                var first = (mask + count) % 13;
                var foreground = mask & 7;
                var background = (mask >> 3) & 7;
                ulong expected = 0;
                for (var dot = 0; dot < count; dot++)
                {
                    var position = first + dot;
                    var on = position < 12 && (mask & (1 << (11 - position))) != 0;
                    expected |= (ulong)(on ? foreground : background) << (dot * 3);
                }
                Assert.Equal(expected, TeletextSaa5050.PackSerialColours(reversed >> first,
                    count, foreground, background));
            }
        }
    }

    public static IEnumerable<object[]> PacketSizes => Enumerable.Range(1, Saa5050Constants.MaximumPackedHalfDots)
        .Select(count => new object[] { count });

    [Theory]
    [MemberData(nameof(PacketSizes))]
    public void BatchedTransitionsPreserveRgbAndFinalPinsAcrossPipelineAndWindowEdges(int count)
    {
        var rom = Enumerable.Range(0, Saa5050Constants.CharacterRomSize).Select(index => (byte)(index * 37)).ToArray();
        var reference = new SignalFixture(rom);
        var grouped = new SignalFixture(rom);
        var level = false;
        for (var index = 0; index < 4096; index++)
        {
            var data = (byte)(index & Saa5050Constants.CharacterDataMask);
            reference.Sample(data);
            grouped.Sample(data);
            var window = index % 31 != 0;
            var field = index % 113 == 0;
            reference.Chip.SetDataEntryWindow(field);
            grouped.Chip.SetDataEntryWindow(field);
            reference.Chip.SetLoadOutputShiftEnable(window);
            grouped.Chip.SetLoadOutputShiftEnable(window);
            reference.Chip.PictureOn = grouped.Chip.PictureOn = index % 17 == 0;
            reference.Chip.SuperimposeInputHigh = grouped.Chip.SuperimposeInputHigh = index % 7 == 0;
            reference.Chip.CharacterRoundingSelect = grouped.Chip.CharacterRoundingSelect = index % 2 == 0;
            ulong expected = 0;
            for (var dot = 0; dot < count; dot++)
            {
                reference.HalfDots(1);
                expected |= (ulong)reference.Chip.Rgb << (dot * Saa5050Constants.ColourBits);
                level = !level;
            }
            Assert.Equal(expected, grouped.Chip.ClockHalfDots(count));
            Assert.Equal(reference.Chip.Rgb, grouped.Chip.Rgb);
            Assert.Equal(reference.Chip.Monochrome, grouped.Chip.Monochrome);
            Assert.Equal(reference.Chip.Blanking, grouped.Chip.Blanking);
            Assert.Equal(reference.Chip.CharacterRow, grouped.Chip.CharacterRow);
            Assert.Equal(reference.Chip.CharacterScanline, grouped.Chip.CharacterScanline);
            Assert.Equal(reference.Chip.FlashField, grouped.Chip.FlashField);
            Assert.Equal(reference.Chip.TransmittedLargeCharacterHigh, grouped.Chip.TransmittedLargeCharacterHigh);
            Assert.Equal(reference.Chip.SuperimposeDriveLow, grouped.Chip.SuperimposeDriveLow);
            Assert.Equal(reference.Chip.TextMode, grouped.Chip.TextMode);
            Assert.Equal(reference.Chip.Reveal, grouped.Chip.Reveal);
            Assert.Equal(reference.Chip.Superimpose, grouped.Chip.Superimpose);
            Assert.Equal(reference.Chip.Boxed, grouped.Chip.Boxed);
            // Le niveau TR6 doit aussi rester compatible avec le chemin broche par broche.
            reference.Chip.SetDotClock(level);
            grouped.Chip.SetDotClock(level);
            Assert.Equal(reference.Chip.Rgb, grouped.Chip.Rgb);
        }
    }

    [Fact]
    public void ClosedWindowRetainsFuturePipelineInputsWhenReopened()
    {
        var reference = new SignalFixture();
        var grouped = new SignalFixture();
        for (var index = 0; index < 1024; index++)
        {
            var visible = index % 97 == 0;
            reference.Chip.SetLoadOutputShiftEnable(visible);
            grouped.Chip.SetLoadOutputShiftEnable(visible);
            reference.Sample((byte)(index & Saa5050Constants.CharacterDataMask));
            grouped.Sample((byte)(index & Saa5050Constants.CharacterDataMask));
            // La lecture continue après chaque réouverture et inclut les échéances encore en vol.
            ulong expected = 0;
            for (var dot = 0; dot < Saa5050Constants.HalfDotsPerCharacter; dot++)
            {
                reference.HalfDots(1);
                expected |= (ulong)reference.Chip.Rgb << (dot * Saa5050Constants.ColourBits);
            }
            Assert.Equal(expected, grouped.Chip.ClockHalfDots(Saa5050Constants.HalfDotsPerCharacter));
            Assert.Equal(reference.Chip.Monochrome, grouped.Chip.Monochrome);
            Assert.Equal(reference.Chip.Blanking, grouped.Chip.Blanking);
        }
    }

    [Fact]
    public void InvalidCountsDoNotAdvanceTheChip()
    {
        var fixture = new SignalFixture();
        Assert.Throws<ArgumentOutOfRangeException>(() => fixture.Chip.ClockHalfDots(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => fixture.Chip.ClockHalfDots(Saa5050Constants.MaximumPackedHalfDots + 1));
        Assert.Equal(0UL, fixture.Chip.ClockHalfDots(0));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConstantRgbPreservesShiftAndMonochromeForEveryPacketSize(bool solidGlyph)
    {
        var rom = Enumerable.Repeat(solidGlyph ? byte.MaxValue : (byte)0,
            Saa5050Constants.CharacterRomSize).ToArray();
        var reference = new SignalFixture(rom);
        var grouped = new SignalFixture(rom);
        var sameColourDotObserved = false;
        for (var index = 0; index < 1024; index++)
        {
            // Fond blanc puis caractère : les RGB peuvent rester blancs alors
            // que la sortie monochrome doit toujours suivre les points du glyphe.
            var data = index % 7 == 0 ? (byte)Saa5050Control.NewBackground : (byte)'A';
            reference.Sample(data);
            grouped.Sample(data);
            var count = 1 + index % Saa5050Constants.MaximumPackedHalfDots;
            ulong expected = 0;
            for (var dot = 0; dot < count; dot++)
            {
                reference.HalfDots(1);
                expected |= (ulong)reference.Chip.Rgb << (dot * Saa5050Constants.ColourBits);
            }
            Assert.Equal(expected, grouped.Chip.ClockHalfDots(count));
            Assert.Equal(reference.Chip.Monochrome, grouped.Chip.Monochrome);
            Assert.Equal(reference.Chip.Blanking, grouped.Chip.Blanking);
            sameColourDotObserved |= grouped.Chip.Monochrome && grouped.Chip.Rgb == Saa5050Constants.White;
        }
        Assert.Equal(solidGlyph, sameColourDotObserved);
    }
}
