using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Images.Models.Sequential;
using RetroMediaLab.MediaEngine.Images.Reading.Decoding.Sequential.Acorn;
using RetroMediaLab.MediaEngine.Images.Writing.Encoding.Sequential.Acorn;

namespace RetroMediaLab.MediaAudit.Acorn;

public sealed class AcornPcmSegmentationTests
{
    [Fact]
    public async Task PresentationBoundariesInsideAByteDoNotChangeTheDecodedBbcFile()
    {
        const int rate = 48000;
        var payload = Enumerable.Range(0, 64).Select(value => (byte)value).ToArray();
        var metadata = new Dictionary<string, string> { ["fileName"] = "P", ["loadAddress"] = "ffff1900", ["executionAddress"] = "ffff1900", ["blockNumber"] = "0", ["flags"] = "80", ["nextAddress"] = "00000000" };
        var block = new SequentialDecodedBlock(0, payload, [], true, metadata);
        var representation = await new AcornTapeEncoder().EncodeAsync(new(TapeImageFormatIds.Wav, "acorn-bbc", [block], parameters: new Dictionary<string, string> { ["sampleRate"] = "48000" }));
        var profile = new Dictionary<string, string> { ["channels"] = "1", ["sampleRate"] = "48000", ["bitsPerSample"] = "16", ["blockAlign"] = "2" };
        var original = MediaImageDocument.CreateUnexplored(new("memory.wav", []), TapeImageFormatIds.Wav, MediaKind.Tape, representation, [], profile);
        var decoder = new AcornTapeDecoder();
        var before = await decoder.DecodeAsync(original);
        Assert.True(Assert.Single(before.Blocks).IntegrityValid);
        var segment = Assert.Single(representation.Segments!);
        var range = segment.DataRange!;
        var count = checked((int)(range.Length / 2));
        var cut = checked((int)(rate * UefConstants.InitialCarrierSeconds)) + 23 * 40 + 17;
        Assert.InRange(cut, 1, count - 1);
        var split = new[]
        {
            new SequentialMediaSegment(0, SequentialSegmentKind.Samples, cut, TimeSpan.Zero, TimeSpan.FromSeconds((double)cut / rate), channelNumber: 0, dataRange: new MediaDataRange(0, cut * 2L, MediaDataRangeKind.Stored, range.Source, range.SourceOffset)),
            new SequentialMediaSegment(1, SequentialSegmentKind.Samples, count - cut, TimeSpan.FromSeconds((double)cut / rate), TimeSpan.FromSeconds((double)(count - cut) / rate), channelNumber: 0, dataRange: new MediaDataRange(0, (count - cut) * 2L, MediaDataRangeKind.Stored, range.Source, range.SourceOffset + cut * 2L))
        };
        var indexed = MediaImageDocument.CreateUnexplored(original.Source, original.FormatId, original.MediaKind, new SequentialMediaImageRepresentation(representation.LogicalLength, representation.Duration, split), [], profile);
        var after = await decoder.DecodeAsync(indexed);
        var file = Assert.Single(after.Blocks);
        Assert.True(file.IntegrityValid);
        Assert.Equal(payload, file.Data.ToArray());
    }
}
