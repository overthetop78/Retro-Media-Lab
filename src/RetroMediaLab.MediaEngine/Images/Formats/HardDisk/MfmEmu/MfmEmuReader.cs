using System.Collections.Frozen;
using System.IO;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Images.Reading.Recognition;
using RetroMediaLab.MediaEngine.Images.Reading.Sources;
using RetroMediaLab.MediaEngine.Interfaces.Reading;

namespace RetroMediaLab.MediaEngine.Images.Formats.HardDisk.MfmEmu;

/// <summary>Reconnaît les pistes MFM EMU par signature et conserve leur source bornée.</summary>
public sealed class MfmEmuReader : IMediaImageReader
{
    public IReadOnlySet<string> FormatIds { get; } = new[] { HardDiskImageFormatIds.MfmEmu }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    public IReadOnlySet<string> Extensions { get; } = new[] { DiskImageFileExtensions.Emu }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    public IReadOnlySet<string> AssociatedFileExtensions => FrozenSet<string>.Empty;
    public IReadOnlyList<ReadOnlyMemory<byte>> Signatures { get; } = [MfmEmuFormat.Signature.ToArray()];
    public IReadOnlySet<MediaKind> MediaKinds { get; } = new[] { MediaKind.HardDisk }.ToFrozenSet();
    public IReadOnlySet<MediaRepresentationKind> RepresentationKinds { get; } = new[] { MediaRepresentationKind.Flux }.ToFrozenSet();
    public bool SupportsFormatId(string formatId) => FormatIds.Contains(formatId);

    public async ValueTask<bool> CanReadAsync(MediaRecognitionContext context, CancellationToken cancellationToken)
    {
        if (context.RequestedFormatId is { } requested && !SupportsFormatId(requested)) return false;
        if (context.Length < MfmEmuFormat.FixedHeaderLength) return false;
        var signature = await context.ReadHeaderAsync(MfmEmuFormat.Signature.Length, cancellationToken).ConfigureAwait(false);
        return signature.Span.SequenceEqual(MfmEmuFormat.Signature);
    }

    public async Task<MediaImageDocument> ReadAsync(MediaRecognitionContext context, CancellationToken cancellationToken)
    {
        if (!await CanReadAsync(context, cancellationToken).ConfigureAwait(false)) throw new InvalidDataException(MfmEmuFormat.InvalidHeader);
        var container = await MfmEmuContainer.OpenAsync(new FileRandomAccessData(context.Source.PrimaryPath), cancellationToken).ConfigureAwait(false);
        return MediaImageDocument.CreateUnexplored(context.Source, HardDiskImageFormatIds.MfmEmu, MediaKind.HardDisk,
            new MfmEmuImageRepresentation(container), [], new Dictionary<string, string>());
    }
}
