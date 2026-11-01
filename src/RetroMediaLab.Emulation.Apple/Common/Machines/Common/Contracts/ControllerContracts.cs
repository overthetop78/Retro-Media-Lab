using RetroMediaLab.Emulation.Apple.Common.Machines.Common.Enums;
using RetroMediaLab.Emulation.Apple.Common.Constants;
namespace RetroMediaLab.Emulation.Apple.Common.Machines.Common.Contracts;

/// <param name="OutputBindings">Associations des commandes de retour du cœur aux périphériques physiques et à leurs moteurs ; null en absence de réglage.</param>
public sealed record ControllerBinding(
    int Port,
    PeripheralCategory Peripheral,
    string? DeviceId = null,
    IReadOnlyDictionary<string, string>? Mappings = null,
    int DeadZonePercent = ControllerConstants.DefaultDeadZonePercent,
    string? VisualId = null, IReadOnlyDictionary<string, EmulationControllerOutputBinding>? OutputBindings = null);
