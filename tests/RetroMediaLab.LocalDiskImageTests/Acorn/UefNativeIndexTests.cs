using System.IO;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Functions;
using RetroMediaLab.MediaEngine.Images.Formats.Tape.Uef;
using RetroMediaLab.MediaEngine.Images.Models.Sequential;
using RetroMediaLab.MediaEngine.Images.Reading.Sources;
using RetroMediaLab.MediaEngine.Interfaces;

namespace RetroMediaLab.MediaAudit.Acorn;

public sealed class UefNativeIndexTests
{
    [Fact]
    public async Task PresentationIndexPreservesNativeUefChunksAndMetadataOnRewrite()
    {
        byte[] origin = "recording information\0"u8.ToArray();
        byte[] pulse = [1, 0, 0, (byte)'P', (byte)'W', 0];
        var document = MediaImageDocument.CreateUnexplored(new("memory.uef", []), TapeImageFormatIds.Uef, MediaKind.Tape,
            new SequentialMediaImageRepresentation(null, segments: [Segment(0, "0000", origin), Segment(1, "0114", pulse)]), [], new Dictionary<string, string>());
        var before = new Capture();
        await new UefWriter(before).WriteAsync(document, "before.uef", TapeImageFormatIds.Uef);
        var indexed = await TapeBlockFunctions.IndexAsync(document, CancellationToken.None);
        var after = new Capture();
        await new UefWriter(after).WriteAsync(indexed, "after.uef", TapeImageFormatIds.Uef);
        Assert.Equal(before.Bytes, after.Bytes);
        Assert.True(after.Bytes.AsSpan().IndexOf(origin) >= 0);
    }

    private static SequentialMediaSegment Segment(long position, string id, byte[] data)
        => new(position, SequentialSegmentKind.Pulse, dataRange: new MediaDataRange(0, data.Length, MediaDataRangeKind.Stored, new MemoryRandomAccessData(data)),
            metadata: new Dictionary<string, string> { [UefConstants.ChunkIdMetadataKey] = id, [UefConstants.BaseFrequencyMetadataKey] = "1200", [UefConstants.BaudRateMetadataKey] = "300", [UefConstants.PhaseMetadataKey] = "180" });

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
