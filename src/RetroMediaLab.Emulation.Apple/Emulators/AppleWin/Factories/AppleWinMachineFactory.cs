using RetroMediaLab.Emulation.HardDisks;
using CommonHardDiskFormats = RetroMediaLab.Emulation.Apple.Common.Machines.Common.Functions.HardDiskFormats;
using RetroMediaLab.Emulation.Apple.Emulators.Common.Interop.Factories;
using RetroMediaLab.Emulation.Apple.Emulators.AppleWin.Constants;

namespace RetroMediaLab.Emulation.Apple.Emulators.AppleWin.Factories;

internal sealed class AppleWinMachineFactory() : MachineFactory(EmulatorConstants.Entry), IEmulatorSettings, IEmulatorSettingsApplication, IEmulatorStorageSettings, IEmulatorPointerCapabilities, IEmulatorConfigurationOptions, IEmulatorRuntimePolicy
{
    /// <inheritdoc/>
    public IReadOnlyList<CoreOption> GetSettingsOptions(MachineConfiguration? configuration = null, EmulatorManagementContext? context = null) => RetroMediaLab.Emulation.Apple.Emulators.AppleWin.Dictionaries.AppleWinOptionCatalog.All;

    public bool SupportsPointer(MachineConfiguration configuration) => configuration.Options.GetValueOrDefault(OptionConstants.Slot4) == ExpansionConstants.Mouse;
    public MachineConfiguration ApplySettings(MachineConfiguration configuration, IReadOnlyDictionary<string, string?> values) => RetroMediaLab.Emulation.Apple.Emulators.AppleWin.Functions.ExpansionFunctions.ApplySettings(configuration, values);
    public MachineConfiguration ApplyStorageConfiguration(MachineConfiguration configuration, IReadOnlyList<EmulationMediaSlot> slots) => RetroMediaLab.Emulation.Apple.Emulators.AppleWin.Functions.ExpansionFunctions.ApplyStorage(configuration, slots);
    public override IReadOnlyList<EmulationSettingsBlock> DescribeSettings(MachineConfiguration configuration, EmulatorManagementContext? context = null) => RetroMediaLab.Emulation.Apple.Emulators.AppleWin.Functions.SettingsDescriptionFunctions.Create(configuration);
    public void InitializeOptions(MachineConfiguration configuration, IDictionary<string, string> options) => RetroMediaLab.Emulation.Apple.Emulators.AppleWin.Functions.ConfigurationFunctions.InitializeOptions(configuration, options);
    public override void ValidateConfiguration(MachineConfiguration configuration) => RetroMediaLab.Emulation.Apple.Emulators.AppleWin.Functions.ConfigurationFunctions.ValidateConfiguration(configuration);
    public bool RequiresMediaRestart(MachineConfiguration configuration, EmulationMediaSlot slot, string? path) => RetroMediaLab.Emulation.Apple.Emulators.AppleWin.Functions.RuntimeFunctions.RequiresMediaRestart(slot, path);
    public bool RequiresOptionRestart(MachineConfiguration configuration, string key) => RetroMediaLab.Emulation.Apple.Emulators.AppleWin.Functions.RuntimeFunctions.RequiresOptionRestart(key);

    /// <inheritdoc/>
    public IReadOnlyList<HardDiskImageFormat> GetHardDiskFormats(MachineConfiguration configuration, EmulationMediaSlot slot) => slot == EmulationMediaSlot.HardDisk0
        ? [CommonHardDiskFormats.Describe(new(EmulatorConstants.HardDiskFormatId, EmulatorConstants.HardDiskExtension, EmulatorConstants.HardDiskInterface, EmulatorConstants.HardDiskMaximumBytes, EmulatorConstants.HardDiskDefaultBytes, [HardDiskPreparation.Blank]))] : [];
}
