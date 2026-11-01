using RetroMediaLab.Emulation;

namespace RetroMediaLab.App.Services.Input.GameInput;

internal sealed record GameInputPhysicalState(IReadOnlySet<EmulationKey> Keys, EmulationPointerState Pointer, IReadOnlyList<EmulationControllerState> Controllers);
