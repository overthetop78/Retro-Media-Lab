using RetroMediaLab.Emulation.Snk.Common.Services;

namespace RetroMediaLab.Emulation.Snk.Modules;

public sealed class SnkEmulationModuleFactory : IEmulationModuleFactory
{
    public string Id => EmulationModuleConstants.ModuleId;

    public IEmulationModule Create(EmulationModuleContext context) => Engine.Create(context);
}
