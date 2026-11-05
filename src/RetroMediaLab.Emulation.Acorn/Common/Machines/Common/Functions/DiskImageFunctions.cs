using CommonHost = RetroMediaLab.Emulation.Acorn.Common.Functions.CoreHostFunctions;
using RetroMediaLab.Emulation.HardDisks;

namespace RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Functions;

/// <summary>Accès aux images Acorn, exclusivement par le service média du SDK ; les limites matérielles appartiennent aux cartes.</summary>
internal static class AcornDiskFunctions
{
    /// <summary>Transfère au cœur la possession de l'image ; les formats restent gérés par MediaEngine.</summary>
    internal static IEmulationDiskImage Open(string path, bool writable, string? formatId = null, IEmulationDriveMechanics? mechanics = null) => CommonHost.MediaProcessing.OpenDiskAsync(path, writable, formatId, mechanics: mechanics).GetAwaiter().GetResult();
    internal static EmulationDiskSector Read(IEmulationDiskImage image, long sector) => image.ReadSectorAsync(sector).GetAwaiter().GetResult();
    internal static void Save(IEmulationDiskImage image) => image.FlushAsync().GetAwaiter().GetResult();
    /// <summary>Réutilise la publication du conteneur et la préparation de volume fournies par le SDK.</summary>
    internal static void CreateWinchester(string path, long bytes, bool fixedSize, HardDiskPreparation preparation, DiskImageGeometry? geometry)
        => CreateWinchester(path, bytes, fixedSize, preparation, geometry, MediaConstants.WinchesterFormatId);

    internal static void CreatePhysicalWinchester(string path, long bytes, bool fixedSize, HardDiskPreparation preparation, DiskImageGeometry? geometry)
        => CreateWinchester(path, bytes, fixedSize, preparation, geometry, MediaConstants.WinchesterPhysicalFormatId);

    private static void CreateWinchester(string path, long bytes, bool fixedSize, HardDiskPreparation preparation,
        DiskImageGeometry? geometry, string formatId)
    {
        if (geometry is null || geometry.Capacity != bytes || preparation is not (HardDiskPreparation.Blank or HardDiskPreparation.AcornAdfs)) throw new ArgumentException(ErrorMessages.IncompatibleMedia, nameof(geometry));
        CommonHost.MediaProcessing.CreateHardDiskAsync(path, formatId, geometry, preparation == HardDiskPreparation.AcornAdfs ? MediaConstants.AcornAdfsFileSystemId : null, preallocate: fixedSize).GetAwaiter().GetResult();
    }

}
