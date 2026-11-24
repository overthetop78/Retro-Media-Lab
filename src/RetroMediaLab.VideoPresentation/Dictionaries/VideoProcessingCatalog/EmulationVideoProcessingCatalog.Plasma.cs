using RetroMediaLab.VideoPresentation.Enums;

namespace RetroMediaLab.VideoPresentation.Dictionaries;

public static partial class EmulationVideoProcessingCatalog
{
    private static class PlasmaOptions
    {
        internal static readonly IReadOnlyDictionary<EmulationLcdDefinition, string> Definitions =
            new[] { EmulationLcdDefinition.Native, EmulationLcdDefinition.Vga,
                EmulationLcdDefinition.Xga, EmulationLcdDefinition.Hd, EmulationLcdDefinition.HdReady,
                EmulationLcdDefinition.FullHd }.ToDictionary(value => value, value => $"Emulation.Video.Lcd.Definition.{value}");

        internal static readonly IReadOnlyDictionary<EmulationCrtConnection, string> Connections =
            ResourceKeys("Crt.Connection", Enum.GetValues<EmulationCrtConnection>()
                .Where(value => value != EmulationCrtConnection.LegacyMonitor));

    }

    public static IReadOnlyDictionary<EmulationLcdDefinition, string> PlasmaDefinitionResourceKeys => PlasmaOptions.Definitions;
    public static IReadOnlyDictionary<EmulationCrtConnection, string> PlasmaConnectionResourceKeys => PlasmaOptions.Connections;
    public static IReadOnlyDictionary<EmulationPlasmaCellStructure, string> PlasmaCellStructureResourceKeys { get; } =
        new Dictionary<EmulationPlasmaCellStructure, string>
        {
            [EmulationPlasmaCellStructure.Stripe] = "Emulation.Video.Plasma.CellStructure.Stripe",
            [EmulationPlasmaCellStructure.Waffle] = "Emulation.Video.Plasma.CellStructure.Waffle",
            [EmulationPlasmaCellStructure.Delta] = "Emulation.Video.Plasma.CellStructure.Delta",
            [EmulationPlasmaCellStructure.Monochrome] = "Emulation.Video.Subpixels.Monochrome"
        };
}
