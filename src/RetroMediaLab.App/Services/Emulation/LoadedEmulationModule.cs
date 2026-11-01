using RetroMediaLab.Emulation;
using RetroMediaLab.Emulation.Contracts;

namespace RetroMediaLab.App.Services.Emulation;

internal sealed record LoadedEmulationModule(IEmulationModule Module, EmulationModuleManifest Manifest, string InstallationDirectory);
