using System.Windows;
using System.Windows.Controls;
using GWGUI.App.Contracts.Emulation.Configurations;
using GWGUI.App.Theme.Common;
using GWGUI.App.Views.Controls.Emulation.Machine;
using MachineView = GWGUI.App.Theme.Orbit.Views.MachineView;

namespace GWGUI.App.Theme.Orbit;

internal sealed partial class OrbitWorkspace
{
    private sealed record MachineEntry(ContentControl Host, MachinePresentationLease Lease, EmulationConfigurationListItem Configuration);
    private readonly Dictionary<MachineController, MachineEntry> machines = [];
    private readonly List<Action> sessionDetach = [];
    private MachineController? selectedMachine;
    private bool refreshingMachines;
    private static readonly Thickness SessionGap = new(0, 0, 8, 4);
    private const double SessionImageSize = 24;
    private const double SessionsMaximumHeight = 92;

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
            var opened = machineSource.OpenedMachines;
            foreach (var controller in machines.Keys.Where(controller => !opened.Any(item => ReferenceEquals(item.Controller, controller))).ToArray()) ReleaseMachine(controller);
            foreach (var item in opened)
                if (!item.Controller.IsStopped && !machines.ContainsKey(item.Controller)) CreateMachine(item.Configuration, item.Controller);
            foreach (var action in sessionDetach) action();
            sessionDetach.Clear();
            var sessions = new WrapPanel();
            var home = new Button { Margin = SessionGap };
            home.SetBinding(ContentControl.ContentProperty, GWGUI.App.Localization.Extensions.LocExtension.CreateBinding("Options.Themes.Orbit.Machines"));
            RoutedEventHandler showLibrary = (_, _) => ShowLibrary();
            home.Click += showLibrary;
            sessionDetach.Add(() => home.Click -= showLibrary);
            sessions.Children.Add(home);
            foreach (var (controller, entry) in machines)
            {
                var identity = new StackPanel { Orientation = Orientation.Horizontal };
                var imagePath = entry.Configuration.Module.Machines.FirstOrDefault(machine => machine.Id == entry.Configuration.Configuration.MachineId)?.ImagePath;
                identity.Children.Add(new Image { Source = EmulationAssetFunctions.Load(imagePath), Width = SessionImageSize, Height = SessionImageSize, Margin = SessionGap });
                identity.Children.Add(new TextBlock { Text = entry.Configuration.DisplayName, VerticalAlignment = VerticalAlignment.Center });
                var button = new Button { Content = identity, Margin = SessionGap };
                RoutedEventHandler selected = (_, _) => { if (!disposed) machineSource.SelectOpenedMachine(controller); };
                button.Click += selected;
                sessionDetach.Add(() => button.Click -= selected);
                var close = new Button { Margin = SessionGap };
                close.SetBinding(ContentControl.ContentProperty, GWGUI.App.Localization.Extensions.LocExtension.CreateBinding("Common.Close"));
                RoutedEventHandler closed = async (_, _) =>
                {
                    if (disposed) return;
                    try { await machineSource.CloseOpenedMachineAsync(controller); }
                    catch (Exception error) { if (!disposed) ShowMachineError(error); }
                };
                close.Click += closed;
                sessionDetach.Add(() => close.Click -= closed);
                var group = new StackPanel { Orientation = Orientation.Horizontal };
                group.Children.Add(button);
                group.Children.Add(close);
                sessions.Children.Add(group);
            }
            shell.SessionContent = new ScrollViewer { Content = sessions, MaxHeight = SessionsMaximumHeight,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        }
        finally { refreshingMachines = false; }
    }

    private void CreateMachine(EmulationConfigurationListItem configuration, MachineController controller)
    {
        var host = new ContentControl { HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch };
        MachinePresentationLease? lease = null;
        var retained = false;
        try
        {
            lease = new MachinePresentationLease(controller, host, () => new MachineView(),
                () => !disposed && IsVisible && ReferenceEquals(selectedMachine, controller) && shell.Area == OrbitArea.Machines);
            machines.Add(controller, new(host, lease, configuration));
            retained = true;
        }
        finally { if (!retained) lease?.Dispose(); }
    }

    private void MachineSelected(MachineController controller)
    {
        if (disposed) return;
        RefreshMachines();
        if (!machines.TryGetValue(controller, out var entry)) return;
        selectedMachine = controller;
        shell.MachineContent = entry.Host;
        shell.Area = OrbitArea.Machines;
        shell.ContextTitle = entry.Configuration.DisplayName;
    }

    private void ReleaseMachine(MachineController controller)
    {
        if (!machines.Remove(controller, out var entry)) return;
        if (ReferenceEquals(selectedMachine, controller)) ShowLibrary();
        try { entry.Lease.Dispose(); }
        finally { entry.Host.Content = null; }
    }
}
