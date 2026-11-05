namespace RetroMediaLab.MediaEngine.Images.Formats.HardDisk.MfmEmu;

/// <summary>Disposition EMU publiée par le projet MFM de David Gesswein, emu_tran_file.c.</summary>
internal static class MfmEmuFormat
{
    internal static ReadOnlySpan<byte> Signature => [0xee, 0x4d, 0x46, 0x4d, 0x0d, 0x0a, 0x1a, 0x00];
    internal const uint CurrentVersion = 0x02020200;
    internal const uint TypeMask = 0xff000000;
    internal const uint FileType = 0x02000000;
    internal const uint MajorMask = 0x00ff0000;
    internal const uint FirstMajor = 0x00010000;
    internal const uint LatestMajor = 0x00020000;
    internal const uint ReservedVersionMask = 0x000000ff;
    internal const uint StringVersion = 0x02020000;
    internal const uint StartTimeVersion = 0x02020200;
    internal const int FixedHeaderLength = 36;
    internal const int VersionOffset = 8;
    internal const int HeaderLengthOffset = 12;
    internal const int TrackDataLengthOffset = 16;
    internal const int TrackHeaderLengthOffset = 20;
    internal const int CylinderCountOffset = 24;
    internal const int HeadCountOffset = 28;
    internal const int SampleRateOffset = 32;
    internal const int WordLength = 4;
    internal const int MinimumTrackHeaderLength = 12;
    internal const int TrackCylinderOffset = 4;
    internal const int TrackHeadOffset = 8;
    internal const uint TrackMarker = 0x12345678;
    internal const int EndAddress = -1;
    internal const int StringTerminatorLength = 1;
    internal const int CopyBufferLength = 65536;
    internal const uint AdaptecSampleRate = 10_000_000;
    internal const int AdaptecTrackByteLength = 20_836;
    internal const int BitsPerByte = 8;
    internal const int BitsPerWord = 32;
    internal const uint FirstWordBit = 0x80000000;
    internal const ulong NanosecondsPerSecond = 1000000000;
    internal const string UnsupportedSignalRate = "The recorded cell rate exceeds the nanosecond signal resolution.";
    internal const string UncapturedWrite = "The write window contains a transition outside the stored track cells.";
    internal const string InvalidHeader = "Invalid MFM EMU header.";
    internal const string InvalidLength = "The EMU length does not match its declared tracks and end marker.";
    internal const string InvalidTrack = "The EMU track address does not match its physical position.";
    internal const string InvalidEndMarker = "Invalid MFM EMU end marker.";
    internal const string InvalidOutput = "MFM EMU output requires a physical MFM EMU representation.";
}
