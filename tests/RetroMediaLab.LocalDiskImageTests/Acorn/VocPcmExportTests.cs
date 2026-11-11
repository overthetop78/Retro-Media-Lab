using System.IO;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Functions;
using RetroMediaLab.MediaEngine.Images.Formats.Tape.Voc;
using RetroMediaLab.MediaEngine.Interfaces;
using RetroMediaLab.MediaEngine.Contracts;

namespace RetroMediaLab.MediaAudit.Acorn;

public sealed class VocPcmExportTests
{
    [Fact]
    public async Task LongPcmUsesContinuationBlocksWithoutDroppingSamples()
    {
        const int count = 0xffffff + 17;
        var pcm = Enumerable.Repeat((byte)128, count).ToArray();
        var document = TapePcmFunctions.RawDocument(new("memory.raw", []), pcm, new TapePcmProfile(8000, 1, 8, false));
        var capture = new Capture();
        await new VocWriter(capture).WriteAsync(document, "memory.voc", TapeImageFormatIds.Voc);
        Assert.Equal(1, capture.Bytes[26]);
        Assert.Equal(new byte[] { 255, 255, 255 }, capture.Bytes.AsSpan(27, 3).ToArray());
        const int continuation = 32 + 0xfffffd;
        Assert.Equal(2, capture.Bytes[continuation]);
        Assert.Equal(new byte[] { 19, 0, 0 }, capture.Bytes.AsSpan(continuation + 1, 3).ToArray());
        Assert.Equal(count + 37, capture.Bytes.Length);
        Assert.True(capture.Bytes.AsSpan(32, 0xfffffd).IndexOfAnyExcept((byte)128) < 0);
        Assert.True(capture.Bytes.AsSpan(continuation + 4, 19).IndexOfAnyExcept((byte)128) < 0);
        Assert.Equal(0, capture.Bytes[^1]);
    }

    [Fact]
    public async Task MoreThanOneSecondOfPcmDoesNotOverflowTheResamplingIndex()
    {
        const int rate = 48000;
        var samples = Enumerable.Range(0, rate * 3).Select(index => (short)(index % 2 == 0 ? 16000 : -16000)).ToArray();
        var document = TapePcmFunctions.Document(new("memory.csw", []), TapeImageFormatIds.Csw, new(samples, rate));
        var capture = new Capture();
        await new VocWriter(capture).WriteAsync(document, "memory.voc", TapeImageFormatIds.Voc);
        var timeConstant = capture.Bytes[30];
        var vocRate = 1_000_000d / (256 - timeConstant);
        var count = (int)Math.Round(samples.Length * vocRate / rate);
        Assert.Equal(count + 33, capture.Bytes.Length);
        for (var index = 0; index < count; index++)
        {
            var sourceIndex = Math.Min(samples.Length - 1, (long)Math.Floor(index * (rate / vocRate)));
            Assert.Equal((byte)((samples[sourceIndex] >> 8) + 128), capture.Bytes[32 + index]);
        }
        Assert.Equal(0, capture.Bytes[^1]);
    }

    private sealed class Capture : IAtomicImageFileWriter
    {
        internal byte[] Bytes = [];
        public async Task WriteAsync(string path, Func<Stream, CancellationToken, Task> write, CancellationToken cancellationToken = default)
        {
            using var stream = new MemoryStream();
            await write(stream, cancellationToken);
            Bytes = stream.ToArray();
        }
    }
}
