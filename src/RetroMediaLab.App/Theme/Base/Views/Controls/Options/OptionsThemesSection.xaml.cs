using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using RetroMediaLab.App.Contracts.Theming;
using RetroMediaLab.App.Dictionaries.Theming;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.Infrastructure.Settings;

namespace RetroMediaLab.App.Views.Controls.Options;

public partial class OptionsThemesSection : UserControl
{
    private const string ChooseResource = "Common.Choose";
    private static readonly char[] ChoicePunctuation = ['…', '.'];
    private const string ActiveResource = "Options.Themes.Active";
    private const string SystemResource = "Options.Theme.System";
    private const string DarkResource = "Options.Theme.Dark";
    private const string LightResource = "Options.Theme.Light";
    private const string AppearanceSeparator = " / ";
    private const string VariantGroup = "ThemeAppearance";
    private static readonly Thickness VariantMargin = new(0, 0, 16, 0);
    private Func<AppThemeDesign, AppTheme, ThemeEditionSettings, Task<bool>>? _save;
    private IReadOnlyList<ThemeDefinition> _definitions = ThemeCatalog.All;
    private Dictionary<AppThemeDesign, ThemeEditionSettings> _editionSettings = [];
    private ThemeEditionSettings _currentEdition = new();
    private bool _refreshing;
    private AppThemeDesign _current;
    private AppTheme _currentVariant;
    private AppTheme _variant;
    private bool _saving;

    public OptionsThemesSection()
    {
        InitializeComponent();
        RefreshLocalizedContent();
    }

    internal ListBox Themes => ThemeList;
    internal Button SaveAction => SaveTheme;
    internal AppThemeDesign SelectedTheme => (ThemeList.SelectedItem as Choice)?.Definition.Id ?? _current;
    internal ComboBox EditionSelector => EditionChoices;
    internal ComboBox EditionStyleSelector => EditionAppearance;
    internal IReadOnlyList<RadioButton> AppearanceOptions => Variants.Children.OfType<RadioButton>().ToArray();
    internal event EventHandler? PresentationChanged;

    private ThemeDefinition Definition(AppThemeDesign id) => _definitions.FirstOrDefault(item => item.Id == id) ?? _definitions[0];
    private ThemeEditionSettings Choices(AppThemeDesign id)
    {
        if (!_editionSettings.TryGetValue(id, out var settings)) _editionSettings[id] = settings = new();
        if (Definition(id).FindEdition(settings.Edition) is { } edition)
        {
            settings.Edition = edition.Id;
            settings.Styles.TryGetValue(edition.Id, out var style);
            settings.Styles[edition.Id] = edition.NormalizeStyle(style);
        }
        return settings;
    }

    internal void Configure(AppThemeDesign current, AppTheme variant, Func<AppThemeDesign, AppTheme, Task<bool>> save)
        => Configure(current, variant, new Dictionary<AppThemeDesign, ThemeEditionSettings>(),
            (design, appearance, _) => save(design, appearance));

    internal void Configure(AppThemeDesign current, AppTheme variant,
        IReadOnlyDictionary<AppThemeDesign, ThemeEditionSettings> editions,
        Func<AppThemeDesign, AppTheme, ThemeEditionSettings, Task<bool>> save,
        IReadOnlyList<ThemeDefinition>? definitions = null)
    {
        _definitions = definitions ?? ThemeCatalog.All;
        _editionSettings = editions.ToDictionary(pair => pair.Key, pair => pair.Value.Copy());
        _current = Definition(current).Id;
        _currentVariant = Definition(_current).Normalize(variant);
        _currentEdition = Choices(_current).Copy();
        _variant = _currentVariant;
        _save = save;
        RefreshLocalizedContent();
        ThemeList.SelectedItem = ThemeList.Items.Cast<Choice>().First(choice => choice.Definition.Id == _current);
        RefreshSelection();
    }

    internal void RefreshLocalizedContent()
    {
        SaveTheme.Content = LocExtension.Get(ChooseResource).TrimEnd(ChoicePunctuation);
        var selected = SelectedTheme;
        ThemeList.ItemsSource = _definitions.Select(definition => new Choice(definition,
            LocExtension.Get(definition.NameResourceKey), string.Join(AppearanceSeparator,
                definition.Editions.Count > 0 ? definition.Editions.Select(edition => LocExtension.Get(edition.NameResourceKey))
                    : definition.Variants.Select(VariantName)))).ToArray();
        ThemeList.SelectedItem = ThemeList.Items.Cast<Choice>().FirstOrDefault(choice => choice.Definition.Id == selected)
            ?? ThemeList.Items.Cast<Choice>().FirstOrDefault();
        RefreshSelection();
    }

    private void SelectionChanged(object sender, SelectionChangedEventArgs args) => RefreshSelection();

    private void RefreshSelection()
    {
        if (ThemeList.SelectedItem is not Choice choice) return;
        ThemeName.Text = choice.Name;
        Description.Text = LocExtension.Get(choice.Definition.DescriptionResourceKey);
        ActiveTheme.Text = LocExtension.Get(ActiveResource, ActiveName());
        _variant = choice.Definition.Normalize(_variant);
        Variants.Children.Clear();
        _refreshing = true;
        try
        {
            var hasEditions = choice.Definition.Editions.Count > 0;
            EditionHeading.Text = LocExtension.Get(choice.Definition.EditionHeadingResourceKey);
            EditionHeading.Visibility = EditionChoices.Visibility = hasEditions ? Visibility.Visible : Visibility.Collapsed;
            EditionChoices.ItemsSource = choice.Definition.Editions.Select(edition => new NamedChoice(edition.Id, LocExtension.Get(edition.NameResourceKey))).ToArray();
            var settings = Choices(choice.Definition.Id);
            EditionChoices.SelectedItem = EditionChoices.Items.Cast<NamedChoice>().FirstOrDefault(item => item.Id == settings.Edition);
            var edition = choice.Definition.FindEdition(settings.Edition);
            EditionAppearance.ItemsSource = edition?.Styles.Select(style => new NamedChoice(style.Id, LocExtension.Get(style.NameResourceKey))).ToArray();
            EditionAppearance.SelectedItem = EditionAppearance.Items.Cast<NamedChoice>().FirstOrDefault(item =>
                edition is not null && settings.Styles.TryGetValue(edition.Id, out var style) && style == item.Id);
            EditionAppearance.Visibility = edition?.Styles.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
            Variants.Visibility = !hasEditions && choice.Definition.Variants.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
            AppearanceHeading.Visibility = hasEditions ? EditionAppearance.Visibility : Variants.Visibility;
        }
        finally { _refreshing = false; }
        foreach (var variant in choice.Definition.Variants)
        {
            var option = new RadioButton { Content = VariantName(variant), Tag = variant, GroupName = VariantGroup, IsChecked = variant == _variant, Margin = VariantMargin };
            option.Checked += (_, _) => { _variant = variant; RefreshPreview(choice.Definition); };
            Variants.Children.Add(option);
        }
        RefreshPreview(choice.Definition);
        PresentationChanged?.Invoke(this, EventArgs.Empty);
    }

    private string ActiveName()
    {
        var definition = Definition(_current);
        var edition = definition.FindEdition(_currentEdition.Edition);
        if (edition is null) return string.Join(AppearanceSeparator, LocExtension.Get(definition.NameResourceKey), VariantName(_currentVariant));
        _currentEdition.Styles.TryGetValue(edition.Id, out var selected);
        var style = edition.Styles.First(item => item.Id == edition.NormalizeStyle(selected));
        return string.Join(AppearanceSeparator, LocExtension.Get(definition.NameResourceKey),
            LocExtension.Get(edition.NameResourceKey), LocExtension.Get(style.NameResourceKey));
    }

    private void EditionChanged(object sender, SelectionChangedEventArgs args)
    {
        if (_refreshing || EditionChoices.SelectedItem is not NamedChoice edition) return;
        Choices(SelectedTheme).Edition = edition.Id;
        RefreshSelection();
    }

    private void EditionStyleChanged(object sender, SelectionChangedEventArgs args)
    {
        if (_refreshing || EditionAppearance.SelectedItem is not NamedChoice style) return;
        var settings = Choices(SelectedTheme);
        settings.Styles[settings.Edition] = style.Id;
        RefreshPreview(Definition(SelectedTheme));
    }

    private static string VariantName(AppTheme variant) => LocExtension.Get(variant == AppTheme.System ? SystemResource : variant == AppTheme.Dark ? DarkResource : LightResource);

    private void RefreshPreview(ThemeDefinition definition)
    {
        SaveTheme.IsEnabled = !_saving && _save is not null && HasChanges(definition.Id);
        Preview.Source = null;
        try
        {
            var settings = Choices(definition.Id);
            var edition = definition.FindEdition(settings.Edition);
            var preview = edition?.PreviewUri is { } editionPreview
                ? editionPreview(edition.NormalizeStyle(settings.Styles.GetValueOrDefault(edition.Id)))
                : definition.PreviewUri(_variant == AppTheme.System && RetroMediaLab.App.Services.Theming.ThemeManager.SystemUsesDarkTheme() ? AppTheme.Dark : _variant);
            var resource = Application.GetResourceStream(new Uri(preview, UriKind.Relative));
            if (resource is not null)
            {
                using var stream = resource.Stream;
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze();
                Preview.Source = bitmap;
            }
        }
        catch (IOException) { }
        PreviewUnavailable.Visibility = Preview.Source is null ? Visibility.Visible : Visibility.Collapsed;
    }

    private bool HasChanges(AppThemeDesign id) => id != _current || _variant != _currentVariant
        || !Choices(id).HasSameChoices(_currentEdition);

    internal async Task SaveSelectedAsync()
    {
        if (_saving || _save is null || !HasChanges(SelectedTheme)) return;
        var selected = SelectedTheme;
        var variant = _variant;
        var edition = Choices(selected).Copy();
        _saving = true;
        ThemeList.IsEnabled = false;
        Variants.IsEnabled = false;
        EditionChoices.IsEnabled = EditionAppearance.IsEnabled = false;
        SaveTheme.IsEnabled = false;
        try { if (await _save(selected, variant, edition)) { _current = selected; _currentVariant = variant; _currentEdition = edition.Copy(); } }
        finally
        {
            _saving = false;
            ThemeList.IsEnabled = true;
            Variants.IsEnabled = true;
            EditionChoices.IsEnabled = EditionAppearance.IsEnabled = true;
            RefreshSelection();
        }
    }

    private async void SaveClicked(object sender, RoutedEventArgs args) => await SaveSelectedAsync();
    private sealed record Choice(ThemeDefinition Definition, string Name, string Appearance);
    private sealed record NamedChoice(string Id, string Name);
}
