using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using RetroMediaLab.App.Controllers.Emulation.Machine;
using RetroMediaLab.App.Input.Mouse;
using RetroMediaLab.App.Services.Input.GameInput;
using RetroMediaLab.App.Views.Controls.Emulation.Machine;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.Emulation.Interfaces;
using RetroMediaLab.Tests.Application.TestInfrastructure;

namespace RetroMediaLab.Tests.Interface.EmulationViews;

[Collection("WPF")]
public sealed class MachineClickFocusTests(StaExecutionScenarios sta)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task ClickOutsideTheMachineReturnsToTheVideoAfterTheCommand(bool handled) => Run(async ui =>
    {
        var invoked = false;
        ui.Button.Click += (_, _) => invoked = true;
        ui.Button.Focus();
        ui.Click(ui.Button, handled);
        ui.Button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        Assert.True(invoked);
        Assert.Equal(0, ui.FocusCalls);
        await Idle();
        Assert.Equal(1, ui.FocusCalls);
        Assert.Same(ui.Input, Keyboard.FocusedElement);
        ui.Click(ui.Background);
        await Idle();
        Assert.Equal(2, ui.FocusCalls);
    });

    [Fact]
    public Task EditableTextAndAnOpenDropdownKeepFocusUntilTheNextOrdinaryClick() => Run(async ui =>
    {
        ui.Text.Focus();
        ui.Click(ui.Text);
        await Idle();
        Assert.Equal(0, ui.FocusCalls);
        Assert.Same(ui.Text, Keyboard.FocusedElement);
        ui.Click(ui.Background); // A non-focusable background does not itself blur the editor.
        await Idle();
        Assert.Equal(1, ui.FocusCalls);
        Assert.Same(ui.Input, Keyboard.FocusedElement);
        ui.Combo.Focus();
        ui.Combo.IsDropDownOpen = true;
        try
        {
            ui.Click(ui.Combo);
            await Idle();
            Assert.Equal(1, ui.FocusCalls);
            Assert.True(ui.Combo.IsDropDownOpen);
        }
        finally { ui.Combo.IsDropDownOpen = false; }
        ui.Button.Focus();
        ui.Click(ui.Button);
        await Idle();
        Assert.Same(ui.Input, Keyboard.FocusedElement);
    });

    [Fact]
    public Task InactiveHiddenStoppedAndDisposedMachinesNeverTakeFocus() => Run(async ui =>
    {
        ui.Button.Focus();
        ui.Click(ui.Button);
        ui.Active = false; // Another machine/tab was selected by this click.
        await Idle();
        Assert.Equal(0, ui.FocusCalls);
        ui.Active = true;
        ui.View.Visibility = Visibility.Collapsed;
        ui.Click(ui.Button);
        await Idle();
        Assert.Equal(0, ui.FocusCalls);
        ui.View.Visibility = Visibility.Visible;
        ui.Controller.SetPowered(false);
        ui.Click(ui.Button);
        await Idle();
        Assert.Equal(0, ui.FocusCalls);
        ui.Controller.SetPowered(true);
        ui.Click(ui.Button);
        ui.Controller.Dispose(); // Cancels the pending Dispatcher operation and unhooks the window.
        await Idle();
        ui.Click(ui.Button);
        await Idle();
        Assert.Equal(0, ui.FocusCalls);
    });

    [Fact]
    public Task DeferredFocusUsesTheCurrentNativeVideoHandle() => Run(async ui =>
    {
        ui.Button.Focus();
        ui.Click(ui.Button);
        ui.Handle = new IntPtr(202);
        await Idle();
        Assert.Equal(ui.Handle, ui.FocusedHandle);
    });

    private Task Run(Func<Harness, Task> scenario) => sta.RunAsync(async () =>
    {
        using var ui = new Harness();
        try
        {
            ui.Window.Show();
            ui.Window.Activate();
            await Idle();
            Assert.True(ui.Window.IsActive);
            ui.Controller.SetPowered(true);
            await scenario(ui);
        }
        finally
        {
            ui.Dispose();
            await Idle();
        }
    });

    private static async Task Idle() => await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);

    private sealed class Harness : IDisposable
    {
        internal MachineView View { get; } = new();
        internal Border Input { get; } = new() { Focusable = true };
        internal Border Background { get; } = new() { Height = 20, Background = System.Windows.Media.Brushes.Transparent };
        internal Button Button { get; } = new() { Content = "command" };
        internal TextBox Text { get; } = new();
        internal ComboBox Combo { get; } = new() { ItemsSource = new[] { "one", "two" } };
        internal Window Window { get; }
        internal MachineInputController Controller { get; }
        internal bool Active = true;
        internal int FocusCalls;
        internal IntPtr Handle = new(101);
        internal IntPtr FocusedHandle;
        private bool _disposed;

        internal Harness()
        {
            var input = ControlledDependencies.Simulate<IEmulationInput>((method, _) => method.Name switch
            {
                "get_KeyboardBindings" => new Dictionary<string, string>(),
                "SetInput" => null,
                _ => throw new InvalidOperationException(method.Name)
            });
            var machine = ControlledDependencies.Simulate<IEmulatedMachine>((method, _) => method.Name switch
            {
                "get_Input" => input,
                "get_State" => EmulationMachineState.Running,
                _ => throw new InvalidOperationException(method.Name)
            });
            var pointer = ControlledDependencies.Simulate<IRelativeMouseCapture>((method, _) => method.Name switch
            {
                "get_IsCaptured" => false,
                "Release" => null,
                _ => throw new InvalidOperationException(method.Name)
            });
            View.SetVideoView(Input);
            Controller = new MachineInputController(View, Input, IntPtr.Zero, () => machine, [],
                _ => Task.CompletedTask, () => Active, pointerCapture: pointer, automaticPolling: false,
                focus: (element, handle) => { FocusCalls++; FocusedHandle = handle; element.Focus(); },
                feedbackSource: ControlledDependencies.Reject<IGameInputControllerSource>(), inputHandleProvider: () => Handle);
            var root = new DockPanel();
            var commands = new StackPanel();
            commands.Children.Add(Button);
            commands.Children.Add(Text);
            commands.Children.Add(Combo);
            commands.Children.Add(Background);
            DockPanel.SetDock(commands, Dock.Top);
            root.Children.Add(commands);
            root.Children.Add(View);
            Window = new Window { Content = root, Width = 640, Height = 480, ShowInTaskbar = false };
        }

        internal void Click(UIElement element, bool handled = false) => element.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
            { RoutedEvent = Mouse.PreviewMouseUpEvent, Handled = handled });

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { Controller.Dispose(); }
            finally
            {
                try { View.Dispose(); }
                finally { Combo.IsDropDownOpen = false; Window.Content = null; Window.Close(); }
            }
        }
    }
}
