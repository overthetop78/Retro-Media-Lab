using System.IO;
using RetroMediaLab.Emulation.Interfaces;
using RetroMediaLab.Emulation.Snk.Emulators.FbAlpha.Constants;
using CommonCoreHostFunctions = RetroMediaLab.Emulation.Snk.Common.Functions.CoreHostFunctions;

namespace RetroMediaLab.Emulation.Snk.Emulators.FbAlpha.Functions;

internal static class CueRuntimeFunctions
{
    internal static IEmulationCueSheet Prepare(IEmulationCueSheet sheet, string stagedBinary, string stagedCue)
    {
        Validate(sheet);
        return CommonCoreHostFunctions.MediaProcessing.WithSingleCueFile(sheet, Path.GetRelativePath(Path.GetDirectoryName(stagedCue)!, stagedBinary));
    }

    internal static void Validate(IEmulationCueSheet sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        if (!CommonCoreHostFunctions.MediaProcessing.IsSingleBinaryRawAudioCue(sheet, RuntimeOptionConstants.NativeMaximumTrackNumber)) throw Unsupported();
    }

    private static EmulationException Unsupported() => new(ErrorCategory.Content, ErrorCode.ContentUnsupported, ErrorMessages.ContentLoadFailed);
}