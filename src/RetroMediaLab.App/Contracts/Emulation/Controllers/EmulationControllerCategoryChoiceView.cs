using RetroMediaLab.Emulation;
using RetroMediaLab.Emulation.Enums;

namespace RetroMediaLab.App.Contracts.Emulation.Controllers;

internal sealed record EmulationControllerCategoryChoiceView(EmulationControllerCategory Category, string DisplayName, IReadOnlyList<EmulationControllerChoice> Choices);
