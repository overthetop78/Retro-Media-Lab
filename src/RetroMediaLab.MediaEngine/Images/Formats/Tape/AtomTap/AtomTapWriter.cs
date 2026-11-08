using System.IO;
using System.Collections.Frozen;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Images.Models.Sequential;
using RetroMediaLab.MediaEngine.Interfaces;
using RetroMediaLab.MediaEngine.Interfaces.Writing;

namespace RetroMediaLab.MediaEngine.Images.Formats.Tape.AtomTap;

/// <summary>Writes complete checksum-valid Atom files using the native 22-byte header.</summary>
public sealed class AtomTapWriter(IAtomicImageFileWriter? files = null) : IMediaImageWriter
{
    private readonly IAtomicImageFileWriter files = files ?? new AtomicImageFileWriter();
    public string Id => MediaImageWriterIds.TapeAtomTap;
    public IReadOnlySet<string> FormatIds { get; } = new[] { TapeImageFormatIds.AtomTap }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    public IReadOnlySet<MediaRepresentationKind> RepresentationKinds { get; } = new[] { MediaRepresentationKind.Sequential }.ToFrozenSet();
    public IReadOnlySet<string> ProducedFileExtensions { get; } = new[] { DiskImageFileExtensions.Tap }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    public bool ProducesMultipleFiles => false;
    public bool CanWrite(MediaImageDocument document, string targetFormatId, string targetExtension) => FormatIds.Contains(targetFormatId)
        && ProducedFileExtensions.Contains(targetExtension) && document.FormatId.Equals(TapeImageFormatIds.AtomTap, StringComparison.OrdinalIgnoreCase)
        && document.Representation is SequentialMediaImageRepresentation && document.MediaKind == MediaKind.Tape;
    public async Task<IReadOnlyList<string>> WriteAsync(MediaImageDocument document, string outputPath, string targetFormatId, CancellationToken cancellationToken = default)
    {
        if (!CanWrite(document, targetFormatId, Path.GetExtension(outputPath))) throw new InvalidDataException("Not an Atom TAP destination.");
        var decoded = await AtomTapCodec.DecodeAsync((SequentialMediaImageRepresentation)document.Representation, cancellationToken).ConfigureAwait(false);
        if (decoded.UndecodedSegments.Count != 0 || decoded.Diagnostics.Count != 0) throw new InvalidDataException("Atom TAP cannot discard undecoded or damaged blocks.");
        var bytes = AtomTapCodec.Encode(decoded.Blocks);
        await files.WriteAsync(outputPath, (output, token) => output.WriteAsync(bytes, token).AsTask(), cancellationToken).ConfigureAwait(false);
        return [outputPath];
    }
}
