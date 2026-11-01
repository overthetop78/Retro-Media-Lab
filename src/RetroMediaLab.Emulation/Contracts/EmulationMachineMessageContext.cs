namespace RetroMediaLab.Emulation.Contracts;

public sealed record EmulationMachineMessageContext(string MachineId) : IEmulationMachineMessageContext;
