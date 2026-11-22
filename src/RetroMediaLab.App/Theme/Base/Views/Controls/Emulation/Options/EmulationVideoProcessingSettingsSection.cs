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
    private EmulationVideoProcessingConfiguration _configuration = new();
    private FrameworkElement? _displaySettings;
    private FrameworkElement? _rendererChoice;
    private FrameworkElement? _nativeSettings;
    private string? _emulatorName;
    private bool _loading;
    private Slider? _activeSliderEdit;
    private bool _sliderSavePending;
    private bool _shaderLoading;
    private FrameworkElement? _shaderLoadingIndicator;
    private string _selectedSettingsTab = EmulationVideoSettingsLayoutConstants.DisplayTabId;

    internal EmulationVideoProcessingSettingsSection()
    {
        RebuildContent();
    }

    internal event EventHandler? ConfigurationChanged;
    internal event EventHandler? ConfigurationSaveRequested;

    internal EmulationVideoProcessingConfiguration Configuration => _configuration;

    internal void SetShaderLoading(bool isLoading)
    {
        _shaderLoading = isLoading;
        if (_shaderLoadingIndicator is not null)
            _shaderLoadingIndicator.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
    }

    internal void SetConfiguration(EmulationVideoProcessingConfiguration? configuration)
    {
        _configuration = EmulationVideoProcessingConfigurationFunctions.Normalize(configuration);
        RebuildContent();
    }

    internal void SetConfiguration(EmulationVideoProcessingConfiguration? configuration, FrameworkElement? displaySettings, FrameworkElement? rendererChoice,
        FrameworkElement? nativeSettings = null, string? emulatorName = null)
    {
        _configuration = EmulationVideoProcessingConfigurationFunctions.Normalize(configuration);
        _displaySettings = displaySettings;
        _rendererChoice = rendererChoice;
        _nativeSettings = nativeSettings;
        _emulatorName = emulatorName;
        RebuildContent();
    }

    internal void RefreshLocalizedContent() => RebuildContent();

    private void RebuildContent()
    {
        var offsets = (Content as TabControl)?.Items.Cast<TabItem>().Where(item => item.Content is ScrollViewer).ToDictionary(item => (string)item.Tag, item => ((ScrollViewer)item.Content).VerticalOffset, StringComparer.Ordinal)
            ?? new Dictionary<string, double>(StringComparer.Ordinal);
        _loading = true;
        try
        {
            if (_displaySettings is not null)
                EmulationSettingsLayout.DetachReusableElement(_displaySettings);
            if (_rendererChoice is not null)
                EmulationSettingsLayout.DetachReusableElement(_rendererChoice);
            if (_nativeSettings is not null)
                EmulationSettingsLayout.DetachReusableElement(_nativeSettings);
            var tabs = new TabControl { Margin = new Thickness(12) };
            tabs.Items.Add(Tab(EmulationVideoSettingsLayoutConstants.DisplayTabId,
                EmulationResourceKeys.VideoTabDisplay,
                CreateDisplayAndRendering(),
                EmulationVideoSettingsLayoutConstants.DisplayTabContentMaximumWidth,
                compactFields: false, frameContent: false));
            tabs.Items.Add(Tab(EmulationVideoSettingsLayoutConstants.ImageTabId, EmulationResourceKeys.VideoTabImage,
                CreateImageTab(), double.PositiveInfinity,
                compactFields: false, frameContent: false));
            if (_nativeSettings is not null)
            {
                var emulator = Tab(nameof(_nativeSettings), "Emulation.Core.Emulator", _nativeSettings,
                    EmulationVideoSettingsLayoutConstants.DisplayTabContentMaximumWidth, compactFields: false, frameContent: false);
                if (!string.IsNullOrWhiteSpace(_emulatorName)) emulator.Header = _emulatorName;
                tabs.Items.Add(emulator);
            }
            tabs.SelectedItem = tabs.Items.Cast<TabItem>().FirstOrDefault(item => string.Equals((string)item.Tag, _selectedSettingsTab, StringComparison.Ordinal)) ?? tabs.Items[0];
            tabs.SelectionChanged += (_, args) =>
            {
                if (!ReferenceEquals(args.Source, tabs) || tabs.SelectedItem is not TabItem selected)
                    return;
                _selectedSettingsTab = (string)selected.Tag;
            };
            Content = tabs;
            TrackSliderEdits(tabs);
            tabs.Loaded += (_, _) =>
            {
                foreach (var item in tabs.Items.Cast<TabItem>())
                    if (item.Content is ScrollViewer scroller
                        && offsets.TryGetValue((string)item.Tag, out var offset))
                        scroller.ScrollToVerticalOffset(offset);
            };
        }
        finally
        {
            _loading = false;
        }
        PresentationChanged?.Invoke(this, EventArgs.Empty);
    }

}
