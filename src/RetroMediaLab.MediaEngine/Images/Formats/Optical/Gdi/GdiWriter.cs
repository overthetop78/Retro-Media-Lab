using RetroMediaLab.MediaEngine.Functions;
using System.Collections.Frozen;
using System.Globalization;
using System.IO;
using System.Text;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Images.Models.Optical;
using RetroMediaLab.MediaEngine.Images.Writing;
using RetroMediaLab.MediaEngine.Interfaces.Writing;

namespace RetroMediaLab.MediaEngine.Images.Formats.Optical.Gdi;

/// <summary>Écrit un descripteur GDI et un fichier de données par piste optique.</summary>
public sealed class GdiWriter : IMediaImageWriter
{
    private static readonly IReadOnlySet<string> SupportedFormatIds = new[] { GdiConstants.FormatId }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    private static readonly IReadOnlySet<MediaRepresentationKind> SupportedRepresentationKinds = new[] { MediaRepresentationKind.OpticalTracks }.ToFrozenSet();

    public string Id => MediaImageWriterIds.OpticalGdi;
    public IReadOnlySet<string> FormatIds => SupportedFormatIds;
    public IReadOnlySet<MediaRepresentationKind> RepresentationKinds => SupportedRepresentationKinds;
    public IReadOnlySet<string> ProducedFileExtensions => GdiConstants.Extensions;
    public bool ProducesMultipleFiles => true;

    public bool CanWrite(MediaImageDocument document, string targetFormatId, string targetExtension)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (!SupportedFormatIds.Contains(targetFormatId)
            || !GdiConstants.Extensions.Contains(MediaFileExtensionFunctions.NormalizeCore(targetExtension))
            || document.MediaKind != MediaKind.Optical
            || document.Representation is not OpticalMediaImageRepresentation { Tracks.Count: > 0 } optical
            || optical.LayerCount is not null
            || optical.FaceCount is not null)
            return false;
        bool isGdChd = IsGdChd(document);
        bool isGdCue = document.FormatId == OpticalImageFormatIds.BinCue &&
            document.Metadata.GetValueOrDefault("mediaProfile") == GdiConstants.MediaProfile;
        if (!isGdChd && !isGdCue && document.FormatId != GdiConstants.FormatId)
            return false;

        var numbers = new HashSet<int>();
        long previousEnd = 0;
        foreach (var track in optical.Tracks!.OrderBy(track => track.TrackNumber))
        {
            int session = track.FirstSector >= GdiConstants.HighDensityStartLba ? 2 : 1;
            if (!numbers.Add(track.TrackNumber) || track.FirstSector < previousEnd ||
                track.SessionNumber != session || track.StoredPregapSectors != 0 ||
                track.PostgapSectors != 0 ||
                (isGdCue && (track.PregapSectors != 0 || track.Flags.Count != 0 || track.CatalogNumber is not null ||
                    track.Isrc is not null || track.Indexes.Any(index => index.Number != 1 || index.RelativeSector != 0))) ||
                track.HasSubchannels || GetTrackSectorSize(track, isGdChd) == 0 ||
                track.SectorCount <= 0 || track.FirstSector > long.MaxValue - track.SectorCount)
                return false;

            previousEnd = track.FirstSector + track.SectorCount;
        }

        return true;
    }

    public async Task<IReadOnlyList<string>> WriteAsync(MediaImageDocument document, string outputPath, string targetFormatId, CancellationToken cancellationToken = default)
    {
        var descriptorPath = Path.GetFullPath(outputPath);
        if (!CanWrite(document, targetFormatId, Path.GetExtension(descriptorPath))
            || document.Representation is not OpticalMediaImageRepresentation optical)
            throw new InvalidDataException("The optical document cannot be written as a GDI track set.");

        var directory = Path.GetDirectoryName(descriptorPath) ?? Directory.GetCurrentDirectory();
        var baseName = Path.GetFileNameWithoutExtension(descriptorPath);
        var files = new Dictionary<string, Func<Stream, CancellationToken, Task>>(StringComparer.OrdinalIgnoreCase);
        var entries = new List<GdiOutputEntry>(optical.Tracks!.Count);
        bool isGdChd = IsGdChd(document);
        bool swapAudio = isGdChd && document.Metadata.GetValueOrDefault("audioByteOrder") != "little";
        foreach (var track in optical.Tracks.OrderBy(track => track.TrackNumber))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var name = $"{baseName}.track{track.TrackNumber:D2}{DiskImageFileExtensions.Bin}";
            var path = Path.Combine(directory, name);
            var sectorSize = GetTrackSectorSize(track, isGdChd);
            var control = track.IsAudio ? GdiConstants.AudioControl : GdiConstants.DataControl;
            files.Add(path, (output, token) => CopyTrackAsync(track, output, sectorSize, swapAudio && track.IsAudio, token));
            entries.Add(new GdiOutputEntry(track.TrackNumber, track.FirstSector, control, sectorSize, name));
        }
        files.Add(descriptorPath, (output, token) => WriteDescriptorAsync(output, entries, token));
        await AtomicMediaFileSetWriter.WriteAsync(files, cancellationToken).ConfigureAwait(false);
        return files.Keys.ToArray();
    }

    private static async Task WriteDescriptorAsync(Stream output, IReadOnlyList<GdiOutputEntry> entries, CancellationToken cancellationToken)
    {
        await using var writer = new StreamWriter(output, new UTF8Encoding(false), 1024, leaveOpen: true);
        await writer.WriteLineAsync(entries.Count.ToString(CultureInfo.InvariantCulture).AsMemory(), cancellationToken).ConfigureAwait(false);
        foreach (var entry in entries)
        {
            var line = string.Join(
                ' ',
                entry.Number.ToString(CultureInfo.InvariantCulture),
                entry.Lba.ToString(CultureInfo.InvariantCulture),
                entry.Control.ToString(CultureInfo.InvariantCulture),
                entry.SectorSize.ToString(CultureInfo.InvariantCulture),
                Quote(entry.FileName),
                "0");
            await writer.WriteLineAsync(line.AsMemory(), cancellationToken).ConfigureAwait(false);
        }
        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task CopyTrackAsync(OpticalTrackDescriptor track, Stream output, int sectorSize, bool swapAudio, CancellationToken cancellationToken)
    {
        if (track.StoredSectorSize != sectorSize || swapAudio)
        {
            var sector = new byte[sectorSize];
            for (long index = 0; index < track.SectorCount; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                long offset = checked(track.SourceOffset + index * track.StoredSectorSize);
                await track.DataSource.ReadExactlyAsync(offset, sector, cancellationToken).ConfigureAwait(false);
                if (swapAudio)
                    for (int sample = 0; sample < sector.Length; sample += 2)
                        (sector[sample], sector[sample + 1]) = (sector[sample + 1], sector[sample]);
                await output.WriteAsync(sector, cancellationToken).ConfigureAwait(false);
            }
            return;
        }

        var buffer = new byte[64 * DataSizeConstants.BytesPerKibibyte];
        var total = checked(track.SectorCount * sectorSize);
        long copied = 0;
        while (copied < total)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var count = (int)Math.Min(buffer.Length, total - copied);
            await track.DataSource.ReadExactlyAsync(checked(track.SourceOffset + copied), buffer.AsMemory(0, count), cancellationToken).ConfigureAwait(false);
            await output.WriteAsync(buffer.AsMemory(0, count), cancellationToken).ConfigureAwait(false);
            copied += count;
        }
    }

    private static int GetTrackSectorSize(OpticalTrackDescriptor track, bool isGdChd)
    {
        int size = track.Mode switch
        {
            OpticalTrackMode.Audio or OpticalTrackMode.Mode1Raw2352 => GdiConstants.RawSectorSize,
            OpticalTrackMode.Mode1Data2048 => GdiConstants.DataSectorSize,
            _ => 0
        };
        return track.StoredSectorSize == size || (isGdChd && track.StoredSectorSize == ChdConstants.CdFrameSize) ? size : 0;
    }

    private static bool IsGdChd(MediaImageDocument document) =>
        document.FormatId == OpticalImageFormatIds.Chd && document.Metadata.GetValueOrDefault("mediaProfile") == GdiConstants.MediaProfile;

    private static string Quote(string value) => $"\"{value.Replace("\"", "\\\"")}\"";

    private sealed record GdiOutputEntry(int Number, long Lba, int Control, int SectorSize, string FileName);
}
