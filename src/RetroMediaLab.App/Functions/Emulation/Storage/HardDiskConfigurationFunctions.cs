using RetroMediaLab.App.Constants.Emulation;
using RetroMediaLab.App.Contracts.Storage;
using RetroMediaLab.App.Functions.Storage;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Services.Storage;
using RetroMediaLab.Emulation.HardDisks;
using System.IO;

namespace RetroMediaLab.App.Functions.Emulation.Storage;

internal static class HardDiskConfigurationFunctions
{
    internal enum ExistingImageError { None, Missing, InvalidFormat, InvalidSize }
    internal enum NewImageError { None, InvalidFormat, AlreadyExists, MissingSize, UnsupportedCapacity, InvalidGeometry }

    internal static HardDiskImageFormat? FindExistingFormat(
        IReadOnlyList<HardDiskImageFormat> formats, string? path, bool allowExtensionFallback = false)
    {
        if (!allowExtensionFallback && !File.Exists(path)) return null;
        var extension = Path.GetExtension(path);
        var candidates = formats.Where(format => string.Equals(format.Extension, extension, StringComparison.OrdinalIgnoreCase));
        var image = new FileInfo(path!);

        return candidates.FirstOrDefault(format => HardDiskImageValidation.IsSupportedCapacity(image.Length, format))
            ?? (allowExtensionFallback ? candidates.First() : null);
    }

    internal static (ExistingImageError Error, string? Path, HardDiskImageFormat? Format) ValidateExistingImage(
        IReadOnlyList<HardDiskImageFormat> formats, string path, int? fixedSize)
    {
        if (!File.Exists(path)) return (ExistingImageError.Missing, null, null);
        var absolutePath = Path.GetFullPath(path);
        var format = fixedSize is null
            ? FindExistingFormat(formats, absolutePath)
            : formats.FirstOrDefault(candidate => string.Equals(candidate.Extension, Path.GetExtension(absolutePath), StringComparison.OrdinalIgnoreCase));
        if (format is null) return (ExistingImageError.InvalidFormat, absolutePath, null);

        if (fixedSize is { } size)
        {
            if (new FileInfo(absolutePath).Length != size)
                return (ExistingImageError.InvalidSize, absolutePath, null);
        }
        else HardDiskImageValidation.ValidateExisting(absolutePath, format);

        return (ExistingImageError.None, absolutePath, format);
    }

    internal static (NewImageError Error, string? Path) CreateImage(
        string folder, string name, string identifier, HardDiskImageFormat format, int? fixedSize, long? byteSize,
        bool preallocate, HardDiskPreparation preparation, (string Cylinders, string Heads, string Sectors, string SectorSize) geometry)
    {
        var (path, alreadyExists) = PrepareImagePath(folder, name, identifier, format.Extension, fixedSize is not null);
        if (path is null) return (NewImageError.InvalidFormat, null);
        if (alreadyExists) return (NewImageError.AlreadyExists, null);
        if (fixedSize is { } size)
        {
            BlankMediaFile.Create(path, size, format.InitializeBlankImage);
            return (NewImageError.None, path);
        }

        if (byteSize is not { } capacity) return (NewImageError.MissingSize, null);
        if (!HardDiskImageValidation.IsSupportedCapacity(capacity, format)) return (NewImageError.UnsupportedCapacity, null);
        DiskImageGeometry? explicitGeometry = null;
        if (format.SupportsExplicitGeometry)
        {
            if (!long.TryParse(geometry.Cylinders, out var cylinders) || !int.TryParse(geometry.Heads, out var heads)
                || !int.TryParse(geometry.Sectors, out var sectors) || !int.TryParse(geometry.SectorSize, out var sectorSize))
                return (NewImageError.InvalidGeometry, null);
            explicitGeometry = new(cylinders, heads, sectors, sectorSize);
        }

        HardDiskImageCreation.Create(path, capacity, format, format.SupportsFixedAllocation && preallocate, preparation, explicitGeometry);
        return (NewImageError.None, path);
    }

    private static (string? Path, bool AlreadyExists) PrepareImagePath(string folder, string name, string identifier, string extension, bool fixedSize)
    {
        var fileName = Path.GetFileName(name.Trim());
        if (string.IsNullOrWhiteSpace(fileName)) fileName = identifier.TrimEnd(':');
        if (fixedSize) fileName += extension;
        if (string.IsNullOrEmpty(Path.GetExtension(fileName))) fileName += extension;
        if (!string.Equals(Path.GetExtension(fileName), extension, StringComparison.OrdinalIgnoreCase)) return (null, false);
        StoragePaths.EnsureDirectory(folder);
        var path = Path.Combine(folder, fileName);
        return (path, File.Exists(path));
    }

    internal static HardDiskCapacityChoice[] CapacityChoices(HardDiskImageFormat format) =>
        format.AllowedCapacitiesBytes is { } allowed
            ? allowed.Distinct().Order().Where(size => HardDiskImageValidation.IsSupportedCapacity(size, format)).Select(size => new HardDiskCapacityChoice(size, StorageSizeFormatter.FormatCapacity(size))).ToArray()
            : EmulationControlDefaults.HardDiskCapacityMebibytes.Select(size => size * EmulationControlDefaults.BytesPerMebibyte)
                .Append(format.DefaultBytes).Distinct().Order().Where(size => HardDiskImageValidation.IsSupportedCapacity(size, format))
                .Select(size => new HardDiskCapacityChoice(size, StorageSizeFormatter.FormatCapacity(size)))
                .Append(new HardDiskCapacityChoice(null, LocExtension.Get("Emulation.Storage.Geometry.CustomSize"))).ToArray();

    internal static (long Cylinders, int Heads, int SectorsPerTrack, int SectorSize) AutomaticGeometry(HardDiskImageFormat format, long byteSize)
    {
        var heads = format.Geometry?.Heads ?? 1;
        var sectors = format.Geometry?.SectorsPerTrack ?? 32;
        var bytesPerSector = format.LogicalSectorSizes.Contains(EmulationControlDefaults.HardDiskBytesPerSector)
            ? EmulationControlDefaults.HardDiskBytesPerSector : format.LogicalSectorSizes.Min();
        var cylinders = Math.Max(1, (long)Math.Ceiling(byteSize / (double)(heads * sectors * bytesPerSector)));
        return (cylinders, heads, sectors, bytesPerSector);
    }

    internal static bool TryGeometrySize(string cylindersText, string headsText, string sectorsText, string sectorBytesText, out long byteSize)
    {
        if (long.TryParse(cylindersText, out var cylinders) && cylinders > 0 &&
            long.TryParse(headsText, out var heads) && heads > 0 &&
            long.TryParse(sectorsText, out var sectors) && sectors > 0 &&
            long.TryParse(sectorBytesText, out var bytesPerSector) && bytesPerSector > 0)
        {
            try
            {
                byteSize = checked(cylinders * heads * sectors * bytesPerSector);
                return byteSize > 0;
            }
            catch (OverflowException) { }
        }
        byteSize = 0;
        return false;
    }

    internal static bool TryCustomSize(string text, bool gibibytes, out long byteSize)
    {
        if (double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.CurrentCulture, out var custom) && custom > 0)
        {
            var multiplier = gibibytes ? 1024d * 1024d * 1024d : 1024d * 1024d;
            if (custom <= long.MaxValue / multiplier)
            {
                byteSize = (long)Math.Round(custom * multiplier);
                return byteSize > 0;
            }
        }
        byteSize = 0;
        return false;
    }
}
