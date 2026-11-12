using System.IO;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Services;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.SG1000.Enums;
using C = RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Constants.EmulatorConstants;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Factories;

/// <summary>Built-in backend discovered through the existing Common adapter catalogue.</summary>
internal sealed class MachineFactory : IEmulatorAdapter
{
    public string EmulatorId => C.Id;
    public string EmulatorKey => C.Id;
    public EmulationEmulatorDefinition Definition { get; } = new(C.Id, C.Name,
        "Emulation.Emulator.segaemu.Description", new HashSet<string>(StringComparer.Ordinal) { ModelConstants.Sg1000 });
    public bool SupportsNoContent => true;
    public IReadOnlyList<string> SupportedContentExtensions => C.Extensions.ToArray();
    public IReadOnlySet<string>? GetCartridgeExtensions(MachineConfiguration configuration) => C.Extensions;
    public bool SupportsPointerCapture(MachineConfiguration configuration) => false;
    public IReadOnlyList<InputBindingDefinition> GetKeyboardDefinitions(MachineConfiguration configuration) => [];
    public IReadOnlyList<EmulationControllerOutputDefinition> GetControllerOutputs(MachineConfiguration configuration, PeripheralCategory peripheral) => [];
    public MachineConfiguration ApplyInputConfiguration(MachineConfiguration configuration) => configuration;
    public IReadOnlyDictionary<string, string> GetRuntimeOptions(MachineConfiguration configuration) => configuration.Options ?? new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string> GetDefaultOptions(MachineConfiguration configuration) => new Dictionary<string, string>
    {
        [C.InternalGameEnabled] = bool.FalseString,
        [C.CartridgeLayout] = Sg1000CartridgeLayout.Standard.ToString()
    };
    public bool RequiresOptionRestart(MachineConfiguration configuration, string key) => key is C.InternalGameEnabled or C.InternalGameFirmware or C.CartridgeLayout;
    public (int Width, int Height)? GetOutputResolution(MachineConfiguration configuration) => (256, 192);
    public bool TryHandleHostCommand(IReadOnlyList<string> arguments, out int exitCode) { exitCode = 0; return false; }
    public ValueTask<EmulationEmulatorInstallation> GetInstallationAsync(EmulatorManagementContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new EmulationEmulatorInstallation(Definition, C.Version));
    }
    public ValueTask<IReadOnlyList<EmulationEmulatorRelease>> FindReleasesAsync(EmulatorManagementContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<IReadOnlyList<EmulationEmulatorRelease>>([new(C.Id, C.Name, C.Version, IsRequired: true)]);
    }
    public ValueTask<string> InstallAsync(EmulatorManagementContext context, EmulationEmulatorRelease release, IProgress<double>? progress, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (release.Id != C.Id || release.Version != C.Version) throw new ArgumentException(nameof(release));
        progress?.Report(1);
        return ValueTask.FromResult(typeof(MachineFactory).Assembly.Location);
    }
    public ValueTask<string?> FindInstalledCorePathAsync(EmulatorManagementContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<string?>(typeof(MachineFactory).Assembly.Location);
    }
    public IReadOnlyList<EmulationMedia> ResolveConfiguredMedia(MachineConfiguration configuration) => EmulationMediaConversionFunctions.ToCommon(configuration.Media ?? []);
    public IReadOnlyList<EmulationMediaDevice> DescribeMediaDevices(MachineConfiguration configuration) =>
        [new(EmulationMediaSlot.Cartridge0, EmulationMediaType.Cartridge, C.Extensions.ToArray())];
    public IReadOnlyDictionary<EmulationMediaSlot, bool> MediaActivity(MachineConfiguration configuration, IReadOnlyDictionary<int, bool> ledStates) => new Dictionary<EmulationMediaSlot, bool>();
    public ValueTask<EmulationMedia> PrepareMediaAsync(EmulationMedia media, string conversionDirectory) => ValueTask.FromResult(media);
    public Task<MachineConfiguration> PrepareRuntimeConfigurationAsync(MachineConfiguration configuration, string conversionDirectory) => Task.FromResult(configuration);
    public EmulationStorageSettings DescribeStorage(MachineConfiguration configuration) => StorageSettingsFunctions.Describe(configuration);
    public MachineConfiguration ApplyStorage(MachineConfiguration configuration, EmulationStorageSettings settings) => StorageSettingsFunctions.Apply(configuration, settings);
    public EmulationConfigurationSummary Summarize(MachineConfiguration configuration) => ConfigurationSummaryFunctions.Create(configuration);

    public IReadOnlyList<FirmwareSlot> GetFirmwareSlots(MachineConfiguration configuration) =>
        EmulatorCore.ReadRomConfiguration(configuration).InternalGameEnabled
            ? [new(C.InternalGameFirmware, string.Empty, C.InternalRomLabel, IsRequired: true)] : [];
    public async ValueTask<IReadOnlyList<EmulationFirmwareCandidate>> ScanFirmwareFileAsync(MachineConfiguration configuration, string path, CancellationToken cancellationToken)
    {
        var candidates = await FirmwareConfigurationFunctions.ScanFileAsync(configuration, path, this, cancellationToken).ConfigureAwait(false);
        // Size establishes a loading constraint, never the identity of an Othello dump.
        return new FileInfo(path).Length == 16384 ? candidates
            : candidates.Select(candidate => candidate with { Compatibility = EmulationFirmwareCompatibility.Incompatible }).ToArray();
    }
    public void ValidateConfiguration(MachineConfiguration configuration)
    {
        if (configuration.Model != ModelConstants.Sg1000 || configuration.EmulatorId != C.Id) throw new ArgumentException(nameof(configuration));
        _ = EmulatorCore.ReadRomConfiguration(configuration);
        foreach (var controller in configuration.Controllers ?? []) EmulatorCore.ValidateController(controller);
        foreach (var binding in configuration.Input?.Controllers ?? [])
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)binding.Port, 2U);
            EmulatorCore.ValidateController(binding.Peripheral);
        }
        var media = configuration.Media ?? [];
        if (media.Any(item => item.Slot != EmulationMediaSlot.Cartridge0 || item.Category != MediaCategory.Cartridge)
            || media.Count(item => item.IsInserted) > 1) throw new ArgumentException("SG-1000 has one cartridge slot.", nameof(configuration));
    }
    public Machine Create(MachineConfiguration configuration, EmulatorCreationContext context)
    {
        ValidateConfiguration(configuration);
        var id = Guid.NewGuid();
        var core = new EmulatorCore();
        bool transferred = false;
        try
        {
            var machine = new Machine(id, configuration, core, configuration.Media ?? [],
                Path.Combine(context.SessionsDirectory, id.ToString(ConfigurationStoreConstants.MachineIdentifierFormat)),
                saveDirectory: context.SaveDirectoryResolver?.Invoke(configuration), inputMapper: this,
                audioOutputFactory: context.AudioOutputFactory, runtimePolicy: this);
            transferred = true;
            return machine;
        }
        finally { if (!transferred) core.Dispose(); }
    }

    public IReadOnlyList<EmulationSettingsBlock> DescribeSettings(MachineConfiguration configuration, EmulatorManagementContext? context = null)
    {
        var rom = EmulatorCore.ReadRomConfiguration(configuration);
        return
        [
            new("segaemu.rom", EmulationMachineTab.Rom, SettingsDescriptionFunctionsConstants.ResourceRom,
            [new(C.InternalGameEnabled, EmulationMachineTab.Rom, "segaemu.rom", C.InternalGameLabel,
                EmulationSettingsEditor.Toggle, rom.InternalGameEnabled.ToString(), ExplanationResourceKey: C.InternalGameHelp,
                EnabledValue: bool.TrueString, DisabledValue: bool.FalseString, RefreshSettingsOnChange: true, RequiresRestart: true)]),
            new("segaemu.cartridge", EmulationMachineTab.General, C.LayoutLabel,
            [new(C.CartridgeLayout, EmulationMachineTab.General, "segaemu.cartridge", C.LayoutLabel,
                EmulationSettingsEditor.Selection, rom.CartridgeLayout.ToString(),
                [new(Sg1000CartridgeLayout.Standard.ToString(), C.StandardLayoutLabel),
                 new(Sg1000CartridgeLayout.Two8KiB.ToString(), C.TwoRomLayoutLabel),
                 new(Sg1000CartridgeLayout.TheCastle.ToString(), C.CastleLayoutLabel),
                 new(Sg1000CartridgeLayout.Othello.ToString(), C.OthelloLayoutLabel)], RequiresRestart: true)])
        ];
    }
    public MachineConfiguration ApplySettings(MachineConfiguration configuration, IReadOnlyDictionary<string, string?> values)
    {
        // Common has already copied the option values; use that result to resolve a newly enabled slot.
        return FirmwareConfigurationFunctions.Apply(configuration, values, this);
    }
}
