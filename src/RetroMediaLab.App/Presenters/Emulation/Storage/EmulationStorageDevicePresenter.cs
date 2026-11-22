using RetroMediaLab.App.Constants.Controls.Visual;
using RetroMediaLab.App.Constants.Localization;
using RetroMediaLab.App.Contracts.Emulation.Storage;
using RetroMediaLab.App.Functions.Storage;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.Emulation;
using System.IO;

namespace RetroMediaLab.App.Presenters.Emulation.Storage;

internal sealed record EmulationStorageDeviceRow(EmulationStorageDeviceItem Device, string Type, string Support);

internal static class EmulationStorageDevicePresenter
{
    internal static EmulationStorageDeviceRow Present(EmulationStorageDeviceItem device) =>
        new(device, device.TypeDisplayName ?? TypeLabel(device.Type),
            device.SupportDisplayName ?? (string.IsNullOrWhiteSpace(device.SupportPath)
                ? LocExtension.Get(EmulationResourceKeys.NotUsed) : SupportSummary(device.SupportPath)));

    private static string TypeLabel(EmulationMediaType type) => type switch
    {
        EmulationMediaType.Floppy => LocExtension.Get(EmulationResourceKeys.FloppyDevice),
        EmulationMediaType.HardDisk => LocExtension.Get(EmulationResourceKeys.HardDiskDevice),
        EmulationMediaType.CompactDisc => LocExtension.Get(EmulationResourceKeys.CompactDiscDevice),
        EmulationMediaType.MemoryCard => LocExtension.Get(EmulationResourceKeys.MemoryCardDevice),
        EmulationMediaType.Tape => LocExtension.Get(EmulationResourceKeys.TapeDevice),
        EmulationMediaType.Cartridge => LocExtension.Get(EmulationResourceKeys.CartridgeDevice),
        _ => type.ToString()
    };

    private static string SupportSummary(string path)
    {
        var name = Path.GetFileName(path);
        if (!File.Exists(path)) return name;
        var value = StorageSizeFormatter.FormatCapacity(new FileInfo(path).Length);
        return $"{name}{ControlVisualConstants.DetailSeparator}{value}";
    }
}
