using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.App.Views.Controls.Options;

namespace RetroMediaLab.App.Theme.OS.Editions.Windows95.Views;

internal partial class EnginesOptionsView : UserControl, IDisposable
{
    private readonly ThemeBindingScope bindings = new();
    private bool disposed;

    internal EnginesOptionsView(OptionsEnginesSection source)
    {
        try
        {
            InitializeComponent();
            Choice(source.PhysicalRead, PhysicalRead);
            Choice(source.PhysicalWrite, PhysicalWrite);
            Choice(source.Conversion, Conversion);
            Choice(source.ExplorerRead, ExplorerRead);
        }
        catch { Dispose(); throw; }
    }

    private void Choice(ComboBox original, ComboBox target)
    {
        foreach (var originalItem in original.Items.Cast<ComboBoxItem>())
        {
            var item = new ComboBoxItem();
            bindings.OneWay(originalItem, ContentControl.ContentProperty, item, ContentControl.ContentProperty);
            bindings.OneWay(originalItem, IsEnabledProperty, item, IsEnabledProperty);
            bindings.OneWay(originalItem, VisibilityProperty, item, VisibilityProperty);
            target.Items.Add(item);
        }
        bindings.TwoWay(original, Selector.SelectedIndexProperty, target, Selector.SelectedIndexProperty);
        bindings.OneWay(original, IsEnabledProperty, target, IsEnabledProperty);
        bindings.OneWay(original, VisibilityProperty, target, VisibilityProperty);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try { bindings.Dispose(); }
        finally
        {
            foreach (var choice in new[] { PhysicalRead, PhysicalWrite, Conversion, ExplorerRead }) choice?.Items.Clear();
            Content = null;
            Resources.Clear();
        }
    }
}
