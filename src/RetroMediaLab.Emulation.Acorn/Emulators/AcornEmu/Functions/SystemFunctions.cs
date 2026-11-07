using System.Globalization;
using RetroMediaLab.Emulation.Acorn.Common.Machines.AcornSystem.Constants;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Interfaces;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Services;
using C = RetroMediaLab.Emulation.Acorn.Common.Machines.AcornSystem.Constants.SystemModelConstants;
using Bus = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Constants.SystemConstants;
using Tape = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants.TapeConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Functions;

internal static partial class SystemFunctions
{
    internal static bool IsModel(MachineConfiguration configuration) => SystemModelCatalog.IsSystem(configuration.Model);
    internal static HardwareCpu Cpu(MachineConfiguration configuration)
    {
        var model = HardwareModelCatalog.Get(configuration.Model);
        var requested = configuration.Options.GetValueOrDefault(C.CpuOption, model.DefaultCpu.ToString());
        return Enum.TryParse<HardwareCpu>(requested, out var cpu) && model.Cpus.Contains(cpu)
            ? cpu : throw new ArgumentException(ErrorMessages.OptionValueInvalidFormat, nameof(configuration));
    }

    internal static bool HasDisk(MachineConfiguration configuration) => configuration.Model is MachineModel.AcornSystem3 or MachineModel.AcornSystem4 or MachineModel.AcornSystem5;
    internal static bool HasTape(MachineConfiguration configuration) => configuration.Model is MachineModel.AcornSystem1 or MachineModel.AcornSystem2
        || configuration.Model == MachineModel.AcornSystem3 && Cpu(configuration) == HardwareCpu.Motorola6809;

    internal static IReadOnlyDictionary<string, string> Defaults(MachineConfiguration configuration)
    {
        var options = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [C.CpuOption] = HardwareModelCatalog.Get(configuration.Model).DefaultCpu.ToString(),
            [Tape.ConnectedOption] = HasTape(configuration).ToString(),
            [StorageConstants.ControllerOption] = StorageConstants.Intel8271
        };
        for (var drive = 0; drive < StorageConstants.DriveCount; drive++)
        {
            options[StorageConstants.Key(drive, StorageConstants.ConnectedOption)] =
                (HasDisk(configuration) && (drive == 0 || configuration.Model is MachineModel.AcornSystem4 or MachineModel.AcornSystem5)).ToString();
            options[StorageConstants.Key(drive, StorageConstants.ModelOption)] = StorageConstants.Single40;
        }
        return options;
    }

    internal static IReadOnlyList<(int Start, int Bytes)> RamCards(MachineConfiguration configuration) => configuration.Model switch
    {
        MachineModel.AcornSystem1 => [],
        MachineModel.AcornSystem2 => [(Bus.RamCardStart, Bus.RamCardBytes)],
        MachineModel.AcornSystem3 when Cpu(configuration) == HardwareCpu.Motorola6809 =>
            [(0, Bus.RamCardBytes), (Bus.RamCardStart, Bus.RamCardBytes), (Bus.UpperRamCardStart, Bus.RamCardBytes)],
        MachineModel.AcornSystem3 or MachineModel.AcornSystem4 =>
            [(Bus.RamCardStart, Bus.RamCardBytes), (Bus.UpperRamCardStart, Bus.RamCardBytes)],
        MachineModel.AcornSystem5 => [(0, Bus.FastRamBytes)],
        _ => throw new ArgumentOutOfRangeException(nameof(configuration))
    };

    internal static decimal MemoryKib(MachineConfiguration configuration)
    {
        var cards = RamCards(configuration);
        var onCpu = cards.Any(card => card.Start == 0) ? 0 : Bus.CpuCardRamBytes;
        return (cards.Sum(card => card.Bytes) + onCpu) / (decimal)SettingsConstants.Kibibyte;
    }

    internal static IAcornMachine Create(MachineConfiguration configuration)
    {
        var card = configuration.Model == MachineModel.AcornSystem1 ? SystemCpuCard.Keypad6502
            : configuration.Model == MachineModel.AcornSystem5 ? SystemCpuCard.Fast6502A
            : Cpu(configuration) == HardwareCpu.Motorola6809 ? SystemCpuCard.Motorola6809 : SystemCpuCard.Standard6502;
        var monitor = ReadImage(configuration, EmulatorConstants.SystemFirmware, true);
        var characters = configuration.Model == MachineModel.AcornSystem1 ? [] : ReadImage(configuration, EmulatorConstants.CharacterFirmware, true);
        var basic = configuration.Model == MachineModel.AcornSystem2 ? ReadImage(configuration, EmulatorConstants.BasicFirmware, false) : [];
        return new AcornSystemMachine(card, monitor, characters, RamCards(configuration), HasDisk(configuration), basic);
    }

    internal static IReadOnlyList<EmulationSettingsBlock> Describe(MachineConfiguration configuration)
    {
        var hardware = HardwareModelCatalog.Get(configuration.Model);
        return
        [
            new(C.CpuGroup, EmulationMachineTab.Cpu, C.CpuLabel,
                [new(C.CpuOption, EmulationMachineTab.Cpu, C.CpuGroup, C.CpuLabel, EmulationSettingsEditor.Selection,
                    Cpu(configuration).ToString(), hardware.Cpus.Select(cpu => new EmulationSettingsChoice(cpu.ToString(), C.CpuResourcePrefix + cpu)).ToArray(),
                    IsEnabled: hardware.Cpus.Count > 1, RequiresRestart: true, RefreshSettingsOnChange: true)]),
            new(C.RamGroup, EmulationMachineTab.Ram, C.MemoryLabel,
                [new(SettingsConstants.MainMemory, EmulationMachineTab.Ram, C.RamGroup, C.MemoryLabel, EmulationSettingsEditor.Selection,
                    MemoryKib(configuration).ToString(CultureInfo.InvariantCulture),
                    [new(MemoryKib(configuration).ToString(CultureInfo.InvariantCulture), string.Empty,
                        MemoryKib(configuration).ToString(CultureInfo.InvariantCulture) + C.MemorySuffix)], IsEnabled: false)]),
            new(C.RomGroup, EmulationMachineTab.Rom, EmulatorConstants.SystemFirmwareLabel,
                Slots(configuration).Select(slot => new EmulationSettingsField(slot.FieldId, EmulationMachineTab.Rom, C.RomGroup,
                    slot.LabelResourceKey, EmulationSettingsEditor.Path, configuration.Options.GetValueOrDefault(slot.FieldId),
                    DefaultFolderCategory: EmulationDefaultFolderCategory.Firmware, RequiresRestart: true)).ToArray())
        ];
    }

    internal static IReadOnlyList<InputBindingDefinition> KeyboardDefinitions() =>
        [new(nameof(EmulationKey.Break), C.BreakLabel, nameof(EmulationKey.Break))];

    internal static EmulationConfigurationSummary Summary(MachineConfiguration configuration) => new(
        HardwareModelCatalog.Get(configuration.Model).DisplayNameResourceKey,
        [string.Format(CultureInfo.InvariantCulture, C.CpuSummaryFormat, Cpu(configuration)),
         string.Format(CultureInfo.InvariantCulture, C.MemorySummaryFormat, MemoryKib(configuration)),
         string.Format(CultureInfo.InvariantCulture, C.CoreSummaryFormat, configuration.Core)]);

    internal static EmulationInputSnapshot MapInput(EmulationInputSnapshot snapshot, MachineConfiguration configuration)
    {
        var bindings = new Dictionary<string, string>(StringComparer.Ordinal) { [nameof(EmulationKey.Break)] = nameof(EmulationKey.Break) };
        foreach (var binding in configuration.Input.KeyboardBindings ?? new Dictionary<string, string>()) bindings[binding.Key] = binding.Value;
        return snapshot with { Keys = EmulationSdk.InputProcessing.MapKeyboard(snapshot, bindings), Controllers = [] };
    }

    internal static void Validate(MachineConfiguration configuration)
    {
        _ = Cpu(configuration);
        if (NetworkFunctions.Read(configuration).Enabled || StorageFunctions.HasWinchester(configuration))
            throw new ArgumentException(ErrorMessages.OptionValueInvalidFormat, nameof(configuration));
        if (!HasDisk(configuration) && Enumerable.Range(0, StorageConstants.DriveCount).Any(drive => StorageFunctions.Connected(configuration, drive)))
            throw new ArgumentException(ErrorMessages.IncompatibleMedia, nameof(configuration));
        if (StorageFunctions.UsesWd1770(configuration)) throw new ArgumentException(ErrorMessages.IncompatibleMedia, nameof(configuration));
        StorageFunctions.Validate(configuration);
        if (EmulationTapeSettings.Read(configuration.Options).BaudRate is not (0 or C.TapeBaud)) throw new ArgumentException(ErrorMessages.OptionValueInvalidFormat, nameof(configuration));
    }
}
