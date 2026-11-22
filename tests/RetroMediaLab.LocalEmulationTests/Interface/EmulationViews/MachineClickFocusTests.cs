using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using RetroMediaLab.App.Contracts.Input;
using RetroMediaLab.App.Controllers.Emulation.Machine;
using RetroMediaLab.App.Input.Mouse;
using RetroMediaLab.App.Services.Input.GameInput;
using RetroMediaLab.App.Views.Controls.Emulation.Machine;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.Emulation.Interfaces;
using RetroMediaLab.Infrastructure.Settings.Emulation;
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

    [Fact]
    public Task FullscreenOsdShortcutCanToggleWithTheMachinePoweredOff() => Run(ui =>
    {
        ui.Controller.SetPowered(false);
        ui.LeftButton = true;
        ui.Controller.Publish();
        Assert.Empty(ui.ShortcutActions);

        ui.Fullscreen = true;
        ui.Controller.Publish();
        Assert.Equal(EmulationShortcutDefaults.OsdWithoutPause, Assert.Single(ui.ShortcutActions));
        ui.Controller.SetOsdFocus(true);
        ui.Controller.Publish();
        Assert.Single(ui.ShortcutActions);

        ui.LeftButton = false;
        ui.Controller.Publish();
        ui.LeftButton = true;
        ui.Controller.Publish();
        Assert.Equal(2, ui.ShortcutActions.Count);
        Assert.Equal(0, ui.InputCalls);
        return Task.CompletedTask;
    });

    [Theory]
    [InlineData(Key.Divide, EmulationShortcutDefaults.OsdWithPause)]
    [InlineData(Key.Multiply, EmulationShortcutDefaults.OsdWithoutPause)]
    public Task DefaultOsdKeyboardShortcutIsConsumedOnlyInFullscreen(Key key, string action) => Run(ui =>
    {
        const ModifierKeys modifiers = ModifierKeys.Control | ModifierKeys.Shift;
        ui.Controller.HandleKeyDown(Key.LeftCtrl, ModifierKeys.Control);
        ui.Controller.HandleKeyDown(Key.LeftShift, modifiers);
        ui.Controller.HandleKeyDown(key, modifiers);
        Assert.Empty(ui.ShortcutActions);
        ui.Controller.HandleKeyUp(key, modifiers);

        ui.Fullscreen = true;
        Assert.True(ui.Controller.HandleKeyDown(key, modifiers));
        Assert.Equal(action, Assert.Single(ui.ShortcutActions));
        Assert.True(ui.Controller.HandleKeyDown(key, modifiers));
        Assert.Single(ui.ShortcutActions);

        ui.Controller.SetOsdFocus(true);
        ui.Controller.HandleKeyUp(key, modifiers);
        Assert.True(ui.Controller.HandleKeyDown(key, modifiers));
        Assert.Equal([action, action], ui.ShortcutActions);
        return Task.CompletedTask;
    }, useDefaultOsdShortcuts: true);

    [Fact]
    public Task OsdReceivesCombinedKeyboardMouseAndControllerInput() => Run(ui =>
    {
        ui.Fullscreen = true;
        ui.Controller.SetOsdFocus(true);
        int callsAtOpen = ui.InputCalls;
        Assert.True(ui.Controller.HandleKeyDown(Key.K, ModifierKeys.None));
        ui.RightButton = true;
        ui.Controller.Publish();
        Assert.Empty(ui.ShortcutActions);

        ui.ControllerButton = true;
        ui.Controller.Publish();
        ui.Controller.Publish();
        Assert.Equal(EmulationShortcutDefaults.OsdDown + EmulationShortcutDefaults.AlternativeSuffix,
            Assert.Single(ui.ShortcutActions));
        Assert.Equal(callsAtOpen, ui.InputCalls);

        Assert.True(ui.Controller.HandleKeyUp(Key.K, ModifierKeys.None));
        ui.Controller.Publish();
        Assert.True(ui.Controller.HandleKeyDown(Key.K, ModifierKeys.None));
        ui.Controller.Publish();
        Assert.Equal(2, ui.ShortcutActions.Count);
        Assert.Equal(callsAtOpen, ui.InputCalls);

        ui.Controller.HandleKeyUp(Key.K, ModifierKeys.None);
        ui.RightButton = ui.ControllerButton = false;
        ui.Controller.SetOsdFocus(false);
        ui.Controller.HandleKeyDown(Key.K, ModifierKeys.None);
        Assert.True(ui.InputCalls > callsAtOpen);
        return Task.CompletedTask;
    });

    [Fact]
    public Task KeysHeldWhenTheOsdClosesReachTheGameOnlyAfterRelease() => Run(ui =>
    {
        ui.Fullscreen = true;
        ui.Controller.SetOsdFocus(true);
        ui.PhysicalKeys.Add(EmulationKey.K);
        ui.Controller.HandleKeyDown(Key.K, ModifierKeys.None);
        ui.Controller.SetOsdFocus(false);

        ui.Controller.HandleKeyDown(Key.K, ModifierKeys.None);
        ui.Controller.HandleKeyDown(Key.J, ModifierKeys.None);
        Assert.Contains(EmulationKey.J, ui.LastInput.Keys);
        Assert.DoesNotContain(EmulationKey.K, ui.LastInput.Keys);

        ui.PhysicalKeys.Remove(EmulationKey.K);
        ui.Controller.HandleKeyUp(Key.K, ModifierKeys.None);
        ui.PhysicalKeys.Add(EmulationKey.K);
        ui.Controller.HandleKeyDown(Key.K, ModifierKeys.None);
        Assert.Contains(EmulationKey.K, ui.LastInput.Keys);
        return Task.CompletedTask;
    });

    [Fact]
    public Task MouseButtonsHeldWhenTheOsdClosesReachTheGameOnlyAfterRelease() => Run(ui =>
    {
        ui.Fullscreen = true;
        ui.PointerCaptured = true;
        ui.KeyboardBindings[nameof(EmulationKey.K)] = "Mouse:Right";
        ui.Controller.SetOsdFocus(true);
        Assert.False(ui.PointerCaptured);
        ui.RightButton = true;
        ui.Controller.SetOsdFocus(false);
        Assert.True(ui.PointerCaptured);

        ui.MiddleButton = true;
        ui.Controller.Publish();
        Assert.True(ui.LastInput.Pointer.Middle);
        Assert.False(ui.LastInput.Pointer.Right);
        Assert.DoesNotContain(EmulationKey.K, ui.LastInput.Keys);

        ui.RightButton = false;
        ui.Controller.Publish();
        ui.RightButton = true;
        ui.Controller.Publish();
        Assert.True(ui.LastInput.Pointer.Right);
        Assert.Contains(EmulationKey.K, ui.LastInput.Keys);
        return Task.CompletedTask;
    });

    [Fact]
    public Task ControllerInputsHeldWhenTheOsdClosesReachTheGameOnlyAfterRelease() => Run(ui =>
    {
        ui.Fullscreen = true;
        ui.KeyboardBindings[nameof(EmulationKey.K)] = "Controller:test-pad:Button1";
        ui.Controller.SetOsdFocus(true);
        ui.ControllerButton = true;
        ui.ControllerBits = 1;
        ui.LeftStick = 28000;
        ui.RawAxis = .9f;
        ui.Controller.SetOsdFocus(false);

        ui.ControllerBits = 3;
        ui.Controller.Publish();
        var state = Assert.Single(ui.LastInput.Controllers);
        Assert.Equal(2u, state.Buttons);
        Assert.Equal(0, state.LeftX);
        Assert.Equal(.5f, state.Controls["Axis0"]);
        Assert.Equal(0f, state.Controls["Button1"]);
        Assert.DoesNotContain(EmulationKey.K, ui.LastInput.Keys);

        ui.ControllerButton = false;
        ui.ControllerBits = 0;
        ui.LeftStick = 0;
        ui.RawAxis = .5f;
        ui.Controller.Publish();
        ui.ControllerButton = true;
        ui.ControllerBits = 1;
        ui.LeftStick = 25000;
        ui.RawAxis = .1f;
        ui.Controller.Publish();
        state = Assert.Single(ui.LastInput.Controllers);
        Assert.Equal(1u, state.Buttons);
        Assert.Equal(25000, state.LeftX);
        Assert.Equal(.1f, state.Controls["Axis0"]);
        Assert.Contains(EmulationKey.K, ui.LastInput.Keys);
        return Task.CompletedTask;
    });

    private Task Run(Func<Harness, Task> scenario, bool useDefaultOsdShortcuts = false) => sta.RunAsync(async () =>
    {
        using var ui = new Harness(useDefaultOsdShortcuts);
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
        internal bool Fullscreen;
        internal bool LeftButton;
        internal bool RightButton;
        internal bool MiddleButton;
        internal bool PointerCaptured;
        internal bool ControllerButton;
        internal uint ControllerBits;
        internal short LeftStick;
        internal float RawAxis = .5f;
        internal Dictionary<string, string> KeyboardBindings { get; } = [];
        internal int InputCalls;
        internal HashSet<EmulationKey> PhysicalKeys { get; } = [];
        internal EmulationInputSnapshot LastInput = EmulationInputSnapshot.Empty;
        internal List<string> ShortcutActions { get; } = [];
        internal int FocusCalls;
        internal IntPtr Handle = new(101);
        internal IntPtr FocusedHandle;
        private bool _disposed;

        internal Harness(bool useDefaultOsdShortcuts)
        {
            var input = ControlledDependencies.Simulate<IEmulationInput>((method, args) => method.Name switch
            {
                "get_KeyboardBindings" => KeyboardBindings,
                "SetInput" => RecordInput((EmulationInputSnapshot)args[0]!),
                _ => throw new InvalidOperationException(method.Name)
            });
            object? RecordInput(EmulationInputSnapshot snapshot)
            {
                LastInput = snapshot;
                InputCalls++;
                return null;
            }
            var machine = ControlledDependencies.Simulate<IEmulatedMachine>((method, _) => method.Name switch
            {
                "get_Input" => input,
                "get_State" => EmulationMachineState.Running,
                _ => throw new InvalidOperationException(method.Name)
            });
            var pointer = ControlledDependencies.Simulate<IRelativeMouseCapture>((method, _) => method.Name switch
            {
                "get_IsCaptured" => PointerCaptured,
                "Capture" => ChangeCapture(true),
                "Release" => ChangeCapture(false),
                _ => throw new InvalidOperationException(method.Name)
            });
            object? ChangeCapture(bool captured)
            {
                PointerCaptured = captured;
                return null;
            }
            View.SetVideoView(Input);
            Controller = new MachineInputController(View, Input, IntPtr.Zero, () => machine, [],
                action => { ShortcutActions.Add(action); return Task.CompletedTask; },
                () => Active, pointerCapture: pointer, automaticPolling: false,
                readPhysicalInput: () => new(PhysicalKeys, new(0, 0, 0, LeftButton, RightButton, MiddleButton),
                    [EmulationControllerState.Empty with
                    {
                        DeviceId = "test-pad",
                        Buttons = ControllerBits,
                        LeftX = LeftStick,
                        Controls = new([new("Button1", ControllerButton ? 1f : 0f), new("Axis0", RawAxis)])
                    }]),
                focus: (element, handle) => { FocusCalls++; FocusedHandle = handle; element.Focus(); },
                feedbackSource: ControlledDependencies.Reject<IGameInputControllerSource>(), inputHandleProvider: () => Handle,
                osdShortcuts:
                [
                    new GlobalShortcutBinding(EmulationShortcutDefaults.OsdWithoutPause, new(ModifierKeys.None, []), "mouse:left"),
                    new GlobalShortcutBinding(EmulationShortcutDefaults.OsdDown + EmulationShortcutDefaults.AlternativeSuffix,
                        new(ModifierKeys.None, [Key.K]), "Keyboard:K+Mouse:Right+Controller:test-pad:Button1")
                ],
                isFullscreen: () => Fullscreen,
                readShortcutSettings: useDefaultOsdShortcuts ? () => EmulationShortcutDefaults.OsdValues : null);
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
