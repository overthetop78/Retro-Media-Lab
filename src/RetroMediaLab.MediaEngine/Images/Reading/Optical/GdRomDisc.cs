using System.Buffers;
using System.IO;
using System.Text;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Images.Formats.Optical.Gdi;
using RetroMediaLab.MediaFileSystems.FileSystems.Iso9660;
using FileSystemEntryKind = RetroMediaLab.MediaFileSystems.FileSystemEntryKind;
using VolumeDescriptor = RetroMediaLab.MediaFileSystems.Contracts.MediaVolumeDescriptor;

namespace RetroMediaLab.MediaEngine.Images.Reading.Optical;

/// <summary>Reads a GD-ROM image using the drive's frame addresses.</summary>
public sealed class GdRomDisc : IDisposable
{
    public const int FirstFrameAddress = 150;
    public const int DataSectorSize = 2048;
    public const int AudioSectorSize = 2352;
    private const int BootstrapSectorCount = 16;
    private const int BootFileNameOffset = 0x60;
    private const int BootFileNameLength = 16;

    private readonly OpticalDiscSession _session;

    private GdRomDisc(OpticalDiscSession session) => _session = session;

    public IReadOnlyList<OpticalDiscTrack> Tracks => _session.Tracks;

    public static async Task<GdRomDisc> OpenAsync(string path, CancellationToken cancellationToken = default)
    {
        var session = await OpticalDiscSession.OpenAsync(path, cancellationToken).ConfigureAwait(false);
        bool transferred = false;
        try
        {
            if (!session.Tracks.Any(track => track.SessionNumber == 1) ||
                !session.Tracks.Any(track => track is { TrackNumber: 3, SessionNumber: 2, FirstSector: GdiConstants.HighDensityStartLba, IsAudio: false }))
                throw new InvalidDataException("The image does not contain both GD-ROM density areas.");

            var disc = new GdRomDisc(session);
            transferred = true;
            return disc;
        }
        finally
        {
            if (!transferred)
                session.Dispose();
        }
    }

    public ValueTask ReadDataSectorAsync(int frameAddress, Memory<byte> destination, CancellationToken cancellationToken = default)
    {
        if (destination.Length != DataSectorSize)
            throw new ArgumentException("The destination must contain exactly one GD-ROM data sector.", nameof(destination));

        long lba = (long)frameAddress - FirstFrameAddress;
        for (int index = 0; index < Tracks.Count; index++)
        {
            OpticalDiscTrack track = Tracks[index];
            long relative = lba - track.FirstSector;
            if (relative >= 0 && relative < track.SectorCount && !track.IsAudio && track.UserDataLength == DataSectorSize)
                return _session.ReadUserSectorAsync(index, relative, destination, cancellationToken);
        }

        throw new ArgumentOutOfRangeException(nameof(frameAddress), "The frame address is outside GD-ROM data tracks.");
    }

    public ValueTask ReadAudioSectorAsync(int frameAddress, Memory<byte> destination, CancellationToken cancellationToken = default)
    {
        if (destination.Length != AudioSectorSize)
            throw new ArgumentException("The destination must contain exactly one GD-ROM audio sector.", nameof(destination));

        long lba = (long)frameAddress - FirstFrameAddress;
        for (int index = 0; index < Tracks.Count; index++)
        {
            OpticalDiscTrack track = Tracks[index];
            long relative = lba - track.FirstSector;
            if (track.IsAudio && relative >= 0 && relative < track.SectorCount)
                return _session.ReadUserSectorAsync(index, relative, destination, cancellationToken);
        }

        throw new ArgumentOutOfRangeException(nameof(frameAddress), "The frame address is outside GD-ROM audio tracks.");
    }

    /// <summary>Reads the stored sector, excluding any appended subchannel bytes.</summary>
    public ValueTask ReadRawSectorAsync(int frameAddress, Memory<byte> destination, CancellationToken cancellationToken = default)
    {
        if (destination.Length != AudioSectorSize)
            throw new ArgumentException("The destination must contain exactly one raw GD-ROM sector.", nameof(destination));

        long lba = (long)frameAddress - FirstFrameAddress;
        for (int index = 0; index < Tracks.Count; index++)
        {
            OpticalDiscTrack track = Tracks[index];
            long relative = lba - track.FirstSector;
            if (relative < 0 || relative >= track.SectorCount)
                continue;

            if (track.StoredSectorSize == AudioSectorSize)
                return _session.ReadStoredSectorAsync(index, relative, destination, cancellationToken);
            if (track.StoredSectorSize == AudioSectorSize + 96)
                return ReadRawWithSubchannelsAsync(index, relative, destination, cancellationToken);
            throw new InvalidDataException("The image does not store this GD-ROM sector in raw form.");
        }

        throw new ArgumentOutOfRangeException(nameof(frameAddress), "The frame address is outside GD-ROM tracks.");
    }

    private async ValueTask ReadRawWithSubchannelsAsync(
        int trackIndex, long relative, Memory<byte> destination, CancellationToken cancellationToken)
    {
        using var sector = MemoryPool<byte>.Shared.Rent(AudioSectorSize + 96);
        await _session.ReadStoredSectorAsync(trackIndex, relative, sector.Memory[..(AudioSectorSize + 96)],
            cancellationToken).ConfigureAwait(false);
        sector.Memory[..AudioSectorSize].CopyTo(destination);
    }

    /// <summary>Reads the bootstrap and its executable from the high-density area.</summary>
    public async Task<(byte[] Bootstrap, string ExecutableName, byte[] Executable)> ReadBootImageAsync(
        int maximumExecutableBytes, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumExecutableBytes);
        MediaImageDocument document = _session.Document;
        var track = Tracks.Single(track => track is { SessionNumber: 2, TrackNumber: 3 });
        if (track.SectorCount <= BootstrapSectorCount || track.UserDataLength != DataSectorSize)
            throw new InvalidDataException("The GD-ROM boot track does not contain a complete bootstrap and volume descriptor.");

        var bootstrap = new byte[BootstrapSectorCount * DataSectorSize];
        int firstFrame = checked((int)(track.FirstSector + FirstFrameAddress));
        for (int sector = 0; sector < BootstrapSectorCount; sector++)
            await ReadDataSectorAsync(firstFrame + sector, bootstrap.AsMemory(sector * DataSectorSize, DataSectorSize),
                cancellationToken).ConfigureAwait(false);

        if (!bootstrap.AsSpan().StartsWith("SEGA SEGAKATANA "u8))
            throw new InvalidDataException("The high-density boot sector lacks the Dreamcast identifier.");
        string name = Encoding.ASCII.GetString(bootstrap, BootFileNameOffset, BootFileNameLength).TrimEnd(' ', '\0');
        var volume = new VolumeDescriptor(checked(track.FirstSector * DataSectorSize), checked(track.SectorCount * DataSectorSize),
            RetroMediaLab.MediaFileSystems.Constants.MediaVolumeOrigins.OpticalTrack,
            sessionNumber: track.SessionNumber, trackNumber: track.TrackNumber);
        var (catalog, extents) = new Iso9660FileSystemReader().ReadCatalog(document, volume, cancellationToken);
        var file = catalog.Entries.FirstOrDefault(entry => entry.Kind == FileSystemEntryKind.File
            && string.Equals(entry.Name, name, StringComparison.OrdinalIgnoreCase));
        if (file is null || file.Size <= 0 || file.Size > maximumExecutableBytes || file.StorageReference < 0)
            throw new InvalidDataException("The GD-ROM boot executable is missing, empty, or too large.");

        var executable = new byte[(int)file.Size];
        long block = file.StorageReference;
        for (int offset = 0; offset < executable.Length;)
        {
            byte[] data = await extents.ReadUserDataAsync(block++, cancellationToken).ConfigureAwait(false);
            if (data.Length != DataSectorSize)
                throw new InvalidDataException("The GD-ROM boot executable contains an incomplete sector.");
            int count = Math.Min(data.Length, executable.Length - offset);
            data.AsSpan(0, count).CopyTo(executable.AsSpan(offset));
            offset += count;
        }
        return (bootstrap, name, executable);
    }

    public void Dispose() => _session.Dispose();
}
