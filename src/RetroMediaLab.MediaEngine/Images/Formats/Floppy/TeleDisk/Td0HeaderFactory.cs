
using RetroMediaLab.MediaEngine.Images.Models.Sectors;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.BbcDfs;

namespace RetroMediaLab.MediaEngine.Images.Formats.Floppy.TeleDisk;

/// <summary>Construit les métadonnées TeleDisk absentes d'une image sectorielle générique.</summary>
internal static class Td0HeaderFactory
{
    // Sydex TELEDISK 1.05, page 12: source density 2 denotes 500 kbit/s.
    private const byte HighDensityDataRate = 2;
    /// <summary>Crée un en-tête cohérent avec la géométrie de l'image.</summary>
    public static Td0Header Create(SectorImage image) => new(0, 0, Td0Format.DefaultVersion,
        ResolveDataRate(image),
        ResolveDriveType(image), 0, 0, checked((byte)image.Heads));

    private static byte ResolveDataRate(SectorImage image)
    {
        if (image.FormatId.Equals(DiskImageFormatIds.UcsdIbmMfm, StringComparison.OrdinalIgnoreCase))
        {
            var blocks = image.AvailableBlocks;
            if (blocks.FirstOrDefault()?.FormatCode is { } recordedRate
                && blocks.All(block => block.FormatCode == recordedRate && block.RecordedSizeCode.HasValue && block.DiagnosticCode.HasValue))
                return recordedRate;
        }
        // Sydex TELEDISK 1.05, page 12: bit 7 marks single-density FM data.
        if (BbcDfsGeometry.FindByFormatId(image.FormatId) is not null
            || image.FormatId.Equals(DiskImageFormatIds.AcornAtomDos, StringComparison.OrdinalIgnoreCase)) return 0x80;
        return image.FormatId.Equals(DiskImageFormatIds.AcornAdfs1600, StringComparison.OrdinalIgnoreCase)
            ? HighDensityDataRate : Td0Format.DefaultDataRate;
    }

    private static byte ResolveDriveType(SectorImage image)
    {
        if (image.Cylinders <= 40 && image.Heads <= 2) return 1;
        if (image.Capacity <= 737_280) return 3;
        if (image.Capacity <= 1_228_800) return 2;
        return 4;
    }
}
