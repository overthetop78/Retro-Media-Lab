using RetroMediaLab.App.Constants.Controls.Visual;
using RetroMediaLab.App.Constants.Localization;
using RetroMediaLab.App.Constants.Machine;
using RetroMediaLab.App.Contracts.Machine;
using RetroMediaLab.App.Functions.Emulation.Machine;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.Emulation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using RetroMediaLab.App.Theme.Common;

namespace RetroMediaLab.App.Views.Controls.Emulation.Machine;

/// <summary>The single visual shell used by every emulated machine.</summary>
internal sealed class MachineView : EmulationMachinePresentation
{
    private readonly Dictionary<string, Ellipse> _deviceLeds = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TextBlock> _deviceStatuses = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Dictionary<EmulationTapeCommand, Button>> _tapeButtons = new(StringComparer.Ordinal);
    private readonly HashSet<Button> _blinkingTapeButtons = [];
    private readonly DispatcherTimer _tapeBlinkTimer;
    private bool _tapeBlinkVisible = true;
    private readonly Border _shaderLoadingOverlay;
    private readonly TextBlock _shaderLoadingText;
    private FrameworkElement? _videoView;
    private bool _shaderLoading;

    internal MachineView()
    {
        _tapeBlinkTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(400), DispatcherPriority.Render, TapeBlinkTick, Dispatcher);
        _tapeBlinkTimer.Stop();
        Root = new Grid { Background = Brushes.Transparent };
        Root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        Root.RowDefinitions.Add(new RowDefinition());
        Root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        Toolbar = new DockPanel
        {
            Height = 34,
            LastChildFill = true,
            Margin = new Thickness(0, 0, 0, 2)
        };
        Root.Children.Add(Toolbar);

        VideoHost = new Grid { Background = Brushes.Black };
        _shaderLoadingText = new TextBlock
        {
            Text = LocExtension.Get(EmulationResourceKeys.VideoShaderLoading),
            Foreground = Brushes.White,
            FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        var loadingContent = new StackPanel { Width = 220 };
        loadingContent.Children.Add(_shaderLoadingText);
        loadingContent.Children.Add(new ProgressBar
        {
            IsIndeterminate = true,
            Height = 5,
            Margin = new Thickness(0, 10, 0, 0)
        });
        _shaderLoadingOverlay = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(190, 20, 22, 26)),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(20, 16, 20, 16),
            Child = loadingContent,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false,
            Visibility = Visibility.Collapsed
        };
        Panel.SetZIndex(_shaderLoadingOverlay, 1);
        VideoHost.Children.Add(_shaderLoadingOverlay);
        Screen = new Border
        {
            Background = Brushes.Black,
            Child = VideoHost,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            SnapsToDevicePixels = true
        };
        DisplayHost = new Grid { Background = new SolidColorBrush(Color.FromRgb(43, 46, 50)) };
        DisplayHost.Children.Add(Screen);
        Grid.SetRow(DisplayHost, 1);
        Root.Children.Add(DisplayHost);

        DeviceStrip = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        BottomBar = new Border
        {
            MinHeight = 24,
            BorderThickness = new Thickness(1, 1, 1, 0),
            Child = DeviceStrip,
            Padding = new Thickness(4, 1, 4, 1)
        };
        BottomBar.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");
        Grid.SetRow(BottomBar, 2);
        Root.Children.Add(BottomBar);
        Content = Root;
    }

    internal Grid Root { get; }
    internal override DockPanel Toolbar { get; }
    internal override Grid VideoHost { get; }
    internal override Border Screen { get; }
    internal override Grid DisplayHost { get; }
    internal StackPanel DeviceStrip { get; }
    internal Border BottomBar { get; }
    internal override IReadOnlyDictionary<string, Ellipse> DeviceLeds => _deviceLeds;

    internal override void AttachCommands(MachineCommandPresentation commands)
    {
        var rows = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        AddStatusRow(IconGlyphs.Display, commands.SizeStatus, MachinePresentationConstants.SizeStatusColor);
        AddStatusRow(IconGlyphs.Play, commands.FpsStatus, MachinePresentationConstants.FpsStatusColor);
        AddStatusRow(IconGlyphs.Sync, commands.FrequencyStatus, MachinePresentationConstants.FrequencyStatusColor);
        AddStatusRow(IconGlyphs.Processor, commands.ExecutionStatus, MachinePresentationConstants.ExecutionStatusColor);
        var overlay = new Border
        {
            Width = MachinePresentationConstants.StatusMaximumWidth,
            Height = MachinePresentationConstants.StatusOverlayHeight,
            Margin = new Thickness(MachinePresentationConstants.StatusOverlayMargin),
            Padding = new Thickness(MachinePresentationConstants.StatusOverlayPadding),
            Background = new SolidColorBrush(Color.FromArgb(MachinePresentationConstants.StatusOverlayAlpha, 0, 0, 0)),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            IsHitTestVisible = false,
            Child = rows
        };
        var visibility = new Style(typeof(Border));
        var empty = new DataTrigger { Binding = new Binding(nameof(TextBlock.Text)) { Source = commands.Status }, Value = string.Empty };
        empty.Setters.Add(new Setter(VisibilityProperty, Visibility.Collapsed));
        visibility.Triggers.Add(empty);
        overlay.Style = visibility;
        Panel.SetZIndex(overlay, 1);
        DisplayHost.Children.Add(overlay);

        void AddStatusRow(string glyph, TextBlock source, Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            var row = new DockPanel { Height = MachinePresentationConstants.StatusRowHeight, LastChildFill = true };
            var icon = new TextBlock
            {
                Text = glyph,
                FontFamily = ControlVisualConstants.IconFont,
                Width = MachinePresentationConstants.StatusIconWidth,
                FontSize = MachinePresentationConstants.StatusFontSize,
                Foreground = brush,
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(icon, Dock.Left);
            row.Children.Add(icon);
            var value = new TextBlock
            {
                FontSize = MachinePresentationConstants.StatusFontSize,
                Foreground = brush,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.NoWrap,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            value.SetBinding(TextBlock.TextProperty, new Binding(nameof(TextBlock.Text)) { Source = source });
            row.Children.Add(value);
            rows.Children.Add(row);
        }
    }

    internal static Button CreateCommandButton(string glyph, string tooltip)
    {
        var icon = new TextBlock
        {
            Text = glyph, FontFamily = ControlVisualConstants.IconFont, FontSize = 17,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
        };
        icon.SetBinding(TextBlock.ForegroundProperty, new Binding(nameof(Button.Foreground))
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Button), 1)
        });
        var button = new Button
        {
            Content = icon, ToolTip = tooltip, Width = 28, Height = 28, MinWidth = 0, MinHeight = 0,
            Padding = new Thickness(2), Margin = new Thickness(0, 0, 2, 0)
        };
        button.SetResourceReference(StyleProperty, "StatusIconButton");
        ToolTipService.SetIsEnabled(button, true);
        ToolTipService.SetShowOnDisabled(button, true);
        ToolTipService.SetInitialShowDelay(button, MachinePresentationConstants.TooltipDelayMilliseconds);
        ToolTipService.SetShowDuration(button, MachinePresentationConstants.TooltipDurationMilliseconds);
        return button;
    }

    internal static Border CreateToolbarGroup(params UIElement[] children)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };
        foreach (var child in children) panel.Children.Add(child);
        var border = new Border
        {
            Child = panel, Height = 32, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6),
            Padding = new Thickness(2, 1, 2, 1), Margin = new Thickness(2, 1, 2, 1),
            VerticalAlignment = VerticalAlignment.Center
        };
        border.SetResourceReference(BackgroundProperty, "CardBrush");
        border.SetResourceReference(BorderBrushProperty, "BorderBrush");
        return border;
    }

    internal override void SetVideoView(FrameworkElement view)
    {
        if (_videoView is not null) VideoHost.Children.Remove(_videoView);
        _videoView = view;
        _videoView.Visibility = _shaderLoading ? Visibility.Hidden : Visibility.Visible;
        VideoHost.Children.Insert(0, view);
    }

    internal override FrameworkElement? DetachVideoView()
    {
        var view = _videoView;
        if (view is not null) VideoHost.Children.Remove(view);
        _videoView = null;
        return view;
    }

    internal override void SetShaderLoading(bool isLoading)
    {
        _shaderLoadingText.Text = LocExtension.Get(EmulationResourceKeys.VideoShaderLoading);
        _shaderLoading = isLoading;
        if (_videoView is not null)
            _videoView.Visibility = isLoading ? Visibility.Hidden : Visibility.Visible;
        _shaderLoadingOverlay.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
    }

    internal override void SetDevices(IEnumerable<MachineViewDevice> devices, Action<Exception> showError, Action restoreFocus)
    {
        DeviceStrip.Children.Clear();
        _deviceLeds.Clear();
        _deviceStatuses.Clear();
        _tapeButtons.Clear();
        _blinkingTapeButtons.Clear();
        _tapeBlinkTimer.Stop();
        _tapeBlinkVisible = true;
        foreach (var device in devices)
            DeviceStrip.Children.Add(DeviceItem(device, showError, restoreFocus));
        UpdateTapeBlinkTimer();
    }

    private FrameworkElement DeviceItem(MachineViewDevice device, Action<Exception> showError, Action restoreFocus)
    {
        var root = new StackPanel
        {
            Orientation = Orientation.Vertical,
            VerticalAlignment = VerticalAlignment.Center
        };
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        var led = new Ellipse
        {
            Width = 10,
            Height = 10,
            Fill = device.Present || !device.Removable ? Brushes.ForestGreen : Brushes.Gray,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Tag = device.Key
        };
        _deviceLeds[device.Key] = led;
        var ledHost = new Grid
        {
            Width = 18,
            Height = 20,
            VerticalAlignment = VerticalAlignment.Center
        };
        ledHost.Children.Add(led);
        panel.Children.Add(ledHost);

        var open = new Button
        {
            ToolTip = device.Removable ? LocExtension.Get("Common.Browse") : device.Label,
            Height = 20,
            MinHeight = 0,
            MinWidth = 0,
            Padding = new Thickness(2, 0, 2, 0),
            Margin = new Thickness(0),
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children =
                {
                    new TextBlock
                    {
                        Text = device.Glyph,
                        FontFamily = ControlVisualConstants.IconFont,
                        FontSize = 15,
                        Margin = new Thickness(0, 0, 4, 0)
                    },
                    new TextBlock
                    {
                        Text = device.Label,
                        FontWeight = FontWeights.SemiBold,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            }
        };
        if (device.Removable && device.Insert is not null)
            open.Click += async (_, _) => await RunAsync(device.Insert, showError, restoreFocus);
        panel.Children.Add(open);

        if (device.Status is not null)
        {
            var status = new TextBlock
            {
                Text = device.Status,
                Margin = new Thickness(4, 0, 2, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            _deviceStatuses[device.Key] = status;
            panel.Children.Add(status);
        }

        if (device.Commands is { Count: > 0 })
        {
            var commandRow = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };
            var commands = new Border
            {
                Child = commandRow,
                Margin = new Thickness(18, 2, 2, 3),
                Padding = new Thickness(2),
                CornerRadius = new CornerRadius(6),
                BorderThickness = new Thickness(1)
            };
            commands.SetResourceReference(BackgroundProperty, "ControlBrush");
            commands.SetResourceReference(BorderBrushProperty, "BorderBrush");
            var buttons = new Dictionary<EmulationTapeCommand, Button>();
            foreach (var command in device.Commands)
            {
                var icon = new TextBlock
                {
                    Text = command.Glyph,
                    FontFamily = new FontFamily("Segoe UI Symbol"),
                    FontSize = 15,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                var face = new Border
                {
                    Child = icon,
                    Width = 27,
                    Height = 21,
                    CornerRadius = new CornerRadius(4),
                    BorderThickness = new Thickness(1)
                };
                var button = new Button
                {
                    Content = face,
                    ToolTip = command.Label,
                    IsEnabled = command.IsEnabled,
                    Width = 31,
                    Height = 25,
                    MinWidth = 0,
                    MinHeight = 0,
                    Padding = new Thickness(0),
                    Margin = new Thickness(1, 0, 1, 0)
                };
                button.SetResourceReference(StyleProperty, "StatusIconButton");
                SetTapeButtonVisual(button, command.Command, command.IsSupported, command.IsActive, command.IsBlinking);
                button.Click += async (_, _) => await RunAsync(command.Execute, showError, restoreFocus);
                buttons[command.Command] = button;
                commandRow.Children.Add(button);
            }
            _tapeButtons[device.Key] = buttons;
            root.Children.Add(panel);
            root.Children.Add(commands);
        }
        else root.Children.Add(panel);

        if (device.Removable)
        {
            var eject = new Button
            {
                Content = new TextBlock
                {
                    Text = "\u23CF",
                    FontFamily = new FontFamily("Segoe UI Symbol"),
                    FontSize = 15
                },
                ToolTip = LocExtension.Get("Common.Eject"),
                Width = 22,
                Height = 20,
                MinWidth = 0,
                MinHeight = 0,
                Padding = new Thickness(0),
                Margin = new Thickness(3, 0, 0, 0),
                IsEnabled = device.Present && device.Eject is not null,
                Visibility = device.Eject is null ? Visibility.Hidden : Visibility.Visible
            };
            eject.SetResourceReference(StyleProperty, "StatusIconButton");
            if (device.Eject is not null)
                eject.Click += async (_, _) => await RunAsync(device.Eject, showError, restoreFocus);
            panel.Children.Add(eject);
        }

        return new Border
        {
            Child = root,
            Padding = new Thickness(4, 0, 4, 0),
            Margin = new Thickness(0, 0, 3, 0),
            BorderThickness = new Thickness(0, 0, 1, 0),
            BorderBrush = new SolidColorBrush(Color.FromRgb(215, 222, 231))
        };
    }

    internal override void SetDeviceStatus(string key, string status)
    {
        if (_deviceStatuses.TryGetValue(key, out var text)) text.Text = status;
    }

    internal override void SetTapeTransport(string key, EmulationTapeState state, EmulationTapeCommand? activeOperation, IReadOnlySet<EmulationTapeCommand> supportedCommands, bool powered)
    {
        if (!_tapeButtons.TryGetValue(key, out var buttons)) return;
        foreach (var (command, button) in buttons)
        {
            var supported = supportedCommands.Contains(command);
            var active = TapeTransportPresentationFunctions.IsActive(state, activeOperation, command);
            var blinking = TapeTransportPresentationFunctions.IsBlinking(state, activeOperation, command);
            button.IsEnabled = TapeTransportPresentationFunctions.IsEnabled(powered, supported, state, activeOperation, command);
            SetTapeButtonVisual(button, command, supported, active, blinking);
        }
        UpdateTapeBlinkTimer();
    }

    private void SetTapeButtonVisual(Button button, EmulationTapeCommand command, bool supported, bool active, bool blinking)
    {
        _blinkingTapeButtons.Remove(button);
        var foreground = command switch
        {
            EmulationTapeCommand.Play when active => new SolidColorBrush(Color.FromRgb(0, 200, 83)),
            EmulationTapeCommand.Play when supported => new SolidColorBrush(Color.FromRgb(22, 130, 59)),
            EmulationTapeCommand.Play => new SolidColorBrush(Color.FromRgb(111, 156, 124)),
            EmulationTapeCommand.Record when active => new SolidColorBrush(Color.FromRgb(235, 24, 54)),
            EmulationTapeCommand.Record when supported => new SolidColorBrush(Color.FromRgb(180, 35, 24)),
            EmulationTapeCommand.Record => new SolidColorBrush(Color.FromRgb(185, 120, 120)),
            _ when !supported => Brushes.Gray,
            _ when active => Brushes.DodgerBlue,
            _ => Brushes.DimGray
        };
        button.Foreground = foreground;
        button.Background = Brushes.Transparent;
        button.Opacity = supported ? 1 : command is EmulationTapeCommand.Play or EmulationTapeCommand.Record ? 0.72 : 0.42;
        if (button.Content is Border face && face.Child is TextBlock icon)
        {
            icon.Foreground = foreground;
            face.BorderBrush = foreground;
            face.BorderThickness = active ? new Thickness(2) : new Thickness(1);
            face.Background = active
                ? command switch
                {
                    EmulationTapeCommand.Play => new SolidColorBrush(Color.FromArgb(55, 0, 200, 83)),
                    EmulationTapeCommand.Record => new SolidColorBrush(Color.FromArgb(55, 235, 24, 54)),
                    _ => new SolidColorBrush(Color.FromArgb(45, 30, 144, 255))
                }
                : Brushes.Transparent;
        }
        if (blinking) _blinkingTapeButtons.Add(button);
    }

    private void UpdateTapeBlinkTimer()
    {
        if (_blinkingTapeButtons.Count > 0)
        {
            if (!_tapeBlinkTimer.IsEnabled) _tapeBlinkTimer.Start();
            return;
        }
        _tapeBlinkTimer.Stop();
        _tapeBlinkVisible = true;
    }

    private void TapeBlinkTick(object? sender, EventArgs args)
    {
        _tapeBlinkVisible = !_tapeBlinkVisible;
        foreach (var button in _blinkingTapeButtons)
            button.Opacity = _tapeBlinkVisible ? 1 : 0.25;
    }

    public override void Dispose()
    {
        try { _tapeBlinkTimer.Stop(); }
        finally
        {
            _tapeBlinkTimer.Tick -= TapeBlinkTick;
            _blinkingTapeButtons.Clear();
            _tapeButtons.Clear();
            _deviceStatuses.Clear();
            _deviceLeds.Clear();
            DeviceStrip.Children.Clear();
            Toolbar.Children.Clear();
            VideoHost.Children.Clear();
            _videoView = null;
            Content = null;
        }
    }

    private static async Task RunAsync(Func<Task> action, Action<Exception> showError, Action restoreFocus)
    {
        try { await action(); }
        catch (Exception error) { showError(error); }
        finally { restoreFocus(); }
    }
}
