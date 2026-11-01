using RetroMediaLab.Emulation.Commodore.Common.Services;

namespace RetroMediaLab.Emulation.Commodore.Modules;

public sealed class CommodoreEmulationModuleFactory : IEmulationModuleFactory
{
    public string Id => EmulationModuleConstants.ModuleId;

    public IEmulationModule Create(EmulationModuleContext context) => Engine.Create(context);
}
