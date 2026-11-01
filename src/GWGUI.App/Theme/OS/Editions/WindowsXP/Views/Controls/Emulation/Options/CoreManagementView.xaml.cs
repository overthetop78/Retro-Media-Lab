using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using GWGUI.App.Theme.Common;
using GWGUI.App.Views.Controls.Emulation.Options;

namespace GWGUI.App.Theme.OS.Editions.WindowsXP.Views;

internal partial class CoreManagementView : UserControl, IDisposable
{
    internal static readonly DependencyProperty StatusIsErrorProperty = DependencyProperty.Register(
        nameof(StatusIsError), typeof(bool), typeof(CoreManagementView), new PropertyMetadata(false));
    public bool StatusIsError => (bool)GetValue(StatusIsErrorProperty);
    private readonly ThemeBindingScope bindings = new();
    private bool disposed;
    private WindowsXPResources? resourceScope;

    internal CoreManagementView(EmulationCoreManagementPanel source, WindowsXPColorScheme scheme = WindowsXPColorScheme.Blue)
    {
        try
        {
            InitializeComponent();
            resourceScope = new WindowsXPResources(this, scheme);
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
            if (Emulators is not null) Emulators.ItemsSource = null;
            if (Versions is not null) Versions.ItemsSource = null;
            Content = null;
            try { resourceScope?.Dispose(); }
            finally { resourceScope = null; Resources.Clear(); }
        }
    }
}
