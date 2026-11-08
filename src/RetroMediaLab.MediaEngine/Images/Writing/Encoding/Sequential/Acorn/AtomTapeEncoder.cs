using System.IO;
using System.Collections.Frozen;
using System.Buffers.Binary;
using System.Globalization;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Images.Formats.Tape.AtomTap;
using RetroMediaLab.MediaEngine.Images.Models.Sequential;
using RetroMediaLab.MediaEngine.Interfaces.Encoding;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Functions;
using RetroMediaLab.MediaEngine.Images.Reading.Blocks;

namespace RetroMediaLab.MediaEngine.Images.Writing.Encoding.Sequential.Acorn;

/// <summary>Rebuilds Atom TAP serial segments from complete named files.</summary>
public sealed class AtomTapeEncoder : ISequentialMediaEncoder
{
    public string Id => "acorn-atom-tape";
    public IReadOnlySet<string> FormatIds { get; } = new[] { TapeImageFormatIds.AtomTap, TapeImageFormatIds.Uef, TapeImageFormatIds.Wav }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    public IReadOnlySet<string> MachineIds { get; } = new[] { "acorn-atom" }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    public bool CanEncode(SequentialEncodeRequest request)
    {
        if (!FormatIds.Contains(request.TargetFormatId) || !MachineIds.Contains(request.MachineId) || request.RetainedSegments.Count != 0) return false;
        if (request.TargetFormatId.Equals(TapeImageFormatIds.Wav, StringComparison.OrdinalIgnoreCase) && !PcmSampleFunctions.TryReadSampleRate(request.Parameters, out _)) return false;
        try { AtomTapCodec.Encode(request.Blocks); return true; }
        catch (InvalidDataException) { return false; }
    }
    public async Task<SequentialMediaImageRepresentation> EncodeAsync(SequentialEncodeRequest request, CancellationToken cancellationToken = default)
    {
        if (!CanEncode(request)) throw new InvalidDataException("Atom TAP requires complete valid files.");
        var bytes = AtomTapCodec.Encode(request.Blocks);
        var image = (SequentialMediaImageRepresentation)AtomTapReader.ReadBytes(new("encoded.tap", [], bytes.Length), bytes, cancellationToken).Representation;
        if (!request.TargetFormatId.Equals(TapeImageFormatIds.Wav, StringComparison.OrdinalIgnoreCase)) return image;
        PcmSampleFunctions.TryReadSampleRate(request.Parameters, out var rate);
        var samples = new List<short>(); short level = -UefConstants.PcmAmplitude;
        foreach (var segment in image.Segments ?? [])
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (segment.Kind == SequentialSegmentKind.TapeMark) continue;
            var data = await BlockMediaDataReader.ReadRangeAsync(segment.DataRange, cancellationToken).ConfigureAwait(false) ?? [];
            var seconds = segment.Kind == SequentialSegmentKind.Carrier ? BinaryPrimitives.ReadUInt16LittleEndian(data) / (2d * UefConstants.DefaultBaseFrequency)
                : segment.Kind == SequentialSegmentKind.Silence ? BinaryPrimitives.ReadSingleLittleEndian(data)
                : Math.Max(0, data.Length - 3) * (1 + AtomTapeConstants.DataBits + AtomTapeConstants.StopBits) / (double)AtomTapeConstants.Baud;
            if (seconds * rate > 128L * 1024 * 1024 - samples.Count) throw new InvalidDataException("Atom PCM output exceeds the supported length.");
            if (segment.Kind == SequentialSegmentKind.Carrier)
                FskSerialFunctions.AppendPcmCarrier(samples, ref level, rate, AtomTapeConstants.Baud, UefConstants.AcornZeroFrequency, UefConstants.AcornOneFrequency, seconds);
            else if (segment.Kind == SequentialSegmentKind.Silence) PcmSampleFunctions.AppendSilence(samples, rate, seconds);
            else for (var at = 3; at < data.Length; at++) FskSerialFunctions.AppendPcmFrame(samples, ref level, rate, AtomTapeConstants.Baud,
                UefConstants.AcornZeroFrequency, UefConstants.AcornOneFrequency, AtomTapeConstants.DataBits, AtomTapeConstants.StopBits, data[at]);
        }
        var result = (SequentialMediaImageRepresentation)TapePcmFunctions.Document(new("encoded.wav", []), TapeImageFormatIds.Wav, new(samples.ToArray(), rate)).Representation;
        var pcm = result.Segments![0];
        var metadata = new Dictionary<string, string>(pcm.Metadata)
        {
            [UefConstants.ReconstructionMetadataKey] = bool.TrueString,
            ["sampleRate"] = rate.ToString(CultureInfo.InvariantCulture), ["bitsPerSample"] = "16", ["blockAlign"] = "2"
        };
        return new(result.LogicalLength, result.Duration,
            [new(pcm.Position, pcm.Kind, channelNumber: 0, dataRange: pcm.DataRange, metadata: metadata)]);
    }
}
