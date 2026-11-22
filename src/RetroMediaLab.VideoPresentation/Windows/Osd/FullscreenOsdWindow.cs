using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace RetroMediaLab.VideoPresentation.Windows.Osd;

public sealed partial class FullscreenOsdWindow : Window
{
    private const double PanelWidth = 240;
    private readonly Window _owner;
    private readonly FrameworkElement _displayHost;
    private readonly TextBlock _game;
    private readonly Border _mainPanel;
    private readonly FooterHints? _hints;

    public FullscreenOsdWindow(Window owner, FrameworkElement displayHost, string machineName, string? gameName,
        IReadOnlyList<Entry> entries, Action<Exception> showError, string shaderLoadingLabel, FooterHints? hints = null)
    {
        _displayHost = displayHost;
        _hints = hints;
        _owner = owner;
        Owner = owner;
        ShowInTaskbar = false;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        Focusable = true;

        var text = new StackPanel { Margin = new Thickness(20, 26, 16, 16) };
        text.Children.Add(new TextBlock
        {
            Text = machineName,
            FontSize = 21,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brushes.White,
            TextWrapping = TextWrapping.Wrap
        });
        _game = new TextBlock
        {
            FontSize = 15,
            Foreground = new SolidColorBrush(Color.FromRgb(213, 226, 246)),
            Margin = new Thickness(0, 3, 0, 0),
            TextWrapping = TextWrapping.Wrap
        };
        SetGameName(gameName);
        text.Children.Add(_game);
        var main = new DockPanel();
        DockPanel.SetDock(text, Dock.Top);
        main.Children.Add(text);
        var headerSeparator = CreateSeparator(new Thickness(16, 0, 16, 8));
        DockPanel.SetDock(headerSeparator, Dock.Top);
        main.Children.Add(headerSeparator);
        var footer = CreateFooter(hints);
        DockPanel.SetDock(footer, Dock.Bottom);
        main.Children.Add(footer);
        main.Children.Add(new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = _mainRows
        });

        _mainPanel = new Border
        {
            Width = PanelWidth,
            HorizontalAlignment = HorizontalAlignment.Left,
            BorderThickness = new Thickness(0, 0, 1, 0),
            BorderBrush = new SolidColorBrush(Color.FromArgb(166, 84, 159, 211)),
            Background = new LinearGradientBrush(Color.FromArgb(248, 8, 18, 46), Color.FromArgb(242, 11, 27, 52), 90),
            Child = main
        };
        // Nearly transparent pixels still receive mouse input on a layered window.
        _shaderLoading = CreateShaderLoadingBadge(shaderLoadingLabel);
        Content = new Grid { Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)), Children = { _mainPanel, _submenu, _shaderLoading, _editBar } };
        Panel.SetZIndex(_shaderLoading, 1);
        FitDisplayHost();
        InitializeMenu(entries, showError);
        displayHost.SizeChanged += DisplayHostSizeChanged;
        owner.LocationChanged += OwnerLocationChanged;
    }

    public void SetGameName(string? gameName)
    {
        _game.Text = gameName ?? string.Empty;
        _game.Visibility = string.IsNullOrWhiteSpace(gameName) ? Visibility.Collapsed : Visibility.Visible;
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        Keyboard.Focus(this);
    }

    protected override void OnClosed(EventArgs e)
    {
        try
        {
            _displayHost.SizeChanged -= DisplayHostSizeChanged;
            _owner.LocationChanged -= OwnerLocationChanged;
            _closed = true;
            _editing = null;
        }
        finally
        {
            ClearEditBar();
            _submenu.Child = null;
            _levels.Clear();
            _mainRows.Children.Clear();
            Content = null;
            base.OnClosed(e);
        }
    }

    private void DisplayHostSizeChanged(object sender, SizeChangedEventArgs e) => FitDisplayHost();
    private void OwnerLocationChanged(object? sender, EventArgs e) => FitDisplayHost();

    private void FitDisplayHost()
    {
        if (PresentationSource.FromVisual(_displayHost)?.CompositionTarget is not { } target
            || _displayHost.ActualWidth <= 0 || _displayHost.ActualHeight <= 0) return;
        var origin = target.TransformFromDevice.Transform(_displayHost.PointToScreen(new Point()));
        Left = origin.X;
        Top = origin.Y;
        Width = _displayHost.ActualWidth;
        Height = _displayHost.ActualHeight;
        UpdatePanelWidths();
    }
}
