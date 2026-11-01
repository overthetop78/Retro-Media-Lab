using GWGUI.Infrastructure.Settings.Emulation;
using GWGUI.App.Constants.Controls.Visual;
using GWGUI.App.Constants.Localization;
using GWGUI.App.Constants.Machine;
using GWGUI.App.Contracts.Input;
using GWGUI.App.Contracts.Machine;
using GWGUI.App.Functions.Emulation.Shortcuts;
using GWGUI.App.Localization.Extensions;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;


namespace GWGUI.App.Views.Controls.Emulation.Machine;

internal sealed class MachineCommandBar
{
    private readonly Dictionary<MachineCommandId, Button> _commandsById = [];
    private DockPanel _host;
    private readonly SizeChangedEventHandler _resize;
    private readonly List<Button> _requiresPower = [];
    private readonly Button _power;
    private readonly Button _pause;
    private readonly Button _audio;
    private readonly Button _quickSave;
    private readonly Button _quickLoad;
    private readonly Button? _controllerPointer;
    private readonly Action _restoreFocus;
    private bool _powered;
    private bool _supportsSavedStates;
    private bool _quickStateAvailable;

    internal MachineCommandBar(DockPanel host, MachineCommandActions actions, IReadOnlyList<GlobalShortcutBinding> shortcuts, Action<Exception> showError, Action restoreFocus)
    {
        _host = host;
        _restoreFocus = restoreFocus;
        _power = Command(MachineCommandId.Power, MachineCommandGlyphConstants.Power, EmulationResourceKeys.Power, actions.TogglePower, showError, false);
        _power.Foreground = Brushes.LimeGreen;
        _pause = Command(MachineCommandId.PauseResume, MachineCommandGlyphConstants.Pause, EmulationResourceKeys.PauseResume, actions.TogglePause, showError);
        _pause.Foreground = Brushes.DarkOrange;

        var softReset = Command(MachineCommandId.SoftReset, MachineCommandGlyphConstants.SoftReset, EmulationResourceKeys.SoftReset, actions.SoftReset, showError);
        softReset.Foreground = new SolidColorBrush(Color.FromRgb(120, 160, 48));
        var hardReset = Command(MachineCommandId.HardReset, MachineCommandGlyphConstants.HardReset, EmulationResourceKeys.HardReset, actions.HardReset, showError);
        hardReset.Foreground = new SolidColorBrush(Color.FromRgb(220, 92, 48));

        var left = new StackPanel { Orientation = Orientation.Horizontal };
        left.Children.Add(MachineView.CreateToolbarGroup(_power, _pause, softReset, hardReset));
        _quickSave = Command(MachineCommandId.QuickSave, MachineCommandGlyphConstants.QuickSave, EmulationResourceKeys.QuickSave, actions.QuickSave, showError);
        _quickLoad = Command(MachineCommandId.QuickLoad, MachineCommandGlyphConstants.QuickLoad, EmulationResourceKeys.QuickLoad, actions.QuickLoad, showError);
        left.Children.Add(MachineView.CreateToolbarGroup(_quickSave, _quickLoad));
        left.Children.Add(MachineView.CreateToolbarGroup(
            Command(MachineCommandId.CaptureScreen, MachineCommandGlyphConstants.CaptureScreen, EmulationResourceKeys.CaptureScreen, actions.CaptureScreen, showError),
            Command(MachineCommandId.Fullscreen, MachineCommandGlyphConstants.Fullscreen, EmulationResourceKeys.Fullscreen, actions.ToggleFullscreen, showError, false)));
        var stateShortcuts = EmulationShortcutViewFunctions.CreateGroup(shortcuts, (EmulationShortcutDefaults.QuickSave, EmulationResourceKeys.QuickSave), (EmulationShortcutDefaults.QuickLoad, EmulationResourceKeys.QuickLoad));
        left.Children.Add(stateShortcuts);
        DockPanel.SetDock(left, Dock.Left);
        host.Children.Add(left);

        var right = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        var displayShortcuts = EmulationShortcutViewFunctions.CreateGroup(shortcuts, (EmulationShortcutDefaults.ToggleFullscreen, EmulationResourceKeys.Fullscreen), (EmulationShortcutDefaults.ReleaseMouse, EmulationResourceKeys.ReleaseMouse));
        right.Children.Add(displayShortcuts);
        if (actions.OpenCoreOptions is not null)
            right.Children.Add(MachineView.CreateToolbarGroup(Command(MachineCommandId.CoreOptions, MachineCommandGlyphConstants.CoreOptions, "Emulation.Core.NameOptions", actions.OpenCoreOptions, showError)));
        _audio = Command(MachineCommandId.Audio, MachineCommandGlyphConstants.Audio, EmulationResourceKeys.Audio, actions.ToggleAudio, showError);
        var statusItems = new List<UIElement> { _audio };
        if (actions.SwitchControllerPointer is not null)
        {
            _controllerPointer = Command(MachineCommandId.ControllerPointer, MachineCommandGlyphConstants.Pointer, EmulationResourceKeys.SwitchControllerPointer, actions.SwitchControllerPointer, showError);
            _controllerPointer.Foreground = Brushes.LimeGreen;
            statusItems.Add(_controllerPointer);
        }
        ControllerStatus = StatusIcon(MachineCommandGlyphConstants.Controller);
        PointerStatus = StatusIcon(MachineCommandGlyphConstants.Pointer);
        statusItems.Add(ControllerStatus);
        statusItems.Add(PointerStatus);
        right.Children.Add(MachineView.CreateToolbarGroup(statusItems.ToArray()));
        Status = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        right.Children.Add(MachineView.CreateToolbarGroup(Status));
        DockPanel.SetDock(right, Dock.Right);
        host.Children.Add(right);

        RendererStatus = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = FontWeights.SemiBold
        };
        var rendererGroup = MachineView.CreateToolbarGroup(new TextBlock
        {
            Text = MachineCommandGlyphConstants.Renderer,
            FontFamily = ControlVisualConstants.IconFont,
            FontSize = 15,
            Margin = new Thickness(4, 0, 6, 0),
            VerticalAlignment = VerticalAlignment.Center
        }, RendererStatus);
        rendererGroup.Padding = new Thickness(16, 1, 16, 1);
        rendererGroup.Margin = new Thickness(8, 1, 8, 1);
        host.Children.Add(rendererGroup);
        Presentation = new(new System.Collections.ObjectModel.ReadOnlyDictionary<MachineCommandId, Button>(_commandsById),
            Status, RendererStatus, ControllerStatus, PointerStatus);
        _resize = (_, args) =>
        {
            var visibility = args.NewSize.Width >= MachinePresentationConstants.WideToolbarMinimumWidth ? Visibility.Visible : Visibility.Collapsed;
            stateShortcuts.Visibility = visibility;
            displayShortcuts.Visibility = visibility;
        };
        host.SizeChanged += _resize;
    }

    internal void Rehost(DockPanel host)
    {
        if (ReferenceEquals(host, _host)) return;
        if (host.Children.Count != 0) throw new InvalidOperationException(nameof(host));
        var previous = _host;
        var children = previous.Children.Cast<UIElement>().ToArray();
        previous.SizeChanged -= _resize;
        previous.Children.Clear();
        try
        {
            foreach (var child in children) host.Children.Add(child);
            _host = host;
        }
        catch
        {
            host.Children.Clear();
            foreach (var child in children) previous.Children.Add(child);
            throw;
        }
        finally { _host.SizeChanged += _resize; }
    }

    internal TextBlock Status { get; }
    internal TextBlock RendererStatus { get; }
    internal TextBlock ControllerStatus { get; }
    internal TextBlock PointerStatus { get; }
    internal MachineCommandPresentation Presentation { get; }

    internal void SetPowered(bool powered)
    {
        _powered = powered;
        _power.Tag = powered;
        foreach (var button in _requiresPower) button.IsEnabled = powered;
        _power.Foreground = powered ? Brushes.LimeGreen : Brushes.Gray;
        UpdateSavedStateButtons();
    }

    internal void SetSavedStateAvailability(bool supported, bool quickStateAvailable)
    {
        _supportsSavedStates = supported;
        _quickStateAvailable = quickStateAvailable;
        UpdateSavedStateButtons();
    }

    internal void SetPaused(bool paused)
    {
        _pause.Tag = paused;
        SetGlyph(_pause, paused ? MachineCommandGlyphConstants.Continue : MachineCommandGlyphConstants.Pause);
        _pause.Foreground = paused ? Brushes.LimeGreen : Brushes.DarkOrange;
    }

    internal void SetMuted(bool muted)
    {
        _audio.Tag = muted;
        SetGlyph(_audio, muted ? MachineCommandGlyphConstants.Muted : MachineCommandGlyphConstants.Audio);
        _audio.Foreground = muted ? Brushes.Gray : Brushes.Black;
    }

    internal void SetControllerPointerMode(bool pointerMode)
    {
        if (_controllerPointer is null) return;
        _controllerPointer.Tag = pointerMode;
        ((TextBlock)_controllerPointer.Content).Text = pointerMode ? MachineCommandGlyphConstants.Pointer : MachineCommandGlyphConstants.Controller;
        _controllerPointer.Foreground = Brushes.LimeGreen;
    }

    internal void SetInputStatus(bool pointerCaptured, bool controllerAvailable)
    {
        PointerStatus.Foreground = pointerCaptured ? Brushes.LimeGreen : Brushes.LightGray;
        ControllerStatus.Foreground = controllerAvailable ? Brushes.LimeGreen : Brushes.LightGray;
        AutomationProperties.SetItemStatus(PointerStatus, LocExtension.Get(pointerCaptured ? "Emulation.Value.Enabled" : "Emulation.Value.Disabled"));
        AutomationProperties.SetItemStatus(ControllerStatus, LocExtension.Get(controllerAvailable ? "Emulation.Value.Enabled" : "Emulation.Value.Disabled"));
    }

    private void UpdateSavedStateButtons()
    {
        _quickSave.IsEnabled = _powered && _supportsSavedStates;
        _quickLoad.IsEnabled = _powered && _supportsSavedStates && _quickStateAvailable;
    }

    private static void SetGlyph(Button button, string glyph)
    {
        if (button.Content is TextBlock text) text.Text = glyph;
    }

    private Button Command(MachineCommandId id, string glyph, string tooltipResource, Func<Task> action, Action<Exception> showError, bool requiresPower = true)
    {
        var button = MachineView.CreateCommandButton(glyph, LocExtension.Get(tooltipResource));
        button.Tag = false;
        _commandsById.Add(id, button);
        button.Click += async (_, _) => await RunAsync(action, showError);
        if (requiresPower) _requiresPower.Add(button);
        return button;
    }

    private static TextBlock StatusIcon(string glyph) => new()
    {
        Text = glyph,
        FontFamily = ControlVisualConstants.IconFont,
        FontSize = 16,
        Foreground = Brushes.LightGray,
        Margin = new Thickness(5, 0, 5, 0),
        VerticalAlignment = VerticalAlignment.Center
    };

    private async Task RunAsync(Func<Task> action, Action<Exception> showError)
    {
        try { await action(); }
        catch (Exception error) { showError(error); }
        finally { _restoreFocus(); }
    }
}
