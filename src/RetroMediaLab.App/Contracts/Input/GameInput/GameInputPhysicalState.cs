using RetroMediaLab.Emulation;

namespace RetroMediaLab.App.Services.Input.GameInput;

internal sealed record GameInputPhysicalState(IReadOnlySet<EmulationKey> Keys, EmulationPointerState Pointer, IReadOnlyList<EmulationControllerState> Controllers)
{
    /// <summary>Distinguishes a valid stationary mouse from an unavailable GameInput mouse reading.</summary>
    public bool HasPointerReading { get; init; } = true;
}
