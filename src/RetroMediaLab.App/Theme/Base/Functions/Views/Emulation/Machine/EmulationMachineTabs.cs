using RetroMediaLab.App.Constants.Emulation;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Views.Controls.Common;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using RetroMediaLab.Emulation;


namespace RetroMediaLab.App.Functions.Views.Emulation.Machine;

internal static class EmulationMachineTabs
{
    internal static TabControl Create(Func<EmulationMachineTab, UIElement?> contentProvider, string? automationName = null, Func<EmulationMachineTab, Task>? tabActivated = null, EmulationMachineTab? selectedTab = null)
    {
        var tabs = new TabControl
        {
            Margin = new Thickness(EmulationMachineTabConstants.OuterMargin),
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch
        };
        if (!string.IsNullOrWhiteSpace(automationName)) AutomationProperties.SetName(tabs, automationName);
        foreach (var definition in EmulationMachineTabConstants.Definitions)
        {
            var content = contentProvider(definition.Tab);
            if (content is null) continue;
            var title = LocExtension.Get(definition.ResourceKey);
            var tab = new TabItem
            {
                Tag = definition.Tab,
                Header = CreateHeader(definition.Tab, definition.Icon, title),
                Content = content,
                Padding = new Thickness(EmulationMachineTabConstants.HorizontalPadding, EmulationMachineTabConstants.VerticalPadding, EmulationMachineTabConstants.HorizontalPadding, EmulationMachineTabConstants.VerticalPadding),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch
            };
            AutomationProperties.SetName(tab, title);
            tab.SetResourceReference(FrameworkElement.StyleProperty, "MainTabItemStyle");
            tabs.Items.Add(tab);
        }
        if (tabActivated is not null)
            tabs.SelectionChanged += async (_, args) =>
            {
                if (args.Source == tabs && tabs.SelectedItem is TabItem { Tag: EmulationMachineTab tab })
                    await tabActivated(tab);
            };
        if (selectedTab is not null)
        {
            var selected = tabs.Items.Cast<TabItem>().FirstOrDefault(item => item.Tag is EmulationMachineTab tab && tab == selectedTab);
            if (selected is not null) tabs.SelectedItem = selected;
        }
        return tabs;
    }

    private static FrameworkElement CreateHeader(EmulationMachineTab tab, string icon, string title)
    {
        if (tab is not (EmulationMachineTab.Ram or EmulationMachineTab.Rom))
            return new MainTabHeader { Icon = icon, Text = title };
        var shape = new System.Windows.Shapes.Path
        {
            Data = Geometry.Parse(tab == EmulationMachineTab.Ram
                ? EmulationMachineTabConstants.RamGeometry : EmulationMachineTabConstants.RomGeometry),
            Width = 22, Height = 22, Stretch = Stretch.Uniform, StrokeThickness = 1.3,
            Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center
        };
        shape.SetResourceReference(System.Windows.Shapes.Shape.StrokeProperty, "TextBrush");
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Children = { shape, new TextBlock { Text = title, VerticalAlignment = VerticalAlignment.Center } }
        };
    }
}
