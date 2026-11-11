using RetroMediaLab.MediaFileSystems.Primitives;
using RetroMediaLab.MediaEngine.Images.Reading.Blocks;
using System.Buffers.Binary;
using System.Collections.Frozen;
using System.Globalization;
using System.Text;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;

using RetroMediaLab.MediaEngine.Functions;
using RetroMediaLab.MediaEngine.Interfaces.Decoding;
using RetroMediaLab.MediaEngine.Images.Models.Sectors;
using RetroMediaLab.MediaEngine.Images.Reading.Recognition;
using RetroMediaLab.MediaEngine.Images.Models.Sequential;

namespace RetroMediaLab.MediaEngine.Images.Reading.Decoding.Sequential.Acorn;

/// <summary>Decodes Acorn tape filing system blocks from UEF byte streams or compatible PCM FSK signals.</summary>
public sealed class AcornTapeDecoder : ISequentialMediaDecoder
{
    private static readonly IReadOnlySet<string> SupportedFormats = new[] { TapeImageFormatIds.Uef, TapeImageFormatIds.Wav, TapeImageFormatIds.Csw, TapeImageFormatIds.Raw, TapeImageFormatIds.Voc }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    private static readonly IReadOnlySet<string> SupportedMachines = new[] { DiskSystemIds.AcornBbc }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public string Id => UefConstants.DecoderId;
    public IReadOnlySet<string> FormatIds => SupportedFormats;
    public IReadOnlySet<string> MachineIds => SupportedMachines;

    public bool CanDecode(MediaImageDocument document, string? machineId = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        return SequentialDecodeFunctions.CanDecodeTape(document, machineId, SupportedFormats, SupportedMachines);
    }

    public async Task<SequentialDecodeResult> DecodeAsync(MediaImageDocument document, string? machineId = null, CancellationToken cancellationToken = default)
    {
        if (!CanDecode(document, machineId))
            throw new InvalidDataException("The media document is not compatible with the Acorn tape decoder.");
        var representation = (SequentialMediaImageRepresentation)document.Representation;
        return document.FormatId.Equals(TapeImageFormatIds.Uef, StringComparison.OrdinalIgnoreCase) && !document.Metadata.ContainsKey("sampleRate") ? await DecodeUefAsync(document, representation, cancellationToken).ConfigureAwait(false) : await DecodeWaveAsync(document, representation, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<SequentialDecodeResult> DecodeUefAsync(MediaImageDocument document, SequentialMediaImageRepresentation representation, CancellationToken cancellationToken)
    {
        var blocks = new List<SequentialDecodedBlock>();
        var consumed = new HashSet<long>();
        foreach (var segment in (representation.Segments ?? []).OrderBy(segment => segment.Position))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (segment.Kind != SequentialSegmentKind.DataBlock
                || !IsImplicitData(segment)
                || await BlockMediaDataReader.ReadRangeAsync(segment.DataRange, cancellationToken).ConfigureAwait(false) is not { } bytes)
                continue;
            var decoded = DecodeBlocks(bytes, segment.Position, "uef-implicit", blocks.Count);
            if (decoded.Count == 0) continue;
            blocks.AddRange(decoded);
            consumed.Add(segment.Position);
        }
        if ((representation.Segments ?? []).Any(segment => segment.Metadata.GetValueOrDefault(UefConstants.ChunkIdMetadataKey) is "0102" or "0104" or "0105" or "0114"))
        {
            var signal = await TapeSignalFunctions.UefAsync(representation, new Dictionary<long, int>(), cancellationToken).ConfigureAwait(false);
            var wave = TapePcmFunctions.Document(document.Source, TapeImageFormatIds.Wav, signal);
            return await DecodeWaveAsync(wave, (SequentialMediaImageRepresentation)wave.Representation, cancellationToken).ConfigureAwait(false);
        }
        return CreateResult(representation, blocks, consumed, "No valid Acorn tape block was found in the UEF byte streams.");
    }

    private static async Task<SequentialDecodeResult> DecodeWaveAsync(MediaImageDocument document, SequentialMediaImageRepresentation representation, CancellationToken cancellationToken)
    {
        if (!PcmSampleFunctions.TryGetProfile(document.Metadata, out _, out _, out _, out _))
            return new SequentialDecodeResult(UefConstants.DecoderId, 0, [], representation.Segments ?? [], ["The WAV PCM profile is incomplete or incompatible."]);

        // Les segments PCM sont des repères de présentation, pas des resets UART.
        // Le canal est continu même si une frontière coupe un caractère ou sa porteuse.
        var pcm = await TapePcmFunctions.ChannelsAsync(document, cancellationToken).ConfigureAwait(false);
        var serial = FskSerialFunctions.Decode(pcm.Channels[0], pcm.Rate, UefConstants.DefaultBaudRate,
            UefConstants.AcornZeroFrequency, UefConstants.AcornOneFrequency, UefConstants.AcornDataBitsPerFrame,
            UefConstants.AcornStopBitsPerFrame, UefConstants.MinimumSamplesPerBit);
        var slower = FskSerialFunctions.Decode(pcm.Channels[0], pcm.Rate, 300,
            UefConstants.AcornZeroFrequency, UefConstants.AcornOneFrequency, UefConstants.AcornDataBitsPerFrame,
            UefConstants.AcornStopBitsPerFrame, UefConstants.MinimumSamplesPerBit);
        var sourcePositions = (representation.Segments ?? []).Where(segment => segment.Kind == SequentialSegmentKind.Samples && segment.ChannelNumber is null or 0).Select(segment => segment.Position).Distinct().ToArray();
        var fastBlocks = DecodeBlocks(serial.Bytes, sourcePositions, "wav-fsk", 0, serial.Positions);
        var slowBlocks = DecodeBlocks(slower.Bytes, sourcePositions, "wav-fsk", 0, slower.Positions);
        // Le CRC valide les blocs à chaque débit ; une même bande peut changer de débit entre deux fichiers.
        var valid = fastBlocks.Concat(slowBlocks).Where(block => block.IntegrityValid == true).OrderBy(block => int.Parse(block.Metadata["samplePosition"], CultureInfo.InvariantCulture)).DistinctBy(block => (block.Metadata["samplePosition"], block.Metadata["fileName"], block.Metadata["blockNumber"])).ToArray();
        IReadOnlyList<SequentialDecodedBlock> blocks = valid.Length == 0 ? fastBlocks.Count >= slowBlocks.Count ? fastBlocks : slowBlocks : valid.Select((block, index) => new SequentialDecodedBlock(index, block.Data, block.SourceSegmentPositions, block.IntegrityValid, block.Metadata)).ToArray();
        var consumed = blocks.Count == 0 ? [] : sourcePositions.ToHashSet();
        var result = CreateResult(representation, blocks, consumed, "No valid Acorn tape block was found in the decoded WAV serial stream.");
        return result;
    }

    private static IReadOnlyList<SequentialDecodedBlock> DecodeBlocks(IReadOnlyList<byte> bytes, long sourcePosition, string sourceKind, int firstPosition) => DecodeBlocks(bytes, [sourcePosition], sourceKind, firstPosition);

    private static IReadOnlyList<SequentialDecodedBlock> DecodeBlocks(IReadOnlyList<byte> bytes, IReadOnlyList<long> sourcePositions, string sourceKind, int firstPosition, IReadOnlyList<int>? byteSamples = null)
    {
        var blocks = new List<SequentialDecodedBlock>();
        for (var offset = 0; offset < bytes.Count; offset++)
        {
            if (bytes[offset] != UefConstants.AcornBlockSync) continue;
            var nameStart = offset + 1;
            var nameEnd = nameStart;
            while (nameEnd < bytes.Count
                   && nameEnd - nameStart <= UefConstants.AcornMaximumFileNameLength
                   && bytes[nameEnd] != 0)
                nameEnd++;
            if (nameEnd >= bytes.Count
                || bytes[nameEnd] != 0
                || nameEnd - nameStart > UefConstants.AcornMaximumFileNameLength)
                continue;

            var fixedHeaderStart = nameEnd + 1;
            var fixedHeaderLength = UefConstants.AcornLoadAddressLength
                + UefConstants.AcornExecutionAddressLength
                + UefConstants.AcornBlockNumberLength
                + UefConstants.AcornDataLengthFieldLength
                + UefConstants.AcornFlagsLength
                + UefConstants.AcornNextAddressLength;
            var headerEnd = fixedHeaderStart + fixedHeaderLength;
            if (headerEnd + UefConstants.AcornChecksumLength > bytes.Count) continue;

            var dataLengthOffset = fixedHeaderStart + UefConstants.AcornLoadAddressLength + UefConstants.AcornExecutionAddressLength + UefConstants.AcornBlockNumberLength;
            var dataLength = ReadUInt16LittleEndian(bytes, dataLengthOffset);
            var dataStart = headerEnd + UefConstants.AcornChecksumLength;
            var blockEnd = dataStart + dataLength + UefConstants.AcornChecksumLength;
            if (blockEnd > bytes.Count) continue;

            var headerBytes = Copy(bytes, nameStart, headerEnd - nameStart);
            var dataBytes = Copy(bytes, dataStart, dataLength);
            var storedHeaderChecksum = ReadUInt16BigEndian(bytes, headerEnd);
            var storedDataChecksum = ReadUInt16BigEndian(bytes, dataStart + dataLength);
            var headerValid = Crc16Calculator.Compute(headerBytes, UefConstants.AcornChecksumPolynomial, UefConstants.AcornChecksumInitialValue) == storedHeaderChecksum;
            var dataValid = Crc16Calculator.Compute(dataBytes, UefConstants.AcornChecksumPolynomial, UefConstants.AcornChecksumInitialValue) == storedDataChecksum;

            var cursor = fixedHeaderStart;
            var loadAddress = ReadUInt32LittleEndian(bytes, cursor);
            cursor += UefConstants.AcornLoadAddressLength;
            var executionAddress = ReadUInt32LittleEndian(bytes, cursor);
            cursor += UefConstants.AcornExecutionAddressLength;
            var blockNumber = ReadUInt16LittleEndian(bytes, cursor);
            cursor += UefConstants.AcornBlockNumberLength + UefConstants.AcornDataLengthFieldLength;
            var flags = bytes[cursor++];
            var nextAddress = ReadUInt32LittleEndian(bytes, cursor);
            var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [UefConstants.SourceKindMetadataKey] = sourceKind,
                [UefConstants.FileNameMetadataKey] = System.Text.Encoding.ASCII.GetString(Copy(bytes, nameStart, nameEnd - nameStart)),
                [UefConstants.LoadAddressMetadataKey] = loadAddress.ToString("x8", CultureInfo.InvariantCulture),
                [UefConstants.ExecutionAddressMetadataKey] = executionAddress.ToString("x8", CultureInfo.InvariantCulture),
                [UefConstants.BlockNumberMetadataKey] = blockNumber.ToString(CultureInfo.InvariantCulture),
                [UefConstants.FlagsMetadataKey] = flags.ToString("x2", CultureInfo.InvariantCulture),
                [UefConstants.NextAddressMetadataKey] = nextAddress.ToString("x8", CultureInfo.InvariantCulture)
            };
            metadata["byteOffset"] = offset.ToString(CultureInfo.InvariantCulture);
            if (byteSamples is not null && offset < byteSamples.Count) metadata["samplePosition"] = byteSamples[offset].ToString(CultureInfo.InvariantCulture);
            blocks.Add(new SequentialDecodedBlock(firstPosition + blocks.Count, dataBytes, sourcePositions, headerValid && dataValid, metadata));
            offset = blockEnd - 1;
        }
        return blocks;
    }

    private static SequentialDecodeResult CreateResult(SequentialMediaImageRepresentation representation, IReadOnlyList<SequentialDecodedBlock> blocks, IReadOnlySet<long> consumed, string emptyDiagnostic)
    {
        var undecoded = (representation.Segments ?? []).Where(segment => !consumed.Contains(segment.Position)).ToArray();
        var confidence = blocks.Count == 0 ? 0 : blocks.Count(block => block.IntegrityValid == true) / (double)blocks.Count;
        return new SequentialDecodeResult(UefConstants.DecoderId, confidence, blocks, undecoded, blocks.Count == 0 ? [emptyDiagnostic] : []);
    }

    private static bool IsImplicitData(SequentialMediaSegment segment) => segment.Metadata.TryGetValue(UefConstants.ChunkIdMetadataKey, out var chunkId) && ushort.TryParse(chunkId, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var parsed) && parsed == UefConstants.ImplicitData;

    private static byte[] Copy(IReadOnlyList<byte> source, int offset, int length)
        => ByteListCopy.Copy(source, offset, length);

    private static ushort ReadUInt16LittleEndian(IReadOnlyList<byte> source, int offset) => BinaryPrimitives.ReadUInt16LittleEndian(Copy(source, offset, sizeof(ushort)));

    private static ushort ReadUInt16BigEndian(IReadOnlyList<byte> source, int offset) => BinaryPrimitives.ReadUInt16BigEndian(Copy(source, offset, sizeof(ushort)));

    private static uint ReadUInt32LittleEndian(IReadOnlyList<byte> source, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(Copy(source, offset, sizeof(uint)));
}
