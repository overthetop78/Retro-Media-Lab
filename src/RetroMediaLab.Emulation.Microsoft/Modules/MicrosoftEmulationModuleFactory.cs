using RetroMediaLab.Emulation.Microsoft.Common.Services;
using RetroMediaLab.Emulation.Microsoft.Common.Constants;

namespace RetroMediaLab.Emulation.Microsoft.Modules;

public sealed class MicrosoftEmulationModuleFactory : IEmulationModuleFactory
{
    public string Id => EmulationModuleConstants.ModuleId;

    public IEmulationModule Create(EmulationModuleContext context) => Engine.Create(context);
}
