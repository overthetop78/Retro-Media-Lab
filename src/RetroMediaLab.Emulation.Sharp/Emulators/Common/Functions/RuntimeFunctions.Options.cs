using RetroMediaLab.Emulation.Sharp.Emulators.Common.Dictionaries;
namespace RetroMediaLab.Emulation.Sharp.Emulators.Common.Functions;

internal static class NativeRuntimeOptionFunctions
{
    internal static IReadOnlyDictionary<string, string> Prepare(MachineConfiguration configuration) => CoreCatalog.CreateAdapter(configuration.Core).GetRuntimeOptions(configuration);
}
