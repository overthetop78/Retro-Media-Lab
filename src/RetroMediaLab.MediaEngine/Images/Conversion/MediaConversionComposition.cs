using RetroMediaLab.MediaEngine.Images.Formats.Tape;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Images.Reading;

using RetroMediaLab.MediaEngine.Images.Writing;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Scp.Decoding;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Images.Conversion;
using RetroMediaLab.MediaEngine.Images.Conversion.Atari;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Scp.Conversion;
using RetroMediaLab.MediaEngine.Images.Conversion.Optical;
using RetroMediaLab.MediaEngine.Images.Conversion.Sequential;
using RetroMediaLab.MediaEngine.Images.Reading.Decoding.Sequential;
using RetroMediaLab.MediaEngine.Images.Writing.Encoding;
using RetroMediaLab.MediaEngine.Images.Writing.Encoding.Sequential;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Adf;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Atr;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Raw;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Scp;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.St;
using RetroMediaLab.MediaEngine.Interfaces.Conversion;

namespace RetroMediaLab.MediaEngine.Images.Conversion;

/// <summary>Provides the registered in-memory media representation converters.</summary>
public sealed class MediaConversionComposition
{
    private readonly MediaRecognitionComposition recognition;

    internal MediaConversionComposition(MediaRecognitionComposition recognition, ScpSectorDecodingComposition scpDecoding, SequentialMediaComposition sequentialMedia)
    {
        ArgumentNullException.ThrowIfNull(recognition);
        ArgumentNullException.ThrowIfNull(scpDecoding);
        ArgumentNullException.ThrowIfNull(sequentialMedia);
        this.recognition = recognition;
        var sectorFormatIds = new HashSet<string>(recognition.Readers.Where(reader => reader.RepresentationKinds.Contains(MediaRepresentationKind.Sectors)).SelectMany(reader => reader.FormatIds), StringComparer.OrdinalIgnoreCase);
        sectorFormatIds.Add(DiskImageFormatIds.IbmScan);
        sectorFormatIds.UnionWith(RetroMediaLab.MediaEngine.Images.Formats.DiskDefsFormatReader.RawDefinitions.Keys);
        var sectorToFlux = new SectorToFluxRepresentationConverter(new SectorImageScpConversionService(new SectorImageTrackEncoder(), new ScpEncodedTrackFluxService(), new ScpWriter()));
        Converters = [new ScpFluxToSectorRepresentationConverter(scpDecoding.Reader, sectorFormatIds), sectorToFlux,
            new RetroMediaLab.MediaEngine.Images.Conversion.Flux.FluxToHfeRepresentationConverter(), new TapeSignalRepresentationConverter()];
        Registry = new MediaRepresentationConverterRegistry(Converters);
        SequentialDecoders = sequentialMedia.Decoders;
        SequentialEncoders = sequentialMedia.Encoders;
        AmigaAdfRuntimeConverter = new AmigaAdfConversionService(scpDecoding.AmigaReader, new AdfReader(), new AmigaAdfWriter());
        AtariScpRuntimeConverter = new AtariScpRuntimeConversionService(scpDecoding.AtariReader, new AtrWriter(), new AtariStWriter(new LinearSectorImageWriter()));
    }

    public IReadOnlyList<IMediaRepresentationConverter> Converters { get; }

    public MediaRepresentationConverterRegistry Registry { get; }

    public SequentialDecoderRegistry SequentialDecoders { get; }

    public SequentialEncoderRegistry SequentialEncoders { get; }

    public AmigaAdfConversionService AmigaAdfRuntimeConverter { get; }

    public AtariScpRuntimeConversionService AtariScpRuntimeConverter { get; }

    public OpticalImageConversionService CreateOpticalImageConverter(MediaWritingComposition writing)
    {
        ArgumentNullException.ThrowIfNull(writing);
        return new OpticalImageConversionService(recognition.ReadingService, writing.Registry, writing.WritingService);
    }

    public SequentialMediaConversionService CreateSequentialMediaConverter(MediaWritingComposition writing)
    {
        ArgumentNullException.ThrowIfNull(writing);
        return new SequentialMediaConversionService(recognition.ReadingService, SequentialDecoders, SequentialEncoders, writing.Registry, writing.WritingService);
    }
}
