using System.Globalization;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Interfaces;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornElectron.Common.Services;
using C = RetroMediaLab.Emulation.Acorn.Common.Machines.AcornElectron.Constants.ElectronModelConstants;
using Tape = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants.CassetteConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Functions;

internal static partial class ElectronFunctions
{
    internal static bool IsModel(MachineConfiguration configuration) => configuration.Model == MachineModel.AcornElectron;

    internal static IReadOnlyDictionary<string, string> Defaults(MachineConfiguration configuration) =>
        new Dictionary<string, string>(StringComparer.Ordinal) { [Tape.ConnectedOption] = bool.TrueString };

    internal static IAcornMachine Create(MachineConfiguration configuration)
    {
        var system = ReadImage(configuration, EmulatorConstants.SystemFirmware);
        var basic = ReadImage(configuration, EmulatorConstants.BasicFirmware);
        if (basic.Length == 0) basic = system.AsSpan(0, C.RomBytes).ToArray();
        else if (basic.Length == C.CombinedRomBytes) basic = basic.AsSpan(0, C.RomBytes).ToArray();
        if (system.Length == C.CombinedRomBytes) system = system.AsSpan(C.RomBytes, C.RomBytes).ToArray();
        return new ElectronMachine(system, basic);
    }

    internal static IReadOnlyList<EmulationSettingsBlock> Describe(MachineConfiguration configuration) =>
    [
        new(C.Group, EmulationMachineTab.Cpu, SettingsConstants.ResourceCpu,
            [new(C.Group, EmulationMachineTab.Cpu, C.Group, SettingsConstants.ResourceCpu, EmulationSettingsEditor.Selection,
                nameof(HardwareCpu.Mos6502A), [new(nameof(HardwareCpu.Mos6502A), SettingsConstants.CpuResourcePrefix + nameof(HardwareCpu.Mos6502A))], IsEnabled: false)]),
        new(C.Group, EmulationMachineTab.Ram, SettingsConstants.ResourceMemory,
            [new(SettingsConstants.MainMemory, EmulationMachineTab.Ram, C.Group, SettingsConstants.ResourceMemory,
                EmulationSettingsEditor.Selection, C.MemoryKib.ToString(CultureInfo.InvariantCulture),
                [new(C.MemoryKib.ToString(CultureInfo.InvariantCulture), string.Empty, C.MemoryKib.ToString(CultureInfo.InvariantCulture))], IsEnabled: false)]),
        new(C.RomGroup, EmulationMachineTab.Rom, SettingsConstants.ResourceFirmware,
            Slots(configuration).Select(slot => new EmulationSettingsField(slot.FieldId, EmulationMachineTab.Rom, C.RomGroup,
                slot.LabelResourceKey, EmulationSettingsEditor.Path, configuration.Options.GetValueOrDefault(slot.FieldId),
                DefaultFolderCategory: EmulationDefaultFolderCategory.Firmware, RequiresRestart: true, RefreshSettingsOnChange: true)).ToArray())
    ];

    internal static IReadOnlyList<InputBindingDefinition> KeyboardDefinitions() => [new(nameof(EmulationKey.Break), C.BreakLabel, nameof(EmulationKey.Break))];

    internal static EmulationInputSnapshot MapInput(EmulationInputSnapshot snapshot, MachineConfiguration configuration)
    {
        var bindings = KeyboardDefinitions().ToDictionary(binding => binding.Id, binding => binding.DefaultBinding, StringComparer.Ordinal);
        foreach (var binding in configuration.Input.KeyboardBindings ?? new Dictionary<string, string>()) bindings[binding.Key] = binding.Value;
        return snapshot with { Keys = EmulationSdk.InputProcessing.MapKeyboard(snapshot, bindings), Controllers = [] };
    }

    internal static decimal MemoryKib(MachineConfiguration configuration) => C.MemoryKib;

    internal static EmulationConfigurationSummary Summary(MachineConfiguration configuration) => new(
        HardwareModelCatalog.Get(configuration.Model).DisplayNameResourceKey,
        [string.Format(CultureInfo.InvariantCulture, C.CpuSummaryFormat, HardwareCpu.Mos6502A),
         string.Format(CultureInfo.InvariantCulture, C.MemorySummaryFormat, C.MemoryKib),
         string.Format(CultureInfo.InvariantCulture, C.CoreSummaryFormat, configuration.Core)]);

    internal static void Validate(MachineConfiguration configuration)
    {
        if (NetworkFunctions.Read(configuration).Enabled || StorageFunctions.HasWinchester(configuration)
            || Enumerable.Range(0, StorageConstants.DriveCount).Any(drive => StorageFunctions.Connected(configuration, drive)))
            throw new ArgumentException(ErrorMessages.IncompatibleMedia, nameof(configuration));
        if (EmulationCassetteSettings.Read(configuration.Options).BaudRate is not (0 or C.CassetteBaud))
            throw new ArgumentException(ErrorMessages.OptionValueInvalidFormat, nameof(configuration));
    }
}
