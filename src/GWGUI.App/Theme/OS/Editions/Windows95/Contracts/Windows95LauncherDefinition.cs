using System.Windows.Media;

namespace GWGUI.App.Theme.OS.Editions.Windows95;

/// <summary>A presentation entry; its caller retains ownership of the action and its windows.</summary>
internal sealed record Windows95LauncherDefinition(
    string Id,
    Windows95ProgramGroup Group,
    string Title,
    ImageSource? Icon,
    Action Launch);
