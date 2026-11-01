using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GWGUI.App.Theme.OS.Editions.Windows31;

internal static class Windows31PaletteCatalog
{
    private const string DataResource = "GWGUI.App.Theme.OS.Windows31.ColorSchemes.json";
    private const string NamePrefix = "Options.Themes.OS.Palette.";
    private const string DefaultNameResource = "Profile.Default";
    internal static IReadOnlyList<Windows31Palette> All { get; } = Load();

    internal static Windows31Palette Get(Windows31ColorScheme scheme)
        => All.FirstOrDefault(palette => palette.Id == scheme) ?? All.First(palette => palette.Id == Windows31ColorScheme.WindowsDefault);

    private static IReadOnlyList<Windows31Palette> Load()
    {
        using Stream stream = typeof(Windows31PaletteCatalog).Assembly.GetManifestResourceStream(DataResource)
            ?? throw new InvalidDataException(DataResource);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new JsonStringEnumConverter<Windows31ColorRole>());
        var file = JsonSerializer.Deserialize<PaletteFile>(stream, options) ?? throw new InvalidDataException(DataResource);
        var roles = Enum.GetValues<Windows31ColorRole>();
        var palettes = file.Schemes.Select(definition =>
        {
            if (!Enum.TryParse<Windows31ColorScheme>(definition.Id, out var id) || !Enum.IsDefined(id)
                || definition.Colors.Count != roles.Length || roles.Any(role => !definition.Colors.ContainsKey(role)))
                throw new InvalidDataException(definition.Id);
            return new Windows31Palette(id, id == Windows31ColorScheme.WindowsDefault ? DefaultNameResource : NamePrefix + id,
                definition.Colors.ToDictionary(pair => pair.Key, pair => uint.Parse(pair.Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture)));
        }).ToArray();
        if (palettes.Select(palette => palette.Id).Distinct().Count() != Enum.GetValues<Windows31ColorScheme>().Length)
            throw new InvalidDataException(DataResource);
        return Array.AsReadOnly(palettes);
    }

    private sealed record PaletteFile(PaletteDefinition[] Schemes);
    private sealed record PaletteDefinition(string Id, Dictionary<Windows31ColorRole, string> Colors);
}
