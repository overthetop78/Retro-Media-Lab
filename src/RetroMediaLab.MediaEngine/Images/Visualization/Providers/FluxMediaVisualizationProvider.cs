using System.Collections.Frozen;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Contracts;

using RetroMediaLab.MediaEngine.Interfaces.Visualization;
using RetroMediaLab.MediaEngine.Images.Models.Flux;
using RetroMediaLab.MediaEngine.Images.Formats.HardDisk.MfmEmu;

namespace RetroMediaLab.MediaEngine.Images.Visualization.Providers;

/// <summary>Builds visualization traversal data from preserved flux tracks without decoding sectors.</summary>
public sealed class FluxMediaVisualizationProvider : IMediaVisualizationProvider
{
    private static readonly IReadOnlySet<MediaRepresentationKind> SupportedKinds = new[] { MediaRepresentationKind.Flux }.ToFrozenSet();

    public IReadOnlySet<MediaRepresentationKind> RepresentationKinds => SupportedKinds;

    public bool CanProvide(MediaImageDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return document.Representation is FluxMediaImageRepresentation or MfmEmuImageRepresentation;
    }

    /// <summary>La projection des champs d'un format physique appartient à la couche média.</summary>
    public MediaImageDocument PrepareDocument(MediaImageDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return document.Representation is MfmEmuImageRepresentation mfm
            ? mfm.CreateLogicalView(document) ?? document : document;
    }

    public MediaVisualizationDescriptor CreateDescriptor(MediaImageDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (document.Representation is MfmEmuImageRepresentation mfm)
        {
            var surfaces = Enumerable.Range(0, mfm.Heads).ToArray();
            var tracks = new List<MediaVisualizationElement>();
            for (var cylinder = 0; cylinder < mfm.Cylinders; cylinder++)
                foreach (var head in surfaces) tracks.Add(new(cylinder, head));
            return new(MediaRepresentationKind.Flux, surfaces, MediaVisualizationProgressUnit.Track,
                MediaVisualizationDirection.SourceDefined, tracks);
        }
        if (document.Representation is not FluxMediaImageRepresentation flux)
            throw new NotSupportedException($"Representation '{document.Representation.RepresentationKind}' is not a flux representation.");

        var elements = flux.Tracks.Select(track => new MediaVisualizationElement(track.Cylinder, track.Head)).ToArray();
        return new(MediaRepresentationKind.Flux, flux.Surfaces, MediaVisualizationProgressUnit.Track, MediaVisualizationDirection.SourceDefined, elements);
    }
}
