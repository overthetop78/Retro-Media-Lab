using System.IO;
using System.IO.Compression;
using System.Collections.Frozen;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Images.Models.Sectors;
using RetroMediaLab.MediaEngine.Interfaces;
using RetroMediaLab.MediaEngine.Interfaces.Writing;

namespace RetroMediaLab.MediaEngine.Images.Formats.Floppy.Jfd;

/// <summary>Exports native JFD tables, protection metadata and sector edits without flattening them to ADF.</summary>
public sealed class JfdWriter(IAtomicImageFileWriter? files = null) : IMediaImageWriter
{
    private readonly IAtomicImageFileWriter files = files ?? new AtomicImageFileWriter();
    public string Id => MediaImageWriterIds.Jfd;
    public IReadOnlySet<string> FormatIds { get; } = new[] { DiskImageFormatIds.Jfd }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    public IReadOnlySet<string> ProducedFileExtensions { get; } = new[] { DiskImageFileExtensions.Jfd }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    public IReadOnlySet<MediaRepresentationKind> RepresentationKinds { get; } = new[] { MediaRepresentationKind.Sectors }.ToFrozenSet();
    public bool ProducesMultipleFiles => false;
    public bool CanWrite(MediaImageDocument document, string targetFormatId, string targetExtension) => FormatIds.Contains(targetFormatId)
        && ProducedFileExtensions.Contains(targetExtension) && document.MediaKind == MediaKind.Floppy
        && document.Representation is SectorMediaImageRepresentation && document.Metadata.ContainsKey(JfdCodec.NativeKey);
    public async Task<IReadOnlyList<string>> WriteAsync(MediaImageDocument document, string outputPath, string targetFormatId, CancellationToken cancellationToken = default)
    {
        if (!CanWrite(document, targetFormatId, Path.GetExtension(outputPath))) throw new InvalidDataException("JFD export requires its native tables.");
        var native = JfdCodec.Decode(Convert.FromBase64String(document.Metadata[JfdCodec.NativeKey]));
        var bytes = JfdCodec.Rewrite(native, ((SectorMediaImageRepresentation)document.Representation).Image);
        await files.WriteAsync(outputPath, async (output, token) =>
        {
            if (document.Metadata.GetValueOrDefault("jfd.gzip") == bool.FalseString)
                await output.WriteAsync(bytes, token).ConfigureAwait(false);
            else
            {
                await using var compressed = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true);
                await compressed.WriteAsync(bytes, token).ConfigureAwait(false);
            }
        }, cancellationToken).ConfigureAwait(false);
        return [outputPath];
    }
}
