using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using GWGUI.App.Contracts.Emulation.Network;
using GWGUI.App.Localization.Extensions;
using GWGUI.Emulation;
using C = GWGUI.App.Constants.Emulation.EmulationNetworkLayoutConstants;

namespace GWGUI.App.Theme.OS.Editions.Windows95.Views;

/// <summary>Observation only; network routing remains in Emulation and its modules.</summary>
internal sealed class NetworkView : DockPanel, IDisposable
{
    private readonly ItemsControl rows = new();
    private bool subscribed;
    private bool disposed;
    private int refreshPending;
    private const string ControlsUri = "/gwgui.app;component/Theme/OS/Editions/Windows95/Styles/Windows95Controls.xaml";
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

    internal NetworkView()
    {
        try
        {
            Margin = new Thickness(8);
            System.Windows.Documents.TextElement.SetFontFamily(this, new System.Windows.Media.FontFamily(Windows95DesktopMetrics.FontFamily));
            System.Windows.Documents.TextElement.SetFontSize(this, Windows95Metrics.FontSize);
            Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(ControlsUri, UriKind.Relative) });
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
                headings.Children.Add(new Border { Width = column.Width, BorderBrush = System.Windows.Media.Brushes.White,
                    BorderThickness = new Thickness(1, 1, 0, 0), Child = new Border
                    { BorderBrush = System.Windows.Media.Brushes.Gray, BorderThickness = new Thickness(0, 0, 1, 1), Child = heading } });
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
            Resources.Clear();
        }
    }
}
