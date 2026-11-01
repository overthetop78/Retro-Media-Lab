namespace RetroMediaLab.Emulation.Interfaces;

public interface IEmulationModuleFactory
{
    string Id { get; }
    IEmulationModule Create(EmulationModuleContext context);
}
