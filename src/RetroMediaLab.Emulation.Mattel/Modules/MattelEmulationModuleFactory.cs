using RetroMediaLab.Emulation.Mattel.Common.Services;

namespace RetroMediaLab.Emulation.Mattel.Modules;

public sealed class MattelEmulationModuleFactory : IEmulationModuleFactory
{
    public string Id => EmulationModuleConstants.ModuleId;

    public IEmulationModule Create(EmulationModuleContext context) => Engine.Create(context);
}
