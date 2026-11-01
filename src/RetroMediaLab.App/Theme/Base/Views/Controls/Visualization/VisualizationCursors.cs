using System.Windows;
using System.Windows.Input;

namespace RetroMediaLab.App.Views.Controls.Visualization;

internal static class VisualizationCursors
{
    internal static Cursor Grabbing { get; } = LoadGrabbingCursor();

    private static Cursor LoadGrabbingCursor()
    {
        var resource = Application.GetResourceStream(new Uri("/retromedialab.app;component/Assets/Cursors/grabbing.cur", UriKind.Relative));
        if (resource is null) return Cursors.SizeAll;
        using (resource.Stream) return new Cursor(resource.Stream);
    }
}
