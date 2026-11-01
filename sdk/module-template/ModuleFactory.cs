using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Interfaces;

namespace Example.RetroMediaLab.Module;

// Replace the example identity and return your IEmulationModule implementation.
public sealed class ModuleFactory : IEmulationModuleFactory
{
    public string Id => "example";

    public IEmulationModule Create(EmulationModuleContext context) => throw new NotImplementedException("Replace this line with your module implementation.");
}
