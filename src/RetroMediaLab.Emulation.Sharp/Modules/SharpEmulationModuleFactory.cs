using RetroMediaLab.Emulation.Sharp.Common.Services;

namespace RetroMediaLab.Emulation.Sharp.Modules;

public sealed class SharpEmulationModuleFactory : IEmulationModuleFactory
{
    public string Id => EmulationModuleConstants.ModuleId;

    public IEmulationModule Create(EmulationModuleContext context) => Engine.Create(context);
}
