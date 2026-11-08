using System.IO;
using System.Collections.Frozen;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Images.Formats.Tape.AtomTap;
using RetroMediaLab.MediaEngine.Images.Models.Sequential;
using RetroMediaLab.MediaEngine.Interfaces.Decoding;

namespace RetroMediaLab.MediaEngine.Images.Reading.Decoding.Sequential.Acorn;

/// <summary>Extracts named Atom files from their serial blocks.</summary>
public sealed class AtomTapeDecoder : ISequentialMediaDecoder
{
    public string Id => "acorn-atom-tape";
    public IReadOnlySet<string> FormatIds { get; } = new[] { TapeImageFormatIds.AtomTap }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    public IReadOnlySet<string> MachineIds { get; } = new[] { "acorn-atom" }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    public bool CanDecode(MediaImageDocument document, string? machineId = null) => document.MediaKind == MediaKind.Tape
        && FormatIds.Contains(document.FormatId) && (machineId is null || MachineIds.Contains(machineId))
        && document.Representation is SequentialMediaImageRepresentation;
    public Task<SequentialDecodeResult> DecodeAsync(MediaImageDocument document, string? machineId = null, CancellationToken cancellationToken = default)
    {
        if (!CanDecode(document, machineId)) throw new InvalidDataException("Not an Atom TAP document.");
        return AtomTapCodec.DecodeAsync((SequentialMediaImageRepresentation)document.Representation, cancellationToken);
    }
}
