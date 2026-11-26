using MediaSourceDescriptor = RetroMediaLab.MediaEngine.Contracts.MediaSourceDescriptor;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Images.Reading;
using RetroMediaLab.MediaEngine.Images.Reading.Optical;
using RetroMediaLab.MediaEngine.Images.Writing;

namespace RetroMediaLab.MediaEngine.Images.Conversion.Optical;

/// <summary>Converts supported optical images with the same writers and converters as the main conversion service.</summary>
public sealed class OpticalImageConversionService
{
    private readonly MediaImageReadingService reading;
    private readonly MediaConversionService conversion;

    public OpticalImageConversionService(MediaImageReadingService reading, MediaImageWriterRegistry writers, MediaImageWritingService writing)
        : this(reading, writers, writing, new MediaRepresentationConverterRegistry([new GdRomIsoRepresentationConverter(), new GdRomCueToGdiRepresentationConverter()]))
    {
    }

    internal OpticalImageConversionService(MediaImageReadingService reading, MediaImageWriterRegistry writers,
        MediaImageWritingService writing, MediaRepresentationConverterRegistry converters)
    {
        ArgumentNullException.ThrowIfNull(reading);
        this.reading = reading;
        conversion = new MediaConversionService(converters, writers, writing);
    }

    public IReadOnlyList<MediaConversionDestination> GetAvailableDestinations(MediaImageDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return document.MediaKind == MediaKind.Optical ? conversion.GetAvailableDestinations(document) : [];
    }

    public async Task<IReadOnlyList<string>> ConvertAsync(string sourcePath, string outputPath, string targetFormatId, CancellationToken cancellationToken = default) => await ConvertAsync(sourcePath, outputPath, targetFormatId, null, cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<string>> ConvertAsync(string sourcePath, string outputPath, string targetFormatId, Action<MediaExplorationProgress>? progress, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetFormatId);
        var document = await reading.ReadAsync(new MediaSourceDescriptor(sourcePath, []), cancellationToken).ConfigureAwait(false);
        try
        {
            if (document.MediaKind != MediaKind.Optical)
                throw new NotSupportedException("The source is not an optical media image.");

            var result = await conversion.ConvertAsync(new MediaConversionRequest(document, outputPath, targetFormatId,
                progress: progress), cancellationToken).ConfigureAwait(false);
            return result.ProducedFiles;
        }
        finally
        {
            OpticalDiscSession.ReleaseDocument(document);
        }
    }
}
