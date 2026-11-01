using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Interfaces;
using IMediaSectorImage = RetroMediaLab.MediaFileSystems.Interfaces.IMediaSectorImage;
using IMediaSectorRepresentation = RetroMediaLab.MediaFileSystems.Interfaces.IMediaSectorRepresentation;

namespace RetroMediaLab.MediaEngine.Images.Models.Sectors;

/// <summary>Exposes a sector image directly without synthesizing a flux representation.</summary>
public sealed class SectorMediaImageRepresentation : IMediaSectorRepresentation, IMediaImageRepresentation
{
    public SectorMediaImageRepresentation(SectorImage image)
    {
        ArgumentNullException.ThrowIfNull(image);
        Image = image;
    }

    public MediaRepresentationKind RepresentationKind => MediaRepresentationKind.Sectors;

    public long? LogicalLength => Image.Capacity;

    public bool SupportsRandomAccess => true;

    public bool SupportsSequentialAccess => true;

    public SectorImage Image { get; }

    IMediaSectorImage IMediaSectorRepresentation.Image => Image;
}
