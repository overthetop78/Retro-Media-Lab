using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using RetroMediaLab.App.Contracts.Emulation.Network;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.Emulation;
using C = RetroMediaLab.App.Constants.Emulation.EmulationNetworkLayoutConstants;

namespace RetroMediaLab.App.Theme.OS.Editions.WindowsXP.Views;

/// <summary>Observation only; network routing remains in Emulation and its modules.</summary>
internal sealed class NetworkView : DockPanel, IDisposable
{
    private readonly ItemsControl rows = new();
    private bool subscribed;
    private bool disposed;
    private int refreshPending;
    private WindowsXPResources? resourceScope;
    private const double CellInset = 5;
    private const double RowHeight = 22;
    private static readonly (string Resource, string Property, double Width)[] Columns =
    [
        (C.RunningResource, nameof(EmulationNetworkRow.IsRunning), C.RunningWidth),
        (C.MachineResource, nameof(EmulationNetworkRow.MachineId), C.MachineWidth),
        (C.InterfaceResource, nameof(EmulationNetworkRow.Interface), C.InterfaceWidth),
        (C.NetworkResource, nameof(EmulationNetworkRow.Network), C.NetworkWidth),
        (C.ProtocolResource, nameof(EmulationNetworkRow.Protocol), C.ProtocolWidth),
        (C.FormatResource, nameof(EmulationNetworkRow.TransferFormat), C.FormatWidth),
        (C.TransportResource, nameof(EmulationNetworkRow.Transport), C.TransportWidth),
        (C.IpResource, nameof(EmulationNetworkRow.Ip), C.IpWidth),
        (C.PortResource, nameof(EmulationNetworkRow.Port), C.PortWidth),
        (C.AddressResource, nameof(EmulationNetworkRow.Address), C.NativeAddressWidth),
        (C.RelayResource, nameof(EmulationNetworkRow.Relay), C.RelayWidth),
        (C.EndpointResource, nameof(EmulationNetworkRow.HostEndpoint), C.EndpointWidth)
    ];

    internal NetworkView(WindowsXPColorScheme scheme = WindowsXPColorScheme.Blue)
    {
        try
        {
            Margin = new Thickness(8);
            resourceScope = new WindowsXPResources(this, scheme);
            SetResourceReference(BackgroundProperty, WindowsXPResources.FaceResource);
            var description = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8) };
            description.SetBinding(TextBlock.TextProperty, LocExtension.CreateBinding(C.DescriptionResource));
            SetDock(description, Dock.Top);
            Children.Add(description);
            var table = new DockPanel { Width = Columns.Sum(column => column.Width) };
            var headings = new StackPanel { Orientation = Orientation.Horizontal };
            foreach (var column in Columns)
            {
                var heading = new TextBlock { Padding = new Thickness(CellInset), TextWrapping = TextWrapping.Wrap };
                heading.SetBinding(TextBlock.TextProperty, LocExtension.CreateBinding(column.Resource));
                headings.Children.Add(new GridViewColumnHeader
                {
                    Width = column.Width,
                    Content = heading,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    IsHitTestVisible = false,
                    Focusable = false
                });
            }
            SetDock(headings, Dock.Top);
            table.Children.Add(headings);
            var line = new FrameworkElementFactory(typeof(StackPanel));
            line.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
            line.SetValue(MinHeightProperty, RowHeight);
            foreach (var column in Columns)
            {
                var container = new FrameworkElementFactory(typeof(Border));
                container.SetValue(WidthProperty, column.Width);
                container.SetValue(Border.PaddingProperty, new Thickness(CellInset));
                var value = new FrameworkElementFactory(column.Property == nameof(EmulationNetworkRow.IsRunning) ? typeof(CheckBox) : typeof(TextBlock));
                value.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
                if (column.Property == nameof(EmulationNetworkRow.IsRunning))
                {
                    value.SetValue(IsHitTestVisibleProperty, false);
                    value.SetValue(FocusableProperty, false);
                    value.SetBinding(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty, new Binding(column.Property) { Mode = BindingMode.OneWay });
                }
                else
                {
                    value.SetValue(TextBlock.ForegroundProperty, System.Windows.Media.Brushes.Black);
                    value.SetBinding(TextBlock.TextProperty, new Binding(column.Property));
                    value.SetBinding(ToolTipProperty, new Binding(column.Property));
                    value.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
                }
                container.AppendChild(value);
                line.AppendChild(container);
            }
            rows.Foreground = System.Windows.Media.Brushes.Black;
            rows.Background = System.Windows.Media.Brushes.White;
            rows.ItemTemplate = new DataTemplate(typeof(EmulationNetworkRow)) { VisualTree = line };
            table.Children.Add(rows);
            Children.Add(new ScrollViewer { Content = table, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
            Loaded += ViewLoaded;
            Unloaded += ViewUnloaded;
            Refresh();
        }
        catch { Dispose(); throw; }
    }

    private void Refresh() => rows.ItemsSource = EmulationSdk.Networks.Endpoints.Select(endpoint => new EmulationNetworkRow(endpoint)).ToArray();
    private void ViewLoaded(object sender, RoutedEventArgs args)
    {
        if (disposed) return;
        if (!subscribed) EmulationSdk.Networks.Changed += Changed;
        subscribed = true;
        Refresh();
    }
    private void ViewUnloaded(object sender, RoutedEventArgs args)
    {
        if (subscribed) EmulationSdk.Networks.Changed -= Changed;
        subscribed = false;
        rows.ItemsSource = null;
    }
    private void Changed(object? sender, EventArgs args)
    {
        if (disposed || Dispatcher.HasShutdownStarted || Interlocked.Exchange(ref refreshPending, C.RefreshPending) != C.NoRefreshPending) return;
        _ = Dispatcher.InvokeAsync(() =>
        {
            try { if (!disposed && subscribed) Refresh(); }
            finally { Interlocked.Exchange(ref refreshPending, C.NoRefreshPending); }
        });
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try { ViewUnloaded(this, new RoutedEventArgs()); }
        finally
        {
            Loaded -= ViewLoaded;
            Unloaded -= ViewUnloaded;
            Children.Clear();
            try { resourceScope?.Dispose(); }
            finally { resourceScope = null; Resources.Clear(); }
        }
    }
}
