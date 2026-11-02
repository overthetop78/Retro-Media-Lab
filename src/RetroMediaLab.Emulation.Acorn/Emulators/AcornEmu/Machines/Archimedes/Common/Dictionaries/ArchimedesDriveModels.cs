using RetroMediaLab.Emulation.HardDisks;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Functions;
using CommonHost = RetroMediaLab.Emulation.Acorn.Common.Functions.CoreHostFunctions;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.Archimedes.Common.Constants.ArchimedesDriveConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.Archimedes.Common.Dictionaries;

internal static class ArchimedesDriveModels
{
    internal static IReadOnlyList<FloppyDriveModelChoice> Floppies { get; } =
    [
        new(C.Model, string.Empty, BlankImageSize: C.Adfs800Bytes)
        {
            DiameterInches = C.DiameterInches, Tracks = C.Tracks, Sides = C.Sides, DoubleDensity = true,
            AcceptedExtensions = [CommonHost.MediaProcessing.FileExtensions[C.AdfExtensionKey],
                CommonHost.MediaProcessing.FileExtensions[C.AdlExtensionKey], CommonHost.MediaProcessing.FileExtensions[C.ImgExtensionKey]],
            CreationFormats =
            [
                new(C.Adfs800, CommonHost.MediaProcessing.FileExtensions[C.AdfExtensionKey], C.AdfsName, C.Adfs800Bytes)
                    { Tracks = C.Tracks, Sides = C.Sides, DoubleDensity = true },
                new(C.Adfs640, CommonHost.MediaProcessing.FileExtensions[C.AdlExtensionKey], C.AdfsName, C.Adfs640Bytes)
                    { Tracks = C.Tracks, Sides = C.Sides, DoubleDensity = true }
            ]
        }
    ];

    private static readonly DiskImageGeometry DefaultGeometry = new(C.DefaultCylinders, C.DefaultHeads,
        C.DefaultSectorsPerTrack, C.Adfs640SectorBytes);

    internal static IReadOnlyList<HardDiskImageFormat> HardDisks { get; } =
    [
        new(C.St506Format, MediaConstants.WinchesterImageExtension, C.St506Interface,
            C.MaximumHardDiskBytes, DefaultGeometry.Capacity, [HardDiskPreparation.AcornAdfs, HardDiskPreparation.Blank])
        {
            MinimumBytes = C.Adfs640SectorBytes, LogicalSectorSizes = new HashSet<int> { C.Adfs640SectorBytes },
            Geometry = new(C.DefaultHeads, C.DefaultSectorsPerTrack), SupportsExplicitGeometry = true,
            SupportsFixedAllocation = true, InvariantDisplayName = C.St506Interface,
            Operations = DiskFormatOperations.Create | DiskFormatOperations.Read | DiskFormatOperations.Modify,
            ImageCreator = ArchimedesFunctions.CreateHardDisk
        }
    ];

    internal static bool SupportsFloppy(IEmulationDiskImage image) =>
        image.Cylinders is > 0 and <= C.Tracks && image.Heads is > 0 and <= C.Sides
        && image.SectorsPerTrack > 0 && image.SectorSize is >= C.MinimumSectorBytes and <= C.Adfs800SectorBytes
        && System.Numerics.BitOperations.IsPow2(image.SectorSize)
        && (long)image.SectorsPerTrack * image.SectorSize <= C.MaximumTrackBytes
        && image.SectorCount == image.Cylinders * image.Heads * image.SectorsPerTrack;

    internal static bool SupportsHardDisk(IEmulationDiskImage image) => image.FormatId == MediaConstants.WinchesterFormatId
        && image.Cylinders is > 0 and <= C.MaximumCylinders && image.Heads is > 0 and <= C.MaximumHeads
        && image.SectorsPerTrack is > 0 and <= C.MaximumSectorsPerTrack && image.SectorSize == C.Adfs640SectorBytes
        && image.SectorCount == image.Cylinders * image.Heads * image.SectorsPerTrack;
}
