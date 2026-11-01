using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RetroMediaLab.App.Theme.Orbit.Views;

internal sealed class OrbitWindow : Window
{
    public static readonly DependencyProperty DocumentProperty = DependencyProperty.Register(
        nameof(Document), typeof(object), typeof(OrbitWindow), new FrameworkPropertyMetadata(null));
    private bool closed;
    private OrbitResources? resourceScope;
    internal bool AllowClose { get; set; } = true;

    public object? Document
    {
        get => GetValue(DocumentProperty);
        set => SetValue(DocumentProperty, value);
    }

    internal OrbitWindow(bool? dark = null)
    {
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.CanResize;
        try
        {
            resourceScope = new OrbitResources(this, dark);
            Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(OrbitWindowMetrics.ChromeUri, UriKind.Relative) });
            SetResourceReference(StyleProperty, typeof(OrbitWindow));
            Bind(SystemCommands.CloseWindowCommand, Close, () => AllowClose);
            Bind(SystemCommands.MinimizeWindowCommand, () => WindowState = WindowState.Minimized, () => ResizeMode != ResizeMode.NoResize);
            Bind(SystemCommands.MaximizeWindowCommand, () => WindowState = WindowState.Maximized,
                () => ResizeMode is ResizeMode.CanResize or ResizeMode.CanResizeWithGrip);
            Bind(SystemCommands.RestoreWindowCommand, () => WindowState = WindowState.Normal, () => WindowState != WindowState.Normal);
            Bind(SystemCommands.ShowSystemMenuCommand, () => SystemCommands.ShowSystemMenu(this,
                PointToScreen(new Point(0, OrbitWindowMetrics.CaptionHeight))), () => IsVisible);
        }
        catch
        {
            try { Close(); }
            finally { CommandBindings.Clear(); resourceScope?.Dispose(); resourceScope = null; Resources.MergedDictionaries.Clear(); }
            throw;
        }
    }

    private void Bind(RoutedCommand command, Action execute, Func<bool> allowed)
    {
        CommandBindings.Add(new CommandBinding(command, (_, args) =>
        {
            if (closed || !allowed()) return;
            args.Handled = true;
            execute();
        }, (_, args) => { args.CanExecute = !closed && allowed(); args.Handled = true; }));
    }

    protected override void OnClosed(EventArgs args)
    {
        try { base.OnClosed(args); }
        finally
        {
            closed = true;
            try
            {
                if (Template?.FindName(OrbitWindowMetrics.DocumentPart, this) is ContentPresenter presenter) presenter.Content = null;
                ClearValue(DocumentProperty);
            }
            finally
            {
                CommandBindings.Clear();
                try { resourceScope?.Dispose(); }
                finally { resourceScope = null; Resources.MergedDictionaries.Clear(); }
            }
        }
    }
}
