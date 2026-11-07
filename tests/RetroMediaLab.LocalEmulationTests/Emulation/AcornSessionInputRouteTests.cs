using System.IO;
using System.Windows;
using System.Windows.Controls;
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
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public Task CapturedHostMovementReachesTheAcornCoreThroughThePublicSession(bool gameInputAvailable) => sta.Run(() =>
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
        var physical = new GameInputPhysicalState(new HashSet<EmulationKey>(), EmulationInputSnapshot.Empty.Pointer, [])
        { HasPointerReading = gameInputAvailable };
        try
        {
            machine.StartAsync().AsTask().GetAwaiter().GetResult();
            Assert.True(machine.Input.SupportsPointerCapture);
            Assert.True(machine.Input.CapturePointerOnClick);
            controller = new MachineInputController(view, new Border(), IntPtr.Zero, () => machine, [],
                _ => Task.CompletedTask, () => true, new PointerCapture(),
                () => physical, (_, _) => { }, automaticPolling: false,
                readPointerButtons: () => new(0, 0, 0, false, true, true));
            controller.SetPowered(true);
            controller.RequestPointerCapture();
            physical = physical with { Pointer = new(24, -16, 120, true, false, false, false, false, -120) };
            controller.Publish(deltaX: 99, deltaY: 88, wheel: -240, horizontalWheel: 240);
            Assert.True(received.Task.Wait(TimeSpan.FromSeconds(5)));
            var input = received.Task.Result;
            Assert.Equal(gameInputAvailable ? 24 : 99, input.Pointer.DeltaX);
            Assert.Equal(gameInputAvailable ? -16 : 88, input.Pointer.DeltaY);
            Assert.Equal(gameInputAvailable ? 120 : -240, input.Pointer.Wheel);
            Assert.Equal(gameInputAvailable ? -120 : 240, input.Pointer.HorizontalWheel);
            Assert.Equal(gameInputAvailable, input.Pointer.Left);
            Assert.Equal(!gameInputAvailable, input.Pointer.Right);
            Assert.Equal(!gameInputAvailable, input.Pointer.Middle);
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
