using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using GWGUI.App.Localization.Extensions;
using GWGUI.App.Views.Controls.Options;

namespace GWGUI.App.Theme.OS.Editions.Windows31.Views;

/// <summary>Reconstructs the control panel over retained functional sections.</summary>
internal sealed class PreferencesView : DockPanel, IDisposable
{
    private readonly TabControl navigation;
    private readonly ContentControl page = new();
    private readonly Dictionary<int, (FrameworkElement View, IDisposable Lifetime)> pages = [];
    private readonly Dictionary<int, MenuItem> commands = [];
    private bool disposed;
    private const string MenuResource = "Menu.Options";
    private const string CloseResource = "Common.Close";
    private static readonly Thickness PageMargin = new(8);
    private static readonly string[] Titles =
    [
        "Options.General", "Options.Themes.Title", "Options.Logs", "Options.Hardware",
        "Options.Engines", "Options.Profiles", "Controllers.Title"
    ];

    internal PreferencesView(TabControl navigation, Action close)
    {
        this.navigation = navigation;
        try
        {
            var menu = new Menu();
            SetDock(menu, Dock.Top);
            var options = new MenuItem();
            options.SetBinding(HeaderedItemsControl.HeaderProperty, LocExtension.CreateBinding(MenuResource));
            for (var index = 0; index < navigation.Items.Count; index++)
            {
                var item = new MenuItem { Tag = index, IsCheckable = true };
                item.SetBinding(HeaderedItemsControl.HeaderProperty, LocExtension.CreateBinding(Titles[index]));
                item.Click += SelectPage;
                commands.Add(index, item);
                options.Items.Add(item);
            }
            menu.Items.Add(options);
            Children.Add(menu);
            var footer = new DockPanel { Margin = PageMargin, LastChildFill = false };
            SetDock(footer, Dock.Bottom);
            var closeButton = new Button { IsDefault = true };
            closeButton.SetBinding(ContentControl.ContentProperty, LocExtension.CreateBinding(CloseResource));
            closeButton.Click += (_, _) => close();
            SetDock(closeButton, Dock.Right);
            footer.Children.Add(closeButton);
            Children.Add(footer);
            page.Margin = PageMargin;
            Children.Add(page);
            navigation.SelectionChanged += SelectionChanged;
            ShowSelectedPage();
        }
        catch { Dispose(); throw; }
    }

    private void SelectPage(object sender, RoutedEventArgs args)
    {
        if (!disposed && sender is MenuItem { Tag: int index }) navigation.SelectedIndex = index;
    }

    private void SelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        if (!disposed && ReferenceEquals(args.OriginalSource, navigation)) ShowSelectedPage();
    }

    private void ShowSelectedPage()
    {
        var index = navigation.SelectedIndex;
        if (index < 0) { page.Content = null; return; }
        if (!pages.TryGetValue(index, out var entry))
        {
            var source = ((TabItem)navigation.Items[index]).Content;
            var view = source switch
            {
                OptionsGeneralSection general => (FrameworkElement)new GeneralOptionsView(general),
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
        page.Content = entry.View;
        foreach (var pair in commands) pair.Value.IsChecked = pair.Key == index;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        navigation.SelectionChanged -= SelectionChanged;
        Exception? failure = null;
        try
        {
            foreach (var item in pages.Values)
                try { item.Lifetime.Dispose(); }
                catch (Exception error) { failure ??= error; }
        }
        finally
        {
            foreach (var item in commands.Values) item.Click -= SelectPage;
            commands.Clear();
            pages.Clear();
            page.Content = null;
            Children.Clear();
        }
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
