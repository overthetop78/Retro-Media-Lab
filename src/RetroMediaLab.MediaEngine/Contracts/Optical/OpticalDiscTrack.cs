using RetroMediaLab.MediaEngine.Enums;

namespace RetroMediaLab.MediaEngine.Contracts;

/// <summary>Container-independent optical track layout, without exposing data-source implementation interfaces.</summary>
public sealed record OpticalDiscTrack(int TrackNumber, OpticalTrackMode Mode, long FirstSector, long SectorCount,
    int StoredSectorSize, int UserDataOffset, int UserDataLength, long PregapSectors, long PostgapSectors,
    long StoredPregapSectors, bool HasSubchannels)
{
    public bool IsAudio => Mode == OpticalTrackMode.Audio;
}
