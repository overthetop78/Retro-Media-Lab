using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using GWGUI.App.Contracts.Emulation.Configurations;
using GWGUI.App.Enums.Input;
using GWGUI.App.Localization.Extensions;
using GWGUI.App.Theme.OS.Editions.Windows31;
using GWGUI.App.Theme.OS.Editions.Windows31.Views;
using GWGUI.App.Views.Controls.Emulation.Input;
using GWGUI.App.Views.Controls.Emulation.Options;
using GWGUI.Emulation;
using GWGUI.Emulation.Contracts;
using GWGUI.Emulation.Enums;
using GWGUI.Emulation.Interfaces;
using GWGUI.Tests.Application.TestInfrastructure;

namespace GWGUI.Tests.Interface.Themes;

[Collection("WPF")]
public sealed class Windows31EmulationPreferencesTests(StaExecutionScenarios sta)
{
    [Fact]
    public Task FolderFieldsAndShortcutCommandsKeepTheirSourcesAndDetach() => sta.Run(() =>
    {
        var source = new EmulationPreferencesSection([]);
        source.StorageFolder.Text = "virtual-storage";
        source.ShortcutEditor.ConfigureCaptureSources(InputCaptureSources.Keyboard);
        source.ShortcutEditor.SetRows([
            new("first", "Common.Save", "F2"),
            new("second", "Common.Close", "F3")], null);
        using var view = new EmulationPreferencesView(source, () => { });
        Arrange(view);
        Assert.Equal("virtual-storage", ((TextBox)view.FindName("Storage")).Text);
        ((TextBox)view.FindName("Capture")).Text = "virtual-captures";
        Assert.Equal("virtual-captures", source.CaptureFolder.Text);
        var shortcuts = Assert.IsType<InputBindingsView>(((ContentControl)view.FindName("Shortcuts")).Content);
        var rows = (ListBox)shortcuts.FindName("Rows");
        Assert.Same(source.ShortcutEditor.Rows, rows.ItemsSource);
        rows.SelectedIndex = 0;
        var changed = 0;
        source.ShortcutEditor.BindingsChanged += (_, _) => changed++;
        ((Button)shortcuts.FindName("AssignAction")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.IsType<TextBlock>(((Button)shortcuts.FindName("AssignAction")).Content);
        Assert.True(source.ShortcutEditor.CaptureKeyboardKeyDown(System.Windows.Input.Key.F6, System.Windows.Input.ModifierKeys.None));
        Assert.True(source.ShortcutEditor.CaptureKeyboardKeyUp(System.Windows.Input.Key.F6));
        Assert.Equal("F6", source.ShortcutEditor.Rows[0].Binding);
        Assert.Equal(1, changed);
        Arrange(shortcuts);
        Descendants(shortcuts).OfType<Button>().Single(button => Equals(button.Content, LocExtension.Get("Common.Delete")))
            .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Empty(source.ShortcutEditor.Rows[0].Binding);
        Descendants(shortcuts).OfType<Button>().Single(button => Equals(button.Content, LocExtension.Get("Emulation.Input.Binding.RestoreDefaults")))
            .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Equal("F2", source.ShortcutEditor.Rows[0].Binding);
        source.ShortcutEditor.SelectAndStartCapture("first", (Button)shortcuts.FindName("AssignAction"));
        view.Dispose();
        Assert.False(source.ShortcutEditor.CaptureKeyboardKeyDown(System.Windows.Input.Key.F7, System.Windows.Input.ModifierKeys.None));
        Assert.Null(rows.ItemsSource);
        source.StorageFolder.Text = "later";
        Assert.Null(view.Content);
    });

    [Fact]
    public Task ConfigurationFiltersAndEditUseActualRowsAcrossBothSources() => sta.RunAsync(async () =>
    {
        var module = new Module();
        var source = new EmulationPreferencesSection([module.Service]);
        var edits = new List<IEmulationConfiguration>();
        source.EditConfigurationRequested += (_, configuration) => { edits.Add(configuration); return Task.CompletedTask; };
        using var view = new EmulationPreferencesView(source, () => { });
        await source.EnsureConfigurationsAsync();
        var loaded = module.Loads;
        await source.EnsureConfigurationsAsync();
        Assert.Equal(loaded, module.Loads);
        var rows = (ListBox)view.FindName("Configurations");
        Assert.Equal(3, rows.Items.Count);
        var machines = (ComboBox)view.FindName("Machines");
        machines.SelectedIndex = 1;
        Assert.Equal("a", source.SelectedMachineId);
        Assert.Equal(2, rows.Items.Count);
        rows.SelectedIndex = 1;
        var selected = Assert.IsType<EmulationConfigurationTableRow>(rows.SelectedItem);
        ((Button)view.FindName("EditAction")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Same(selected.Configuration, Assert.Single(edits));
        machines.SelectedIndex = 0;
        Assert.Null(source.SelectedMachineId);
        Assert.Equal(3, rows.Items.Count);
        view.Dispose();
        source.SelectMachine("b");
        Assert.Null(rows.ItemsSource);
    });

    [Fact]
    public Task OnePendingConfigurationLoadIsSharedUntilItsOwnedWorkFinishes() => sta.RunAsync(async () =>
    {
        var module = new Module { LoadGate = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        var source = new EmulationPreferencesSection([module.Service]);
        var first = source.EnsureConfigurationsAsync();
        try
        {
            Assert.Same(first, source.EnsureConfigurationsAsync());
            Assert.Same(first, source.WaitForConfigurationsAsync());
            Assert.False(first.IsCompleted);
        }
        finally { module.LoadGate.TrySetResult(); }
        await first;
        Assert.Equal(1, module.Loads);
        Assert.Equal(3, source.ConfigurationRows.Count);
    });

    [Fact]
    public Task NetworkTableShowsEveryFieldAndFollowsTheExistingInMemoryRegistryWithoutSelection() => sta.RunAsync(async () =>
    {
        var id = "network-view-" + Guid.NewGuid().ToString("N");
        var endpoint = new EmulationNetworkEndpoint(id, Guid.NewGuid(), "machine", "ethernet",
            EmulationNetworkInterfaceType.Ethernet, EmulationNetworkType.Ethernet, [EmulationNetworkProtocol.Ipv4],
            EmulationNetworkTransferFormat.EthernetIIFrame, EmulationNetworkTransport.Udp, IPAddress.Loopback, 32768);
        using var registration = EmulationSdk.Networks.Register(endpoint, _ => EmulationNetworkCompatibility.Compatible);
        using var view = new NetworkView();
        view.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
        Arrange(view);
        Assert.Empty(Descendants(view).OfType<ListBox>());
        var rows = Descendants(view).OfType<ItemsControl>().Single();
        Assert.Contains(rows.Items.OfType<GWGUI.App.Contracts.Emulation.Network.EmulationNetworkRow>(), row => row.Endpoint.ModuleId == id && row.Port == 32768 && row.IsRunning);
        Assert.All(Descendants(view).OfType<CheckBox>(), box => { Assert.False(box.IsHitTestVisible); Assert.False(box.Focusable); });
        registration.Update(endpoint with { Port = 32769 });
        await Dispatcher.Yield(DispatcherPriority.ContextIdle);
        Assert.Contains(rows.Items.OfType<GWGUI.App.Contracts.Emulation.Network.EmulationNetworkRow>(), row => row.Endpoint.ModuleId == id && row.Port == 32769);
        view.Dispose();
        registration.Update(endpoint with { Port = 32770 });
        await Dispatcher.Yield(DispatcherPriority.ContextIdle);
        Assert.Null(rows.ItemsSource);
    });

    [Fact]
    public Task SystemCommandUsesTheExistingEmulationPreferencesRequest() => sta.Run(() =>
    {
        var sources = new Windows31WorkspaceTests.Sources();
        var menu = sources.Root.Children.OfType<GWGUI.App.Views.Controls.Shell.MainMenu>().Single();
        var count = 0;
        menu.EmulationPreferencesRequested += (_, _) => count++;
        using var workspace = new Windows31Workspace(sources.Root, sources.Tabs);
        workspace.Desktop.Open(Windows31ApplicationId.EmulationPreferences);
        Assert.Equal(1, count);
        Assert.Empty(workspace.Desktop.OpenApplications);
    });

    internal sealed class Module
    {
        internal string Id { get; } = "preferences-" + Guid.NewGuid().ToString("N");
        internal int Loads { get; private set; }
        internal TaskCompletionSource? LoadGate { get; set; }
        internal IEmulationModule Service { get; }
        internal Module()
        {
            var saved = new IEmulationConfiguration[]
            {
                new Windows31EmulationViewTests.Configuration(Id, Guid.NewGuid(), "a"),
                new Windows31EmulationViewTests.Configuration(Id, Guid.NewGuid(), "a"),
                new Windows31EmulationViewTests.Configuration(Id, Guid.NewGuid(), "b")
            };
            Service = ControlledDependencies.Simulate<IEmulationModule>((method, args) => method.Name switch
            {
                "get_Id" => Id,
                "get_DisplayResourceKey" => "Emulation.Configuration.Brand",
                "get_BrandImagePath" => string.Empty,
                "get_Machines" => new EmulationMachineDefinition[] { new("a", "Emulation.Machine.Model"), new("b", "Emulation.Machine.Model") },
                "LoadConfigurationsAsync" => Load(),
                "Describe" => new EmulationMachineSettings((string)args[0]!, new(new Dictionary<EmulationMachineTab, bool>()), []),
                _ => throw new InvalidOperationException(method.Name)
            });
            async ValueTask<IReadOnlyList<IEmulationConfiguration>> Load()
            {
                Loads++;
                if (LoadGate is { } gate) await gate.Task;
                return saved;
            }
        }
    }

    internal static void Arrange(FrameworkElement view)
    {
        view.Measure(new Size(900, 580));
        view.Arrange(new Rect(0, 0, 900, 580));
        view.UpdateLayout();
    }
    internal static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            yield return child;
            foreach (var next in Descendants(child)) yield return next;
        }
    }
}
