using RetroMediaLab.Emulation.Philips.Emulators.Common.Dictionaries;
namespace RetroMediaLab.Emulation.Philips.Emulators.Common.Functions;

internal static class NativeRuntimeOptionFunctions
{
    internal static IReadOnlyDictionary<string, string> Prepare(MachineConfiguration configuration) => CoreCatalog.CreateAdapter(configuration.Core).GetRuntimeOptions(configuration);
}
