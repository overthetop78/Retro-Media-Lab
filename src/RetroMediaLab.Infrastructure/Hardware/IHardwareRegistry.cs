using RetroMediaLab.Infrastructure.Settings.Hardware;
namespace RetroMediaLab.Infrastructure.Hardware;

public sealed record HardwareScanResult(IReadOnlyList<ControllerSettings> ConfiguredControllers, IReadOnlyList<ControllerSettings> UnconfiguredControllers);

public interface IHardwareRegistry
{
    Task<HardwareScanResult> ScanAsync(string executable, IReadOnlyList<ControllerSettings> configuredControllers, CancellationToken cancellationToken = default);
}
