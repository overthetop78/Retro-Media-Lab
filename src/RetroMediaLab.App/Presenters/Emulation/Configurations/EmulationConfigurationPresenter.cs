using RetroMediaLab.App.Constants.Controls.Visual;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.Emulation;

namespace RetroMediaLab.App.Presenters.Emulation.Configurations;

internal static class EmulationConfigurationPresenter
{
    internal static string DisplayName(IEmulationModule module, IEmulationConfiguration configuration)
    {
        var summary = module.SummarizeConfiguration(configuration);
        var identifier = configuration.Id.ToString(ControlVisualConstants.IdentifierFormat) [..ControlVisualConstants.DisplayIdentifierLength];
        return string.Join(ControlVisualConstants.DetailSeparator, new[] { LocExtension.GetForModule(module, summary.MachineDisplayResourceKey) }.Concat(summary.Details).Append(identifier));
    }
}
