using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GWGUI.App.Localization.Extensions;

namespace GWGUI.App.Theme.OS.Editions.Windows31;

internal enum Windows31SystemCommand
{
    Restore,
    Move,
    Size,
    Minimize,
    Maximize,
    Close
}

/// <summary>An owned menu surface inside the borrowed desktop, without a native popup.</summary>
internal sealed class Windows31SystemMenu : IDisposable
{
    private static readonly (Windows31SystemCommand Command, string Resource)[] Commands =
    [
        (Windows31SystemCommand.Restore, Windows31Metrics.RestoreResource),
        (Windows31SystemCommand.Move, Windows31Metrics.MoveResource),
        (Windows31SystemCommand.Size, Windows31Metrics.SizeResource),
        (Windows31SystemCommand.Minimize, Windows31Metrics.MinimizeResource),
        (Windows31SystemCommand.Maximize, Windows31Metrics.EnlargeResource),
        (Windows31SystemCommand.Close, Windows31Metrics.CloseResource)
    ];

    private readonly Canvas desktop;
    private readonly Border surface = new();
    private readonly Dictionary<Windows31SystemCommand, Button> items = [];
    private readonly Action<Windows31SystemCommand> execute;
    private readonly Action dismiss;
    private readonly Func<Windows31SystemCommand, bool> canExecute;
    private readonly IInputElement? previousFocus;
    private bool disposed;

    internal Windows31SystemMenu(Canvas desktop, Point location,
        Func<Windows31SystemCommand, bool> canExecute, Action<Windows31SystemCommand> execute, Action dismiss)
    {
        this.desktop = desktop;
        this.execute = execute;
        this.dismiss = dismiss;
        this.canExecute = canExecute;
        previousFocus = Keyboard.FocusedElement;
        try
        {
            var rows = new StackPanel();
            surface.Child = rows;
            surface.SetResourceReference(Border.BorderBrushProperty, Windows31PaletteResources.WindowFrame);
            surface.BorderThickness = new Thickness(1);
            surface.SetResourceReference(Border.BackgroundProperty, Windows31PaletteResources.Menu);
            surface.MinWidth = Windows31Metrics.SystemMenuMinimumWidth;
            surface.Padding = new Thickness(1);
            surface.SetResourceReference(TextBlock.FontFamilyProperty, Windows31Metrics.CaptionFontResource);
            surface.SetValue(TextBlock.FontSizeProperty, Windows31Metrics.CaptionFontSize);
            surface.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
            TextOptions.SetTextFormattingMode(surface, TextFormattingMode.Ideal);
            TextOptions.SetTextRenderingMode(surface, TextRenderingMode.Aliased);
            var style = ItemStyle();
            foreach (var (command, resource) in Commands)
            {
                if (command == Windows31SystemCommand.Close)
                {
                    var separator = new Border { Height = 1, Margin = new Thickness(0, 2, 0, 2) };
                    separator.SetResourceReference(Border.BackgroundProperty, Windows31PaletteResources.WindowFrame);
                    rows.Children.Add(separator);
                }
                var button = new Button { Tag = command, Style = style, IsEnabled = canExecute(command) };
                button.SetBinding(ContentControl.ContentProperty, LocExtension.CreateBinding(resource));
                button.Click += ItemClick;
                button.MouseEnter += ItemMouseEnter;
                items.Add(command, button);
                rows.Children.Add(button);
            }
            surface.PreviewKeyDown += MenuKeyDown;
            surface.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var area = desktop.RenderSize;
            Canvas.SetLeft(surface, Math.Clamp(location.X, 0, Math.Max(0, area.Width - surface.DesiredSize.Width)));
            Canvas.SetTop(surface, Math.Clamp(location.Y, 0, Math.Max(0, area.Height - surface.DesiredSize.Height)));
            Panel.SetZIndex(surface, int.MaxValue);
            desktop.Children.Add(surface);
            desktop.PreviewMouseDown += DesktopMouseDown;
            FocusItem(FirstEnabled());
        }
        catch { Dispose(); throw; }
    }

    internal FrameworkElement Surface => surface;
    internal bool CanExecute(Windows31SystemCommand command) => !disposed && canExecute(command);

    internal void Invoke(Windows31SystemCommand command)
    {
        if (!CanExecute(command)) return;
        try { dismiss(); }
        finally { Dispose(); }
        execute(command);
    }

    private void Cancel()
    {
        try { dismiss(); }
        finally { Dispose(); }
    }

    private void ItemClick(object sender, RoutedEventArgs args)
    {
        if (sender is Button { Tag: Windows31SystemCommand command }) Invoke(command);
        args.Handled = true;
    }

    private void ItemMouseEnter(object sender, MouseEventArgs args)
    {
        if (sender is Button { IsEnabled: true } button) button.Focus();
    }

    private Button? FirstEnabled() => items.Values.FirstOrDefault(item => item.IsEnabled);
    private static void FocusItem(Button? item) => item?.Focus();

    private void MenuKeyDown(object sender, KeyEventArgs args)
    {
        if (args.Key is Key.Escape or Key.Tab || args.SystemKey == Key.Space)
        {
            Cancel();
            args.Handled = true;
            return;
        }
        if (args.Key is not (Key.Up or Key.Down or Key.Home or Key.End)) return;
        var enabled = items.Values.Where(item => item.IsEnabled).ToArray();
        if (enabled.Length == 0) return;
        var current = Array.FindIndex(enabled, item => item.IsKeyboardFocusWithin);
        var index = args.Key switch
        {
            Key.Home => 0,
            Key.End => enabled.Length - 1,
            Key.Up => (current - 1 + enabled.Length) % enabled.Length,
            _ => (current + 1) % enabled.Length
        };
        FocusItem(enabled[index]);
        args.Handled = true;
    }

    private void DesktopMouseDown(object sender, MouseButtonEventArgs args)
    {
        if (args.OriginalSource is DependencyObject source && !surface.IsAncestorOf(source) && !ReferenceEquals(surface, source)) Cancel();
    }

    private static Style ItemStyle()
    {
        var style = new Style(typeof(Button));
        style.Setters.Add(new Setter(Control.BackgroundProperty, new DynamicResourceExtension(Windows31PaletteResources.Menu)));
        style.Setters.Add(new Setter(Control.ForegroundProperty, new DynamicResourceExtension(Windows31PaletteResources.MenuText)));
        style.Setters.Add(new Setter(FrameworkElement.HeightProperty, Windows31Metrics.SystemMenuRowHeight));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(12, 0, 20, 0)));
        style.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Left));
        style.Setters.Add(new Setter(Control.FocusVisualStyleProperty, null));
        var template = new ControlTemplate(typeof(Button));
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        var content = new FrameworkElementFactory(typeof(Windows31ContentPresenter));
        content.SetValue(FrameworkElement.MarginProperty, new TemplateBindingExtension(Control.PaddingProperty));
        content.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        border.AppendChild(content);
        template.VisualTree = border;
        style.Setters.Add(new Setter(Control.TemplateProperty, template));
        var focused = new Trigger { Property = UIElement.IsKeyboardFocusWithinProperty, Value = true };
        focused.Setters.Add(new Setter(Control.BackgroundProperty, new DynamicResourceExtension(Windows31PaletteResources.Selection)));
        focused.Setters.Add(new Setter(Control.ForegroundProperty, new DynamicResourceExtension(Windows31PaletteResources.SelectionText)));
        style.Triggers.Add(focused);
        var disabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
        disabled.Setters.Add(new Setter(Control.ForegroundProperty, new DynamicResourceExtension(Windows31PaletteResources.GrayText)));
        style.Triggers.Add(disabled);
        return style;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try
        {
            desktop.PreviewMouseDown -= DesktopMouseDown;
            surface.PreviewKeyDown -= MenuKeyDown;
            foreach (var item in items.Values)
            {
                item.Click -= ItemClick;
                item.MouseEnter -= ItemMouseEnter;
                System.Windows.Data.BindingOperations.ClearAllBindings(item);
            }
        }
        finally
        {
            surface.Child = null;
            items.Clear();
            desktop.Children.Remove(surface);
            if (previousFocus is UIElement { IsVisible: true, IsEnabled: true } previous) previous.Focus();
        }
    }
}
