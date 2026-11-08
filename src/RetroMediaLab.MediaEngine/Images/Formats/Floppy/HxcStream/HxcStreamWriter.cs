using System.IO;
using System.Collections.Frozen;
using System.Text.RegularExpressions;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Images.Models.Flux;
using RetroMediaLab.MediaEngine.Images.Writing;
using RetroMediaLab.MediaEngine.Interfaces.Writing;

namespace RetroMediaLab.MediaEngine.Images.Formats.Floppy.HxcStream;

/// <summary>Publishes the complete related track set atomically, preserving native captures when unchanged.</summary>
public sealed class HxcStreamWriter : IMediaImageWriter
{
    private readonly Func<IReadOnlyDictionary<string, Func<Stream, CancellationToken, Task>>, CancellationToken, Task> publish;
    public HxcStreamWriter() : this((files, token) => AtomicMediaFileSetWriter.WriteAsync(files, token)) { }
    internal HxcStreamWriter(Func<IReadOnlyDictionary<string, Func<Stream, CancellationToken, Task>>, CancellationToken, Task> publish) => this.publish = publish;
    public string Id => MediaImageWriterIds.HxcStream;
    public IReadOnlySet<string> FormatIds { get; } = new[] { DiskImageFormatIds.RawHxcStream }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    public IReadOnlySet<string> ProducedFileExtensions { get; } = new[] { DiskImageFileExtensions.HxcStream }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    public IReadOnlySet<MediaRepresentationKind> RepresentationKinds { get; } = new[] { MediaRepresentationKind.Flux }.ToFrozenSet();
    public bool ProducesMultipleFiles => true;
    public bool CanWrite(MediaImageDocument document, string targetFormatId, string targetExtension) => FormatIds.Contains(targetFormatId)
        && ProducedFileExtensions.Contains(targetExtension) && document.MediaKind == MediaKind.Floppy
        && document.Representation is FluxMediaImageRepresentation { Tracks.Count: > 0 } flux
        && flux.Tracks.All(track => track.Cylinder <= HxcStreamFormat.MaximumTrack && track.Head <= HxcStreamFormat.MaximumHead
            && (Native(document, track, flux.Image.WriteProtected) || HxcStreamEncoding.CanEncode(track)));
    public async Task<IReadOnlyList<string>> WriteAsync(MediaImageDocument document, string outputPath, string targetFormatId, CancellationToken cancellationToken = default)
    {
        if (!CanWrite(document, targetFormatId, Path.GetExtension(outputPath))) throw new InvalidDataException("HxC Stream requires exactly representable flux and index timings.");
        var flux = (FluxMediaImageRepresentation)document.Representation;
        var directory = Path.GetDirectoryName(outputPath) ?? string.Empty;
        var stem = Path.GetFileNameWithoutExtension(outputPath);
        var prefix = Regex.Replace(stem, @"\d{2}\.[01]$", string.Empty, RegexOptions.CultureInvariant);
        var files = new Dictionary<string, Func<Stream, CancellationToken, Task>>(StringComparer.OrdinalIgnoreCase);
        foreach (var track in flux.Tracks.OrderBy(track => track.Cylinder).ThenBy(track => track.Head))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var bytes = Native(document, track, flux.Image.WriteProtected)
                ? Convert.FromBase64String(document.Metadata[$"hxcstream.native.{track.Cylinder}.{track.Head}"])
                : HxcStreamEncoding.Encode(track, flux.Image.WriteProtected, document.Metadata);
            var path = Path.Combine(directory, $"{prefix}{track.Cylinder:00}.{track.Head}.hxcstream");
            files.Add(path, (stream, token) => stream.WriteAsync(bytes, token).AsTask());
        }
        await publish(files, cancellationToken).ConfigureAwait(false);
        return files.Keys.ToArray();
    }
    private static bool Native(MediaImageDocument document, ProtectedTrack track, bool writeProtected) =>
        document.Metadata.ContainsKey($"hxcstream.native.{track.Cylinder}.{track.Head}")
        && document.Metadata.GetValueOrDefault($"hxcstream.digest.{track.Cylinder}.{track.Head}") == HxcStreamEncoding.Digest(track, writeProtected);
}
