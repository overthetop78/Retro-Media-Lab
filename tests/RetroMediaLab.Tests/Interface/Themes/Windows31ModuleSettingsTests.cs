using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using RetroMediaLab.App.Theme.OS.Editions.Windows31.Views;
using RetroMediaLab.App.Views.Controls.Emulation.Options;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.Tests.Application.TestInfrastructure;
using RetroMediaLab.Tests.Interface.EmulationViews;
using RetroMediaLab.VideoPresentation.Services;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Interfaces;

namespace RetroMediaLab.Tests.Interface.Themes;

[Collection("WPF")]
public sealed class Windows31ModuleSettingsTests(StaExecutionScenarios sta)
{
    [Fact]
    public Task MemorySummaryFollowsTheExistingCalculationWhenRadioCapacityChanges() => sta.RunAsync(async () =>
    {
        var module = new MachineConfigurationScenarios.Module();
        var original = module.Service;
        module.Service = ControlledDependencies.Simulate<IEmulationModule>((method, arguments) =>
        {
            var result = method.Invoke(original, arguments);
            if (method.Name != "Describe") return result;
            var settings = (EmulationMachineSettings)result!;
            var tabs = settings.Visibility.Tabs.ToDictionary(pair => pair.Key, pair => pair.Value);
            tabs[EmulationMachineTab.Ram] = true;
            var memory = new EmulationSettingsBlock("memory", EmulationMachineTab.Ram, "Emulation.Memory.Main",
                [new EmulationSettingsField("memory", EmulationMachineTab.Ram, "memory", "Emulation.Memory.Main",
                    EmulationSettingsEditor.Selection, "small",
                    [new("small", string.Empty, "16 KiB", 16 * 1024), new("large", string.Empty, "32 KiB", 32 * 1024)],
                    UseRadioButtons: true)]);
            return settings with { Visibility = new EmulationSettingsVisibility(tabs), Blocks = settings.Blocks.Append(memory).ToArray() };
        });
        var profiles = new VideoPresentationProfileStore("virtual-video", fileSystem: new MemoryVideoProfileFiles(),
            schedule: action => { action(); return Task.CompletedTask; });
        var source = new EmulationModuleSettingsSection(module.Service, profiles, error => throw error);
        ModuleSettingsView? view = null;
        try
        {
            view = new ModuleSettingsView(source, () => { });
            var categories = (ListBox)view.FindName("Categories");
            categories.SelectedItem = categories.Items.Cast<ListBoxItem>().Single(item => Equals(item.Tag, EmulationMachineTab.Ram));
            await Dispatcher.Yield(DispatcherPriority.ContextIdle);
            Windows31EmulationPreferencesTests.Arrange(view);
            var total = Assert.IsType<TextBlock>(source.PresentationMemoryTotal);
            Assert.Contains("16", total.Text);
            var summary = Assert.Single(Windows31EmulationPreferencesTests.Descendants(view).OfType<TextBlock>(), item => item.Text == total.Text);
            Windows31EmulationPreferencesTests.Descendants(view).OfType<RadioButton>().Last().IsChecked = true;
            await Dispatcher.Yield(DispatcherPriority.ContextIdle);
            Assert.Contains("32", total.Text);
            Assert.Equal(total.Text, summary.Text);
            Assert.Empty(view.PendingFields);
        }
        finally
        {
            try { view?.Dispose(); }
            finally
            {
                try { await source.DisposeAsync(); }
                finally { module.Cleanup(); }
            }
        }
    });

    [Fact]
    public Task FunctionalFoldersRemainEditableWithoutReportingExcludedDefaultsAsMissing() => sta.RunAsync(async () =>
    {
        var module = new MachineConfigurationScenarios.Module(includeDirectories: true);
        var profiles = new VideoPresentationProfileStore("virtual-video", fileSystem: new MemoryVideoProfileFiles(),
            schedule: action => { action(); return Task.CompletedTask; });
        var source = new EmulationModuleSettingsSection(module.Service, profiles, error => throw error);
        ModuleSettingsView? view = null;
        try
        {
            view = new ModuleSettingsView(source, () => { });
            Windows31EmulationPreferencesTests.Arrange(view);
            var text = Windows31EmulationPreferencesTests.Descendants(view).OfType<TextBox>().ToArray();
            var folder = Assert.Single(text, item => item.Text == "selected-codes");
            Assert.DoesNotContain(text, item => item.Text == "default-images");
            Assert.Empty(view.PendingFields);
            folder.Text = "new-codes";
            await Dispatcher.Yield(DispatcherPriority.ContextIdle);
            Assert.Equal("new-codes", Assert.Single(MachineConfigurationScenarios.Controls<TextBox>(source.PresentationFields["codes"])).Text);
        }
        finally
        {
            try { view?.Dispose(); }
            finally
            {
                try { await source.DisposeAsync(); }
                finally { module.Cleanup(); }
            }
        }
    });

    [Fact]
    public Task ActualModuleFieldsCreateAndModifyTheSameConfiguration() => sta.RunAsync(async () =>
    {
        var module = new MachineConfigurationScenarios.Module();
        var files = new MemoryVideoProfileFiles();
        var profiles = new VideoPresentationProfileStore("virtual-video", fileSystem: files,
            schedule: action => { action(); return Task.CompletedTask; });
        var source = new EmulationModuleSettingsSection(module.Service, profiles, error => throw error);
        ModuleSettingsView? view = null;
        try
        {
            view = new ModuleSettingsView(source, () => { });
            Assert.Equal(2, ((ComboBox)view.FindName("Machines")).Items.Count);
            Assert.Empty(view.PendingFields);
            Windows31EmulationPreferencesTests.Arrange(view);
            var toggle = Windows31EmulationPreferencesTests.Descendants(view).OfType<CheckBox>().Single();
            toggle.IsChecked = true;
            await Dispatcher.Yield(DispatcherPriority.ContextIdle);
            Assert.Equal("on", ((MachineConfigurationScenarios.Configuration)source.CurrentConfiguration).Value);
            Assert.Empty(module.Saved);
            ((Button)view.FindName("CreateAction")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Dispatcher.Yield(DispatcherPriority.ContextIdle);
            Assert.Equal("on", ((MachineConfigurationScenarios.Configuration)Assert.Single(module.Saved)).Value);
            Assert.Equal(Visibility.Collapsed, ((Button)view.FindName("CreateAction")).Visibility);
            Assert.Empty(view.PendingFields);
            var fields = source.PresentationFields;
            Assert.NotEmpty(fields);
            view.Dispose();
            Assert.Null(view.Content);
            await source.ReloadWhenOpenedAsync();
            Assert.Null(view.Content);
        }
        finally
        {
            try { view?.Dispose(); }
            finally
            {
                try { await source.DisposeAsync(); }
                finally { module.Cleanup(); }
            }
        }
        Assert.Empty(source.PresentationFields);
        Assert.Empty(source.PresentationTabs);
    });

    [Fact]
    public Task CategoriesUseTheRealModuleVisibilityAndKeepReadOnlyFieldsDisabled() => sta.RunAsync(async () =>
    {
        var module = new MachineConfigurationScenarios.Module();
        var profiles = new VideoPresentationProfileStore("virtual-video", fileSystem: new MemoryVideoProfileFiles(),
            schedule: action => { action(); return Task.CompletedTask; });
        var source = new EmulationModuleSettingsSection(module.Service, profiles, error => throw error);
        ModuleSettingsView? view = null;
        try
        {
            view = new ModuleSettingsView(source, () => { });
            var categories = (ListBox)view.FindName("Categories");
            Assert.Equal(source.PresentationTabs, categories.Items.Cast<ListBoxItem>().Select(item => (EmulationMachineTab)item.Tag));
            categories.SelectedItem = categories.Items.Cast<ListBoxItem>().Single(item => Equals(item.Tag, EmulationMachineTab.Cpu));
            await Dispatcher.Yield(DispatcherPriority.ContextIdle);
            Assert.Equal(EmulationMachineTab.Cpu, source.PresentationTab);
            Windows31EmulationPreferencesTests.Arrange(view);
            var text = Windows31EmulationPreferencesTests.Descendants(view).OfType<TextBox>().Single();
            Assert.False(text.IsEnabled);
            Assert.Equal("fixed", text.Text);
            Assert.Empty(view.PendingFields);
            Assert.DoesNotContain(EmulationMachineTab.Video, source.PresentationTabs);
        }
        finally
        {
            try { view?.Dispose(); }
            finally
            {
                try { await source.DisposeAsync(); }
                finally { module.Cleanup(); }
            }
        }
    });
}
