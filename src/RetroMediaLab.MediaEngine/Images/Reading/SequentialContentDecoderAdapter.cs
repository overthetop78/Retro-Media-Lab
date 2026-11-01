using System.IO;
using MediaVolumeOrigins = RetroMediaLab.MediaFileSystems.Constants.MediaVolumeOrigins;
using MediaVolumeDescriptor = RetroMediaLab.MediaFileSystems.Contracts.MediaVolumeDescriptor;
using IMediaImageDocument = RetroMediaLab.MediaFileSystems.Interfaces.IMediaImageDocument;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Images.Reading.Decoding.Sequential;
using RetroMediaLab.MediaFileSystems.Definitions;
using RetroMediaLab.MediaFileSystems.Interfaces.Exploration;
using RetroMediaLab.MediaFileSystems;
using RetroMediaLab.MediaEngine.Images.Models.Sequential;
using RetroMediaLab.MediaFileSystems.Exploration.Sequential;

namespace RetroMediaLab.MediaEngine.Images.Reading;

/// <summary>Décode la bande dans le moteur puis confie les vrais fichiers au lecteur de systèmes de fichiers.</summary>
public sealed class SequentialContentDecoderAdapter : IMediaFileSystemReader
{
    private readonly SequentialDecoderRegistry decoders;
    private readonly SequentialContentFileSystemReader fileSystems = new();

    public SequentialContentDecoderAdapter(SequentialDecoderRegistry decoders)
    {
        ArgumentNullException.ThrowIfNull(decoders);
        this.decoders = decoders;
    }

    public string Id => FileSystemIds.SequentialContent;
    public bool CanRead(IMediaImageDocument document, MediaVolumeDescriptor volume)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(volume);
        return document is MediaImageDocument { MediaKind: MediaKind.Tape }
            && document.Representation is SequentialMediaImageRepresentation
            && (volume.FileSystemId?.Equals(Id, StringComparison.OrdinalIgnoreCase) == true
                || volume.Origin.Equals(MediaVolumeOrigins.SequentialContent, StringComparison.OrdinalIgnoreCase));
    }

    public FileSystemVolume Read(IMediaImageDocument document, MediaVolumeDescriptor volume)
    {
        if (!CanRead(document, volume) || document is not MediaImageDocument decoded)
            throw new InvalidDataException("The requested volume is not decoded sequential media content.");

        var compatible = decoders.FindCompatible(decoded);
        var results = compatible.Select(decoder => decoder.DecodeAsync(decoded).GetAwaiter().GetResult()).OrderByDescending(result => result.Blocks.Count > 0).ThenByDescending(result => result.Confidence)
            .ThenByDescending(result => result.Blocks.Count)
            .ToArray();
        var selected = results.FirstOrDefault();
        var diagnostics = new List<string>(selected?.Diagnostics ?? []);
        if (results.Length > 1 && selected is not null)
            diagnostics.Add($"Selected decoder '{selected.DecoderId}' from {results.Length} compatible sequential decoders.");
        foreach (var segment in selected?.UndecodedSegments ?? [])
            diagnostics.Add($"Undecoded {segment.Kind} segment at position {segment.Position}.");

        var blocks = selected?.Blocks.Select(block => new MediaSequentialDecodedBlock(block.Position, block.Data, block.IntegrityValid, block.Metadata)).ToArray() ?? [];
        var content = new DecodedContent(selected?.DecoderId, blocks, diagnostics);
        var volumeName = document.Metadata.TryGetValue(AtariCasConstants.InternalNameMetadataKey, out var internalName) && !string.IsNullOrWhiteSpace(internalName) ? internalName.Trim() : string.Empty;
        return fileSystems.Read(document, volume, content, volumeName);
    }

    private sealed record DecodedContent(string? DecoderId, IReadOnlyList<MediaSequentialDecodedBlock> Blocks, IReadOnlyList<string> Diagnostics) : IMediaSequentialContent;
}

