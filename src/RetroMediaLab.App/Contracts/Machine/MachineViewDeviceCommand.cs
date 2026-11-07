namespace RetroMediaLab.App.Contracts.Machine;

internal sealed record MachineViewDeviceCommand(EmulationTapeCommand Command, string Label, string Glyph, bool IsSupported, bool IsEnabled, bool IsActive, bool IsBlinking, Func<Task> Execute);
