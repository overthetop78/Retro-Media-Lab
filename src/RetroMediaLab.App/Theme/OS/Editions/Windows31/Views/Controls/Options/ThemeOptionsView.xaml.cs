using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.App.Views.Controls.Options;

namespace RetroMediaLab.App.Theme.OS.Editions.Windows31.Views;

internal partial class ThemeOptionsView : UserControl, IDisposable
{
    private readonly OptionsThemesSection source;
    private readonly ThemeBindingScope bindings = new();
    private ThemeBindingScope? variantBindings;
    private bool disposed;
    private static readonly Thickness VariantMargin = new(0, 0, 12, 0);

    internal ThemeOptionsView(OptionsThemesSection source)
    {
        this.source = source;
        try
        {
            InitializeComponent();
            Choice(source.Themes, ThemeChoices);
            Choice(source.EditionSelector, EditionChoices);
            Choice(source.EditionStyleSelector, EditionAppearance);
            foreach (var target in new[] { EditionHeading, AppearanceHeading, Description, ActiveTheme })
            {
                var original = Original<TextBlock>(target.Name);
                bindings.OneWay(original, TextBlock.TextProperty, target, TextBlock.TextProperty);
                bindings.OneWay(original, VisibilityProperty, target, VisibilityProperty);
            }
            bindings.OneWay(Original<Image>(nameof(Preview)), Image.SourceProperty, Preview, Image.SourceProperty);
            bindings.OneWay(Original<TextBlock>(nameof(PreviewUnavailable)), VisibilityProperty, PreviewUnavailable, VisibilityProperty);
            bindings.OneWay(Original<StackPanel>(nameof(Variants)), VisibilityProperty, Variants, VisibilityProperty);
            bindings.OneWay(Original<StackPanel>(nameof(Variants)), IsEnabledProperty, Variants, IsEnabledProperty);
            bindings.Action(source.SaveAction, ChooseAction, followCaption: true);
            source.PresentationChanged += SourcePresentationChanged;
            RebuildVariants();
        }
        catch { Dispose(); throw; }
    }

    private T Original<T>(string name) where T : FrameworkElement => (T)source.FindName(name);

    private void Choice(Selector original, ComboBox target)
    {
        bindings.OneWay(original, ItemsControl.ItemsSourceProperty, target, ItemsControl.ItemsSourceProperty);
        if (original is ComboBox)
            bindings.OneWay(original, ItemsControl.DisplayMemberPathProperty, target, ItemsControl.DisplayMemberPathProperty);
        bindings.TwoWay(original, Selector.SelectedItemProperty, target, Selector.SelectedItemProperty);
        bindings.OneWay(original, IsEnabledProperty, target, IsEnabledProperty);
        bindings.OneWay(original, VisibilityProperty, target, VisibilityProperty);
    }

    private void SourcePresentationChanged(object? sender, EventArgs args) { if (!disposed) RebuildVariants(); }

    private void RebuildVariants()
    {
        variantBindings?.Dispose();
        variantBindings = new ThemeBindingScope();
        Variants.Children.Clear();
        foreach (var original in source.AppearanceOptions)
        {
            var option = new RadioButton { Margin = VariantMargin,
                GroupName = nameof(ThemeOptionsView) + GetHashCode().ToString(CultureInfo.InvariantCulture) };
            variantBindings.OneWay(original, ContentControl.ContentProperty, option, ContentControl.ContentProperty);
            variantBindings.TwoWay(original, ToggleButton.IsCheckedProperty, option, ToggleButton.IsCheckedProperty);
            Variants.Children.Add(option);
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        source.PresentationChanged -= SourcePresentationChanged;
        try { variantBindings?.Dispose(); }
        finally
        {
            try { bindings.Dispose(); }
            finally
            {
                if (Variants is not null) Variants.Children.Clear();
                Content = null;
            }
        }
    }
}
