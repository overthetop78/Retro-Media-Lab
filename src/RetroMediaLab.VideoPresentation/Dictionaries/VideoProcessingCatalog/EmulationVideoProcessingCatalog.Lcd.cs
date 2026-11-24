using RetroMediaLab.VideoPresentation.Enums;

namespace RetroMediaLab.VideoPresentation.Dictionaries;

public static partial class EmulationVideoProcessingCatalog
{
    public const string LcdBlock = "Emulation.Video.FixedPixel.Technology.Lcd";
    public const string LcdDefinition = "Emulation.Video.Lcd.Definition";
    public const string LcdPixelStructure = "Emulation.Video.Lcd.PixelStructure";
    public const string LcdPixelOrientation = "Emulation.Video.Lcd.PixelOrientation";
    public const string LcdPanelType = "Emulation.Video.Lcd.PanelType";
    public const string LcdBacklight = "Emulation.Video.Parameter.FixedPixelBacklight";
    public const string LedBlock = "Emulation.Video.Led.Block";
    public const string LedBacklightType = "Emulation.Video.Led.BacklightType";

    public static IReadOnlyDictionary<EmulationLedBacklightType, string> LedBacklightTypeResourceKeys { get; } =
        Enum.GetValues<EmulationLedBacklightType>().ToDictionary(type => type,
            type => $"Emulation.Video.Led.BacklightType.{type}");

    public static IReadOnlyDictionary<EmulationLcdDefinition, string> LcdDefinitionResourceKeys { get; } =
        Enum.GetValues<EmulationLcdDefinition>().ToDictionary(definition => definition,
            definition => $"Emulation.Video.Lcd.Definition.{definition}");

    public static (int Width, int Height) LcdDefinitionDimensions(
        EmulationLcdDefinition definition, int sourceWidth, int sourceHeight) => definition switch
    {
        EmulationLcdDefinition.Vga => (640, 480),
        EmulationLcdDefinition.Svga => (800, 600),
        EmulationLcdDefinition.Xga => (1024, 768),
        EmulationLcdDefinition.Hd => (1280, 720),
        EmulationLcdDefinition.Sxga => (1280, 1024),
        EmulationLcdDefinition.HdReady => (1366, 768),
        EmulationLcdDefinition.FullHd => (1920, 1080),
        EmulationLcdDefinition.Qhd => (2560, 1440),
        EmulationLcdDefinition.Uhd4K => (3840, 2160),
        _ => (sourceWidth, sourceHeight)
    };

    public static IReadOnlyDictionary<EmulationLcdPixelStructure, string> LcdPixelStructureResourceKeys { get; } =
        new Dictionary<EmulationLcdPixelStructure, string>
        {
            [EmulationLcdPixelStructure.None] = "Emulation.Video.Lcd.NoGrid",
            [EmulationLcdPixelStructure.Rgb] = "Emulation.Video.Subpixels.Rgb",
            [EmulationLcdPixelStructure.Bgr] = "Emulation.Video.Subpixels.Bgr",
            [EmulationLcdPixelStructure.Monochrome] = "Emulation.Video.Subpixels.Monochrome"
        };

    public static IReadOnlyDictionary<EmulationLcdPixelOrientation, string> LcdPixelOrientationResourceKeys { get; } =
        new Dictionary<EmulationLcdPixelOrientation, string>
        {
            [EmulationLcdPixelOrientation.Horizontal] = "Emulation.Video.Lcd.Horizontal",
            [EmulationLcdPixelOrientation.Vertical] = "Emulation.Video.Lcd.Vertical"
        };

    public static IReadOnlyDictionary<EmulationLcdPanelType, string> LcdPanelTypeResourceKeys { get; } =
        new Dictionary<EmulationLcdPanelType, string>
        {
            [EmulationLcdPanelType.Tn] = "Emulation.Video.Lcd.Tn",
            [EmulationLcdPanelType.Ips] = "Emulation.Video.Lcd.Ips",
            [EmulationLcdPanelType.Va] = "Emulation.Video.Lcd.Va"
        };
}
