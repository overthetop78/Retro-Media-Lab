using RetroMediaLab.MediaFileSystems.Interfaces;

namespace RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Adaptec;

/// <summary>Emprunte le lecteur logique pour les graphiques de blocs, sans copier le disque entier.</summary>
internal sealed class AdaptecMfmBlockData(AdaptecMfmSectorReader reader) : IMediaRandomAccessData
{
    public long Length => reader.Capacity;
    public ValueTask ReadExactlyAsync(long offset, Memory<byte> destination, CancellationToken cancellationToken = default)
        => reader.ReadExactlyAsync(offset, destination, cancellationToken);
}
