using RetroMediaLab.App.Constants.Localization;
using RetroMediaLab.App.Constants.Controls.Visual;
using RetroMediaLab.App.Constants.Views.Emulation;
using RetroMediaLab.App.Functions.Views.Emulation.Settings;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.Emulation.Constants;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Dictionaries;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.Emulation.Functions;
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace RetroMediaLab.App.Views.Controls.Emulation.Options;

internal sealed partial class EmulationVideoProcessingSettingsSection : UserControl
{
    private FrameworkElement CreateCrtSignalControls()
    {
        var crt = _configuration.Crt;
        var panel = Section(EmulationResourceKeys.VideoTechnologyCrt);
        var signal = new Grid();
        signal.ColumnDefinitions.Add(new ColumnDefinition());
        signal.ColumnDefinitions.Add(new ColumnDefinition());
        var standard = ChoiceField(ParameterKey(EmulationVideoProcessingCatalog.CrtColorStandards),
            EmulationVideoProcessingCatalog.CrtColorStandardsResourceKeys, crt.ColorStandards,
            value => SetCrt(_configuration.Crt with { ColorStandards = value }));
        standard.Margin = new Thickness(0, 0, 6, 0);
        signal.Children.Add(standard);
        var frequency = ChoiceField(ParameterKey(EmulationVideoProcessingCatalog.CrtFieldRate),
            EmulationVideoProcessingCatalog.CrtFieldRateResourceKeys, crt.FieldRate,
            value => SetCrt(_configuration.Crt with { FieldRate = value }));
        frequency.Margin = new Thickness(6, 0, 0, 0);
        Grid.SetColumn(frequency, 1);
        signal.Children.Add(frequency);
        panel.Children.Add(signal);
        var maskRow = new Grid();
        maskRow.ColumnDefinitions.Add(new ColumnDefinition());
        maskRow.ColumnDefinitions.Add(new ColumnDefinition());
        var mask = ChoiceField(ParameterKey(EmulationVideoProcessingCatalog.CrtMask),
            EmulationVideoProcessingCatalog.CrtMaskResourceKeys, crt.Mask, value =>
            {
                SetCrt(_configuration.Crt with { Mask = value });
                RebuildContent();
            }, EmulationVideoProcessingCatalog.CrtMask);
        mask.Margin = new Thickness(0, 0, 6, 0);
        maskRow.Children.Add(mask);
        if (crt.Mask == EmulationCrtMask.Monochrome)
        {
            var phosphor = ChoiceField(ParameterKey(EmulationVideoProcessingCatalog.CrtPhosphorColor),
                EmulationVideoProcessingCatalog.CrtPhosphorColorResourceKeys, crt.PhosphorColor,
                value => SetCrt(_configuration.Crt with { PhosphorColor = value }),
                EmulationVideoProcessingCatalog.CrtPhosphorColor);
            phosphor.Margin = new Thickness(6, 0, 0, 0);
            Grid.SetColumn(phosphor, 1);
            maskRow.Children.Add(phosphor);
        }
        panel.Children.Add(maskRow);
        panel.Children.Add(EmulationSettingsFieldControlFunctions.CreateVideoSlider(
            EmulationVideoProcessingCatalog.CrtBeamWidth, crt.BeamWidth,
            EmulationVideoProcessingLimits.AdjustmentMinimum, EmulationVideoProcessingLimits.AdjustmentMaximum,
            value => SetCrt(_configuration.Crt with { BeamWidth = value }),
            1, 38, new Thickness(0, 2, 0, 8)));
        panel.Children.Add(EmulationSettingsFieldControlFunctions.CreateVideoSlider(
            EmulationVideoProcessingCatalog.CrtCurvature, crt.Curvature,
            EmulationVideoProcessingLimits.CrtCurvatureMinimum, EmulationVideoProcessingLimits.CrtCurvatureMaximum,
            value => SetCrt(_configuration.Crt with { Curvature = value }),
            1, 38, new Thickness(0, 2, 0, 8)));
        panel.Children.Add(ChoiceField(ParameterKey(EmulationVideoProcessingCatalog.CrtConnection),
            EmulationVideoProcessingCatalog.CrtConnectionResourceKeys, crt.Connection, value =>
            {
                SetCrt(_configuration.Crt with { Connection = value });
                RebuildContent();
            }, EmulationVideoProcessingCatalog.CrtConnection));
        if (crt.CanDegradeConnection())
            panel.Children.Add(EmulationSettingsFieldControlFunctions.CreateVideoSlider(
                EmulationVideoProcessingCatalog.CrtConnectionDegradation, crt.ConnectionDegradation,
                0, EmulationVideoProcessingLimits.CrtConnectionDegradationMaximum,
                value => SetCrt(_configuration.Crt with { ConnectionDegradation = value }),
                1, 38, new Thickness(0, 2, 0, 8)));
        if (crt.Connection == EmulationCrtConnection.LegacyMonitor)
            panel.Children.Add(ChoiceField(ParameterKey(EmulationVideoProcessingCatalog.CrtMonitorPalette),
                EmulationVideoProcessingCatalog.CrtMonitorPaletteResourceKeys, crt.MonitorPalette,
                value => SetCrt(_configuration.Crt with { MonitorPalette = value }),
                EmulationVideoProcessingCatalog.CrtMonitorPalette));
        return panel;
    }



    private FrameworkElement CreateVectorPanel() => EmulationVectorSettingsBlock.Create(_configuration.Vector, update => SetVector(update(_configuration.Vector)));

    private FrameworkElement CreateVfdPanel() => EmulationVfdSettingsBlock.Create(_configuration.Vfd, update => SetVfd(update(_configuration.Vfd)), RebuildContent);

    private FrameworkElement CreateLedMatrixPanel() => EmulationLedMatrixSettingsBlock.Create(_configuration.LedMatrix, update => SetLedMatrix(update(_configuration.LedMatrix)));

    private FrameworkElement CreateDotMatrixPanel() => EmulationDotMatrixSettingsBlock.Create(_configuration.DotMatrix, update => SetDotMatrix(update(_configuration.DotMatrix)));

    private FrameworkElement CreateSegmentDisplayPanel() => EmulationSegmentDisplaySettingsBlock.Create(_configuration.SegmentDisplay, update => SetSegmentDisplay(update(_configuration.SegmentDisplay)));

    private FrameworkElement CreateEPaperPanel() => EmulationEPaperSettingsBlock.Create(_configuration.EPaper, update => SetEPaper(update(_configuration.EPaper)), RebuildContent);


}
