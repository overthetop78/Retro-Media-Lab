using RetroMediaLab.Emulation.Panasonic.Common.Services;

namespace RetroMediaLab.Emulation.Panasonic.Modules;

public sealed class PanasonicEmulationModuleFactory : IEmulationModuleFactory
{
    public string Id => EmulationModuleConstants.ModuleId;

    public IEmulationModule Create(EmulationModuleContext context) => Engine.Create(context);
}
