using RetroMediaLab.MediaFileSystems.Definitions;
using RetroMediaLab.MediaFileSystems.FileSystems.Amiga;
using RetroMediaLab.MediaFileSystems.FileSystems.Apple.Dos;
using RetroMediaLab.MediaFileSystems.FileSystems.Apple.ProDos;
using RetroMediaLab.MediaFileSystems.FileSystems.Commodore.Dos;
using RetroMediaLab.MediaFileSystems.FileSystems.Fat12;
using RetroMediaLab.MediaFileSystems.Interfaces;

namespace RetroMediaLab.MediaFileSystems.Migration;

/// <summary>Contraintes des systèmes de fichiers cibles, évaluées sur l'image vierge reçue.</summary>
internal static class FileSystemMigrationCapabilityCatalog
{
    public static MigrationTargetCapabilities For(string fileSystemId, IMediaSectorImage image)
    {
        ArgumentNullException.ThrowIfNull(image);
        return fileSystemId switch
        {
            FileSystemIds.AmigaDosFfs or FileSystemIds.AmigaDosOfs =>
                new(fileSystemId, AmigaDosLayout.OrdinaryNameMaximumLength, image.Capacity,
                    true, false, true, true, false, false, "/:", NamePolicy: new AmigaDosNamePolicy(),
                    MaximumVolumeNameLength: AmigaDosLayout.OrdinaryNameMaximumLength,
                    VolumeNamePolicy: new AmigaDosNamePolicy()),
            FileSystemIds.Fat12 =>
                new(fileSystemId, FatDirectoryLayout.NameLength + 1 + FatDirectoryLayout.ExtensionLength,
                    image.Capacity, true, false, true, false, false, false, "\"*+,/:;<=>?[\\]|",
                    NamePolicy: new Fat12ShortNamePolicy(), MaximumVolumeNameLength: FatBootSectorLayout.VolumeLabelLength,
                    VolumeNamePolicy: new Fat12VolumeNamePolicy()),
            FileSystemIds.AppleDos =>
                new(fileSystemId, AppleDosFileSystemLayout.EntryNameLength, Math.Min(image.Capacity, ushort.MaxValue),
                    false, false, false, false, false, false, ",", NamePolicy: new AppleDosNamePolicy(),
                    MaximumVolumeNameLength: AppleDosFileSystemLayout.VolumeNamePrefix.Length + 3,
                    VolumeNamePolicy: new AppleDosVolumeNamePolicy()),
            FileSystemIds.ProDos or FileSystemIds.Sos =>
                new(fileSystemId, ProDosFileSystemLayout.MaximumNameLength,
                    ProDosFileSystemLayout.MaximumFileLength, true, false, true, false, false, false,
                    string.Empty, NamePolicy: new ProDosNamePolicy(),
                    MaximumVolumeNameLength: ProDosFileSystemLayout.MaximumNameLength,
                    VolumeNamePolicy: new ProDosNamePolicy()),
            FileSystemIds.CommodoreDos =>
                new(fileSystemId, CommodoreDosLayout.NameLength,
                    (long)image.BlockCount * CommodoreDosLayout.DataBytesPerSector,
                    false, false, false, false, false, false, string.Empty,
                    NamePolicy: new CommodoreDosNamePolicy(),
                    MaximumVolumeNameLength: CommodoreDosLayout.NameLength,
                    VolumeNamePolicy: new CommodoreDosNamePolicy()),
            _ => throw new ArgumentException($"Unsupported migration file system '{fileSystemId}'.", nameof(fileSystemId))
        };
    }
}
