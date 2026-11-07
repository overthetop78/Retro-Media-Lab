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
    [Theory]
    [InlineData(EmulationFirmwareCompatibility.Official)]
    [InlineData(EmulationFirmwareCompatibility.Compatible)]
    [InlineData(EmulationFirmwareCompatibility.PartiallyCompatible)]
    [InlineData(EmulationFirmwareCompatibility.Unknown)]
    public void SelectorHidesIncompatibleRomsAfterResolvingAllRoles(EmulationFirmwareCompatibility allowed)
    {
        const string destination = "selected-field";
        EmulationFirmwareCandidate[] entries =
        [
            new("wrong-role", "shared.rom", "Other role", null, EmulationFirmwareCompatibility.Official, "other-field"),
            new("right-role", "shared.rom", "Requested role", null, allowed, destination),
            new("incompatible", "incompatible.rom", "Incompatible", null, EmulationFirmwareCompatibility.Incompatible, destination),
            new("other", "other.rom", "Other role only", null, EmulationFirmwareCompatibility.Compatible, "other-field"),
            new("unknown", "unknown.rom", "Unidentified", null, EmulationFirmwareCompatibility.Unknown)
        ];
        var visible = EmulationFirmwareManagementController.CandidatesForField(entries, destination);
        Assert.Equal(2, visible.Count);
        Assert.Contains(visible, candidate => candidate.Id == "right-role" && candidate.Compatibility == allowed);
        Assert.Contains(visible, candidate => candidate.Id == "unknown" && candidate.DestinationFieldId == destination
            && candidate.Compatibility == EmulationFirmwareCompatibility.Unknown);
        Assert.DoesNotContain(visible, candidate => candidate.Compatibility == EmulationFirmwareCompatibility.Incompatible);
    }

    [Fact]
    public Task FirmwarePathEditorReturnsTheEditedPath() => sta.RunAsync(async () =>
    {
        var controller = new EmulationFirmwareManagementController(ControlledDependencies.Reject<IEmulationModule>(),
            ControlledDependencies.Reject<IEmulationFirmwareManager>(), () => throw new InvalidOperationException());
        try
        {
            var row = controller.CreateField(new("firmware", EmulationMachineTab.Rom, "firmware", "firmware",
                EmulationSettingsEditor.Path, "original.rom"), () => Task.CompletedTask);
            var editor = Assert.IsType<System.Windows.Controls.TextBox>(row.Children[0]);
            editor.Text = "replacement.rom";
            Assert.Equal("replacement.rom", Assert.IsType<Func<string>>(row.Tag)());
        }
        finally { await controller.DisposeAsync(); }
    });

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
