using RetroMediaLab.Emulation.Interfaces;
using RetroMediaLab.Emulation.Interop;
using RetroMediaLab.Emulation.Panasonic.Emulators.Opera.Interop.Contracts;

namespace RetroMediaLab.Emulation.Panasonic.Emulators.Opera.Interop.Functions;
internal static class CoreFunctions
{
    internal static ExternalCoreExports ResolveExports(IEmulationCoreLibrary library) => library.ResolveExports(requireLoadGameSpecial: true);
}