using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.App.Views.Controls.Options;

namespace RetroMediaLab.App.Theme.OS.Editions.WindowsXP.Views;

internal partial class ControllersOptionsView : UserControl, IDisposable
{
    private readonly OptionsControllersSection source;
    private readonly ThemeBindingScope bindings = new();
    private readonly bool? previousVisibility;
    private ControllerVisualizer? originalDrawing;
    private bool disposed;
    private WindowsXPResources? resourceScope;

    internal ControllersOptionsView(OptionsControllersSection source, WindowsXPColorScheme scheme = WindowsXPColorScheme.Blue)
    {
        this.source = source;
        previousVisibility = source.PresentationVisibility;
        try
        {
            InitializeComponent();
            resourceScope = new WindowsXPResources(this, scheme);
            Choice(DeviceSelector);
            Choice(ModelSelector);
            foreach (var text in new[] { DetectionStatus, ProductNameText, DeviceIdentityText, StickDeadZoneText,
                TriggerDeadZoneText, OuterDeadZoneText, RumbleStrengthText, TestStatusText, IdentityDetailsText })
                bindings.OneWay(Original<TextBlock>(text.Name), TextBlock.TextProperty, text, TextBlock.TextProperty);
            foreach (var button in new[] { DetectButton, LowFrequencyRumbleButton, HighFrequencyRumbleButton, LeftTriggerRumbleButton, RightTriggerRumbleButton })
                bindings.Action(Original<Button>(button.Name), button, followCaption: true);
            foreach (var slider in new[] { StickDeadZoneSlider, TriggerDeadZoneSlider, OuterDeadZoneSlider, RumbleStrengthSlider }) Range(slider);
            foreach (var list in new[] { AnalogValuesList, ControlsGrid, CapabilitiesList })
                bindings.OneWay(Original<ItemsControl>(list.Name), ItemsControl.ItemsSourceProperty, list, ItemsControl.ItemsSourceProperty);
            bindings.OneWay(Original<Grid>(nameof(ModelSelectorPanel)), VisibilityProperty, ModelSelectorPanel, VisibilityProperty);
            bindings.OneWay(Original<Border>(AnalogPanelName), VisibilityProperty, AnalogTab, VisibilityProperty);
            bindings.OneWay(Original<Border>(RumblePanelName), VisibilityProperty, RumbleTab, VisibilityProperty);
            originalDrawing = Original<ControllerVisualizer>(nameof(Visualizer));
            originalDrawing.PresentationChanged += DrawingChanged;
            DrawingChanged(this, EventArgs.Empty);
            source.SetPresentationVisibility(false);
        }
        catch { Dispose(); throw; }
    }

    private const string AnalogPanelName = "AnalogDeadZonePanel";
    private const string RumblePanelName = "RumblePanel";
    private T Original<T>(string name) where T : FrameworkElement => (T)source.FindName(name);

    private void Choice(ComboBox target)
    {
        var original = Original<ComboBox>(target.Name);
        bindings.OneWay(original, ItemsControl.ItemsSourceProperty, target, ItemsControl.ItemsSourceProperty);
        bindings.OneWay(original, ItemsControl.DisplayMemberPathProperty, target, ItemsControl.DisplayMemberPathProperty);
        bindings.TwoWay(original, Selector.SelectedItemProperty, target, Selector.SelectedItemProperty);
        bindings.OneWay(original, IsEnabledProperty, target, IsEnabledProperty);
    }

    private void Range(Slider target)
    {
        var original = Original<Slider>(target.Name);
        foreach (var property in new[] { RangeBase.MinimumProperty, RangeBase.MaximumProperty, Slider.TickFrequencyProperty,
            Slider.IsSnapToTickEnabledProperty, Slider.IsMoveToPointEnabledProperty, IsEnabledProperty })
            bindings.OneWay(original, property, target, property);
        bindings.TwoWay(original, RangeBase.ValueProperty, target, RangeBase.ValueProperty);
    }

    private void DrawingChanged(object? sender, EventArgs args)
    {
        if (disposed || originalDrawing is null) return;
        Visualizer.Model = originalDrawing.Model;
        Visualizer.State = originalDrawing.State;
        Visualizer.VisualState = originalDrawing.VisualState;
        Visualizer.ArtworkProfile = originalDrawing.ArtworkProfile;
        Visualizer.VisualCommandIds = originalDrawing.VisualCommandIds;
    }

    private void PresentationLoaded(object sender, RoutedEventArgs args) { if (!disposed) source.SetPresentationVisibility(IsVisible); }
    private void PresentationUnloaded(object sender, RoutedEventArgs args) { if (!disposed) source.SetPresentationVisibility(false); }
    private void PresentationVisibilityChanged(object sender, DependencyPropertyChangedEventArgs args)
    {
        if (!disposed && IsInitialized) source.SetPresentationVisibility(IsLoaded && IsVisible);
    }
    private void DeadZoneReleased(object sender, MouseButtonEventArgs args) { if (!disposed) source.CommitPresentationDeadZones(); }
    private void DeadZoneFocusLost(object sender, KeyboardFocusChangedEventArgs args) { if (!disposed) source.CommitPresentationDeadZones(); }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try
        {
            Loaded -= PresentationLoaded;
            Unloaded -= PresentationUnloaded;
            IsVisibleChanged -= PresentationVisibilityChanged;
            if (originalDrawing is not null) originalDrawing.PresentationChanged -= DrawingChanged;
            source.SetPresentationVisibility(false);
        }
        finally
        {
            try { bindings.Dispose(); }
            finally
            {
                Content = null;
                try { resourceScope?.Dispose(); }
            finally { resourceScope = null; Resources.Clear(); }
                source.SetPresentationVisibility(previousVisibility);
            }
        }
    }
}
