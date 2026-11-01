using RetroMediaLab.Emulation.Acorn.Common.Services;

namespace RetroMediaLab.Emulation.Acorn.Modules;

public sealed class AcornEmulationModuleFactory : IEmulationModuleFactory
{
    public string Id => EmulationModuleConstants.ModuleId;

    public IEmulationModule Create(EmulationModuleContext context) => Engine.Create(context);
}
