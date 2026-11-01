using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GWGUI.App.Theme.OS.Editions.WindowsXP.Views;

internal sealed class WindowsXPWindow : Window
{
    public static readonly DependencyProperty DocumentProperty = DependencyProperty.Register(
        nameof(Document), typeof(object), typeof(WindowsXPWindow), new FrameworkPropertyMetadata(null));
    private bool closed;
    private WindowsXPResources? resourceScope;
    private WindowsXPSystemMenu? systemMenu;
    internal bool AllowClose { get; set; } = true;

    public static readonly DependencyProperty SchemeProperty = DependencyProperty.Register(nameof(Scheme),
        typeof(WindowsXPColorScheme), typeof(WindowsXPWindow),
        new FrameworkPropertyMetadata(WindowsXPColorScheme.Blue, (target, _) => ((WindowsXPWindow)target).ApplyScheme()),
        value => Enum.IsDefined((WindowsXPColorScheme)value));

    public WindowsXPColorScheme Scheme
    {
        get => (WindowsXPColorScheme)GetValue(SchemeProperty);
        set => SetValue(SchemeProperty, value);
    }

    public object? Document
    {
        get => GetValue(DocumentProperty);
        set => SetValue(DocumentProperty, value);
    }

    internal WindowsXPWindow()
    {
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.CanResize;
        try
        {
            Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(WindowsXPMetrics.ChromeUri, UriKind.Relative) });
            ApplyScheme();
            Icon = WindowsXPApplicationIcons.ForIcon(WindowsXPIcon.Generic);
            SetResourceReference(StyleProperty, typeof(WindowsXPWindow));
            Bind(SystemCommands.CloseWindowCommand, Close, () => AllowClose);
            Bind(SystemCommands.MinimizeWindowCommand, () => WindowState = WindowState.Minimized, () => ResizeMode != ResizeMode.NoResize);
            Bind(SystemCommands.MaximizeWindowCommand, () => WindowState = WindowState.Maximized,
                () => ResizeMode is ResizeMode.CanResize or ResizeMode.CanResizeWithGrip);
            Bind(SystemCommands.RestoreWindowCommand, () => WindowState = WindowState.Normal, () => WindowState != WindowState.Normal);
            Bind(SystemCommands.ShowSystemMenuCommand, ShowSystemMenu, () => IsVisible);
            InputBindings.Add(new KeyBinding(SystemCommands.ShowSystemMenuCommand, Key.Space, ModifierKeys.Alt));
        }
        catch
        {
            try { Close(); }
            finally { resourceScope?.Dispose(); resourceScope = null; CommandBindings.Clear(); Resources.MergedDictionaries.Clear(); }
            throw;
        }
    }

    private void ApplyScheme()
    {
        if (closed) return;
        systemMenu?.Dispose();
        systemMenu = null;
        resourceScope ??= new WindowsXPResources(this, Scheme);
        WindowsXPResources.ApplyScheme(this, Scheme);
    }

    private void ShowSystemMenu()
    {
        systemMenu?.Dispose();
        systemMenu = new WindowsXPSystemMenu(this);
        systemMenu.Show(new Point(0, WindowsXPImageCatalog.Definition(Scheme, WindowsXPImagePart.FrameCaption).FrameHeight));
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
                if (Template?.FindName(WindowsXPMetrics.DocumentPart, this) is ContentPresenter presenter) presenter.Content = null;
                ClearValue(DocumentProperty);
            }
            finally
            {
                try
                {
                    try { systemMenu?.Dispose(); }
                    finally { resourceScope?.Dispose(); }
                }
                finally
                {
                    systemMenu = null;
                    resourceScope = null;
                    InputBindings.Clear();
                    CommandBindings.Clear();
                    Resources.MergedDictionaries.Clear();
                }
            }
        }
    }
}
