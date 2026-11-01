using RetroMediaLab.App.Functions.Views.Emulation.Settings;
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using RetroMediaLab.App.Constants.Controls.Visual;
using RetroMediaLab.App.Constants.Localization;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.Emulation.Constants;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Dictionaries;

namespace RetroMediaLab.App.Views.Controls.Emulation.Options;

internal static class EmulationDotMatrixSettingsBlock
{
    internal static FrameworkElement Create(EmulationDotMatrixVideoConfiguration value, Action<Func<EmulationDotMatrixVideoConfiguration, EmulationDotMatrixVideoConfiguration>> changed)
    {
        var groups = new Grid();
        groups.ColumnDefinitions.Add(new ColumnDefinition());
        groups.ColumnDefinitions.Add(new ColumnDefinition());
        groups.ColumnDefinitions.Add(new ColumnDefinition());
        EmulationSettingsLayout.AddVideoProcessingGroup(groups, EmulationResourceKeys.VideoDotMatrixGroupCells, 0,
            EmulationSettingsFieldControlFunctions.CreateVideoChoice(EmulationVideoProcessingCatalog.DotMatrixShape, EmulationVideoProcessingCatalog.DotMatrixShapeResourceKeys, value.Shape, shape => changed(current => current with { Shape = shape })),
            EmulationSettingsFieldControlFunctions.CreateVideoSlider(EmulationVideoProcessingCatalog.DotMatrixCellSize, value.CellSize, 100, number => changed(current => current with { CellSize = number })),
            EmulationSettingsFieldControlFunctions.CreateVideoSlider(EmulationVideoProcessingCatalog.DotMatrixDotSize, value.DotSize, 100, number => changed(current => current with { DotSize = number })),
            EmulationSettingsFieldControlFunctions.CreateVideoSlider(EmulationVideoProcessingCatalog.DotMatrixCellGap, value.CellGap, 100, number => changed(current => current with { CellGap = number })));
        EmulationSettingsLayout.AddVideoProcessingGroup(groups, EmulationResourceKeys.VideoDotMatrixGroupLight, 1,
            EmulationSettingsFieldControlFunctions.CreateVideoChoice(EmulationVideoProcessingCatalog.DotMatrixPalette, EmulationVideoProcessingCatalog.DotMatrixPaletteResourceKeys, value.Palette, palette => changed(current => current with { Palette = palette })),
            EmulationSettingsFieldControlFunctions.CreateVideoSlider(EmulationVideoProcessingCatalog.DotMatrixBrightness, value.Brightness, 100, number => changed(current => current with { Brightness = number })),
            EmulationSettingsFieldControlFunctions.CreateVideoSlider(EmulationVideoProcessingCatalog.DotMatrixContrast, value.Contrast, 100, number => changed(current => current with { Contrast = number })),
            EmulationSettingsFieldControlFunctions.CreateVideoSlider(EmulationVideoProcessingCatalog.DotMatrixHaloIntensity, value.HaloIntensity, 100, number => changed(current => current with { HaloIntensity = number })));
        EmulationSettingsLayout.AddVideoProcessingGroup(groups, EmulationResourceKeys.VideoDotMatrixGroupTemporal, 2,
            EmulationSettingsFieldControlFunctions.CreateVideoSlider(EmulationVideoProcessingCatalog.DotMatrixResponseTime,
                value.ResponseTimeMilliseconds,
                EmulationVideoProcessingLimits.DurationMaximumMilliseconds,
                number => changed(current => current with { ResponseTimeMilliseconds = number })),
            EmulationSettingsFieldControlFunctions.CreateVideoSlider(EmulationVideoProcessingCatalog.DotMatrixPersistence,
                value.PersistenceMilliseconds,
                EmulationVideoProcessingLimits.DurationMaximumMilliseconds,
                number => changed(current => current with { PersistenceMilliseconds = number })));
        return groups;
    }
}
