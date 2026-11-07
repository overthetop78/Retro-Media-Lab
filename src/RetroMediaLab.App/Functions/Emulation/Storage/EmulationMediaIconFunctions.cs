using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RetroMediaLab.App.Constants.Controls.Visual;
using RetroMediaLab.App.Rendering.Media;
using RetroMediaLab.Emulation;
using RetroMediaLab.MediaEngine.Constants;

namespace RetroMediaLab.App.Functions.Emulation.Storage;

/// <summary>Présente les supports du lecteur configuré avec les visuels média communs existants.</summary>
internal static class EmulationMediaIconFunctions
{
    internal static EmulationMediaDevice WithConfiguredModel(EmulationMediaDevice device,
        IReadOnlyList<EmulationStorageDeviceSettings>? settings)
    {
        var configured = settings?.FirstOrDefault(item => item.Slot == device.Slot)?.Floppy;
        var model = device.FloppyOptions?.Models.FirstOrDefault(item => item.Value == configured?.Model);
        if (model is null && device.FloppyOptions?.Models.Count == 1) model = device.FloppyOptions.Models[0];
        return device with { FloppyModel = model ?? device.FloppyModel };
    }

    internal static string Select(EmulationMediaDevice device) => device.MediaType switch
    {
        EmulationMediaType.Floppy => device.FloppyModel?.DiameterInches switch
        {
            3m => MediaIconIds.FloppyThreeInch,
            3.5m => MediaIconIds.FloppyThreeAndHalfDouble,
            5.25m => MediaIconIds.FloppyFiveAndQuarter,
            8m => MediaIconIds.FloppyEightInch,
            _ => MediaIconIds.Floppy
        },
        EmulationMediaType.Tape => MediaIconIds.TapeData,
        EmulationMediaType.HardDisk => MediaIconIds.HardDisk,
        EmulationMediaType.CompactDisc => MediaIconIds.Optical,
        EmulationMediaType.Cartridge => MediaIconIds.Cartridge,
        _ => MediaIconIds.File
    };

    internal static FrameworkElement Create(string iconId, double size)
    {
        var visual = MediaIconVisualConstants.For(iconId);
        ImageSource source;
        if (visual.ImageFile is { } file) source = MediaIconImageCache.Load(file);
        else
        {
            var drawing = new GeometryDrawing((Brush)new BrushConverter().ConvertFromString(visual.Foreground)!,
                null, Geometry.Parse(visual.Geometry));
            drawing.Freeze();
            var image = new DrawingImage(drawing); image.Freeze(); source = image;
        }
        return new Image { Source = source, Width = size, Height = size, Stretch = Stretch.Uniform,
            VerticalAlignment = VerticalAlignment.Center };
    }
}
