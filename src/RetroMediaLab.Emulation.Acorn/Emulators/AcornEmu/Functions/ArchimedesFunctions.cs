using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Arm26.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Memc.Enums;
using System.Globalization;
using System.Security.Cryptography;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Interfaces;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.Archimedes.Common.Constants;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.Archimedes.Common.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.Archimedes.Common.Services;
using C = RetroMediaLab.Emulation.Acorn.Common.Machines.Archimedes.Constants.ArchimedesModelConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Functions;

/// <summary>Options de la carte ARM/MEMC/VIDC/IOC ; les périphériques des cartes ultérieures restent distincts.</summary>
internal static partial class ArchimedesFunctions
{
    internal static bool IsModel(MachineConfiguration configuration) => IsModel(configuration.Model);
    internal static bool IsModel(MachineModel model) => model is MachineModel.AcornArchimedes or MachineModel.AcornA3000;
    internal static bool IsA3000(MachineConfiguration configuration) => configuration.Model == MachineModel.AcornA3000;
    internal static bool SupportsArthur(MachineConfiguration configuration) => !IsA3000(configuration);
    internal static int FloppyDriveCount(MachineConfiguration configuration) =>
        IsA3000(configuration) ? C.A3000FloppyDrives : ArchimedesBoardConstants.FloppyDrives;
    private static IEnumerable<int> MemorySizes(MachineConfiguration configuration) => C.MemorySizesKib
        .Where(kib => !IsA3000(configuration) || kib >= C.DefaultMemoryKib);

    internal static IReadOnlyDictionary<string, string> Defaults(MachineConfiguration configuration) =>
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [SettingsConstants.MainMemory] = C.DefaultMemoryKib.ToString(CultureInfo.InvariantCulture),
            [C.CpuOption] = nameof(Arm26Model.Arm2),
            [C.MemcOption] = (IsA3000(configuration) ? ArchimedesMemcRevision.Memc1A : ArchimedesMemcRevision.Memc1).ToString(),
            [C.St506Option] = bool.FalseString,
            [C.SerialOption] = (!IsA3000(configuration)).ToString(),
            [EmulatorConstants.VideoOutput] = EmulatorConstants.RgbOutput,
            [StorageConstants.Key(EmulationMediaSlot.Floppy0.Index, StorageConstants.ConnectedOption)] = bool.TrueString
        };

    internal static Arm26Model Processor(MachineConfiguration configuration) =>
        configuration.Options.GetValueOrDefault(C.CpuOption, nameof(Arm26Model.Arm2)) switch
        {
            nameof(Arm26Model.Arm2) => Arm26Model.Arm2,
            nameof(Arm26Model.Arm3) => Arm26Model.Arm3,
            _ => throw new ArgumentException(ErrorMessages.OptionValueInvalidFormat, nameof(configuration))
        };

    internal static ArchimedesMemcRevision MemoryController(MachineConfiguration configuration) =>
        configuration.Options.GetValueOrDefault(C.MemcOption,
            (IsA3000(configuration) ? ArchimedesMemcRevision.Memc1A : ArchimedesMemcRevision.Memc1).ToString()) switch
        {
            nameof(ArchimedesMemcRevision.Memc1) when !IsA3000(configuration) => ArchimedesMemcRevision.Memc1,
            nameof(ArchimedesMemcRevision.Memc1A) => ArchimedesMemcRevision.Memc1A,
            _ => throw new ArgumentException(ErrorMessages.OptionValueInvalidFormat, nameof(configuration))
        };

    internal static int MemoryBytes(MachineConfiguration configuration)
    {
        if (!int.TryParse(configuration.Options.GetValueOrDefault(SettingsConstants.MainMemory,
                C.DefaultMemoryKib.ToString(CultureInfo.InvariantCulture)), NumberStyles.None,
                CultureInfo.InvariantCulture, out var kib) || !MemorySizes(configuration).Contains(kib)
            || kib > C.MaximumSingleMemcMemoryKib && MemoryController(configuration) != ArchimedesMemcRevision.Memc1A)
            throw new ArgumentException(ErrorMessages.OptionValueInvalidFormat, nameof(configuration));
        return kib * C.Kibibyte;
    }

    internal static bool HasSt506(MachineConfiguration configuration) =>
        bool.TryParse(configuration.Options.GetValueOrDefault(C.St506Option, bool.FalseString), out var enabled)
            && (!enabled || !IsA3000(configuration)) ? enabled
            : throw new ArgumentException(ErrorMessages.OptionValueInvalidFormat, nameof(configuration));

    internal static bool HasSerialPort(MachineConfiguration configuration) =>
        bool.TryParse(configuration.Options.GetValueOrDefault(C.SerialOption, (!IsA3000(configuration)).ToString()), out var enabled)
            && (enabled || IsA3000(configuration)) ? enabled
            : throw new ArgumentException(ErrorMessages.OptionValueInvalidFormat, nameof(configuration));

    internal static IAcornMachine Create(MachineConfiguration configuration)
    {
        var rom = ReadSystemRom(configuration, SupportsArthur(configuration));
        AcornFirmware.BySha1.TryGetValue(Convert.ToHexString(SHA1.HashData(rom)), out var identity);
        var profile = identity?.Name.StartsWith(C.ArthurNamePrefix, StringComparison.Ordinal) == true
            ? ArchimedesCmosProfile.Arthur
            : identity?.Version?.StartsWith(C.RiscOs3VersionPrefix, StringComparison.Ordinal) == true
                || identity is null && rom.Length == C.LargeSystemRomBytes
                ? ArchimedesCmosProfile.RiscOs3 : ArchimedesCmosProfile.RiscOs2;
        var now = DateTime.Now;
        var cpu = Processor(configuration);
        var floppies = Enumerable.Range(0, FloppyDriveCount(configuration))
            .Where(drive => FloppyConnected(configuration, drive)).Select(drive => drive + 1).DefaultIfEmpty().Max();
        var hardDisks = Enumerable.Range(0, ArchimedesBoardConstants.HardDiskDrives)
            .Where(unit => HardDiskConnected(configuration, unit)).Select(unit => unit + 1).DefaultIfEmpty().Max();
        return new ArchimedesMachine(rom, MemoryBytes(configuration), cpu,
            cpu == Arm26Model.Arm2 ? C.Arm2ClockHz : C.Arm3ClockHz, initialTime: now,
            initialCmosRam: ArchimedesCmosConstants.Create(profile, now, floppies, hardDisks), st506: HasSt506(configuration),
            memoryRevision: MemoryController(configuration), floppyDrives: FloppyDriveCount(configuration),
            serialPort: HasSerialPort(configuration));
    }

    internal static IReadOnlyList<EmulationSettingsBlock> Describe(MachineConfiguration configuration) =>
    [
        new(C.Group, EmulationMachineTab.Cpu, SettingsConstants.ResourceCpu,
            [new(C.CpuOption, EmulationMachineTab.Cpu, C.Group, SettingsConstants.ResourceCpu,
                EmulationSettingsEditor.Selection, Processor(configuration).ToString(),
                [new(nameof(Arm26Model.Arm2), C.ResourcePrefix + nameof(Arm26Model.Arm2)),
                 new(nameof(Arm26Model.Arm3), C.ResourcePrefix + nameof(Arm26Model.Arm3))], RequiresRestart: true)]),
        new(C.Group, EmulationMachineTab.Ram, SettingsConstants.ResourceMemory,
            [new(SettingsConstants.MainMemory, EmulationMachineTab.Ram, C.Group, SettingsConstants.ResourceMemory,
                EmulationSettingsEditor.Selection, (MemoryBytes(configuration) / C.Kibibyte).ToString(CultureInfo.InvariantCulture),
                MemorySizes(configuration).Where(kib => kib <= C.MaximumSingleMemcMemoryKib
                    || MemoryController(configuration) == ArchimedesMemcRevision.Memc1A)
                    .Select(kib => new EmulationSettingsChoice(kib.ToString(CultureInfo.InvariantCulture), string.Empty,
                        IsA3000(configuration) && kib == C.ExpandedMemoryKib ? C.A3000ExpandedMemoryDisplay
                            : string.Format(CultureInfo.InvariantCulture, C.MemoryDisplayFormat, kib))).ToArray(), RequiresRestart: true),
             new(C.MemcOption, EmulationMachineTab.Ram, C.Group, C.MemcResource,
                EmulationSettingsEditor.Selection, MemoryController(configuration).ToString(),
                Enum.GetValues<ArchimedesMemcRevision>().Where(revision => !IsA3000(configuration)
                    || revision == ArchimedesMemcRevision.Memc1A).Select(revision => new EmulationSettingsChoice(
                    revision.ToString(), C.ResourcePrefix + revision)).ToArray(), RequiresRestart: true,
                IsEnabled: !IsA3000(configuration), RefreshSettingsOnChange: true)]),
        new(C.RomGroup, EmulationMachineTab.Rom, SettingsConstants.ResourceFirmware,
            Slots(configuration).Select(slot => new EmulationSettingsField(slot.FieldId, EmulationMachineTab.Rom,
                C.RomGroup, slot.LabelResourceKey, EmulationSettingsEditor.Path,
                configuration.Options.GetValueOrDefault(slot.FieldId), DefaultFolderCategory: EmulationDefaultFolderCategory.Firmware,
                RequiresRestart: true)).ToArray()),
        new(C.Group, EmulationMachineTab.Extensions, IsA3000(configuration) ? C.SerialResource : C.St506Resource,
            IsA3000(configuration)
            ? [new(C.SerialOption, EmulationMachineTab.Extensions, C.Group, C.SerialResource,
                EmulationSettingsEditor.Toggle, HasSerialPort(configuration).ToString(), EnabledValue: bool.TrueString,
                DisabledValue: bool.FalseString, RequiresRestart: true)]
            : [new(C.St506Option, EmulationMachineTab.Extensions, C.Group, C.St506Resource,
                EmulationSettingsEditor.Toggle, HasSt506(configuration).ToString(), EnabledValue: bool.TrueString,
                DisabledValue: bool.FalseString, RequiresRestart: true, RefreshSettingsOnChange: true)])
    ];

    internal static EmulationConfigurationSummary Summary(MachineConfiguration configuration) => new(
        HardwareModelCatalog.Get(configuration.Model).DisplayNameResourceKey,
        [string.Format(CultureInfo.InvariantCulture, C.CpuSummaryFormat, Processor(configuration)),
         string.Format(CultureInfo.InvariantCulture, C.MemorySummaryFormat, MemoryBytes(configuration) / C.Kibibyte),
         string.Format(CultureInfo.InvariantCulture, C.CoreSummaryFormat, configuration.Core)]);

    internal static IReadOnlyList<InputBindingDefinition> KeyboardDefinitions() =>
        [new(nameof(EmulationKey.Break), C.BreakResource, nameof(EmulationKey.Break))];

    internal static EmulationInputSnapshot MapInput(EmulationInputSnapshot snapshot, MachineConfiguration configuration)
    {
        var bindings = KeyboardDefinitions().ToDictionary(binding => binding.Id, binding => binding.DefaultBinding, StringComparer.Ordinal);
        foreach (var binding in configuration.Input.KeyboardBindings ?? new Dictionary<string, string>()) bindings[binding.Key] = binding.Value;
        return snapshot with { Keys = EmulationSdk.InputProcessing.MapKeyboard(snapshot, bindings), Controllers = [] };
    }

    internal static void Validate(MachineConfiguration configuration)
    {
        _ = Processor(configuration);
        _ = MemoryController(configuration);
        _ = MemoryBytes(configuration);
        _ = HasSt506(configuration);
        _ = HasSerialPort(configuration);
        if (NetworkFunctions.Read(configuration).Enabled) throw new ArgumentException(ErrorMessages.IncompatibleMedia, nameof(configuration));
        ValidateStorage(configuration);
    }
}
