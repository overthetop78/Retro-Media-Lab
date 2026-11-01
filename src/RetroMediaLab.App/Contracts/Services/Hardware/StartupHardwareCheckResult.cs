using RetroMediaLab.Infrastructure.Settings.Hardware;
namespace RetroMediaLab.App.Contracts.Services.Hardware;

public sealed record StartupHardwareCheckResult(bool Performed, IReadOnlyList<ControllerSettings> MissingControllers, IReadOnlyList<ControllerSettings> NewControllers);
