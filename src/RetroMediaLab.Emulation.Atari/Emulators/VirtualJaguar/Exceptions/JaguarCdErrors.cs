using RetroMediaLab.Emulation.Atari.Emulators.VirtualJaguar.Constants;
using RetroMediaLab.Emulation.Atari.Emulators.VirtualJaguar.Contracts;
using RetroMediaLab.Emulation.Atari.Emulators.VirtualJaguar.Functions;

namespace RetroMediaLab.Emulation.Atari.Emulators.VirtualJaguar.Exceptions;

internal static class JaguarCdErrors
{
    internal static string ModelRequired => ErrorMessages.IncompatibleMedia;
    internal static string CompleteDiscRequired => ErrorMessages.ContentExtensionUnsupported;
    internal static string MissingCueTrack => ErrorMessages.ContentFileMissing;
    internal static string EmptyCue => ErrorMessages.ContentLoadFailed;
    internal static string FileUnreadable => ErrorMessages.ContentFileMissing;
    internal static string EjectionUnsupported => ErrorMessages.DynamicMediaUnsupported;
}
