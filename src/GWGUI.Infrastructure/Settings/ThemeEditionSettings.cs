namespace GWGUI.Infrastructure.Settings;

public sealed class ThemeEditionSettings
{
    public string Edition { get; set; } = string.Empty;
    public Dictionary<string, string> Styles { get; set; } = [];

    public ThemeEditionSettings Copy() => new()
    {
        Edition = Edition,
        Styles = new Dictionary<string, string>(Styles)
    };

    public bool HasSameChoices(ThemeEditionSettings other) => Edition == other.Edition
        && Styles.Count == other.Styles.Count
        && Styles.All(pair => other.Styles.TryGetValue(pair.Key, out var value) && value == pair.Value);
}
