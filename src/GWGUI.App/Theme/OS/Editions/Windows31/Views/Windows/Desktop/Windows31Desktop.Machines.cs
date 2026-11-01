using System.ComponentModel;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GWGUI.App.Constants.Emulation.Errors;
using GWGUI.App.Contracts.Emulation.Configurations;
using GWGUI.App.Localization.Extensions;
using GWGUI.App.Presenters.Common;
using GWGUI.App.Theme.Common;
using GWGUI.App.Views.Controls.Emulation.Machine;
using MachinePresentation = GWGUI.App.Theme.OS.Editions.Windows31.Views.MachineView;

namespace GWGUI.App.Theme.OS.Editions.Windows31;

internal sealed partial class Windows31Desktop
{
    private readonly record struct MachineKey(string ModuleId, Guid ConfigurationId);
    private sealed record MachineEntry(MachineKey Key, MachineController Controller, Windows31Frame Frame,
        Windows31WindowPlacement Placement, ListBoxItem Icon, MachinePresentationLease Lease,
        PropertyChangedEventHandler StateChanged, MouseButtonEventHandler Activated, CancelEventHandler Closing)
    {
        internal bool IsClosing { get; set; }
    }

    private readonly Dictionary<MachineKey, MachineEntry> machineWindows = [];
    private EmulationSection? machineSource;
    private bool machineSourceAttached;
    private bool refreshingMachines;
    private const double MachineWidth = 800;
    private const double MachineHeight = 600;

    internal IReadOnlyList<(MachineController Controller, Windows31WindowPlacement Placement)> OpenMachineWindows =>
        machineWindows.Values.Select(entry => (entry.Controller, entry.Placement)).ToArray();

    internal void ConnectMachines(EmulationSection source)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (machineSource is not null && !ReferenceEquals(machineSource, source))
            throw new InvalidOperationException(nameof(source));
        machineSource = source;
        if (manager is not null) InitializeMachineWindows();
    }

    private void InitializeMachineWindows()
    {
        if (machineSource is null || machineSourceAttached) return;
        machineSource.PresentationChanged += MachineConfigurationsChanged;
        machineSource.MachineSelected += SelectedMachineChanged;
        machineSourceAttached = true;
        RefreshMachineWindows();
    }

    private void MachineConfigurationsChanged(object? sender, EventArgs args)
    {
        if (disposed) return;
        try { RefreshMachineWindows(); }
        catch (Exception error) { ShowMachineError(error, LocExtension.Get(MachineGroupResource)); }
    }

    private void SelectedMachineChanged(MachineController controller)
    {
        if (disposed) return;
        try
        {
            RefreshMachineWindows();
            var entry = machineWindows.Values.FirstOrDefault(item => ReferenceEquals(item.Controller, controller));
            if (entry is not null && !entry.Placement.IsActive) ActivateMachine(entry);
        }
        catch (Exception error) { ShowMachineError(error, LocExtension.Get(MachineGroupResource)); }
    }

    private void RefreshMachineWindows()
    {
        if (disposed || refreshingMachines || machineSource is null) return;
        refreshingMachines = true;
        try
        {
            manager.SetMachines(machineSource.Configurations, OpenConfiguredMachineAsync);
            var opened = machineSource.OpenedMachines;
            var keys = opened.Select(item => MachineIdentity(item.Configuration)).ToHashSet();
            foreach (var entry in machineWindows.Values.Where(entry => !keys.Contains(entry.Key)).ToArray()) ReleaseMachine(entry);
            foreach (var item in opened)
            {
                var key = MachineIdentity(item.Configuration);
                if (machineWindows.TryGetValue(key, out var existing))
                {
                    var title = MachineCaption(item.Configuration);
                    existing.Frame.Title = title;
                    Windows31ProgramManager.SetLiteralIconTitle(existing.Icon, title);
                }
                else if (!item.Controller.IsStopped) CreateMachineWindow(item.Configuration, item.Controller);
            }
        }
        finally { refreshingMachines = false; }
    }

    private async Task OpenConfiguredMachineAsync(EmulationConfigurationListItem configuration)
    {
        if (disposed || machineSource is null) return;
        try { await machineSource.RequestOpenAsync(configuration); }
        catch (Exception error) { if (!disposed) ShowMachineError(error, configuration.DisplayName); }
    }

    private void CreateMachineWindow(EmulationConfigurationListItem configuration, MachineController controller)
    {
        var key = MachineIdentity(configuration);
        var offset = Windows31Metrics.DesktopInset + (applications.Count + machineWindows.Count) * Windows31Metrics.CascadeStep;
        var placement = new Windows31WindowPlacement(DesktopBounds(), new Rect(offset, offset, MachineWidth, MachineHeight),
            new Size(Windows31Metrics.ApplicationMinimumWidth, Windows31Metrics.ApplicationMinimumHeight));
        var host = new ContentControl { HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch };
        Windows31Frame? frame = null;
        MachinePresentationLease? lease = null;
        MachineEntry? entry = null;
        try
        {
            var title = MachineCaption(configuration);
            frame = new Windows31Frame(placement, host) { Title = title };
            var machine = configuration.Module.Machines.FirstOrDefault(item => item.Id == configuration.Configuration.MachineId);
            var icon = manager.CreateIcon(title, EmulationAssetFunctions.Load(machine?.ImagePath), Windows31ApplicationId.Emulation, literalTitle: true);
            icon.Tag = key;
            lease = new MachinePresentationLease(controller, host, () => new MachinePresentation(),
                () => !disposed && placement.IsActive && placement.IsVisible);
            PropertyChangedEventHandler changed = async (_, _) =>
            {
                if (entry is null) return;
                if (entry.Placement.State == Windows31WindowState.Closed) await RequestMachineCloseAsync(entry);
                else UpdateMachineWindow(entry);
            };
            MouseButtonEventHandler activated = (_, _) => { if (entry is not null) ActivateMachine(entry); };
            CancelEventHandler closing = async (_, args) =>
            {
                args.Cancel = true;
                if (entry is not null) await RequestMachineCloseAsync(entry);
            };
            entry = new(key, controller, frame, placement, icon, lease, changed, activated, closing);
            machineWindows.Add(key, entry);
            placement.PropertyChanged += changed;
            frame.PreviewMouseDown += activated;
            frame.Closing += closing;
            workspace.Children.Add(frame);
            ConnectMachineDevices(entry);
            ActivateMachine(entry);
        }
        catch
        {
            if (entry is not null && machineWindows.ContainsKey(key)) ReleaseMachine(entry);
            else
            {
                try { frame?.Dispose(); }
                finally { lease?.Dispose(); }
            }
            throw;
        }
    }

    internal Task CloseMachineWindowAsync(MachineController controller) =>
        disposed || machineSource is null ? Task.CompletedTask : machineSource.CloseOpenedMachineAsync(controller);

    private async Task RequestMachineCloseAsync(MachineEntry entry)
    {
        if (disposed || entry.IsClosing) return;
        entry.IsClosing = true;
        try { await CloseMachineWindowAsync(entry.Controller); }
        catch (Exception error) { if (!disposed) ShowMachineError(error, entry.Frame.Title); }
        finally { entry.IsClosing = false; }
    }

    private void ActivateMachine(MachineEntry entry)
    {
        if (disposed) return;
        DeactivateDialogs();
        foreach (var device in deviceWindows.Values) device.Placement.Deactivate();
        managerPlacement.Deactivate();
        foreach (var application in applications.Values) application.Placement.Deactivate();
        foreach (var machine in machineWindows.Values) if (!ReferenceEquals(machine, entry)) machine.Placement.Deactivate();
        entry.Placement.Activate();
        Panel.SetZIndex(entry.Frame, ++zIndex);
        machineSource?.SelectOpenedMachine(entry.Controller);
    }

    private void DeactivateMachineWindows()
    {
        foreach (var entry in machineWindows.Values) entry.Placement.Deactivate();
        foreach (var entry in deviceWindows.Values) entry.Placement.Deactivate();
    }

    private bool ActivateNativeMachine(FrameworkElement layer)
    {
        var device = deviceWindows.Values.FirstOrDefault(item => ReferenceEquals(item.Frame, layer));
        if (device is not null) { ActivateDevice(device); return true; }
        var entry = machineWindows.Values.FirstOrDefault(item => ReferenceEquals(item.Frame, layer));
        if (entry is null) return false;
        ActivateMachine(entry);
        return true;
    }

    private bool RestoreMachineIcon(object? tag)
    {
        if (tag is not MachineKey key || !machineWindows.TryGetValue(key, out var entry)) return false;
        ActivateMachine(entry);
        return true;
    }

    private void UpdateMachineWindow(MachineEntry entry)
    {
        if (disposed) return;
        var reduced = entry.Placement.State == Windows31WindowState.Iconified;
        if (reduced && !icons.Items.Contains(entry.Icon)) icons.Items.Add(entry.Icon);
        if (!reduced) icons.Items.Remove(entry.Icon);
        ArrangeIcons();
    }

    private void ReleaseMachine(MachineEntry entry)
    {
        if (!machineWindows.Remove(entry.Key)) return;
        entry.Placement.PropertyChanged -= entry.StateChanged;
        entry.Frame.PreviewMouseDown -= entry.Activated;
        entry.Frame.Closing -= entry.Closing;
        try
        {
            try
            {
                if (detachDeviceRequests.Remove(entry.Key, out var detach)) detach();
                ReleaseMachineDevices(entry.Key);
            }
            finally
            {
                try { nativeWorkspace?.ReleaseLayer(entry.Frame); }
                finally { entry.Frame.Dispose(); }
            }
        }
        finally
        {
            try { entry.Lease.Dispose(); }
            finally
            {
                workspace.Children.Remove(entry.Frame);
                icons.Items.Remove(entry.Icon);
                ArrangeIcons();
                if (!disposed && entry.Placement.IsActive)
                {
                    var next = machineWindows.Values.Where(item => item.Placement.IsVisible)
                        .OrderByDescending(item => Panel.GetZIndex(item.Frame)).FirstOrDefault();
                    if (next is not null) ActivateMachine(next);
                    else if (applications.Values.Where(item => item.Placement.IsVisible)
                        .OrderByDescending(item => Panel.GetZIndex(item.Frame)).FirstOrDefault() is { } application) Activate(application);
                    else BringManagerForward();
                }
            }
        }
    }

    private void DisconnectMachines()
    {
        if (machineSource is not null && machineSourceAttached)
        {
            machineSource.PresentationChanged -= MachineConfigurationsChanged;
            machineSource.MachineSelected -= SelectedMachineChanged;
        }
        machineSourceAttached = false;
        Exception? failure = null;
        foreach (var entry in machineWindows.Values.ToArray())
            try { ReleaseMachine(entry); }
            catch (Exception error) { failure ??= error; }
        machineSource = null;
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private void ShowMachineError(Exception error, string title) =>
        ControlErrorPresenter.ShowEmulation(this, error, ControlErrorContexts.EmulationConfigurationOpening, title);

    private static MachineKey MachineIdentity(EmulationConfigurationListItem configuration) => new(configuration.Module.Id, configuration.Configuration.Id);
    private static string MachineCaption(EmulationConfigurationListItem configuration)
    {
        var machine = configuration.Module.Machines.FirstOrDefault(item => item.Id == configuration.Configuration.MachineId);
        return machine is null ? configuration.DisplayName : LocExtension.GetForModule(configuration.Module, machine.DisplayResourceKey);
    }
    private const string MachineGroupResource = "Tab.Emulation";
}
