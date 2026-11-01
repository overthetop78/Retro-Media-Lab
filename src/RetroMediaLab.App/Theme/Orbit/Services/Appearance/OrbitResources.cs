using System.Windows;
using System.Windows.Controls;

namespace RetroMediaLab.App.Theme.Orbit;

/// <summary>Owns local presentation resources; the caller keeps its element and dictionaries.</summary>
internal sealed class OrbitResources : IDisposable
{
    private readonly FrameworkElement target;
    private readonly ResourceDictionary resources = new();
    private ResourceDictionary? palette;
    private bool disposed;

    // Explicit appearance is used for isolated previews. In the application the
    // active theme supplies dynamic palette keys, so changing appearance is live.
    internal OrbitResources(FrameworkElement target, bool? dark = null)
    {
        target.VerifyAccess();
        this.target = target;
        try
        {
            if (dark.HasValue) SetAppearance(dark.Value);
            resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(OrbitMetrics.ControlsUri, UriKind.Relative) });
            target.Resources.MergedDictionaries.Add(resources);
            // Do not inherit the Base ScrollViewer style through application resources.
            resources[typeof(ScrollViewer)] = new Style(typeof(ScrollViewer));
        }
        catch { Dispose(); throw; }
    }

    internal void SetAppearance(bool dark)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        target.VerifyAccess();
        var replacement = new ResourceDictionary { Source = new Uri(dark ? OrbitMetrics.DarkUri : OrbitMetrics.LightUri, UriKind.Relative) };
        var index = palette is null ? 0 : resources.MergedDictionaries.IndexOf(palette);
        if (palette is not null) resources.MergedDictionaries.Remove(palette);
        resources.MergedDictionaries.Insert(index, replacement);
        palette = replacement;
    }

    public void Dispose()
    {
        if (disposed) return;
        target.VerifyAccess();
        disposed = true;
        try { target.Resources.MergedDictionaries.Remove(resources); }
        finally
        {
            resources.MergedDictionaries.Clear();
            resources.Clear();
            palette = null;
        }
    }
}
