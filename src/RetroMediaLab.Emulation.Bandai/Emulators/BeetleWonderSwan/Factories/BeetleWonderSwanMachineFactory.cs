using RetroMediaLab.Emulation.Bandai.Emulators.Common.Interop.Factories;
using RetroMediaLab.Emulation.Bandai.Emulators.BeetleWonderSwan.Constants;

namespace RetroMediaLab.Emulation.Bandai.Emulators.BeetleWonderSwan.Factories;

internal sealed class BeetleWonderSwanMachineFactory() : MachineFactory(EmulatorConstants.Entry), IEmulatorSettings, IEmulatorContentFormats
{
    public override IReadOnlyDictionary<string, string> GetRuntimeOptions(MachineConfiguration configuration) =>
        RetroMediaLab.Emulation.Bandai.Emulators.BeetleWonderSwan.Functions.PaletteFunctions.RuntimeOptions(configuration);

    public IReadOnlySet<string> ResolveContentExtensions(IReadOnlySet<string> declaredExtensions, bool requiresFilePath)
    {
        var extensions = new HashSet<string>(declaredExtensions, StringComparer.OrdinalIgnoreCase);
        // The native loader consumes ROM bytes rather than selecting a parser by filename.
        if (!requiresFilePath) extensions.Add(EmulatorConstants.RawCartridgeExtension);
        return extensions;
    }
    public override IReadOnlyList<EmulationSettingsBlock> DescribeSettings(MachineConfiguration configuration, EmulatorManagementContext? context = null) => RetroMediaLab.Emulation.Bandai.Emulators.BeetleWonderSwan.Functions.SettingsDescriptionFunctions.Create(configuration);
    public IReadOnlyList<CoreOption> GetSettingsOptions(MachineConfiguration? configuration = null, EmulatorManagementContext? context = null) => RetroMediaLab.Emulation.Bandai.Emulators.BeetleWonderSwan.Dictionaries.BeetleWonderSwanOptionCatalog.All;
}
