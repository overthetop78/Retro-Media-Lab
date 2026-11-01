using System.IO.Enumeration;

namespace RetroMediaLab.App.Contracts.Services.Dialogs;

internal sealed record FileSelectionFilter(string Name, IReadOnlyList<string> Patterns)
{
    internal const string AllPattern = "*.*";
    internal const char FilterSeparator = '|';
    internal const char PatternSeparator = ';';
    internal static readonly char[] Wildcards = ['*', '?'];

    internal bool Matches(string name) => Patterns.Any(pattern =>
        FileSystemName.MatchesWin32Expression(FileSystemName.TranslateWin32Expression(pattern), name, ignoreCase: true));

    internal IEnumerable<string> Extensions => Patterns.Where(pattern => pattern.Contains('.'))
        .Select(pattern => pattern[(pattern.LastIndexOf('.') + 1)..]);

    internal static IReadOnlyList<FileSelectionFilter> Parse(string filter, string allFilesLabel)
    {
        if (string.IsNullOrWhiteSpace(filter)) return [new(allFilesLabel, [AllPattern])];
        var parts = filter.Split(FilterSeparator);
        if (parts.Length % 2 != 0) throw new ArgumentException(nameof(filter));
        var result = new List<FileSelectionFilter>();
        for (var index = 0; index < parts.Length; index += 2)
            result.Add(new(parts[index], parts[index + 1].Split(PatternSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)));
        return result;
    }
}
