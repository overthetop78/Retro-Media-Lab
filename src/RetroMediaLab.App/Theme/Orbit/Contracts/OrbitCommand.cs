using System.Windows.Controls;

namespace RetroMediaLab.App.Theme.Orbit;

/// <summary>A command descriptor; its functional menu item remains owned by the application.</summary>
internal sealed class OrbitCommand(MenuItem source, string title, string path)
{
    internal MenuItem Source { get; } = source;
    public string Title { get; } = title;
    public string Path { get; } = path;
    public bool IsEnabled => Source.IsEnabled;
    public bool IsCheckable => Source.IsCheckable;
    public bool IsChecked => Source.IsChecked;

    internal bool Execute()
    {
        if (!Source.IsEnabled || Source.HasItems) return false;
        Source.RaiseEvent(new System.Windows.RoutedEventArgs(MenuItem.ClickEvent, Source));
        return true;
    }
}
