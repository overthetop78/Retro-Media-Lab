using System.IO;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.HxcStream;
using RetroMediaLab.MediaEngine.Images.Models.Flux;
using RetroMediaLab.MediaEngine.Images.Writing;
using RetroMediaLab.MediaEngine.Images.Reading;
using RetroMediaLab.MediaEngine.PhysicalMedia.Writing;
using RetroMediaLab.MediaEngine.Functions;
using RetroMediaLab.MediaEngine;
using RetroMediaLab.MediaEngine.Images.Models.Sectors;

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
        var original = HxcStreamEncoding.Encode(track, true, new Dictionary<string, string> { [HxcStreamMetadataKeys.Fields(0, 0) + "name"] = "native" });
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
        await Assert.ThrowsAsync<InvalidDataException>(() => writer.WriteAsync(document, "silently-renamed.hxcstream", DiskImageFormatIds.RawHxcStream));
        await Assert.ThrowsAsync<InvalidDataException>(() => writer.WriteAsync(document, "wrong43.0.hxcstream", DiskImageFormatIds.RawHxcStream));
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

    [Fact]
    public void PhysicalWritePlanKeepsFluxAndSilentEndOfRevolution()
    {
        var track = Track(3, 1, new FluxRevolution(320, new uint[] { 31, 53, 100 }));
        var service = new FloppyMediaWritePlanningService(DiskImageExplorer.CreateDefault());
        var plan = service.CreatePlan(new ProtectedTrackImage([track], false));
        var unit = Assert.Single(plan.DataUnits);
        var revolution = Assert.Single(MediaFluxTrackDataFunctions.Deserialize(unit.Data.Span).Revolutions);
        Assert.Equal(12800u, revolution.IndexTimeNanoseconds);
        Assert.Equal(new uint[] { 1240, 2120, 4000 }, revolution.FluxIntervalsNanoseconds);
    }

    [Fact]
    public async Task CommonExplorerReadsFilesFromFluxAndKeepsTheCaptureForVisualization()
    {
        var bytes = new byte[10 * 256];
        System.Text.Encoding.ASCII.GetBytes("HELLO  $", bytes.AsSpan(8));
        bytes[256 + 5] = 8; bytes[256 + 7] = 10;
        bytes[256 + 8 + 4] = 5; bytes[256 + 8 + 7] = 2;
        "12345"u8.CopyTo(bytes.AsSpan(512));
        var blocks = Enumerable.Range(0, 10).Select(index => new SectorBlock(index, new(0, 0, index), bytes.AsSpan(index * 256, 256).ToArray()));
        var sectors = new SectorImage("acorn.dfs.ss", 256, 1, 1, 10, blocks);
        var source = MediaImageDocument.CreateUnexplored(new("source.ssd", []), sectors.FormatId, MediaKind.Floppy,
            new SectorMediaImageRepresentation(sectors), [], new Dictionary<string, string>());
        var engine = MediaEngineComposition.CreateDefault();
        var jfd = RetroMediaLab.MediaEngine.Images.Formats.Floppy.Jfd.JfdReader.ReadBytes(new("memory.jfd", []),
            RetroMediaLab.MediaEngine.Images.Formats.Floppy.Jfd.JfdCodec.Create(sectors));
        Assert.Contains("HELLO", (await engine.Explorer.ExploreAsync(jfd)).FirstFileName);
        var converter = engine.Conversion.Registry.Resolve(source, DiskImageFormatIds.RawHxcStream, MediaRepresentationKind.Flux)!;
        var converted = await converter.ConvertAsync(source, DiskImageFormatIds.RawHxcStream, MediaRepresentationKind.Flux, new Dictionary<string, string>());
        var flux = (FluxMediaImageRepresentation)converted.Document.Representation;
        var native = HxcStreamEncoding.Encode(Assert.Single(flux.Tracks), false, new Dictionary<string, string>());
        var track = new ProtectedTrack(0, 0, null, [], [], [], HxcStreamReader.ReadTrackBytes(native).Revolutions);
        var capture = MediaImageDocument.CreateUnexplored(new("track00.0.hxcstream", []), DiskImageFormatIds.RawHxcStream, MediaKind.Floppy,
            new FluxMediaImageRepresentation(new ProtectedTrackImage([track], false)), [], new Dictionary<string, string>());
        var decodedDisk = await DiskImageExplorer.CreateDefault().ExploreAsync(capture, DiskImageFormatIds.RawHxcStream);
        var decodedDocument = MediaImageDocument.CreateUnexplored(capture.Source, decodedDisk.Image.FormatId, MediaKind.Floppy,
            new SectorMediaImageRepresentation(decodedDisk.Image), capture.Diagnostics, capture.Metadata);
        var result = await engine.Explorer.ExploreAsync(decodedDocument);
        Assert.IsType<FluxMediaImageRepresentation>(capture.Representation);
        var file = Assert.Single(result.Volumes.Where(volume => volume.FileSystem is not null).SelectMany(volume => volume.FileSystem!.Entries));
        Assert.Contains("HELLO", file.Name);
        Assert.Equal("12345"u8.ToArray(), file.Content);
        Assert.NotNull(engine.Visualization.Registry.CreateDescriptor(capture));
    }

    [Fact]
    public void ExportDoesNotCopyOtherContainersOrOtherTracksMetadata()
    {
        var track = Track(0, 0, new FluxRevolution(320, new uint[] { 10, 20 }));
        var metadata = new Dictionary<string, string>
        {
            ["jfd.native"] = "unrelated",
            [HxcStreamMetadataKeys.Fields(0, 0) + "name"] = "first",
            [HxcStreamMetadataKeys.Fields(1, 0) + "name"] = "second"
        };
        var decoded = HxcStreamReader.ReadTrackBytes(HxcStreamEncoding.Encode(track, false, metadata));
        Assert.Equal("first", decoded.Metadata["name"]);
        Assert.DoesNotContain("jfd.native", decoded.Metadata.Keys);
        Assert.Equal(2, decoded.Metadata.Count); // Native name and generated sample rate.
    }

    [Fact]
    public void DeclarativeWeakBitsCannotBeSilentlyExportedAsOneFixedCapture()
    {
        var track = new ProtectedTrack(0, 0, new bool[16], [], [],
            [new(TrackFeatureKind.WeakRegion, 0, 16)], [new(40, new(320, new uint[] { 10, 20 }))]);
        var document = MediaImageDocument.CreateUnexplored(new("track00.0.hxcstream", []), DiskImageFormatIds.RawHxcStream,
            MediaKind.Floppy, new FluxMediaImageRepresentation(new ProtectedTrackImage([track], false)), [],
            new Dictionary<string, string>
            {
                [HxcStreamMetadataKeys.Native(0, 0)] = "",
                [HxcStreamMetadataKeys.Digest(0, 0)] = HxcStreamEncoding.Digest(track, false)
            });
        Assert.False(new HxcStreamWriter().CanWrite(document, DiskImageFormatIds.RawHxcStream, ".hxcstream"));
    }

    private static ProtectedTrack Track(int cylinder, int head, params FluxRevolution[] revolutions) =>
        new(cylinder, head, null, [], [], [], revolutions.Select(flux => new TrackFluxRevolution(40, flux)).ToArray());
}
