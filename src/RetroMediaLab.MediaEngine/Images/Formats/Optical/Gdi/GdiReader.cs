using System.Collections.Frozen;
using System.Globalization;
using System.IO;
using MediaSourceDescriptor = RetroMediaLab.MediaEngine.Contracts.MediaSourceDescriptor;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Images.Models.Optical;
using RetroMediaLab.MediaEngine.Images.Reading.Recognition;
using RetroMediaLab.MediaEngine.Images.Reading.Sources;
using RetroMediaLab.MediaEngine.Interfaces.Reading;

namespace RetroMediaLab.MediaEngine.Images.Formats.Optical.Gdi;

/// <summary>Lit un descripteur Dreamcast GDI et toutes ses pistes associées.</summary>
public sealed class GdiReader : IMediaImageReader
{
    private static readonly IReadOnlySet<string> SupportedFormatIds = new[] { GdiConstants.FormatId }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    private static readonly IReadOnlySet<MediaKind> SupportedMediaKinds = new[] { MediaKind.Optical }.ToFrozenSet();
    private static readonly IReadOnlySet<MediaRepresentationKind> SupportedRepresentationKinds = new[] { MediaRepresentationKind.OpticalTracks }.ToFrozenSet();
    private readonly GdiDescriptorReader descriptors;

    public GdiReader() : this(new GdiDescriptorReader())
    {
    }

    internal GdiReader(GdiDescriptorReader descriptors) => this.descriptors = descriptors ?? throw new ArgumentNullException(nameof(descriptors));

    IReadOnlySet<string> IMediaImageReader.FormatIds => SupportedFormatIds;
    IReadOnlySet<string> IMediaImageReader.Extensions => GdiConstants.Extensions;
    IReadOnlyList<ReadOnlyMemory<byte>> IMediaImageReader.Signatures => [];
    IReadOnlySet<string> IMediaImageReader.AssociatedFileExtensions => new[] { DiskImageFileExtensions.Bin, DiskImageFileExtensions.Raw }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    IReadOnlySet<MediaKind> IMediaImageReader.MediaKinds => SupportedMediaKinds;
    IReadOnlySet<MediaRepresentationKind> IMediaImageReader.RepresentationKinds => SupportedRepresentationKinds;
    bool IMediaImageReader.SupportsFormatId(string formatId) => SupportedFormatIds.Contains(formatId);

    async ValueTask<bool> IMediaImageReader.CanReadAsync(MediaRecognitionContext context, CancellationToken cancellationToken)
    {
        if (context.RequestedFormatId is not null && !SupportedFormatIds.Contains(context.RequestedFormatId))
            return false;
        if (!GdiConstants.Extensions.Contains(context.Extension)) return false;
        try
        {
            var descriptor = await descriptors.ReadAsync(context.Source.PrimaryPath, cancellationToken).ConfigureAwait(false);
            ValidateAssociatedFiles(context.Source.PrimaryPath, descriptor);
            return true;
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or NotSupportedException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    async Task<MediaImageDocument> IMediaImageReader.ReadAsync(MediaRecognitionContext context, CancellationToken cancellationToken)
    {
        var descriptor = await descriptors.ReadAsync(context.Source.PrimaryPath, cancellationToken).ConfigureAwait(false);
        var directory = Path.GetDirectoryName(Path.GetFullPath(context.Source.PrimaryPath)) ?? Directory.GetCurrentDirectory();
        ValidateAssociatedFiles(context.Source.PrimaryPath, descriptor);
        var paths = descriptor.Tracks.Select(track => ResolveDataPath(directory, track.FileName)).ToArray();
        var sources = paths.Distinct(StringComparer.OrdinalIgnoreCase)
            .ToDictionary(path => path, path => new FileRandomAccessData(path), StringComparer.OrdinalIgnoreCase);

        var tracks = new List<OpticalTrackDescriptor>(descriptor.Tracks.Count);
        for (var index = 0; index < descriptor.Tracks.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var declaration = descriptor.Tracks[index];
            var source = sources[paths[index]];
            var endOffset = source.Length;
            for (int next = index + 1; next < paths.Length; next++)
            {
                if (!StringComparer.OrdinalIgnoreCase.Equals(paths[index], paths[next]))
                    continue;
                endOffset = descriptor.Tracks[next].FileOffset;
                break;
            }

            long byteCount = endOffset - declaration.FileOffset;
            if (endOffset > source.Length || byteCount <= 0 || byteCount % declaration.SectorSize != 0)
                throw new InvalidDataException($"GDI track {declaration.Number} is not aligned to its declared sector size.");
            var available = byteCount / declaration.SectorSize;
            if (available <= 0) throw new InvalidDataException($"GDI track {declaration.Number} has no sectors.");
            var sectorCount = available;
            if (index + 1 < descriptor.Tracks.Count)
            {
                var nextLba = descriptor.Tracks[index + 1].Lba;
                var declaredCount = nextLba - declaration.Lba;
                sectorCount = Math.Min(available, declaredCount);
            }
            var mode = ResolveMode(declaration);
            var (userOffset, userLength) = ResolveUserDataWindow(mode, declaration.SectorSize);
            int session = declaration.Lba >= GdiConstants.HighDensityStartLba ? 2 : 1;
            tracks.Add(new OpticalTrackDescriptor(session, declaration.Number, mode, declaration.Lba, sectorCount, declaration.SectorSize, userOffset, userLength, source, declaration.FileOffset, [new OpticalTrackIndex(1, 0)]));
        }

        var associatedPaths = sources.Keys.ToArray();
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [GdiConstants.TrackCountMetadata] = tracks.Count.ToString(CultureInfo.InvariantCulture),
            ["mediaProfile"] = GdiConstants.MediaProfile
        };
        foreach (var declaration in descriptor.Tracks)
        {
            var prefix = $"{GdiConstants.TrackPrefixMetadata}{declaration.Number}";
            metadata[prefix + GdiConstants.FileMetadataSuffix] = declaration.FileName;
            metadata[prefix + GdiConstants.LbaMetadataSuffix] = declaration.Lba.ToString(CultureInfo.InvariantCulture);
            metadata[prefix + GdiConstants.ControlMetadataSuffix] = declaration.Control.ToString(CultureInfo.InvariantCulture);
            metadata[prefix + GdiConstants.SectorSizeMetadataSuffix] = declaration.SectorSize.ToString(CultureInfo.InvariantCulture);
        }
        var sourceDescriptor = new MediaSourceDescriptor(context.Source.PrimaryPath, associatedPaths, context.Source.KnownLength, context.Source.RequestedFormatId);
        var representation = new OpticalMediaImageRepresentation(tracks.Sum(track => checked(track.SectorCount * track.UserDataLength)), tracks, associatedFiles: [context.Source.PrimaryPath, .. associatedPaths]);
        return new MediaImageDocument(sourceDescriptor, GdiConstants.FormatId, MediaKind.Optical, representation, [], [], metadata);
    }

    private static void ValidateAssociatedFiles(string descriptorPath, GdiDescriptor descriptor)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(descriptorPath)) ?? Directory.GetCurrentDirectory();
        foreach (var track in descriptor.Tracks)
        {
            var path = ResolveDataPath(directory, track.FileName);
            if (!File.Exists(path)) throw new InvalidDataException($"GDI track file '{track.FileName}' is missing.");
        }
    }

    private static string ResolveDataPath(string directory, string fileName)
    {
        var root = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var path = Path.GetFullPath(Path.Combine(directory, fileName));
        if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("A GDI track file must remain inside the descriptor directory.");
        return path;
    }

    private static OpticalTrackMode ResolveMode(GdiTrackDeclaration declaration) => declaration.Control == GdiConstants.AudioControl ? OpticalTrackMode.Audio : declaration.SectorSize == GdiConstants.DataSectorSize ? OpticalTrackMode.Mode1Data2048 : OpticalTrackMode.Mode1Raw2352;

    private static (int Offset, int Length) ResolveUserDataWindow(OpticalTrackMode mode, int sectorSize) => mode switch
    {
        OpticalTrackMode.Audio => (0, sectorSize),
        OpticalTrackMode.Mode1Data2048 => (0, GdiConstants.DataSectorSize),
        OpticalTrackMode.Mode1Raw2352 => (OpticalSectorConstants.Mode1RawUserDataOffset, OpticalSectorConstants.Mode1RawUserDataLength),
        _ => throw new NotSupportedException("The GDI track mode is not supported.")
    };
}
