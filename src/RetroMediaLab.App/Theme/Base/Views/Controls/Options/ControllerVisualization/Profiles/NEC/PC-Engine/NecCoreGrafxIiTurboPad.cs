using RetroMediaLab.Emulation.Enums;

namespace RetroMediaLab.App.Views.Controls.Options.ControllerVisualization;

internal static partial class ControllerArtworkCatalog
{
    private static ProfileDefinition CreateNecCoreGrafxIiTurboPad() => new(ControllerArtworkFileNames.NecCoregrafxIiTurbopad, NecCoreGrafxTurboPadZones);
}
