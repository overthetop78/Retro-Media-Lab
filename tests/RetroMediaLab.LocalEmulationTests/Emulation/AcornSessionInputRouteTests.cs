using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RetroMediaLab.App.Controllers.Emulation.Machine;
using RetroMediaLab.App.Input.Mouse;
using RetroMediaLab.App.Services.Input.GameInput;
using RetroMediaLab.App.Views.Controls.Emulation.Machine;
using RetroMediaLab.Emulation.Acorn.Common.Interfaces;
using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Contracts;
using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Enums;
using RetroMediaLab.Emulation.Acorn.Common.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Factories;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.Tests.Application.TestInfrastructure;

namespace RetroMediaLab.Tests.Emulation;

[Collection("WPF")]
public sealed class AcornSessionInputRouteTests(StaExecutionScenarios sta)
{
    [Fact]
    public Task KeyboardShortcutChangesReleaseOldCommandsAndUseTheNewBinding() => sta.Run(() =>
    {
        IReadOnlyDictionary<string, string> bindings = new Dictionary<string, string> { ["F1"] = "F8" };
        var snapshots = new List<EmulationInputSnapshot>();
        var input = ControlledDependencies.Simulate<RetroMediaLab.Emulation.Interfaces.IEmulationInput>((method, args) =>
        {
            if (method.Name == "get_KeyboardBindings") return bindings;
            if (method.Name == "SetInput") snapshots.Add((EmulationInputSnapshot)args[0]!);
            return method.ReturnType == typeof(bool) ? false : null;
        });
        var machine = ControlledDependencies.Simulate<RetroMediaLab.Emulation.Interfaces.IEmulatedMachine>((method, _) => method.Name switch
        {
            "get_Input" => input,
            "get_State" => EmulationMachineState.Running,
            _ => null
        });
        var view = new MachineView();
        MachineInputController? controller = null;
        try
        {
            controller = new MachineInputController(view, new Border(), IntPtr.Zero, () => machine, [],
                _ => Task.CompletedTask, () => true, new PointerCapture(),
                () => new GameInputPhysicalState(new HashSet<EmulationKey>(), EmulationInputSnapshot.Empty.Pointer, []),
                (_, _) => { }, automaticPolling: false);
            controller.SetPowered(true);
            controller.HandleKeyDown(Key.F8, ModifierKeys.None);
            Assert.Contains(EmulationKey.F1, snapshots[^1].Keys);
            bindings = new Dictionary<string, string> { ["F1"] = "F9" };
            controller.RefreshBindings();
            Assert.Empty(snapshots[^1].Keys);
            controller.HandleKeyDown(Key.F8, ModifierKeys.None);
            Assert.DoesNotContain(EmulationKey.F1, snapshots[^1].Keys);
            controller.HandleKeyUp(Key.F8, ModifierKeys.None);
            controller.HandleKeyDown(Key.F9, ModifierKeys.None);
            Assert.Contains(EmulationKey.F1, snapshots[^1].Keys);
        }
        finally
        {
            try { controller?.Dispose(); }
            finally { view.Content = null; }
        }
    });

    [Fact]
    public void BindingChangesReachTheExistingSessionWithoutRestartingItsCore()
    {
        var starts = 0;
        var mappings = new List<MachineConfiguration>();
        var core = ControlledDependencies.Simulate<IEmulatorCore>((method, _) =>
        {
            if (method.Name == "Initialize") starts++;
            return method.ReturnType != typeof(void) && method.ReturnType.IsValueType
                ? Activator.CreateInstance(method.ReturnType) : null;
        });
        var mapper = ControlledDependencies.Simulate<IEmulatorInputMapper>((method, args) =>
        {
            mappings.Add((MachineConfiguration)args[1]!);
            return args[0];
        });
        var configuration = new MachineConfiguration(MachineModel.BbcMaster128, core: Emulator.AcornEmu);
        var machine = new Machine(Guid.NewGuid(), configuration, core, "unused", inputMapper: mapper);
        try
        {
            machine.Input.SetInput(EmulationInputSnapshot.Empty);
            var changed = configuration with
            {
                Input = configuration.Input with
                {
                    KeyboardBindings = new Dictionary<string, string> { ["F1"] = "F8" },
                    MouseBindings = new Dictionary<string, string> { ["Left"] = "Mouse:Right" },
                    Controllers = [new ControllerBinding(0, PeripheralCategory.AnalogJoystick)
                    {
                        Mappings = new Dictionary<string, string> { ["Fire1"] = "Keyboard:Space" }
                    }]
                },
                Options = new Dictionary<string, string> { ["unrelated-hardware-change"] = "ignored" }
            };
            machine.Input.UpdateBindings(changed);
            machine.Input.SetInput(EmulationInputSnapshot.Empty);
            Assert.Equal("F8", machine.Input.KeyboardBindings["F1"]);
            Assert.Same(configuration.Input, mappings[0].Input);
            Assert.Same(changed.Input, mappings[1].Input);
            Assert.Same(configuration.Options, mappings[1].Options);
            Assert.Equal(0, starts);
            Assert.Equal(EmulationMachineState.Created, machine.State);
        }
        finally { machine.DisposeAsync().AsTask().GetAwaiter().GetResult(); }
    }

    [Fact]
    public Task CapturedHostMovementReachesTheAcornCoreThroughThePublicSession() => sta.Run(() =>
    {
        var received = new TaskCompletionSource<EmulationInputSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        var core = ControlledDependencies.Simulate<IEmulatorCore>((method, args) =>
        {
            if (method.Name == "SetInput")
            {
                var snapshot = (EmulationInputSnapshot)args[0]!;
                if (snapshot.Pointer.DeltaX != 0) received.TrySetResult(snapshot);
                return null;
            }
            return method.Name switch
            {
                "get_FramesPerSecond" => 50d,
                "get_SampleRate" => 48000,
                "get_Diagnostics" => Array.Empty<string>(),
                _ => method.ReturnType != typeof(void) && method.ReturnType.IsValueType ? Activator.CreateInstance(method.ReturnType) : null
            };
        });
        var configuration = new MachineConfiguration(MachineModel.BbcMaster128, core: Emulator.AcornEmu);
        configuration = configuration with
        {
            Options = new Dictionary<string, string>(configuration.Options)
            { [RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants.UserPortPointerConstants.ConnectedOption] = bool.TrueString },
            Input = configuration.Input with { CaptureMouse = true }
        };
        var machine = new Machine(Guid.NewGuid(), configuration, core,
            Path.Combine(Path.GetTempPath(), "rml-input-" + Guid.NewGuid().ToString("N")), inputMapper: new AcornEmuMachineFactory());
        var view = new MachineView();
        MachineInputController? controller = null;
        var physical = new GameInputPhysicalState(new HashSet<EmulationKey>(), EmulationInputSnapshot.Empty.Pointer, []);
        try
        {
            machine.StartAsync().AsTask().GetAwaiter().GetResult();
            Assert.True(machine.Input.SupportsPointerCapture);
            Assert.True(machine.Input.CapturePointerOnClick);
            controller = new MachineInputController(view, new Border(), IntPtr.Zero, () => machine, [],
                _ => Task.CompletedTask, () => true, new PointerCapture(),
                () => physical, (_, _) => { }, automaticPolling: false);
            controller.SetPowered(true);
            controller.RequestPointerCapture();
            physical = physical with { Pointer = new(24, -16, 120, true, false, false, false, false, -120) };
            controller.Publish(deltaX: 99, deltaY: 88, wheel: -240, horizontalWheel: 240);
            Assert.True(received.Task.Wait(TimeSpan.FromSeconds(5)));
            var input = received.Task.Result;
            Assert.Equal(24, input.Pointer.DeltaX);
            Assert.Equal(-16, input.Pointer.DeltaY);
            Assert.Equal(120, input.Pointer.Wheel);
            Assert.Equal(-120, input.Pointer.HorizontalWheel);
            Assert.True(input.Pointer.Left);
            Assert.False(input.Pointer.Right);
            Assert.False(input.Pointer.Middle);
        }
        finally
        {
            try { controller?.Dispose(); }
            finally
            {
                view.Content = null;
                machine.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
        }
    });

    private sealed class PointerCapture : IRelativeMouseCapture
    {
        public bool IsCaptured { get; private set; }
        public void Capture(FrameworkElement display, FrameworkElement screen, IntPtr nativeHandle) => IsCaptured = true;
        public void Release(FrameworkElement display, IntPtr nativeHandle) => IsCaptured = false;
        public void ProcessMovement(FrameworkElement screen, Action<int, int> moved) { }
    }
}
