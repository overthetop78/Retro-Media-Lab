using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using RetroMediaLab.App.Localization.Extensions;

namespace RetroMediaLab.App.Theme.Studio;

internal sealed class StudioCaptionBar : Grid
{
    public StudioCaptionBar()
    {
        SetResourceReference(BackgroundProperty, StudioConstants.ActiveTitleBrush);
        ColumnDefinitions.Add(new ColumnDefinition());
        ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var title = new TextBlock { Margin = new Thickness(14, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false };
        title.SetBinding(TextBlock.TextProperty, new Binding(nameof(Window.Title)) { RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Window), 1) });
        title.SetResourceReference(TextBlock.ForegroundProperty, StudioConstants.CaptionTextBrush);
        Children.Add(title);
        var commands = new StackPanel { Orientation = Orientation.Horizontal };
        SetColumn(commands, 1);
        var minimize = Command(StudioConstants.MinimizeGlyph, StudioConstants.MinimizeResource, () => SystemCommands.MinimizeWindow(Window.GetWindow(this)));
        commands.Children.Add(minimize);
        commands.Children.Add(Command(StudioConstants.MaximizeGlyph, StudioConstants.MaximizeResource, () => Toggle(Window.GetWindow(this))));
        commands.Children.Add(Command(StudioConstants.CloseGlyph, StudioConstants.CloseResource, () => SystemCommands.CloseWindow(Window.GetWindow(this))));
        Children.Add(commands);
        Loaded += (_, _) =>
        {
            var window = Window.GetWindow(this);
            minimize.Visibility = window?.Owner is not null && !window.ShowInTaskbar ? Visibility.Collapsed : Visibility.Visible;
        };
        MouseLeftButtonDown += (_, args) =>
        {
            if (args.OriginalSource is not DependencyObject source || FindButton(source)) return;
            var window = Window.GetWindow(this);
            if (window is null) return;
            if (args.ClickCount == 2) Toggle(window);
            else if (Mouse.LeftButton == MouseButtonState.Pressed) window.DragMove();
        };
    }

    internal static Button Command(string glyph, string resource, Action action)
    {
        var button = new Button { Content = glyph, ToolTip = LocExtension.Get(resource), Width = StudioConstants.CommandWidth, Padding = new Thickness(0), Margin = new Thickness(0), Focusable = true };
        button.SetResourceReference(StyleProperty, StudioConstants.CommandStyle);
        System.Windows.Automation.AutomationProperties.SetName(button, LocExtension.Get(resource));
        button.Click += (_, _) => action();
        return button;
    }

    private static bool FindButton(DependencyObject source)
    {
        for (DependencyObject? current = source; current is not null; current = System.Windows.Media.VisualTreeHelper.GetParent(current))
            if (current is Button) return true;
        return false;
    }

    private static void Toggle(Window? window)
    {
        if (window is null || window.ResizeMode is ResizeMode.NoResize or ResizeMode.CanMinimize) return;
        if (window.WindowState == WindowState.Maximized) SystemCommands.RestoreWindow(window);
        else SystemCommands.MaximizeWindow(window);
    }
}
