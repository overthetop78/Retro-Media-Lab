using RetroMediaLab.Emulation.Interfaces;
using RetroMediaLab.Emulation.Interop;
using RetroMediaLab.Emulation.Wenquxing.Emulators.Wqxemu.Interop.Contracts;

namespace RetroMediaLab.Emulation.Wenquxing.Emulators.Wqxemu.Interop.Functions;
internal static class CoreFunctions
{
    internal static ExternalCoreExports ResolveExports(IEmulationCoreLibrary library) => library.ResolveExports(requireLoadGameSpecial: true);
}