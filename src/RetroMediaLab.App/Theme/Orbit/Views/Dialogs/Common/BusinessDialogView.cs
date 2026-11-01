using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Documents;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Theme.Common;

namespace RetroMediaLab.App.Theme.Orbit.Views;

internal abstract class BusinessDialogView : DockPanel, IDisposable
{
    protected readonly ThemeBindingScope Bindings = new();
    protected readonly Window Source;
    protected static readonly Thickness ContentMargin = new(20);
    protected static readonly Thickness ActionMargin = new(6, 0, 0, 0);
    private readonly List<(Button Button, RoutedEventHandler Handler)> handlers = [];
    private bool disposed;
    private OrbitResources? resourceScope;

    protected BusinessDialogView(Window source, bool? dark = null)
    {
        Source = source;
        try
        {
            resourceScope = new OrbitResources(this, dark);
            SetResourceReference(BackgroundProperty, OrbitMetrics.WindowBrush);
            Margin = ContentMargin;
            SetResourceReference(TextElement.ForegroundProperty, OrbitMetrics.TextBrush);
            PreviewKeyDown += KeyPressed;
        }
        catch { Dispose(); throw; }
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
                try { resourceScope?.Dispose(); }
                finally { resourceScope = null; Resources.Clear(); }
            }
        }
    }
}
