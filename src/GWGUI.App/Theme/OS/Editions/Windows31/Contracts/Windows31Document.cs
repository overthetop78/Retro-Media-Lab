using System.Windows;

namespace GWGUI.App.Theme.OS.Editions.Windows31;

/// <summary>The factory explicitly transfers a lifetime; borrowed views have no lifetime to dispose.</summary>
internal sealed class Windows31Document(FrameworkElement view, IDisposable? lifetime = null) : IDisposable
{
    private bool disposed;
    internal FrameworkElement View { get; } = view;

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        lifetime?.Dispose();
    }
}
