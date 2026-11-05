using RetroMediaLab.Emulation.HardDisks;
using RetroMediaLab.Emulation.Interfaces;
using RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMicro.Functions;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.StorageConstants;
using W = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.WinchesterModelConstants;
using P = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000ParameterConstants;
using F = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000FormatConstants;
using RetroMediaLab.Emulation.Acorn.Common.Constants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Dictionaries;

/// <summary>Format Winchester du BBC : géométrie explicite, paramètres DSC et préparation ADFS par le SDK.</summary>
internal static class HardDiskFormats
{
    internal static bool Supports(IEmulationDiskImage image)
    {
        if (image.FormatId == MediaConstants.WinchesterPhysicalFormatId)
            return image.Cylinders is >= P.MinimumCylinders and <= P.MaximumCylinders
                && image.Heads is >= P.MinimumHeads and <= P.MaximumHeads
                && image.SectorSize is 0 or P.MinimumBlockSize or P.MediumBlockSize or P.MaximumBlockSize;
        return image.FormatId == C.HardDiskFormat
            && image.Cylinders is >= P.MinimumCylinders and <= P.MaximumCylinders
            && image.Heads is >= P.MinimumHeads and <= P.MaximumHeads
            && image.SectorSize == P.MinimumBlockSize
            && image.SectorsPerTrack is F.SmallBlockSequentialSectors or F.SmallBlockInterleavedSectors
            && All.Any(format => HardDiskImageValidation.IsSupportedCapacity(image.SectorCount * image.SectorSize, format));
    }

    internal static IReadOnlyList<HardDiskImageFormat> All { get; } = Array.AsReadOnly<HardDiskImageFormat>(
    [
        Create(W.Model110, W.Model110Name, new(W.Cylinders110, W.Heads110, F.SmallBlockInterleavedSectors, P.MinimumBlockSize)),
        Create(W.Model130, W.Model130Name, new(W.Cylinders130, W.Heads130, F.SmallBlockInterleavedSectors, P.MinimumBlockSize)),
        Create(W.Custom, W.CustomName, new(W.Cylinders110, W.Heads110, F.SmallBlockInterleavedSectors, P.MinimumBlockSize), custom: true),
        Create(W.PhysicalModel110, W.Model110Name, new(W.Cylinders110, W.Heads110, F.SmallBlockInterleavedSectors, P.MinimumBlockSize), physical: true),
        Create(W.PhysicalModel130, W.Model130Name, new(W.Cylinders130, W.Heads130, F.SmallBlockInterleavedSectors, P.MinimumBlockSize), physical: true),
        Create(W.PhysicalCustom, W.CustomName, new(W.Cylinders110, W.Heads110, F.SmallBlockInterleavedSectors, P.MinimumBlockSize), custom: true, physical: true)
    ]);

    private static HardDiskImageFormat Create(string id, string name, DiskImageGeometry geometry, bool custom = false, bool physical = false) =>
        new(id, physical ? MediaConstants.WinchesterPhysicalImageExtension : MediaConstants.WinchesterImageExtension,
            C.ScsiDisplay, custom ? W.MaximumBytes : geometry.Capacity, geometry.Capacity,
            [HardDiskPreparation.AcornAdfs, HardDiskPreparation.Blank])
        {
            MinimumBytes = custom ? geometry.SectorsPerTrack * geometry.SectorSize : geometry.Capacity,
            AllowedCapacitiesBytes = custom ? null : [geometry.Capacity],
            LogicalSectorSizes = new HashSet<int> { geometry.SectorSize },
            Geometry = new DiskChsGeometry(geometry.Heads, geometry.SectorsPerTrack),
            FixedGeometry = custom ? null : geometry,
            SupportsExplicitGeometry = true,
            SupportsFixedAllocation = true,
            InvariantDisplayName = name,
            Operations = DiskFormatOperations.Create | DiskFormatOperations.Read | DiskFormatOperations.Modify,
            ImageCreator = physical ? AcornDiskFunctions.CreatePhysicalWinchester : AcornDiskFunctions.CreateWinchester
        };
}
