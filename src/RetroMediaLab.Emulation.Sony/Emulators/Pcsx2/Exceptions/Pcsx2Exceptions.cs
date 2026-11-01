using RetroMediaLab.Emulation.Sony.Common.Exceptions;

namespace RetroMediaLab.Emulation.Sony.Emulators.Pcsx2.Exceptions;

internal static class Pcsx2Exceptions
{
    internal static string FullContentPathsRequired() => ErrorMessages.ForEmulator(null, "Emulation.Error.Pcsx2.FullContentPathsRequired");
}
