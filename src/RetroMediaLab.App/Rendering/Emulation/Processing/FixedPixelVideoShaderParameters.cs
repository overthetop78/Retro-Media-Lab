using System.Numerics;

namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal readonly record struct FixedPixelVideoShaderParameters(Vector4 Display, Vector4 Spatial, Vector4 Technology, Vector4 Temporal)
{
    internal static FixedPixelVideoShaderParameters From(EmulationVideoProcessingConfiguration configuration,
        int sourceWidth, int sourceHeight, bool hasHistory, double elapsedMilliseconds)
    {
        var fixedPixel = configuration.FixedPixel;
        var lcd = EmulationVideoProcessingConfigurationFunctions.LcdPanel(configuration);
        var oled = configuration.Oled;
        var isOled = fixedPixel.Technology == EmulationFixedPixelTechnology.Oled;
        var (panelWidth, panelHeight) = EmulationVideoProcessingCatalog.LcdDefinitionDimensions(
            isOled ? oled.Definition : lcd.Definition, sourceWidth, sourceHeight);
        return new FixedPixelVideoShaderParameters(
            new Vector4(configuration.DisplayTechnology == EmulationVideoDisplayTechnology.FixedPixel ? 1f : 0f,
                (float)fixedPixel.Technology, isOled ? (float)oled.PixelStructure : FilterLcdPixelStructure.Mode(lcd), 0f),
            new Vector4(0f, panelWidth, panelHeight, 0f),
            isOled ? new Vector4(0f, 0f, 0f, (float)oled.PanelType)
                : new Vector4(lcd.BacklightIntensity / 100f, (float)lcd.BacklightType, 0f, (float)lcd.PanelType),
            new Vector4(isOled ? 0f : FilterLcdDisplay.ResponseTime(lcd.PanelType),
                0f, !isOled && hasHistory ? 1f : 0f, (float)Math.Max(0, elapsedMilliseconds)));
    }
}
