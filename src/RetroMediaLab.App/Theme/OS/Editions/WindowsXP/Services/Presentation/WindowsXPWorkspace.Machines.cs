using System.ComponentModel;
using MachineView = RetroMediaLab.App.Theme.OS.Editions.WindowsXP.Views.MachineView;
using System.Windows;
using System.Windows.Controls;
using RetroMediaLab.App.Contracts.Emulation.Configurations;
using RetroMediaLab.App.Presenters.Common;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.App.Theme.OS.Editions.WindowsXP.Views;
using RetroMediaLab.App.Views.Controls.Emulation.Machine;

namespace RetroMediaLab.App.Theme.OS.Editions.WindowsXP;

internal sealed partial class WindowsXPWorkspace
{
    private sealed record MachineEntry(MachineController Controller, WindowsXPWindow Window, MachineView View,
        MachinePresentationLease Lease, CancelEventHandler Closing, EventHandler Activated,
        Action<string> DeviceRequested, EventHandler DevicesChanging)
    {
        internal bool ClosingRequested { get; set; }
        internal Dictionary<string, WindowsXPWindow> Devices { get; } = [];
    }
    private readonly Dictionary<MachineController, MachineEntry> machines = [];
    private bool refreshingMachines;
    private static readonly Size MachineSize = new(1000, 680);
    private static readonly Size DeviceSize = new(460, 180);

    private IEnumerable<WindowsXPLauncherDefinition> MachineLaunchers() => machineSource?.Configurations.Select(configuration =>
        new WindowsXPLauncherDefinition(configuration.Configuration.Id.ToString(), WindowsXPProgramGroup.Emulation,
            configuration.DisplayName,
            EmulationAssetFunctions.Load(configuration.Module.Machines.FirstOrDefault(machine => machine.Id == configuration.Configuration.MachineId)?.ImagePath),
            async () =>
            {
                try { if (!disposed && machineSource is not null) await machineSource.RequestOpenAsync(configuration); }
                catch (Exception error) { if (!disposed) ShowMachineError(error); }
            })) ?? [];

    private void MachinesChanged(object? sender, EventArgs args)
    {
        if (disposed) return;
        try { RefreshMachines(); }
        catch (Exception error) { ShowMachineError(error); }
    }

    private void RefreshMachines()
    {
        if (disposed || refreshingMachines || machineSource is null) return;
        refreshingMachines = true;
        try
        {
            desktop?.SetLaunchers(launchers.Concat(MachineLaunchers()));
            var opened = machineSource.OpenedMachines;
            foreach (var controller in machines.Keys.Where(controller => !opened.Any(item => ReferenceEquals(item.Controller, controller))).ToArray())
                ReleaseMachine(controller);
            foreach (var item in opened)
                if (!item.Controller.IsStopped && !machines.ContainsKey(item.Controller)) CreateMachine(item.Configuration, item.Controller);
        }
        finally { refreshingMachines = false; }
    }

    private void MachineSelected(MachineController controller)
    {
        if (disposed) return;
        try
        {
            RefreshMachines();
            if (!machines.TryGetValue(controller, out var entry) || entry.Window.IsActive) return;
            if (entry.Window.WindowState == WindowState.Minimized) entry.Window.WindowState = WindowState.Normal;
            entry.Window.Activate();
        }
        catch (Exception error) { ShowMachineError(error); }
    }

    private void CreateMachine(EmulationConfigurationListItem configuration, MachineController controller)
    {
        WindowsXPWindow? window = null;
        MachinePresentationLease? lease = null;
        MachineEntry? entry = null;
        try
        {
            var host = new ContentControl { HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch };
            window = new WindowsXPWindow { Document = host, Title = configuration.DisplayName,
                Width = MachineSize.Width, Height = MachineSize.Height, WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = Window.GetWindow(this), ShowActivated = IsVisible, ShowInTaskbar = false };
            var image = configuration.Module.Machines.FirstOrDefault(machine => machine.Id == configuration.Configuration.MachineId)?.ImagePath;
            window.Icon = EmulationAssetFunctions.Load(image);
            lease = new MachinePresentationLease(controller, host, () => new MachineView(scheme: Scheme),
                () => !disposed && window.IsActive && window.IsVisible && window.WindowState != WindowState.Minimized);
            var view = (MachineView)controller.Presentation;
            CancelEventHandler closing = async (_, args) =>
            {
                args.Cancel = true;
                if (entry is null || disposed || entry.ClosingRequested || machineSource is null) return;
                entry.ClosingRequested = true;
                try { await machineSource.CloseOpenedMachineAsync(controller); }
                catch (Exception error) { if (!disposed) ShowMachineError(error); }
                finally { entry.ClosingRequested = false; }
            };
            EventHandler activated = (_, _) => { if (!disposed) machineSource?.SelectOpenedMachine(controller); };
            Action<string> requested = key =>
            {
                if (entry is null || disposed) return;
                try { OpenDevice(entry, key); }
                catch (Exception error) { ShowMachineError(error); }
            };
            EventHandler changing = (_, _) => { if (entry is not null) CloseDevices(entry); };
            entry = new(controller, window, view, lease, closing, activated, requested, changing);
            machines.Add(controller, entry);
            window.Closing += closing;
            window.Activated += activated;
            view.DeviceRequested += requested;
            view.DevicePanelsChanging += changing;
            RegisterWindow(window);
            window.Show();
        }
        catch
        {
            if (entry is not null) ReleaseMachine(controller);
            else
            {
                try { window?.Close(); }
                finally { lease?.Dispose(); }
            }
            throw;
        }
    }

    private void OpenDevice(MachineEntry entry, string key)
    {
        if (entry.Devices.TryGetValue(key, out var current))
        {
            if (current.WindowState == WindowState.Minimized) current.WindowState = WindowState.Normal;
            current.Activate();
            return;
        }
        if (entry.View.DevicePanel(key) is not { } panel) return;
        var window = new WindowsXPWindow { Document = panel, Title = entry.View.DeviceLabel(key),
            Width = DeviceSize.Width, Height = DeviceSize.Height, WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = entry.Window, ShowActivated = IsVisible, ShowInTaskbar = false };
        EventHandler? closed = null;
        closed = (_, _) =>
        {
            window.Closed -= closed;
            entry.Devices.Remove(key);
        };
        try
        {
            entry.Devices.Add(key, window);
            window.Closed += closed;
            RegisterWindow(window);
            window.Show();
        }
        catch
        {
            try { window.Close(); }
            finally { entry.Devices.Remove(key); window.Closed -= closed; }
            throw;
        }
    }

    private static void CloseDevices(MachineEntry entry)
    {
        Exception? failure = null;
        try
        {
            foreach (var window in entry.Devices.Values.ToArray())
                try { window.Close(); }
                catch (Exception error) { failure ??= error; }
        }
        finally { entry.Devices.Clear(); }
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private void ReleaseMachine(MachineController controller)
    {
        if (!machines.Remove(controller, out var entry)) return;
        entry.Window.Closing -= entry.Closing;
        entry.Window.Activated -= entry.Activated;
        entry.View.DeviceRequested -= entry.DeviceRequested;
        entry.View.DevicePanelsChanging -= entry.DevicesChanging;
        try { CloseDevices(entry); }
        finally
        {
            // Return renderer/input ownership before destroying the native parent HWND.
            try { entry.Lease.Dispose(); }
            finally { entry.Window.Close(); }
        }
    }
}
