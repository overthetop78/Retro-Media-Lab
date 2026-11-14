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
    public IReadOnlyList<OpticalDiscTrack> Tracks { get; }
    public string FormatId => _document.FormatId;
    public bool AudioBigEndian => FormatId == OpticalImageFormatIds.Chd;

    private OpticalDiscSession(MediaImageDocument document)
    {
        _document = document;
        if (document.Representation is not OpticalMediaImageRepresentation { Tracks: { Count: > 0 } tracks })
            throw new InvalidDataException("The media does not contain readable optical tracks.");
        _tracks = tracks.OrderBy(track => track.TrackNumber).ToArray();
        Tracks = Array.AsReadOnly(_tracks.Select(track => new OpticalDiscTrack(track.TrackNumber,track.Mode,track.FirstSector,
            track.SectorCount,track.StoredSectorSize,track.UserDataOffset,track.UserDataLength,track.PregapSectors,
            track.PostgapSectors,track.StoredPregapSectors,track.HasSubchannels)).ToArray());
    }
    public static async Task<OpticalDiscSession> OpenAsync(string path, CancellationToken cancellationToken = default)
    {
        MediaImageDocument document = await MediaRecognitionComposition.CreateDefault().ReadingService
            .ReadAsync(new MediaSourceDescriptor(path,[]),cancellationToken).ConfigureAwait(false);
        bool transferred = false;
        try { OpticalDiscSession session = new(document); transferred = true; return session; }
        finally { if (!transferred) ReleaseDocument(document); }
    }
    public ValueTask ReadStoredSectorAsync(int trackIndex, long relativeSector, Memory<byte> destination, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed,this);
        return _sectors.ReadStoredSectorAsync(_tracks[trackIndex],relativeSector,destination,cancellationToken);
    }
    public ValueTask<byte[]?> ReadSubchannelAsync(int trackIndex, long relativeSector, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed,this);
        return _sectors.ReadSubchannelAsync(_tracks[trackIndex],relativeSector,cancellationToken);
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true; ReleaseDocument(_document);
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
