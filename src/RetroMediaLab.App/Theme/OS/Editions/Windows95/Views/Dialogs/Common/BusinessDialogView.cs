using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Theme.Common;

namespace RetroMediaLab.App.Theme.OS.Editions.Windows95.Views;

internal abstract class BusinessDialogView : DockPanel, IDisposable
{
    protected readonly ThemeBindingScope Bindings = new();
    protected readonly Window Source;
    protected static readonly Thickness ContentMargin = new(8);
    protected static readonly Thickness ActionMargin = new(6, 0, 0, 0);
    private readonly List<(Button Button, RoutedEventHandler Handler)> handlers = [];
    private bool disposed;
    private const string DesktopUri = "/retromedialab.app;component/Theme/OS/Editions/Windows95/Styles/Windows95Desktop.xaml";
    private const string ControlsUri = "/retromedialab.app;component/Theme/OS/Editions/Windows95/Styles/Windows95Controls.xaml";

    protected BusinessDialogView(Window source)
    {
        Source = source;
        System.Windows.Documents.TextElement.SetFontFamily(this, new System.Windows.Media.FontFamily(Windows95DesktopMetrics.FontFamily));
        System.Windows.Documents.TextElement.SetFontSize(this, Windows95Metrics.FontSize);
        System.Windows.Documents.TextElement.SetForeground(this, System.Windows.Media.Brushes.Black);
        Background = System.Windows.Media.Brushes.Silver;
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(DesktopUri, UriKind.Relative) });
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(ControlsUri, UriKind.Relative) });
        Margin = ContentMargin;
        PreviewKeyDown += KeyPressed;
    }

    protected static TextBlock Label(string resource)
    {
        var text = new TextBlock { TextWrapping = TextWrapping.Wrap };
        text.SetBinding(TextBlock.TextProperty, LocExtension.CreateBinding(resource));
        return text;
    }

    protected Button Command(string resource, Action action, bool isDefault = false)
    {
        var button = new Button { Margin = ActionMargin, IsDefault = isDefault };
        button.SetBinding(ContentControl.ContentProperty, LocExtension.CreateBinding(resource));
        RoutedEventHandler handler = (_, _) => { if (!disposed) action(); };
        button.Click += handler;
        handlers.Add((button, handler));
        return button;
    }

    protected Button Forward(Button original)
    {
        var button = new Button { Margin = ActionMargin };
        Bindings.Action(original, button, followCaption: true);
        return button;
    }

    protected void Footer(params Button[] buttons)
    {
        var panel = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
        foreach (var button in buttons) panel.Children.Add(button);
        SetDock(panel, Dock.Bottom);
        Children.Add(panel);
    }

    protected virtual void Cancel() => ThemeDialogCompletion.Complete(Source, false);

    private void KeyPressed(object sender, KeyEventArgs args)
    {
        if (args.Key != Key.Escape) return;
        args.Handled = true;
        Cancel();
    }

    protected virtual void Detach() { }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try { Bindings.Dispose(); }
        finally
        {
            try { Detach(); }
            finally
            {
                PreviewKeyDown -= KeyPressed;
                foreach (var item in handlers) item.Button.Click -= item.Handler;
                handlers.Clear();
                Children.Clear();
                Resources.Clear();
            }
        }
    }
}
