using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MachinePresentation = RetroMediaLab.App.Theme.OS.Editions.Windows31.Views.MachineView;

namespace RetroMediaLab.App.Theme.OS.Editions.Windows31;

internal sealed partial class Windows31Desktop
{
    private sealed record DeviceEntry(MachineKey Owner, string Key, Windows31Frame Frame,
        Windows31WindowPlacement Placement, ContentControl Host, PropertyChangedEventHandler Changed, MouseButtonEventHandler Activated);

    private readonly Dictionary<(MachineKey Owner, string Key), DeviceEntry> deviceWindows = [];
    private readonly Dictionary<MachineKey, Action> detachDeviceRequests = [];
    private static readonly Size DeviceWindowSize = new(440, 140);
    private const double DeviceWindowMinimumHeight = 110;
    private static readonly Thickness DevicePanelMargin = new(8);

    private void ConnectMachineDevices(MachineEntry owner)
    {
        var view = (MachinePresentation)owner.Controller.Presentation;
        Action<string> requested = key =>
        {
            try { OpenDevice(owner, view, key); }
            catch (Exception error) { if (!disposed) ShowMachineError(error, view.DeviceLabel(key)); }
        };
        EventHandler changing = (_, _) => ReleaseMachineDevices(owner.Key);
        view.DeviceRequested += requested;
        view.DevicePanelsChanging += changing;
        detachDeviceRequests.Add(owner.Key, () =>
        {
            view.DeviceRequested -= requested;
            view.DevicePanelsChanging -= changing;
        });
    }

    private void OpenDevice(MachineEntry owner, MachinePresentation view, string key)
    {
        if (disposed || !machineWindows.ContainsKey(owner.Key)) return;
        if (deviceWindows.TryGetValue((owner.Key, key), out var existing)) { ActivateDevice(existing); return; }
        if (view.DevicePanel(key) is not { } panel) return;
        var bounds = owner.Placement.Bounds;
        var placement = new Windows31WindowPlacement(DesktopBounds(),
            new Rect(bounds.Left + Windows31Metrics.CascadeStep, bounds.Top + Windows31Metrics.CascadeStep, DeviceWindowSize.Width, DeviceWindowSize.Height),
            new Size(Windows31Metrics.ApplicationMinimumWidth, DeviceWindowMinimumHeight));
        var host = new ContentControl { Content = panel, Margin = DevicePanelMargin, HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Top };
        Windows31Frame? frame = null;
        DeviceEntry? entry = null;
        try
        {
            frame = new Windows31Frame(placement, host) { Title = view.DeviceLabel(key) };
            frame.ConfigureAsDialog();
            PropertyChangedEventHandler changed = (_, _) =>
            {
                if (entry is not null && placement.State == Windows31WindowState.Closed) ReleaseDevice(entry);
            };
            MouseButtonEventHandler activated = (_, _) => { if (entry is not null) ActivateDevice(entry); };
            entry = new(owner.Key, key, frame, placement, host, changed, activated);
            deviceWindows.Add((owner.Key, key), entry);
            placement.PropertyChanged += changed;
            frame.PreviewMouseDown += activated;
            workspace.Children.Add(frame);
            ActivateDevice(entry);
        }
        catch
        {
            try
            {
                if (entry is not null && deviceWindows.ContainsKey((owner.Key, key))) ReleaseDevice(entry);
                else frame?.Dispose();
            }
            finally { host.Content = null; }
            throw;
        }
    }

    private void ActivateDevice(DeviceEntry entry)
    {
        if (!machineWindows.TryGetValue(entry.Owner, out var owner)) return;
        ActivateMachine(owner);
        owner.Placement.Deactivate();
        entry.Placement.Activate();
        Panel.SetZIndex(entry.Frame, ++zIndex);
    }

    private void ReleaseMachineDevices(MachineKey owner)
    {
        Exception? failure = null;
        foreach (var entry in deviceWindows.Values.Where(item => item.Owner == owner).ToArray())
            try { ReleaseDevice(entry); }
            catch (Exception error) { failure ??= error; }
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private void ReleaseDevice(DeviceEntry entry)
    {
        if (!deviceWindows.Remove((entry.Owner, entry.Key))) return;
        entry.Placement.PropertyChanged -= entry.Changed;
        entry.Frame.PreviewMouseDown -= entry.Activated;
        try
        {
            try { nativeWorkspace?.ReleaseLayer(entry.Frame); }
            finally { entry.Frame.Dispose(); }
        }
        finally
        {
            entry.Host.Content = null;
            workspace.Children.Remove(entry.Frame);
        }
    }
}
