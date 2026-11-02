using RetroMediaLab.App.Controllers.Emulation.Firmware;
using RetroMediaLab.Emulation;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Interfaces;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.Tests.Application.TestInfrastructure;

namespace RetroMediaLab.Tests.Interface.EmulationViews;

[Collection("WPF")]
public sealed class FirmwareRefreshCancellationTests(StaExecutionScenarios sta)
{
    [Fact]
    public Task ClosingControllerDuringScanCompletesRefreshWithoutDispatcherFailure() => sta.RunAsync(async () =>
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var configuration = ControlledDependencies.Simulate<IEmulationConfiguration>((method, _) =>
            method.Name == "get_MachineId" ? "machine" : throw new InvalidOperationException(method.Name));
        var manager = ControlledDependencies.Simulate<IEmulationFirmwareManager>((method, args) =>
            method.Name == nameof(IEmulationFirmwareManager.ScanFirmwareAsync)
                ? new ValueTask<IReadOnlyList<EmulationFirmwareCandidate>>(Scan((CancellationToken)args[2]!))
                : throw new InvalidOperationException(method.Name));
        var module = ControlledDependencies.Reject<IEmulationModule>();
        var controller = new EmulationFirmwareManagementController(module, manager, () => configuration);
        try
        {
            controller.CreateField(new("firmware", EmulationMachineTab.Rom, "firmware", "firmware",
                EmulationSettingsEditor.Text, null), () => Task.CompletedTask);
            var refresh = controller.RefreshAsync();
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await controller.DisposeAsync();
            await refresh;
        }
        finally { await controller.DisposeAsync(); }

        async Task<IReadOnlyList<EmulationFirmwareCandidate>> Scan(CancellationToken token)
        {
            entered.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return [];
        }
    });

    [Fact]
    public Task ScanCancellationWithoutClosingAndOtherFailuresRemainObservable() => sta.RunAsync(async () =>
    {
        foreach (var failure in new Exception[] { new OperationCanceledException(), new InvalidOperationException("scan failed") })
        {
            var configuration = ControlledDependencies.Simulate<IEmulationConfiguration>((method, _) =>
                method.Name == "get_MachineId" ? "machine" : throw new InvalidOperationException(method.Name));
            var manager = ControlledDependencies.Simulate<IEmulationFirmwareManager>((_, _) =>
                ValueTask.FromException<IReadOnlyList<EmulationFirmwareCandidate>>(failure));
            var controller = new EmulationFirmwareManagementController(ControlledDependencies.Reject<IEmulationModule>(), manager, () => configuration);
            try
            {
                controller.CreateField(new("firmware", EmulationMachineTab.Rom, "firmware", "firmware",
                    EmulationSettingsEditor.Text, null), () => Task.CompletedTask);
                var observed = await Record.ExceptionAsync(controller.RefreshAsync);
                Assert.Same(failure, observed);
            }
            finally { await controller.DisposeAsync(); }
        }
    });
}
