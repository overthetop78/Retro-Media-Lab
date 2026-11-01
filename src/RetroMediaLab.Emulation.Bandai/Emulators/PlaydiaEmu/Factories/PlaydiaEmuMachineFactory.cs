using RetroMediaLab.Emulation.Bandai.Emulators.Common.Interop.Factories;
using RetroMediaLab.Emulation.Bandai.Emulators.PlaydiaEmu.Constants;

namespace RetroMediaLab.Emulation.Bandai.Emulators.PlaydiaEmu.Factories;

internal sealed class PlaydiaEmuMachineFactory() : MachineFactory(EmulatorConstants.Entry), IEmulatorSettings
{
    public override IReadOnlyList<EmulationSettingsBlock> DescribeSettings(MachineConfiguration configuration, EmulatorManagementContext? context = null) => RetroMediaLab.Emulation.Bandai.Emulators.PlaydiaEmu.Functions.SettingsDescriptionFunctions.Create(configuration);
    public IReadOnlyList<CoreOption> GetSettingsOptions(MachineConfiguration? configuration = null, EmulatorManagementContext? context = null) => RetroMediaLab.Emulation.Bandai.Emulators.PlaydiaEmu.Dictionaries.PlaydiaEmuOptionCatalog.All;
}
