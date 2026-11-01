using RetroMediaLab.Emulation.Fairchild.Emulators.Common.Interop.Dictionaries;
using RetroMediaLab.Emulation.Fairchild.Emulators.Common.Interop.Functions;
using Settings = RetroMediaLab.Emulation.Fairchild.Emulators.FreeChaF.Functions.SettingsDescriptionFunctions;
using RetroMediaLab.Emulation.Fairchild.Emulators.FreeChaF.Constants;
using RetroMediaLab.Emulation.Fairchild.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Fairchild.Emulators.FreeChaF.Factories;

internal sealed class FreeChaFMachineFactory() : MachineFactory(EmulatorConstants.Entry), IEmulatorSettings, IEmulatorControllerAdapter, IEmulatorMediaCapabilities
{
    public override IReadOnlyList<InputActionDefinition> DescribeInput(MachineConfiguration configuration, int port, PeripheralCategory peripheral) => ControllerProtocolDictionary.Actions(configuration.Model, Emulator.FreeChaF, port).Select(action => new InputActionDefinition(action.Definition)).ToArray();
    public override EmulationInputSnapshot MapInput(EmulationInputSnapshot snapshot, MachineConfiguration configuration) => NativeInputSnapshotFunctions.Apply(snapshot, configuration.Input, configuration.Model, Emulator.FreeChaF);
    public IReadOnlyList<EmulationMediaDevice> DescribeMediaDevices(MachineConfiguration configuration) => Settings.Storage(configuration);

    public override IReadOnlyList<EmulationSettingsBlock> DescribeSettings(MachineConfiguration configuration, EmulatorManagementContext? context = null) => Settings.Create(configuration);
    public IReadOnlyList<CoreOption> GetSettingsOptions(MachineConfiguration? configuration = null, EmulatorManagementContext? context = null) => Settings.GetOptions();
}
