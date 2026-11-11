using System.IO;
using RetroMediaLab.MediaEngine;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Functions;
using RetroMediaLab.MediaEngine.Interfaces;
using RetroMediaLab.MediaEngine.PhysicalMedia.Reading;
using RetroMediaLab.MediaEngine.PhysicalMedia.Writing;
using RetroMediaLab.Tests.Application.TestInfrastructure;

namespace RetroMediaLab.Tests.Media;

public sealed class TapePhysicalMediaTests
{
    [Theory]
    [InlineData(44100)]
    [InlineData(48000)]
    public void PhysicalPlanRetainsEverySignedPcmValueAndItsRate(int rate)
    {
        var samples = Enumerable.Range(short.MinValue, 65536).Select(value => (short)value).ToArray();
        var engine = MediaEngineComposition.CreateDefault();
        var plan = engine.TapeMediaWritePlanningService.CreatePlan(new(samples, rate));
        Assert.Equal(MediaKind.Tape, plan.MediaKind);
        Assert.Equal(MediaRepresentationKind.Sequential, plan.RepresentationKind);
        Assert.Equal(new[] { 0 }, plan.WriteOrder);
        var unit = Assert.Single(plan.DataUnits);
        Assert.Equal(new byte[] { 0, 128, 1, 128 }, unit.Data.Span[..4].ToArray());
        Assert.Equal(new byte[] { 254, 127, 255, 127 }, unit.Data.Span[^4..].ToArray());
        var restored = engine.TapeMediaAcquisitionService.CreateSignal(new(plan.MediaKind, plan.RepresentationKind, plan.DataUnits));
        Assert.Equal(rate, restored.SampleRate);
        Assert.Equal(samples, restored.Samples.ToArray());
        samples[0] = 123;
        Assert.Equal(short.MinValue, restored.Samples.Span[0]);
        Assert.Equal(0, unit.Data.Span[0]);
    }

    [Fact]
    public void CaptureFragmentsKeepExactSilencesAndSampleOffsets()
    {
        var first = MediaTapePhysicalDataFunctions.CreateUnit(new(new short[] { 0, -1 }, 48000));
        var second = MediaTapePhysicalDataFunctions.CreateUnit(new(new short[] { 0, 0, 1 }, 48000), 2);
        var acquisition = new MediaAcquisitionResult(MediaKind.Tape, MediaRepresentationKind.Sequential, [second, first]);
        var signal = new TapeMediaAcquisitionService().CreateSignal(acquisition);
        Assert.Equal(new short[] { 0, -1, 0, 0, 1 }, signal.Samples.ToArray());
        Assert.Null(signal.Blocks);
        Assert.Null(signal.BlockPositions);
    }

    [Theory]
    [InlineData("empty")]
    [InlineData("partial-sample")]
    [InlineData("encoding")]
    [InlineData("missing-rate")]
    [InlineData("invalid-rate")]
    [InlineData("zero-rate")]
    [InlineData("changed-rate")]
    [InlineData("gap")]
    [InlineData("overlap")]
    public void MalformedAcquisitionIsRejectedWithoutInventingSamples(string fault)
    {
        var first = MediaTapePhysicalDataFunctions.CreateUnit(new(new short[] { -1, 0 }, 48000));
        var metadata = new Dictionary<string, string>(first.Metadata);
        if (fault == "encoding") metadata[MediaPhysicalMetadataKeys.Encoding] = "unknown";
        if (fault == "missing-rate") metadata.Remove(MediaPhysicalMetadataKeys.SampleRate);
        if (fault == "invalid-rate") metadata[MediaPhysicalMetadataKeys.SampleRate] = "48kHz";
        if (fault == "zero-rate") metadata[MediaPhysicalMetadataKeys.SampleRate] = "0";
        if (fault == "changed-rate") metadata[MediaPhysicalMetadataKeys.SampleRate] = "44100";
        var data = fault == "partial-sample" ? new byte[] { 0 } : first.Data.ToArray();
        var unit = new MediaPhysicalDataUnit(fault == "gap" ? 3 : fault == "overlap" ? 1 : 2, data, metadata);
        IReadOnlyList<MediaPhysicalDataUnit> units = fault == "empty" ? [] : [first, unit];
        var acquisition = new MediaAcquisitionResult(MediaKind.Tape, MediaRepresentationKind.Sequential, units);
        Assert.Throws<InvalidDataException>(() => new TapeMediaAcquisitionService().CreateSignal(acquisition));
    }

    [Theory]
    [InlineData(MediaKind.Floppy, MediaRepresentationKind.Sequential)]
    [InlineData(MediaKind.Tape, MediaRepresentationKind.Flux)]
    public void AcquisitionDoesNotReinterpretOtherMedia(MediaKind kind, MediaRepresentationKind representation)
    {
        Assert.Throws<NotSupportedException>(() => new TapeMediaAcquisitionService().CreateSignal(new(kind, representation, [])));
    }

    [Fact]
    public async Task FilePreparationUsesBorrowedCommonReaderAndExplicitSourceParameters()
    {
        using var cancellation = new CancellationTokenSource();
        var parameters = new Dictionary<string, string> { ["sampleRate"] = "44100" };
        var calls = 0;
        var preparation = ControlledDependencies.Simulate<IMediaRuntimePreparation>((method, args) =>
        {
            Assert.Equal(nameof(IMediaRuntimePreparation.ReadTapeAsync), method.Name);
            Assert.Equal("selected.raw", args[0]);
            Assert.Equal(cancellation.Token, args[1]);
            Assert.Null(args[2]);
            Assert.Same(parameters, args[3]);
            calls++;
            return Task.FromResult(new MediaTapeSignal(new short[] { 0, -123, 456, 0 }, 44100));
        });
        var plan = await new TapeMediaWritePlanningService(preparation).CreatePlanAsync("selected.raw", parameters, cancellation.Token);
        Assert.Equal(1, calls);
        Assert.Equal(new short[] { 0, -123, 456, 0 }, new TapeMediaAcquisitionService().CreateSignal(new(plan.MediaKind, plan.RepresentationKind, plan.DataUnits)).Samples.ToArray());
    }

    [Fact]
    public async Task CancellationBeforePreparationNeverCallsReader()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var service = new TapeMediaWritePlanningService(ControlledDependencies.Reject<IMediaRuntimePreparation>());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.CreatePlanAsync("selected.wav", cancellationToken: cancellation.Token));
    }

    [Fact]
    public void EmptyOrUnclockedSignalsAreNotPhysicalWritePlans()
    {
        var service = new TapeMediaWritePlanningService(ControlledDependencies.Reject<IMediaRuntimePreparation>());
        Assert.Throws<InvalidDataException>(() => service.CreatePlan(new(ReadOnlyMemory<short>.Empty, 48000)));
        Assert.Throws<ArgumentOutOfRangeException>(() => service.CreatePlan(new(new short[] { 1 }, 0)));
    }
}
