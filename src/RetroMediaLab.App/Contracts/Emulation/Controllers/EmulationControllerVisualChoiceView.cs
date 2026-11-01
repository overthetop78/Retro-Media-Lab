using System.Windows.Media;
using RetroMediaLab.Emulation;

namespace RetroMediaLab.App.Contracts.Emulation.Controllers;

internal sealed record EmulationControllerVisualChoiceView(string Key, string Value, ImageSource? Artwork)
{
    internal EmulationControllerChoice? ControllerChoice { get; init; }
    internal string? VisualId { get; init; }
}
