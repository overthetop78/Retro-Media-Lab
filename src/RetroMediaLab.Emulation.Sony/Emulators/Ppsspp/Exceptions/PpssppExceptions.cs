using RetroMediaLab.Emulation.Sony.Common.Exceptions;

namespace RetroMediaLab.Emulation.Sony.Emulators.Ppsspp.Exceptions;

internal static class PpssppExceptions
{
    internal static string FullContentPathsRequired() => ErrorMessages.ForEmulator(null, "Emulation.Error.Ppsspp.FullContentPathsRequired");
    internal static string SystemFilesMissing() => ErrorMessages.ForEmulator(null, "Emulation.Error.Ppsspp.SystemFilesMissing");
}
