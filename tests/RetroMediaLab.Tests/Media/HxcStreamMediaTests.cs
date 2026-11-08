using System.IO;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.HxcStream;
using RetroMediaLab.MediaEngine.Images.Models.Flux;
using RetroMediaLab.MediaEngine.Images.Writing;

namespace RetroMediaLab.Tests.Media;

public sealed class HxcStreamMediaTests
{
    [Fact]
    public void IndexBoundariesDoNotMoveToTheNextFluxTransition()
    {
        var track = Track(0, 0, new(320, new uint[] { 31, 53, 100 }), new(400, new uint[] { 42, 17, 200 }));
        var bytes = HxcStreamEncoding.Encode(track, true, new Dictionary<string, string>());
        var decoded = HxcStreamReader.ReadTrackBytes(bytes);
        Assert.True(decoded.WriteProtected);
        Assert.Equal(2, decoded.Revolutions.Count);
        for (var at = 0; at < 2; at++)
        {
            Assert.Equal(track.Revolutions[at].Flux.IndexTimeTicks, decoded.Revolutions[at].Flux.IndexTimeTicks);
            Assert.Equal(track.Revolutions[at].Flux.FluxIntervals, decoded.Revolutions[at].Flux.FluxIntervals);
        }
    }

    [Fact]
    public void AllVariableLengthPulseCodesRoundTrip()
    {
        uint[] pulses = [127, 128, 16384, 2097152];
        var sum = pulses.Sum(value => (long)value);
        var duration = checked((uint)((sum + 31) / 16 * 16));
        var track = Track(0, 0, new FluxRevolution(duration, pulses));
        var result = HxcStreamReader.ReadTrackBytes(HxcStreamEncoding.Encode(track, false, new Dictionary<string, string>()));
        Assert.Equal(pulses, Assert.Single(result.Revolutions).Flux.FluxIntervals);
    }

    [Fact]
    public void IndexTimingPreservesARevolutionWithoutMagneticTransitions()
    {
        var track = Track(0, 0, new FluxRevolution(320, Array.Empty<uint>()));
        var result = HxcStreamReader.ReadTrackBytes(HxcStreamEncoding.Encode(track, false, new Dictionary<string, string>()));
        Assert.Empty(Assert.Single(result.Revolutions).Flux.FluxIntervals);
        Assert.Equal(320u, result.Revolutions[0].Flux.IndexTimeTicks);
    }

    [Fact]
    public async Task RelatedTracksUseTheNativeWriterAndKeepUnchangedCaptureBytes()
    {
        var track = Track(0, 0, new FluxRevolution(320, new uint[] { 31, 53, 100 }));
        var original = HxcStreamEncoding.Encode(track, true, new Dictionary<string, string> { ["name"] = "native" });
        var other = Track(1, 1, new FluxRevolution(400, new uint[] { 23, 77, 110 }));
        var metadata = new Dictionary<string, string>
        {
            ["hxcstream.native.0.0"] = Convert.ToBase64String(original),
            ["hxcstream.digest.0.0"] = HxcStreamEncoding.Digest(track, true)
        };
        var document = MediaImageDocument.CreateUnexplored(new("memory00.0.hxcstream", []), DiskImageFormatIds.RawHxcStream,
            MediaKind.Floppy, new FluxMediaImageRepresentation(new ProtectedTrackImage([track, other], true)), [], metadata);
        var published = new Dictionary<string, byte[]>();
        var writer = new HxcStreamWriter(async (files, token) =>
        {
            foreach (var pair in files)
            {
                using var stream = new MemoryStream(); await pair.Value(stream, token); published[pair.Key] = stream.ToArray();
            }
        });
        var paths = await writer.WriteAsync(document, "memory00.0.hxcstream", DiskImageFormatIds.RawHxcStream);
        Assert.Equal(new[] { "memory00.0.hxcstream", "memory01.1.hxcstream" }, paths);
        Assert.Equal(original, published[paths[0]]);
        Assert.Equal(other.Revolutions[0].Flux.FluxIntervals, Assert.Single(HxcStreamReader.ReadTrackBytes(published[paths[1]]).Revolutions).Flux.FluxIntervals);
        Assert.NotEmpty(MediaWritingComposition.CreateDefault().Registry.FindTargetCandidates(DiskImageFormatIds.RawHxcStream, ".hxcstream"));
    }

    [Fact]
    public void InexactTimingAndDamagedChunksAreRejected()
    {
        var track = new ProtectedTrack(0, 0, null, [], [], [], [new(25, new(320, new uint[] { 1, 3, 20 }))]);
        Assert.False(HxcStreamEncoding.CanEncode(track));
        Assert.Throws<InvalidDataException>(() => HxcStreamEncoding.Encode(track, false, new Dictionary<string, string>()));
        var bytes = HxcStreamEncoding.Encode(Track(0, 0, new FluxRevolution(320, new uint[] { 10, 20 })), false, new Dictionary<string, string>());
        bytes[8] ^= 1;
        Assert.Throws<InvalidDataException>(() => HxcStreamReader.ReadTrackBytes(bytes));
    }

    private static ProtectedTrack Track(int cylinder, int head, params FluxRevolution[] revolutions) =>
        new(cylinder, head, null, [], [], [], revolutions.Select(flux => new TrackFluxRevolution(40, flux)).ToArray());
}
