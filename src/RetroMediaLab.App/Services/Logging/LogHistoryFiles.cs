using System.IO;

namespace RetroMediaLab.App.Services.Logging;

internal static class LogHistoryFiles
{
    internal static FileInfo[] List(string directory)
    {
        if (!Directory.Exists(directory)) return [];
        return new DirectoryInfo(directory).GetFiles("*.log")
            .Where(file => !file.Name.StartsWith("errors-", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .ToArray();
    }

    internal static async Task<(string? Content, IOException? Error)> ReadAsync(FileInfo file, CancellationToken cancellationToken)
    {
        try { return (await File.ReadAllTextAsync(file.FullName, cancellationToken), null); }
        catch (IOException error)
        {
            ErrorLog.Write(error, "Reading log history");
            return (null, error);
        }
    }

    internal static Task ExportAsync(string path, string content) => File.WriteAllTextAsync(path, content);
}
