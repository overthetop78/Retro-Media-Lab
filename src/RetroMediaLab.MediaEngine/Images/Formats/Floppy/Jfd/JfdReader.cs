using System.IO;
using System.Collections.Frozen;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Images.Models.Sectors;
using RetroMediaLab.MediaEngine.Images.Reading.Recognition;
using RetroMediaLab.MediaEngine.Interfaces.Reading;

namespace RetroMediaLab.MediaEngine.Images.Formats.Floppy.Jfd;

/// <summary>Reads JASPP JFDI/gzip images, retaining native tables and protection records.</summary>
public sealed class JfdReader : IMediaImageReader
{
    private readonly Func<string, CancellationToken, Task<byte[]>> read;
    public JfdReader(Func<string, CancellationToken, Task<byte[]>>? read = null) => this.read = read ?? File.ReadAllBytesAsync;
    public IReadOnlySet<string> FormatIds { get; } = new[] { DiskImageFormatIds.Jfd }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    public IReadOnlySet<string> Extensions { get; } = new[] { DiskImageFileExtensions.Jfd }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyList<ReadOnlyMemory<byte>> Signatures => ["JFDI"u8.ToArray()];
    public IReadOnlySet<string> AssociatedFileExtensions => FrozenSet<string>.Empty;
    public IReadOnlySet<MediaKind> MediaKinds { get; } = new[] { MediaKind.Floppy }.ToFrozenSet();
    public IReadOnlySet<MediaRepresentationKind> RepresentationKinds { get; } = new[] { MediaRepresentationKind.Sectors }.ToFrozenSet();
    public bool SupportsFormatId(string formatId) => FormatIds.Contains(formatId);
    public async ValueTask<bool> CanReadAsync(MediaRecognitionContext context, CancellationToken cancellationToken)
    {
        if (context.RequestedFormatId is not null && !SupportsFormatId(context.RequestedFormatId)
            || !Extensions.Contains(context.Extension) || context.Length is < 4 or > JfdCodec.MaximumLength) return false;
        var bytes = await read(context.Source.PrimaryPath, cancellationToken).ConfigureAwait(false);
        try { return JfdCodec.Unpack(bytes).AsSpan().StartsWith("JFDI"u8); }
        catch (InvalidDataException) { return false; }
    }
    public async Task<MediaImageDocument> ReadAsync(MediaRecognitionContext context, CancellationToken cancellationToken)
    {
        if (context.Length > JfdCodec.MaximumLength) throw new InvalidDataException("JFD exceeds the supported size.");
        var bytes = await read(context.Source.PrimaryPath, cancellationToken).ConfigureAwait(false);
        return ReadBytes(context.Source, bytes);
    }
    internal static MediaImageDocument ReadBytes(MediaSourceDescriptor source, byte[] bytes)
    {
        var image = JfdCodec.Decode(bytes);
        return MediaImageDocument.CreateUnexplored(source, DiskImageFormatIds.Jfd, MediaKind.Floppy,
            new SectorMediaImageRepresentation(image.Sectors), image.Diagnostics, new Dictionary<string, string>
            {
                // Retain delta history, Obey, timing descriptors and future fields for native export.
                [JfdCodec.NativeKey] = Convert.ToBase64String(image.Bytes),
                ["jfd.gzip"] = (bytes.Length >= 2 && bytes[0] == 0x1f && bytes[1] == 0x8b).ToString()
            });
    }
}
