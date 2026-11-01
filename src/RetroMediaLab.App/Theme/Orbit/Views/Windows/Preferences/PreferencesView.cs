using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Views.Controls.Options;

namespace RetroMediaLab.App.Theme.Orbit.Views;

internal sealed class PreferencesView : DockPanel, IDisposable
{
    private readonly TabControl navigation;
    private readonly ListBox tabs = new() { Width = NavigationWidth };
    private readonly ContentPresenter document = new();
    private const double NavigationWidth = 210;
    private readonly Button closeButton = new() { MinWidth = 80, IsDefault = true };
    private readonly Action close;
    private readonly Dictionary<int, (FrameworkElement View, IDisposable Lifetime)> pages = [];
    private bool selecting;
    private bool disposed;
    private OrbitResources? resourceScope;
    private readonly bool? dark;
    private static readonly string[] Titles =
    [
        "Options.General", "Options.Themes.Title", "Options.Logs", "Options.Hardware",
        "Options.Engines", "Options.Profiles", "Controllers.Title"
    ];

    internal PreferencesView(TabControl navigation, Action close, bool? dark = null)
    {
        this.navigation = navigation;
        this.close = close;
        this.dark = dark;
        try
        {
            resourceScope = new OrbitResources(this, dark);
            Margin = new Thickness(12);
            SetResourceReference(BackgroundProperty, OrbitMetrics.WindowBrush);
            var footer = new DockPanel { Margin = new Thickness(0, 8, 0, 0), LastChildFill = false };
            SetDock(footer, Dock.Bottom);
            closeButton.SetBinding(ContentControl.ContentProperty, LocExtension.CreateBinding("Common.Close"));
            closeButton.Click += CloseClicked;
            SetDock(closeButton, Dock.Right);
            footer.Children.Add(closeButton);
            Children.Add(footer);
            SetDock(tabs, Dock.Left);
            tabs.Margin = new Thickness(0, 0, 24, 0);
            Children.Add(tabs);
            Children.Add(document);
            for (var index = 0; index < navigation.Items.Count; index++)
            {
                var tab = new ListBoxItem();
                tab.SetBinding(ContentControl.ContentProperty, LocExtension.CreateBinding(Titles[index]));
                tabs.Items.Add(tab);
            }
            navigation.SelectionChanged += SourceSelectionChanged;
            tabs.SelectionChanged += PageSelectionChanged;
            ShowSelectedPage();
        }
        catch { Dispose(); throw; }
    }

    private void CloseClicked(object sender, RoutedEventArgs args) { if (!disposed) close(); }
    private void SourceSelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        if (!disposed && ReferenceEquals(args.OriginalSource, navigation)) ShowSelectedPage();
    }
    private void PageSelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        if (disposed || selecting || !ReferenceEquals(args.OriginalSource, tabs)) return;
        navigation.SelectedIndex = tabs.SelectedIndex;
        ShowSelectedPage();
    }
    private void ShowSelectedPage()
    {
        if (selecting) return;
        selecting = true;
        try
        {
            var index = navigation.SelectedIndex;
            tabs.SelectedIndex = index;
            if (index < 0) return;
            if (!pages.TryGetValue(index, out var entry))
            {
                var source = ((TabItem)navigation.Items[index]).Content;
                FrameworkElement view = source switch
                {
                    OptionsGeneralSection general => new GeneralOptionsView(general, dark),
                    OptionsThemesSection themes => new ThemeOptionsView(themes, dark),
                    OptionsLogsSection logs => new LogsOptionsView(logs, dark),
                    OptionsHardwareSection hardware => new HardwareOptionsView(hardware, dark),
                    OptionsEnginesSection engines => new EnginesOptionsView(engines, dark),
                    OptionsProfilesSection profiles => new ProfilesOptionsView(profiles, dark),
                    OptionsControllersSection controllers => new ControllersOptionsView(controllers, dark),
                    _ => throw new InvalidOperationException(nameof(source))
                };
                entry = (view, (IDisposable)view);
                pages.Add(index, entry);
            }
            document.Content = entry.View;
        }
        finally { selecting = false; }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        navigation.SelectionChanged -= SourceSelectionChanged;
        tabs.SelectionChanged -= PageSelectionChanged;
        closeButton.Click -= CloseClicked;
        Exception? failure = null;
        try
        {
            foreach (var item in pages.Values)
                try { item.Lifetime.Dispose(); }
                catch (Exception error) { failure ??= error; }
        }
        finally
        {
            document.Content = null;
            foreach (ListBoxItem item in tabs.Items) item.Content = null;
            tabs.Items.Clear();
            pages.Clear();
            Children.Clear();
            try { resourceScope?.Dispose(); }
            finally { resourceScope = null; Resources.Clear(); }
        }
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
