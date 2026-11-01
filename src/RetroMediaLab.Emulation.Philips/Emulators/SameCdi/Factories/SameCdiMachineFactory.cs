using RetroMediaLab.Emulation.Philips.Emulators.Common.Interop.Constants;
using RetroMediaLab.Emulation.Philips.Emulators.Common.Interop.Factories;
using RetroMediaLab.Emulation.Philips.Emulators.SameCdi.Functions;
using Settings = RetroMediaLab.Emulation.Philips.Emulators.SameCdi.Functions.SettingsDescriptionFunctions;
using RetroMediaLab.Emulation.Philips.Emulators.SameCdi.Constants;

using System.IO;

namespace RetroMediaLab.Emulation.Philips.Emulators.SameCdi.Factories;

internal sealed class SameCdiMachineFactory() : LibretroMachineFactory(EmulatorConstants.Entry), IEmulatorSettings, IEmulatorMediaCapabilities
{
    public override IReadOnlyList<EmulationSettingsBlock> DescribeSettings(MachineConfiguration configuration, EmulatorManagementContext? context = null) => Settings.Create(configuration);
    public IReadOnlyList<CoreOption> GetSettingsOptions(MachineConfiguration? configuration = null, EmulatorManagementContext? context = null) => Settings.GetOptions();
    public IReadOnlyList<EmulationMediaDevice> DescribeMediaDevices(MachineConfiguration configuration) => RetroMediaLab.Emulation.Philips.Emulators.SameCdi.Functions.StorageDescriptionFunctions.Describe(configuration);

    public override bool RequiresMediaRestart(MachineConfiguration configuration, EmulationMediaSlot slot, string? path) => path is not null && string.Equals(Path.GetExtension(path), MediaRuntimeConstants.PlaylistExtension, StringComparison.OrdinalIgnoreCase);

    public override IReadOnlyDictionary<string, string> GetRuntimeOptions(MachineConfiguration configuration) => RetroMediaLab.Emulation.Philips.Emulators.SameCdi.Functions.SameCdiRuntimeFunctions.Options(configuration);
}
