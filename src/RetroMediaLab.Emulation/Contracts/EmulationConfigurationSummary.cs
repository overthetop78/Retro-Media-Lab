namespace RetroMediaLab.Emulation.Contracts;

public sealed record EmulationConfigurationSummary(string MachineDisplayResourceKey, IReadOnlyList<string> Details);
