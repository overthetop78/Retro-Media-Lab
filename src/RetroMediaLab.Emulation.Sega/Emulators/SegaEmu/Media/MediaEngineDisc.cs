using System.Buffers.Binary;
using System.IO;
using RetroMediaLab.Chips.Controllers.Sega.CD;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Images.Reading.Optical;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Media;

/// <summary>Adapts already decoded MediaEngine optical tracks to the emulated drive.</summary>
internal sealed class MediaEngineDisc : ICdDriveMedia, IDisposable
{
    private readonly OpticalDiscSession _image;
    private readonly OpticalDiscTrack[] _tracks;
    private readonly CdTrack[] _driveTracks;
    private readonly CancellationToken _cancellation;
    private readonly bool _bigEndianAudio;
    private readonly byte[] _dataSector, _audioSector;
    private int _dataTrack = -1, _audioTrack = -1;
    private long _dataIndex = long.MinValue, _audioIndex = long.MinValue;
    private bool _disposed;
    public IReadOnlyList<CdTrack> Tracks => _driveTracks;
    public int LeadOutLba { get; }
    public bool HasSubchannel => _tracks.Any(track => track.HasSubchannels);
    public string FormatId => _image.FormatId;

    private MediaEngineDisc(OpticalDiscSession image, CancellationToken cancellation)
    {
        _image = image; _cancellation = cancellation;
        _tracks = image.Tracks.ToArray();
        if (_tracks.Length > 99 || _tracks[0].IsAudio) throw new InvalidDataException("A Mega-CD disc needs a first data track and at most 99 tracks.");
        _driveTracks = new CdTrack[_tracks.Length];
        _bigEndianAudio = image.AudioBigEndian;
        long gapOffset = 0;
        for (int i = 0; i < _tracks.Length; i++)
        {
            OpticalDiscTrack track = _tracks[i];
            // The BIN/CUE reader numbers content sectors consecutively; the drive also needs INDEX 00 and virtual gaps.
            if (image.FormatId == OpticalImageFormatIds.BinCue)
            {
                gapOffset = checked(gapOffset + track.PregapSectors + track.StoredPregapSectors);
                if (i > 0) gapOffset = checked(gapOffset + _tracks[i - 1].PostgapSectors);
            }
            int start = checked((int)(track.FirstSector + gapOffset));
            int end = checked(start + (int)track.SectorCount);
            int type = track.IsAudio ? 0 : track.Mode is OpticalTrackMode.Mode2Raw2352 or OpticalTrackMode.Mode2Data2336 or
                OpticalTrackMode.Mode2Form1Data2048 or OpticalTrackMode.Mode2Form2Data2324 or OpticalTrackMode.Mode2FormMixData2336 ? 2 : 1;
            _driveTracks[i] = new(start,end,type);
        }
        LeadOutLba = checked(_driveTracks[^1].EndLba + (int)_tracks[^1].PostgapSectors);
        int maximumSector = _tracks.Max(track => track.StoredSectorSize);
        _dataSector = new byte[maximumSector]; _audioSector = new byte[maximumSector];
    }

    public static async Task<MediaEngineDisc> OpenAsync(string path, CancellationToken cancellation)
    {
        OpticalDiscSession image = await OpticalDiscSession.OpenAsync(path,cancellation).ConfigureAwait(false);
        bool transferred = false;
        try
        {
            MediaEngineDisc disc = new(image, cancellation); transferred = true; return disc;
        }
        finally { if (!transferred) image.Dispose(); }
    }
    public void ReadHeader(Span<byte> destination)
    {
        CheckAlive();
        OpticalDiscTrack track = _tracks[0];
        ReadSector(0, 0, _dataSector, audio: false);
        if (destination.Length > track.UserDataLength) throw new ArgumentException("The disc header exceeds a user-data sector.", nameof(destination));
        _dataSector.AsSpan(track.UserDataOffset, destination.Length).CopyTo(destination);
    }
    public void ReadData(int lba, Span<byte> destination, Span<byte> subheader)
    {
        CheckAlive(); int index = FindTrack(lba);
        if (index < 0 || _tracks[index].IsAudio) { destination.Clear(); subheader.Clear(); return; }
        OpticalDiscTrack track = _tracks[index];
        long relative = lba - _driveTracks[index].StartLba;
        ReadSector(index, relative, _dataSector, audio: false);
        int offset = track.UserDataOffset;
        if (!subheader.IsEmpty)
        {
            int headerOffset = track.StoredSectorSize >= 2352 ? 16 : 0;
            _dataSector.AsSpan(headerOffset, subheader.Length).CopyTo(subheader);
            offset = headerOffset + 8;
        }
        int count = Math.Min(destination.Length, track.StoredSectorSize - offset);
        _dataSector.AsSpan(offset, count).CopyTo(destination); destination[count..].Clear();
    }
    public int ReadAudio(int trackIndex, long sampleFrame, Span<short> destination)
    {
        CheckAlive();
        if ((destination.Length & 1) != 0) throw new ArgumentException("Stereo samples are required.", nameof(destination));
        if ((uint)trackIndex >= _tracks.Length || !_tracks[trackIndex].IsAudio) { destination.Clear(); return 0; }
        OpticalDiscTrack track = _tracks[trackIndex];
        int completed = 0, requested = destination.Length / 2;
        while (completed < requested)
        {
            _cancellation.ThrowIfCancellationRequested();
            long frame = sampleFrame + completed;
            long sector = Math.DivRem(frame, 588, out long inSector);
            if (inSector < 0) { sector--; inSector += 588; }
            int count = Math.Min(requested - completed, 588 - (int)inSector);
            Span<short> samples = destination.Slice(completed * 2, count * 2);
            if (sector < -track.StoredPregapSectors || sector >= track.SectorCount)
                samples.Clear();
            else
            {
                ReadSector(trackIndex,sector,_audioSector,audio: true);
                int offset = checked(track.UserDataOffset + (int)inSector * 4);
                for (int i = 0; i < samples.Length; i++)
                {
                    ReadOnlySpan<byte> value = _audioSector.AsSpan(offset + i * 2,2);
                    samples[i] = _bigEndianAudio ? BinaryPrimitives.ReadInt16BigEndian(value) : BinaryPrimitives.ReadInt16LittleEndian(value);
                }
            }
            completed += count;
        }
        return completed;
    }
    public void ReadSubchannel(int lba, Span<byte> destination)
    {
        CheckAlive(); destination.Clear();
        int index = FindTrack(lba); if (index < 0) return;
        long relative = lba - _driveTracks[index].StartLba;
        if (relative < 0) return;
        byte[]? data = _image.ReadSubchannelAsync(index,relative,_cancellation).GetAwaiter().GetResult();
        if (data is not null) data.AsSpan(0,Math.Min(data.Length,destination.Length)).CopyTo(destination);
    }
    private void ReadSector(int track, long index, byte[] buffer, bool audio)
    {
        if (audio ? _audioTrack == track && _audioIndex == index : _dataTrack == track && _dataIndex == index) return;
        OpticalDiscTrack descriptor = _tracks[track];
        _image.ReadStoredSectorAsync(track,index,buffer.AsMemory(0,descriptor.StoredSectorSize),_cancellation).GetAwaiter().GetResult();
        if (audio) { _audioTrack = track; _audioIndex = index; } else { _dataTrack = track; _dataIndex = index; }
    }
    private int FindTrack(int lba)
    {
        for (int i = _driveTracks.Length - 1; i >= 0; i--)
            if (lba >= _driveTracks[i].StartLba && lba < _driveTracks[i].EndLba) return i;
        return -1;
    }
    private void CheckAlive() { ObjectDisposedException.ThrowIf(_disposed,this); _cancellation.ThrowIfCancellationRequested(); }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true; _image.Dispose();
    }
}
