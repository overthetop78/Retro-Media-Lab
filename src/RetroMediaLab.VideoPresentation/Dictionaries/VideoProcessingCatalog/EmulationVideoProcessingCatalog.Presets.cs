using RetroMediaLab.VideoPresentation.Contracts;
using RetroMediaLab.VideoPresentation.Enums;

namespace RetroMediaLab.VideoPresentation.Dictionaries;

public static partial class EmulationVideoProcessingCatalog
{
    private static IReadOnlyDictionary<EmulationVideoPreset, EmulationVideoProcessingConfiguration> CreatePresets() =>
        new Dictionary<EmulationVideoPreset, EmulationVideoProcessingConfiguration>
        {
            [EmulationVideoPreset.Normal] = new(),
            [EmulationVideoPreset.CrtArcadeColor] = CrtPreset(EmulationCrtMask.ApertureGrille),
            [EmulationVideoPreset.CrtTelevisionColor] = CrtPreset(EmulationCrtMask.SlotMask),
            [EmulationVideoPreset.LcdColor] = FixedPixelPreset(EmulationFixedPixelTechnology.Lcd, EmulationSubpixelLayout.Rgb, 35, 20, 16, 10, 70, 8),
            [EmulationVideoPreset.LcdMonochrome] = FixedPixelPreset(EmulationFixedPixelTechnology.Lcd, EmulationSubpixelLayout.Monochrome, 45, 25, 35, 25, 60, 15),
            [EmulationVideoPreset.LedBacklitLcd] = FixedPixelPreset(EmulationFixedPixelTechnology.LedBacklitLcd, EmulationSubpixelLayout.Rgb, 25, 15, 8, 5, 85, 12),
            [EmulationVideoPreset.Oled] = FixedPixelPreset(EmulationFixedPixelTechnology.Oled, EmulationSubpixelLayout.Rgb, 15, 10, 1, 0, null, 100),
            [EmulationVideoPreset.Plasma] = new()
            {
                DisplayTechnology = EmulationVideoDisplayTechnology.Plasma,
                Sampling = EmulationVideoSampling.Bilinear,
                Plasma = new(35, 30, 20, 20, 55, 25, 20, 35)
            },
            [EmulationVideoPreset.Vector] = new()
            {
                DisplayTechnology = EmulationVideoDisplayTechnology.Vector,
                Sampling = EmulationVideoSampling.Bilinear,
                Vector = new(50, 75, 45, 30)
            }
        };

    private static EmulationVideoProcessingConfiguration CrtPreset(EmulationCrtMask mask) => new()
    {
        DisplayTechnology = EmulationVideoDisplayTechnology.Crt,
        Crt = new(Mask: mask)
    };

    private static EmulationVideoProcessingConfiguration FixedPixelPreset(
        EmulationFixedPixelTechnology technology, EmulationSubpixelLayout subpixels, int grid,
        int gap, int responseMilliseconds, int persistence, int? backlight, int? blackDepth) => new()
        {
            DisplayTechnology = EmulationVideoDisplayTechnology.FixedPixel,
            Sampling = EmulationVideoSampling.Nearest,
            FixedPixel = new(technology, subpixels, GridIntensity: grid, PixelGap: gap, ResponseTimeMilliseconds: responseMilliseconds, PersistenceIntensity: persistence, BacklightIntensity: backlight, BlackDepth: blackDepth)
        };
}
