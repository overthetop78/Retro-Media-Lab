using RetroMediaLab.Emulation.Interfaces;
using CommonCoreHostFunctions = RetroMediaLab.Emulation.Amstrad.Common.Functions.CoreHostFunctions;
using RetroMediaLab.Emulation.Amstrad.Emulators.Common.Interop.Constants;
using System.IO;

namespace RetroMediaLab.Emulation.Amstrad.Emulators.Common.Interop.Services;

internal static class CoreOptionsReader
{
    internal static IReadOnlyList<CoreOption> Read(string libraryPath)
    {
        if (!File.Exists(libraryPath)) return [];
        var directory = Path.GetDirectoryName(Path.GetFullPath(libraryPath))!;
        IEmulationCoreLibrary? library = null;
        ExternalHostCallbacks? callbacks = null;
        try
        {
            callbacks = new ExternalHostCallbacks(directory, directory, directory, null, createDirectories: false);
            library = CommonCoreHostFunctions.NativeResources.CreateLibrary(libraryPath);
            library.Resolve<ExternalCoreApi.SetEnvironment>(ExternalCoreConstants.RetroSetEnvironment)(callbacks.Environment);
            return callbacks.OptionCatalog.ToArray();
        }
        finally
        {
            try { library?.Dispose(); }
            finally { callbacks?.Dispose(); }
        }
    }
}
