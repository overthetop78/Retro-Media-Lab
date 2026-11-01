using System.ComponentModel;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Threading;
using GWGUI.App.Localization.Extensions;
using GWGUI.App.Views.Dialogs.Common;
using GWGUI.App.Theme.Common;
using GWGUI.App.Theme.Orbit.Views;

namespace GWGUI.App.Theme.Orbit;

/// <summary>Owns native presentations; functional source windows retain their controllers and results.</summary>
internal sealed partial class OrbitWindowRouter : IDisposable
{
    private sealed record Presentation(OrbitWindow Window, Action RequestClose);
    private readonly FrameworkElement context;
    private readonly Func<bool?> appearance;
    private readonly Dictionary<Window, Presentation> presentations = [];
    private bool disposed;

    internal OrbitWindowRouter(FrameworkElement context, Func<bool?> appearance)
    {
        this.context = context;
        this.appearance = appearance;
    }

    internal bool TryShowWindow(Window source)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        var route = Route(source);
        if (route is null) return false;
        Present(source, route);
        return true;
    }

    private void Present(Window source, WindowRoute route)
    {
        var closed = false;
        var closing = false;
        var wait = new DispatcherFrame();
        OrbitWindow? window = null;
        FrameworkElement? view = null;
        EventHandler sourceClosed = (_, _) =>
        {
            closed = true;
            wait.Continue = false;
            if (!closing) window?.Close();
        };
        CancelEventHandler presentationClosing = (_, args) =>
        {
            if (closed) return;
            if (source is MessageDialog { CancelResult: MessageBoxResult.None })
            {
                args.Cancel = true;
                return;
            }
            closing = true;
            try { route.RequestClose(); }
            finally { closing = false; args.Cancel = !closed; }
        };
        source.Closed += sourceClosed;
        using var completion = ThemeDialogCompletion.Attach(source, _ => { if (!closed) route.RequestClose(); });
        using var bindings = new ThemeBindingScope();
        try
        {
            view = route.Create();
            if (closed) return;
            var owner = ThemeDialogCompletion.Owner(source) ?? source.Owner;
            var nativeOwner = owner is not null && presentations.TryGetValue(owner, out var parent)
                ? parent.Window : presentations.Values.LastOrDefault(item => item.Window.IsActive)?.Window ?? Window.GetWindow(context);
            window = new OrbitWindow
            {
                Document = view,
                AllowClose = source is not MessageDialog { CancelResult: MessageBoxResult.None },
                Width = route.Size.Width,
                Height = route.Size.Height,
                ResizeMode = source.ResizeMode,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = nativeOwner,
                ShowInTaskbar = false
            };
            window.SetBinding(Window.TitleProperty, LocExtension.CreateBinding(route.Title));
            if (!string.IsNullOrWhiteSpace(source.Title)) bindings.OneWay(source, Window.TitleProperty, window, Window.TitleProperty);
            if (source.Icon is not null) bindings.OneWay(source, Window.IconProperty, window, Window.IconProperty);
            window.Closing += presentationClosing;
            presentations.Add(source, new Presentation(window, route.RequestClose));
            window.ShowDialog();
        }
        finally
        {
            try
            {
                if (!closed)
                {
                    route.RequestClose();
                    if (!closed && !source.Dispatcher.HasShutdownStarted) Dispatcher.PushFrame(wait);
                }
            }
            finally
            {
                source.Closed -= sourceClosed;
                presentations.Remove(source);
                try
                {
                    if (window is not null)
                    {
                        window.Closing -= presentationClosing;
                        window.Close();
                    }
                }
                finally { (view as IDisposable)?.Dispose(); }
            }
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Exception? failure = null;
        try
        {
            foreach (var pair in presentations.Reverse().ToArray())
            {
                try
                {
                    var closed = false;
                    var wait = new DispatcherFrame();
                    EventHandler complete = (_, _) => { closed = true; wait.Continue = false; };
                    pair.Key.Closed += complete;
                    try
                    {
                        pair.Value.RequestClose();
                        if (!closed && !pair.Key.Dispatcher.HasShutdownStarted) Dispatcher.PushFrame(wait);
                    }
                    finally { pair.Key.Closed -= complete; }
                }
                catch (Exception error) { failure ??= error; }
            }
        }
        finally { if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw(); }
    }
}
