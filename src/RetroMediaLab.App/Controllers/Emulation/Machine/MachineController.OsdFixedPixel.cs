using RetroMediaLab.App.Functions.Emulation.Settings;
using RetroMediaLab.App.Constants.Localization;
using RetroMediaLab.Emulation;
using Catalog = RetroMediaLab.VideoPresentation.Dictionaries.EmulationVideoProcessingCatalog;
using Entry = RetroMediaLab.VideoPresentation.Windows.Osd.FullscreenOsdWindow.Entry;

namespace RetroMediaLab.App.Controllers.Emulation.Machine;

internal sealed partial class MachineController
{
    private Entry OsdFixedPixelSettings() =>
        VideoGroup(EmulationResourceKeys.VideoTechnologyFixedPixel,
            VideoGroup(EmulationResourceKeys.VideoDisplaySettings,
                FixedPixelChoice(Catalog.FixedPixelSubpixels, Catalog.SubpixelLayoutResourceKeys, c => c.Subpixels, (c, v) => c with { Subpixels = v }),
                FixedPixelChoice(Catalog.FixedPixelMonochromeColor, Catalog.MonochromePaletteResourceKeys, c => c.MonochromePalette, (c, v) => c with { MonochromePalette = v })
                    with { IsVisible = () => _video.VideoProcessing.FixedPixel.Subpixels == EmulationSubpixelLayout.Monochrome }),
            VideoGroup(Catalog.ParameterResourceKeys[Catalog.FixedPixelGridIntensity],
                FixedPixelNumber(Catalog.FixedPixelGridIntensity, c => c.GridIntensity, (c, v) => c with { GridIntensity = v }),
                FixedPixelNumber(Catalog.FixedPixelPixelGap, c => c.PixelGap, (c, v) => c with { PixelGap = v })),
            VideoGroup(EmulationResourceKeys.VideoPlasmaGroupLight,
                FixedPixelNumber(Catalog.FixedPixelBacklight, FixedPixelSettingsValues.Backlight,
                    (c, v) => c with { BacklightIntensity = v }) with { IsVisible = FixedPixelHasBacklight },
                FixedPixelNumber(Catalog.FixedPixelBacklightBleed, c => c.BacklightBleedIntensity,
                    (c, v) => c with { BacklightBleedIntensity = v }) with { IsVisible = FixedPixelHasBacklight },
                FixedPixelNumber(Catalog.FixedPixelOledEmission, c => c.OledEmissionIntensity,
                    (c, v) => c with { OledEmissionIntensity = v }) with { IsVisible = () => !FixedPixelHasBacklight() },
                FixedPixelNumber(Catalog.FixedPixelBlackDepth, FixedPixelSettingsValues.BlackDepth, (c, v) => c with { BlackDepth = v })),
            VideoGroup(EmulationResourceKeys.VideoParameterFixedPixelPersistence,
                FixedPixelNumber(Catalog.FixedPixelResponseTime, c => c.ResponseTimeMilliseconds,
                    (c, v) => c with { ResponseTimeMilliseconds = v }, EmulationVideoProcessingLimits.DurationMinimumMilliseconds,
                    EmulationVideoProcessingLimits.DurationMaximumMilliseconds),
                FixedPixelNumber(Catalog.FixedPixelPersistence, c => c.PersistenceIntensity, (c, v) => c with { PersistenceIntensity = v })))
            with { IsVisible = () => _video.VideoProcessing.DisplayTechnology == EmulationVideoDisplayTechnology.FixedPixel };

    private bool FixedPixelHasBacklight() => _video.VideoProcessing.FixedPixel.Technology != EmulationFixedPixelTechnology.Oled;

    private Entry FixedPixelNumber(string parameter, Func<EmulationFixedPixelVideoConfiguration, int> read,
        Func<EmulationFixedPixelVideoConfiguration, int, EmulationFixedPixelVideoConfiguration> update,
        int minimum = EmulationVideoProcessingLimits.IntensityMinimum, int maximum = EmulationVideoProcessingLimits.IntensityMaximum) =>
        VideoNumber(Catalog.ParameterResourceKeys[parameter], settings => read(settings.FixedPixel),
            (settings, value) => settings with { FixedPixel = update(settings.FixedPixel, value) }, minimum, maximum);

    private Entry FixedPixelChoice<T>(string parameter, IReadOnlyDictionary<T, string> keys, Func<EmulationFixedPixelVideoConfiguration, T> read,
        Func<EmulationFixedPixelVideoConfiguration, T, EmulationFixedPixelVideoConfiguration> update) where T : notnull =>
        VideoChoice(Catalog.ParameterResourceKeys[parameter], keys, settings => read(settings.FixedPixel),
            (settings, value) => settings with { FixedPixel = update(settings.FixedPixel, value) });
}
