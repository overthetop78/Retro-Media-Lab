using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMaster.Tube.Enums;
using System.Globalization;
using RetroMediaLab.Emulation.Acorn.Common.Constants;
using System.IO;
using RetroMediaLab.Emulation.Acorn.Common.Exceptions;
using RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMicro.Functions;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMaster.Common.Constants.MasterConstants;
using S = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMaster.Common.Constants.MasterSettingsConstants;
using Tube = RetroMediaLab.Chips.IO.Acorn.Tube.ULA.Constants.TubeConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Functions;

internal static class MasterFunctions
{
    internal static bool IsModel(MachineConfiguration configuration) => IsModel(configuration.Model);
    internal static bool IsModel(MachineModel model) => model is MachineModel.BbcMaster128 or MachineModel.BbcMasterET or MachineModel.BbcMasterCompact;
    internal static bool IsEt(MachineConfiguration configuration) => configuration.Model == MachineModel.BbcMasterET;
    internal static bool IsCompact(MachineConfiguration configuration) => configuration.Model == MachineModel.BbcMasterCompact;
    internal static string ChipField(int bank) => S.ChipPrefix + bank.ToString(CultureInfo.InvariantCulture);
    internal static MasterCoprocessor Coprocessor(MachineConfiguration configuration)
    {
        var value = configuration.Options.GetValueOrDefault(S.Coprocessor, nameof(MasterCoprocessor.None));
        if (!Enum.TryParse<MasterCoprocessor>(value, out var result) || !Enum.IsDefined(result)
            || configuration.Model != MachineModel.BbcMaster128 && result != MasterCoprocessor.None) throw new ArgumentException(nameof(configuration));
        return result;
    }
    internal static HardwareCpu? CoprocessorCpu(MachineConfiguration configuration) => Coprocessor(configuration) switch
    {
        MasterCoprocessor.Turbo => HardwareCpu.Gte65C102, MasterCoprocessor.Master512 => HardwareCpu.Intel80186, _ => null
    };
    internal static decimal MemoryKib(MachineConfiguration configuration) => S.MemoryKib + (Coprocessor(configuration) switch
    {
        MasterCoprocessor.Turbo => S.TurboMemoryKib, MasterCoprocessor.Master512 => S.Master512MemoryKib, _ => 0
    });
    private static byte[] CoprocessorRom(MachineConfiguration configuration)
    {
        if (Coprocessor(configuration) == MasterCoprocessor.None) return [];
        var path = configuration.Options.GetValueOrDefault(S.CoprocessorRom);
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) throw new EmulationException(ErrorCategory.Firmware, ErrorCode.FirmwareMissing,
            ExceptionText.Get(CoreErrorResourceKeys.NamedFirmwareMissing, ExceptionText.Get(S.CoprocessorRomLabel)), isLocalized: true);
        var bytes = File.ReadAllBytes(path);
        var expected = Coprocessor(configuration) == MasterCoprocessor.Turbo ? Tube.TurboRomBytes : Tube.Master512RomBytes;
        if (bytes.Length != expected)
            throw new EmulationException(ErrorCategory.Firmware, ErrorCode.FirmwareInvalid, ExceptionText.Get(CoreErrorResourceKeys.FirmwareInvalid), isLocalized: true);
        return bytes;
    }
    internal static bool HasFixedDrive(MachineConfiguration configuration) => IsCompact(configuration);
    internal static IReadOnlyDictionary<string, string> Defaults(MachineConfiguration configuration) => new Dictionary<string, string>
    {
        [SettingsConstants.MainMemory] = S.Memory128,
        [StorageConstants.ControllerOption] = StorageConstants.AcornWd1770,
        [StorageConstants.Key(0, StorageConstants.ConnectedOption)] = HasFixedDrive(configuration).ToString(),
        [NetworkConstants.Enabled] = IsEt(configuration).ToString()
    };
    private static bool HasSystemBundle(MachineConfiguration configuration)
    {
        var path = configuration.Options.GetValueOrDefault(EmulatorConstants.SystemFirmware);
        return !string.IsNullOrWhiteSpace(path) && File.Exists(path) && new FileInfo(path).Length == C.SystemBytes;
    }
    internal static byte[] SystemImage(MachineConfiguration configuration)
    {
        var path = configuration.Options.GetValueOrDefault(EmulatorConstants.SystemFirmware);
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            throw new EmulationException(ErrorCategory.Firmware, ErrorCode.FirmwareMissing, ExceptionText.Get(CoreErrorResourceKeys.NamedFirmwareMissing, FirmwareRomNames.Mos), isLocalized: true);
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length != C.BankBytes && bytes.Length != (configuration.Model == MachineModel.BbcMaster128 ? C.SystemBytes : C.CompactSystemBytes))
            throw new EmulationException(ErrorCategory.Firmware, ErrorCode.FirmwareInvalid, ExceptionText.Get(S.FirmwareSizeError), isLocalized: true);
        return bytes;
    }
    internal static BbcMicroMachine Create(MachineConfiguration configuration)
    {
        var system = SystemImage(configuration);
        var banks = new Dictionary<int, byte[]>();
        if (system.Length == C.SystemBytes)
            for (var bank = C.FirstSystemBank; bank <= 15; bank++) banks[bank] = system.AsSpan((bank - C.FirstSystemBank + 1) * C.BankBytes, C.BankBytes).ToArray();
        else if (system.Length == C.CompactSystemBytes)
            for (var offset = 1; offset < 4; offset++) banks[C.CompactBasicBank + offset - 2] = system.AsSpan(offset * C.BankBytes, C.BankBytes).ToArray();
        foreach (var bank in S.ExtensionBanks.Concat(S.InternalBanks))
        {
            var path = configuration.Options.GetValueOrDefault(ChipField(bank));
            if (!string.IsNullOrWhiteSpace(path)) banks[bank] = BbcMicroFirmwareFunctions.GetRomBytes(File.ReadAllBytes(path), FirmwareCategory.Language);
        }
        var basicBank = IsCompact(configuration) ? C.CompactBasicBank : C.BasicBank;
        if (!banks.TryGetValue(basicBank, out var basic)) basic = FirmwareFunctions.Read(configuration, EmulatorConstants.BasicFirmware);
        banks.Remove(basicBank);
        foreach (var bank in RequiredInternalBanks(configuration))
            if (!banks.ContainsKey(bank)) throw new EmulationException(ErrorCategory.Firmware, ErrorCode.FirmwareMissing,
                ExceptionText.Get(CoreErrorResourceKeys.NamedFirmwareMissing, ExceptionText.Get(BankLabel(configuration, bank))), isLocalized: true);
        var network = NetworkFunctions.Read(configuration);
        if (network.Enabled && !banks.ContainsKey(8)) banks[8] = FirmwareFunctions.Read(configuration, NetworkConstants.Firmware);
        return new BbcMicroMachine(system.AsSpan(0, C.BankBytes), basic, C.MainBytes,
            FirmwareFunctions.Read(configuration, EmulatorConstants.CharacterFirmware), !IsEt(configuration), banks,
            wd1770: true, winchesterInterface: !IsEt(configuration) && !IsCompact(configuration) && StorageFunctions.HasWinchester(configuration),
            econetInterface: network.Enabled, econetStation: network.Station, econetCyclesPerByte: network.CyclesPerByte,
            expandedIo: !IsEt(configuration), videoStandard: VideoStandardFunctions.Resolve(configuration), master: true, compact: IsCompact(configuration), coprocessor: Coprocessor(configuration), coprocessorRom: CoprocessorRom(configuration));
    }
    private static string BankLabel(MachineConfiguration configuration, int bank) => (IsCompact(configuration) && S.InternalBanks.Contains(bank) ? S.CompactBankPrefix : S.RomGroup) + bank.ToString(CultureInfo.InvariantCulture);
    private static IEnumerable<int> RequiredInternalBanks(MachineConfiguration configuration) => IsEt(configuration) ? [S.TerminalBank]
        : IsCompact(configuration) ? [S.AdfsBank, S.TerminalBank] : [];
    private static IEnumerable<int> VisibleInternalBanks(MachineConfiguration configuration) => HasSystemBundle(configuration) ? []
        : S.InternalBanks.Where(bank => bank != (IsCompact(configuration) ? C.CompactBasicBank : C.BasicBank));
    internal static IEnumerable<FirmwareSlot> Slots(MachineConfiguration configuration)
    {
        yield return new(EmulatorConstants.SystemFirmware, string.Empty, EmulatorConstants.SystemFirmwareLabel, IsRequired: true);
        if (!HasSystemBundle(configuration)) yield return new(EmulatorConstants.BasicFirmware, string.Empty, S.BasicLabel, IsRequired: true);
        if (Coprocessor(configuration) != MasterCoprocessor.None) yield return new(S.CoprocessorRom, string.Empty, S.CoprocessorRomLabel, IsRequired: true);
        yield return new(EmulatorConstants.CharacterFirmware, string.Empty, S.CharacterLabel, IsRequired: true);
        if (NetworkFunctions.Read(configuration).Enabled) yield return new(NetworkConstants.Firmware, string.Empty, S.NetworkRomLabel, IsRequired: string.IsNullOrWhiteSpace(configuration.Options.GetValueOrDefault(ChipField(S.NetworkBank))));
        foreach (var bank in S.ExtensionBanks.Concat(VisibleInternalBanks(configuration))) yield return new(ChipField(bank), string.Empty, BankLabel(configuration, bank), IsRequired: RequiredInternalBanks(configuration).Contains(bank), DisplayName: ExceptionText.Get(BankLabel(configuration, bank)));
    }
    private static EmulationSettingsBlock Video(MachineConfiguration configuration) => new(VideoStandardConstants.Option, EmulationMachineTab.Video, VideoStandardConstants.Label,
        [new(VideoStandardConstants.Option, EmulationMachineTab.Video, VideoStandardConstants.Option, VideoStandardConstants.Label, EmulationSettingsEditor.Selection,
            configuration.Options.GetValueOrDefault(VideoStandardConstants.Option, VideoStandardConstants.Automatic),
            [new(VideoStandardConstants.Automatic, VideoStandardConstants.AutomaticLabel), new(VideoStandardConstants.Pal, VideoStandardConstants.PalLabel), new(VideoStandardConstants.Ntsc, VideoStandardConstants.NtscLabel)], RequiresRestart: true)]);
    internal static IReadOnlyList<EmulationSettingsBlock> Describe(MachineConfiguration configuration)
    {
        var main = Slots(configuration).Where(slot => !S.ExtensionBanks.Any(bank => slot.FieldId == ChipField(bank))).Select(slot =>
            new EmulationSettingsField(slot.FieldId, EmulationMachineTab.Rom, S.FirmwareBlock, slot.LabelResourceKey, EmulationSettingsEditor.Path,
                configuration.Options.GetValueOrDefault(slot.FieldId), DefaultFolderCategory: EmulationDefaultFolderCategory.Firmware, RequiresRestart: true, RefreshSettingsOnChange: slot.FieldId == EmulatorConstants.SystemFirmware)).ToArray();
        var chips = S.ExtensionBanks.Select(bank => new EmulationSettingsField(ChipField(bank), EmulationMachineTab.Rom, S.RomGroup,
            BankLabel(configuration, bank), EmulationSettingsEditor.Path, configuration.Options.GetValueOrDefault(ChipField(bank)), DefaultFolderCategory: EmulationDefaultFolderCategory.Firmware, RequiresRestart: true)).ToArray();
        var coprocessor = new EmulationSettingsBlock(S.CoprocessorGroup, EmulationMachineTab.Extensions, S.CoprocessorGroup,
            [new(S.Coprocessor, EmulationMachineTab.Extensions, S.CoprocessorGroup, S.CoprocessorGroup, EmulationSettingsEditor.Selection,
                Coprocessor(configuration).ToString(),
                [new(nameof(MasterCoprocessor.None), MemoryExtensionConstants.NoneLabel),
                 new(nameof(MasterCoprocessor.Turbo), S.TurboLabel), new(nameof(MasterCoprocessor.Master512), S.Master512Label)],
                RequiresRestart: true, RefreshSettingsOnChange: true)], IsVisible: configuration.Model == MachineModel.BbcMaster128);
        return [coprocessor, new(S.FirmwareBlock, EmulationMachineTab.Rom, S.FirmwareLabel, main),
            new(S.RomGroup, EmulationMachineTab.Rom, S.RomGroup, chips),
            new(S.MemoryGroup, EmulationMachineTab.Ram, S.MemoryGroup,
                [new(S.MemoryDescription, EmulationMachineTab.Ram, S.MemoryGroup, S.MemoryDescription, EmulationSettingsEditor.Information, S.Memory128Display)]),
            NetworkFunctions.Describe(configuration), Video(configuration)];
    }
}
