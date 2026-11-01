using System.Windows;
using System.Windows.Controls;

namespace GWGUI.App.Contracts.Theming;

internal sealed record ThemeEditionDefinition(string Id, string NameResourceKey,
    IReadOnlyList<ThemeStyleDefinition> Styles, string DefaultStyle)
{
    internal Func<Grid, TabControl, IThemeWorkspace>? CreateWorkspace { get; init; }
    internal Action<FrameworkElement, string>? ApplyStyle { get; init; }
    internal Func<string, string>? PreviewUri { get; init; }
    internal string NormalizeStyle(string? style) => Styles.Any(choice => choice.Id == style)
        ? style! : DefaultStyle;
}
