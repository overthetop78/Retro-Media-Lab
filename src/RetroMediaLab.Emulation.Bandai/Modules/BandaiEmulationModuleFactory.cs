using RetroMediaLab.Emulation.Bandai.Common.Services;

namespace RetroMediaLab.Emulation.Bandai.Modules;

public sealed class BandaiEmulationModuleFactory : IEmulationModuleFactory
{
    public string Id => EmulationModuleConstants.ModuleId;

    public IEmulationModule Create(EmulationModuleContext context) => Engine.Create(context);
}
