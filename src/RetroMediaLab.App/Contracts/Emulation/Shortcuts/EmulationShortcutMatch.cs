using RetroMediaLab.App.Enums.Emulation.Shortcuts;
namespace RetroMediaLab.App.Contracts.Emulation.Shortcuts;

internal sealed record EmulationShortcutMatch(EmulationShortcutMatchCategory Category, string? Action = null, bool ShouldExecute = false);
