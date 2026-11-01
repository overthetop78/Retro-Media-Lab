using System.Windows.Media;

namespace GWGUI.App.Theme.OS.Editions.WindowsXP;

internal sealed record WindowsXPLauncherDefinition(string Id, WindowsXPProgramGroup Group,
    string Title, ImageSource? Icon, Action Launch);
