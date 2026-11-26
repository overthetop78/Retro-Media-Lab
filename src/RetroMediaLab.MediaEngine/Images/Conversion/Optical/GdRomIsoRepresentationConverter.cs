using System.Buffers.Binary;
using System.IO;
using System.Text;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Images.Formats.Optical.Gdi;
using RetroMediaLab.MediaEngine.Images.Models.Optical;
using RetroMediaLab.MediaEngine.Interfaces.Conversion;
using RetroMediaLab.MediaFileSystems;
using RetroMediaLab.MediaFileSystems.FileSystems.Iso9660;
using RetroMediaLab.MediaFileSystems.Interfaces;
using FileSystemEntryKind = RetroMediaLab.MediaFileSystems.FileSystemEntryKind;
using VolumeDescriptor = RetroMediaLab.MediaFileSystems.Contracts.MediaVolumeDescriptor;

namespace RetroMediaLab.MediaEngine.Images.Conversion.Optical;

/// <summary>Reauthors the GD-ROM high-density file system as a standard single-track data image.</summary>
internal sealed class GdRomIsoRepresentationConverter : IMediaRepresentationConverter
{
    private const int BlockSize = 2048;
    private const int PrimaryBlock = 16;
    private const int LittlePathTableBlock = 18;

    public string Id => "gd-rom-files-to-iso";
    public IReadOnlySet<MediaRepresentationKind> SourceRepresentationKinds { get; } = new HashSet<MediaRepresentationKind> { MediaRepresentationKind.OpticalTracks };
    public IReadOnlySet<MediaRepresentationKind> TargetRepresentationKinds => SourceRepresentationKinds;

    public bool CanConvert(MediaImageDocument source, string targetFormatId, MediaRepresentationKind targetRepresentationKind) =>
        targetFormatId == OpticalImageFormatIds.Iso && targetRepresentationKind == MediaRepresentationKind.OpticalTracks &&
        FindVolume(source) is { } volume && new Iso9660FileSystemReader().CanRead(source, volume);

    public Task<MediaRepresentationConversionResult> ConvertAsync(MediaImageDocument source, string targetFormatId,
        MediaRepresentationKind targetRepresentationKind, IReadOnlyDictionary<string, string> options,
        Action<MediaExplorationProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        if (targetFormatId != OpticalImageFormatIds.Iso || targetRepresentationKind != MediaRepresentationKind.OpticalTracks ||
            FindVolume(source) is not { } volume)
            throw new NotSupportedException("The source does not contain a standard GD-ROM high-density track.");

        var (catalog, extents) = new Iso9660FileSystemReader().ReadCatalog(source, volume, cancellationToken);
        if (catalog.Warnings.Count != 0)
            throw new InvalidDataException("The GD-ROM directory tree could not be read in full.");

        var image = BuildImage(catalog, extents, cancellationToken);
        var track = new OpticalTrackDescriptor(1, 1, OpticalTrackMode.Mode1Data2048, 0, image.Length / BlockSize,
            BlockSize, 0, BlockSize, image, 0);
        var result = new MediaImageDocument(source.Source, OpticalImageFormatIds.Iso, MediaKind.Optical,
            new OpticalMediaImageRepresentation(image.Length, [track]), [], [], new Dictionary<string, string>());
        return Task.FromResult(new MediaRepresentationConversionResult(result, [],
            ["This data ISO contains the primary ISO 9660 files from the GD-ROM high-density area. The low-density area, boot sectors, audio tracks, gaps and original disc layout are omitted."]));
    }

    private static VolumeDescriptor? FindVolume(MediaImageDocument source)
    {
        if (source.Representation is not OpticalMediaImageRepresentation { Tracks: { } tracks } ||
            (source.FormatId != OpticalImageFormatIds.Gdi && source.Metadata.GetValueOrDefault("mediaProfile") != GdiConstants.MediaProfile))
            return null;

        var data = tracks.FirstOrDefault(track => track is { SessionNumber: 2, TrackNumber: 3,
            FirstSector: GdiConstants.HighDensityStartLba, UserDataLength: BlockSize, IsAudio: false });
        return data is null ? null : new VolumeDescriptor(checked(data.FirstSector * BlockSize),
            checked(data.SectorCount * BlockSize), RetroMediaLab.MediaFileSystems.Constants.MediaVolumeOrigins.OpticalTrack,
            sessionNumber: data.SessionNumber, trackNumber: data.TrackNumber);
    }

    private static IsoImageData BuildImage(FileSystemVolume catalog, IMediaOpticalTrack extents, CancellationToken cancellationToken)
    {
        var root = new DirectoryLayout(null, [0], 1, 1, 0);
        var directories = new List<DirectoryLayout> { root };
        var files = new List<FileLayout>();
        for (int index = 0; index < directories.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var directory = directories[index];
            foreach (var entry in (directory.Entry?.Children ?? catalog.Entries).OrderBy(entry => entry.Name, StringComparer.Ordinal))
            {
                var identifier = EncodeIdentifier(entry);
                if (!directory.Names.Add(entry.Name))
                    throw new InvalidDataException($"The GD-ROM directory has duplicate file identifiers for '{entry.Name}'.");

                if (entry.Kind == FileSystemEntryKind.Directory)
                {
                    if (directory.Depth >= 8 || directories.Count == ushort.MaxValue)
                        throw new NotSupportedException("The GD-ROM directory hierarchy exceeds the ISO 9660 limits.");
                    var child = new DirectoryLayout(entry, identifier, (ushort)(directories.Count + 1), directory.Number, directory.Depth + 1);
                    directory.Children.Add(child);
                    directories.Add(child);
                }
                else
                {
                    if (entry.Kind != FileSystemEntryKind.File || entry.StorageReference < 0 || entry.Size is < 0 or > uint.MaxValue)
                        throw new NotSupportedException($"Unsupported ISO 9660 file layout for '{entry.Name}'.");
                    var file = new FileLayout(entry, identifier);
                    directory.Children.Add(file);
                    files.Add(file);
                }
            }
        }

        int pathTableBytes = directories.Sum(directory => 8 + directory.Identifier.Length + (directory.Identifier.Length & 1));
        uint bigPathTableBlock = checked(LittlePathTableBlock + Blocks(pathTableBytes));
        uint nextBlock = checked(bigPathTableBlock + Blocks(pathTableBytes));

        foreach (var directory in directories)
        {
            int offset = 68;
            foreach (var child in directory.Children)
                offset = checked(RecordOffset(offset, child.Identifier) + RecordLength(child.Identifier));
            uint blocks = Blocks(offset);
            directory.Block = nextBlock;
            directory.ByteLength = checked(blocks * BlockSize);
            nextBlock = checked(nextBlock + blocks);
        }

        var metadata = new byte[checked((int)(nextBlock * (long)BlockSize))];
        foreach (var file in files)
        {
            file.Block = file.ByteLength == 0 ? root.Block : nextBlock;
            nextBlock = checked(nextBlock + Blocks(file.ByteLength));
        }

        WritePrimaryDescriptor(metadata.AsSpan(PrimaryBlock * BlockSize, BlockSize), catalog.Name, root, nextBlock,
            checked((uint)pathTableBytes), bigPathTableBlock);
        WriteDescriptorHeader(metadata.AsSpan((PrimaryBlock + 1) * BlockSize, BlockSize), 255);
        WritePathTable(metadata.AsSpan(LittlePathTableBlock * BlockSize, pathTableBytes), directories, bigEndian: false);
        WritePathTable(metadata.AsSpan(checked((int)(bigPathTableBlock * BlockSize)), pathTableBytes), directories, bigEndian: true);

        foreach (var directory in directories)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var parent = directories[directory.ParentNumber - 1];
            Span<byte> data = metadata.AsSpan(checked((int)(directory.Block * BlockSize)), checked((int)directory.ByteLength));
            int offset = WriteDirectoryRecord(data, directory, [0]);
            offset += WriteDirectoryRecord(data[offset..], parent, [1]);
            foreach (var child in directory.Children)
            {
                offset = RecordOffset(offset, child.Identifier);
                offset += WriteDirectoryRecord(data[offset..], child, child.Identifier);
            }
        }

        return new IsoImageData(metadata, files.Where(file => file.ByteLength != 0).ToArray(), extents, nextBlock);
    }

    private static byte[] EncodeIdentifier(FileSystemEntry entry)
    {
        string name = entry.Name;
        if (name.Length is 0 or > 31 || name.Any(character => !IsIdentifierCharacter(character) && character != '.') ||
            (entry.Kind == FileSystemEntryKind.Directory && name.Contains('.')) ||
            name.Count(character => character == '.') > 1 || name.StartsWith('.') || name.EndsWith('.'))
            throw new NotSupportedException($"The name '{name}' cannot be preserved in the standard ISO 9660 directory.");

        if (entry.Kind == FileSystemEntryKind.File)
            name += name.Contains('.') ? ";1" : ".;1";
        return Encoding.ASCII.GetBytes(name);
    }

    private static bool IsIdentifierCharacter(char character) => character is >= 'A' and <= 'Z' or >= '0' and <= '9' or '_';

    private static void WriteDescriptorHeader(Span<byte> descriptor, byte type)
    {
        descriptor[0] = type;
        "CD001"u8.CopyTo(descriptor[1..]);
        descriptor[6] = 1;
    }

    private static void WritePrimaryDescriptor(Span<byte> descriptor, string name, DirectoryLayout root, uint volumeBlocks,
        uint pathTableBytes, uint bigPathTableBlock)
    {
        WriteDescriptorHeader(descriptor, 1);
        descriptor[8..72].Fill(0x20);
        for (int index = 0; index < Math.Min(name.Length, 32); index++)
        {
            char character = char.ToUpperInvariant(name[index]);
            descriptor[40 + index] = (byte)(IsIdentifierCharacter(character) || character == ' ' ? character : '_');
        }

        WriteBoth32(descriptor[80..], volumeBlocks);
        WriteBoth16(descriptor[120..], 1);
        WriteBoth16(descriptor[124..], 1);
        WriteBoth16(descriptor[128..], BlockSize);
        WriteBoth32(descriptor[132..], pathTableBytes);
        BinaryPrimitives.WriteUInt32LittleEndian(descriptor[140..], LittlePathTableBlock);
        BinaryPrimitives.WriteUInt32BigEndian(descriptor[148..], bigPathTableBlock);
        WriteDirectoryRecord(descriptor[156..], root, [0]);
        descriptor[190..813].Fill(0x20);
        for (int offset = 813; offset < 881; offset += 17)
            descriptor.Slice(offset, 16).Fill((byte)'0');
        descriptor[881] = 1;
    }

    private static void WritePathTable(Span<byte> data, List<DirectoryLayout> directories, bool bigEndian)
    {
        foreach (var directory in directories)
        {
            data[0] = (byte)directory.Identifier.Length;
            if (bigEndian)
            {
                BinaryPrimitives.WriteUInt32BigEndian(data[2..], directory.Block);
                BinaryPrimitives.WriteUInt16BigEndian(data[6..], directory.ParentNumber);
            }
            else
            {
                BinaryPrimitives.WriteUInt32LittleEndian(data[2..], directory.Block);
                BinaryPrimitives.WriteUInt16LittleEndian(data[6..], directory.ParentNumber);
            }

            directory.Identifier.CopyTo(data[8..]);
            data = data[(8 + directory.Identifier.Length + (directory.Identifier.Length & 1))..];
        }
    }

    private static int WriteDirectoryRecord(Span<byte> data, EntryLayout entry, ReadOnlySpan<byte> identifier)
    {
        int length = RecordLength(identifier);
        data[0] = (byte)length;
        WriteBoth32(data[2..], entry.Block);
        WriteBoth32(data[10..], entry.ByteLength);
        var date = entry.Entry?.Modified?.UtcDateTime ?? DateTime.UnixEpoch;
        if (date.Year is < 1900 or > 2155) date = DateTime.UnixEpoch;
        data[18] = (byte)(date.Year - 1900);
        data[19] = (byte)date.Month;
        data[20] = (byte)date.Day;
        data[21] = (byte)date.Hour;
        data[22] = (byte)date.Minute;
        data[23] = (byte)date.Second;
        data[25] = entry is DirectoryLayout ? (byte)2 : (byte)0;
        WriteBoth16(data[28..], 1);
        data[32] = (byte)identifier.Length;
        identifier.CopyTo(data[33..]);
        return length;
    }

    private static uint Blocks(long bytes) => checked((uint)((bytes + BlockSize - 1) / BlockSize));
    private static int RecordLength(ReadOnlySpan<byte> identifier) => 33 + identifier.Length + ((identifier.Length + 1) & 1);
    private static int RecordOffset(int offset, ReadOnlySpan<byte> identifier) =>
        offset % BlockSize + RecordLength(identifier) <= BlockSize ? offset : checked((int)(Blocks(offset) * BlockSize));

    private static void WriteBoth32(Span<byte> destination, uint value)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(destination, value);
        BinaryPrimitives.WriteUInt32BigEndian(destination[4..], value);
    }

    private static void WriteBoth16(Span<byte> destination, ushort value)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(destination, value);
        BinaryPrimitives.WriteUInt16BigEndian(destination[2..], value);
    }

    private abstract class EntryLayout(FileSystemEntry? entry, byte[] identifier)
    {
        public FileSystemEntry? Entry { get; } = entry;
        public byte[] Identifier { get; } = identifier;
        public uint Block { get; set; }
        public uint ByteLength { get; set; } = checked((uint)(entry?.Size ?? 0));
    }

    private sealed class DirectoryLayout(FileSystemEntry? entry, byte[] identifier, ushort number, ushort parentNumber, int depth)
        : EntryLayout(entry, identifier)
    {
        public List<EntryLayout> Children { get; } = [];
        public HashSet<string> Names { get; } = new(StringComparer.Ordinal);
        public ushort Number { get; } = number;
        public ushort ParentNumber { get; } = parentNumber;
        public int Depth { get; } = depth;
    }

    private sealed class FileLayout(FileSystemEntry entry, byte[] identifier) : EntryLayout(entry, identifier)
    {
        public long SourceBlock { get; } = entry.StorageReference;
    }

    private sealed class IsoImageData(byte[] metadata, FileLayout[] files, IMediaOpticalTrack extents, uint blockCount)
        : IMediaRandomAccessData
    {
        private readonly long[] _fileOffsets = files.Select(file => (long)file.Block * BlockSize).ToArray();
        public long Length => (long)blockCount * BlockSize;

        public async ValueTask ReadExactlyAsync(long offset, Memory<byte> destination, CancellationToken cancellationToken = default)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            if (offset > Length - destination.Length)
                throw new ArgumentOutOfRangeException(nameof(destination));

            while (!destination.IsEmpty)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int count;
                if (offset < metadata.Length)
                {
                    count = (int)Math.Min(destination.Length, metadata.Length - offset);
                    metadata.AsMemory((int)offset, count).CopyTo(destination);
                }
                else
                {
                    int index = Array.BinarySearch(_fileOffsets, offset);
                    if (index < 0) index = ~index - 1;
                    var file = files[index];
                    long position = offset - _fileOffsets[index];
                    int withinBlock = (int)(position % BlockSize);
                    count = Math.Min(destination.Length, BlockSize - withinBlock);
                    if (position >= file.ByteLength)
                    {
                        destination.Span[..count].Clear();
                    }
                    else
                    {
                        byte[] data = await extents.ReadUserDataAsync(file.SourceBlock + position / BlockSize, cancellationToken).ConfigureAwait(false);
                        int available = (int)Math.Min(count, file.ByteLength - position);
                        data.AsMemory(withinBlock, available).CopyTo(destination);
                        destination.Span[available..count].Clear();
                    }
                }
                offset += count;
                destination = destination[count..];
            }
        }
    }
}
