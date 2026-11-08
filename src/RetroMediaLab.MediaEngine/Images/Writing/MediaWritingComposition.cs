using RetroMediaLab.MediaEngine.Images.Formats;
using RetroMediaLab.MediaEngine.Images.Formats.Tape.Csw;
using RetroMediaLab.MediaEngine.Images.Formats.Tape.Raw;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.AcornAtom;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Apridisk;
using RetroMediaLab.MediaEngine.Images.Reading.Reconstruction;
using RetroMediaLab.MediaEngine.Images.Formats.Cartridge.AmstradCpr;
using RetroMediaLab.MediaEngine.Images.Formats.Cartridge.AmstradRom;
using RetroMediaLab.MediaEngine.Images.Formats.Cartridge.Console;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.FamicomFds;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Images.Conversion;
using RetroMediaLab.MediaEngine.Images.Conversion.Acorn;
using RetroMediaLab.MediaEngine.Images.Conversion.Apple;
using RetroMediaLab.MediaEngine.Images.Conversion.Atari;
using RetroMediaLab.MediaEngine.Images.Conversion.Commodore;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Adf;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Apple;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Atr;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.BbcDfs;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.CommodoreDos;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.CpcDsk;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.D81;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.DiskCopy;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Hfe;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.ImageDisk;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Msa;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Raw;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Rx02;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Scp;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.St;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.TeleDisk;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.TwoImg;
using RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Raw;
using RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Chd;
using RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Qcow2;
using RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Vdi;
using RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Vhd;
using RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Vhdx;
using RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Vmdk;
using RetroMediaLab.MediaEngine.Images.Formats.Optical.BinCue;
using RetroMediaLab.MediaEngine.Images.Formats.Optical.Gdi;
using RetroMediaLab.MediaEngine.Images.Formats.Optical.Iso;
using RetroMediaLab.MediaEngine.Images.Formats.Optical.Xdvdfs;
using RetroMediaLab.MediaEngine.Images.Formats.Optical.WiiU;
using RetroMediaLab.MediaEngine.Images.Formats.Tape.AtariCas;
using RetroMediaLab.MediaEngine.Images.Formats.Tape.CommodoreTap;
using RetroMediaLab.MediaEngine.Images.Formats.Tape.MsxCas;
using RetroMediaLab.MediaEngine.Images.Formats.Tape.Simh;
using RetroMediaLab.MediaEngine.Images.Formats.Tape.SpectrumTap;
using RetroMediaLab.MediaEngine.Images.Formats.Tape.AtomTap;
using RetroMediaLab.MediaEngine.Images.Formats.Tape.Tzx;
using RetroMediaLab.MediaEngine.Images.Formats.Tape.Uef;
using RetroMediaLab.MediaEngine.Images.Formats.Tape.Voc;
using RetroMediaLab.MediaEngine.Images.Formats.Tape.Wav;
using RetroMediaLab.MediaEngine.Interfaces.Writing;
using RetroMediaLab.MediaEngine.Images.Models.Flux;
using RetroMediaLab.MediaEngine.Images.Models.Sectors;
using RetroMediaLab.MediaEngine.Images.Writing;

namespace RetroMediaLab.MediaEngine.Images.Writing;

/// <summary>Provides the registered media image writers and their writing service.</summary>
public sealed class MediaWritingComposition
{
    private static readonly BuiltInImageFormatCatalog Formats = new();

    private MediaWritingComposition(IReadOnlyList<IMediaImageWriter> writers)
    {
        Writers = writers;
        Registry = new MediaImageWriterRegistry(writers);
        WritingService = new MediaImageWritingService(Registry);
    }

    public IReadOnlyList<IMediaImageWriter> Writers { get; }

    public MediaImageWriterRegistry Registry { get; }

    public MediaImageWritingService WritingService { get; }

    public static MediaWritingComposition CreateDefault()
    {
        var acornAdf = new AcornAdfWriter();
        var amstradCpr = new AmstradCprWriter();
        var amstradRom = new AmstradRomWriter();
        var consoleCartridge = new ConsoleCartridgeWriter();
        var nintendoFamicomDisk = new FamicomFdsWriter();
        var amigaAdf = new AmigaAdfWriter();
        var bbcDfs = new BbcDfsImageWriter();
        var ibmRaw = new IbmRawImageWriter();
        var msxRaw = new MsxRawImageWriter();
        var appleRaw = new AppleRawImageWriter();
        var twoImg = new TwoImgWriter();
        var appleNibble = new AppleDiskImageWriter();
        var macintoshRaw = new MacintoshRawImageWriter();
        var diskCopy = new DiskCopyWriter();
        var atariSt = new AtariStWriter(new LinearSectorImageWriter());
        var msa = new MsaWriter();
        var atr = new AtrWriter();
        var d81 = new D81Writer(new LinearSectorImageWriter());
        var commodoreDos = new CommodoreDosContainerWriter();
        var coherent = new CoherentRawImageWriter();
        var amstrad = new CpcDskWriter();
        var epsonRaw = new EpsonQx10RawImageWriter();
        var imd = new ImdWriter();
        var decRx02 = new DecRx02Writer();
        var linear = new LinearSectorImageWriter();
        var apridisk = new ApridiskWriter();
        var definedRaw = new DefinedRawImageWriter();
        var td0 = new Td0Writer();
        var scp = new ScpWriter();
        var hfe = new HfeWriter();
        var hardDiskRaw = new RawHardDiskWriter();
        var acornWinchester = new RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Acorn.AcornWinchesterWriter();
        var hardDiskQcow2 = new Qcow2Writer();
        var hardDiskVhd = new VhdWriter();
        var hardDiskVhdx = new VhdxWriter();
        var hardDiskVdi = new VdiWriter();
        var hardDiskVmdk = new VmdkWriter();
        var hardDiskChd = new ChdHardDiskWriter();
        var opticalIso = new IsoWriter();
        var opticalBinCue = new BinCueWriter();
        var opticalGdi = new GdiWriter();
        var opticalXboxXdvdfs = new XdvdfsWriter();
        var nintendoWiiU = new WiiUWriter();
        var tapeWav = new WavTapeWriter();
        var tapeVoc = new VocWriter();
        var tapeAtariCas = new AtariCasWriter();
        var tapeTzx = new TzxWriter();
        var tapeSpectrumTap = new SpectrumTapWriter();
        var tapeCommodoreTap = new CommodoreTapWriter();
        var tapeMsxCas = new MsxCasWriter();
        var tapeUef = new UefWriter();
        var tapeSimh = new SimhTapeWriter();

        IMediaImageWriter[] writers =
        [
            new SectorMediaImageWriterAdapter("defined-img", DiskDefsFormatReader.RawDefinitions.Keys, [".img"],
                (image, _, format, _) => image.FormatId.Equals(format, StringComparison.OrdinalIgnoreCase),
                async (image, _, path, format, token) =>
                {
                    await definedRaw.WriteAsync(image, path, format, token).ConfigureAwait(false);
                    return [path];
                }),
            amstradCpr,
            amstradRom,
            consoleCartridge,
            nintendoFamicomDisk,
            Sector(MediaImageWriterIds.AcornAtomDsk, [DiskImageFileExtensions.Dsk],
                (format, extension) => format.Equals(DiskImageFormatIds.AcornAtomDos, StringComparison.OrdinalIgnoreCase) && extension.Equals(DiskImageFileExtensions.Dsk, StringComparison.OrdinalIgnoreCase),
                (image, _, path, _, token) => WriteSingleAsync(linear.WriteAsync(image.WithFormatId(DiskImageFormatIds.AcornAtomDos), path,
                    new RegularSectorGeometry(DiskImageFormatIds.AcornAtomDos, AcornAtomDskReader.BlockSize, AcornAtomDskReader.Cylinders, AcornAtomDskReader.Heads, AcornAtomDskReader.SectorsPerTrack), token), path)),
            Sector(MediaImageWriterIds.Apridisk, [DiskImageFileExtensions.Dsk],
                (format, extension) => format.Equals(DiskImageFormatIds.ApricotPcXi315, StringComparison.OrdinalIgnoreCase) && extension.Equals(DiskImageFileExtensions.Dsk, StringComparison.OrdinalIgnoreCase),
                (image, _, path, _, token) => WriteSingleAsync(apridisk.WriteAsync(image.WithFormatId(DiskImageFormatIds.ApricotPcXi315), path, token), path)),
            new SectorMediaImageWriterAdapter(MediaImageWriterIds.AcornAdf, AcornAdfGeometry.All.Select(geometry => geometry.FormatId), [DiskImageFileExtensions.Adf, DiskImageFileExtensions.Ads, DiskImageFileExtensions.Adm, DiskImageFileExtensions.Adl, DiskImageFileExtensions.AcornAdfs640, DiskImageFileExtensions.Img, DiskImageFileExtensions.AcornDosPlus800],
                (image, _, format, extension) => image.FormatId.Equals(format, StringComparison.OrdinalIgnoreCase) && AcornAdfConversionService.CanCreate(format, extension), (image, _, path, _, token) => WriteSingleAsync(acornAdf.WriteAsync(image, path, token), path)),
            Sector(MediaImageWriterIds.AmigaAdf, [DiskImageFileExtensions.Adf], AmigaAdfConversionService.CanCreate, (image, _, path, _, token) => WriteSingleAsync(amigaAdf.WriteAsync(image, path, token), path)),
            Sector(MediaImageWriterIds.BbcDfs, [DiskImageFileExtensions.Ssd, DiskImageFileExtensions.Dsd], BbcDfsConversionService.CanCreate,
                (image, _, path, formatId, token) => WriteSingleAsync(bbcDfs.WriteAsync(image, path, formatId, token), path)),
            Sector(MediaImageWriterIds.IbmRaw, [DiskImageFileExtensions.Ima, DiskImageFileExtensions.Img], IbmRawConversionService.CanCreate,
                (image, _, path, formatId, token) => WriteSingleAsync(ibmRaw.WriteAsync(image, path, formatId, token), path)),
            Sector(MediaImageWriterIds.MsxRaw, [DiskImageFileExtensions.Dsk], MsxRawConversionService.CanCreate, (image, _, path, formatId, token) => WriteSingleAsync(msxRaw.WriteAsync(image, path, formatId, token), path)),
            Sector(MediaImageWriterIds.AppleSector, [DiskImageFileExtensions.TwoMg, DiskImageFileExtensions.D13, DiskImageFileExtensions.Do, DiskImageFileExtensions.Dsk, DiskImageFileExtensions.Po], AppleSectorConversionService.CanCreate,
                (image, _, path, formatId, token) => WriteAppleSectorAsync(appleRaw, twoImg, image, path, formatId, token)),
            Sector(MediaImageWriterIds.AppleNibble, [DiskImageFileExtensions.Nib, DiskImageFileExtensions.Woz], AppleNibbleConversionService.CanCreate,
                (image, _, path, _, token) => WriteSingleAsync(appleNibble.WriteAsync(image, path, token), path)),
            Sector(MediaImageWriterIds.Macintosh, [DiskImageFileExtensions.Img, DiskImageFileExtensions.Image, DiskImageFileExtensions.Dc42], MacintoshConversionService.CanCreate,
                (image, document, path, _, token) => WriteMacintoshAsync(macintoshRaw, diskCopy, image, document, path, token)),
            Sector(MediaImageWriterIds.LisaDiskCopy, [DiskImageFileExtensions.Image, DiskImageFileExtensions.Dc42], LisaConversionService.CanCreate,
                (image, document, path, _, token) => WriteSingleAsync(diskCopy.WriteAsync(image, path, DiskCopyMetadataFunctions.Restore(image, document.Metadata), token), path)),
            Sector(MediaImageWriterIds.AtariSt, [DiskImageFileExtensions.St, DiskImageFileExtensions.Msa], AtariStConversionService.CanCreate,
                (image, document, path, formatId, token) => WriteAtariStAsync(atariSt, msa, image, document, path, formatId, token)),
            Sector(MediaImageWriterIds.AtariAtr, [DiskImageFileExtensions.Atr], AtrConversionService.CanCreate, (image, _, path, formatId, token) => WriteSingleAsync(atr.WriteAsync(image, path, formatId, token), path)),
            Sector(MediaImageWriterIds.CommodoreD81, [DiskImageFileExtensions.D81], D81ConversionService.CanCreate, (image, _, path, _, token) => WriteSingleAsync(d81.WriteAsync(image, path, token), path)),
            Sector(MediaImageWriterIds.CommodoreDos, [DiskImageFileExtensions.D64, DiskImageFileExtensions.D71], CommodoreDosConversionService.CanCreate,
                (image, _, path, formatId, token) => WriteCommodoreDosAsync(commodoreDos, image, path, formatId, token)),
            Sector(MediaImageWriterIds.CoherentRaw, [DiskImageFileExtensions.Bin, DiskImageFileExtensions.Img], CoherentConversionService.CanCreate,
                (image, _, path, _, token) => WriteSingleAsync(coherent.WriteAsync(image, path, token), path)),
            new SectorMediaImageWriterAdapter(MediaImageWriterIds.AmstradDsk,
                FormatIds(AmstradDskConversionService.CanCreate, [DiskImageFileExtensions.Dsk, DiskImageFileExtensions.Edsk]).Append(CpcDskFormat.FormatId),
                [DiskImageFileExtensions.Dsk, DiskImageFileExtensions.Edsk],
                (_, _, format, extension) => AmstradDskConversionService.CanCreate(format, extension)
                    || format.Equals(CpcDskFormat.FormatId, StringComparison.OrdinalIgnoreCase)
                    && (extension.Equals(DiskImageFileExtensions.Dsk, StringComparison.OrdinalIgnoreCase) || extension.Equals(DiskImageFileExtensions.Edsk, StringComparison.OrdinalIgnoreCase)),
                (image, _, path, _, token) => WriteAmstradAsync(amstrad, image, path, token)),
            Sector(MediaImageWriterIds.EpsonQx10, [DiskImageFileExtensions.Img, DiskImageFileExtensions.Imd], EpsonQx10ConversionService.CanCreate,
                (image, document, path, formatId, token) => WriteEpsonAsync(epsonRaw, imd, image, document, path, formatId, token)),
            new SectorMediaImageWriterAdapter(MediaImageWriterIds.ImageDisk, [DiskImageFormatIds.Imd], [DiskImageFileExtensions.Imd],
                (_, document, format, extension) => format.Equals(DiskImageFormatIds.Imd, StringComparison.OrdinalIgnoreCase)
                    && extension.Equals(DiskImageFileExtensions.Imd, StringComparison.OrdinalIgnoreCase)
                    && document.Metadata.ContainsKey(ImdMetadataKeys.Structure),
                (image, document, path, _, token) => WriteSingleAsync(imd.WriteAsync(
                    ImdMetadataFunctions.Restore(image, document.Metadata) ?? throw new NotSupportedException(), path, token), path)),
            Sector(MediaImageWriterIds.DecRx02, [DiskImageFileExtensions.Img], DecRx02ConversionService.CanCreate, (image, _, path, _, token) => WriteSingleAsync(decRx02.WriteAsync(image, path, token), path)),
            Sector(MediaImageWriterIds.Ucsd, [DiskImageFileExtensions.Img, DiskImageFileExtensions.Td0], UcsdImgConversionService.CanCreate, (image, _, path, _, token) => WriteUcsdAsync(linear, td0, image, path, token)),
            Flux(MediaImageWriterIds.Scp, DiskImageFormatIds.RawScp, DiskImageFileExtensions.Scp, (image, metadata, path, token) => WriteSingleAsync(scp.WriteAsync(path, ProtectedTrackScpImageAdapter.Create(image, metadata), token), path)),
            Flux(MediaImageWriterIds.Hfe, DiskImageFormatIds.RawHfe, DiskImageFileExtensions.Hfe, (image, metadata, path, token) => WriteSingleAsync(hfe.WriteAsync(image, metadata, path, token), path)),
            hardDiskRaw,
            acornWinchester,
            new RetroMediaLab.MediaEngine.Images.Formats.HardDisk.MfmEmu.MfmEmuWriter(),
            hardDiskQcow2,
            hardDiskVhd,
            hardDiskVhdx,
            hardDiskVdi,
            hardDiskVmdk,
            hardDiskChd,
            opticalIso,
            opticalBinCue,
            opticalGdi,
            opticalXboxXdvdfs,
            nintendoWiiU,
            tapeWav,
            new CswWriter(),
            new RawTapeWriter(),
            tapeVoc,
            tapeAtariCas,
            tapeTzx,
            tapeSpectrumTap,
            new AtomTapWriter(),
            tapeCommodoreTap,
            tapeMsxCas,
            tapeUef,
            tapeSimh
        ];
        return new MediaWritingComposition(writers);
    }

    private static SectorMediaImageWriterAdapter Sector(
        string id,
        IReadOnlyList<string> extensions,
        Func<string, string, bool> supports,
        Func<SectorImage, MediaImageDocument, string, string, CancellationToken, Task<IReadOnlyList<string>>> write)
        => new(id, FormatIds(supports, extensions), extensions, (_, _, formatId, extension) => supports(formatId, extension), write);

    private static FluxMediaImageWriterAdapter Flux(string id, string formatId, string extension, Func<ProtectedTrackImage, IReadOnlyDictionary<string, string>, string, CancellationToken, Task<IReadOnlyList<string>>> write)
    {
        var formatIds = DiskDefsFormatReader.Definitions.Values
            .Where(definition => definition.Tracks.All(track => track.Codec == "bitcell"))
            .Select(definition => definition.Id).Append(formatId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return new(
            id,
            formatIds,
            [extension],
            (image, targetFormatId, targetExtension) =>
                formatIds.Contains(targetFormatId) &&
                (extension != DiskImageFileExtensions.Hfe || ProtectedTrackHfeImageAdapter.CanRepresentExactly(image)) &&
                targetExtension.Equals(extension, StringComparison.OrdinalIgnoreCase),
            (image, metadata, path, _, token) => write(image, metadata, path, token));
    }

    private static IReadOnlyList<string> FormatIds(Func<string, string, bool> supports, IReadOnlyList<string> extensions) => Formats.Formats.Where(format => extensions.Any(extension => supports(format.Id, extension))).Select(format => format.Id).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    private static async Task<IReadOnlyList<string>> WriteSingleAsync(Task write, string outputPath)
    {
        await write.ConfigureAwait(false);
        return [outputPath];
    }

    private static Task<IReadOnlyList<string>> WriteAppleSectorAsync(AppleRawImageWriter raw, TwoImgWriter twoImg, SectorImage image, string path, string formatId, CancellationToken cancellationToken)
    {
        AppleSectorConversionValidationFunctions.Validate(image, formatId);
        return Path.GetExtension(path).Equals(DiskImageFileExtensions.TwoMg, StringComparison.OrdinalIgnoreCase) ? WriteSingleAsync(twoImg.WriteAsync(image, path, formatId, cancellationToken), path) : WriteSingleAsync(raw.WriteAsync(image, path, formatId, cancellationToken), path);
    }

    private static Task<IReadOnlyList<string>> WriteMacintoshAsync(MacintoshRawImageWriter raw, DiskCopyWriter diskCopy, SectorImage image, MediaImageDocument document, string path, CancellationToken cancellationToken)
        => Path.GetExtension(path).Equals(DiskImageFileExtensions.Img, StringComparison.OrdinalIgnoreCase)
            ? WriteSingleAsync(raw.WriteAsync(image, path, cancellationToken), path)
            : WriteSingleAsync(diskCopy.WriteAsync(image, path, DiskCopyMetadataFunctions.Restore(image, document.Metadata), cancellationToken), path);

    private static Task<IReadOnlyList<string>> WriteAtariStAsync(AtariStWriter st, MsaWriter msa, SectorImage image, MediaImageDocument document, string path, string formatId, CancellationToken cancellationToken)
    {
        var sourceIsScp = Path.GetExtension(document.Source.PrimaryPath).Equals(DiskImageFileExtensions.Scp, StringComparison.OrdinalIgnoreCase);
        var transformed = AtariStGeometryTransformer.Transform(image, formatId, sourceIsScp);
        return Path.GetExtension(path).Equals(DiskImageFileExtensions.Msa, StringComparison.OrdinalIgnoreCase) ? WriteSingleAsync(msa.WriteAsync(transformed, path, cancellationToken), path) : WriteSingleAsync(st.WriteAsync(transformed, path, cancellationToken), path);
    }

    private static Task<IReadOnlyList<string>> WriteCommodoreDosAsync(CommodoreDosContainerWriter writer, SectorImage image, string path, string formatId, CancellationToken cancellationToken)
    {
        if (!image.FormatId.Equals(formatId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Commodore source format '{image.FormatId}' cannot be written as '{formatId}' without a geometry transformation.");
        var errorMapMode = image.AvailableBlocks.All(block => block.DiagnosticCode.HasValue) ? CommodoreDosErrorMapMode.Preserve : CommodoreDosErrorMapMode.None;
        return WriteSingleAsync(writer.WriteAsync(image, path, errorMapMode, cancellationToken), path);
    }

    private static Task<IReadOnlyList<string>> WriteAmstradAsync(CpcDskWriter writer, SectorImage image, string path, CancellationToken cancellationToken)
    {
        var kind = Path.GetExtension(path).Equals(DiskImageFileExtensions.Edsk, StringComparison.OrdinalIgnoreCase) ? CpcDskContainerKind.Extended : CpcDskContainerKind.Standard;
        var container = CpcDskImageBuilder.Build(image, kind);
        if (kind == CpcDskContainerKind.Standard && (container.Tracks.Any(track => !track.IsPresent
            || track.Sectors.Any(sector => sector.Data.Count != (CpcDskLayout.MinimumSectorSize << (sector.SizeCode & CpcDskLayout.SectorSizeCodeMask))))
            || container.Tracks.Select(track => CpcDskLayout.TrackInformationBlockSize + track.Sectors.Sum(sector => sector.Data.Count)).Distinct().Skip(1).Any()))
            container = container with { Kind = CpcDskContainerKind.Extended };
        return WriteSingleAsync(writer.WriteAsync(container, path, cancellationToken), path);
    }

    private static Task<IReadOnlyList<string>> WriteEpsonAsync(EpsonQx10RawImageWriter raw, ImdWriter imd, SectorImage image, MediaImageDocument document, string path, string formatId, CancellationToken cancellationToken)
    {
        if (!image.FormatId.Equals(formatId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Epson source format '{image.FormatId}' does not match '{formatId}'.");
        return Path.GetExtension(path).Equals(DiskImageFileExtensions.Imd, StringComparison.OrdinalIgnoreCase)
            ? WriteSingleAsync(imd.WriteAsync(ImdMetadataFunctions.Restore(image, document.Metadata) ?? ImdImageBuilder.BuildEpson(image), path, cancellationToken), path)
            : WriteSingleAsync(raw.WriteAsync(image, path, formatId, cancellationToken), path);
    }

    private static Task<IReadOnlyList<string>> WriteUcsdAsync(LinearSectorImageWriter linear, Td0Writer td0, SectorImage image, string path, CancellationToken cancellationToken)
    {
        var source = image.WithFormatId(DiskImageFormatIds.UcsdIbmMfm);
        return Path.GetExtension(path).Equals(DiskImageFileExtensions.Td0, StringComparison.OrdinalIgnoreCase) ? WriteSingleAsync(td0.WriteAsync(source, path, cancellationToken), path) : WriteSingleAsync(linear.WriteAsync(source, path, UcsdIbmMfmGeometry.SectorGeometry, cancellationToken), path);
    }
}
