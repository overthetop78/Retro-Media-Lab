using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMaster.Common.Constants;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMaster.Tube.Enums;
using Plus = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.ModelBPlus.Constants.ModelBPlusConstants;
using System.IO;
using CommonCoreHostFunctions = RetroMediaLab.Emulation.Acorn.Common.Functions.CoreHostFunctions;
using RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMicro.Functions;
using RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMicro.Constants;
using RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMicro.Dictionaries;
using NativeModelCatalog = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Dictionaries.ModelCatalog;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Functions;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;
using CassetteConstants = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants.CassetteConstants;
using NativeStorage = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Functions.StorageFunctions;
using NativeStorageConstants = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.StorageConstants;
using SystemSettings = RetroMediaLab.Emulation.Acorn.Common.Machines.AcornSystem.Constants.SystemModelConstants;
using AtomSettings = RetroMediaLab.Emulation.Acorn.Common.Machines.AcornAtom.Constants.AtomModelConstants;
using ArchSettings = RetroMediaLab.Emulation.Acorn.Common.Machines.Archimedes.Constants.ArchimedesModelConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Factories;

/// <summary>Adaptateur du cœur interne, découvert par le catalogue Common sans chemin spécial dans l'application.</summary>
/// <remarks>Le cœur est livré dans l'assembly du module. La sélection et l'inspection des ROM réutilisent les facettes existantes.</remarks>
internal sealed class AcornEmuMachineFactory : IEmulatorAdapter
{
    public string EmulatorId => EmulatorConstants.Id;
    public string EmulatorKey => Emulator.AcornEmu.ToString();
    public EmulationEmulatorDefinition Definition { get; } = new(EmulatorConstants.Id, EmulatorConstants.Name, "Emulation.Emulator.acornemu.Description", NativeModelCatalog.All.Select(model => model.Model.ToString()).ToHashSet(StringComparer.Ordinal));
    public bool SupportsNoContent => true;
    public IReadOnlyDictionary<string, string> GetDefaultOptions(MachineConfiguration configuration) =>
        ArchimedesFunctions.IsModel(configuration) ? ArchimedesFunctions.Defaults(configuration) : ElectronFunctions.IsModel(configuration) ? ElectronFunctions.Defaults(configuration) : AtomFunctions.IsModel(configuration) ? AtomFunctions.Defaults(configuration) : SystemFunctions.IsModel(configuration) ? SystemFunctions.Defaults(configuration) : MasterFunctions.IsModel(configuration) ? MasterFunctions.Defaults(configuration) : ModelBPlusFunctions.IsModel(configuration)
            ? new Dictionary<string, string> { [SettingsConstants.MainMemory] = configuration.Model == MachineModel.BbcMicroModelBPlus128 ? Plus.Memory128 : Plus.Memory64, [NativeStorageConstants.ControllerOption] = NativeStorageConstants.AcornWd1770, [NativeStorageConstants.Key(EmulationMediaSlot.Floppy0.Index, NativeStorageConstants.ConnectedOption)] = bool.TrueString }
            : configuration.Model == MachineModel.BbcMicroModelB
            ? new Dictionary<string, string> { [SettingsConstants.MainMemory] = BbcMicroModelConstants.ThirtyTwoKibibytesValue }
            : new Dictionary<string, string>();
    private static string AssemblyPath => typeof(AcornEmuMachineFactory).Assembly.Location;
    public bool TryHandleHostCommand(IReadOnlyList<string> arguments, out int exitCode) { exitCode = 0; return false; }
    public ValueTask<EmulationEmulatorInstallation> GetInstallationAsync(EmulatorManagementContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new EmulationEmulatorInstallation(Definition, EmulatorConstants.Version));
    }
    public ValueTask<IReadOnlyList<EmulationEmulatorRelease>> FindReleasesAsync(EmulatorManagementContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<IReadOnlyList<EmulationEmulatorRelease>>([new(EmulatorConstants.Id, EmulatorConstants.Name, EmulatorConstants.Version, IsRequired: true)]);
    }
    public ValueTask<string> InstallAsync(EmulatorManagementContext context, EmulationEmulatorRelease release, IProgress<double>? progress, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (release.Id != EmulatorConstants.Id || release.Version != EmulatorConstants.Version) throw new ArgumentException(nameof(release));
        progress?.Report(1);
        return ValueTask.FromResult(AssemblyPath);
    }
    public ValueTask<string?> FindInstalledCorePathAsync(EmulatorManagementContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<string?>(AssemblyPath);
    }
    public IReadOnlyList<EmulationMedia> ResolveConfiguredMedia(MachineConfiguration configuration) => configuration.Media.Select(EmulationMediaConversionFunctions.ToCommon).ToArray();
    /// <summary>Annonce les seuls lecteurs explicitement raccordés, cassette et carte disque optionnelles comprises.</summary>
    public IReadOnlyList<EmulationMediaDevice> DescribeMediaDevices(MachineConfiguration configuration) => ArchimedesFunctions.IsModel(configuration) ? ArchimedesFunctions.Devices(configuration) : ElectronFunctions.IsModel(configuration) ? ElectronFunctions.Devices(configuration) : AtomFunctions.IsModel(configuration) ? AtomFunctions.Devices(configuration) : SystemFunctions.IsModel(configuration) ? SystemFunctions.Devices(configuration) : (CassetteConnected(configuration) ? CassetteDevices(configuration) : []).Concat(NativeStorage.Devices(configuration).Where(device => NativeStorage.Connected(configuration, device.Slot.Index))).Concat(NativeStorage.HardDiskDevices(configuration).Where(device => NativeStorage.HardDiskConnected(configuration, device.Slot.Index))).ToArray();
    private static IReadOnlyList<EmulationMediaDevice> CassetteDevices(MachineConfiguration configuration) => MasterFunctions.IsEt(configuration) || MasterFunctions.IsCompact(configuration) ? [] : [new(EmulationMediaSlot.Cassette0, EmulationMediaType.Cassette, CassetteConstants.Extensions, ConfigurationKind: EmulationStorageConfigurationKind.CassetteDrive, ConfigurationBlocks: EmulationCassetteSettings.DescribeBlocks(configuration.Options, [300, 1200]))];
    /// <summary>Un média déjà choisi exprime un branchement explicite ; une configuration vide n'ajoute aucun lecteur.</summary>
    private static bool CassetteConnected(MachineConfiguration configuration) => !MasterFunctions.IsEt(configuration) && !MasterFunctions.IsCompact(configuration) && ( configuration.Options.TryGetValue(CassetteConstants.ConnectedOption, out var value) ? bool.Parse(value) : configuration.Media.Any(media => media.Slot == EmulationMediaSlot.Cassette0));
    public IReadOnlyDictionary<string, string> GetRuntimeOptions(MachineConfiguration configuration) => configuration.Options;
    public bool SupportsPointerCapture(MachineConfiguration configuration) => ArchimedesFunctions.IsModel(configuration);
    public IReadOnlySet<string>? GetCartridgeExtensions(MachineConfiguration configuration) => null;
    public EmulationConfigurationSummary Summarize(MachineConfiguration configuration) => ArchimedesFunctions.IsModel(configuration) ? ArchimedesFunctions.Summary(configuration) : ElectronFunctions.IsModel(configuration) ? ElectronFunctions.Summary(configuration) : AtomFunctions.IsModel(configuration) ? AtomFunctions.Summary(configuration) : SystemFunctions.IsModel(configuration) ? SystemFunctions.Summary(configuration) : ConfigurationSummaryFunctions.Create(configuration);
    /// <summary>Propose le lecteur à Ajouter, indépendamment des seuls périphériques effectivement raccordés au runtime.</summary>
    public EmulationStorageSettings DescribeStorage(MachineConfiguration configuration) => ArchimedesFunctions.IsModel(configuration) ? ArchimedesFunctions.DescribeStorage(configuration) : ElectronFunctions.IsModel(configuration) ? ElectronFunctions.DescribeStorage(configuration) : AtomFunctions.IsModel(configuration) ? AtomFunctions.DescribeStorage(configuration) : SystemFunctions.IsModel(configuration) ? SystemFunctions.DescribeStorage(configuration) : StorageSettingsFunctions.Describe(configuration) with { AvailableDevices = MasterFunctions.IsEt(configuration) ? [] : CassetteDevices(configuration).Concat(NativeStorage.Devices(configuration)).Concat(NativeStorage.HardDiskDevices(configuration)).ToArray() };
    public IReadOnlyList<EmulationStorageDeviceSettings> DescribeDeviceSettings(MachineConfiguration configuration) => ArchimedesFunctions.IsModel(configuration) ? ArchimedesFunctions.DeviceSettings(configuration) : ElectronFunctions.IsModel(configuration) ? ElectronFunctions.DeviceSettings(configuration) : AtomFunctions.IsModel(configuration) ? AtomFunctions.DeviceSettings(configuration) : SystemFunctions.IsModel(configuration) ? SystemFunctions.DeviceSettings(configuration) : NativeStorage.DescribeSettings(configuration).Concat(NativeStorage.DescribeHardDiskSettings(configuration)).Concat(CassetteConnected(configuration) ? [new EmulationStorageDeviceSettings(EmulationMediaSlot.Cassette0, Options: configuration.Options.Where(pair => pair.Key.StartsWith(EmulationCassetteSettings.Prefix, StringComparison.Ordinal)).ToDictionary())] : []).ToArray();
    public MachineConfiguration ApplyDeviceSettings(MachineConfiguration configuration, IReadOnlyList<EmulationStorageDeviceSettings> settings) => ArchimedesFunctions.IsModel(configuration) ? ArchimedesFunctions.ApplyDeviceSettings(configuration, settings) : ElectronFunctions.IsModel(configuration) ? ElectronFunctions.ApplyDeviceSettings(configuration, settings) : AtomFunctions.IsModel(configuration) ? AtomFunctions.ApplyDeviceSettings(configuration, settings) : SystemFunctions.IsModel(configuration) ? SystemFunctions.ApplyDeviceSettings(configuration, settings) : NativeStorage.ApplySettings(configuration, settings);
    public IReadOnlyDictionary<string, string> NormalizeOptions(MachineConfiguration configuration) => (ArchimedesFunctions.IsModel(configuration) || ElectronFunctions.IsModel(configuration) || AtomFunctions.IsModel(configuration) || SystemFunctions.IsModel(configuration)) ? configuration.Options : VideoStandardFunctions.Normalize(configuration);
    public bool RequiresOptionRestart(MachineConfiguration configuration, string key) => !NativeStorage.IsMechanicalSoundOption(key) && (key.StartsWith(ArchSettings.OptionPrefix, StringComparison.Ordinal) || key.StartsWith(NativeStorageConstants.FloppyPrefix, StringComparison.Ordinal) || key.StartsWith(NativeStorageConstants.HardDiskPrefix, StringComparison.Ordinal) || key.StartsWith(NetworkConstants.Prefix, StringComparison.Ordinal) || key == NetworkConstants.Firmware || key == SidewaysExpansionConstants.Option || key.StartsWith(SidewaysExpansionConstants.ChipPrefix, StringComparison.Ordinal) || key.StartsWith(SidewaysExpansionConstants.AriesChipPrefix, StringComparison.Ordinal) || key is MemoryExtensionConstants.CardOption or MemoryExtensionConstants.Firmware || key == NativeStorageConstants.DfsFirmware || key == VideoStandardConstants.Option || key == SettingsConstants.MainMemory || key == SystemSettings.CpuOption || key.StartsWith(AtomSettings.OptionPrefix, StringComparison.Ordinal) || key.StartsWith(Plus.ChipPrefix, StringComparison.Ordinal));
    /// <summary>Conserve le branchement même sans cassette et retire le lecteur lorsque l'utilisateur le supprime.</summary>
    public MachineConfiguration ApplyStorage(MachineConfiguration configuration, EmulationStorageSettings settings)
    {
        if (ArchimedesFunctions.IsModel(configuration)) return ArchimedesFunctions.ApplyStorage(configuration, settings);
        if (ElectronFunctions.IsModel(configuration)) return ElectronFunctions.ApplyStorage(configuration, settings);
        if (AtomFunctions.IsModel(configuration)) return AtomFunctions.ApplyStorage(configuration, settings);
        if (SystemFunctions.IsModel(configuration)) return SystemFunctions.ApplyStorage(configuration, settings);
        if ((ModelBPlusFunctions.IsModel(configuration) || MasterFunctions.HasFixedDrive(configuration)) && !settings.ConfiguredSlots.Contains(EmulationMediaSlot.Floppy0))
            settings = settings with { ConfiguredSlots = settings.ConfiguredSlots.Append(EmulationMediaSlot.Floppy0).ToArray() };
        var options = new Dictionary<string, string>(configuration.Options, StringComparer.Ordinal) { [CassetteConstants.ConnectedOption] = settings.ConfiguredSlots.Contains(EmulationMediaSlot.Cassette0).ToString() };
        foreach (var pair in settings.DeviceSettings?.FirstOrDefault(device => device.Slot == EmulationMediaSlot.Cassette0)?.Options ?? new Dictionary<string, string>())
            if (pair.Key.StartsWith(EmulationCassetteSettings.Prefix, StringComparison.Ordinal)) options[pair.Key] = pair.Value;
        return StorageSettingsFunctions.Apply(NativeStorage.SelectSlots(configuration with { Options = options }, settings.ConfiguredSlots), NativeStorage.MigrateMediaProtection(configuration, settings));
    }
    public IReadOnlyDictionary<EmulationMediaSlot, bool> MediaActivity(MachineConfiguration configuration, IReadOnlyDictionary<int, bool> ledStates)
    {
        var activity = CommonCoreHostFunctions.MachineExecution.FromLedStates(ledStates).ToDictionary();
        foreach (var device in DescribeMediaDevices(configuration))
        {
            var slot = device.Slot;
            if (slot.Category == EmulationMediaCategory.FloppyDrive)
                activity[slot] = ledStates.GetValueOrDefault(NativeStorageConstants.FloppyLed(slot.Index));
            else if (slot.Category == EmulationMediaCategory.HardDisk)
                activity[slot] = ledStates.GetValueOrDefault(NativeStorageConstants.HardDiskLedStart + slot.Index);
        }
        return activity;
    }
    public ValueTask<EmulationMedia> PrepareMediaAsync(EmulationMedia media, string conversionDirectory) => ValueTask.FromResult(media);
    public Task<MachineConfiguration> PrepareRuntimeConfigurationAsync(MachineConfiguration configuration, string conversionDirectory) => Task.FromResult(configuration);
    public IReadOnlyList<InputBindingDefinition> GetKeyboardDefinitions(MachineConfiguration configuration) => ArchimedesFunctions.IsModel(configuration) ? ArchimedesFunctions.KeyboardDefinitions() : ElectronFunctions.IsModel(configuration) ? ElectronFunctions.KeyboardDefinitions() : AtomFunctions.IsModel(configuration) ? AtomFunctions.KeyboardDefinitions() : SystemFunctions.IsModel(configuration) ? SystemFunctions.KeyboardDefinitions() : InputSettingsFunctions.KeyboardDefinitions(configuration);
    EmulationInputSnapshot IEmulatorInputMapper.MapInput(EmulationInputSnapshot snapshot, MachineConfiguration configuration) =>
        ArchimedesFunctions.IsModel(configuration) ? ArchimedesFunctions.MapInput(snapshot, configuration) : ElectronFunctions.IsModel(configuration) ? ElectronFunctions.MapInput(snapshot, configuration) : AtomFunctions.IsModel(configuration) ? AtomFunctions.MapInput(snapshot, configuration) : SystemFunctions.IsModel(configuration) ? SystemFunctions.MapInput(snapshot, configuration) : InputSnapshotFunctions.Apply(snapshot, configuration.Input);
    public MachineConfiguration ApplyInputConfiguration(MachineConfiguration configuration) => configuration;

    /// <summary>Annonce et valide la RAM réellement allouée par l'implémentation du modèle.</summary>
    public decimal? GetMemoryKib(MachineConfiguration configuration) => ArchimedesFunctions.IsModel(configuration) ? ArchimedesFunctions.MemoryBytes(configuration) / (decimal)SettingsConstants.Kibibyte : ElectronFunctions.IsModel(configuration) ? ElectronFunctions.MemoryKib(configuration) : AtomFunctions.IsModel(configuration) ? AtomFunctions.MemoryKib(configuration) : SystemFunctions.IsModel(configuration) ? SystemFunctions.MemoryKib(configuration) : MasterFunctions.IsModel(configuration) ? MasterFunctions.MemoryKib(configuration) : (ModelBPlusFunctions.IsModel(configuration) ? ModelBPlusFunctions.MemoryBytes(configuration) : MemoryExtensionFunctions.TotalBytes(configuration)) / (decimal)SettingsConstants.Kibibyte;
    public void ValidateOptions(MachineConfiguration configuration)
    {
        if (ArchimedesFunctions.IsModel(configuration)) { ArchimedesFunctions.Validate(configuration); return; }
        if (ElectronFunctions.IsModel(configuration)) { ElectronFunctions.Validate(configuration); return; }
        if (AtomFunctions.IsModel(configuration)) { AtomFunctions.Validate(configuration); return; }
        if (SystemFunctions.IsModel(configuration)) { SystemFunctions.Validate(configuration); return; }
        _ = VideoStandardFunctions.Resolve(configuration.Options, null);
        if (MasterFunctions.IsModel(configuration))
        {
            _ = MasterFunctions.Coprocessor(configuration);
            if (MemoryExtensionFunctions.Read(configuration).Enabled || SidewaysExpansionFunctions.Read(configuration) != new Contracts.SidewaysExpansionSettings() || FloppyExtensionFunctions.HasSolidisk(configuration)) throw new ArgumentException(ErrorMessages.OptionValueInvalidFormat, nameof(configuration));
        }
        if (ModelBPlusFunctions.IsModel(configuration))
        {
            _ = ModelBPlusFunctions.MemoryBytes(configuration);
            if (MemoryExtensionFunctions.Read(configuration).Enabled || SidewaysExpansionFunctions.Read(configuration) != new Contracts.SidewaysExpansionSettings()) throw new ArgumentException(ErrorMessages.OptionValueInvalidFormat, nameof(configuration));
            if (configuration.Options.GetValueOrDefault(NativeStorageConstants.ControllerOption, NativeStorageConstants.AcornWd1770) != NativeStorageConstants.AcornWd1770 || FloppyExtensionFunctions.HasSolidisk(configuration)) throw new ArgumentException(ErrorMessages.OptionValueInvalidFormat, nameof(configuration));
        }
        NativeStorage.Validate(configuration);
        _ = NetworkFunctions.Read(configuration);
        _ = MemoryExtensionFunctions.Read(configuration);
        _ = SidewaysExpansionFunctions.Read(configuration);
        if (EmulationCassetteSettings.Read(configuration.Options).BaudRate is not (0 or 300 or 1200)) throw new ArgumentOutOfRangeException(nameof(configuration));
        _ = BbcMicroMemoryFunctions.MainBytes(configuration);
        if (configuration.Options.TryGetValue(CassetteConstants.ConnectedOption, out var connected) && !bool.TryParse(connected, out _)) throw new ArgumentException(ErrorMessages.OptionValueInvalidFormat, nameof(configuration));
        if (!EmulatorConstants.VideoOutputs.Contains(configuration.Options.GetValueOrDefault(EmulatorConstants.VideoOutput, EmulatorConstants.UhfOutput), StringComparer.Ordinal)) throw new ArgumentException(ErrorMessages.OptionValueInvalidFormat, nameof(configuration));
    }

    /// <summary>Construit la session commune et lui transfère le cœur seulement après création réussie.</summary>
    public Machine Create(MachineConfiguration configuration, EmulatorCreationContext context)
    {
        if (configuration.Core != Emulator.AcornEmu) throw new ArgumentException(nameof(configuration));
        var id = Guid.NewGuid();
        var core = new EmulatorCore(configuration.Model);
        var transferred = false;
        try
        {
            var machine = new Machine(id, configuration, core, Path.Combine(context.SessionsDirectory, id.ToString(ConfigurationStoreConstants.MachineIdentifierFormat)), audioOutputFactory: context.AudioOutputFactory, saveDirectory: context.SaveDirectoryResolver?.Invoke(configuration), inputMapper: this, runtimePolicy: this);
            transferred = true;
            return machine;
        }
        finally { if (!transferred) core.Dispose(); }
    }

    public IReadOnlyList<FirmwareSlot> GetFirmwareSlots(MachineConfiguration configuration) => ArchimedesFunctions.IsModel(configuration) ? ArchimedesFunctions.Slots(configuration) : ElectronFunctions.IsModel(configuration) ? ElectronFunctions.Slots(configuration) : AtomFunctions.IsModel(configuration) ? AtomFunctions.Slots(configuration) : SystemFunctions.IsModel(configuration) ? SystemFunctions.Slots(configuration) : MasterFunctions.IsModel(configuration) ? MasterFunctions.Slots(configuration).ToArray() : BbcMicroModelCatalog.All.Single(model => model.Model == configuration.Model).Firmware
        .Where(category => category != FirmwareCategory.DiscFilingSystem && !(ModelBPlusFunctions.IsModel(configuration) && category == FirmwareCategory.Basic))
        .Append(FirmwareCategory.CharacterGenerator).Distinct()
        .Where(category => FirmwareFunctions.Field(category) is not null)
        .Select(category => new FirmwareSlot(FirmwareFunctions.Field(category)!, "", category switch { FirmwareCategory.OperatingSystem => ModelBPlusFunctions.IsModel(configuration) ? Plus.SystemRomLabel : EmulatorConstants.SystemFirmwareLabel, FirmwareCategory.CharacterGenerator => "Emulation.Acorn.Firmware.CharacterGenerator", _ => "Emulation.Firmware.Rom.Basic" }, IsRequired: true, MachineIds: new HashSet<string> { configuration.Model.ToString() }))
        .Concat(FloppyExtensionFunctions.Slots(configuration))
        .Append(MemoryExtensionFunctions.Slot(configuration))
        .Concat(SidewaysExpansionFunctions.Slots(configuration))
        .Concat(NetworkFunctions.Read(configuration).Enabled ? [new FirmwareSlot(NetworkConstants.Firmware, "", "Emulation.Acorn.Network.Rom", IsRequired: true, MachineIds: new HashSet<string> { configuration.Model.ToString() })] : [])
        .Where(slot => !ModelBPlusFunctions.IsModel(configuration) || slot.FieldId is not (MemoryExtensionConstants.Firmware or FloppyExtensionConstants.IntelRom))
        .Select(slot => slot with { IsRequired = slot.IsRequired && !(SidewaysExpansionFunctions.RoleBank(configuration, slot.FieldId) is { } bank && SidewaysExpansionFunctions.ProvidesBank(configuration, bank)) }).ToArray();
    /// <summary>Analyse uniquement le dossier ROM choisi par le parcours commun.</summary>
    public async ValueTask<IReadOnlyList<EmulationFirmwareCandidate>> ScanFirmwareAsync(MachineConfiguration configuration, string directory, CancellationToken cancellationToken)
    {
        var scanned = await CommonCoreHostFunctions.FirmwareProcessing.ScanDirectoryAsync(directory,
            (path, token) => ScanFirmwareFileAsync(configuration, path, token), cancellationToken).ConfigureAwait(false);
        return scanned.SelectMany(set => set).ToArray();
    }

    public async ValueTask<IReadOnlyList<EmulationFirmwareCandidate>> ScanFirmwareFileAsync(MachineConfiguration configuration, string path, CancellationToken cancellationToken)
    {
        if (ArchimedesFunctions.IsModel(configuration)) return await ArchimedesFunctions.ScanFirmwareFileAsync(configuration, path, supportsArthur: ArchimedesFunctions.SupportsArthur(configuration), cancellationToken).ConfigureAwait(false);
        if (ElectronFunctions.IsModel(configuration)) return await ElectronFunctions.ScanFirmwareFileAsync(configuration, path, cancellationToken).ConfigureAwait(false);
        if (AtomFunctions.IsModel(configuration)) return await AtomFunctions.ScanFirmwareFileAsync(configuration, path, cancellationToken).ConfigureAwait(false);
        if (SystemFunctions.IsModel(configuration)) return await SystemFunctions.ScanFirmwareFileAsync(configuration, path, cancellationToken).ConfigureAwait(false);
        // Le gestionnaire firmware peut attendre ce résultat depuis le thread UI : ne pas y rapatrier la continuation.
        var candidates = await BbcMicroFirmwareFunctions.ScanAsync(configuration.Model, path, FirmwareFunctions.Field, cancellationToken).ConfigureAwait(false);
        if (ModelBPlusFunctions.IsModel(configuration))
            candidates = candidates.Select(candidate => candidate.DestinationFieldId == EmulatorConstants.SystemFirmware && new FileInfo(path).Length != Plus.ChipBytes
                    ? candidate with { Compatibility = EmulationFirmwareCompatibility.Incompatible } : candidate).ToArray();
        if (MasterFunctions.IsModel(configuration))
        {
            var masterCandidates = candidates.ToList();
            if (MasterFunctions.Coprocessor(configuration) != MasterCoprocessor.None)
                masterCandidates.Add(candidates[0] with { Id = candidates[0].Id + MasterSettingsConstants.CoprocessorRom,
                    DestinationFieldId = MasterSettingsConstants.CoprocessorRom,
                    Compatibility = AcornFirmware.BySha1.TryGetValue(candidates[0].Id.Split(':')[0], out var coproIdentity)
                        ? coproIdentity.Category == FirmwareCategory.Coprocessor && coproIdentity.CoprocessorCpu == MasterFunctions.CoprocessorCpu(configuration)
                            ? AcornBBCMicroModel.Get(configuration.Model, candidates[0].Id.Split(':')[0], FirmwareCategory.Coprocessor)
                            : EmulationFirmwareCompatibility.Incompatible : EmulationFirmwareCompatibility.Unknown });
            if (new FileInfo(path).Length is 8192 or 16384)
                foreach (var slot in MasterFunctions.Slots(configuration).Where(slot => slot.FieldId.StartsWith(MasterSettingsConstants.ChipPrefix, StringComparison.Ordinal)))
                    masterCandidates.Add(candidates[0] with { Id = candidates[0].Id + slot.FieldId, DestinationFieldId = slot.FieldId, Compatibility = SocketCompatibility(configuration.Model, candidates[0]) });
            return masterCandidates;
        }
        var result = candidates.Where(candidate => !ModelBPlusFunctions.IsModel(configuration) || candidate.DestinationFieldId != EmulatorConstants.BasicFirmware).SelectMany(candidate => candidate.DestinationFieldId == NativeStorageConstants.DfsFirmware
            ? FloppyExtensionFunctions.Destinations(candidate)
            : candidate.DestinationFieldId == MemoryExtensionConstants.Firmware && MemoryExtensionFunctions.Read(configuration).Enabled
                ? new[] { MemoryExtensionFunctions.Destination(configuration, candidate) } : new[] { candidate }).ToList();
        if (MemoryExtensionFunctions.Read(configuration).Enabled && candidates.Count > 0
            && candidates.All(candidate => candidate.Compatibility == EmulationFirmwareCompatibility.Unknown)
            && new FileInfo(path).Length is BbcMicroFirmwareConstants.BankSize or BbcMicroFirmwareConstants.BankSize / 2)
            result.Add(candidates[0] with { Id = candidates[0].Id + ":" + FirmwareCategory.MemoryExtension, DestinationFieldId = MemoryExtensionConstants.Firmware });
        if (candidates.Count > 0 && (ModelBPlusFunctions.IsModel(configuration) ? ModelBPlusFunctions.AcceptsLength(new FileInfo(path).Length) : SidewaysExpansionFunctions.AcceptsLength(SidewaysExpansionFunctions.Read(configuration), new FileInfo(path).Length)))
            foreach (var slot in SidewaysExpansionFunctions.Slots(configuration))
                result.Add(candidates[0] with { Id = candidates[0].Id + slot.FieldId, DestinationFieldId = slot.FieldId, Compatibility = SocketCompatibility(configuration.Model, candidates[0]) });
        return result;
    }
    internal static EmulationFirmwareCompatibility SocketCompatibility(MachineModel model, EmulationFirmwareCandidate candidate)
    {
        var hash = candidate.Id.Split(':')[0];
        if (!AcornFirmware.BySha1.ContainsKey(hash)) return EmulationFirmwareCompatibility.Unknown;
        foreach (var category in new[] { FirmwareCategory.Basic, FirmwareCategory.Language, FirmwareCategory.Application,
                     FirmwareCategory.DiscFilingSystem, FirmwareCategory.Network, FirmwareCategory.MemoryExtension })
        {
            var compatibility = AcornBBCMicroModel.Get(model, hash, category);
            if (compatibility != EmulationFirmwareCompatibility.Incompatible) return compatibility;
        }
        return EmulationFirmwareCompatibility.Incompatible;
    }

    public IReadOnlyList<EmulationSettingsBlock> DescribeSettings(MachineConfiguration configuration, EmulatorManagementContext? context = null)
    {
        if (ArchimedesFunctions.IsModel(configuration)) return ArchimedesFunctions.Describe(configuration);
        if (ElectronFunctions.IsModel(configuration)) return ElectronFunctions.Describe(configuration);
        if (AtomFunctions.IsModel(configuration)) return AtomFunctions.Describe(configuration);
        if (SystemFunctions.IsModel(configuration)) return SystemFunctions.Describe(configuration);
        if (MasterFunctions.IsModel(configuration)) return MasterFunctions.Describe(configuration);
        var fields = GetFirmwareSlots(configuration).Where(slot => slot.FieldId is not (FloppyExtensionConstants.IntelRom or FloppyExtensionConstants.WdRom or MemoryExtensionConstants.Firmware) && !slot.FieldId.StartsWith(Plus.ChipPrefix, StringComparison.Ordinal) && !slot.FieldId.StartsWith(SidewaysExpansionConstants.ChipPrefix, StringComparison.Ordinal) && !slot.FieldId.StartsWith(SidewaysExpansionConstants.AriesChipPrefix, StringComparison.Ordinal)).Select(slot => new EmulationSettingsField(slot.FieldId, EmulationMachineTab.Rom, "Rom", slot.LabelResourceKey, EmulationSettingsEditor.Path, configuration.Options.GetValueOrDefault(slot.FieldId), IsEnabled: !(SidewaysExpansionFunctions.RoleBank(configuration, slot.FieldId) is { } bank && SidewaysExpansionFunctions.ProvidesBank(configuration, bank)), DefaultFolderCategory: EmulationDefaultFolderCategory.Firmware, RequiresRestart: true)).ToArray();
        return
        [
            NetworkFunctions.Describe(configuration),
            FloppyExtensionFunctions.Describe(configuration),
            new(SettingsConstants.MainMemory, EmulationMachineTab.Ram, SettingsConstants.ResourceMemory,
                [new(SettingsConstants.MainMemory, EmulationMachineTab.Ram, SettingsConstants.MainMemory, SettingsConstants.ResourceMemory, EmulationSettingsEditor.Selection,
                    configuration.Options.GetValueOrDefault(SettingsConstants.MainMemory, ModelBPlusFunctions.IsModel(configuration) ? Plus.Memory64 : configuration.Model == MachineModel.BbcMicroModelB ? BbcMicroModelConstants.ThirtyTwoKibibytesValue : BbcMicroModelAConstants.StandardMemoryKib),
                    ModelBPlusFunctions.IsModel(configuration)
                        ? [new(Plus.Memory64, string.Empty, Plus.Memory64Display), new(Plus.Memory128, string.Empty, Plus.Memory128Display)]
                        : configuration.Model == MachineModel.BbcMicroModelB
                        ? [new(BbcMicroModelConstants.ThirtyTwoKibibytesValue, string.Empty, BbcMicroModelConstants.ThirtyTwoKibibytesDisplay)]
                        : [new(BbcMicroModelAConstants.StandardMemoryKib, string.Empty, BbcMicroModelConstants.SixteenKibibytesDisplay), new(BbcMicroModelAConstants.ExpandedMemoryKib, string.Empty, BbcMicroModelConstants.ThirtyTwoKibibytesDisplay)], IsEnabled: configuration.Model != MachineModel.BbcMicroModelB && !MemoryExtensionFunctions.Read(configuration).Enabled && !SidewaysExpansionFunctions.Read(configuration).AriesB12, RequiresRestart: true, RefreshSettingsOnChange: true)]),
            ModelBPlusFunctions.IsModel(configuration) ? ModelBPlusFunctions.DescribeMemory(configuration) : MemoryExtensionFunctions.Describe(configuration),
            SidewaysExpansionFunctions.Describe(configuration) with { IsVisible = !ModelBPlusFunctions.IsModel(configuration) },
            SidewaysExpansionFunctions.DescribeRam(configuration),
            SidewaysExpansionFunctions.DescribeRoms(configuration),
            new("Rom", EmulationMachineTab.Rom, "Emulation.Firmware", fields),
            new(VideoStandardConstants.Option, EmulationMachineTab.Video, VideoStandardConstants.Label,
                [new(VideoStandardConstants.Option, EmulationMachineTab.Video, VideoStandardConstants.Option, VideoStandardConstants.Label, EmulationSettingsEditor.Selection,
                    configuration.Options.GetValueOrDefault(VideoStandardConstants.Option, VideoStandardConstants.Automatic),
                    [new(VideoStandardConstants.Automatic, VideoStandardConstants.AutomaticLabel), new(VideoStandardConstants.Pal, VideoStandardConstants.PalLabel), new(VideoStandardConstants.Ntsc, VideoStandardConstants.NtscLabel)],
                    RequiresRestart: true, ExplanationResourceKey: VideoStandardConstants.Help)]),
            new(EmulatorConstants.VideoOutput, EmulationMachineTab.Video, "Emulation.Acorn.Video.Output",
                [new(EmulatorConstants.VideoOutput, EmulationMachineTab.Video, EmulatorConstants.VideoOutput, "Emulation.Acorn.Video.Output", EmulationSettingsEditor.Selection,
                    configuration.Options.GetValueOrDefault(EmulatorConstants.VideoOutput, EmulatorConstants.UhfOutput),
                    [new(EmulatorConstants.UhfOutput, "Emulation.Acorn.Video.Output.Uhf"), new(EmulatorConstants.MonochromeOutput, "Emulation.Acorn.Video.Output.Monochrome"), new(EmulatorConstants.ColourCompositeOutput, "Emulation.Acorn.Video.Output.ColourComposite"), new(EmulatorConstants.RgbOutput, "Emulation.Acorn.Video.Output.Rgb")])])
        ];
    }
}

