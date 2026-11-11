using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Images.Models.Sectors;

namespace RetroMediaLab.MediaEngine.Images.Conversion.Apple;

/// <summary>Validates semantic compatibility between Apple sector sources and target formats.</summary>
internal static class AppleSectorConversionValidationFunctions
{
    internal static bool CanWrite(SectorImage image, string targetFormatId)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetFormatId);
        var valid = targetFormatId.Equals(DiskImageFormatIds.AppleIIAppleDos113, StringComparison.OrdinalIgnoreCase) &&
                    image.FormatId.Equals(DiskImageFormatIds.AppleIIDos32, StringComparison.OrdinalIgnoreCase)
            || targetFormatId.Equals(DiskImageFormatIds.AppleIIAppleDos140, StringComparison.OrdinalIgnoreCase) &&
                    (image.FormatId.Equals(DiskImageFormatIds.AppleIIDos33, StringComparison.OrdinalIgnoreCase) ||
                     image.FormatId.Equals(DiskImageFormatIds.AppleIIAppleDos140, StringComparison.OrdinalIgnoreCase))
            || targetFormatId.Equals(DiskImageFormatIds.AppleIIProDos140, StringComparison.OrdinalIgnoreCase) &&
                    (image.FormatId.Equals(DiskImageFormatIds.AppleIIProDos, StringComparison.OrdinalIgnoreCase) ||
                     image.FormatId.Equals(DiskImageFormatIds.AppleIIProDos140, StringComparison.OrdinalIgnoreCase))
            || targetFormatId.Equals(DiskImageFormatIds.AppleIIProDos800, StringComparison.OrdinalIgnoreCase) &&
                    (image.FormatId.Equals(DiskImageFormatIds.AppleIIProDos, StringComparison.OrdinalIgnoreCase) ||
                     image.FormatId.Equals(DiskImageFormatIds.AppleIIProDos800, StringComparison.OrdinalIgnoreCase))
            || targetFormatId.Equals(DiskImageFormatIds.AppleIIISos, StringComparison.OrdinalIgnoreCase) &&
                    image.FormatId.Equals(DiskImageFormatIds.AppleIIISos, StringComparison.OrdinalIgnoreCase);
        return valid;
    }

    public static void Validate(SectorImage image, string targetFormatId)
    {
        if (!CanWrite(image, targetFormatId))
            throw new InvalidDataException($"Apple source format '{image.FormatId}' cannot be written as '{targetFormatId}' without changing its file system.");
    }
}
