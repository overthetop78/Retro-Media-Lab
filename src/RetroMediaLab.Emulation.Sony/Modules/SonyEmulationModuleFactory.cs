using RetroMediaLab.Emulation.Sony.Common.Services;
using RetroMediaLab.Emulation.Sony.Common.Constants;

namespace RetroMediaLab.Emulation.Sony.Modules;

public sealed class SonyEmulationModuleFactory : IEmulationModuleFactory
{
    public string Id => EmulationModuleConstants.ModuleId;

    public IEmulationModule Create(EmulationModuleContext context) => Engine.Create(context);
}
