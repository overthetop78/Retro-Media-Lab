using System.Windows;
using System.Windows.Controls;
using RetroMediaLab.App.Theme.OS.Editions.Windows95.Views;
using Module = RetroMediaLab.Tests.Interface.Themes.Windows31EmulationViewTests.Module;
using Configuration = RetroMediaLab.Tests.Interface.Themes.Windows31EmulationViewTests.Configuration;
using RetroMediaLab.App.Views.Controls.Emulation.Machine;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.Emulation;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Interfaces;
using RetroMediaLab.Tests.Application.TestInfrastructure;

namespace RetroMediaLab.Tests.Interface.Themes;

[Collection("WPF")]
public sealed class Windows95EmulationViewTests(StaExecutionScenarios sta)
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
        var tabs = RetroMediaLab.Tests.Interface.EmulationViews.MachineConfigurationScenarios.Controls<TabControl>(source).Single();
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
            var selected = Assert.IsType<RetroMediaLab.App.Contracts.Emulation.Configurations.EmulationConfigurationListItem>(selectedRow.Tag);
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

}
