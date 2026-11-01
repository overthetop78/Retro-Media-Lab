using RetroMediaLab.Emulation.Enums;

namespace RetroMediaLab.App.Contracts.Machine;

internal sealed record MachineViewDevice(string Key, string Label, string Glyph, bool Removable, bool Present, Func<Task>? Insert, Func<Task>? Eject, string? Status = null, IReadOnlyList<MachineViewDeviceCommand>? Commands = null, EmulationMediaType? MediaType = null);
