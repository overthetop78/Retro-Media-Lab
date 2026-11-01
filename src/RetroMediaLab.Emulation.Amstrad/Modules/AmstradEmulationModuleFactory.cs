using RetroMediaLab.Emulation.Amstrad.Common.Services;

namespace RetroMediaLab.Emulation.Amstrad.Modules;

public sealed class AmstradEmulationModuleFactory : IEmulationModuleFactory
{
    public string Id => EmulationModuleConstants.ModuleId;

    public IEmulationModule Create(EmulationModuleContext context) => Engine.Create(context);
}
