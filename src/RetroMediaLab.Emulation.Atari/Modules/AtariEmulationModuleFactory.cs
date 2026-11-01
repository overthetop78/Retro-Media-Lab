using RetroMediaLab.Emulation.Atari.Common.Services;
namespace RetroMediaLab.Emulation.Atari.Modules;

public sealed class AtariEmulationModuleFactory : IEmulationModuleFactory
{
    public string Id => EmulationModuleConstants.ModuleId;

    public IEmulationModule Create(EmulationModuleContext context) => Engine.Create(context);
}
