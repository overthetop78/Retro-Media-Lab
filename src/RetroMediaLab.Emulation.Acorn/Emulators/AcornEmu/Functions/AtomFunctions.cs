using System.Globalization;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Interfaces;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornAtom.Common.Services;
using C = RetroMediaLab.Emulation.Acorn.Common.Machines.AcornAtom.Constants.AtomModelConstants;
using Tape = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants.TapeConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Functions;

internal static partial class AtomFunctions
{
    internal static bool IsModel(MachineConfiguration configuration) => configuration.Model == MachineModel.AcornAtom;
    internal static bool HasDiscPack(MachineConfiguration configuration) => bool.Parse(configuration.Options.GetValueOrDefault(C.DiscPackOption, bool.FalseString));
    internal static IReadOnlyDictionary<string, string> Defaults(MachineConfiguration configuration)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [C.TextRamOption] = C.MaximumTextRamBlocks.ToString(CultureInfo.InvariantCulture),
            [C.VideoRamOption] = C.MaximumVideoRamBlocks.ToString(CultureInfo.InvariantCulture),
            [C.DiscPackOption] = bool.FalseString,
            [Tape.ConnectedOption] = bool.TrueString,
            [StorageConstants.ControllerOption] = StorageConstants.Intel8271
        };
        for (var drive = 0; drive < StorageConstants.DriveCount; drive++)
        {
            values[StorageConstants.Key(drive, StorageConstants.ConnectedOption)] = bool.FalseString;
            values[StorageConstants.Key(drive, StorageConstants.ModelOption)] = StorageConstants.Single40;
        }
        return values;
    }

    private static int Blocks(MachineConfiguration configuration, string key, int minimum, int maximum)
    {
        var value = configuration.Options.GetValueOrDefault(key, maximum.ToString(CultureInfo.InvariantCulture));
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var blocks) || blocks < minimum || blocks > maximum)
            throw new ArgumentException(ErrorMessages.OptionValueInvalidFormat, nameof(configuration));
        return blocks;
    }

    internal static int TextRamBytes(MachineConfiguration configuration) => Blocks(configuration, C.TextRamOption, 0, C.MaximumTextRamBlocks) * C.RamBlockBytes;
    internal static int VideoRamBytes(MachineConfiguration configuration) => Blocks(configuration, C.VideoRamOption, C.MinimumVideoRamBlocks, C.MaximumVideoRamBlocks) * C.RamBlockBytes;
    internal static decimal MemoryKib(MachineConfiguration configuration) =>
        (C.CpuRamBytes + TextRamBytes(configuration) + VideoRamBytes(configuration) + (HasDiscPack(configuration) ? C.DiscPackRamBytes : 0)) / (decimal)C.RamBlockBytes;

    internal static IAcornMachine Create(MachineConfiguration configuration) => new AtomMachine(
        ReadImage(configuration, EmulatorConstants.SystemFirmware), ReadImage(configuration, EmulatorConstants.CharacterFirmware),
        TextRamBytes(configuration), VideoRamBytes(configuration), ReadImage(configuration, C.FloatingPointFirmware),
        ReadImage(configuration, C.UtilityFirmware), HasDiscPack(configuration) ? ReadImage(configuration, StorageConstants.DfsFirmware) : []);

    internal static IReadOnlyList<EmulationSettingsBlock> Describe(MachineConfiguration configuration)
    {
        var installed = HasDiscPack(configuration);
        return
        [
            new(C.Group, EmulationMachineTab.Cpu, SettingsConstants.ResourceCpu,
                [new(C.Group, EmulationMachineTab.Cpu, C.Group, SettingsConstants.ResourceCpu, EmulationSettingsEditor.Selection,
                    nameof(HardwareCpu.Mos6502), [new(nameof(HardwareCpu.Mos6502), SettingsConstants.CpuResourcePrefix + nameof(HardwareCpu.Mos6502))], IsEnabled: false)]),
            new(C.Group, EmulationMachineTab.Ram, C.MemoryLabel,
                [MemoryField(configuration, C.TextRamOption, C.TextRamLabel, 0, C.MaximumTextRamBlocks),
                 MemoryField(configuration, C.VideoRamOption, C.VideoRamLabel, C.MinimumVideoRamBlocks, C.MaximumVideoRamBlocks)]),
            new(C.RomGroup, EmulationMachineTab.Rom, EmulatorConstants.SystemFirmwareLabel,
                Slots(configuration).Where(slot => slot.FieldId != StorageConstants.DfsFirmware).Select(slot => RomField(configuration, slot, EmulationMachineTab.Rom, C.RomGroup)).ToArray()),
            new(C.Group, EmulationMachineTab.Extensions, C.DiscPackGroupLabel,
                [new(C.DiscPackOption, EmulationMachineTab.Extensions, C.Group, C.DiscPackLabel, EmulationSettingsEditor.Toggle,
                    installed.ToString(), EnabledValue: bool.TrueString, DisabledValue: bool.FalseString, RequiresRestart: true, RefreshSettingsOnChange: true),
                 RomField(configuration, Slots(configuration).Single(slot => slot.FieldId == StorageConstants.DfsFirmware), EmulationMachineTab.Extensions, C.Group) with { IsEnabled = installed }])
        ];
    }

    private static EmulationSettingsField MemoryField(MachineConfiguration configuration, string key, string label, int minimum, int maximum) =>
        new(key, EmulationMachineTab.Ram, C.Group, label, EmulationSettingsEditor.Selection,
            Blocks(configuration, key, minimum, maximum).ToString(CultureInfo.InvariantCulture),
            Enumerable.Range(minimum, maximum - minimum + 1).Select(blocks => new EmulationSettingsChoice(
                blocks.ToString(CultureInfo.InvariantCulture), string.Empty, blocks.ToString(CultureInfo.InvariantCulture) + C.MemorySuffix)).ToArray(),
            RequiresRestart: true, RefreshSettingsOnChange: true);

    private static EmulationSettingsField RomField(MachineConfiguration configuration, FirmwareSlot slot, EmulationMachineTab tab, string group) =>
        new(slot.FieldId, tab, group, slot.LabelResourceKey, EmulationSettingsEditor.Path, configuration.Options.GetValueOrDefault(slot.FieldId),
            DefaultFolderCategory: EmulationDefaultFolderCategory.Firmware, RequiresRestart: true);

    internal static IReadOnlyList<InputBindingDefinition> KeyboardDefinitions() => [new(nameof(EmulationKey.Break), C.BreakLabel, nameof(EmulationKey.Break))];
    internal static EmulationInputSnapshot MapInput(EmulationInputSnapshot snapshot, MachineConfiguration configuration)
    {
        var bindings = KeyboardDefinitions().ToDictionary(binding => binding.Id, binding => binding.DefaultBinding, StringComparer.Ordinal);
        foreach (var binding in configuration.Input.KeyboardBindings ?? new Dictionary<string, string>()) bindings[binding.Key] = binding.Value;
        return snapshot with { Keys = EmulationSdk.InputProcessing.MapKeyboard(snapshot, bindings), Controllers = [] };
    }

    internal static EmulationConfigurationSummary Summary(MachineConfiguration configuration) => new(
        HardwareModelCatalog.Get(configuration.Model).DisplayNameResourceKey,
        [string.Format(CultureInfo.InvariantCulture, C.CpuSummaryFormat, HardwareCpu.Mos6502),
         string.Format(CultureInfo.InvariantCulture, C.MemorySummaryFormat, MemoryKib(configuration)),
         string.Format(CultureInfo.InvariantCulture, C.CoreSummaryFormat, configuration.Core)]);

    internal static void Validate(MachineConfiguration configuration)
    {
        _ = TextRamBytes(configuration);
        _ = VideoRamBytes(configuration);
        var disc = HasDiscPack(configuration);
        if (NetworkFunctions.Read(configuration).Enabled || StorageFunctions.HasWinchester(configuration)
            || StorageFunctions.UsesWd1770(configuration) || FloppyExtensionFunctions.HasSolidisk(configuration)
            || !disc && Enumerable.Range(0, StorageConstants.DriveCount).Any(drive => StorageFunctions.Connected(configuration, drive)))
            throw new ArgumentException(ErrorMessages.IncompatibleMedia, nameof(configuration));
        StorageFunctions.Validate(configuration);
        if (EmulationTapeSettings.Read(configuration.Options).BaudRate is not (0 or C.TapeBaud))
            throw new ArgumentException(ErrorMessages.OptionValueInvalidFormat, nameof(configuration));
    }
}
