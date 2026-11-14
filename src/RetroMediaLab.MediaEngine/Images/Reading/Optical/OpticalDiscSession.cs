using System.IO;
using System.Runtime.ExceptionServices;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Images.Models.Optical;

namespace RetroMediaLab.MediaEngine.Images.Reading.Optical;

/// <summary>Owns a recognized optical image and exposes bounded sector reads without filesystem interfaces.</summary>
public sealed class OpticalDiscSession : IDisposable
{
    private readonly MediaImageDocument _document;
    private readonly OpticalTrackDescriptor[] _tracks;
    private readonly OpticalSectorReader _sectors = new();
    private bool _disposed;
    private byte[][]? _cachedSectors;
    private byte[]?[]? _cachedSubchannels;
    public IReadOnlyList<OpticalDiscTrack> Tracks { get; }
    public string FormatId => _document.FormatId;
    public bool AudioBigEndian => FormatId == OpticalImageFormatIds.Chd;

    private OpticalDiscSession(MediaImageDocument document)
    {
        _document = document;
        if (document.Representation is not OpticalMediaImageRepresentation { Tracks: { Count: > 0 } tracks })
            throw new InvalidDataException("The media does not contain readable optical tracks.");
        _tracks = tracks.OrderBy(track => track.TrackNumber).ToArray();
        Tracks = Array.AsReadOnly(_tracks.Select((track,index) => CreateTrack(track,index==0 ? document.Metadata : null)).ToArray());
    }
    private static OpticalDiscTrack CreateTrack(OpticalTrackDescriptor track,IReadOnlyDictionary<string,string>? fallback)
    {
        IReadOnlyDictionary<string,string> metadata = track.Metadata.ContainsKey("REM:LOOP") || track.Metadata.ContainsKey("REM:NOLOOP") ? track.Metadata : fallback ?? track.Metadata;
        int loopMode = metadata.ContainsKey("REM:NOLOOP") ? -1 : metadata.ContainsKey("REM:LOOP") ? 1 : 0;
        int.TryParse(metadata.GetValueOrDefault("REM:LOOP"),System.Globalization.NumberStyles.Integer,System.Globalization.CultureInfo.InvariantCulture,out int loopOffset);
        return new OpticalDiscTrack(track.TrackNumber,track.Mode,track.FirstSector,
            track.SectorCount,track.StoredSectorSize,track.UserDataOffset,track.UserDataLength,track.PregapSectors,
            track.PostgapSectors,track.StoredPregapSectors,track.HasSubchannels,loopMode,loopOffset);
    }
    public static async Task<OpticalDiscSession> OpenAsync(string path, CancellationToken cancellationToken = default, bool includeCompanionAudio = false)
    {
        MediaImageDocument document = await MediaRecognitionComposition.CreateDefault().ReadingService
            .ReadAsync(new MediaSourceDescriptor(path,[]) { TrackDriveAccesses = false },cancellationToken).ConfigureAwait(false);
        bool transferred = false;
        try
        {
            if (includeCompanionAudio) document=await OpticalCompanionAudio.AttachAsync(document,cancellationToken).ConfigureAwait(false);
            OpticalDiscSession session = new(document); transferred = true; return session;
        }
        finally { if (!transferred) ReleaseDocument(document); }
    }
    public ValueTask ReadStoredSectorAsync(int trackIndex, long relativeSector, Memory<byte> destination, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed,this);
        cancellationToken.ThrowIfCancellationRequested();
        if (_cachedSectors is { } cached)
        {
            OpticalTrackDescriptor track = _tracks[trackIndex];
            long sector = relativeSector + track.StoredPregapSectors;
            if (sector < 0 || sector >= track.SectorCount + track.StoredPregapSectors || destination.Length != track.StoredSectorSize)
                throw new ArgumentOutOfRangeException(nameof(relativeSector));
            cached[trackIndex].AsMemory(checked((int)(sector * track.StoredSectorSize)),track.StoredSectorSize).CopyTo(destination);
            return ValueTask.CompletedTask;
        }
        return _sectors.ReadStoredSectorAsync(_tracks[trackIndex],relativeSector,destination,cancellationToken);
    }
    public ValueTask<byte[]?> ReadSubchannelAsync(int trackIndex, long relativeSector, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed,this);
        cancellationToken.ThrowIfCancellationRequested();
        if (_cachedSubchannels is { } cached)
        {
            byte[]? data = cached[trackIndex];
            if (data is null) return ValueTask.FromResult<byte[]?>(null);
            if (relativeSector < 0 || relativeSector >= _tracks[trackIndex].SectorCount)
                throw new ArgumentOutOfRangeException(nameof(relativeSector));
            return ValueTask.FromResult<byte[]?>(data.AsSpan(checked((int)(relativeSector * 96)),96).ToArray());
        }
        return _sectors.ReadSubchannelAsync(_tracks[trackIndex],relativeSector,cancellationToken);
    }
    /// <summary>Caches decoded sectors once; cancellation leaves the existing streaming session usable.</summary>
    public async Task PrecacheAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed,this);
        if (_cachedSectors is not null) return;
        byte[][] sectors = new byte[_tracks.Length][];
        byte[]?[] subchannels = new byte[_tracks.Length][];
        for (int index = 0; index < _tracks.Length; index++)
        {
            OpticalTrackDescriptor track = _tracks[index];
            long storedCount = track.SectorCount + track.StoredPregapSectors;
            sectors[index] = new byte[checked((int)(storedCount * track.StoredSectorSize))];
            if (track.HasSubchannels) subchannels[index] = new byte[checked((int)(track.SectorCount * 96))];
            for (long sector = -track.StoredPregapSectors; sector < track.SectorCount; sector++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Memory<byte> target = sectors[index].AsMemory(checked((int)((sector + track.StoredPregapSectors) * track.StoredSectorSize)),track.StoredSectorSize);
                await _sectors.ReadStoredSectorAsync(track,sector,target,cancellationToken).ConfigureAwait(false);
                if (sector >= 0 && subchannels[index] is { } sub)
                {
                    byte[]? data = await _sectors.ReadSubchannelAsync(track,sector,cancellationToken).ConfigureAwait(false);
                    if (data is not null) data.CopyTo(sub,checked((int)(sector * 96)));
                }
            }
        }
        _cachedSubchannels = subchannels;
        _cachedSectors = sectors;
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { ReleaseDocument(_document); }
        finally { _cachedSectors = null; _cachedSubchannels = null; }
    }
    private static void ReleaseDocument(MediaImageDocument document)
    {
        Exception? failure = null;
        try
        {
            if (document.Representation is OpticalMediaImageRepresentation { Tracks: { } tracks })
            {
                HashSet<object> released = new(ReferenceEqualityComparer.Instance);
                foreach (OpticalTrackDescriptor track in tracks)
                {
                    foreach (object? resource in new object?[] {track.DataSource,track.SubchannelSource})
                    {
                        if (resource is not IDisposable disposable || !released.Add(resource)) continue;
                        try { disposable.Dispose(); } catch (Exception error) { failure ??= error; }
                    }
                }
            }
        }
        finally { document.Source.Drive?.Reset(); }
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
