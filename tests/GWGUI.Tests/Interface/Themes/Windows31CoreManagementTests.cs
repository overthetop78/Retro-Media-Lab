using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using GWGUI.App.Controllers.Emulation.Options;
using GWGUI.App.Localization.Extensions;
using GWGUI.App.Theme.OS.Editions.Windows31.Views;
using GWGUI.App.Views.Controls.Emulation.Options;
using GWGUI.Emulation.Contracts;
using GWGUI.Emulation.Interfaces;
using GWGUI.Tests.Application.TestInfrastructure;

namespace GWGUI.Tests.Interface.Themes;

[Collection("WPF")]
public sealed class Windows31CoreManagementTests(StaExecutionScenarios sta)
{
    [Fact]
    public Task ControllerSelectionSearchInstallProgressAndSavedLockFollowTheExistingOperations() => sta.RunAsync(async () =>
    {
        var manager = new Manager();
        var configuration = new Windows31EmulationViewTests.Configuration("core-view", Guid.NewGuid(), "machine");
        var saved = false;
        var controller = new EmulationEmulatorManagementController(manager.Service, () => configuration, _ => { }, () => saved);
        var panel = (EmulationCoreManagementPanel)controller.CreateView(new Button());
        CoreManagementView? view = null;
        try
        {
            view = new CoreManagementView(panel);
            await controller.RefreshAsync();
            var emulators = (ComboBox)view.FindName("Emulators");
            Assert.Equal(2, emulators.Items.Count);
            Assert.True(emulators.IsEnabled);
            emulators.SelectedIndex = 1;
            await Dispatcher.Yield(DispatcherPriority.ContextIdle);
            Assert.Equal("second", manager.Selected);
            ((Button)view.FindName("Search")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Dispatcher.Yield(DispatcherPriority.ContextIdle);
            var versions = (ComboBox)view.FindName("Versions");
            Assert.Equal(2, versions.Items.Count);
            Assert.Equal(Visibility.Visible, versions.Visibility);
            ((Button)view.FindName("Download")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Dispatcher.Yield(DispatcherPriority.ContextIdle);
            Assert.False(((Button)view.FindName("Search")).IsEnabled);
            Assert.Equal(Visibility.Visible, ((Button)view.FindName("Cancel")).Visibility);
            Assert.Equal(.5, ((ProgressBar)view.FindName("Progress")).Value);
            var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            DependencyPropertyChangedEventHandler completed = (_, _) =>
            {
                if (panel.Search.IsEnabled) finished.TrySetResult();
            };
            panel.Search.IsEnabledChanged += completed;
            try
            {
                manager.Download.TrySetResult("virtual-core");
                await finished.Task.WaitAsync(TimeSpan.FromSeconds(5));
            }
            finally { panel.Search.IsEnabledChanged -= completed; }
            Assert.True(((Button)view.FindName("Search")).IsEnabled);
            Assert.Equal(Visibility.Collapsed, ((Button)view.FindName("Cancel")).Visibility);
            Assert.Contains("2.0", ((TextBlock)view.FindName("Installed")).Text);
            saved = true;
            await controller.RefreshAsync();
            Assert.False(emulators.IsEnabled);
            panel.SetStatus("failure", true);
            Assert.True(view.StatusIsError);
            view.Dispose();
            panel.SetStatus("later", false);
            Assert.True(view.StatusIsError);
            Assert.Null(emulators.ItemsSource);
        }
        finally
        {
            try { view?.Dispose(); }
            finally { await controller.DisposeAsync(); }
        }
    });

    [Fact]
    public Task CancelAndControllerDisposalEndTheOwnedInstallation() => sta.RunAsync(async () =>
    {
        var manager = new Manager();
        var configuration = new Windows31EmulationViewTests.Configuration("core-cancel", Guid.NewGuid(), "machine");
        var controller = new EmulationEmulatorManagementController(manager.Service, () => configuration, _ => { }, () => false);
        var panel = (EmulationCoreManagementPanel)controller.CreateView(new Button());
        using var view = new CoreManagementView(panel);
        try
        {
            await controller.RefreshAsync();
            ((Button)view.FindName("Search")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Dispatcher.Yield(DispatcherPriority.ContextIdle);
            ((Button)view.FindName("Download")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            ((Button)view.FindName("Cancel")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Dispatcher.Yield(DispatcherPriority.ContextIdle);
            Assert.True(manager.Cancelled);
            Assert.Equal(LocExtension.Get("Operation.Cancelled"), ((TextBlock)view.FindName("Status")).Text);
            Assert.False(manager.Installing);
        }
        finally { await controller.DisposeAsync(); }
    });

    internal sealed class Manager
    {
        internal string Selected = "first";
        private string version = "1.0";
        internal readonly TaskCompletionSource<string> Download = new();
        internal bool Installing;
        internal bool Cancelled;
        internal IEmulationEmulatorManager Service { get; }
        internal Manager()
        {
            Service = ControlledDependencies.Simulate<IEmulationEmulatorManager>((method, args) => method.Name switch
            {
                "GetEmulatorInstallationsAsync" => ValueTask.FromResult<IReadOnlyList<EmulationEmulatorInstallation>>([Installation("first"), Installation("second")]),
                "GetEmulatorInstallationAsync" => ValueTask.FromResult(Installation(Selected)),
                "UseEmulatorAsync" => Use((IEmulationConfiguration)args[0]!, (string)args[1]!),
                "FindEmulatorReleasesAsync" => ValueTask.FromResult<IReadOnlyList<EmulationEmulatorRelease>>([new("v2", "2.0", "2.0", true), new("v1", "1.0", "1.0")]),
                "InstallEmulatorAsync" => Install((IProgress<double>?)args[2], (CancellationToken)args[3]!),
                _ => throw new InvalidOperationException(method.Name)
            });
        }
        private EmulationEmulatorInstallation Installation(string id) => new(
            new EmulationEmulatorDefinition(id, id, "Emulation.Core.Emulator", new HashSet<string> { "machine" }), version);
        private ValueTask<IEmulationConfiguration> Use(IEmulationConfiguration configuration, string id)
        {
            Selected = id;
            return ValueTask.FromResult(configuration);
        }
        private async ValueTask<string> Install(IProgress<double>? progress, CancellationToken token)
        {
            Installing = true;
            try
            {
                progress?.Report(.5);
                var path = await Download.Task.WaitAsync(token);
                version = "2.0";
                return path;
            }
            catch (OperationCanceledException) { Cancelled = true; throw; }
            finally { Installing = false; }
        }
    }
}
