using RetroMediaLab.Emulation.Interfaces;
using RetroMediaLab.Emulation.Interop;
using RetroMediaLab.Emulation.Watara.Emulators.Potator.Interop.Contracts;

namespace RetroMediaLab.Emulation.Watara.Emulators.Potator.Interop.Functions;
internal static class CoreFunctions
{
    internal static ExternalCoreExports ResolveExports(IEmulationCoreLibrary library) => library.ResolveExports(requireLoadGameSpecial: true);
}