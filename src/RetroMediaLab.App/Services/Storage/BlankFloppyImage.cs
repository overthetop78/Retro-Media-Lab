using System.IO;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.Emulation;
using RetroMediaLab.Emulation.Contracts;

namespace RetroMediaLab.App.Services.Storage;

internal static class BlankFloppyImage
{
    internal static Task CreateAsync(string path, FloppyCreationFormatChoice? format) =>
        format is null ? throw new InvalidOperationException(LocExtension.Get("Emulation.Hdd.InvalidFormat"))
            : EmulationSdk.MediaProcessing.CreateBlankDiskAsync(path, format.FormatId);

    internal static async Task CreateForDriveAsync(string path, FloppyDriveModelChoice? model)
    {
        if (model?.BlankFormats is { Count: > 0 } formats)
        {
            var extension = Path.GetExtension(path);
            var choice = formats.FirstOrDefault(format => format.Key.Equals(extension, StringComparison.OrdinalIgnoreCase));
            if (choice.Value is null) throw new ArgumentException(LocExtension.Get("Emulation.Hdd.InvalidFormat"));
            await EmulationSdk.MediaProcessing.CreateBlankDiskAsync(path, choice.Value);
        }
        else BlankMediaFile.Create(path, Math.Max(0, model?.BlankImageSize ?? 0));
    }
}
