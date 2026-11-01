using RetroMediaLab.Emulation.Sharp.Emulators.Common.Interop.Factories;
using RetroMediaLab.Emulation.Sharp.Emulators.Common.Interop.Functions;
using RetroMediaLab.Emulation.Sharp.Emulators.Common.Interop.Services;
using RetroMediaLab.Emulation.Sharp.Emulators.X1.Functions;
using RetroMediaLab.Emulation.Sharp.Emulators.X1.Constants;
using Settings = RetroMediaLab.Emulation.Sharp.Emulators.X1.Functions.SettingsDescriptionFunctions;
using FirmwarePreparation = RetroMediaLab.Emulation.Sharp.Emulators.X1.Functions.FirmwareRuntimeFunctions;
using RuntimeOptions = RetroMediaLab.Emulation.Sharp.Emulators.X1.Functions.RuntimeOptionFunctions;

namespace RetroMediaLab.Emulation.Sharp.Emulators.X1.Factories;

internal sealed class X1MachineFactory() : LibretroMachineFactory(EmulatorConstants.Entry), IEmulatorSettings
{
    public override IReadOnlyList<EmulationSettingsBlock> DescribeSettings(MachineConfiguration configuration, EmulatorManagementContext? context = null) => Settings.Create(configuration);
    public IReadOnlyList<CoreOption> GetSettingsOptions(MachineConfiguration? configuration = null, EmulatorManagementContext? context = null) => Settings.GetOptions(configuration);

    protected override bool RequiresResetRestart => true;
    internal override LoadedContent? PrepareContent(MachineConfiguration configuration, string sessionDirectory, IReadOnlySet<string> supportedExtensions) => RetroMediaLab.Emulation.Sharp.Emulators.X1.Functions.MediaRuntimeFunctions.Prepare(configuration, sessionDirectory, supportedExtensions);
    public override bool RequiresMediaRestart(MachineConfiguration configuration, EmulationMediaSlot slot, string? path) => slot != EmulationMediaSlot.Floppy0;
    public override void PrepareFirmware(MachineConfiguration configuration, string systemDirectory) => FirmwarePreparation.Prepare(configuration, systemDirectory);
    public override IReadOnlyDictionary<string, string> GetRuntimeOptions(MachineConfiguration configuration) => RuntimeOptions.Prepare(configuration);
    public override IReadOnlyList<EmulationMediaDevice> DescribeMediaDevices(MachineConfiguration configuration) => [new(EmulationMediaSlot.Floppy0, EmulationMediaType.Floppy, MediaRuntimeConstants.FloppyExtensions)];
}
