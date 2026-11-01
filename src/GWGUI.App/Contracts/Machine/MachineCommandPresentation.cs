using System.Windows.Controls;

namespace GWGUI.App.Contracts.Machine;

/// <summary>Existing command endpoints shared with independent theme presentations.</summary>
internal sealed record MachineCommandPresentation(
    IReadOnlyDictionary<MachineCommandId, Button> Commands,
    TextBlock Status,
    TextBlock RendererStatus,
    TextBlock ControllerStatus,
    TextBlock PointerStatus);
