using RetroMediaLab.Emulation;

namespace RetroMediaLab.App.Contracts.Emulation.Configurations;

internal sealed record EmulationConfigurationListItem(IEmulationModule Module, IEmulationConfiguration Configuration, string DisplayName);

internal sealed record EmulationModuleListItem(IEmulationModule Module, string DisplayName);
