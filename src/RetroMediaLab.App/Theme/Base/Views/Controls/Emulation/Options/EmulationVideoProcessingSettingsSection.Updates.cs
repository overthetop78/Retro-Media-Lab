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
    private void SetAdjustments(EmulationVideoScreenType screen,
        Func<EmulationImageAdjustments, EmulationImageAdjustments> update) =>
        Update(configuration => EmulationVideoProcessingConfigurationFunctions.WithImageAdjustments(configuration, screen, update));

    private void SetRestoration(EmulationImageRestorationConfiguration value) => Update(configuration => configuration with { Restoration = value });



    private void SetCrt(EmulationCrtVideoConfiguration value) => Update(configuration => configuration with { Crt = value });




    private void SetVfd(EmulationVfdVideoConfiguration value) => Update(configuration => configuration with { Vfd = value });

    private void SetLedMatrix(EmulationLedMatrixVideoConfiguration value) => UpdateScreenControls(configuration => configuration with { LedMatrix = value });

    private void SetDotMatrix(EmulationDotMatrixVideoConfiguration value) => UpdateScreenControls(configuration => configuration with { DotMatrix = value });

    private void SetSegmentDisplay(EmulationSegmentDisplayVideoConfiguration value) => Update(configuration => configuration with { SegmentDisplay = value });

    private void SetEPaper(EmulationEPaperVideoConfiguration value) => Update(configuration => configuration with { EPaper = value });


    private void UpdateScreenControls(Func<EmulationVideoProcessingConfiguration, EmulationVideoProcessingConfiguration> update)
    {
        var previousControls = EmulationVideoProcessingConfigurationFunctions.AdjustmentControls(_configuration);
        Update(update);
        if (previousControls != EmulationVideoProcessingConfigurationFunctions.AdjustmentControls(_configuration))
            RebuildContent();
    }

    private void Update(Func<EmulationVideoProcessingConfiguration, EmulationVideoProcessingConfiguration> update)
    {
        if (_loading) return;
        _configuration = EmulationVideoProcessingConfigurationFunctions.Normalize(update(_configuration));
        ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        if (_activeSliderEdit is null)
            ConfigurationSaveRequested?.Invoke(this, EventArgs.Empty);
        else
            _sliderSavePending = true;
    }

    private void TrackSliderEdits(DependencyObject root)
    {
        foreach (var slider in Descendants(root).OfType<Slider>())
        {
            slider.PreviewMouseLeftButtonDown += (_, _) =>
            {
                _activeSliderEdit = slider;
                _sliderSavePending = false;
            };
            slider.PreviewMouseLeftButtonUp += (_, _) => CompleteSliderEdit(slider);
            slider.LostMouseCapture += (_, _) => CompleteSliderEdit(slider);
        }
    }

    private void CompleteSliderEdit(Slider slider)
    {
        if (!ReferenceEquals(_activeSliderEdit, slider)) return;
        _activeSliderEdit = null;
        if (!_sliderSavePending) return;
        _sliderSavePending = false;
        ConfigurationSaveRequested?.Invoke(this, EventArgs.Empty);
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
        {
            yield return child;
            foreach (var descendant in Descendants(child))
                yield return descendant;
        }
    }

    private sealed record Choice<T>(T Value, string DisplayName) where T : struct, Enum;
}
