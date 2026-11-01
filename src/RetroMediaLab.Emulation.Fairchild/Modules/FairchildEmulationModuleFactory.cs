using RetroMediaLab.Emulation.Fairchild.Common.Services;

namespace RetroMediaLab.Emulation.Fairchild.Modules;

public sealed class FairchildEmulationModuleFactory : IEmulationModuleFactory
{
    public string Id => EmulationModuleConstants.ModuleId;

    public IEmulationModule Create(EmulationModuleContext context) => Engine.Create(context);
}

