using RetroMediaLab.App.Services.Windows;
using RetroMediaLab.App.Constants.Storage;
using RetroMediaLab.Infrastructure.Settings;
using System.IO;

namespace RetroMediaLab.App.Services.Storage;

public static class StoragePaths
{
    private static string? _configuredEmulationStorageDirectory;
    private static string? _configuredEmulationStateDirectory;
    private static string? _configuredEmulationCaptureDirectory;
    public static bool IsPortable => File.Exists(Path.Combine(AppContext.BaseDirectory, "portable.flag"));
    public static string DataDirectory => ResolveDataDirectory(AppContext.BaseDirectory, Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
    internal static string ShaderCacheDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        ApplicationBrandMigrationConstants.ProductDirectory, "ShaderCache");
    public static string LogsDirectory => LogsDirectoryFor(DataDirectory);
    internal static string LogsDirectoryFor(string dataDirectory) => Path.Combine(dataDirectory, "Logs");
    internal static ISettingsStore OpenSettingsStore(string dataDirectory) =>
        new JsonSettingsStore(Path.Combine(dataDirectory, "settings.json"));
    public static string EmulationDirectory => Path.Combine(DataDirectory, "Emulation");
    public static string FirmwareCatalogDirectory => Path.Combine(EmulationDirectory, "Catalogs", "Firmware");
    public static string FirmwareCatalogPath(string platform) => Path.Combine(FirmwareCatalogDirectory, $"{platform.ToLowerInvariant()}.json");
    public static string EmulationStorageDirectory => string.IsNullOrWhiteSpace(_configuredEmulationStorageDirectory) ? Path.Combine(DataDirectory, "Emulation", "Storage") : Path.GetFullPath(_configuredEmulationStorageDirectory);
    public static string EmulationStateDirectory => ResolveConfiguredDirectory(_configuredEmulationStateDirectory, Path.Combine(EmulationStorageDirectory, StoragePathConstants.StatesDirectory));
    public static string EmulationCaptureDirectory => ResolveConfiguredDirectory(_configuredEmulationCaptureDirectory, Path.Combine(EmulationStorageDirectory, StoragePathConstants.CapturesDirectory));

    public static void ConfigureEmulationStorageDirectory(string? directory) => _configuredEmulationStorageDirectory = string.IsNullOrWhiteSpace(directory) ? null : directory.Trim();

    public static void ConfigureEmulationStateDirectory(string? directory) => _configuredEmulationStateDirectory = NormalizeConfiguredDirectory(directory);

    public static void ConfigureEmulationCaptureDirectory(string? directory) => _configuredEmulationCaptureDirectory = NormalizeConfiguredDirectory(directory);

    internal static void ConfigureEmulationDirectories(AppSettings settings)
    {
        ConfigureEmulationStorageDirectory(settings.EmulationStorageFolder);
        ConfigureEmulationCaptureDirectory(settings.EmulationCaptureFolder);
        ConfigureEmulationStateDirectory(settings.EmulationStateFolder);
    }

    private static string ResolveConfiguredDirectory(string? configured, string fallback) => string.IsNullOrWhiteSpace(configured) ? fallback : Path.GetFullPath(configured);

    private static string? NormalizeConfiguredDirectory(string? directory) => string.IsNullOrWhiteSpace(directory) ? null : directory.Trim();
    public static string HostToolsDirectory => Path.Combine(DataDirectory, "Greaseweazle");

    public static string? NormalizeHostToolsPath(string? path) => path;

    public static string ResolveDataDirectory(string applicationDirectory, string roamingDirectory) => ResolveDataDirectory(applicationDirectory, roamingDirectory, File.Exists);

    internal static string ResolveDataDirectory(string applicationDirectory, string roamingDirectory, Func<string, bool> exists) => exists(Path.Combine(applicationDirectory, "portable.flag")) ? Path.Combine(applicationDirectory, "Data") : Path.Combine(roamingDirectory, ApplicationBrandMigrationConstants.ProductDirectory);

    internal static string EmulationModuleDirectory(string moduleId, string machineId, EmulationDefaultFolderCategory? category, string? firmwareDirectory = null)
    {
        var moduleFolder = moduleId.Length == 0 ? moduleId : char.ToUpperInvariant(moduleId[0]) + moduleId[1..];
        return category switch
        {
            EmulationDefaultFolderCategory.Floppy => Path.Combine(EmulationStorageDirectory, StoragePathConstants.FloppiesDirectory, moduleFolder),
            EmulationDefaultFolderCategory.CompactDisc => Path.Combine(EmulationStorageDirectory, StoragePathConstants.CompactDiscsDirectory, moduleFolder),
            EmulationDefaultFolderCategory.HardDisk => Path.Combine(EmulationStorageDirectory, StoragePathConstants.HardDisksDirectory, moduleFolder),
            EmulationDefaultFolderCategory.Cartridge => Path.Combine(EmulationStorageDirectory, StoragePathConstants.CartridgesDirectory, moduleFolder),
            EmulationDefaultFolderCategory.Tape => Path.Combine(EmulationStorageDirectory, StoragePathConstants.TapesDirectory, moduleFolder),
            EmulationDefaultFolderCategory.State => Path.Combine(EmulationStateDirectory, moduleFolder),
            EmulationDefaultFolderCategory.Capture => Path.Combine(EmulationCaptureDirectory, moduleFolder),
            EmulationDefaultFolderCategory.Cheats => Path.Combine(EmulationStorageDirectory, "Cheats", moduleFolder, machineId),
            EmulationDefaultFolderCategory.Firmware => firmwareDirectory
                ?? Path.Combine(EmulationStorageDirectory, StoragePathConstants.RomsDirectory, moduleFolder),
            _ => EmulationStorageDirectory
        };
    }

    internal static void OpenDirectoryInExplorer(string directory)
    {
        EnsureDirectory(directory);
        ShellOpenService.Open(directory);
    }

    internal static void EnsureDirectory(string directory) => Directory.CreateDirectory(directory);

    internal static string PrepareMemoryCardDirectory(string path)
    {
        var directory = Path.GetFullPath(path);
        EnsureDirectory(directory);
        return directory;
    }
}
