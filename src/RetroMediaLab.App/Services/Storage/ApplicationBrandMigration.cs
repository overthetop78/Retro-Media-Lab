using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using RetroMediaLab.App.Constants.Storage;
using C = RetroMediaLab.App.Constants.Storage.ApplicationBrandMigrationConstants;

namespace RetroMediaLab.App.Services.Storage;

internal static class ApplicationBrandMigration
{
    private static readonly object Gate = new();
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    internal static void Migrate(string applicationDirectory, string roamingDirectory, string documentsDirectory)
    {
        lock (Gate)
        {
            var portable = File.Exists(Path.Combine(applicationDirectory, C.PortableFlag));
            var dataDirectory = portable
                ? Path.Combine(applicationDirectory, C.PortableDataDirectory)
                : Path.Combine(roamingDirectory, C.ProductDirectory);
            var marker = Path.Combine(dataDirectory, C.MarkerFile);
            if (File.Exists(marker)) return;

            var mappings = new List<(string Source, string Target)>();
            if (!portable) mappings.Add((Path.Combine(roamingDirectory, C.LegacyProductDirectory), dataDirectory));
            mappings.Add((Path.Combine(documentsDirectory, C.LegacyProductDirectory), Path.Combine(documentsDirectory, C.ProductDirectory)));
            foreach (var mapping in mappings) MergeDirectory(mapping.Source, mapping.Target);

            foreach (var root in mappings.Select(mapping => mapping.Target).Append(dataDirectory).Distinct(StringComparer.OrdinalIgnoreCase))
                if (Directory.Exists(root))
                    foreach (var file in JsonFiles(root)) RebaseJson(file, mappings);

            Directory.CreateDirectory(dataDirectory);
            File.WriteAllText(marker, C.ProductDirectory);
        }
    }

    private static void MergeDirectory(string source, string target)
    {
        if (!Directory.Exists(source)) return;
        EnsureRegularDirectory(source);
        if (!Directory.Exists(target))
        {
            Directory.Move(source, target);
            return;
        }
        EnsureRegularDirectory(target);
        foreach (var directory in Directory.EnumerateDirectories(source))
            MergeDirectory(directory, Path.Combine(target, Path.GetFileName(directory)));
        foreach (var file in Directory.EnumerateFiles(source))
        {
            if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0)
                throw new IOException($"Cannot migrate a linked file: {file}");
            var destination = Path.Combine(target, Path.GetFileName(file));
            var index = C.InitialConflictIndex;
            while (File.Exists(destination) || Directory.Exists(destination))
                destination = Path.Combine(target, Path.GetFileName(file))
                    + string.Format(CultureInfo.InvariantCulture, C.ConflictSuffixFormat, index++);
            File.Move(file, destination);
        }
        Directory.Delete(source);
    }

    private static IEnumerable<string> JsonFiles(string directory)
    {
        EnsureRegularDirectory(directory);
        foreach (var file in Directory.EnumerateFiles(directory, C.JsonPattern).Concat(Directory.EnumerateFiles(directory, C.JsonBackupPattern)))
            if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) == 0) yield return file;
        foreach (var child in Directory.EnumerateDirectories(directory))
            if ((File.GetAttributes(child) & FileAttributes.ReparsePoint) == 0)
                foreach (var file in JsonFiles(child)) yield return file;
    }

    private static void EnsureRegularDirectory(string directory)
    {
        if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
            throw new IOException($"Cannot migrate a linked directory: {directory}");
    }

    private static void RebaseJson(string file, IReadOnlyList<(string Source, string Target)> mappings)
    {
        JsonNode? root;
        try { root = JsonNode.Parse(File.ReadAllText(file)); }
        catch (JsonException) { return; }
        if (root is null || !RebaseNode(root, mappings)) return;
        var backup = file + C.OriginalJsonSuffix;
        if (!File.Exists(backup)) File.Copy(file, backup);
        var temporary = file + C.TemporarySuffix;
        try
        {
            File.WriteAllText(temporary, root.ToJsonString(JsonOptions));
            File.Move(temporary, file, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    private static bool RebaseNode(JsonNode node, IReadOnlyList<(string Source, string Target)> mappings)
    {
        var changed = false;
        if (node is JsonObject obj)
        {
            foreach (var property in obj.ToArray())
            {
                if (property.Value is JsonValue value && value.TryGetValue<string>(out var text))
                {
                    var updated = RebasePath(text, mappings);
                    if (updated != text) { obj[property.Key] = updated; changed = true; }
                }
                else if (property.Value is not null) changed |= RebaseNode(property.Value, mappings);
            }
        }
        else if (node is JsonArray array)
        {
            for (var index = 0; index < array.Count; index++)
            {
                if (array[index] is JsonValue value && value.TryGetValue<string>(out var text))
                {
                    var updated = RebasePath(text, mappings);
                    if (updated != text) { array[index] = updated; changed = true; }
                }
                else if (array[index] is { } child) changed |= RebaseNode(child, mappings);
            }
        }
        return changed;
    }

    internal static string RebasePath(string text, IReadOnlyList<(string Source, string Target)> mappings)
    {
        var normalized = text.Replace('\\', '/');
        foreach (var mapping in mappings)
        {
            var source = mapping.Source.Replace('\\', '/').TrimEnd('/');
            if (string.Equals(normalized, source, StringComparison.OrdinalIgnoreCase)) return mapping.Target;
            if (normalized.StartsWith(source + '/', StringComparison.OrdinalIgnoreCase))
                return mapping.Target + text[source.Length..];
        }
        return text;
    }
}
