using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace RetroMediaLab.App.Theme.OS.Editions.WindowsXP;

/// <summary>Owns an XP resource scope; removing it restores the caller's dictionaries unchanged.</summary>
internal sealed class WindowsXPResources : IDisposable
{
    public const string SchemeResource = "WindowsXP.ColorScheme";
    private const string LunaPrefix = "/PresentationFramework.Luna;component/themes/Luna.";
    private const string LunaSuffix = ".xaml";
    private const string BluePalette = "NormalColor";
    private const string SilverPalette = "Metallic";
    private const string OlivePalette = "Homestead";
    private const string ControlsUri = "/retromedialab.app;component/Theme/OS/Editions/WindowsXP/Styles/WindowsXPControls.xaml";
    public const string FaceResource = "WindowsXP.Face";
    public const string CaptionFontFamilyResource = "WindowsXP.CaptionFontFamily";
    public const string CaptionFontSizeResource = "WindowsXP.CaptionFontSize";
    public const string CaptionFontWeightResource = "WindowsXP.CaptionFontWeight";
    public const string CaptionTextResource = "WindowsXP.CaptionText";
    public const string InactiveCaptionTextResource = "WindowsXP.InactiveCaptionText";
    public const string CaptionShadowResource = "WindowsXP.CaptionShadow";
    public const string CaptionRowHeightResource = "WindowsXP.CaptionRowHeight";
    public const string CaptionHeightResource = "WindowsXP.CaptionHeight";
    private const string SelectionResource = "WindowsXP.Selection";
    private readonly FrameworkElement target;
    private readonly ResourceDictionary resources = new();
    private bool disposed;
    private static readonly Type[] ControlTypes =
    [
        typeof(Button), typeof(ToggleButton), typeof(CheckBox), typeof(RadioButton),
        typeof(TextBox), typeof(ComboBox), typeof(ComboBoxItem), typeof(ListBox), typeof(ListBoxItem),
        typeof(ListView), typeof(ListViewItem), typeof(GridViewColumnHeader), typeof(TreeView), typeof(TreeViewItem),
        typeof(TabControl), typeof(TabItem), typeof(GroupBox), typeof(Slider), typeof(ScrollBar), typeof(ScrollViewer),
        typeof(ProgressBar), typeof(Menu), typeof(MenuItem), typeof(ContextMenu), typeof(ToolTip)
    ];

    private static readonly DependencyProperty ScopeProperty = DependencyProperty.RegisterAttached(
        "Scope", typeof(WindowsXPResources), typeof(WindowsXPResources));
    internal static readonly DependencyProperty SchemeProperty = DependencyProperty.RegisterAttached(
        "Scheme", typeof(WindowsXPColorScheme?), typeof(WindowsXPResources),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits, InheritedSchemeChanged));
    private WindowsXPColorScheme? currentScheme;

    internal static WindowsXPColorScheme CurrentScheme(FrameworkElement target) =>
        (WindowsXPColorScheme?)target.GetValue(SchemeProperty) ?? WindowsXPColorScheme.Blue;

    internal WindowsXPResources(FrameworkElement target, WindowsXPColorScheme scheme)
    {
        target.VerifyAccess();
        this.target = target;
        try
        {
            if (target.GetValue(ScopeProperty) is not null)
                throw new InvalidOperationException("An XP resource scope already belongs to this element.");
            target.Resources.MergedDictionaries.Add(resources);
            target.SetValue(ScopeProperty, this);
            target.Loaded += TargetLoaded;
            Update((WindowsXPColorScheme?)target.GetValue(SchemeProperty) ?? scheme);
        }
        catch { Dispose(); throw; }
    }

    /// <summary>Inherited by logical children, including pages in inactive tabs.</summary>
    internal static void ApplyScheme(FrameworkElement root, WindowsXPColorScheme scheme)
    {
        root.VerifyAccess();
        if (!Enum.IsDefined(scheme)) throw new ArgumentOutOfRangeException(nameof(scheme));
        root.SetValue(SchemeProperty, scheme);
    }

    private static void InheritedSchemeChanged(DependencyObject target, DependencyPropertyChangedEventArgs args)
    {
        if (args.NewValue is WindowsXPColorScheme scheme && target.GetValue(ScopeProperty) is WindowsXPResources scope)
            scope.Update(scheme);
    }

    private void TargetLoaded(object sender, RoutedEventArgs args)
    {
        if (!disposed && target.GetValue(SchemeProperty) is WindowsXPColorScheme scheme) Update(scheme);
    }

    private void Update(WindowsXPColorScheme scheme)
    {
        if (disposed || currentScheme == scheme) return;
        var replacement = CreateResources(scheme);
        resources.MergedDictionaries.Clear();
        resources.MergedDictionaries.Add(replacement);
        currentScheme = scheme;
    }

    private static ResourceDictionary CreateResources(WindowsXPColorScheme scheme)
    {
        var resources = new ResourceDictionary();
        var palette = scheme switch
        {
            WindowsXPColorScheme.Blue => BluePalette,
            WindowsXPColorScheme.Silver => SilverPalette,
            WindowsXPColorScheme.Olive => OlivePalette,
            _ => throw new ArgumentOutOfRangeException(nameof(scheme))
        };
        var luna = new ResourceDictionary { Source = new Uri(LunaPrefix + palette + LunaSuffix, UriKind.Relative) };
        var adjustments = new ResourceDictionary { Source = new Uri(ControlsUri, UriKind.Relative) };
        resources.MergedDictionaries.Add(luna);
        resources.MergedDictionaries.Add(adjustments);
        foreach (var type in ControlTypes)
        {
            var original = luna[type] as Style;
            var style = original is null ? new Style(type) : new Style(type, original);
            if (adjustments[type] is Style custom)
                foreach (Setter setter in Setters(custom))
                    style.Setters.Add(new Setter(setter.Property, setter.Value));
            if (type == typeof(TextBox))
            {
                var multiline = new Trigger { Property = TextBox.AcceptsReturnProperty, Value = true };
                multiline.Setters.Add(new Setter(Control.VerticalContentAlignmentProperty, VerticalAlignment.Top));
                style.Triggers.Add(multiline);
            }
            resources[type] = style;
        }
        var face = WindowsXPSystemMetrics.Face(scheme);
        var selection = WindowsXPSystemMetrics.Selection(scheme);
        var caption = WindowsXPSystemMetrics.CaptionFont(scheme);
        resources[CaptionFontFamilyResource] = caption.Family;
        resources[CaptionFontSizeResource] = caption.Size;
        resources[CaptionFontWeightResource] = caption.Weight;
        resources[CaptionTextResource] = WindowsXPSystemMetrics.CaptionText(scheme);
        resources[InactiveCaptionTextResource] = WindowsXPSystemMetrics.InactiveCaptionText(scheme);
        resources[CaptionShadowResource] = WindowsXPSystemMetrics.CaptionShadow(scheme).Color;
        var captionHeight = WindowsXPImageCatalog.Definition(scheme, WindowsXPImagePart.FrameCaption).FrameHeight;
        resources[CaptionHeightResource] = (double)captionHeight;
        resources[CaptionRowHeightResource] = new GridLength(captionHeight);
        resources[SchemeResource] = scheme;
        resources[FaceResource] = face;
        resources[SelectionResource] = selection;
        resources[SystemColors.ControlBrushKey] = face;
        resources[SystemColors.ControlDarkBrushKey] = WindowsXPSystemMetrics.ControlShadow(scheme);
        resources[SystemColors.ControlLightLightBrushKey] = WindowsXPSystemMetrics.ControlHighlight(scheme);
        resources[SystemColors.ControlTextBrushKey] = Brushes.Black;
        resources[SystemColors.WindowBrushKey] = Brushes.White;
        resources[SystemColors.WindowTextBrushKey] = Brushes.Black;
        resources[SystemColors.HighlightBrushKey] = selection;
        resources[SystemColors.HighlightTextBrushKey] = Brushes.White;
        return resources;
    }

    private static IEnumerable<Setter> Setters(Style style)
    {
        if (style.BasedOn is not null)
            foreach (var inherited in Setters(style.BasedOn)) yield return inherited;
        foreach (Setter setter in style.Setters) yield return setter;
    }

    public void Dispose()
    {
        if (disposed) return;
        target.VerifyAccess();
        disposed = true;
        try
        {
            target.Loaded -= TargetLoaded;
            if (ReferenceEquals(target.GetValue(ScopeProperty), this)) target.ClearValue(ScopeProperty);
            target.Resources.MergedDictionaries.Remove(resources);
        }
        finally
        {
            resources.MergedDictionaries.Clear();
            resources.Clear();
        }
    }
}
