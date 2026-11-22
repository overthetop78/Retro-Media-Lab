using RetroMediaLab.App.Constants.Localization;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.Emulation.Constants;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Dictionaries;
using System.Windows;
using ImageControls = RetroMediaLab.VideoPresentation.Enums.EmulationImageAdjustmentControls;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace RetroMediaLab.App.Views.Controls.Emulation.Options;

internal static class EmulationImageParametersSettingsBlock
{
    internal static FrameworkElement Create(EmulationImageAdjustments adjustments, ImageControls controls, Action<Func<EmulationImageAdjustments, EmulationImageAdjustments>> changed,
        EmulationImageRestorationConfiguration restoration, bool host, bool supportsDedithering, bool supportsDeinterlacing,
        Action<Func<EmulationImageRestorationConfiguration, EmulationImageRestorationConfiguration>> restorationChanged)
    {
        var block = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
        block.Children.Add(new TextBlock
        {
            Text = LocExtension.Get(EmulationResourceKeys.VideoGeneralSettings),
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 12)
        });
        var sliders = new UniformGrid { Rows = 1, HorizontalAlignment = HorizontalAlignment.Stretch };
        if (controls.HasFlag(ImageControls.Brightness))
            sliders.Children.Add(VerticalSlider(EmulationVideoProcessingCatalog.Brightness, adjustments.Brightness, value => changed(current => current with { Brightness = value })));
        if (controls.HasFlag(ImageControls.Contrast))
            sliders.Children.Add(VerticalSlider(EmulationVideoProcessingCatalog.Contrast, adjustments.Contrast, value => changed(current => current with { Contrast = value })));
        if (controls.HasFlag(ImageControls.Saturation))
            sliders.Children.Add(VerticalSlider(EmulationVideoProcessingCatalog.Saturation, adjustments.Saturation,
                value => changed(current => current with { Saturation = value }), EmulationImageAdjustmentFunctions.FormatSaturation));
        if (controls.HasFlag(ImageControls.Sharpness))
            sliders.Children.Add(VerticalSlider(EmulationVideoProcessingCatalog.Sharpness, adjustments.Sharpness, value => changed(current => current with { Sharpness = value })));
        if (host)
            EmulationImageRestorationSettingsBlock.AddIntensityControls(sliders, restoration, restorationChanged);
        if (sliders.Children.Count > 0) block.Children.Add(sliders);

        var choices = new UniformGrid { Rows = 1, Margin = new Thickness(0, 16, 0, 0) };
        if (controls.HasFlag(ImageControls.Gamma))
            choices.Children.Add(GammaChoice(adjustments.Gamma, value => changed(current => current with { Gamma = value })));
        EmulationImageRestorationSettingsBlock.AddChoices(choices, restoration, supportsDedithering, supportsDeinterlacing, restorationChanged);
        if (choices.Children.Count > 0) block.Children.Add(choices);
        return block;

    }

    private static FrameworkElement GammaChoice(int setting, Action<int> changed)
    {
        var original = LocExtension.Get(EmulationResourceKeys.VideoGammaOriginal);
        var column = new StackPanel { Margin = new Thickness(0, 0, 18, 0) };
        var label = LocExtension.Get(EmulationResourceKeys.VideoParameterGamma);
        column.Children.Add(new TextBlock
        {
            Text = label,
            Margin = new Thickness(0, 0, 0, 6)
        });
        var choice = new ComboBox
        {
            ItemsSource = EmulationImageAdjustmentFunctions.GammaCurves.Select(value => new
            {
                Value = value,
                Label = EmulationImageAdjustmentFunctions.FormatGamma(value, original)
            }),
            DisplayMemberPath = "Label",
            SelectedValuePath = "Value",
            SelectedValue = EmulationImageAdjustmentFunctions.NormalizeGamma(setting)
        };
        AutomationProperties.SetAutomationId(choice, EmulationVideoProcessingCatalog.Gamma);
        AutomationProperties.SetName(choice, label);
        choice.SelectionChanged += (_, _) =>
        {
            if (choice.SelectedValue is int value) changed(value);
        };
        column.Children.Add(choice);
        return column;
    }

    private static FrameworkElement VerticalSlider(string id, int value, Action<int> changed, Func<int, string>? format = null)
    {
        format ??= EmulationImageAdjustmentFunctions.FormatSigned;
        var column = new StackPanel
        {
            Margin = new Thickness(6, 0, 6, 0)
        };
        column.Children.Add(new TextBlock
        {
            Text = LocExtension.Get(EmulationVideoProcessingCatalog.ParameterResourceKeys[id]),
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 40,
            Margin = new Thickness(0, 0, 0, 6)
        });
        var slider = new Slider
        {
            Orientation = Orientation.Vertical,
            Minimum = EmulationVideoProcessingLimits.AdjustmentMinimum,
            Maximum = EmulationVideoProcessingLimits.AdjustmentMaximum,
            Value = value,
            TickFrequency = 1,
            IsSnapToTickEnabled = true,
            Height = 220,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        AutomationProperties.SetAutomationId(slider, id);
        var displayedValue = new TextBlock
        {
            Text = format(value),
            MinWidth = 36,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        };
        slider.ValueChanged += (_, _) =>
        {
            var number = (int)slider.Value;
            displayedValue.Text = format(number);
            changed(number);
        };
        column.Children.Add(slider);
        column.Children.Add(displayedValue);
        return column;
    }
}
