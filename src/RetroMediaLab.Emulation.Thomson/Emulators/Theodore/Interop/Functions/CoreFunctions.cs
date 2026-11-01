using RetroMediaLab.Emulation.Interfaces;
using RetroMediaLab.Emulation.Interop;
using RetroMediaLab.Emulation.Thomson.Emulators.Theodore.Interop.Contracts;

namespace RetroMediaLab.Emulation.Thomson.Emulators.Theodore.Interop.Functions;
internal static class CoreFunctions
{
    internal static ExternalCoreExports ResolveExports(IEmulationCoreLibrary library) => library.ResolveExports(requireLoadGameSpecial: true);
}