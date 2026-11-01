using RetroMediaLab.Emulation.Nec.Common.Services;
using RetroMediaLab.Emulation.Nec.Common.Constants;

namespace RetroMediaLab.Emulation.Nec.Modules;

public sealed class NecEmulationModuleFactory : IEmulationModuleFactory
{
    public string Id => EmulationModuleConstants.ModuleId;

    public IEmulationModule Create(EmulationModuleContext context) => Engine.Create(context);
}
