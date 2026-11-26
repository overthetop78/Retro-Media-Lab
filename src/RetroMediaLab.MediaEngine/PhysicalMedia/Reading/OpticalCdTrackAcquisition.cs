using System.Buffers.Binary;
using System.IO;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Images.Formats.Optical.BinCue;
using RetroMediaLab.MediaEngine.Images.Writing;
using RetroMediaLab.MediaEngine.Interfaces;

namespace RetroMediaLab.MediaEngine.PhysicalMedia.Reading;

/// <summary>Exports the main-channel sectors exposed by a CD drive as a BIN/CUE image.</summary>
public sealed class OpticalCdTrackAcquisition
{
    private const int TocHeaderSize = 4;
    private const int TocEntrySize = 8;
    private const byte LeadOut = 0xAA;
    private const byte DataTrackFlag = 0x04;
    private const byte SectorsPerTransfer = 6;

    /// <summary>Exports the tracks of the current TOC. Track index 00 and subchannel data require a dedicated acquisition path.</summary>
    public Task ReadBinCueAsync(IOpticalCdTrackReader drive, string destinationCuePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(drive);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationCuePath);
        string destination = Path.GetFullPath(destinationCuePath);
        if (!Path.GetExtension(destination).Equals(".cue", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("A CD track export requires a .cue destination.", nameof(destinationCuePath));

        return Task.Run(() => ExportAsync(drive, destination, cancellationToken), cancellationToken);
    }

    private static async Task ExportAsync(IOpticalCdTrackReader drive, string destination, CancellationToken token)
    {
        var tracks = ParseToc(drive.ReadTableOfContents());
        var files = new List<CueFileDescriptor>(tracks.Length);
        var outputs = new Dictionary<string, Func<Stream, CancellationToken, Task>>(StringComparer.OrdinalIgnoreCase);
        string directory = Path.GetDirectoryName(destination)!;
        string imageName = Path.GetFileNameWithoutExtension(destination);

        for (int index = 0; index < tracks.Length - 1; index++)
        {
            token.ThrowIfCancellationRequested();
            TocTrack track = tracks[index];
            uint end = tracks[index + 1].Lba;
            OpticalTrackMode mode = track.IsAudio ? OpticalTrackMode.Audio : ReadDataMode(drive, track.Lba);
            string fileName = $"{imageName} (Track {track.Number:D2}).bin";
            var declaration = new CueTrackDeclaration(track.Number, mode, [new OpticalTrackIndex(1, 0)]);
            files.Add(new CueFileDescriptor(fileName, CueFileKind.Binary, [declaration]));
            outputs.Add(Path.Combine(directory, fileName), (stream, cancellation) =>
                CopyTrackAsync(drive, track, end, stream, cancellation));
        }

        outputs.Add(destination, (stream, cancellation) =>
        {
            if (!tracks.AsSpan().SequenceEqual(ParseToc(drive.ReadTableOfContents())))
                throw new IOException("The disc table of contents changed during acquisition.");
            return new CueSheetWriter().WriteAsync(new CueSheetDocument(files), stream, cancellation);
        });
        await AtomicMediaFileSetWriter.WriteAsync(outputs, token, replaceExisting: false).ConfigureAwait(false);
    }

    private static OpticalTrackMode ReadDataMode(IOpticalCdTrackReader drive, uint lba)
    {
        byte[] sector = drive.ReadRawCdSectors(lba, 1, audio: false);
        if (sector.Length != OpticalSectorConstants.RawSectorSize)
            throw new InvalidDataException($"LBA {lba} has an invalid raw sector length.");
        return sector[15] switch
        {
            1 => OpticalTrackMode.Mode1Raw2352,
            2 => OpticalTrackMode.Mode2Raw2352,
            _ => throw new InvalidDataException($"LBA {lba} does not contain a Mode 1 or Mode 2 sector header.")
        };
    }

    private static async Task CopyTrackAsync(IOpticalCdTrackReader drive, TocTrack track, uint end, Stream output,
        CancellationToken cancellationToken)
    {
        for (uint lba = track.Lba; lba < end;)
        {
            cancellationToken.ThrowIfCancellationRequested();
            byte count = (byte)Math.Min(SectorsPerTransfer, end - lba);
            byte[] sectors = drive.ReadRawCdSectors(lba, count, track.IsAudio);
            if (sectors.Length != count * OpticalSectorConstants.RawSectorSize)
                throw new EndOfStreamException($"The drive returned an incomplete transfer at LBA {lba}.");
            await output.WriteAsync(sectors, cancellationToken).ConfigureAwait(false);
            lba += count;
        }
    }

    private static TocTrack[] ParseToc(ReadOnlySpan<byte> toc)
    {
        if (toc.Length < TocHeaderSize)
            throw new InvalidDataException("The CD table of contents header is incomplete.");
        int length = BinaryPrimitives.ReadUInt16BigEndian(toc) + 2;
        int first = toc[2];
        int last = toc[3];
        if (length > toc.Length || first < 1 || last < first || last > 99 ||
            length != TocHeaderSize + (last - first + 2) * TocEntrySize)
            throw new InvalidDataException("The CD table of contents has invalid track bounds.");

        var tracks = new TocTrack[last - first + 2];
        for (int index = 0; index < tracks.Length; index++)
        {
            var entry = toc.Slice(TocHeaderSize + index * TocEntrySize, TocEntrySize);
            int number = entry[2];
            uint lba = BinaryPrimitives.ReadUInt32BigEndian(entry[4..]);
            if (number != (index == tracks.Length - 1 ? LeadOut : first + index) ||
                (index > 0 && lba <= tracks[index - 1].Lba))
                throw new InvalidDataException("CD track entries or lead-out are out of order.");
            tracks[index] = new TocTrack(number, lba, (entry[1] & DataTrackFlag) == 0);
        }
        if (first != 1 || tracks[0].Lba != 0)
            throw new NotSupportedException("This acquisition requires a single CD session beginning at track 1, LBA 0.");
        return tracks;
    }

    private readonly record struct TocTrack(int Number, uint Lba, bool IsAudio);
}
