using RetroMediaLab.Emulation.Philips.Emulators.Common.Interop.Factories;
using RetroMediaLab.Emulation.Philips.Emulators.M2000.Functions;
using Settings = RetroMediaLab.Emulation.Philips.Emulators.M2000.Functions.SettingsDescriptionFunctions;
using RetroMediaLab.Emulation.Philips.Emulators.M2000.Constants;

namespace RetroMediaLab.Emulation.Philips.Emulators.M2000.Factories;

internal sealed class M2000MachineFactory() : LibretroMachineFactory(EmulatorConstants.Entry), IEmulatorSettings, IEmulatorMediaCapabilities
{
    public override IReadOnlyList<EmulationSettingsBlock> DescribeSettings(MachineConfiguration configuration, EmulatorManagementContext? context = null) => Settings.Create(configuration);
    public IReadOnlyList<CoreOption> GetSettingsOptions(MachineConfiguration? configuration = null, EmulatorManagementContext? context = null) => Settings.GetOptions();
    public IReadOnlyList<EmulationMediaDevice> DescribeMediaDevices(MachineConfiguration configuration) => RetroMediaLab.Emulation.Philips.Emulators.M2000.Functions.StorageDescriptionFunctions.Describe(configuration);

    public override bool RequiresMediaRestart(MachineConfiguration configuration, EmulationMediaSlot slot, string? path) => true;

    public override IReadOnlyDictionary<string, string> GetRuntimeOptions(MachineConfiguration configuration) => RetroMediaLab.Emulation.Philips.Emulators.M2000.Functions.M2000RuntimeFunctions.Options(configuration);
}
