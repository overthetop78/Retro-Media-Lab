using RetroMediaLab.Emulation.Enums;

namespace RetroMediaLab.Emulation.Contracts;

public sealed record EmulationInputBindingSet(
    IReadOnlyList<InputBindingDefinition> Definitions,
    IReadOnlyDictionary<string, string> Values,
    EmulationInputSource Sources,
    bool PrefixKeyboardSource = false,
    IReadOnlyList<string>? CompatibleVisualIds = null,
    string? DefaultVisualId = null,
    IReadOnlyDictionary<EmulationControllerVisualControl, string>? VisualCommandIds = null);
