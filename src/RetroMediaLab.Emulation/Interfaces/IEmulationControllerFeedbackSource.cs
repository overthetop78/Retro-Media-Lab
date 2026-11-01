using RetroMediaLab.Emulation.Contracts;

namespace RetroMediaLab.Emulation.Interfaces;

/// <summary>Optional controller outputs. Values are normalized to [0, 1]; an empty snapshot stops previous outputs.</summary>
public interface IEmulationControllerFeedbackSource
{
    IReadOnlyList<EmulationControllerOutputState> ControllerOutputs { get; }
}
