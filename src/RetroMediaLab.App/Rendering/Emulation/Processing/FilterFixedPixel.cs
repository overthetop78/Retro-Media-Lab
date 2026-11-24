using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;

namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal static class FilterFixedPixel
{
    internal static void Apply(float[] colors, int sourceWidth, int sourceHeight, int outputWidth, int outputHeight,
        EmulationVideoProcessingConfiguration configuration)
    {
        var fixedPixel = configuration.FixedPixel;
        var lcd = EmulationVideoProcessingConfigurationFunctions.LcdPanel(configuration);
        if (EmulationVideoProcessingConfigurationFunctions.UsesLcdPanel(configuration))
        {
            var (panelWidth, panelHeight) = EmulationVideoProcessingCatalog.LcdDefinitionDimensions(lcd.Definition, sourceWidth, sourceHeight);
            FilterLcdPixelStructure.Apply(colors, panelWidth, panelHeight, outputWidth, outputHeight, lcd);
            if (fixedPixel.Technology == EmulationFixedPixelTechnology.Lcd)
                FilterLcdDisplay.Apply(colors, outputWidth, outputHeight, lcd);
            return;
        }

        var (oledWidth, oledHeight) = EmulationVideoProcessingCatalog.LcdDefinitionDimensions(
            configuration.Oled.Definition, sourceWidth, sourceHeight);
        FilterOledPixelStructure.Apply(colors, oledWidth, oledHeight, outputWidth, outputHeight, configuration.Oled.PixelStructure);
    }
}
