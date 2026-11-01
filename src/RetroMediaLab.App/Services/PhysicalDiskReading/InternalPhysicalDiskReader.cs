using RetroMediaLab.MediaEngine;
using System.Globalization;
using RetroMediaLab.App.Constants.Services.PhysicalDiskReading;
using RetroMediaLab.App.Contracts.Services.PhysicalDiskReading;
using RetroMediaLab.App.Enums.Services.PhysicalDiskReading;
using RetroMediaLab.App.Functions.Services.PhysicalDiskReading;
using MediaPhysicalMetadataKeys = RetroMediaLab.MediaEngine.Constants.MediaPhysicalMetadataKeys;
using MediaAcquisitionProgress = RetroMediaLab.MediaEngine.Contracts.MediaAcquisitionProgress;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.Infrastructure.Constants;
using RetroMediaLab.Infrastructure.Hardware.Greaseweazle;
using RetroMediaLab.Infrastructure.Hardware.Media;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Scp.Reading;
using RetroMediaLab.MediaEngine.Interfaces;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Scp;

namespace RetroMediaLab.App.Services.PhysicalDiskReading;

public sealed class InternalPhysicalDiskReader(Func<IGreaseweazleReadDevice> deviceFactory)
{
    public async Task<PhysicalDiskReadResult> ReadAsync(PhysicalDiskReadOptions options, string outputPath, IProgress<PhysicalDiskReadOperationProgress>? progress = null, CancellationToken cancellationToken = default, string? targetFormatId = null)
    {
        var fluxService = new FloppyFluxAcquisitionService();
        var registry = new MediaAcquisitionProviderRegistry([new GreaseweazleMediaAcquisitionProvider(deviceFactory)]);
        var acquisitionProgress = new Progress<MediaAcquisitionProgress>(value => ReportProgress(value, options.Tracks, fluxService, progress));
        var acquisition = await registry.AcquireAsync(MediaKind.Floppy, options.PortName, CreateOptions(options), acquisitionProgress, cancellationToken).ConfigureAwait(false);
        return await PhysicalDiskReadService.CreateDefault().ReadAsync(acquisition, outputPath, progress, cancellationToken, targetFormatId).ConfigureAwait(false);
    }

    public static InternalPhysicalDiskReader CreateDefault() => new(() => new GreaseweazleProtocolClient(new WindowsGreaseweazleSerialTransport()));

    internal static IReadOnlyDictionary<string, string> CreateOptions(PhysicalDiskReadOptions options)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [GreaseweazleMediaOptionKeys.BusType] = options.BusType.ToString(),
            [GreaseweazleMediaOptionKeys.DriveUnit] = options.DriveUnit.ToString(CultureInfo.InvariantCulture),
            [GreaseweazleMediaOptionKeys.Tracks] = string.Join(';', options.Tracks.Select(track => $"{track.Cylinder}:{track.Head}:{track.DriveCylinder}:{track.DriveHead}")),
            [GreaseweazleMediaOptionKeys.DiskType] = options.DiskType.ToString(),
            [GreaseweazleMediaOptionKeys.Revolutions] = options.Revolutions.ToString(CultureInfo.InvariantCulture),
            [GreaseweazleMediaOptionKeys.FluxOverflowRetries] = options.FluxOverflowRetries.ToString(CultureInfo.InvariantCulture),
            [GreaseweazleMediaOptionKeys.SeekRetries] = options.SeekRetries.ToString(CultureInfo.InvariantCulture),
            [GreaseweazleMediaOptionKeys.HardSectors] = options.HardSectors.ToString(CultureInfo.InvariantCulture)
        };
        AddMilliseconds(values, GreaseweazleMediaOptionKeys.FakeIndexMilliseconds, options.FakeIndexPeriod);
        AddMilliseconds(values, GreaseweazleMediaOptionKeys.MotorSpinUpMilliseconds, options.MotorSpinUpDelay);
        AddMilliseconds(values, GreaseweazleMediaOptionKeys.TrackSettleMilliseconds, options.TrackSettleDelay);
        return values;
    }

    private static void AddMilliseconds(IDictionary<string, string> values, string key, TimeSpan? duration)
    {
        if (duration is { } present)
            values[key] = present.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
    }

    private static void ReportProgress(MediaAcquisitionProgress value, IReadOnlyList<PhysicalDiskTrackAddress> tracks, FloppyFluxAcquisitionService fluxService, IProgress<PhysicalDiskReadOperationProgress>? progress)
    {
        var cylinder = MediaEngineComposition.FileSystems.ReadIntOrDefault(value.Metadata, MediaPhysicalMetadataKeys.Cylinder);
        var head = MediaEngineComposition.FileSystems.ReadIntOrDefault(value.Metadata, MediaPhysicalMetadataKeys.Head);
        IPiste? acquiredTrack = null;
        if (value.AcquiredUnit is not null)
        {
            var track = fluxService.CreateTrack(value.AcquiredUnit);
            acquiredTrack = ScpTrackContractMapper.FromScpTrack(track, ScpFormatConstants.ResolutionStepNanoseconds * (ScpFormatConstants.InternalCaptureResolution + ScpFormatConstants.ResolutionIndexOffset));
        }
        progress?.Report(new PhysicalDiskReadOperationProgress(PhysicalDiskReadStage.Acquiring, value.CompletedUnits, value.TotalUnits, cylinder, head, value.Attempt, tracks, acquiredTrack));
    }

}
