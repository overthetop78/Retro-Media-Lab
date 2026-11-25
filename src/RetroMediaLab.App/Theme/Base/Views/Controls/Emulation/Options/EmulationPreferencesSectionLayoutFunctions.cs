using RetroMediaLab.App.Constants.Localization;
using RetroMediaLab.App.Functions.Views.Emulation.Settings;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Views.Controls.Common;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;


namespace RetroMediaLab.App.Views.Controls.Emulation.Options;

public sealed partial class EmulationPreferencesSection
{
    private ScrollViewer BuildGeneralTab()
    {
        var form = EmulationSettingsLayout.CompactForm(1,
            (LocExtension.Get("Emulation.Folder.StorageBase"), FolderControl(_storageFolder)),
            (LocExtension.Get("Emulation.Folder.Capture"), FolderControl(_captureFolder)),
            (LocExtension.Get("Emulation.Folder.State"), FolderControl(_stateFolder)),
            (LocExtension.Get(EmulationResourceKeys.OsdTransparency), OsdTransparencyControl()));
        BindFormLabel(form, 0, "Emulation.Folder.StorageBase");
        BindFormLabel(form, 1, "Emulation.Folder.Capture");
        BindFormLabel(form, 2, "Emulation.Folder.State");
        BindFormLabel(form, 3, EmulationResourceKeys.OsdTransparency);
        form.Margin = new Thickness(12);
        return EmulationSettingsLayout.ScrollPage(form);
    }

    private Grid OsdTransparencyControl()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var value = new TextBlock { MinWidth = 48, Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        value.SetBinding(TextBlock.TextProperty, new Binding(nameof(Slider.Value))
        {
            Source = _osdTransparency,
            StringFormat = "{0:0} %"
        });
        _osdTransparency.SetBinding(System.Windows.Automation.AutomationProperties.NameProperty,
            LocExtension.CreateBinding(EmulationResourceKeys.OsdTransparency));
        grid.Children.Add(_osdTransparency);
        Grid.SetColumn(value, 1);
        grid.Children.Add(value);
        return grid;
    }

    private ScrollViewer BuildShortcutsTab()
    {
        _shortcuts.ConfigurePresentation(LocExtension.Get("Emulation.Input.Actions"), LocExtension.Get("Emulation.Input.Binding.Search"));
        return EmulationSettingsLayout.ScrollPage(new StackPanel
        {
            Margin = new Thickness(12),
            Children = { EmulationSettingsLayout.InputBindings(_shortcuts, LocExtension.Get("Emulation.Shortcut.Global")) }
        });
    }

    private Grid BuildConfigurationsTab()
    {
        _configurationSelector.Margin = new Thickness(0, 0, 0, 12);

        var content = new Grid { Margin = new Thickness(12) };
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition());
        content.Children.Add(_configurationSelector);
        Grid.SetRow(_configurationTable, 1);
        content.Children.Add(_configurationTable);
        return content;
    }

    private Grid FolderControl(TextBox target)
    {
        var browse = new Button { MinWidth = 100 };
        BindingOperations.SetBinding(browse, ContentControl.ContentProperty, LocExtension.CreateBinding("Common.Browse"));
        browse.Click += async (_, _) => await BrowseFolderAsync(target);
        return PathControl(target, browse);
    }

    private static void BindFormLabel(Grid form, int row, string resourceKey)
    {
        var label = form.Children.OfType<TextBlock>().Single(element => Grid.GetRow(element) == row);
        BindingOperations.SetBinding(label, TextBlock.TextProperty, LocExtension.CreateBinding(resourceKey));
    }

    private static Grid PathControl(TextBox path, Button browse)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.Children.Add(path);
        Grid.SetColumn(browse, 1);
        grid.Children.Add(browse);
        return grid;
    }

    private TabItem AddTab(ItemsControl tabs, string icon, string resourceKey, UIElement content)
    {
        var tab = new TabItem
        {
            Header = new MainTabHeader { Icon = icon, Text = LocExtension.Get(resourceKey) },
            Content = content,
            Padding = new Thickness(14, 8, 14, 8)
        };
        tab.SetResourceReference(FrameworkElement.StyleProperty, "MainTabItemStyle");
        tabs.Items.Add(tab);
        _localizedTabs.Add((tab, resourceKey));
        return tab;
    }
}
