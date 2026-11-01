using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.App.Views.Controls.Options;

namespace RetroMediaLab.App.Theme.OS.Editions.Windows95.Views;

internal partial class HardwareOptionsView : UserControl, IDisposable
{
    private readonly OptionsHardwareSection source;
    private readonly ThemeBindingScope bindings = new();
    private bool disposed;

    internal HardwareOptionsView(OptionsHardwareSection source)
    {
        this.source = source;
        try
        {
            InitializeComponent();
            bindings.Action(source.ScanAction, ScanAction, followCaption: true);
            bindings.Action(source.AddDriveAction, AddAction, followCaption: true);
            bindings.Action(source.DownloadAction, DownloadAction, followCaption: true);
            bindings.OneWay(source.Drives, ItemsControl.ItemsSourceProperty, Drives, ItemsControl.ItemsSourceProperty);
            bindings.TwoWay(source.Drives, Selector.SelectedItemProperty, Drives, Selector.SelectedItemProperty);
            bindings.TwoWay(source.GwPath, TextBox.TextProperty, ToolPath, TextBox.TextProperty);
            bindings.OneWay(source.GwPath, IsEnabledProperty, ToolPath, IsEnabledProperty);
            bindings.OneWay(source.HostToolsState, TextBlock.TextProperty, State, TextBlock.TextProperty);
            foreach (var property in new[] { RangeBase.ValueProperty, RangeBase.MinimumProperty, RangeBase.MaximumProperty,
                ProgressBar.IsIndeterminateProperty, VisibilityProperty })
                bindings.OneWay(source.DownloadProgress, property, Progress, property);
        }
        catch { Dispose(); throw; }
    }

    private void SaveDriveClick(object sender, RoutedEventArgs args) { if (!disposed) source.RequestSaveDrive(sender, args); }
    private void ForgetDriveClick(object sender, RoutedEventArgs args) { if (!disposed) source.RequestForgetDrive(sender, args); }
    private void PathEditingFinished(object sender, KeyboardFocusChangedEventArgs args) { if (!disposed) source.RequestPathEditingFinished(sender, args); }
    private void BrowseClick(object sender, RoutedEventArgs args) { if (!disposed) source.RequestBrowseTool(); }
    private void DetectClick(object sender, RoutedEventArgs args) { if (!disposed) source.RequestDetectTool(); }
    private void CheckClick(object sender, RoutedEventArgs args) { if (!disposed) source.RequestCheckTool(); }
    private void RollbackClick(object sender, RoutedEventArgs args) { if (!disposed) source.RequestRollbackTool(); }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try { bindings.Dispose(); }
        finally
        {
            if (Drives is not null) Drives.ItemsSource = null;
            if (ToolPath is not null) ToolPath.LostKeyboardFocus -= PathEditingFinished;
            Content = null;
            Resources.Clear();
        }
    }
}
