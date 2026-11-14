namespace RetroMediaLab.Chips.Controllers.Sega.CD;

/// <summary>Borrowed, decoded disc content. The caller owns its lifetime and container readers.</summary>
public interface ICdDriveMedia
{
    IReadOnlyList<CdTrack> Tracks { get; }
    int LeadOutLba { get; }
    bool HasSubchannel { get; }
    void ReadData(int lba, Span<byte> destination, Span<byte> subheader);
    /// <summary>Reads interleaved signed 16-bit stereo at 44.1 kHz, relative to the track's index 1.</summary>
    int ReadAudio(int trackIndex, long sampleFrame, Span<short> destination);
    void ReadSubchannel(int lba, Span<byte> destination);
}
