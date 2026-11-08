using System.IO;
using System.Collections.Frozen;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Images.Formats.Tape.AtomTap;
using RetroMediaLab.MediaEngine.Images.Models.Sequential;
using RetroMediaLab.MediaEngine.Interfaces.Encoding;

namespace RetroMediaLab.MediaEngine.Images.Writing.Encoding.Sequential.Acorn;

/// <summary>Rebuilds Atom TAP serial segments from complete named files.</summary>
public sealed class AtomTapeEncoder : ISequentialMediaEncoder
{
    public string Id => "acorn-atom-tape";
    public IReadOnlySet<string> FormatIds { get; } = new[] { TapeImageFormatIds.AtomTap }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    public IReadOnlySet<string> MachineIds { get; } = new[] { "acorn-atom" }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    public bool CanEncode(SequentialEncodeRequest request)
    {
        if (!FormatIds.Contains(request.TargetFormatId) || !MachineIds.Contains(request.MachineId) || request.RetainedSegments.Count != 0) return false;
        try { AtomTapCodec.Encode(request.Blocks); return true; }
        catch (InvalidDataException) { return false; }
    }
    public Task<SequentialMediaImageRepresentation> EncodeAsync(SequentialEncodeRequest request, CancellationToken cancellationToken = default)
    {
        if (!CanEncode(request)) throw new InvalidDataException("Atom TAP requires complete valid files.");
        var bytes = AtomTapCodec.Encode(request.Blocks);
        return Task.FromResult((SequentialMediaImageRepresentation)AtomTapReader.ReadBytes(new("encoded.tap", [], bytes.Length), bytes, cancellationToken).Representation);
    }
}
