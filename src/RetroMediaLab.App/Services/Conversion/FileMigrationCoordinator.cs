using RetroMediaLab.MediaEngine.Contracts.Migration;
using System.IO;
using RetroMediaLab.MediaEngine.Images.Reading;
using RetroMediaLab.MediaEngine.Contracts.Explorer;
using RetroMediaLab.MediaEngine.Images.Creation;

namespace RetroMediaLab.App.Services.Conversion;

/// <summary>Prépare et exécute les migrations de fichiers sans les confondre avec les conversions d'images.</summary>
public sealed class FileMigrationCoordinator(DiskImageExplorer explorer, FileSystemMigrationService migrationService)
{
    internal enum DestinationStatus { Available, Existing, SameAsSource }

    internal static DestinationStatus CheckDestination(string source, string destination)
    {
        if (Path.GetFullPath(source).Equals(Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase))
            return DestinationStatus.SameAsSource;

        return File.Exists(destination) ? DestinationStatus.Existing : DestinationStatus.Available;
    }

    internal static string SuggestedOutputName(string source, FileSystemMigrationTarget? target) =>
        target is null ? string.Empty : $"{Path.GetFileNameWithoutExtension(source)}-migrated{target.Extension}";

    internal static string SuggestedOutputPath(string source, FileSystemMigrationTarget? target) =>
        target is null || string.IsNullOrWhiteSpace(source) ? string.Empty
            : Path.Combine(Path.GetDirectoryName(source) ?? string.Empty, SuggestedOutputName(source, target));

    public async Task<FileSystemVolume> ReadSourceAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) throw new FileNotFoundException("The migration source image does not exist.", path);
        var explored = await explorer.ExploreAsync(path, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (!explored.FileSystemRecognized) throw new InvalidDataException("The source image does not contain a recognized file-system catalogue.");
        return explored.Volume;
    }

    public MigrationValidationReport Validate(FileSystemVolume source, string targetFormatId, bool acceptMetadataLoss = false) => migrationService.Validate(source, targetFormatId, acceptMetadataLoss);

    public Task<MigrationResult> ExecuteAsync(FileSystemVolume source, string outputPath, string targetFormatId, bool acceptMetadataLoss, CancellationToken cancellationToken = default) => migrationService.WriteAsync(source, outputPath, targetFormatId, acceptMetadataLoss, cancellationToken);
}
