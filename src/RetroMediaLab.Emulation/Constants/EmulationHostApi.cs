namespace RetroMediaLab.Emulation.Constants;

public static class EmulationHostApi
{
    public const int ManifestSchemaVersion = 2;
    public const string ManifestFileName = "module.json";
    public static Version CurrentVersion { get; } = new(2, 0);
    /// <summary>Première version de module acceptée par cette génération de l’application.</summary>
    public static Version MinimumModuleVersion { get; } = new(2, 0, 0);
}
