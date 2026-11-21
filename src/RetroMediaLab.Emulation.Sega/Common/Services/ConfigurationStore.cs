using CommonCoreHostFunctions = RetroMediaLab.Emulation.Sega.Common.Functions.CoreHostFunctions;
using RetroMediaLab.Emulation;
using System.IO;
using System.Text.Json;
using RetroMediaLab.Emulation.Functions;

namespace RetroMediaLab.Emulation.Sega.Common.Services;

internal sealed class ConfigurationStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = ConfigurationStoreConstants.WriteIndentedJson
    };
    private readonly string _directory;
    private readonly string _pathBase;
    private static readonly SemaphoreSlim SaveGate = new(ConfigurationStoreConstants.InitialWriterCount, ConfigurationStoreConstants.MaximumWriterCount);

    public ConfigurationStore(string directory, string? pathBase = null)
    {
        _directory = Path.GetFullPath(directory);
        _pathBase = Path.GetFullPath(pathBase ?? directory);
    }

    public Task<IReadOnlyList<MachineConfiguration>> LoadAllAsync(CancellationToken cancellationToken = default)
        => CommonCoreHostFunctions.ConfigurationFiles.ReadDocumentsAsync(_directory, ConfigurationStoreConstants.MachineFileName,
            static json => JsonSerializer.Deserialize<MachineConfiguration>(json, JsonOptions),
            static value => value.SchemaVersion == ConfigurationStoreConstants.CurrentSchemaVersion,
            value => ResolvePaths(MigrateMegaDrive(MigrateMasterSystem(value.EnsureId()))), cancellationToken);

    public Task SaveAsync(MachineConfiguration configuration, CancellationToken cancellationToken = default)
        => CommonCoreHostFunctions.ConfigurationFiles.WriteDocumentAsync(SaveGate, _directory, configuration,
            static value => value.EnsureId() with { SchemaVersion = ConfigurationStoreConstants.CurrentSchemaVersion },
            static value => value.Id, StorePaths, JsonOptions, cancellationToken);

    public void Delete(Guid id) => CommonCoreHostFunctions.ConfigurationFiles.DeleteDirectoryIfExists(Path.Combine(_directory, id.ToString(ConfigurationStoreConstants.MachineIdentifierFormat)), ConfigurationStoreConstants.RecursiveDirectoryDelete);

    internal static MachineConfiguration MigrateMasterSystem(MachineConfiguration configuration)
    {
        if (configuration.Model != ModelConstants.MasterSystem || configuration.Options is not { } options)
            return configuration;
        const string nativeVariant = "segaemu_master_system_model";
        if (!options.ContainsKey(SettingsConstants.MasterSystemVariant) && !options.ContainsKey(nativeVariant))
            return configuration;

        var current = new Dictionary<string, string>(options, StringComparer.Ordinal);
        bool isModelII = options.GetValueOrDefault(SettingsConstants.MasterSystemVariant) == ModelConstants.MasterSystemSmsIi
            || options.GetValueOrDefault(nativeVariant) == "master system II";
        current.Remove(SettingsConstants.MasterSystemVariant);
        current.Remove(nativeVariant);
        if (!isModelII) return configuration with { Options = current };

        const string emulator = Emulators.SegaEmu.Constants.CoreConstants.Id;
        return configuration with
        {
            Model = ModelConstants.MasterSystemII,
            EmulatorId = emulator,
            Options = current,
            Media = configuration.Media?.Where(media => media.Slot != EmulationMediaSlot.Cartridge1).ToArray()
        };
    }

    internal static MachineConfiguration MigrateMegaDrive(MachineConfiguration configuration)
    {
        if (configuration.EmulatorId != Emulators.SegaEmu.Constants.CoreConstants.Id)
            return configuration;

        string oldModel = configuration.Model;
        if (!ModelConstants.IsMegaDriveFamily(oldModel)) return configuration;

        bool cd = ModelConstants.HasMegaCd(oldModel) || configuration.MegaCdEnabled
            || oldModel == ModelConstants.ThirtyTwoX && configuration.Media?.Any(media => media.Category == MediaCategory.CompactDisc) == true;
        bool x32 = ModelConstants.HasThirtyTwoX(oldModel) || configuration.ThirtyTwoXEnabled;
        string model = cd
            ? configuration.MegaCdModel == ModelConstants.MegaCdII ? ModelConstants.MegaCdSecond : ModelConstants.MegaCd
            : configuration.IsMegaDriveModelII ? ModelConstants.MegaDriveII : ModelConstants.MegaDrive;
        var options = new Dictionary<string, string>(configuration.Options ?? new Dictionary<string, string>(), StringComparer.Ordinal);
        options.Remove(SettingsConstants.MegaCdEnabled);
        options.Remove(SettingsConstants.MegaCdModel);
        if (!cd) options.Remove(SettingsConstants.MegaDriveModel);
        if (x32) options[SettingsConstants.MegaDriveThirtyTwoX] = "enabled";
        return configuration with { Model = model, Options = options };
    }

    private MachineConfiguration StorePaths(MachineConfiguration configuration) => configuration with
    {
        Media = configuration.Media?.Select(media => media with { Path = StorePath(media.Path)! }).ToArray()
    };

    private MachineConfiguration ResolvePaths(MachineConfiguration configuration) => configuration with
    {
        Media = configuration.Media?.Select(media => media with { Path = ResolvePath(media.Path)! }).ToArray()
    };

    private string? StorePath(string? path)
        => CommonCoreHostFunctions.ConfigurationFiles.StorePath(string.IsNullOrWhiteSpace(path) ? null : path, _pathBase);

    private string? ResolvePath(string? path)
        => CommonCoreHostFunctions.ConfigurationFiles.ResolvePath(string.IsNullOrWhiteSpace(path) ? null : path, _pathBase);
}
