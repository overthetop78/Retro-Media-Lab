using System.Windows;
using System.Windows.Controls;
using GWGUI.App.Theme.OS.Editions.Windows31.Views;
using GWGUI.App.Views.Controls.Emulation.Machine;
using GWGUI.App.Localization.Extensions;
using GWGUI.Emulation;
using GWGUI.Emulation.Contracts;
using GWGUI.Emulation.Interfaces;
using GWGUI.Tests.Application.TestInfrastructure;

namespace GWGUI.Tests.Interface.Themes;

[Collection("WPF")]
public sealed class Windows31EmulationViewTests(StaExecutionScenarios sta)
{
    [Fact]
    public Task ExistingConfigurationsRefreshAndOpenThroughTheOriginalSelection() => sta.RunAsync(async () =>
    {
        var module = new Module();
        var first = new Configuration(module.Id, Guid.NewGuid(), "first");
        var second = new Configuration(module.Id, Guid.NewGuid(), "second");
        module.Saved.AddRange([first, second]);
        var source = new EmulationSection([module.Service]);
        await source.ReloadConfigurationsAsync();
        var errors = new List<Exception>();
        using var view = new EmulationView(source, () => { }, errors.Add);
        var list = Field<ListBox>(view, "Configurations");
        var open = Field<MenuItem>(view, "OpenCommand");
        Assert.Equal(2, list.Items.Count);
        Assert.True(open.IsEnabled);
        var tabs = GWGUI.Tests.Interface.EmulationViews.MachineConfigurationScenarios.Controls<TabControl>(source).Single();
        var originalParent = tabs.Parent;
        var screen = new Grid();
        var runtime = new EmulationMachineRuntime(second, _ => throw new InvalidOperationException("No core may start"), [], [], "Emulation.Machine.Model", false);
        await source.AddMachineAsync(source.Configurations[1], runtime, screen, () => Task.CompletedTask);
        var machineTab = Assert.IsType<TabItem>(tabs.SelectedItem);
        try
        {
            tabs.SelectedIndex = 0;
            list.SelectedIndex = 1;
            open.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Assert.Same(machineTab, tabs.SelectedItem);
            Assert.Same(originalParent, tabs.Parent);
            Assert.Same(screen, tabs.SelectedContent);
            module.Saved.Remove(first);
            Field<MenuItem>(view, "RefreshCommand").RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Assert.Single(list.Items);
            var selectedRow = Assert.IsType<ListBoxItem>(list.SelectedItem);
            var selected = Assert.IsType<GWGUI.App.Contracts.Emulation.Configurations.EmulationConfigurationListItem>(selectedRow.Tag);
            Assert.Equal(second.Id, selected.Configuration.Id);
            Assert.Empty(errors);
        }
        finally { await source.CloseMachineAsync((module.Id, second.Id), machineTab, () => Task.CompletedTask); }
    });

    [Fact]
    public Task EmptyListAndDisposedPresentationCannotOpenOrRetainConfigurations() => sta.RunAsync(async () =>
    {
        var module = new Module();
        var source = new EmulationSection([module.Service]);
        await source.ReloadConfigurationsAsync();
        var closeCalls = 0;
        var view = new EmulationView(source, () => closeCalls++, error => throw error);
        var list = Field<ListBox>(view, "Configurations");
        var refresh = Field<MenuItem>(view, "RefreshCommand");
        var close = Field<MenuItem>(view, "CloseCommand");
        try
        {
            Assert.False(Field<MenuItem>(view, "OpenCommand").IsEnabled);
            module.Saved.Add(new Configuration(module.Id, Guid.NewGuid(), "new"));
            await source.ReloadConfigurationsAsync();
            Assert.Single(list.Items);
        }
        finally { view.Dispose(); }
        var calls = module.LoadCalls;
        refresh.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        close.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Assert.Equal(calls, module.LoadCalls);
        await source.ReloadConfigurationsAsync();
        Assert.Empty(list.Items);
        Assert.Equal(0, closeCalls);
        Assert.Null(view.Content);
    });

    private static T Field<T>(EmulationView view, string name) => Assert.IsType<T>(view.FindName(name));

    internal sealed record Configuration(string ModuleId, Guid Id, string MachineId) : IEmulationConfiguration;

    internal sealed class Module
    {
        internal string Id { get; } = "theme-test-" + Guid.NewGuid().ToString("N");
        internal List<IEmulationConfiguration> Saved { get; } = [];
        internal int LoadCalls { get; private set; }
        internal IEmulationModule Service { get; }

        internal Module(string brandResource = "Emulation.Configuration.Brand")
        {
            Service = ControlledDependencies.Simulate<IWindows31TestLocalizedModule>((method, args) =>
            {
                if (method.Name == nameof(IEmulationModuleLocalization.TryGetString))
                {
                    args[2] = brandResource.Contains('.') ? LocExtension.Get(brandResource) : brandResource;
                    return Equals(args[0], brandResource);
                }
                return method.Name switch
                {
                    "get_Id" => Id,
                    "get_DisplayResourceKey" => brandResource,
                    "get_BrandImagePath" => string.Empty,
                    "get_Machines" => Saved.Select(item => new EmulationMachineDefinition(item.MachineId, "Emulation.Machine.Model")).ToArray(),
                    "LoadConfigurationsAsync" => Load(),
                    "SummarizeConfiguration" => new EmulationConfigurationSummary("Emulation.Machine.Model", [((IEmulationConfiguration)args[0]!).MachineId]),
                    _ => throw new InvalidOperationException(method.Name)
                };
            });
        }

        private ValueTask<IReadOnlyList<IEmulationConfiguration>> Load()
        {
            LoadCalls++;
            return ValueTask.FromResult<IReadOnlyList<IEmulationConfiguration>>(Saved.ToArray());
        }
    }
}

public interface IWindows31TestLocalizedModule : IEmulationModule, IEmulationModuleLocalization;
