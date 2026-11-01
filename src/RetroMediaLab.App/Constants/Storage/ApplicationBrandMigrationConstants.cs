namespace RetroMediaLab.App.Constants.Storage;

internal static class ApplicationBrandMigrationConstants
{
    internal const string ProductDirectory = "Retro Media Lab";
    internal const string LegacyProductDirectory = "GW GUI";
    internal const string PortableFlag = "portable.flag";
    internal const string PortableDataDirectory = "Data";
    internal const string MarkerFile = ".brand-migration-v1";
    internal const string JsonPattern = "*.json";
    internal const string JsonBackupPattern = "*.json.bak";
    internal const string OriginalJsonSuffix = ".before-rename";
    internal const string TemporarySuffix = ".brand-migration.tmp";
    internal const string ConflictSuffixFormat = ".previous-installation.{0}";
    internal const int InitialConflictIndex = 1;
}
