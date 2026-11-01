namespace RetroMediaLab.Emulation.Interfaces;

public interface IEmulationEmulatorMessageContext : IEmulationMessageContext
{
    string EmulatorId { get; }
}
