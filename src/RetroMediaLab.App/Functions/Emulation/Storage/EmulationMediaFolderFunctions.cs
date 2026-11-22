using RetroMediaLab.Infrastructure.Settings;
using RetroMediaLab.Infrastructure.Settings.Emulation;
using RetroMediaLab.App.Contracts.Emulation.Configurations;
using RetroMediaLab.Emulation;

namespace RetroMediaLab.App.Functions.Emulation.Storage;

internal static class EmulationMediaFolderFunctions
{
    internal static string? InitialMediaDirectory(AppSettings settings, EmulationConfigurationListItem selected, EmulationMediaDevice device)
    {
        var category = MediaFolderCategory(device.MediaType);
        return settings.EmulationMediaFolders.FirstOrDefault(item =>
            string.Equals(item.ModuleId, selected.Module.Id, StringComparison.Ordinal)
            && string.Equals(item.MachineId, selected.Configuration.MachineId, StringComparison.Ordinal)
            && item.Category == category)?.Folder;
    }

    internal static void RememberMediaDirectory(AppSettings settings, EmulationConfigurationListItem selected, EmulationMediaDevice device, string directory)
    {
        var category = MediaFolderCategory(device.MediaType);
        var existing = settings.EmulationMediaFolders.FirstOrDefault(item =>
            string.Equals(item.ModuleId, selected.Module.Id, StringComparison.Ordinal)
            && string.Equals(item.MachineId, selected.Configuration.MachineId, StringComparison.Ordinal)
            && item.Category == category);
        if (existing is null)
        {
            settings.EmulationMediaFolders.Add(new EmulationMediaFolderSettings
            {
                ModuleId = selected.Module.Id,
                MachineId = selected.Configuration.MachineId,
                Category = category,
                Folder = directory
            });
            return;
        }
        existing.Folder = directory;
    }

    private static EmulationMediaFolderCategory MediaFolderCategory(EmulationMediaType mediaType) => mediaType switch
    {
        EmulationMediaType.Floppy => EmulationMediaFolderCategory.Floppy,
        EmulationMediaType.CompactDisc => EmulationMediaFolderCategory.CompactDisc,
        EmulationMediaType.HardDisk => EmulationMediaFolderCategory.HardDisk,
        EmulationMediaType.Cartridge => EmulationMediaFolderCategory.Cartridge,
        EmulationMediaType.Tape => EmulationMediaFolderCategory.Tape,
        _ => throw new ArgumentOutOfRangeException(nameof(mediaType), mediaType, null)
    };
}
