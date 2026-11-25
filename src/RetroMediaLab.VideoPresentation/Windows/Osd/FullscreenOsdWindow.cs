using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace RetroMediaLab.VideoPresentation.Windows.Osd;

public sealed partial class FullscreenOsdWindow : Window
{
    private const double PanelWidth = 240;
    private readonly Window _owner;
    private readonly FrameworkElement _displayHost;
    private readonly TextBlock _game;
    private readonly Border _mainPanel;
    private readonly Dictionary<Brush, Brush> _translucentBackgrounds = new();
    private int _transparency;
    private readonly Brush _mainBackground = new LinearGradientBrush(
        Color.FromRgb(10, 19, 38), Color.FromRgb(15, 35, 54), 115);
    private readonly FooterHints? _hints;
    private readonly DispatcherTimer _idleTimer = new(DispatcherPriority.Background)
    {
        Interval = TimeSpan.FromSeconds(15)
    };

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
            Background = _mainBackground,
            Child = main
        };
        // Nearly transparent pixels still receive mouse input on a layered window.
        _shaderLoading = CreateShaderLoadingBadge(shaderLoadingLabel);
        Content = new Grid { Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)), Children = { _mainPanel, _submenu, _shaderLoading, _editBar, _centerPanel } };
        Panel.SetZIndex(_shaderLoading, 1);
        FitDisplayHost();
        InitializeMenu(entries, showError);
        displayHost.SizeChanged += DisplayHostSizeChanged;
        owner.LocationChanged += OwnerLocationChanged;
        _idleTimer.Tick += IdleTimerElapsed;
        PreviewMouseMove += OsdPointerActivity;
        PreviewMouseDown += OsdPointerActivity;
        PreviewMouseUp += OsdPointerActivity;
        PreviewMouseWheel += OsdPointerActivity;
    }

    public void SetTransparency(int percent)
    {
        _transparency = Math.Clamp(percent, 0, 100);
        _translucentBackgrounds.Clear();
        _mainPanel.Background = TranslucentBackground(_mainBackground);
        _centerPanel.Background = TranslucentBackground(_mainBackground);
        ApplyTransparency(_shaderLoading, _transparency);
        if (_levels.Count > 1 && Current.Panel is { } submenu)
            submenu.Background = TranslucentBackground(Current.ActiveSectionBackground() ?? DefaultSubmenuBackground);
        _editBar.Background = TranslucentBackground(Current.ActiveSectionBackground() ?? DefaultEditorBackground);
    }

    private Brush TranslucentBackground(Brush source)
    {
        if (!_translucentBackgrounds.TryGetValue(source, out var background))
        {
            background = source.Clone();
            background.Opacity = 1 - _transparency / 100d;
            background.Freeze();
            _translucentBackgrounds.Add(source, background);
        }
        return background;
    }

    internal static void ApplyTransparency(Border panel, int percent)
    {
        var background = panel.Background.Clone();
        background.Opacity = 1 - Math.Clamp(percent, 0, 100) / 100d;
        panel.Background = background;
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

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        RestartIdleTimer();
    }

    private void RestartIdleTimer()
    {
        if (_closed) return;
        _idleTimer.Stop();
        _idleTimer.Start();
    }

    public bool IsPointerOverControls => _mainPanel.IsMouseOver || _submenu.IsMouseOver || _editBar.IsMouseOver
        || _centerPanel.IsMouseOver || _centerPanel.IsMouseCaptureWithin || _editSlider?.IsMouseCaptureWithin == true;

    private void OsdPointerActivity(object sender, MouseEventArgs e)
    {
        if (IsPointerOverControls) RestartIdleTimer();
    }

    private void IdleTimerElapsed(object? sender, EventArgs e)
    {
        if (_editSlider?.IsMouseCaptureWithin == true || _centerPanel.IsMouseCaptureWithin) return;
        _idleTimer.Stop();
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        try
        {
            _idleTimer.Stop();
            _idleTimer.Tick -= IdleTimerElapsed;
            PreviewMouseMove -= OsdPointerActivity;
            PreviewMouseDown -= OsdPointerActivity;
            PreviewMouseUp -= OsdPointerActivity;
            PreviewMouseWheel -= OsdPointerActivity;
            _displayHost.SizeChanged -= DisplayHostSizeChanged;
            _owner.LocationChanged -= OwnerLocationChanged;
            _closed = true;
            _editing = null;
        }
        finally
        {
            ClearEditBar();
            ClearCenterPanel();
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
        FitCenterPanel();
    }
}
