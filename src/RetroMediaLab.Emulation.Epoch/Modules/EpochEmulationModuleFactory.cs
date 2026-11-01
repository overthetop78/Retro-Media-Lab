using RetroMediaLab.Emulation.Epoch.Common.Services;

namespace RetroMediaLab.Emulation.Epoch.Modules;

public sealed class EpochEmulationModuleFactory : IEmulationModuleFactory
{
    public string Id => EmulationModuleConstants.ModuleId;

    public IEmulationModule Create(EmulationModuleContext context) => Engine.Create(context);
}

