namespace RetroMediaLab.Emulation.Interfaces;

public interface IEmulationMachineMessageContext : IEmulationMessageContext
{
    string MachineId { get; }
}
