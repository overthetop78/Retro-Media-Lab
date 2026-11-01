using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using RetroMediaLab.App.Contracts.Machine;
using RetroMediaLab.App.Views.Controls.Emulation.Machine;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.VideoPresentation.Enums;
using RetroMediaLab.VideoPresentation.Contracts;
using RetroMediaLab.Emulation.Interfaces;
using RetroMediaLab.App.Services.Emulation;
using RetroMediaLab.App.Controllers.Emulation.Machine;
using RetroMediaLab.App.Input.Mouse;
using RetroMediaLab.App.Interfaces.Rendering.Emulation;
using RetroMediaLab.App.Presenters.Emulation.Machine;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.App.Services.Input.GameInput;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Tests.Emulation.Video;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using RetroMediaLab.Tests.Application.TestInfrastructure;
using Windows31MachineView = RetroMediaLab.App.Theme.OS.Editions.Windows31.Views.MachineView;

namespace RetroMediaLab.Tests.Interface.Themes;

[Collection("WPF")]
public sealed class Windows31MachineViewTests(StaExecutionScenarios sta)
{
    [Fact]
    public Task VideoTransferKeepsTheSurfaceAndRestoresItAfterARejectedPresentation() => sta.Run(() =>
    {
        var source = new VideoProcessingScenarios.VideoSource();
        using var original = new RetroMediaLab.App.Views.Controls.Emulation.Machine.MachineView();
        using var replacement = new Windows31MachineView();
        using var rejected = new RejectedPresentation();
        var surface = new Surface();
        using var presenter = new MachineVideoPresenter(original, source.Machine, EmulationVideoRenderer.Wpf, createSurface: _ => surface);
        Assert.Equal(1, source.Subscribers);
        presenter.SetPresentation(replacement);
        Assert.Same(replacement.VideoHost, surface.View.Parent);
        Assert.Same(surface.View, presenter.InputView);
        Assert.Equal(1, source.Subscribers);
        Assert.False(surface.Disposed);
        Assert.Throws<InvalidOperationException>(() => presenter.SetPresentation(rejected));
        Assert.Same(replacement.VideoHost, surface.View.Parent);
        Assert.Null(rejected.DetachVideoView());
        presenter.SetPresentation(original);
        Assert.Same(original.VideoHost, surface.View.Parent);
        Assert.Equal(1, source.Subscribers);
        presenter.Dispose();
        Assert.True(surface.Disposed);
        Assert.Equal(0, source.Subscribers);
    });

    [Fact]
    public Task CommandTransferKeepsActionsAndStateAfterTheOldPresentationIsReleased() => sta.Run(() =>
    {
        var calls = 0;
        Task Action() { calls++; return Task.CompletedTask; }
        using var original = new RetroMediaLab.App.Views.Controls.Emulation.Machine.MachineView();
        using var replacement = new Windows31MachineView();
        var commands = new MachineCommandBar(original.Toolbar,
            new MachineCommandActions(Action, Action, Action, Action, Action, Action, Action, Action, Action),
            [], error => throw error, () => { });
        commands.SetPowered(true);
        commands.SetPaused(true);
        replacement.AttachCommands(commands.Presentation);
        var children = original.Toolbar.Children.Cast<UIElement>().ToArray();
        commands.Rehost(replacement.Toolbar);
        Assert.Equal(children, replacement.Toolbar.Children.Cast<UIElement>());
        Assert.Empty(original.Toolbar.Children);
        original.Dispose();
        var pause = Field<MenuItem>(replacement, "PauseCommand");
        Assert.True(pause.IsChecked);
        pause.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Assert.Equal(1, calls);
        using var restored = new RetroMediaLab.App.Views.Controls.Emulation.Machine.MachineView();
        commands.Rehost(restored.Toolbar);
        replacement.Dispose();
        commands.Presentation.Commands[MachineCommandId.Power].RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        Assert.Equal(2, calls);
        Assert.Equal(children, restored.Toolbar.Children.Cast<UIElement>());
    });

    [Fact]
    public Task InputHostTransferDetachesThePreviousHostWithoutPollingHardware() => sta.Run(() =>
    {
        using var original = new RetroMediaLab.App.Views.Controls.Emulation.Machine.MachineView();
        using var replacement = new Windows31MachineView();
        var input = ControlledDependencies.Simulate<IEmulationInput>((method, _) => method.Name == "get_KeyboardBindings"
            ? new Dictionary<string, string>() : throw new InvalidOperationException(method.Name));
        var machine = ControlledDependencies.Simulate<IEmulatedMachine>((method, _) => method.Name == "get_Input"
            ? input : throw new InvalidOperationException(method.Name));
        var pointer = ControlledDependencies.Simulate<IRelativeMouseCapture>((method, _) => method.Name switch
        {
            "get_IsCaptured" => false,
            "Release" => null,
            _ => throw new InvalidOperationException(method.Name)
        });
        var focused = 0;
        var handle = new IntPtr(101);
        var focusedHandles = new List<IntPtr>();
        using var controller = new MachineInputController(original, new Border(), IntPtr.Zero, () => machine, [],
            _ => Task.CompletedTask, () => true, pointerCapture: pointer, focus: (_, current) => { focused++; focusedHandles.Add(current); },
            automaticPolling: false, feedbackSource: ControlledDependencies.Reject<IGameInputControllerSource>(), inputHandleProvider: () => handle);
        controller.SetPowered(true);
        controller.SetPresentation(replacement);
        original.DisplayHost.RaiseEvent(Click());
        Assert.Equal(0, focused);
        replacement.DisplayHost.RaiseEvent(Click());
        Assert.Equal(1, focused);
        handle = new IntPtr(202);
        controller.SetPresentation(original);
        replacement.DisplayHost.RaiseEvent(Click());
        Assert.Equal(1, focused);
        original.DisplayHost.RaiseEvent(Click());
        Assert.Equal(2, focused);
        Assert.Equal(new[] { new IntPtr(101), new IntPtr(202) }, focusedHandles);

        static MouseButtonEventArgs Click() => new(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = UIElement.MouseLeftButtonDownEvent };
    });

    [Fact]
    public Task FailedPresentationAwaitsCoreDisposalAndRemovesItsSession() => sta.RunAsync(async () =>
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var lifecycle = ControlledDependencies.Simulate<IEmulationLifecycle>((method, _) =>
            method.Name == nameof(IEmulationLifecycle.StopAsync) ? ValueTask.CompletedTask : throw new InvalidOperationException(method.Name));
        var machine = ControlledDependencies.Simulate<IEmulatedMachine>((method, _) =>
        {
            if (method.Name == "get_Lifecycle") return lifecycle;
            if (method.Name != nameof(IAsyncDisposable.DisposeAsync)) throw new InvalidOperationException(method.Name);
            entered.TrySetResult();
            return new ValueTask(release.Task);
        });
        var failure = new InvalidOperationException("presentation");
        var options = new MachineControllerOptions("module", "machine", Guid.NewGuid(), machine, _ => machine,
            (_, _) => throw new InvalidOperationException("No runtime may start"), [], [], EmulationVideoRenderer.Wpf,
            new(), [], "state", "capture", "title", error => throw error, false, _ => null, (_, _) => { }, () => true,
            CreatePresentation: () => throw failure);
        var creation = MachineController.CreateAsync(options);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.False(creation.IsCompleted);
        }
        finally
        {
            release.TrySetResult();
            Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => creation));
        }
        Assert.DoesNotContain(MachineSession.All, session => ReferenceEquals(machine, session.Machine));
    });

    [Fact]
    public Task MenusUseOriginalActionsAndAvailabilityAndDetachOnClose() => sta.Run(() =>
    {
        var calls = 0;
        Task Action() { calls++; return Task.CompletedTask; }
        using var view = new Windows31MachineView();
        var commands = new MachineCommandBar(view.Toolbar,
            new MachineCommandActions(Action, Action, Action, Action, Action, Action, Action, Action, Action),
            [], error => throw error, () => { });
        commands.SetPowered(false);
        commands.SetSavedStateAvailability(true, false);
        view.AttachCommands(commands.Presentation);
        var pause = Field<MenuItem>(view, "PauseCommand");
        var power = Field<MenuItem>(view, "PowerCommand");
        Assert.False(pause.IsEnabled);
        Assert.True(power.IsEnabled);
        pause.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Assert.Equal(0, calls);
        power.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Assert.Equal(1, calls);
        commands.SetPowered(true);
        commands.SetPaused(true);
        commands.SetMuted(true);
        Assert.True(power.IsChecked);
        Assert.True(pause.IsChecked);
        Assert.True(Field<MenuItem>(view, "AudioCommand").IsChecked);
        Assert.True(Field<MenuItem>(view, "QuickSaveCommand").IsEnabled);
        Assert.False(Field<MenuItem>(view, "QuickLoadCommand").IsEnabled);
        Assert.Equal(Visibility.Collapsed, Field<MenuItem>(view, "CoreCommand").Visibility);
        Assert.Equal(Visibility.Collapsed, Field<MenuItem>(view, "PointerCommand").Visibility);
        commands.SetSavedStateAvailability(true, true);
        Assert.True(Field<MenuItem>(view, "QuickLoadCommand").IsEnabled);
        pause.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Assert.Equal(2, calls);
        view.Dispose();
        power.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Assert.Equal(2, calls);
        Assert.Null(view.Content);
    });

    [Fact]
    public Task DeviceActionsCassetteStatesAndVideoSurfaceUseExistingModels() => sta.Run(() =>
    {
        using var view = new Windows31MachineView();
        var inserted = 0;
        var ejected = 0;
        var played = 0;
        var commands = new[]
        {
            new MachineViewDeviceCommand(EmulationCassetteCommand.Play, "Play", "", true, true, false, false,
                () => { played++; return Task.CompletedTask; })
        };
        var device = new MachineViewDevice("cassette", "Cassette", "", true, true,
            () => { inserted++; return Task.CompletedTask; }, () => { ejected++; return Task.CompletedTask; }, "Stopped", commands);
        view.SetDevices([device], error => throw error, () => { });
        var supports = Field<MenuItem>(view, "SupportsMenu");
        var menu = Assert.IsType<MenuItem>(Assert.Single(supports.Items));
        var insert = Assert.IsType<MenuItem>(menu.Items[0]);
        var eject = Assert.IsType<MenuItem>(menu.Items[2]);
        var play = Assert.IsType<MenuItem>(menu.Items[4]);
        insert.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        eject.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        play.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Assert.Equal((1, 1, 1), (inserted, ejected, played));
        view.SetDeviceStatus("cassette", "Playing");
        Assert.Equal("Playing", Assert.IsType<MenuItem>(menu.Items[1]).Header);
        view.SetCassetteTransport("cassette", EmulationCassetteState.Playing, EmulationCassetteCommand.Play,
            new HashSet<EmulationCassetteCommand> { EmulationCassetteCommand.Play }, true);
        Assert.True(play.IsChecked);
        Assert.False(play.IsEnabled);
        play.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Assert.Equal(1, played);
        var firstSurface = new Border();
        var secondSurface = new Border();
        view.SetVideoView(firstSurface);
        Assert.Same(view.VideoHost, firstSurface.Parent);
        view.SetShaderLoading(true);
        Assert.Equal(Visibility.Hidden, firstSurface.Visibility);
        view.SetVideoView(secondSurface);
        Assert.Null(firstSurface.Parent);
        Assert.Equal(Visibility.Hidden, secondSurface.Visibility);
        view.SetShaderLoading(false);
        Assert.Equal(Visibility.Visible, secondSurface.Visibility);
        view.SetDevices([], error => throw error, () => { });
        insert.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Assert.Equal(1, inserted);
        Assert.Empty(view.DeviceLeds);
        Assert.False(supports.IsEnabled);
        view.Dispose();
        Assert.Null(secondSurface.Parent);
    });

    [Fact]
    public Task ManyDevicesKeepACompactFooterAndTheirTypedMenusAndDetachedCommands() => sta.Run(() =>
    {
        using var view = new Windows31MachineView();
        var types = Enum.GetValues<EmulationMediaType>();
        var devices = Enumerable.Range(0, 30).Select(index => new MachineViewDevice(
            index.ToString(), $"Drive {index}", string.Empty, true, true, () => Task.CompletedTask, null,
            Commands: [new(EmulationCassetteCommand.Play, "Play", string.Empty, true, false, false, false, () => Task.CompletedTask)],
            MediaType: types[index % types.Length])).ToArray();
        view.SetDevices(devices, error => throw error, () => { });
        var footer = Field<StackPanel>(view, "Devices");
        Assert.Equal(Orientation.Horizontal, footer.Orientation);
        Assert.Equal(devices.Length, footer.Children.Count);
        Assert.All(footer.Children.Cast<StackPanel>(), indicator => Assert.Single(indicator.Children.OfType<Button>()));
        Assert.Equal(types.Length, Field<MenuItem>(view, "SupportsMenu").Items.Count);
        Assert.Equal(devices.Length, Field<MenuItem>(view, "SupportsMenu").Items.Cast<MenuItem>().Sum(group => group.Items.Count));
        var requested = new List<string>();
        view.DeviceRequested += requested.Add;
        Assert.Single(((StackPanel)footer.Children[0]).Children.OfType<Button>()).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Equal(new[] { devices[0].Key }, requested);
        Assert.Null(view.DevicePanel(devices[0].Key)!.Parent);
        Assert.Equal(2, Assert.IsType<WrapPanel>(view.DevicePanel(devices[0].Key)).Children.OfType<Button>().Count());
        view.SetDevices([], error => throw error, () => { });
        Assert.Null(view.DevicePanel(devices[0].Key));
    });

    private static T Field<T>(Windows31MachineView view, string name) => Assert.IsType<T>(view.FindName(name));

    private sealed class RejectedPresentation : EmulationMachinePresentation
    {
        internal override DockPanel Toolbar { get; } = new();
        internal override Grid VideoHost { get; } = new();
        internal override Border Screen { get; } = new();
        internal override Grid DisplayHost { get; } = new();
        internal override IReadOnlyDictionary<string, Ellipse> DeviceLeds { get; } = new Dictionary<string, Ellipse>();
        internal override void AttachCommands(MachineCommandPresentation commands) { }
        internal override void SetVideoView(FrameworkElement view)
        {
            VideoHost.Children.Add(view);
            throw new InvalidOperationException("Rejected surface");
        }
        internal override FrameworkElement? DetachVideoView()
        {
            var view = VideoHost.Children.OfType<FrameworkElement>().SingleOrDefault();
            VideoHost.Children.Clear();
            return view;
        }
        internal override void SetShaderLoading(bool loading) { }
        internal override void SetDevices(IEnumerable<MachineViewDevice> devices, Action<Exception> error, Action focus) { }
        internal override void SetDeviceStatus(string key, string status) { }
        internal override void SetCassetteTransport(string key, EmulationCassetteState state, EmulationCassetteCommand? active,
            IReadOnlySet<EmulationCassetteCommand> supported, bool powered) { }
        public override void Dispose() => VideoHost.Children.Clear();
    }

    private sealed class Surface : IEmulationVideoSurface
    {
        public FrameworkElement View { get; } = new Border();
        public BitmapSource? Snapshot => null;
        public EmulationVideoRenderer Renderer => EmulationVideoRenderer.Wpf;
        public IntPtr InputHandle => IntPtr.Zero;
        internal bool Disposed { get; private set; }
        public void SetVideoProcessing(EmulationVideoProcessingConfiguration configuration) { }
        public void Present(VideoFrame frame) { }
        public void Dispose() => Disposed = true;
    }
}
