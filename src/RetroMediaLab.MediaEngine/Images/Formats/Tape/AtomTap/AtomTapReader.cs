using System.IO;
using System.Buffers.Binary;
using System.Collections.Frozen;
using System.Globalization;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Interfaces.Reading;
using RetroMediaLab.MediaEngine.Images.Models.Sequential;
using RetroMediaLab.MediaEngine.Images.Reading.Recognition;
using RetroMediaLab.MediaEngine.Images.Reading.Sources;
using C = RetroMediaLab.MediaEngine.Constants.AtomTapeConstants;

namespace RetroMediaLab.MediaEngine.Images.Formats.Tape.AtomTap;

/// <summary>Lit les fichiers TAP/ATM successifs et reconstruit leurs blocs tape Atom, sans injection en RAM.</summary>
public sealed class AtomTapReader : IMediaImageReader
{
    public IReadOnlySet<string> FormatIds { get; } = new[] { TapeImageFormatIds.AtomTap }.ToFrozenSet();
    public IReadOnlySet<string> Extensions { get; } = new[] { DiskImageFileExtensions.Tap }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyList<ReadOnlyMemory<byte>> Signatures => [];
    public IReadOnlySet<string> AssociatedFileExtensions => FrozenSet<string>.Empty;
    public IReadOnlySet<MediaKind> MediaKinds { get; } = new[] { MediaKind.Tape }.ToFrozenSet();
    public IReadOnlySet<MediaRepresentationKind> RepresentationKinds { get; } = new[] { MediaRepresentationKind.Sequential }.ToFrozenSet();
    public bool SupportsFormatId(string formatId) => FormatIds.Contains(formatId);

    public async ValueTask<bool> CanReadAsync(MediaRecognitionContext context, CancellationToken cancellationToken)
    {
        if (context.RequestedFormatId is not null && !SupportsFormatId(context.RequestedFormatId)) return false;
        if (!Extensions.Contains(Path.GetExtension(context.Source.PrimaryPath))) return false;
        if (context.Length is < C.FileHeaderBytes or > UefConstants.MaximumDecompressedLength) return false;
        var bytes = await File.ReadAllBytesAsync(context.Source.PrimaryPath, cancellationToken).ConfigureAwait(false);
        return Validate(bytes);
    }

    public async Task<MediaImageDocument> ReadAsync(MediaRecognitionContext context, CancellationToken cancellationToken)
    {
        if (context.Length > UefConstants.MaximumDecompressedLength) throw new InvalidDataException("Atom TAP exceeds the supported length.");
        var bytes = await File.ReadAllBytesAsync(context.Source.PrimaryPath, cancellationToken).ConfigureAwait(false);
        return ReadBytes(context.Source, bytes, cancellationToken);
    }

    internal static bool Validate(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty) return false;
        var offset = 0;
        while (offset < bytes.Length)
        {
            if (bytes.Length - offset < C.FileHeaderBytes) return false;
            var header = bytes.Slice(offset, C.FileHeaderBytes);
            var terminated = false;
            foreach (var value in header[..C.FileNameBytes])
            {
                if (value == 0) { terminated = true; continue; }
                if (terminated || value < ' ' || value > '~') return false;
            }
            if (header[0] == 0) return false;
            offset += C.FileHeaderBytes + BinaryPrimitives.ReadUInt16LittleEndian(header[C.LengthOffset..]);
            if (offset > bytes.Length) return false;
        }
        return true;
    }

    internal static MediaImageDocument ReadBytes(MediaSourceDescriptor source, byte[] bytes, CancellationToken token = default)
    {
        if (!Validate(bytes)) throw new InvalidDataException("Invalid or truncated Atom TAP header or payload.");
        var segments = new List<SequentialMediaSegment>();
        var offset = 0;
        while (offset < bytes.Length)
        {
            token.ThrowIfCancellationRequested();
            var file = bytes.AsSpan(offset, C.FileHeaderBytes);
            var nameLength = file[..C.FileNameBytes].IndexOf((byte)0);
            if (nameLength < 0) nameLength = C.FileNameBytes;
            var name = file[..nameLength].ToArray();
            var load = BinaryPrimitives.ReadUInt16LittleEndian(file[C.LoadAddressOffset..]);
            var execute = BinaryPrimitives.ReadUInt16LittleEndian(file[C.ExecutionAddressOffset..]);
            var length = BinaryPrimitives.ReadUInt16LittleEndian(file[C.LengthOffset..]);
            offset += C.FileHeaderBytes;
            var copied = 0;
            var blockNumber = 0;
            var history = ((load + length) >> C.EndAddressShift) & C.FlagHistoryMask;
            do
            {
                token.ThrowIfCancellationRequested();
                var count = Math.Min(C.MaximumBlockBytes, length - copied);
                var flags = (byte)(history | (count > 0 ? C.ContainsData : 0)
                    | (copied + count < length ? C.MoreBlocks : 0) | (blockNumber > 0 ? C.Continuation : 0));
                using var header = new MemoryStream();
                header.Write(Enumerable.Repeat(C.Sync, C.SyncBytes).ToArray());
                header.Write(name);
                header.WriteByte(C.NameTerminator);
                header.WriteByte(flags);
                BigEndian(header, blockNumber);
                header.WriteByte((byte)(count - 1));
                BigEndian(header, execute);
                BigEndian(header, load + copied);
                var headerBytes = header.ToArray();
                var payload = bytes.AsSpan(offset + copied, count).ToArray();
                var checksum = headerBytes.Skip(C.SyncBytes).Sum(value => (int)value) + payload.Sum(value => (int)value);
                Array.Resize(ref payload, count + 1);
                payload[count] = unchecked((byte)checksum);
                Carrier(C.CarrierCycles);
                Data(headerBytes);
                Carrier(C.HeaderDataCarrierCycles);
                Data(payload);
                Add(UefConstants.FloatingGap, BitConverter.GetBytes((float)C.GapSeconds), SequentialSegmentKind.Silence);
                copied += count;
                blockNumber++;
                history = flags >> C.FlagHistoryShift & C.FlagHistoryMask;
            } while (copied < length);
            offset += length;
        }
        return MediaImageDocument.CreateUnexplored(source, TapeImageFormatIds.AtomTap, MediaKind.Tape,
            new SequentialMediaImageRepresentation(bytes.Length, segments: segments), [], new Dictionary<string, string>());

        void Carrier(int cycles)
        {
            var data = new byte[sizeof(ushort)];
            BinaryPrimitives.WriteUInt16LittleEndian(data, checked((ushort)cycles));
            Add(UefConstants.CarrierTone, data, SequentialSegmentKind.Carrier);
        }
        void Data(byte[] data) => Add(UefConstants.DefinedData,
            new byte[] { C.DataBits, C.NoParity, C.StopBits }.Concat(data).ToArray(), SequentialSegmentKind.DataBlock);
        void Add(ushort id, byte[] data, SequentialSegmentKind kind) => segments.Add(new(segments.Count, kind,
            dataRange: new MediaDataRange(0, data.Length, MediaDataRangeKind.Stored, new MemoryRandomAccessData(data)),
            metadata: new Dictionary<string, string>
            {
                [UefConstants.ChunkIdMetadataKey] = id.ToString("x4", CultureInfo.InvariantCulture),
                [UefConstants.BaseFrequencyMetadataKey] = UefConstants.DefaultBaseFrequency.ToString(CultureInfo.InvariantCulture),
                [UefConstants.BaudRateMetadataKey] = C.Baud.ToString(CultureInfo.InvariantCulture),
                [UefConstants.PhaseMetadataKey] = C.InitialPhase.ToString(CultureInfo.InvariantCulture)
            }));
    }

    private static void BigEndian(Stream output, int value)
    {
        output.WriteByte(unchecked((byte)(value >> AtomTapeConstants.DataBits)));
        output.WriteByte(unchecked((byte)value));
    }
}
