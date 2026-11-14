using RetroMediaLab.MediaEngine.Contracts;

namespace RetroMediaLab.MediaEngine.Images.Reading.Optical;

/// <summary>Reads stored or user-data sectors through a container-independent optical track descriptor.</summary>
public sealed class OpticalSectorReader
{
    /// <summary>Reads a complete stored sector into caller-owned memory, including an available stored INDEX 00 pregap.</summary>
    public ValueTask ReadStoredSectorAsync(OpticalTrackDescriptor track, long relativeSector, Memory<byte> destination, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(track);
        if (destination.Length != track.StoredSectorSize)
            throw new ArgumentException("The destination must contain exactly one stored sector.", nameof(destination));
        long sourceOffset;
        if (relativeSector < 0)
        {
            if (relativeSector < -track.StoredPregapSectors || track.StoredPregapSourceOffset is not { } pregapOffset)
                throw new ArgumentOutOfRangeException(nameof(relativeSector), "The sector is outside the stored pregap.");
            sourceOffset = checked(pregapOffset + (relativeSector + track.StoredPregapSectors) * track.StoredSectorSize);
        }
        else
        {
            ValidateRelativeSector(track, relativeSector);
            sourceOffset = checked(track.SourceOffset + relativeSector * track.StoredSectorSize);
        }
        return track.DataSource.ReadExactlyAsync(sourceOffset, destination, cancellationToken);
    }

    public async ValueTask<byte[]> ReadStoredSectorAsync(OpticalTrackDescriptor track, long relativeSector, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(track);
        ValidateRelativeSector(track, relativeSector);
        var sector = new byte[track.StoredSectorSize];
        await track.DataSource.ReadExactlyAsync(checked(track.SourceOffset + relativeSector * track.StoredSectorSize), sector, cancellationToken).ConfigureAwait(false);
        return sector;
    }

    public async ValueTask<byte[]> ReadUserDataAsync(OpticalTrackDescriptor track, long relativeSector, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(track);
        ValidateRelativeSector(track, relativeSector);
        var data = new byte[track.UserDataLength];
        await track.DataSource.ReadExactlyAsync(checked(track.SourceOffset + relativeSector * track.StoredSectorSize + track.UserDataOffset), data, cancellationToken).ConfigureAwait(false);
        return data;
    }

    public async ValueTask<byte[]?> ReadSubchannelAsync(OpticalTrackDescriptor track, long relativeSector, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(track);
        ValidateRelativeSector(track, relativeSector);
        if (track.SubchannelSource is null)
            return null;

        var data = new byte[track.SubchannelBytesPerSector];
        await track.SubchannelSource.ReadExactlyAsync(checked(track.SubchannelSourceOffset + relativeSector * track.SubchannelStride), data, cancellationToken).ConfigureAwait(false);
        return data;
    }

    private static void ValidateRelativeSector(OpticalTrackDescriptor track, long relativeSector)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(relativeSector);
        if (relativeSector >= track.SectorCount)
            throw new ArgumentOutOfRangeException(nameof(relativeSector), "The sector is outside the optical track.");
    }
}
