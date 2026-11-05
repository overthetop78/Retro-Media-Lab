using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Images.Visualization;

namespace RetroMediaLab.MediaEngine.Interfaces.Visualization;

/// <summary>Describes the visualization capabilities of one or more media representations.</summary>
public interface IMediaVisualizationProvider
{
    IReadOnlySet<MediaRepresentationKind> RepresentationKinds { get; }

    bool CanProvide(MediaImageDocument document);

    /// <summary>Prépare une représentation de présentation sans modifier le document source.</summary>
    MediaImageDocument PrepareDocument(MediaImageDocument document) => document;

    MediaVisualizationDescriptor CreateDescriptor(MediaImageDocument document);
}
