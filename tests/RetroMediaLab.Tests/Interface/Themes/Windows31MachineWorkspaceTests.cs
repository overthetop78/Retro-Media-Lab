using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RetroMediaLab.App.Contracts.Emulation.Configurations;
using System.Windows.Controls;
using RetroMediaLab.App.Contracts.Machine;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.App.Theme.OS.Editions.Windows31;
using RetroMediaLab.App.Views.Controls.Emulation.Machine;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Interfaces;
using RetroMediaLab.Tests.Application.TestInfrastructure;
using RetroMediaLab.Tests.Emulation.Video;
using RetroMediaLab.VideoPresentation.Enums;
using Windows31MachineView = RetroMediaLab.App.Theme.OS.Editions.Windows31.Views.MachineView;

namespace RetroMediaLab.Tests.Interface.Themes;

[Collection("WPF")]
public sealed class Windows31MachineWorkspaceTests(StaExecutionScenarios sta)
{
    [Fact]
    public Task DesktopHostsTwoMachinesAndRestoresTheRemainingSessionWhenRemoved() => sta.RunAsync(async () =>
    {
        var module = new Windows31EmulationViewTests.Module();
        var first = new Windows31EmulationViewTests.Configuration(module.Id, Guid.NewGuid(), "first");
        var second = new Windows31EmulationViewTests.Configuration(module.Id, Guid.NewGuid(), "second");
        module.Saved.AddRange([first, second]);
        var source = new EmulationSection([module.Service]);
        await source.ReloadConfigurationsAsync();
        var firstFixture = new MachineFixture();
        var secondFixture = new MachineFixture();
        await using var firstController = await firstFixture.CreateAsync();
        await using var secondController = await secondFixture.CreateAsync();
        await source.AddMachineAsync(source.Configurations[0], Runtime(first), firstController, firstController.StopAsync);
        await source.AddMachineAsync(source.Configurations[1], Runtime(second), secondController, secondController.StopAsync);
        using var desktop = new Windows31Desktop(current => { current.ConnectMachines(source); return []; });
        try
        {
            Assert.Equal(2, desktop.OpenMachineWindows.Count);
            Assert.IsType<Windows31MachineView>(firstController.Presentation);
            Assert.IsType<Windows31MachineView>(secondController.Presentation);
            var firstWindow = desktop.OpenMachineWindows.Single(item => ReferenceEquals(item.Controller, firstController)).Placement;
            var secondWindow = desktop.OpenMachineWindows.Single(item => ReferenceEquals(item.Controller, secondController)).Placement;
            firstWindow.Iconify();
            await source.RequestOpenAsync(source.Configurations[0]);
            Assert.True(firstWindow.IsVisible);
            Assert.True(firstWindow.IsActive);
            Assert.False(secondWindow.IsActive);
            firstWindow.Maximize();
            Assert.Equal(Windows31WindowState.Maximized, firstWindow.State);
            await desktop.CloseMachineWindowAsync(firstController);
            Assert.Same(secondController, Assert.Single(desktop.OpenMachineWindows).Controller);
            Assert.Equal(1, firstFixture.Disposals);
            desktop.Dispose();
            Assert.Empty(desktop.OpenMachineWindows);
            Assert.IsType<MachineView>(secondController.Content);
            Assert.Same(secondFixture.Machine, secondController.Machine);
            Assert.Equal(0, secondFixture.Disposals);
            Assert.Single(source.OpenedMachines);
        }
        finally
        {
            desktop.Dispose();
            await source.StopAllAsync();
        }
    });

    [Fact]
    public Task DesktopTracksMachinesOpenedLaterAndClosesTheirOwnerFromWindowState() => sta.RunAsync(async () =>
    {
        var module = new Windows31EmulationViewTests.Module();
        var configuration = new Windows31EmulationViewTests.Configuration(module.Id, Guid.NewGuid(), "machine");
        module.Saved.Add(configuration);
        var source = new EmulationSection([module.Service]);
        await source.ReloadConfigurationsAsync();
        var fixture = new MachineFixture();
        await using var controller = await fixture.CreateAsync();
        using var desktop = new Windows31Desktop([]);
        desktop.ConnectMachines(source);
        try
        {
            Assert.Empty(desktop.OpenMachineWindows);
            await source.AddMachineAsync(source.Configurations[0], Runtime(configuration), controller, controller.StopAsync);
            var window = Assert.Single(desktop.OpenMachineWindows);
            Assert.Same(controller, window.Controller);
            window.Placement.Close();
            Assert.Empty(source.OpenedMachines);
            Assert.Empty(desktop.OpenMachineWindows);
            Assert.Equal(1, fixture.Disposals);
        }
        finally { desktop.Dispose(); await source.StopAllAsync(); }
    });

    [Fact]
    public Task ProgramGroupListsStoppedConfigurationsAndRefreshesWithoutAnIntermediateApplication() => sta.RunAsync(async () =>
    {
        var module = new Windows31EmulationViewTests.Module();
        var first = new Windows31EmulationViewTests.Configuration(module.Id, Guid.NewGuid(), "first");
        var second = new Windows31EmulationViewTests.Configuration(module.Id, Guid.NewGuid(), "second");
        module.Saved.AddRange([first, second]);
        var source = new EmulationSection([module.Service]);
        await source.ReloadConfigurationsAsync();
        using var desktop = new Windows31Desktop(current => { current.ConnectMachines(source); return []; });
        desktop.Measure(new Size(1280, 800));
        desktop.Arrange(new Rect(0, 0, 1280, 800));
        desktop.UpdateLayout();
        var manager = Descendants(desktop).OfType<Windows31ProgramManager>().Single();
        var groupIcons = Assert.IsType<ListBox>(manager.FindName("GroupIcons"));
        groupIcons.SelectedItem = Assert.Single(groupIcons.Items.Cast<ListBoxItem>(), item => Equals(item.Tag, Windows31ProgramGroup.Emulation));
        groupIcons.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left) { RoutedEvent = Control.MouseDoubleClickEvent });
        desktop.UpdateLayout();
        var machines = Descendants(manager).OfType<ListBox>().Single(list => list.Items.OfType<ListBoxItem>().Any(item => item.Tag is EmulationConfigurationListItem));
        Assert.Equal(new[] { first.Id, second.Id }, machines.Items.Cast<ListBoxItem>().Select(item => ((EmulationConfigurationListItem)item.Tag).Configuration.Id));
        Assert.Empty(source.OpenedMachines);
        Assert.Empty(desktop.OpenApplications);
        module.Saved.Remove(first);
        await source.ReloadConfigurationsAsync();
        Assert.Equal(second.Id, ((EmulationConfigurationListItem)Assert.Single(machines.Items.Cast<ListBoxItem>()).Tag).Configuration.Id);
        var fixture = new MachineFixture();
        await using var controller = await fixture.CreateAsync();
        try
        {
            await source.AddMachineAsync(source.Configurations.Single(), Runtime(second), controller, controller.StopAsync);
            var placement = Assert.Single(desktop.OpenMachineWindows).Placement;
            placement.Iconify();
            machines.SelectedIndex = 0;
            Assert.IsType<MenuItem>(manager.FindName("OpenCommand")).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Assert.True(placement.IsVisible);
            Assert.True(placement.IsActive);
            Assert.Equal(0, fixture.Disposals);
            Assert.Empty(desktop.OpenApplications);
        }
        finally { desktop.Dispose(); await source.StopAllAsync(); }
    });

    [Fact]
    public Task MachineArtworkIsUsedDirectlyAtIconSizeWithoutLosingItsAlphaOrAspectRatio() => sta.Run(() =>
    {
        using var manager = new Windows31ProgramManager([], _ => { });
        var pixels = new byte[64 * 16 * 4];
        pixels[3] = 128;
        var artwork = BitmapSource.Create(64, 16, 96, 96, PixelFormats.Bgra32, null, pixels, 64 * 4);
        artwork.Freeze();
        var item = manager.CreateIcon("Machine", artwork, Windows31ApplicationId.Emulation, literalTitle: true);
        var image = Assert.Single(Assert.IsType<StackPanel>(item.Content).Children.OfType<Image>());
        Assert.Same(artwork, image.Source);
        Assert.Equal(Windows31Metrics.IconImageSize, image.Width);
        Assert.Equal(Windows31Metrics.IconImageSize, image.Height);
        Assert.Equal(Stretch.Uniform, image.Stretch);
        Assert.Equal(PixelFormats.Bgra32, Assert.IsAssignableFrom<BitmapSource>(image.Source).Format);
    });

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            yield return child;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }

    [Fact]
    public Task DeviceWindowIsUniqueAndDetachesItsBorrowedPanelWhenDevicesOrMachineDisappear() => sta.RunAsync(async () =>
    {
        var module = new Windows31EmulationViewTests.Module();
        var configuration = new Windows31EmulationViewTests.Configuration(module.Id, Guid.NewGuid(), "machine");
        module.Saved.Add(configuration);
        var source = new EmulationSection([module.Service]);
        await source.ReloadConfigurationsAsync();
        var fixture = new MachineFixture();
        await using var controller = await fixture.CreateAsync();
        using var desktop = new Windows31Desktop(current => { current.ConnectMachines(source); return []; });
        try
        {
            await source.AddMachineAsync(source.Configurations.Single(), Runtime(configuration), controller, controller.StopAsync);
            var view = Assert.IsType<Windows31MachineView>(controller.Presentation);
            var device = new MachineViewDevice("cassette", "Tape drive", string.Empty, true, true, () => Task.CompletedTask, null);
            view.SetDevices([device], error => throw error, () => { });
            var footer = Assert.IsType<StackPanel>(view.FindName("Devices"));
            var launch = Assert.Single(Assert.IsType<StackPanel>(footer.Children[0]).Children.OfType<Button>());
            var panel = view.DevicePanel(device.Key)!;
            launch.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            desktop.Measure(new Size(1280, 800));
            desktop.Arrange(new Rect(0, 0, 1280, 800));
            desktop.UpdateLayout();
            var frame = Assert.Single(Descendants(desktop).OfType<Windows31Frame>(), item => item.Title == device.Label);
            Assert.NotNull(panel.Parent);
            launch.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Same(frame, Assert.Single(Descendants(desktop).OfType<Windows31Frame>(), item => item.Title == device.Label));
            view.SetDevices([device], error => throw error, () => { });
            Assert.Null(panel.Parent);
            Assert.Null(frame.Content);
            launch = Assert.Single(Assert.IsType<StackPanel>(footer.Children[0]).Children.OfType<Button>());
            var replacement = view.DevicePanel(device.Key)!;
            launch.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await source.CloseOpenedMachineAsync(controller);
            Assert.Null(replacement.Parent);
            Assert.Equal(1, fixture.Disposals);
        }
        finally { desktop.Dispose(); await source.StopAllAsync(); }
    });

    private static EmulationMachineRuntime Runtime(IEmulationConfiguration configuration) => new(configuration,
        _ => throw new InvalidOperationException("No core may start"), [], [], "Emulation.Machine.Model", false);

    [Fact]
    public Task SourceNotificationsTrackTwoMachinesThroughSelectionAndCompleteClosure() => sta.RunAsync(async () =>
    {
        var module = new Windows31EmulationViewTests.Module();
        var first = new Windows31EmulationViewTests.Configuration(module.Id, Guid.NewGuid(), "first");
        var second = new Windows31EmulationViewTests.Configuration(module.Id, Guid.NewGuid(), "second");
        module.Saved.AddRange([first, second]);
        var source = new EmulationSection([module.Service]);
        await source.ReloadConfigurationsAsync();
        var firstFixture = new MachineFixture();
        var secondFixture = new MachineFixture();
        await using var firstController = await firstFixture.CreateAsync();
        await using var secondController = await secondFixture.CreateAsync();
        var notifications = 0;
        var selected = new List<MachineController>();
        EventHandler changed = (_, _) => notifications++;
        source.PresentationChanged += changed;
        source.MachineSelected += selected.Add;
        try
        {
            await source.AddMachineAsync(source.Configurations[0], Runtime(first), firstController, firstController.StopAsync);
            await source.AddMachineAsync(source.Configurations[1], Runtime(second), secondController, secondController.StopAsync);
            Assert.Equal(new[] { first.Id, second.Id }, source.OpenedMachines.Select(item => item.Configuration.Configuration.Id));
            Assert.Same(secondController, selected.Last());
            source.SelectOpenedMachine(firstController);
            Assert.Same(firstController, selected.Last());
            await source.RequestOpenAsync(source.Configurations[0]);
            Assert.Same(firstController, selected.Last());
            Assert.Equal(0, firstFixture.Disposals);
            await source.CloseOpenedMachineAsync(firstController);
            Assert.Same(secondController, Assert.Single(source.OpenedMachines).Controller);
            Assert.Equal(1, firstFixture.Disposals);
            await source.StopAllAsync();
            Assert.Empty(source.OpenedMachines);
            var tabs = RetroMediaLab.Tests.Interface.EmulationViews.MachineConfigurationScenarios.Controls<TabControl>(source).Single();
            Assert.Single(tabs.Items);
            Assert.Equal(1, secondFixture.Disposals);
            Assert.True(notifications > 0);
        }
        finally
        {
            source.PresentationChanged -= changed;
            source.MachineSelected -= selected.Add;
            await source.StopAllAsync();
        }

        static EmulationMachineRuntime Runtime(IEmulationConfiguration configuration) => new(configuration,
            _ => throw new InvalidOperationException("No core may start"), [], [], "Emulation.Machine.Model", false);
    });

    [Fact]
    public Task LeaseRestoresTheSourceWithoutReplacingItsCoreOrVideoSurface() => sta.RunAsync(async () =>
    {
        var fixture = new MachineFixture();
        await using var controller = await fixture.CreateAsync();
        var parent = new Grid();
        parent.Children.Add(controller);
        var host = new ContentControl();
        var surface = controller.Presentation.VideoHost.Children[0];
        var previousFactory = controller.PresentationFactory;
        var previousActivation = controller.PresentationActivation;
        try
        {
            using var lease = new MachinePresentationLease(controller, host, () => new Windows31MachineView(), () => true);
            var view = Assert.IsType<Windows31MachineView>(controller.Presentation);
            Assert.Same(view, host.Content);
            Assert.Null(controller.Content);
            Assert.Same(parent, controller.Parent);
            Assert.Same(fixture.Machine, controller.Machine);
            Assert.Same(surface, view.VideoHost.Children[0]);
            Assert.Equal(0, fixture.Disposals);
            Assert.Equal(1, fixture.Video.Subscribers);
            lease.Dispose();
            var restored = Assert.IsType<MachineView>(controller.Content);
            Assert.Same(surface, restored.VideoHost.Children[0]);
            Assert.Same(previousFactory, controller.PresentationFactory);
            Assert.Same(previousActivation, controller.PresentationActivation);
            Assert.Same(fixture.Machine, controller.Machine);
            Assert.Null(host.Content);
            Assert.Equal(0, fixture.Disposals);
            Assert.Equal(1, fixture.Video.Subscribers);
        }
        finally
        {
            await controller.StopAsync();
            parent.Children.Clear();
        }
        Assert.Equal(1, fixture.Disposals);
        Assert.Equal(0, fixture.Video.Subscribers);
    });

    [Fact]
    public Task OccupiedHostIsPreservedAndTheFailedLeaseRestoresTheSource() => sta.RunAsync(async () =>
    {
        var fixture = new MachineFixture();
        await using var controller = await fixture.CreateAsync();
        var existing = new TextBlock();
        var host = new ContentControl { Content = existing };
        try
        {
            Assert.Throws<InvalidOperationException>(() => new MachinePresentationLease(controller, host,
                () => new Windows31MachineView(), () => true));
            Assert.Same(existing, host.Content);
            Assert.IsType<MachineView>(controller.Content);
            Assert.Same(controller, controller.PresentationHost);
            Assert.Same(fixture.Machine, controller.Machine);
            Assert.Equal(0, fixture.Disposals);
        }
        finally { host.Content = null; }
    });

    [Fact]
    public Task ClosingTheOwnerDetachesTheBorrowedViewAndLeaseDoesNotRestartIt() => sta.RunAsync(async () =>
    {
        var fixture = new MachineFixture();
        await using var controller = await fixture.CreateAsync();
        var host = new ContentControl();
        using var lease = new MachinePresentationLease(controller, host, () => new Windows31MachineView(), () => true);
        await controller.StopAsync();
        Assert.True(controller.IsStopped);
        Assert.Null(host.Content);
        lease.Dispose();
        Assert.Null(controller.Content);
        Assert.Same(controller, controller.PresentationHost);
        Assert.Equal(1, fixture.Disposals);
        Assert.Equal(0, fixture.Video.Subscribers);
    });

    internal sealed class MachineFixture
    {
        internal VideoProcessingScenarios.VideoSource Video { get; } = new();
        internal IEmulatedMachine Machine { get; }
        internal int Disposals { get; private set; }

        internal MachineFixture()
        {
            var audio = ControlledDependencies.Simulate<IEmulationAudio>((method, _) => method.Name switch
            {
                "get_IsMuted" => false,
                "SetMuted" => null,
                _ => throw new InvalidOperationException(method.Name)
            });
            var input = ControlledDependencies.Simulate<IEmulationInput>((method, _) => method.Name switch
            {
                "get_KeyboardBindings" => new Dictionary<string, string>(),
                "get_SupportsControllerPointerSwitch" => false,
                _ => throw new InvalidOperationException(method.Name)
            });
            var runtime = ControlledDependencies.Simulate<IEmulationRuntime>((method, _) => method.Name == "get_AvailableOptions"
                ? Array.Empty<EmulationOption>() : throw new InvalidOperationException(method.Name));
            var states = ControlledDependencies.Simulate<IEmulationSavedStates>((method, _) => method.Name == "get_IsSupported"
                ? false : throw new InvalidOperationException(method.Name));
            var lifecycle = ControlledDependencies.Simulate<IEmulationLifecycle>((method, _) => method.Name == nameof(IEmulationLifecycle.StopAsync)
                ? ValueTask.CompletedTask : throw new InvalidOperationException(method.Name));
            Machine = ControlledDependencies.Simulate<IEmulatedMachine>((method, _) =>
            {
                if (method.Name == nameof(IAsyncDisposable.DisposeAsync)) { Disposals++; return ValueTask.CompletedTask; }
                return method.Name switch
                {
                    "get_Audio" => audio,
                    "get_Input" => input,
                    "get_Video" => Video.Machine.Video,
                    "get_Runtime" => runtime,
                    "get_SavedStates" => states,
                    "get_Lifecycle" => lifecycle,
                    _ => throw new InvalidOperationException(method.Name)
                };
            });
        }

        internal Task<MachineController> CreateAsync() => MachineController.CreateAsync(new MachineControllerOptions(
            "module", "machine", Guid.NewGuid(), Machine, _ => Machine,
            (_, _) => throw new InvalidOperationException("No runtime may start"), [], [], EmulationVideoRenderer.Wpf,
            new(), [], "unused-state", "unused-capture", "test", error => throw error, false, _ => null, (_, _) => { }, () => false));
    }
}
