using RetroMediaLab.Emulation.Wenquxing.Common.Services;

namespace RetroMediaLab.Emulation.Wenquxing.Modules;

public sealed class WenquxingEmulationModuleFactory : IEmulationModuleFactory
{
    public string Id => EmulationModuleConstants.ModuleId;

    public IEmulationModule Create(EmulationModuleContext context) => Engine.Create(context);
}
