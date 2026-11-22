using RetroMediaLab.VideoPresentation.Contracts;
using RetroMediaLab.VideoPresentation.Enums;

namespace RetroMediaLab.App.Functions.Emulation.Settings;

internal static class FixedPixelSettingsValues
{
    internal static int Backlight(EmulationFixedPixelVideoConfiguration settings) =>
        settings.BacklightIntensity ?? (settings.Technology == EmulationFixedPixelTechnology.Lcd ? 65 : 80);

    internal static int BlackDepth(EmulationFixedPixelVideoConfiguration settings) =>
        settings.BlackDepth ?? settings.Technology switch
        {
            EmulationFixedPixelTechnology.Lcd => 35,
            EmulationFixedPixelTechnology.LedBacklitLcd => 55,
            _ => 100
        };
}
