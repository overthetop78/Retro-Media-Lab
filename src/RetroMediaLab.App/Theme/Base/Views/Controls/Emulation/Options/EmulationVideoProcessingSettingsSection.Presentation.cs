using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using RetroMediaLab.Emulation;
using RetroMediaLab.Emulation.Dictionaries;
using RetroMediaLab.App.Constants.Views.Emulation;
using RetroMediaLab.App.Localization.Extensions;

namespace RetroMediaLab.App.Views.Controls.Emulation.Options;

internal sealed partial class EmulationVideoProcessingSettingsSection
{
    internal sealed record PresentationPage(string Id, string Label, FrameworkElement Content);
    internal event EventHandler? PresentationChanged;
    internal IReadOnlyList<PresentationPage> PresentationPages => Content is TabControl tabs
        ? tabs.Items.Cast<TabItem>().Select(tab => new PresentationPage((string)tab.Tag, (string)tab.Header, (FrameworkElement)tab.Content)).ToArray()
        : [];
    internal FrameworkElement? PresentationShaderLoading => _shaderLoadingIndicator;
    internal void BeginPresentationSliderEdit(Slider slider)
    {
        _activeSliderEdit = slider;
        _sliderSavePending = false;
    }
    internal void EndPresentationSliderEdit(Slider slider) => CompleteSliderEdit(slider);

    // Enumerate functional leaves only. Their layout, dimensions and brushes stay with Base.
    internal static IEnumerable<FrameworkElement> PresentationLeaves(DependencyObject root,
        IReadOnlyDictionary<FrameworkElement, EmulationSettingsField> moduleFields)
    {
        if (root is FrameworkElement endpoint && (moduleFields.ContainsKey(endpoint) || endpoint is ComboBox or Slider or CheckBox or TextBox or Button))
        {
            yield return endpoint;
            yield break;
        }
        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
            foreach (var leaf in PresentationLeaves(child, moduleFields)) yield return leaf;
    }

    internal static string PresentationLabel(FrameworkElement endpoint)
    {
        var id = AutomationProperties.GetAutomationId(endpoint);
        if (EmulationVideoProcessingCatalog.ParameterResourceKeys.TryGetValue(id, out var resource))
            return LocExtension.Get(resource);
        if (id.EndsWith(EmulationVideoSettingsLayoutConstants.EnabledAutomationIdSuffix, StringComparison.Ordinal)
            && EmulationVideoProcessingCatalog.ParameterResourceKeys.TryGetValue(
                id[..^EmulationVideoSettingsLayoutConstants.EnabledAutomationIdSuffix.Length], out resource))
            return LocExtension.Get(resource);
        if (endpoint is ContentControl { Content: string caption } && !string.IsNullOrWhiteSpace(caption)) return caption;
        for (var parent = LogicalTreeHelper.GetParent(endpoint); parent is not null; parent = LogicalTreeHelper.GetParent(parent))
        {
            var label = LogicalTreeHelper.GetChildren(parent).OfType<TextBlock>().FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(label?.Text)) return label.Text;
        }
        throw new InvalidOperationException(AutomationProperties.GetAutomationId(endpoint));
    }
}
