namespace RetroMediaLab.Emulation.Interfaces;

public interface IEmulationConfiguration
{
    string ModuleId { get; }
    Guid Id { get; }
    string MachineId { get; }
}
