using RetroMediaLab.Emulation.Thomson.Common.Services;

namespace RetroMediaLab.Emulation.Thomson.Modules;

public sealed class ThomsonEmulationModuleFactory : IEmulationModuleFactory
{
    public string Id => EmulationModuleConstants.ModuleId;

    public IEmulationModule Create(EmulationModuleContext context) => Engine.Create(context);
}
