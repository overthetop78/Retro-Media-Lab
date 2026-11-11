using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Functions;
using RetroMediaLab.MediaEngine.Images.Models.Sequential;
using RetroMediaLab.MediaEngine.Images.Reading.Sources;

namespace RetroMediaLab.MediaAudit.Acorn;

public sealed class UefPulseEndpointTests
{
    [Theory]
    [InlineData(180, true, 16000)]
    [InlineData(180, false, -16000)]
    [InlineData(0, true, -16000)]
    [InlineData(0, false, 16000)]
    public async Task PartialSecurityCycleUsesThePulseDefinedByTheUefSpecification(int phase, bool highOnly, short expected)
    {
        byte[] bytes = [1, 0, 0, (byte)(highOnly ? 'P' : 'W'), (byte)(highOnly ? 'W' : 'P'), 0];
        var metadata = new Dictionary<string, string> { [UefConstants.ChunkIdMetadataKey] = "0114", [UefConstants.BaseFrequencyMetadataKey] = "1200", [UefConstants.BaudRateMetadataKey] = "300", [UefConstants.PhaseMetadataKey] = phase.ToString(System.Globalization.CultureInfo.InvariantCulture) };
        var segment = new SequentialMediaSegment(0, SequentialSegmentKind.Pulse, dataRange: new MediaDataRange(0, bytes.Length, MediaDataRangeKind.Stored, new MemoryRandomAccessData(bytes)), metadata: metadata);
        var signal = await TapeSignalFunctions.UefAsync(new(null, segments: [segment]), new(), CancellationToken.None);
        Assert.Equal(Enumerable.Repeat(expected, 20), signal.Samples.ToArray());
    }

    [Fact]
    public async Task WritingIsolatedPulsesUsesTheSpecifiedEndpointFlags()
    {
        short[] samples = [16000, 16000, 16000, -16000, -16000, 16000, 0, 0, -16000];
        var document = TapeSignalFunctions.UefDocument(new("memory.uef", []), new(samples, 48000), CancellationToken.None);
        var flags = new List<string>();
        foreach (var segment in ((SequentialMediaImageRepresentation)document.Representation).Segments!)
        {
            if (segment.Metadata.GetValueOrDefault(UefConstants.ChunkIdMetadataKey) != "0114") continue;
            var bytes = await RetroMediaLab.MediaEngine.Images.Reading.Blocks.BlockMediaDataReader.ReadRangeAsync(segment.DataRange, CancellationToken.None);
            flags.Add(System.Text.Encoding.ASCII.GetString(bytes!, 3, 2));
        }
        Assert.Equal(new[] { "WP", "PW", "WP", "PW" }, flags);
    }
}
