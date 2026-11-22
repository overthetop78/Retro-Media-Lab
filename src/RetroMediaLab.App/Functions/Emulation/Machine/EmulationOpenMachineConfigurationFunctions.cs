using RetroMediaLab.App.Controllers.Emulation.Machine;
using RetroMediaLab.App.Services.Emulation;
using RetroMediaLab.Emulation;

namespace RetroMediaLab.App.Functions.Emulation.Machine;

internal static class EmulationOpenMachineConfigurationFunctions
{
    internal static async Task ApplyAsync(MachineController controller, IReadOnlyList<IEmulationModule> modules,
        IEmulationConfiguration configuration, bool videoOnly)
    {
        var profile = EmulationVideoPresentationProfiles.Store.Get(configuration.ModuleId, configuration.Id);
        controller.ApplyVideoConfiguration(profile.Renderer, profile.Processing!);
        var module = modules.First(item => item.Id == configuration.ModuleId);
        if (!videoOnly) await controller.ApplyConfigurationAsync(configuration);
        await controller.ApplyRuntimeOptionsAsync(module.RuntimeOptions(configuration));
    }
}
