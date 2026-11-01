using RetroMediaLab.Emulation.Coleco.Common.Services;

namespace RetroMediaLab.Emulation.Coleco.Modules;

public sealed class ColecoEmulationModuleFactory : IEmulationModuleFactory
{
    public string Id => EmulationModuleConstants.ModuleId;

    public IEmulationModule Create(EmulationModuleContext context) => Engine.Create(context);
}

