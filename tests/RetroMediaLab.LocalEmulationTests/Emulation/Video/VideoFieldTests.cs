using RetroMediaLab.App.Functions.Rendering.Emulation;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;

namespace RetroMediaLab.Tests.Emulation.Video;

public sealed class VideoFieldTests
{
    [Theory]
    [InlineData(EmulationPixelFormat.Xrgb8888)]
    [InlineData(EmulationPixelFormat.Rgb565)]
    [InlineData(EmulationPixelFormat.Rgb1555)]
    public void RetainsBothParitiesBeforeRenderingAndOwnsComposedSnapshots(EmulationPixelFormat format)
    {
        var display = new EmulationVideoFieldComposer();
        var even = Field(EmulationVideoScanMode.InterlacedEvenField, 1, 11, format);
        var odd = Field(EmulationVideoScanMode.InterlacedOddField, 2, 47, format);
        display.Receive(even);
        var first = display.Compose()!;
        Assert.Equal(4, first.Height);
        Assert.Equal(EmulationVideoScanMode.Progressive, first.ScanMode);
        CheckRows(first, even, null);
        display.Receive(odd);
        var nextEven = Field(EmulationVideoScanMode.InterlacedEvenField, 3, 83, format);
        display.Receive(nextEven); // L'affichage n'a pas présenté le champ impair intermédiaire.
        var combined = display.Compose()!;
        CheckRows(combined, nextEven, odd);
        CheckRows(first, even, null);
        Assert.Same(combined, display.Compose());
        Assert.Equal(nextEven.Timestamp, combined.Timestamp);
        Assert.Equal(nextEven.Sequence, combined.Sequence);
        Assert.Equal(nextEven.AspectRatio, combined.AspectRatio);
        Assert.Equal(format, combined.PixelFormat);
        display.Receive(Field(EmulationVideoScanMode.InterlacedEvenField, 4, 117, format));
        CheckRows(combined, nextEven, odd);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void ClearsOppositeFieldOnGeometryFormatAndTimelineChanges(int change)
    {
        var display = new EmulationVideoFieldComposer();
        display.Receive(Field(EmulationVideoScanMode.InterlacedOddField, 20, 25));
        var next = Field(EmulationVideoScanMode.InterlacedEvenField, 21, 75);
        next = change switch
        {
            0 => next with { Sequence = 1 },
            1 => next with { Timestamp = TimeSpan.Zero },
            2 => next with { Width = next.Width - 1 },
            3 => next with { PixelFormat = EmulationPixelFormat.Rgb565 },
            _ => next with { Height = 1, Pixels = next.Pixels[..next.Pitch] }
        };
        display.Receive(next);
        CheckRows(display.Compose()!, next, null);
    }

    [Fact]
    public void ProgressiveFramesRemainUnchangedAndResetClearsInterlacedHistory()
    {
        var display = new EmulationVideoFieldComposer();
        display.Receive(Field(EmulationVideoScanMode.InterlacedOddField, 1, 25));
        var progressive = Field(EmulationVideoScanMode.Progressive, 2, 75);
        display.Receive(progressive);
        Assert.Same(progressive, display.Compose());
        var even = Field(EmulationVideoScanMode.InterlacedEvenField, 3, 95);
        display.Receive(even);
        CheckRows(display.Compose()!, even, null);
        display.Reset();
        Assert.Null(display.Compose());
        var odd = Field(EmulationVideoScanMode.InterlacedOddField, 4, 35);
        display.Receive(odd);
        CheckRows(display.Compose()!, null, odd);
    }

    private static VideoFrame Field(EmulationVideoScanMode mode, long sequence, byte value,
        EmulationPixelFormat format = EmulationPixelFormat.Xrgb8888)
    {
        const int width = 3, height = 2;
        var pitch = width * (format == EmulationPixelFormat.Xrgb8888 ? 4 : 2) + 2;
        var pixels = Enumerable.Range(0, pitch * height).Select(index => (byte)(value + index)).ToArray();
        return new(pixels, width, height, pitch, format, 4f / 3f, sequence, TimeSpan.FromMilliseconds(sequence * 20)) { ScanMode = mode };
    }

    private static void CheckRows(VideoFrame frame, VideoFrame? even, VideoFrame? odd)
    {
        for (var row = 0; row < frame.Height; row++)
        {
            var source = (row & 1) == 0 ? even : odd;
            var expected = source is null ? new byte[frame.Pitch]
                : source.Pixels.Slice(row / 2 * source.Pitch, source.Pitch).ToArray();
            Assert.Equal(expected, frame.Pixels.Slice(row * frame.Pitch, frame.Pitch).ToArray());
        }
    }
}
