using RetroMediaLab.App.Enums.Emulation.Shortcuts;
using RetroMediaLab.App.Contracts.Input;
namespace RetroMediaLab.App.Contracts.Emulation.Shortcuts;

internal sealed record EmulationShortcutMatch(EmulationShortcutMatchCategory Category, GlobalShortcutBinding? Binding = null);
