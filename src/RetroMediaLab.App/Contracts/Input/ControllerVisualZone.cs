using RetroMediaLab.App.Enums.Input;
using RetroMediaLab.Emulation.Enums;

namespace RetroMediaLab.App.Contracts.Input;

internal sealed record ControllerVisualZone(EmulationControllerVisualControl Control, ControllerVisualZoneShape Shape, double XPercent, double YPercent, double WidthPercent, double HeightPercent);
