using RetroMediaLab.App.Constants.Localization;
using RetroMediaLab.Emulation;
using Catalog = RetroMediaLab.VideoPresentation.Dictionaries.EmulationVideoProcessingCatalog;
using Entry = RetroMediaLab.VideoPresentation.Windows.Osd.FullscreenOsdWindow.Entry;

namespace RetroMediaLab.App.Controllers.Emulation.Machine;

internal sealed partial class MachineController
{
    private static readonly IReadOnlyDictionary<bool, string> VideoToggleResourceKeys = new Dictionary<bool, string>
    {
        [false] = "Emulation.Value.Disabled",
        [true] = "Emulation.Value.Enabled"
    };

    private Entry OsdCrtSettings() =>
        VideoGroup(EmulationResourceKeys.VideoTechnologyCrt,
            VideoGroup(EmulationResourceKeys.VideoCrtGroupColor,
                CrtChoice(Catalog.CrtColorMode, Catalog.CrtColorModeResourceKeys, c => c.ColorMode, (c, v) => c with { ColorMode = v })),
            VideoGroup(EmulationResourceKeys.VideoCrtGroupBeam,
                CrtNumber(Catalog.CrtBeamWidth, c => c.BeamWidth, (c, v) => c with { BeamWidth = v }),
                CrtNumber(Catalog.CrtBeamIntensity, c => c.BeamIntensity, (c, v) => c with { BeamIntensity = v }),
                CrtNumber(Catalog.CrtBeamDiffusion, c => c.BeamDiffusion, (c, v) => c with { BeamDiffusion = v }),
                CrtNumber(Catalog.CrtHaloIntensity, c => c.HaloIntensity, (c, v) => c with { HaloIntensity = v })),
            VideoGroup(EmulationResourceKeys.VideoCrtGroupMask,
                CrtChoice(Catalog.CrtMask, Catalog.CrtMaskResourceKeys, c => c.Mask, (c, v) => c with { Mask = v }),
                CrtChoice(Catalog.CrtMaskSubpixels, Catalog.SubpixelLayoutResourceKeys, c => c.MaskSubpixels, (c, v) => c with { MaskSubpixels = v })
                    with { IsVisible = () => _video.VideoProcessing.Crt is { ColorMode: EmulationCrtColorMode.Color, Mask: not EmulationCrtMask.None } },
                CrtNumber(Catalog.CrtMaskIntensity, c => c.MaskIntensity, (c, v) => c with { MaskIntensity = v })
                    with { IsVisible = () => _video.VideoProcessing.Crt.Mask != EmulationCrtMask.None }),
            VideoGroup(EmulationResourceKeys.VideoCrtGroupGeometry,
                CrtNumber(Catalog.CrtHorizontalCurvature, c => c.HorizontalCurvature, (c, v) => c with { HorizontalCurvature = v }, -100, 100),
                CrtNumber(Catalog.CrtVerticalCurvature, c => c.VerticalCurvature, (c, v) => c with { VerticalCurvature = v }, -100, 100),
                CrtNumber(Catalog.CrtTrapezoid, c => c.Trapezoid, (c, v) => c with { Trapezoid = v }, -100, 100),
                CrtNumber(Catalog.CrtVignette, c => c.Vignette, (c, v) => c with { Vignette = v })),
            VideoGroup(EmulationResourceKeys.VideoCrtGroupScanlines,
                CrtChoice(Catalog.CrtScanlinesEnabled, VideoToggleResourceKeys, c => c.ScanlinesEnabled, (c, v) => c with { ScanlinesEnabled = v }),
                CrtChoice(Catalog.CrtScanlineOrientation, Catalog.PatternOrientationResourceKeys, c => c.ScanlineOrientation, (c, v) => c with { ScanlineOrientation = v })
                    with { IsVisible = CrtScanlinesEnabled },
                CrtNumber(Catalog.CrtScanlineIntensity, c => c.ScanlineIntensity, (c, v) => c with { ScanlineIntensity = v })
                    with { IsVisible = CrtScanlinesEnabled },
                CrtNumber(Catalog.CrtScanlineThickness, c => c.ScanlineThickness, (c, v) => c with { ScanlineThickness = v })
                    with { IsVisible = CrtScanlinesEnabled },
                CrtChoice(Catalog.CrtScanlinePhase, Catalog.ScanlinePhaseResourceKeys, c => c.ScanlinePhase, (c, v) => c with { ScanlinePhase = v })
                    with { IsVisible = CrtScanlinesEnabled },
                CrtNumber(Catalog.CrtScanlineCompensation, c => c.ScanlineCompensation, (c, v) => c with { ScanlineCompensation = v })
                    with { IsVisible = CrtScanlinesEnabled }),
            VideoGroup(EmulationResourceKeys.VideoCrtGroupInterference,
                CrtChoice(Catalog.CrtPatternEnabled, VideoToggleResourceKeys, c => c.PatternEnabled, (c, v) => c with { PatternEnabled = v }),
                CrtChoice(Catalog.CrtPatternOrientation, Catalog.PatternOrientationResourceKeys, c => c.PatternOrientation, (c, v) => c with { PatternOrientation = v })
                    with { IsVisible = CrtPatternEnabled },
                CrtNumber(Catalog.CrtPatternFrequency, c => c.PatternFrequency, (c, v) => c with { PatternFrequency = v })
                    with { IsVisible = CrtPatternEnabled },
                CrtNumber(Catalog.CrtPatternPhase, c => c.PatternPhase, (c, v) => c with { PatternPhase = v })
                    with { IsVisible = CrtPatternEnabled },
                CrtNumber(Catalog.CrtPatternIntensity, c => c.PatternIntensity, (c, v) => c with { PatternIntensity = v })
                    with { IsVisible = CrtPatternEnabled }))
            with { IsVisible = () => _video.VideoProcessing.DisplayTechnology == EmulationVideoDisplayTechnology.Crt };

    private bool CrtScanlinesEnabled() => _video.VideoProcessing.Crt.ScanlinesEnabled;
    private bool CrtPatternEnabled() => _video.VideoProcessing.Crt.PatternEnabled;

    private Entry CrtNumber(string parameter, Func<EmulationCrtVideoConfiguration, int> read,
        Func<EmulationCrtVideoConfiguration, int, EmulationCrtVideoConfiguration> update,
        int minimum = EmulationVideoProcessingLimits.IntensityMinimum, int maximum = EmulationVideoProcessingLimits.IntensityMaximum) =>
        VideoNumber(Catalog.ParameterResourceKeys[parameter], settings => read(settings.Crt),
            (settings, value) => settings with { Crt = update(settings.Crt, value) }, minimum, maximum, signed: minimum < 0);

    private Entry CrtChoice<T>(string parameter, IReadOnlyDictionary<T, string> keys, Func<EmulationCrtVideoConfiguration, T> read,
        Func<EmulationCrtVideoConfiguration, T, EmulationCrtVideoConfiguration> update) where T : notnull =>
        VideoChoice(Catalog.ParameterResourceKeys[parameter], keys, settings => read(settings.Crt),
            (settings, value) => settings with { Crt = update(settings.Crt, value) });
}
