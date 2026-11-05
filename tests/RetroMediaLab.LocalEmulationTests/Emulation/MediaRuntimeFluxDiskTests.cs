using RetroMediaLab.MediaEngine;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Functions;
using RetroMediaLab.MediaEngine.Images.Models.Flux;
using RetroMediaLab.MediaEngine.Services;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Hfe;

namespace RetroMediaLab.Tests.Emulation;

public sealed class MediaRuntimeFluxDiskTests
{
    [Fact]
    public void CapturedRevolutionsRetainTheirOwnResolutionAndIndex()
    {
        var track = new ProtectedTrack(0, 0, null, [], [], [],
            [new(25, new(8000, [320, 160, 240])), new(50, new(4001, [161, 79, 121]))]);
        var result = MediaRuntimeFluxDisk.ToRuntimeTrack(track);
        Assert.Equal(2, result.Revolutions.Count);
        Assert.Equal(200000u, result.Revolutions[0].IndexTimeNanoseconds);
        Assert.Equal(new uint[] { 8000, 4000, 6000 }, result.Revolutions[0].FluxIntervalsNanoseconds);
        Assert.Equal(200050u, result.Revolutions[1].IndexTimeNanoseconds);
        Assert.Equal(new uint[] { 8050, 3950, 6050 }, result.Revolutions[1].FluxIntervalsNanoseconds);
        var roundTrip = MediaRuntimeFluxDisk.ToRuntimeTrack(MediaRuntimeFluxDisk.FromRuntimeTrack(0, 0, result));
        Assert.Equal(MediaFluxTrackDataFunctions.Serialize(result), MediaFluxTrackDataFunctions.Serialize(roundTrip));
    }

    [Fact]
    public void BitCellsDoNotInventAnImpulseAtTheEndOfTheSilentTail()
    {
        var track = Bits([true, false, true, false, false], 2000);
        var result = MediaRuntimeFluxDisk.ToRuntimeTrack(track).Revolutions[0];
        Assert.Equal(10000u, result.IndexTimeNanoseconds);
        Assert.Equal(new uint[] { 2000, 4000 }, result.FluxIntervalsNanoseconds);
        var restored = MediaRuntimeFluxDisk.FromRuntimeTrack(0, 0, new([result]));
        Assert.Equal(result.FluxIntervalsNanoseconds, MediaRuntimeFluxDisk.ToRuntimeTrack(restored).Revolutions[0].FluxIntervalsNanoseconds);
    }

    [Fact]
    public void VariableBitTimingIsRetainedDuringPhysicalReading()
    {
        var track = new ProtectedTrack(0, 0, [true, false, true, true],
            [new(0, 2, 2000), new(2, 2, 4000)], [], [], []);
        var result = MediaRuntimeFluxDisk.ToRuntimeTrack(track).Revolutions[0];
        Assert.Equal(12000u, result.IndexTimeNanoseconds);
        Assert.Equal(new uint[] { 2000, 6000, 4000 }, result.FluxIntervalsNanoseconds);
    }

    [Theory]
    [InlineData(0u, 4000u, false)]
    [InlineData(6000u, 6000u, false)]
    [InlineData(6000u, 6000u, true)]
    public void PartialWriteAcrossIndexKeepsThePhysicalBitGrid(uint start, uint duration, bool includeEnd)
    {
        var track = Bits([true, true, true, true, true], 2000);
        var source = MediaRuntimeFluxDisk.ToRuntimeTrack(track).Revolutions[0];
        uint[] pulses = [2000];
        var changed = MediaFluxTrackDataFunctions.ReplaceWindow(source, start, duration, pulses, includeEnd);
        var replacement = MediaRuntimeFluxDisk.FromRuntimeTrack(0, 0, new([changed]));
        var actual = MediaRuntimeFluxDisk.ToRuntimeTrack(replacement).Revolutions[0];
        Assert.Equal(changed.IndexTimeNanoseconds, actual.IndexTimeNanoseconds);
        Assert.Equal(changed.FluxIntervalsNanoseconds, actual.FluxIntervalsNanoseconds);
        Assert.All(Enumerable.Range(0, track.Bits!.Count), index =>
        {
            var phase = (uint)((index + 1) * 2000) % source.IndexTimeNanoseconds;
            var relative = (phase + source.IndexTimeNanoseconds - start) % source.IndexTimeNanoseconds;
            if (relative > duration || !includeEnd && relative == duration)
                Assert.Contains(phase == 0 ? source.IndexTimeNanoseconds : phase,
                    actual.FluxIntervalsNanoseconds.Select((_, position) => actual.FluxIntervalsNanoseconds.Take(position + 1).Aggregate(0u, (sum, interval) => sum + interval)));
        });
    }

    [Fact]
    public void OffGridWriteRemainsPhysicalInsteadOfBeingRoundedIntoAnotherImpulse()
    {
        var original = Bits([true, false, true, false], 2000);
        var written = MediaRuntimeFluxDisk.FromRuntimeTrack(0, 0, new([new(8000, [2100])]));
        Assert.Equal(new uint[] { 2100 }, MediaRuntimeFluxDisk.ToRuntimeTrack(written).Revolutions[0].FluxIntervalsNanoseconds);
        Assert.Equal(new[] { true, false, true, false }, original.Bits);
    }

    [Fact]
    public void UncapturedTrackHasNoSyntheticFieldsAndNoInventedGeometry()
    {
        var image = new ProtectedTrackImage([new(1, 1, null, [], [], [], [new(25, new(8000, [320])), new(25, new(8001, [321]))])], false);
        var document = MediaImageDocument.CreateUnexplored(new("memory.scp", []), DiskImageFormatIds.RawScp,
            MediaKind.Floppy, new FluxMediaImageRepresentation(image), [], new Dictionary<string, string>());
        MediaRuntimeFluxDisk? runtime = null;
        try
        {
            runtime = new(MediaEngineComposition.CreateDefault(), document, false, false);
            Assert.Equal(2, runtime.Cylinders);
            Assert.Equal(2, runtime.Heads);
            Assert.Equal(0, runtime.SectorSize);
            Assert.Equal(0, runtime.SectorCount);
            Assert.Equal(0, runtime.SectorsPerTrack);
            Assert.Null(runtime.FindSector(1, 1, 0));
            var missing = runtime.ReadTrack(0, 0, string.Empty, 4000, 200000);
            Assert.Equal(2, missing.Revolutions.Count);
            Assert.All(missing.Revolutions, revolution => Assert.Empty(revolution.FluxIntervalsNanoseconds));
            Assert.Same(runtime.ReadTrack(1, 1, string.Empty, 4000, 200000), runtime.ReadTrack(1, 1, string.Empty, 2000, 300000));
            Assert.Throws<UnauthorizedAccessException>(() => runtime.WriteTrackWindow(1, 1, string.Empty, 4000, 200000, 0, 4000, []));
            Assert.Throws<ArgumentOutOfRangeException>(() => runtime.ReadTrack(2, 0, string.Empty, 4000, 200000));
        }
        finally { runtime?.Dispose(); }
        Assert.Throws<ObjectDisposedException>(() => runtime!.ReadTrack(0, 0, string.Empty, 4000, 200000));
    }

    private static ProtectedTrack Bits(bool[] bits, uint cell) => new(0, 0, bits,
        [new(0, bits.Length, cell)], [], [], []);

    [Fact]
    public void HfeUsesOneFinerExactGridAndPreservesTheUnwrittenTrackSignal()
    {
        var original = Bits([true, false], 4000);
        var changed = MediaRuntimeFluxDisk.FromRuntimeTrack(1, 0, new([new(8000, [3000, 4000])]));
        var hfe = ProtectedTrackHfeImageAdapter.Create(new([original, changed], false),
            new Dictionary<string, string> { [HfeMetadataKeys.BitRate] = "125", [HfeMetadataKeys.Encoding] = "2" });
        var restored = HfeProtectedTrackImageAdapter.Create(hfe);
        Assert.Equal(500, hfe.BitRate);
        Assert.Equal(new uint[] { 4000 }, MediaRuntimeFluxDisk.ToRuntimeTrack(restored.Tracks[0]).Revolutions[0].FluxIntervalsNanoseconds);
        Assert.Equal(new uint[] { 3000, 4000 }, MediaRuntimeFluxDisk.ToRuntimeTrack(restored.Tracks[1]).Revolutions[0].FluxIntervalsNanoseconds);
    }

    [Fact]
    public void HfeRejectsTimingOutsideItsResolutionRatherThanRoundingIt()
    {
        var changed = MediaRuntimeFluxDisk.FromRuntimeTrack(0, 0, new([new(8000, [3001])]));
        Assert.Throws<NotSupportedException>(() => ProtectedTrackHfeImageAdapter.Create(new([changed], false), new Dictionary<string, string>()));
    }

    [Theory]
    [InlineData(500u)]
    [InlineData(1000u)]
    [InlineData(1500u)]
    public void HfeV3PreservesSubcellPhaseAndTheSilentTail(uint phase)
    {
        var source = new MediaFluxTrackData([new(200000000, new uint[] { phase, 8000, 12000, 4000 })]);
        var track = MediaRuntimeFluxDisk.FromRuntimeTrack(0, 0, source);
        var bytes = HfeV3ImageBuilder.Build(new([track], false), new Dictionary<string, string> { [HfeMetadataKeys.Encoding] = "2" });
        var read = HfeProtectedTrackImageAdapter.Create(new HfeReader().Read(bytes));
        var actual = MediaRuntimeFluxDisk.ToRuntimeTrack(read.Tracks[0]).Revolutions[0];
        Assert.Equal(source.Revolutions[0].IndexTimeNanoseconds, actual.IndexTimeNanoseconds);
        Assert.Equal(source.Revolutions[0].FluxIntervalsNanoseconds, actual.FluxIntervalsNanoseconds);
        Assert.True(bytes.Length < 20000);
    }

    [Fact]
    public void HfeV3EscapesDataWhichLooksLikeOpcodes()
    {
        var source = new MediaFluxTrackData([new(200000000, Enumerable.Repeat(4000u, 50000).ToArray())]);
        var bytes = HfeV3ImageBuilder.Build(new([MediaRuntimeFluxDisk.FromRuntimeTrack(0, 0, source)], false),
            new Dictionary<string, string> { [HfeMetadataKeys.Encoding] = "2" });
        var actual = MediaRuntimeFluxDisk.ToRuntimeTrack(HfeProtectedTrackImageAdapter.Create(new HfeReader().Read(bytes)).Tracks[0]);
        Assert.Equal(source.Revolutions[0].FluxIntervalsNanoseconds, actual.Revolutions[0].FluxIntervalsNanoseconds);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void HfeHeaderProtectionSurvivesModelAndPhysicalRepresentationRoundTrips(bool version3, bool protectedImage)
    {
        var image = new ProtectedTrackImage([Bits([true, false, true, false, true, false, true, false], 4000)], protectedImage);
        var metadata = new Dictionary<string, string> { [HfeMetadataKeys.Encoding] = "2" };
        var bytes = version3 ? HfeV3ImageBuilder.Build(image, metadata)
            : HfeWriter.Build(ProtectedTrackHfeImageAdapter.Create(image, metadata));
        Assert.Equal(protectedImage ? HfeFormat.WriteForbidden : HfeFormat.WriteAllowed, bytes[HfeLayout.WriteAllowedOffset]);
        var read = new HfeReader().Read(bytes);
        Assert.Equal(protectedImage, read.WriteProtected);
        var physical = HfeProtectedTrackImageAdapter.Create(read);
        Assert.Equal(protectedImage, physical.WriteProtected);
        Assert.Equal(image.Tracks[0].Bits, physical.Tracks[0].Bits);
        var rewritten = HfeWriter.Build(ProtectedTrackHfeImageAdapter.Create(physical, metadata));
        Assert.Equal(protectedImage ? HfeFormat.WriteForbidden : HfeFormat.WriteAllowed, rewritten[HfeLayout.WriteAllowedOffset]);
    }

    [Fact]
    public void ProtectedFluxRuntimeRejectsWriteWindowsEvenWhenTheCallerRequestsWriting()
    {
        var image = new ProtectedTrackImage([Bits([true, false, true, false, true, false, true, false], 4000)], true);
        var document = MediaImageDocument.CreateUnexplored(new("memory.hfe", []), DiskImageFormatIds.RawHfe,
            MediaKind.Floppy, new FluxMediaImageRepresentation(image), [], new Dictionary<string, string>());
        MediaRuntimeFluxDisk? runtime = null;
        try
        {
            runtime = new(MediaEngineComposition.CreateDefault(), document, true, false);
            Assert.True(runtime.IsReadOnly);
            var before = runtime.ReadTrack(0, 0, string.Empty, 4000, 32000);
            Assert.Throws<UnauthorizedAccessException>(() => runtime.WriteTrackWindow(0, 0, string.Empty, 4000, 32000, 0, 8000, []));
            Assert.False(runtime.HasChanges);
            Assert.Same(before, runtime.ReadTrack(0, 0, string.Empty, 4000, 32000));
        }
        finally { runtime?.Dispose(); }
    }
}
