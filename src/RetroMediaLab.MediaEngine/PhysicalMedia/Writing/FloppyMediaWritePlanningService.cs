using System.Globalization;
using MediaPhysicalEncodingIds = RetroMediaLab.MediaEngine.Constants.MediaPhysicalEncodingIds;
using MediaPhysicalMetadataKeys = RetroMediaLab.MediaEngine.Constants.MediaPhysicalMetadataKeys;
using MediaFluxRevolutionData = RetroMediaLab.MediaEngine.Contracts.MediaFluxRevolutionData;
using MediaFluxTrackData = RetroMediaLab.MediaEngine.Contracts.MediaFluxTrackData;
using MediaPhysicalDataUnit = RetroMediaLab.MediaEngine.Contracts.MediaPhysicalDataUnit;
using MediaWritePlan = RetroMediaLab.MediaEngine.Contracts.MediaWritePlan;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Functions;
using RetroMediaLab.MediaEngine.Images.Writing.Encoding;
using RetroMediaLab.MediaEngine.Images.Reading;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Scp;
using System.IO;
using RetroMediaLab.MediaEngine.Images.Models.Flux;
using RetroMediaLab.MediaEngine.Images.Models.Sectors;

namespace RetroMediaLab.MediaEngine.PhysicalMedia.Writing;

/// <summary>Builds neutral physical floppy write plans from recognized images.</summary>
public sealed class FloppyMediaWritePlanningService(DiskImageExplorer explorer)
{
    public async Task<MediaWritePlan> CreatePlanAsync(string sourcePath, string? formatId = null, int scpRevolution = 0, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        var document = await explorer.ReadDocumentAsync(sourcePath, formatId, cancellationToken).ConfigureAwait(false);
        if (document.Representation is FluxMediaImageRepresentation flux) return CreatePlan(flux.Image, scpRevolution);
        var explored = await explorer.ExploreAsync(document, formatId, cancellationToken: cancellationToken).ConfigureAwait(false);
        return CreatePlan(new SectorImageTrackEncoder().Encode(explored.Image, cancellationToken));
    }

    public MediaWritePlan CreatePlan(ProtectedTrackImage image, int revolution = 0)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentOutOfRangeException.ThrowIfNegative(revolution);
        var units = image.Tracks.OrderBy(track => track.Cylinder).ThenBy(track => track.Head).Select((track, index) =>
        {
            if (revolution >= track.Revolutions.Count) throw new InvalidDataException($"Track {track.Cylinder}.{track.Head} does not contain revolution {revolution}.");
            var captured = track.Revolutions[revolution];
            return CreateUnit(track.Cylinder, track.Head, captured.Flux, checked((uint)captured.ResolutionNanoseconds), index);
        }).ToArray();
        return CreatePlan(units);
    }

    public MediaWritePlan CreatePlan(ScpImage image, int revolution)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentOutOfRangeException.ThrowIfNegative(revolution);
        var tickNanoseconds = checked((uint)image.Header.ResolutionNanoseconds);
        var units = image.Tracks.OrderBy(track => track.Cylinder).ThenBy(track => track.Head).Select((track, index) =>
        {
            if (revolution >= track.Revolutions.Count)
                throw new InvalidDataException($"Track {track.Cylinder}.{track.Head} does not contain revolution {revolution}.");
            return CreateUnit(track.Cylinder, track.Head, track.Revolutions[revolution].Flux, tickNanoseconds, index);
        }).ToArray();
        return CreatePlan(units);
    }

    public MediaWritePlan CreatePlan(IReadOnlyList<EncodedDiskTrack> tracks)
    {
        ArgumentNullException.ThrowIfNull(tracks);
        var units = tracks.OrderBy(track => track.Cylinder).ThenBy(track => track.Head).Select((track, index) => CreateUnit(track.Cylinder, track.Head, track.Track.Revolution, EncodedTrackTiming.TickNanoseconds, index)).ToArray();
        return CreatePlan(units);
    }

    private static MediaWritePlan CreatePlan(IReadOnlyList<MediaPhysicalDataUnit> units)
    {
        if (units.Count == 0) throw new InvalidDataException("The floppy image produced no physical track to write.");
        return new MediaWritePlan(MediaKind.Floppy, MediaRepresentationKind.Flux, units, Enumerable.Range(0, units.Count).ToArray(), new Dictionary<string, string>(StringComparer.Ordinal));
    }

    private static MediaPhysicalDataUnit CreateUnit(int cylinder, int head, RetroMediaLab.MediaEngine.Images.Models.Flux.FluxRevolution revolution, uint sourceTickNanoseconds, int position)
    {
        var intervals = revolution.FluxIntervals.Select(interval => checked(interval * sourceTickNanoseconds)).ToArray();
        var track = new MediaFluxTrackData([new MediaFluxRevolutionData(checked(revolution.IndexTimeTicks * sourceTickNanoseconds), intervals)]);
        return new MediaPhysicalDataUnit(
            position,
            MediaFluxTrackDataFunctions.Serialize(track),
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [MediaPhysicalMetadataKeys.Encoding] = MediaPhysicalEncodingIds.FluxTrackV1,
                [MediaPhysicalMetadataKeys.Cylinder] = cylinder.ToString(CultureInfo.InvariantCulture),
                [MediaPhysicalMetadataKeys.Head] = head.ToString(CultureInfo.InvariantCulture),
                [MediaPhysicalMetadataKeys.RevolutionCount] = "1",
                [MediaPhysicalMetadataKeys.TickNanoseconds] = "1"
            });
    }
}
