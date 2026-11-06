using System.Windows.Controls;

namespace RetroMediaLab.App.Contracts.Machine;

/// <summary>Existing command endpoints shared with independent theme presentations.</summary>
internal sealed record MachineCommandPresentation(
    IReadOnlyDictionary<MachineCommandId, Button> Commands,
    TextBlock Status,
    TextBlock RendererStatus,
    TextBlock ControllerStatus,
    TextBlock PointerStatus)
{
    internal TextBlock SizeStatus { get; } = new();
    internal TextBlock FpsStatus { get; } = new();
    internal TextBlock FrequencyStatus { get; } = new();
    internal TextBlock ExecutionStatus { get; } = new();
}
