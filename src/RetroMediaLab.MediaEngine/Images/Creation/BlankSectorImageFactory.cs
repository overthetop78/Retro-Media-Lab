using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Images.Conversion.Fat12;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Adf;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Apple;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.CommodoreDos;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Raw;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.BbcDfs;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.AcornAtom;
using RetroMediaLab.MediaEngine.Images.Models.Sectors;
using RetroMediaLab.MediaEngine.Images.Reading.Reconstruction;
using RetroMediaLab.MediaEngine.Images.Reading.Reconstruction.Sectors;
using RetroMediaLab.MediaEngine.Images.Reading.Reconstruction.Sectors.Apple;
using RetroMediaLab.MediaFileSystems.Formats.Commodore;
using RetroMediaLab.MediaFileSystems.FileSystems.Acorn.BbcDfs;

namespace RetroMediaLab.MediaEngine.Images.Creation;

/// <summary>Construit les images sectorielles vierges avant l'injection de fichiers.</summary>
internal static class BlankSectorImageFactory
{
    public static SectorImage Create(string formatId)
    {
        if (formatId == DiskImageFormatIds.AcornAtomDos)
        {
            var bytes = new byte[AcornAtomDskReader.Capacity];
            var count = AcornAtomDskReader.Cylinders * AcornAtomDskReader.Heads * AcornAtomDskReader.SectorsPerTrack;
            var catalogue = bytes.AsSpan(AcornAtomDskReader.BlockSize, AcornAtomDskReader.BlockSize);
            catalogue[BbcDfsFileSystemLayout.TotalSectorsHighOffset] = (byte)(count >> 8);
            catalogue[BbcDfsFileSystemLayout.TotalSectorsLowOffset] = (byte)count;
            return LinearSectorImageBuilder.Create(bytes, formatId, new LinearSectorImageGeometry(AcornAtomDskReader.BlockSize,
                AcornAtomDskReader.Cylinders, AcornAtomDskReader.Heads, AcornAtomDskReader.SectorsPerTrack));
        }
        if (AcornAdfGeometry.BbcGeometries.FirstOrDefault(geometry => geometry.FormatId == formatId) is { } adfs)
        {
            var bytes = new byte[adfs.Capacity];
            RetroMediaLab.MediaFileSystems.FileSystems.Acorn.Adfs.AcornAdfsFormatter.CreateHeader(adfs.BlockCount).CopyTo(bytes, 0);
            return RegularSectorImageBuilder.Create(bytes, adfs, CancellationToken.None);
        }
        if (BbcDfsGeometry.FindByFormatId(formatId) is { } dfs)
        {
            var bytes = new byte[dfs.Capacity];
            var sectorsPerSide = dfs.Cylinders * BbcDfsGeometry.SectorsPerTrack;
            for (var head = 0; head < dfs.Heads; head++)
            {
                var catalogue = head * BbcDfsGeometry.TrackSize + BbcDfsGeometry.SectorSize;
                bytes[catalogue + 6] = (byte)(sectorsPerSide >> 8);
                bytes[catalogue + 7] = (byte)sectorsPerSide;
            }
            return LinearSectorImageBuilder.Create(bytes, formatId, new LinearSectorImageGeometry(BbcDfsGeometry.SectorSize, dfs.Cylinders, dfs.Heads, BbcDfsGeometry.SectorsPerTrack, SectorNumbering.ZeroBased));
        }
        if (Fat12TargetGeometryCatalog.TryResolve(formatId, out var fat))
            return CreateLinearBlank(formatId, fat.SectorSize, fat.Cylinders, fat.Heads, fat.SectorsPerTrack, SectorNumbering.OneBased);
        if (formatId is DiskImageFormatIds.AmigaDos or DiskImageFormatIds.AmigaDosHighDensity)
        {
            var geometry = formatId == DiskImageFormatIds.AmigaDos ? AmigaAdfGeometry.DoubleDensity : AmigaAdfGeometry.HighDensity;
            return CreateLinearBlank(formatId, geometry.BlockSize, geometry.Cylinders, geometry.Heads, geometry.SectorsPerTrack, SectorNumbering.ZeroBased);
        }
        if (formatId is DiskImageFormatIds.AppleIIAppleDos113 or DiskImageFormatIds.AppleIIAppleDos140)
            return CreateLinearBlank(formatId, AppleIIGeometry.SectorSize, AppleIIGeometry.TrackCount, 1, formatId == DiskImageFormatIds.AppleIIAppleDos113 ? AppleIIGeometry.Dos32SectorsPerTrack : AppleIIGeometry.SectorsPerTrack, SectorNumbering.ZeroBased);
        if (formatId is DiskImageFormatIds.AppleIIProDos140 or DiskImageFormatIds.AppleIIISos)
            return CreateLinearBlank(formatId, AppleIIGeometry.ProDosBlockSize, AppleIIGeometry.TrackCount, 1, AppleIIGeometry.ProDosBlocksPerTrack, SectorNumbering.ZeroBased);
        if (formatId == DiskImageFormatIds.AppleIIProDos800)
        {
            var geometry = MacintoshGcrGeometry.ForHeads(MacintoshGcrGeometry.DoubleSidedHeadCount);
            return MacintoshGcrSectorImageBuilder.Create(new byte[geometry.Capacity], formatId, geometry);
        }
        if (formatId is DiskImageFormatIds.Commodore1541 or DiskImageFormatIds.Commodore1571)
        {
            var tracks = Commodore1541Geometry.StandardTrackCount;
            var sides = formatId == DiskImageFormatIds.Commodore1541 ? 1 : Commodore1571Geometry.SideCount;
            var count = Commodore1541Geometry.BlocksPerSide(tracks) * sides;
            return Commodore1541SectorImageBuilder.Create(new byte[count * Commodore1541Geometry.SectorSize],
                formatId, tracks, sides, count, null,
                (expected, actual) => new InvalidDataException($"Expected {expected} error entries; found {actual}."),
                CancellationToken.None);
        }
        if (formatId == DiskImageFormatIds.Commodore1581)
            return CreateLinearBlank(formatId, Commodore1581Geometry.LogicalBlockSize, Commodore1581Geometry.LogicalCylinderCount, Commodore1581Geometry.LogicalHeadCount, Commodore1581Geometry.LogicalBlocksPerTrack, SectorNumbering.ZeroBased);
        throw new ArgumentException($"Unsupported migration target format '{formatId}'.", nameof(formatId));
    }

    private static SectorImage CreateLinearBlank(string formatId, int blockSize, int cylinders, int heads, int sectorsPerTrack, SectorNumbering numbering)
    {
        var geometry = new LinearSectorImageGeometry(blockSize, cylinders, heads, sectorsPerTrack, numbering);
        return LinearSectorImageBuilder.Create(new byte[geometry.Capacity], formatId, geometry);
    }

}
