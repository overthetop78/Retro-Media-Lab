using RetroMediaLab.Emulation.Watara.Common.Services;

namespace RetroMediaLab.Emulation.Watara.Modules;

public sealed class WataraEmulationModuleFactory : IEmulationModuleFactory
{
    public string Id => EmulationModuleConstants.ModuleId;

    public IEmulationModule Create(EmulationModuleContext context) => Engine.Create(context);
}
