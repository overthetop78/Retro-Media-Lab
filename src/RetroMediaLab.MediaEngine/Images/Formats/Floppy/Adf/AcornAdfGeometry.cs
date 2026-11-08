using RetroMediaLab.MediaEngine.Functions;
using RetroMediaLab.MediaEngine.Images.Models.Sectors;
using RetroMediaLab.MediaEngine.Images.Reading.Reconstruction;
using RetroMediaLab.MediaEngine.Constants;

namespace RetroMediaLab.MediaEngine.Images.Formats.Floppy.Adf;

/// <summary>Définit la géométrie de l'image ADF Acorn double densité.</summary>
public static class AcornAdfGeometry
{
    /// <summary>Taille d'un bloc Acorn ADF en octets.</summary>
    public const int BlockSize = 1024;
    /// <summary>Nombre de secteurs par piste.</summary>
    public const int SectorsPerTrack = 5;
    /// <summary>Nombre de secteurs par piste des images Archimedes haute densité.</summary>
    public const int HighDensitySectorsPerTrack = 10;
    /// <summary>Nombre d'octets de rembourrage autorisé après la capacité utile.</summary>
    public const int PaddedTrailingByteCount = DataSizeConstants.BytesPerKibibyte;
    /// <summary>Capacité sectorielle utile en octets.</summary>
    public const int Capacity = BlockSize * DiskGeometryConstants.EightyTrackCylinderCount * DiskGeometryConstants.DoubleSidedHeadCount * SectorsPerTrack;
    /// <summary>Taille stockée de la variante rembourrée.</summary>
    public const int PaddedCapacity = Capacity + PaddedTrailingByteCount;
    /// <summary>Capacité sectorielle utile de l'image Archimedes 1600 Kio.</summary>
    public const int HighDensityCapacity = BlockSize * DiskGeometryConstants.EightyTrackCylinderCount * DiskGeometryConstants.DoubleSidedHeadCount * HighDensitySectorsPerTrack;
    /// <summary>Géométrie utile de 800 Kio, hors rembourrage optionnel.</summary>
    public static RegularSectorGeometry Geometry { get; } = new(DiskImageFormatIds.AcornAdfs800, BlockSize, DiskGeometryConstants.EightyTrackCylinderCount, DiskGeometryConstants.DoubleSidedHeadCount, SectorsPerTrack);
    /// <summary>Le format DOS Plus partage la capacité ADFS D, mais ses identifiants commencent à un.</summary>
    public static RegularSectorGeometry DosPlus800 { get; } = Geometry with { FormatId = DiskImageFormatIds.AcornDosPlus800, FirstSectorNumber = 1 };
    /// <summary>Reconnaît la FAT sans secteur d'amorçage du format DOS Plus 800 Kio.</summary>
    public static bool HasDosPlusHeader(ReadOnlySpan<byte> data) => data.Length >= 3 && data[0] == 0xfd && data[1] == 0xff && data[2] == 0xff;
    /// <summary>Géométrie Archimedes haute densité de 1600 Kio.</summary>
    public static RegularSectorGeometry HighDensityGeometry { get; } = new(DiskImageFormatIds.AcornAdfs1600, BlockSize, DiskGeometryConstants.EightyTrackCylinderCount, DiskGeometryConstants.DoubleSidedHeadCount, HighDensitySectorsPerTrack);
    /// <summary>Géométries BBC ADFS S/M/L, rangées par cylindre, face puis secteur à base zéro.</summary>
    public static IReadOnlyList<RegularSectorGeometry> BbcGeometries { get; } = Array.AsReadOnly<RegularSectorGeometry>([new(DiskImageFormatIds.AcornAdfs160, 256, 40, 1, 16), new(DiskImageFormatIds.AcornAdfs320, 256, 80, 1, 16), new(DiskImageFormatIds.AcornAdfs640, 256, 80, 2, 16)]);
    public static IReadOnlyList<RegularSectorGeometry> All { get; } = Array.AsReadOnly(BbcGeometries.Concat([Geometry, HighDensityGeometry, DosPlus800]).ToArray());
}
