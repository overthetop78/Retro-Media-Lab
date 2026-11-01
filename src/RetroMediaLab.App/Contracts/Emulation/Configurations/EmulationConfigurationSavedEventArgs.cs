using RetroMediaLab.Emulation;

namespace RetroMediaLab.App.Contracts.Emulation.Configurations;

public sealed class EmulationConfigurationSavedEventArgs(IEmulationConfiguration configuration) : EventArgs
{
    public IEmulationConfiguration Configuration { get; } = configuration;
}
