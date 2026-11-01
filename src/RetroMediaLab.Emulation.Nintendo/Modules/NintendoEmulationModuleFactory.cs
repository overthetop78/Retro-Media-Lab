using RetroMediaLab.Emulation.Nintendo.Common.Services;
using RetroMediaLab.Emulation.Nintendo.Common.Constants;

namespace RetroMediaLab.Emulation.Nintendo.Modules;

public sealed class NintendoEmulationModuleFactory : IEmulationModuleFactory
{
    public string Id => EmulationModuleConstants.ModuleId;

    public IEmulationModule Create(EmulationModuleContext context) => Engine.Create(context);
}
