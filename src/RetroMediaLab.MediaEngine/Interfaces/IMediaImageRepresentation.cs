using RetroMediaLab.MediaEngine.Enums;

namespace RetroMediaLab.MediaEngine.Interfaces;

/// <summary>Expose à l'application la représentation du média décodé par le moteur.</summary>
public interface IMediaImageRepresentation : RetroMediaLab.MediaFileSystems.Interfaces.IMediaImageRepresentation
{
    MediaRepresentationKind RepresentationKind { get; }

    new long? LogicalLength { get; }

    new bool SupportsRandomAccess { get; }

    new bool SupportsSequentialAccess { get; }
}
