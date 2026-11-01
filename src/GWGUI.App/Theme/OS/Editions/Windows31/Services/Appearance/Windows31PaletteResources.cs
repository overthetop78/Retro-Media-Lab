using System.Windows;

namespace GWGUI.App.Theme.OS.Editions.Windows31;

internal static class Windows31PaletteResources
{
    private const string Prefix = "Windows31.Color.";
    public const string Desktop = Prefix + nameof(Windows31ColorRole.Desktop);
    public const string ApplicationWorkspace = Prefix + nameof(Windows31ColorRole.ApplicationWorkspace);
    public const string Window = Prefix + nameof(Windows31ColorRole.Window);
    public const string WindowText = Prefix + nameof(Windows31ColorRole.WindowText);
    public const string Menu = Prefix + nameof(Windows31ColorRole.Menu);
    public const string MenuText = Prefix + nameof(Windows31ColorRole.MenuText);
    public const string ActiveCaption = Prefix + nameof(Windows31ColorRole.ActiveCaption);
    public const string InactiveCaption = Prefix + nameof(Windows31ColorRole.InactiveCaption);
    public const string CaptionText = Prefix + nameof(Windows31ColorRole.CaptionText);
    public const string ActiveBorder = Prefix + nameof(Windows31ColorRole.ActiveBorder);
    public const string InactiveBorder = Prefix + nameof(Windows31ColorRole.InactiveBorder);
    public const string WindowFrame = Prefix + nameof(Windows31ColorRole.WindowFrame);
    public const string ScrollBar = Prefix + nameof(Windows31ColorRole.ScrollBar);
    public const string ButtonFace = Prefix + nameof(Windows31ColorRole.ButtonFace);
    public const string ButtonShadow = Prefix + nameof(Windows31ColorRole.ButtonShadow);
    public const string ButtonText = Prefix + nameof(Windows31ColorRole.ButtonText);
    public const string GrayText = Prefix + nameof(Windows31ColorRole.GrayText);
    public const string Selection = Prefix + nameof(Windows31ColorRole.Selection);
    public const string SelectionText = Prefix + nameof(Windows31ColorRole.SelectionText);
    public const string InactiveCaptionText = Prefix + nameof(Windows31ColorRole.InactiveCaptionText);
    public const string ButtonHighlight = Prefix + nameof(Windows31ColorRole.ButtonHighlight);

    internal static readonly DependencyProperty ColorSchemeProperty = DependencyProperty.RegisterAttached(
        "ColorScheme", typeof(Windows31ColorScheme), typeof(Windows31PaletteResources),
        new FrameworkPropertyMetadata(Windows31ColorScheme.WindowsDefault, FrameworkPropertyMetadataOptions.Inherits,
            (target, _) =>
            {
                if (target is Windows31Frame or Windows31Desktop or Windows31ProgramManager) Initialize((FrameworkElement)target);
            }));

    internal static Windows31ColorScheme GetScheme(DependencyObject target)
        => (Windows31ColorScheme)target.GetValue(ColorSchemeProperty);

    internal static void Apply(FrameworkElement target, Windows31ColorScheme scheme)
    {
        var palette = Windows31PaletteCatalog.Get(scheme);
        target.SetCurrentValue(ColorSchemeProperty, palette.Id);
        Initialize(target);
    }

    internal static void Initialize(FrameworkElement target)
    {
        var palette = Windows31PaletteCatalog.Get(GetScheme(target));
        foreach (var role in Enum.GetValues<Windows31ColorRole>()) target.Resources[Key(role)] = palette[role];
    }

    internal static string Key(Windows31ColorRole role) => Prefix + role;
}
