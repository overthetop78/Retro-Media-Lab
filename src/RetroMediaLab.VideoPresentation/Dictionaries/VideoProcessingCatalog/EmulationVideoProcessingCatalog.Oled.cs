using RetroMediaLab.VideoPresentation.Enums;

namespace RetroMediaLab.VideoPresentation.Dictionaries;

public static partial class EmulationVideoProcessingCatalog
{
    public const string OledBlock = "Emulation.Video.FixedPixel.Technology.Oled";

    public static IReadOnlyDictionary<EmulationOledPanelType, string> OledPanelTypeResourceKeys { get; } =
        Enum.GetValues<EmulationOledPanelType>().ToDictionary(type => type,
            type => $"Emulation.Video.Oled.Panel.{type}");

    public static IReadOnlyDictionary<EmulationOledPixelStructure, string> OledPixelStructureResourceKeys { get; } =
        new Dictionary<EmulationOledPixelStructure, string>
        {
            [EmulationOledPixelStructure.None] = "Emulation.Video.Lcd.NoGrid",
            [EmulationOledPixelStructure.RgbVertical] = "Emulation.Video.Oled.Structure.RgbVertical",
            [EmulationOledPixelStructure.BgrVertical] = "Emulation.Video.Oled.Structure.BgrVertical",
            [EmulationOledPixelStructure.Wrgb] = "Emulation.Video.Oled.Structure.Wrgb",
            [EmulationOledPixelStructure.TriangularRgb] = "Emulation.Video.Oled.Structure.TriangularRgb",
            [EmulationOledPixelStructure.PenTileDiamond] = "Emulation.Video.Oled.Structure.PenTileDiamond",
            [EmulationOledPixelStructure.Monochrome] = "Emulation.Video.Subpixels.Monochrome"
        };

    public static bool IsOledConnection(EmulationCrtConnection connection) =>
        connection is not (EmulationCrtConnection.Scart or EmulationCrtConnection.SVideo);

    public static IReadOnlyDictionary<EmulationCrtConnection, string> OledConnectionResourceKeys =>
        CrtConnectionResourceKeys.Where(entry => IsOledConnection(entry.Key))
            .ToDictionary(entry => entry.Key, entry => entry.Value);
}
