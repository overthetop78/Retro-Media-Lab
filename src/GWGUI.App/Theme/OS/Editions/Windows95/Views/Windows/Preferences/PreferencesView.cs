using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using GWGUI.App.Localization.Extensions;
using GWGUI.App.Views.Controls.Options;

namespace GWGUI.App.Theme.OS.Editions.Windows95.Views;

internal sealed class PreferencesView : DockPanel, IDisposable
{
    private readonly TabControl navigation;
    private readonly TabControl tabs = new();
    private readonly Button closeButton = new() { MinWidth = 80, IsDefault = true };
    private readonly Action close;
    private readonly Dictionary<int, (FrameworkElement View, IDisposable Lifetime)> pages = [];
    private bool selecting;
    private bool disposed;
    private const string ControlsUri = "/gwgui.app;component/Theme/OS/Editions/Windows95/Styles/Windows95Controls.xaml";
    private static readonly string[] Titles =
    [
        "Options.General", "Options.Themes.Title", "Options.Logs", "Options.Hardware",
        "Options.Engines", "Options.Profiles", "Controllers.Title"
    ];

    internal PreferencesView(TabControl navigation, Action close)
    {
        this.navigation = navigation;
        this.close = close;
        try
        {
            Margin = new Thickness(8);
            Background = Brushes.Silver;
            TextElement.SetForeground(this, Brushes.Black);
            TextElement.SetFontFamily(this, new FontFamily(Windows95DesktopMetrics.FontFamily));
            TextElement.SetFontSize(this, Windows95Metrics.FontSize);
            Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(ControlsUri, UriKind.Relative) });
            var footer = new DockPanel { Margin = new Thickness(0, 8, 0, 0), LastChildFill = false };
            SetDock(footer, Dock.Bottom);
            closeButton.SetBinding(ContentControl.ContentProperty, LocExtension.CreateBinding("Common.Close"));
            closeButton.Click += CloseClicked;
            SetDock(closeButton, Dock.Right);
            footer.Children.Add(closeButton);
            Children.Add(footer);
            Children.Add(tabs);
            for (var index = 0; index < navigation.Items.Count; index++)
            {
                var tab = new TabItem();
                tab.SetBinding(HeaderedContentControl.HeaderProperty, LocExtension.CreateBinding(Titles[index]));
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
                    OptionsGeneralSection general => new GeneralOptionsView(general),
                    OptionsThemesSection themes => new ThemeOptionsView(themes),
                    OptionsLogsSection logs => new LogsOptionsView(logs),
                    OptionsHardwareSection hardware => new HardwareOptionsView(hardware),
                    OptionsEnginesSection engines => new EnginesOptionsView(engines),
                    OptionsProfilesSection profiles => new ProfilesOptionsView(profiles),
                    OptionsControllersSection controllers => new ControllersOptionsView(controllers),
                    _ => throw new InvalidOperationException(nameof(source))
                };
                entry = (view, (IDisposable)view);
                pages.Add(index, entry);
            }
            ((TabItem)tabs.Items[index]).Content = entry.View;
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
            foreach (TabItem item in tabs.Items) item.Content = null;
            tabs.Items.Clear();
            pages.Clear();
            Children.Clear();
            Resources.Clear();
        }
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
