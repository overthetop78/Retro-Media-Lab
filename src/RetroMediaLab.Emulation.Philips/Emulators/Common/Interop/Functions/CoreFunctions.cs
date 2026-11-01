using RetroMediaLab.Emulation.Interfaces;
using CommonCoreHostFunctions = RetroMediaLab.Emulation.Philips.Common.Functions.CoreHostFunctions;
using RetroMediaLab.Emulation.Philips.Emulators.Common.Dictionaries;
using RetroMediaLab.Emulation.Philips.Emulators.Common.Interop.Constants;
using RetroMediaLab.Emulation.Philips.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Philips.Emulators.Common.Interop.Services;
using System.Globalization;
using System.Runtime.InteropServices;

namespace RetroMediaLab.Emulation.Philips.Emulators.Common.Interop.Functions;

internal static class CoreFunctions
{

    internal static ExternalCoreInfo ReadInfo(RetroMediaLab.Emulation.Interop.ExternalCoreApi.SystemInfo nativeInfo, Emulator expectedEmulator)
        => CommonCoreHostFunctions.NativeResources.ReadInfo(nativeInfo, expectedEmulator, ExpectedLibraryName, CommonCoreHostFunctions.NativeResources.ParseExtensions,
            static (expectedName, libraryName) => new EmulationException(ErrorCategory.Core, ErrorCode.CoreRejected,
                ErrorMessages.CoreIdentityMismatch,
                new Dictionary<string, string>
                {
                    [ErrorContextConstants.Expected] = expectedName,
                    [ErrorContextConstants.Actual] = libraryName
                }),
            static (emulator, libraryName, libraryVersion, extensions, needsFullPath, blocksArchiveExtraction)
                => new ExternalCoreInfo(emulator, libraryName, libraryVersion, extensions, needsFullPath, blocksArchiveExtraction));
    internal static string CreateInvalidOptionValueMessage(string key, string value) => string.Format(CultureInfo.InvariantCulture, ErrorMessages.OptionValueInvalidFormat, key, value);
    internal static string ExpectedLibraryName(Emulator emulator) => CoreCatalog.Get(emulator).LibraryName;

    internal static ExternalCoreExports ResolveExports(IEmulationCoreLibrary library) => library.ResolveExports(requireLoadGameSpecial: true);

    internal static void InstallCallbacks(ExternalCoreExports exports, ExternalHostCallbacks callbacks)
    => CommonCoreHostFunctions.EnvironmentProcessing.InstallCallbacks(exports, callbacks);

    internal static bool WritePointer(nint destination, nint value)
        => CommonCoreHostFunctions.EnvironmentProcessing.WritePointer(destination, value);

    internal static bool WriteInteger(nint destination, int value)
        => CommonCoreHostFunctions.EnvironmentProcessing.WriteInteger(destination, value);
}
