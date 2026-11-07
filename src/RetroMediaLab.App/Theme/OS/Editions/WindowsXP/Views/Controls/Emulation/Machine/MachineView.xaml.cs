using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using RetroMediaLab.App.Contracts.Machine;
using RetroMediaLab.App.Constants.Localization;
using RetroMediaLab.App.Functions.Emulation.Machine;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.Emulation;

namespace RetroMediaLab.App.Theme.OS.Editions.WindowsXP.Views;

internal partial class MachineView : EmulationMachinePresentation
{
    private ThemeBindingScope? commandBindings;
    private WindowsXPResources? resourceScope;
    private readonly List<Action> detachDevices = [];
    private readonly Dictionary<string, string> deviceLabels = new(StringComparer.Ordinal);
    internal string DeviceLabel(string key) => deviceLabels.GetValueOrDefault(key, key);
    private readonly Dictionary<string, FrameworkElement> devicePanels = new(StringComparer.Ordinal);
    internal event Action<string>? DeviceRequested;
    internal event EventHandler? DevicePanelsChanging;
    internal FrameworkElement? DevicePanel(string key) => devicePanels.GetValueOrDefault(key);

    private readonly Dictionary<string, Ellipse> leds = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (TextBlock Text, MenuItem Menu)> statuses = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Dictionary<EmulationTapeCommand, (Button Button, MenuItem Menu)>> tape = new(StringComparer.Ordinal);
    private readonly HashSet<Button> blinking = [];
    private readonly DispatcherTimer blinkTimer;
    private FrameworkElement? video;
    private bool shaderLoading;
    private bool blinkVisible = true;
    private bool disposed;

    internal MachineView(WindowsXPColorScheme scheme = WindowsXPColorScheme.Blue)
    {
        blinkTimer = new DispatcherTimer(DispatcherPriority.Render, Dispatcher) { Interval = BlinkInterval };
        blinkTimer.Tick += BlinkTick;
        try
        {
            InitializeComponent();
            resourceScope = new WindowsXPResources(this, scheme);
        }
        catch { Dispose(); throw; }
    }

    // The existing command controller owns these endpoints; its Base toolbar is not displayed here.
    internal override DockPanel Toolbar { get; } = new();
    internal override Grid VideoHost => VideoArea;
    internal override Border Screen => ScreenArea;
    internal override Grid DisplayHost => DisplayArea;
    internal override IReadOnlyDictionary<string, Ellipse> DeviceLeds => leds;

    internal override void AttachCommands(MachineCommandPresentation commands)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        commandBindings?.Dispose();
        commandBindings = new ThemeBindingScope();
        Bind(MachineCommandId.Power, PowerCommand, state: true);
        Bind(MachineCommandId.PauseResume, PauseCommand, state: true);
        Bind(MachineCommandId.SoftReset, SoftResetCommand);
        Bind(MachineCommandId.HardReset, HardResetCommand);
        Bind(MachineCommandId.QuickSave, QuickSaveCommand);
        Bind(MachineCommandId.QuickLoad, QuickLoadCommand);
        Bind(MachineCommandId.Audio, AudioCommand, state: true);
        Bind(MachineCommandId.ControllerPointer, PointerCommand, state: true);
        Bind(MachineCommandId.CoreOptions, CoreCommand);
        Bind(MachineCommandId.CaptureScreen, CaptureCommand);
        Bind(MachineCommandId.Fullscreen, FullscreenCommand);
        Tool(MachineCommandId.Power, PowerToolbar, state: true);
        Tool(MachineCommandId.PauseResume, PauseToolbar, state: true);
        Tool(MachineCommandId.SoftReset, ResetToolbar);
        Tool(MachineCommandId.CoreOptions, CoreToolbar);
        commandBindings.OneWay(commands.Status, TextBlock.TextProperty, Status, TextBlock.TextProperty);
        commandBindings.OneWay(commands.RendererStatus, TextBlock.TextProperty, Renderer, TextBlock.TextProperty);
        InputStatus(commands.ControllerStatus, ControllerLight);
        InputStatus(commands.PointerStatus, PointerLight);

        void Bind(MachineCommandId id, MenuItem target, bool state = false)
        {
            if (!commands.Commands.TryGetValue(id, out var original)) { target.Visibility = Visibility.Collapsed; return; }
            commandBindings.Action(original, target);
            if (state) commandBindings.OneWay(original, TagProperty, target, MenuItem.IsCheckedProperty);
        }
        void Tool(MachineCommandId id, System.Windows.Controls.Primitives.ButtonBase target, bool state = false)
        {
            if (!commands.Commands.TryGetValue(id, out var original)) { target.Visibility = Visibility.Collapsed; return; }
            commandBindings.Action(original, target);
            commandBindings.OneWay(original, ToolTipProperty, target, ToolTipProperty);
            if (state) commandBindings.OneWay(original, TagProperty, target, ToggleButton.IsCheckedProperty);
        }
        void InputStatus(TextBlock original, Ellipse target)
        {
            commandBindings.OneWay(original, TextBlock.ForegroundProperty, target, Shape.FillProperty);
            commandBindings.OneWay(original, AutomationProperties.ItemStatusProperty, target, ToolTipProperty);
        }
    }

    internal override void SetVideoView(FrameworkElement view)
    {
        if (video is not null) VideoArea.Children.Remove(video);
        video = view;
        view.Visibility = shaderLoading ? Visibility.Hidden : Visibility.Visible;
        VideoArea.Children.Insert(VideoIndex, view);
    }

    internal override FrameworkElement? DetachVideoView()
    {
        var previous = video;
        if (previous is not null) VideoArea.Children.Remove(previous);
        video = null;
        return previous;
    }

    internal override void SetShaderLoading(bool isLoading)
    {
        shaderLoading = isLoading;
        if (video is not null) video.Visibility = isLoading ? Visibility.Hidden : Visibility.Visible;
        Loading.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
    }

    internal override void SetDevices(IEnumerable<MachineViewDevice> devices, Action<Exception> showError, Action restoreFocus)
    {
        ClearDevices();
        var groups = new Dictionary<EmulationMediaType, MenuItem>();
        foreach (var device in devices)
        {
            var menu = new MenuItem { Header = device.Label };
            if (device.MediaType is { } type)
            {
                if (!groups.TryGetValue(type, out var group))
                {
                    group = new MenuItem();
                    group.SetBinding(MenuItem.HeaderProperty, LocExtension.CreateBinding(DeviceResource(type)));
                    groups.Add(type, group);
                    SupportsMenu.Items.Add(group);
                }
                group.Items.Add(menu);
            }
            else SupportsMenu.Items.Add(menu);
            var row = new WrapPanel { Margin = DeviceMargin, VerticalAlignment = VerticalAlignment.Center };
            var led = new Ellipse { Width = LedSize, Height = LedSize, Fill = device.Present || !device.Removable ? Brushes.ForestGreen : Brushes.Gray,
                VerticalAlignment = VerticalAlignment.Center, Margin = LedMargin };
            leds.Add(device.Key, led);
            var indicator = new Grid { Margin = DeviceMargin };
            indicator.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            indicator.ColumnDefinitions.Add(new ColumnDefinition());
            indicator.Children.Add(led);
            var launcher = new Button
            {
                Content = new TextBlock { Text = device.Label, TextTrimming = TextTrimming.CharacterEllipsis },
                ToolTip = device.Label,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Margin = CommandMargin
            };
            Grid.SetColumn(launcher, DeviceLabelColumn);
            indicator.Children.Add(launcher);
            RoutedEventHandler request = (_, args) => { args.Handled = true; if (!disposed) DeviceRequested?.Invoke(device.Key); };
            launcher.Click += request;
            detachDevices.Add(() => launcher.Click -= request);
            Devices.Children.Add(indicator);
            if (device.Removable && device.Insert is not null)
            {
                var insert = new MenuItem { Header = LocExtension.Get(InsertResource) };
                menu.Items.Add(insert);
                var button = new Button { Content = LocExtension.Get(InsertResource), Margin = CommandMargin };
                row.Children.Add(button);
                Action(insert, button, device.Insert);
            }
            else row.Children.Add(new TextBlock { Text = device.Label, Margin = CommandMargin, VerticalAlignment = VerticalAlignment.Center });
            if (device.Status is not null)
            {
                var text = new TextBlock { Text = device.Status, Margin = CommandMargin, VerticalAlignment = VerticalAlignment.Center };
                row.Children.Add(text);
                var status = new MenuItem { Header = device.Status, IsEnabled = false };
                menu.Items.Add(status);
                statuses.Add(device.Key, (text, status));
            }
            if (device.Eject is not null)
            {
                var eject = new MenuItem { Header = LocExtension.Get(EjectResource), IsEnabled = device.Present };
                menu.Items.Add(eject);
                var button = new Button { Content = LocExtension.Get(EjectResource), IsEnabled = device.Present, Margin = CommandMargin };
                row.Children.Add(button);
                Action(eject, button, device.Eject);
            }
            if (device.Commands is { Count: > 0 })
            {
                menu.Items.Add(new Separator());
                var commands = new Dictionary<EmulationTapeCommand, (Button, MenuItem)>();
                foreach (var command in device.Commands)
                {
                    var item = new MenuItem { Header = command.Label, IsEnabled = command.IsEnabled, IsChecked = command.IsActive };
                    var button = new Button { Content = command.Label, IsEnabled = command.IsEnabled, Margin = CommandMargin };
                    menu.Items.Add(item);
                    row.Children.Add(button);
                    commands.Add(command.Command, (button, item));
                    TapeAppearance(button, command.IsActive, command.IsBlinking);
                    Action(item, button, command.Execute);
                }
                tape.Add(device.Key, commands);
            }
            devicePanels.Add(device.Key, row);
            deviceLabels.Add(device.Key, device.Label);
        }
        SupportsMenu.IsEnabled = SupportsMenu.Items.Count > 0;
        DeviceBand.Visibility = Devices.Children.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        UpdateBlinkTimer();

        void Action(MenuItem item, Button button, Func<Task> execute)
        {
            RoutedEventHandler invoke = async (_, args) =>
            {
                args.Handled = true;
                if (disposed || !button.IsEnabled) return;
                try { await execute(); }
                catch (Exception error) { if (!disposed) showError(error); }
                finally { if (!disposed) restoreFocus(); }
            };
            item.Click += invoke;
            button.Click += invoke;
            detachDevices.Add(() => { item.Click -= invoke; button.Click -= invoke; });
        }
    }

    internal override void SetDeviceStatus(string key, string status)
    {
        if (!statuses.TryGetValue(key, out var target)) return;
        target.Text.Text = status;
        target.Menu.Header = status;
    }

    internal override void SetTapeTransport(string key, EmulationTapeState state, EmulationTapeCommand? activeOperation,
        IReadOnlySet<EmulationTapeCommand> supportedCommands, bool powered)
    {
        if (!tape.TryGetValue(key, out var commands)) return;
        foreach (var (command, targets) in commands)
        {
            var active = TapeTransportPresentationFunctions.IsActive(state, activeOperation, command);
            var enabled = TapeTransportPresentationFunctions.IsEnabled(powered, supportedCommands.Contains(command), state, activeOperation, command);
            targets.Button.IsEnabled = targets.Menu.IsEnabled = enabled;
            targets.Menu.IsChecked = active;
            TapeAppearance(targets.Button, active, TapeTransportPresentationFunctions.IsBlinking(state, activeOperation, command));
        }
        UpdateBlinkTimer();
    }

    private void TapeAppearance(Button button, bool active, bool isBlinking)
    {
        button.FontWeight = active ? FontWeights.Bold : FontWeights.Normal;
        button.Opacity = FullOpacity;
        blinking.Remove(button);
        if (isBlinking) blinking.Add(button);
    }

    private void UpdateBlinkTimer()
    {
        if (blinking.Count > 0) blinkTimer.Start();
        else { blinkTimer.Stop(); blinkVisible = true; }
    }

    private void BlinkTick(object? sender, EventArgs args)
    {
        blinkVisible = !blinkVisible;
        foreach (var button in blinking) button.Opacity = blinkVisible ? FullOpacity : BlinkOpacity;
    }

    private void ClearDevices()
    {
        try { DevicePanelsChanging?.Invoke(this, EventArgs.Empty); }
        finally { ClearDeviceControls(); }
    }

    private void ClearDeviceControls()
    {
        blinkTimer.Stop();
        foreach (var detach in detachDevices) detach();
        detachDevices.Clear();
        blinking.Clear();
        tape.Clear();
        statuses.Clear();
        leds.Clear();
        devicePanels.Clear();
        deviceLabels.Clear();
        if (Devices is not null) Devices.Children.Clear();
        if (DeviceBand is not null) DeviceBand.Visibility = Visibility.Collapsed;
        if (SupportsMenu is not null) SupportsMenu.Items.Clear();
    }

    public override void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try { commandBindings?.Dispose(); }
        finally
        {
            try { ClearDevices(); }
            finally
            {
                blinkTimer.Tick -= BlinkTick;
                DeviceRequested = null;
                DevicePanelsChanging = null;
                Toolbar.Children.Clear();
                if (VideoArea is not null) VideoArea.Children.Clear();
                video = null;
                try { resourceScope?.Dispose(); }
                finally { resourceScope = null; Content = null; }
            }
        }
    }

    private static string DeviceResource(EmulationMediaType type) => type switch
    {
        EmulationMediaType.Floppy => EmulationResourceKeys.FloppyDevice,
        EmulationMediaType.HardDisk => EmulationResourceKeys.HardDiskDevice,
        EmulationMediaType.CompactDisc => EmulationResourceKeys.CompactDiscDevice,
        EmulationMediaType.Tape => EmulationResourceKeys.TapeDevice,
        EmulationMediaType.Cartridge => EmulationResourceKeys.CartridgeDevice,
        EmulationMediaType.MemoryCard => EmulationResourceKeys.MemoryCardDevice,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    private const string InsertResource = "Emulation.Shortcut.InsertMedia";
    private const string EjectResource = "Common.Eject";
    private const int VideoIndex = 0;
    private const int DeviceLabelColumn = 1;
    private const double LedSize = 8;
    private const double FullOpacity = 1;
    private const double BlinkOpacity = 0.4;
    private static readonly TimeSpan BlinkInterval = TimeSpan.FromMilliseconds(400);
    private static readonly Thickness DeviceMargin = new(4, 2, 8, 2);
    private static readonly Thickness CommandMargin = new(2);
    private static readonly Thickness LedMargin = new(0, 0, 4, 0);
}
