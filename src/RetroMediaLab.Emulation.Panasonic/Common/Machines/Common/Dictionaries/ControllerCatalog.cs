using ThreeDOControllers = RetroMediaLab.Emulation.Panasonic.Common.Machines.ThreeDO.Dictionaries.ControllerCatalog;

namespace RetroMediaLab.Emulation.Panasonic.Common.Machines.Common.Dictionaries;

internal static class ControllerCatalog
{
    internal static IReadOnlyList<ControllerCommand> Commands(PeripheralCategory peripheral) => ThreeDOControllers.Commands(peripheral);
    internal static IReadOnlyList<InputBindingDefinition> Definitions(PeripheralCategory peripheral) => ThreeDOControllers.Definitions(peripheral);
    internal static IReadOnlyDictionary<EmulationControllerVisualControl, string> VisualCommands(PeripheralCategory peripheral) => ThreeDOControllers.VisualCommands(peripheral);
}
