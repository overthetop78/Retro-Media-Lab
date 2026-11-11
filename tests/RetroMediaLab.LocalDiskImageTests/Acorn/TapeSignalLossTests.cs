using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Images.Conversion.Sequential;
using RetroMediaLab.MediaEngine.Images.Formats.Tape.AtomTap;
using RetroMediaLab.MediaEngine.Images.Models.Sequential;
using RetroMediaLab.MediaEngine.Images.Reading.Sources;

namespace RetroMediaLab.MediaAudit.Acorn;

public sealed class TapeSignalLossTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RenderingDeclaresItsContainerLossWithoutChangingTheSource(bool atomTap)
    {
        MediaImageDocument source;
        if (atomTap)
        {
            var bytes = new byte[23]; bytes[0] = (byte)'P'; bytes[20] = 1; bytes[22] = 42;
            source = AtomTapReader.ReadBytes(new("memory.tap", []), bytes);
        }
        else
        {
            byte[] origin = "original recorder\0"u8.ToArray();
            byte[] data = [42];
            var metadata = new Dictionary<string, string> { [UefConstants.ChunkIdMetadataKey] = "0100", [UefConstants.BaseFrequencyMetadataKey] = "1200", [UefConstants.BaudRateMetadataKey] = "1200", [UefConstants.PhaseMetadataKey] = "180" };
            var segments = new[]
            {
                new SequentialMediaSegment(0, SequentialSegmentKind.Unknown, dataRange: new MediaDataRange(0, origin.Length, MediaDataRangeKind.Stored, new MemoryRandomAccessData(origin)), metadata: new Dictionary<string, string> { [UefConstants.ChunkIdMetadataKey] = "0000" }),
                new SequentialMediaSegment(1, SequentialSegmentKind.DataBlock, dataRange: new MediaDataRange(0, data.Length, MediaDataRangeKind.Stored, new MemoryRandomAccessData(data)), metadata: metadata)
            };
            source = MediaImageDocument.CreateUnexplored(new("memory.uef", []), TapeImageFormatIds.Uef, MediaKind.Tape, new SequentialMediaImageRepresentation(null, segments: segments), [], new Dictionary<string, string>());
        }
        var original = source.Representation;
        var converted = await new TapeSignalRepresentationConverter().ConvertAsync(source, TapeImageFormatIds.Wav, MediaRepresentationKind.Sequential, new Dictionary<string, string>());
        Assert.NotEmpty(converted.Losses);
        Assert.Equal(TapeImageFormatIds.Wav, converted.Document.FormatId);
        Assert.True(converted.Document.Metadata.ContainsKey("sampleRate"));
        Assert.Same(original, source.Representation);
        Assert.NotSame(original, converted.Document.Representation);
    }
}
