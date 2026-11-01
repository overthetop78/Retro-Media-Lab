using RetroMediaLab.Emulation.Sega.Common.Services;
using RetroMediaLab.Emulation.Sega.Common.Constants;

namespace RetroMediaLab.Emulation.Sega.Modules;

public sealed class SegaEmulationModuleFactory : IEmulationModuleFactory
{
    public string Id => EmulationModuleConstants.ModuleId;

    public IEmulationModule Create(EmulationModuleContext context) => Engine.Create(context);
}
