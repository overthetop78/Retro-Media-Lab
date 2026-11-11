using System.IO;
using System.Collections.Frozen;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Images.Formats.Tape.AtomTap;
using RetroMediaLab.MediaEngine.Images.Models.Sequential;
using RetroMediaLab.MediaEngine.Interfaces.Decoding;
using RetroMediaLab.MediaEngine.Functions;
using RetroMediaLab.MediaEngine.Images.Reading.Blocks;

namespace RetroMediaLab.MediaEngine.Images.Reading.Decoding.Sequential.Acorn;

/// <summary>Extracts named Atom files from their serial blocks.</summary>
public sealed class AtomTapeDecoder : ISequentialMediaDecoder
{
    public string Id => "acorn-atom-tape";
    public IReadOnlySet<string> FormatIds { get; } = new[] { TapeImageFormatIds.AtomTap, TapeImageFormatIds.Uef, TapeImageFormatIds.Wav, TapeImageFormatIds.Csw, TapeImageFormatIds.Raw, TapeImageFormatIds.Voc }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    public IReadOnlySet<string> MachineIds { get; } = new[] { "acorn-atom" }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    public bool CanDecode(MediaImageDocument document, string? machineId = null) => document.MediaKind == MediaKind.Tape
        && FormatIds.Contains(document.FormatId) && (machineId is null || MachineIds.Contains(machineId))
        && document.Representation is SequentialMediaImageRepresentation;
    public async Task<SequentialDecodeResult> DecodeAsync(MediaImageDocument document, string? machineId = null, CancellationToken cancellationToken = default)
    {
        if (!CanDecode(document, machineId)) throw new InvalidDataException("Not an Atom TAP document.");
        var image = (SequentialMediaImageRepresentation)document.Representation;
        if (document.FormatId.Equals(TapeImageFormatIds.AtomTap, StringComparison.OrdinalIgnoreCase))
            return await AtomTapCodec.DecodeAsync(image, cancellationToken).ConfigureAwait(false);
        var pcm = document;
        // L'indexeur matérialise les cycles UEF en PCM avant de rechercher les blocs.
        // Ce document garde son format source, mais ne contient plus de chunks UEF.
        if (document.FormatId.Equals(TapeImageFormatIds.Uef, StringComparison.OrdinalIgnoreCase)
            && !PcmSampleFunctions.TryGetProfile(document.Metadata, out _, out _, out _, out _))
        {
            if ((image.Segments ?? []).All(segment => segment.Kind is SequentialSegmentKind.Carrier or SequentialSegmentKind.Silence
                || segment.Kind == SequentialSegmentKind.DataBlock && segment.Metadata.GetValueOrDefault(UefConstants.ChunkIdMetadataKey) is "0100" or "0104" or "0105"
                || segment.Kind == SequentialSegmentKind.TapeMark && segment.Metadata.GetValueOrDefault(UefConstants.ChunkIdMetadataKey) is "0117" or "0115" or "0113"))
            {
                var values = new List<byte>();
                foreach (var segment in image.Segments ?? [])
                {
                    if (segment.Kind != SequentialSegmentKind.DataBlock) continue;
                    var bytes = await BlockMediaDataReader.ReadRangeAsync(segment.DataRange, cancellationToken).ConfigureAwait(false) ?? [];
                    var id = segment.Metadata.GetValueOrDefault(UefConstants.ChunkIdMetadataKey);
                    if (id is "0100" or "0105") values.AddRange(bytes);
                    else if (id == "0104" && bytes.Length >= 3 && bytes[0] == AtomTapeConstants.DataBits
                        && bytes[1] == AtomTapeConstants.NoParity && bytes[2] is 1 or 2) values.AddRange(bytes.AsSpan(3).ToArray());
                    else return new(Id, 0, [], image.Segments ?? [], ["UEF serial framing is not compatible with Atom tape blocks."]);
                }
                return FromSource(await AtomTapCodec.DecodeStreamAsync(values.ToArray(), cancellationToken).ConfigureAwait(false));
            }
            var signal = await TapeSignalFunctions.UefAsync(image, new Dictionary<long, int>(), cancellationToken).ConfigureAwait(false);
            pcm = TapePcmFunctions.Document(document.Source, TapeImageFormatIds.Wav, signal);
        }
        if (!PcmSampleFunctions.TryGetProfile(pcm.Metadata, out _, out _, out _, out _))
            return new(Id, 0, [], image.Segments ?? [], ["Atom decoding requires a PCM signal."]);
        var channels = await TapePcmFunctions.ChannelsAsync(pcm, cancellationToken).ConfigureAwait(false);
        var serial = FskSerialFunctions.Decode(channels.Channels[0], channels.Rate, AtomTapeConstants.Baud,
            UefConstants.AcornZeroFrequency, UefConstants.AcornOneFrequency, AtomTapeConstants.DataBits, AtomTapeConstants.StopBits,
            UefConstants.MinimumSamplesPerBit);
        var decoded = await AtomTapCodec.DecodeStreamAsync(serial.Bytes.ToArray(), cancellationToken).ConfigureAwait(false);
        return FromSource(decoded);

        SequentialDecodeResult FromSource(SequentialDecodeResult result)
        {
            var positions = (image.Segments ?? []).Select(segment => segment.Position).Distinct().ToArray();
            var blocks = result.Blocks.Select(block => new SequentialDecodedBlock(block.Position, block.Data, positions, block.IntegrityValid, block.Metadata)).ToArray();
            return new(Id, result.Confidence, blocks, blocks.Length == 0 || result.UndecodedSegments.Count > 0 || result.Diagnostics.Count > 0 ? image.Segments ?? [] : [], result.Diagnostics);
        }
    }
}
