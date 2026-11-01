using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.App.Views.Controls.Emulation.Options;

namespace RetroMediaLab.App.Theme.OS.Editions.Windows95.Views;

internal partial class CoreManagementView : UserControl, IDisposable
{
    internal static readonly DependencyProperty StatusIsErrorProperty = DependencyProperty.Register(
        nameof(StatusIsError), typeof(bool), typeof(CoreManagementView), new PropertyMetadata(false));
    public bool StatusIsError => (bool)GetValue(StatusIsErrorProperty);
    private readonly ThemeBindingScope bindings = new();
    private bool disposed;

    internal CoreManagementView(EmulationCoreManagementPanel source)
    {
        try
        {
            InitializeComponent();
            Choice(source.Emulators, Emulators);
            Choice(source.Versions, Versions);
            foreach (var pair in new[] { (source.Installed, Installed), (source.Description, Description), (source.Status, Status) })
            {
                bindings.OneWay(pair.Item1, TextBlock.TextProperty, pair.Item2, TextBlock.TextProperty);
                bindings.OneWay(pair.Item1, VisibilityProperty, pair.Item2, VisibilityProperty);
            }
            bindings.OneWay(source, EmulationCoreManagementPanel.StatusIsErrorProperty, this, StatusIsErrorProperty);
            bindings.Action(source.Search, Search);
            bindings.Action(source.Download, Download);
            bindings.Action(source.Cancel, Cancel);
            foreach (var property in new[] { RangeBase.MinimumProperty, RangeBase.MaximumProperty, RangeBase.ValueProperty,
                ProgressBar.IsIndeterminateProperty, VisibilityProperty })
                bindings.OneWay(source.Progress, property, Progress, property);
        }
        catch { Dispose(); throw; }
    }

    private void Choice(ComboBox source, ComboBox target)
    {
        bindings.OneWay(source, ItemsControl.DisplayMemberPathProperty, target, ItemsControl.DisplayMemberPathProperty);
        bindings.OneWay(source, Selector.SelectedValuePathProperty, target, Selector.SelectedValuePathProperty);
        bindings.OneWay(source, ItemsControl.ItemsSourceProperty, target, ItemsControl.ItemsSourceProperty);
        bindings.TwoWay(source, Selector.SelectedItemProperty, target, Selector.SelectedItemProperty);
        bindings.OneWay(source, IsEnabledProperty, target, IsEnabledProperty);
        bindings.OneWay(source, VisibilityProperty, target, VisibilityProperty);
        bindings.OneWay(source, ToolTipProperty, target, ToolTipProperty);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try { bindings.Dispose(); }
        finally
        {
            Emulators.ItemsSource = null;
            Versions.ItemsSource = null;
            Content = null;
            Resources.Clear();
        }
    }
}
