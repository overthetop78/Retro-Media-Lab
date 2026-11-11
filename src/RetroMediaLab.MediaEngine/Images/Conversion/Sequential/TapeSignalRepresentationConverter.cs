using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Functions;
using RetroMediaLab.MediaEngine.Images.Models.Sequential;
using RetroMediaLab.MediaEngine.Interfaces.Conversion;

namespace RetroMediaLab.MediaEngine.Images.Conversion.Sequential;

/// <summary>Convertit les signaux tape sans exiger un système de fichiers ou un protocole reconnus.</summary>
/// <remarks>Le contenu d'une bande protégée reste un signal. L'analyse des fichiers est une opération séparée.
/// Les documents et leurs sources sont empruntés ; aucun fichier de sortie n'est créé ici.</remarks>
public sealed class TapeSignalRepresentationConverter : IMediaRepresentationConverter
{
    public string Id => "tape-signal";
    public IReadOnlySet<MediaRepresentationKind> SourceRepresentationKinds { get; } = new HashSet<MediaRepresentationKind> { MediaRepresentationKind.Sequential };
    public IReadOnlySet<MediaRepresentationKind> TargetRepresentationKinds { get; } = new HashSet<MediaRepresentationKind> { MediaRepresentationKind.Sequential };
    public bool CanConvert(MediaImageDocument source, string targetFormatId, MediaRepresentationKind targetRepresentationKind) => source.MediaKind == MediaKind.Tape && source.Representation is SequentialMediaImageRepresentation && targetRepresentationKind == MediaRepresentationKind.Sequential
        && (source.FormatId is TapeImageFormatIds.Uef or TapeImageFormatIds.AtomTap && targetFormatId is TapeImageFormatIds.Wav or TapeImageFormatIds.Csw or TapeImageFormatIds.Raw
            || targetFormatId == TapeImageFormatIds.Uef && PcmSampleFunctions.TryGetProfile(source.Metadata, out _, out _, out _, out _));

    public async Task<MediaRepresentationConversionResult> ConvertAsync(MediaImageDocument source, string targetFormatId, MediaRepresentationKind targetRepresentationKind, IReadOnlyDictionary<string, string> options, Action<MediaExplorationProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        if (!CanConvert(source, targetFormatId, targetRepresentationKind)) throw new NotSupportedException("This tape signal conversion is not supported.");
        var descriptor = source.Source with { Parameters = options.Count > 0 ? options : source.Source.Parameters };
        MediaImageDocument document;
        IReadOnlyList<string> losses = [];
        if (targetFormatId == TapeImageFormatIds.Uef)
        {
            var native = await TapePcmFunctions.ChannelsAsync(source, cancellationToken).ConfigureAwait(false);
            var samples = native.Channels[0].Select(value => (short)(Math.Sign(value) * 16000)).ToArray();
            document = TapeSignalFunctions.UefDocument(descriptor, new(samples, native.Rate), cancellationToken);
            losses = ["UEF retains signal timing and polarity rather than the original PCM amplitude."];
        }
        else
        {
            var signal = await TapeSignalFunctions.UefAsync((SequentialMediaImageRepresentation)source.Representation, new Dictionary<long, int>(), cancellationToken).ConfigureAwait(false);
            document = TapePcmFunctions.Document(descriptor, targetFormatId, signal);
            losses = source.FormatId == TapeImageFormatIds.Uef
                ? ["Original UEF chunk structure and non-signal information are not retained by the target signal format."]
                : ["The output signal is reconstructed from Atom file data; TAP contains no recorded pulse timing."];
        }
        return new(document, source.Diagnostics, losses);
    }
}
