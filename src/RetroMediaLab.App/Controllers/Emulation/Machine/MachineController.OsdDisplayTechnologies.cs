using RetroMediaLab.App.Constants.Localization;
using RetroMediaLab.Emulation;
using Catalog = RetroMediaLab.VideoPresentation.Dictionaries.EmulationVideoProcessingCatalog;
using Entry = RetroMediaLab.VideoPresentation.Windows.Osd.FullscreenOsdWindow.Entry;

namespace RetroMediaLab.App.Controllers.Emulation.Machine;

internal sealed partial class MachineController
{
    private Entry OsdPlasmaSettings()
    {
        var fields = new VideoTechnologyFields<EmulationPlasmaVideoConfiguration>(this,
            config => config.Plasma, (config, plasma) => config with { Plasma = plasma });
        return VideoGroup(EmulationResourceKeys.VideoTechnologyPlasma,
            fields.Choice(Catalog.CrtColorStandards, Catalog.CrtColorStandardsResourceKeys,
                c => c.ColorStandards, (c, v) => c with { ColorStandards = v }),
            fields.Choice(Catalog.CrtFieldRate, Catalog.CrtFieldRateResourceKeys,
                c => c.FieldRate, (c, v) => c with { FieldRate = v }),
            VideoChoice(Catalog.LcdDefinition, Catalog.PlasmaDefinitionResourceKeys, c => c.Plasma.Definition,
                (c, v) => c with { Plasma = c.Plasma with { Definition = v } }),
            fields.Choice(Catalog.PlasmaCellStructure, Catalog.PlasmaCellStructureResourceKeys,
                c => c.CellStructure, (c, v) => c with { CellStructure = v }),
            fields.Choice(Catalog.CrtConnection, Catalog.PlasmaConnectionResourceKeys,
                c => c.Connection, (c, v) => c with { Connection = v }),
            fields.Number(Catalog.CrtConnectionDegradation, c => c.ConnectionDegradation,
                (c, v) => c with { ConnectionDegradation = v }, EmulationVideoProcessingLimits.CrtConnectionDegradationMaximum)
                with { IsVisible = () => _video.VideoProcessing.Plasma.CanDegradeConnection() })
            with { IsVisible = () => _video.VideoProcessing.DisplayTechnology == EmulationVideoDisplayTechnology.Plasma };
    }

    private Entry OsdVectorSettings()
    {
        var fields = new VideoTechnologyFields<EmulationVectorVideoConfiguration>(this,
            s => s.Vector, (s, c) => s with { Vector = c });
        return VideoGroup(EmulationResourceKeys.VideoTechnologyVector,
            VideoGroup(EmulationResourceKeys.VideoVectorGroupDrawing,
                fields.Number(Catalog.VectorLineThreshold, c => c.LineThreshold, (c, v) => c with { LineThreshold = v }),
                fields.Number(Catalog.VectorLineIntensity, c => c.LineIntensity, (c, v) => c with { LineIntensity = v }),
                fields.Number(Catalog.VectorBeamWidth, c => c.BeamWidth, (c, v) => c with { BeamWidth = v }),
                fields.Number(Catalog.VectorBeamFocus, c => c.BeamFocus, (c, v) => c with { BeamFocus = v }),
                fields.Choice(Catalog.VectorPhosphorColor, Catalog.CrtColorModeResourceKeys, c => c.PhosphorColor, (c, v) => c with { PhosphorColor = v })),
            VideoGroup(EmulationResourceKeys.VideoVectorGroupGlow,
                fields.Number(Catalog.VectorHaloIntensity, c => c.HaloIntensity, (c, v) => c with { HaloIntensity = v }),
                fields.Number(Catalog.VectorHaloRadius, c => c.HaloRadius, (c, v) => c with { HaloRadius = v }),
                fields.Number(Catalog.VectorPersistence, c => c.PersistenceIntensity, (c, v) => c with { PersistenceIntensity = v })))
            with { IsVisible = () => _video.VideoProcessing.DisplayTechnology == EmulationVideoDisplayTechnology.Vector };
    }

    private Entry OsdVfdSettings()
    {
        var fields = new VideoTechnologyFields<EmulationVfdVideoConfiguration>(this,
            s => s.Vfd, (s, c) => s with { Vfd = c });
        return VideoGroup(EmulationResourceKeys.VideoTechnologyVfd,
            VideoGroup(EmulationResourceKeys.VideoVfdGroupPhosphorAndGlass,
                fields.Choice(Catalog.VfdColor, Catalog.VfdColorResourceKeys, c => c.Color, (c, v) => c with { Color = v }),
                fields.Number(Catalog.VfdPhosphorIntensity, c => c.PhosphorIntensity, (c, v) => c with { PhosphorIntensity = v }),
                fields.Number(Catalog.VfdEmissionThreshold, c => c.EmissionThreshold, (c, v) => c with { EmissionThreshold = v }),
                fields.Number(Catalog.VfdGlassDarkening, c => c.GlassDarkening, (c, v) => c with { GlassDarkening = v })),
            VideoGroup(EmulationResourceKeys.VideoVfdGroupAnodes,
                fields.Choice(Catalog.VfdStructure, Catalog.VfdStructureResourceKeys, c => c.Structure, (c, v) => c with { Structure = v }),
                fields.Number(Catalog.VfdCellSize, c => c.CellSize, (c, v) => c with { CellSize = v })
                    with { IsEnabled = () => _video.VideoProcessing.Vfd.Structure == EmulationVfdStructure.DotMatrix },
                fields.Number(Catalog.VfdCellGap, c => c.CellGap, (c, v) => c with { CellGap = v })
                    with { IsEnabled = () => _video.VideoProcessing.Vfd.Structure == EmulationVfdStructure.DotMatrix }),
            VideoGroup(EmulationResourceKeys.VideoVfdGroupGlowAndResponse,
                fields.Number(Catalog.VfdHaloIntensity, c => c.HaloIntensity, (c, v) => c with { HaloIntensity = v }),
                fields.Number(Catalog.VfdHaloRadius, c => c.HaloRadius, (c, v) => c with { HaloRadius = v }),
                fields.Number(Catalog.VfdPersistence, c => c.PersistenceMilliseconds, (c, v) => c with { PersistenceMilliseconds = v }, EmulationVideoProcessingLimits.DurationMaximumMilliseconds)))
            with { IsVisible = () => _video.VideoProcessing.DisplayTechnology == EmulationVideoDisplayTechnology.Vfd };
    }

    private Entry OsdLedMatrixSettings()
    {
        var fields = new VideoTechnologyFields<EmulationLedMatrixVideoConfiguration>(this,
            s => s.LedMatrix, (s, c) => s with { LedMatrix = c });
        return VideoGroup(EmulationResourceKeys.VideoTechnologyLedMatrix,
            VideoGroup(EmulationResourceKeys.VideoLedMatrixGroupStructure,
                fields.Choice(Catalog.LedMatrixShape, Catalog.LedMatrixShapeResourceKeys, c => c.Shape, (c, v) => c with { Shape = v }),
                fields.Number(Catalog.LedMatrixCellSize, c => c.CellSize, (c, v) => c with { CellSize = v }),
                fields.Number(Catalog.LedMatrixCellGap, c => c.CellGap, (c, v) => c with { CellGap = v })),
            VideoGroup(EmulationResourceKeys.VideoLedMatrixGroupEmission,
                fields.Choice(Catalog.LedMatrixColor, Catalog.LedMatrixColorResourceKeys, c => c.Color, (c, v) => c with { Color = v }),
                fields.Number(Catalog.LedMatrixBrightness, c => c.Brightness, (c, v) => c with { Brightness = v }),
                fields.Number(Catalog.LedMatrixDiffusion, c => c.Diffusion, (c, v) => c with { Diffusion = v }),
                fields.Number(Catalog.LedMatrixHaloRadius, c => c.HaloRadius, (c, v) => c with { HaloRadius = v }),
                fields.Number(Catalog.LedMatrixBlackDepth, c => c.BlackDepth, (c, v) => c with { BlackDepth = v })))
            with { IsVisible = () => _video.VideoProcessing.DisplayTechnology == EmulationVideoDisplayTechnology.LedMatrix };
    }

    private Entry OsdDotMatrixSettings()
    {
        var fields = new VideoTechnologyFields<EmulationDotMatrixVideoConfiguration>(this,
            s => s.DotMatrix, (s, c) => s with { DotMatrix = c });
        return VideoGroup(EmulationResourceKeys.VideoTechnologyDotMatrix,
            VideoGroup(EmulationResourceKeys.VideoDotMatrixGroupCells,
                fields.Choice(Catalog.DotMatrixShape, Catalog.DotMatrixShapeResourceKeys, c => c.Shape, (c, v) => c with { Shape = v }),
                fields.Number(Catalog.DotMatrixCellSize, c => c.CellSize, (c, v) => c with { CellSize = v }),
                fields.Number(Catalog.DotMatrixDotSize, c => c.DotSize, (c, v) => c with { DotSize = v }),
                fields.Number(Catalog.DotMatrixCellGap, c => c.CellGap, (c, v) => c with { CellGap = v })),
            VideoGroup(EmulationResourceKeys.VideoDotMatrixGroupLight,
                fields.Choice(Catalog.DotMatrixPalette, Catalog.DotMatrixPaletteResourceKeys, c => c.Palette, (c, v) => c with { Palette = v }),
                fields.Number(Catalog.DotMatrixBrightness, c => c.Brightness, (c, v) => c with { Brightness = v }),
                fields.Number(Catalog.DotMatrixContrast, c => c.Contrast, (c, v) => c with { Contrast = v }),
                fields.Number(Catalog.DotMatrixHaloIntensity, c => c.HaloIntensity, (c, v) => c with { HaloIntensity = v })),
            VideoGroup(EmulationResourceKeys.VideoDotMatrixGroupTemporal,
                fields.Number(Catalog.DotMatrixResponseTime, c => c.ResponseTimeMilliseconds, (c, v) => c with { ResponseTimeMilliseconds = v }, EmulationVideoProcessingLimits.DurationMaximumMilliseconds),
                fields.Number(Catalog.DotMatrixPersistence, c => c.PersistenceMilliseconds, (c, v) => c with { PersistenceMilliseconds = v }, EmulationVideoProcessingLimits.DurationMaximumMilliseconds)))
            with { IsVisible = () => _video.VideoProcessing.DisplayTechnology == EmulationVideoDisplayTechnology.DotMatrix };
    }

    private Entry OsdSegmentDisplaySettings()
    {
        var fields = new VideoTechnologyFields<EmulationSegmentDisplayVideoConfiguration>(this,
            s => s.SegmentDisplay, (s, c) => s with { SegmentDisplay = c });
        return VideoGroup(EmulationResourceKeys.VideoTechnologySegmentDisplay,
            VideoGroup(EmulationResourceKeys.VideoSegmentDisplayGroupCells,
                fields.Choice(Catalog.SegmentDisplayLayout, Catalog.SegmentDisplayLayoutResourceKeys, c => c.Layout, (c, v) => c with { Layout = v }),
                fields.Number(Catalog.SegmentDisplayCellSize, c => c.CellSize, (c, v) => c with { CellSize = v }),
                fields.Number(Catalog.SegmentDisplayHorizontalGap, c => c.HorizontalGap, (c, v) => c with { HorizontalGap = v }),
                fields.Number(Catalog.SegmentDisplayVerticalGap, c => c.VerticalGap, (c, v) => c with { VerticalGap = v })),
            VideoGroup(EmulationResourceKeys.VideoSegmentDisplayGroupGeometry,
                fields.Number(Catalog.SegmentDisplayThickness, c => c.Thickness, (c, v) => c with { Thickness = v }),
                fields.Number(Catalog.SegmentDisplaySegmentGap, c => c.SegmentGap, (c, v) => c with { SegmentGap = v }),
                fields.Choice(Catalog.SegmentDisplayEndShape, Catalog.SegmentDisplayEndShapeResourceKeys, c => c.EndShape, (c, v) => c with { EndShape = v }),
                fields.Choice(Catalog.SegmentDisplayDecimalPoint, VideoToggleResourceKeys, c => c.DecimalPoint, (c, v) => c with { DecimalPoint = v }),
                fields.Choice(Catalog.SegmentDisplayColon, VideoToggleResourceKeys, c => c.Colon, (c, v) => c with { Colon = v })),
            VideoGroup(EmulationResourceKeys.VideoSegmentDisplayGroupEmission,
                fields.Choice(Catalog.SegmentDisplayColor, Catalog.SegmentDisplayColorResourceKeys, c => c.Color, (c, v) => c with { Color = v }),
                fields.Number(Catalog.SegmentDisplayBrightness, c => c.Brightness, (c, v) => c with { Brightness = v }),
                fields.Number(Catalog.SegmentDisplayActivationThreshold, c => c.ActivationThreshold, (c, v) => c with { ActivationThreshold = v }),
                fields.Number(Catalog.SegmentDisplayContrast, c => c.Contrast, (c, v) => c with { Contrast = v }),
                fields.Number(Catalog.SegmentDisplayOffSegmentVisibility, c => c.OffSegmentVisibility, (c, v) => c with { OffSegmentVisibility = v }),
                fields.Number(Catalog.SegmentDisplayBlackDepth, c => c.BlackDepth, (c, v) => c with { BlackDepth = v })),
            VideoGroup(EmulationResourceKeys.VideoSegmentDisplayGroupLightAndResponse,
                fields.Number(Catalog.SegmentDisplayGlow, c => c.Glow, (c, v) => c with { Glow = v }),
                fields.Number(Catalog.SegmentDisplayHaloRadius, c => c.HaloRadius, (c, v) => c with { HaloRadius = v }),
                fields.Number(Catalog.SegmentDisplayResponseTime, c => c.ResponseTimeMilliseconds, (c, v) => c with { ResponseTimeMilliseconds = v }, EmulationVideoProcessingLimits.DurationMaximumMilliseconds),
                fields.Number(Catalog.SegmentDisplayPersistence, c => c.PersistenceMilliseconds, (c, v) => c with { PersistenceMilliseconds = v }, EmulationVideoProcessingLimits.DurationMaximumMilliseconds)))
            with { IsVisible = () => _video.VideoProcessing.DisplayTechnology == EmulationVideoDisplayTechnology.SegmentDisplay };
    }

    private Entry OsdEPaperSettings()
    {
        var fields = new VideoTechnologyFields<EmulationEPaperVideoConfiguration>(this,
            s => s.EPaper, (s, c) => s with { EPaper = c });
        return VideoGroup(EmulationResourceKeys.VideoTechnologyEPaper,
            VideoGroup(EmulationResourceKeys.VideoEPaperGroupInkAndColor,
                fields.Choice(Catalog.EPaperColorMode, Catalog.EPaperColorModeResourceKeys, c => c.ColorMode, (c, v) => c with { ColorMode = v }),
                fields.Number(Catalog.EPaperInkDensity, c => c.InkDensity, (c, v) => c with { InkDensity = v }),
                fields.Number(Catalog.EPaperContrast, c => c.Contrast, (c, v) => c with { Contrast = v }),
                fields.Number(Catalog.EPaperDithering, c => c.Dithering, (c, v) => c with { Dithering = v }),
                fields.Number(Catalog.EPaperColorSaturation, c => c.ColorSaturation, (c, v) => c with { ColorSaturation = v })
                    with { IsVisible = () => _video.VideoProcessing.EPaper.ColorMode == EmulationEPaperColorMode.Color4096 }),
            VideoGroup(EmulationResourceKeys.VideoEPaperGroupPaperSurface,
                fields.Number(Catalog.EPaperPaperBrightness, c => c.PaperBrightness, (c, v) => c with { PaperBrightness = v }),
                fields.Number(Catalog.EPaperPaperWarmth, c => c.PaperWarmth, (c, v) => c with { PaperWarmth = v }),
                fields.Number(Catalog.EPaperSurfaceTexture, c => c.SurfaceTexture, (c, v) => c with { SurfaceTexture = v }),
                fields.Number(Catalog.EPaperEdgeSoftness, c => c.EdgeSoftness, (c, v) => c with { EdgeSoftness = v })),
            VideoGroup(EmulationResourceKeys.VideoEPaperGroupRefresh,
                fields.Number(Catalog.EPaperRefreshTime, c => c.RefreshTimeMilliseconds, (c, v) => c with { RefreshTimeMilliseconds = v }, EmulationVideoProcessingLimits.DurationMaximumMilliseconds),
                fields.Number(Catalog.EPaperGhosting, c => c.Ghosting, (c, v) => c with { Ghosting = v })))
            with { IsVisible = () => _video.VideoProcessing.DisplayTechnology == EmulationVideoDisplayTechnology.EPaper };
    }

    private Entry OsdProjectionSettings()
    {
        var fields = new VideoTechnologyFields<EmulationProjectionVideoConfiguration>(this,
            config => config.Projection, (config, projection) => config with { Projection = projection });
        return VideoGroup(EmulationResourceKeys.VideoTechnologyProjection,
            fields.Choice(Catalog.CrtColorStandards, Catalog.CrtColorStandardsResourceKeys,
                c => c.ColorStandards, (c, v) => c with { ColorStandards = v }),
            fields.Choice(Catalog.CrtFieldRate, Catalog.CrtFieldRateResourceKeys,
                c => c.FieldRate, (c, v) => c with { FieldRate = v }),
            ProjectionChoice(Catalog.LcdDefinition, Catalog.LcdDefinitionResourceKeys,
                c => c.Definition, (c, v) => c with { Definition = v }),
            ProjectionChoice(Catalog.ProjectionTechnology, Catalog.ProjectionTechnologyResourceKeys,
                c => c.Technology, (c, v) => c with { Technology = v }),
            ProjectionChoice(Catalog.ProjectionSurface, Catalog.ProjectionSurfaceResourceKeys,
                c => c.Surface, (c, v) => c with { Surface = v }),
            fields.Number(Catalog.ProjectionAmbientLight, c => c.AmbientLight,
                (c, v) => c with { AmbientLight = v }, EmulationProjectionVideoConfiguration.MaximumAmbientLight),
            fields.Choice(Catalog.CrtConnection, Catalog.ProjectionConnectionResourceKeys,
                c => c.Connection, (c, v) => c with { Connection = v }),
            fields.Number(Catalog.CrtConnectionDegradation, c => c.ConnectionDegradation,
                (c, v) => c with { ConnectionDegradation = v }, EmulationVideoProcessingLimits.CrtConnectionDegradationMaximum)
                with { IsVisible = () => _video.VideoProcessing.Projection.CanDegradeConnection() })
            with { IsVisible = () => _video.VideoProcessing.DisplayTechnology == EmulationVideoDisplayTechnology.Projection };
    }

    private Entry ProjectionChoice<T>(string label, IReadOnlyDictionary<T, string> keys,
        Func<EmulationProjectionVideoConfiguration, T> read,
        Func<EmulationProjectionVideoConfiguration, T, EmulationProjectionVideoConfiguration> update) where T : notnull =>
        VideoChoice(label, keys, config => read(config.Projection),
            (config, value) => config with { Projection = update(config.Projection, value) });

    private sealed class VideoTechnologyFields<T>(MachineController owner, Func<EmulationVideoProcessingConfiguration, T> read,
        Func<EmulationVideoProcessingConfiguration, T, EmulationVideoProcessingConfiguration> update)
    {
        internal Entry Number(string parameter, Func<T, int> get, Func<T, int, T> set,
            int maximum = EmulationVideoProcessingLimits.IntensityMaximum) =>
            owner.VideoNumber(Catalog.ParameterResourceKeys[parameter], settings => get(read(settings)),
                (settings, value) => update(settings, set(read(settings), value)),
                EmulationVideoProcessingLimits.IntensityMinimum, maximum);

        internal Entry Choice<TValue>(string parameter, IReadOnlyDictionary<TValue, string> keys,
            Func<T, TValue> get, Func<T, TValue, T> set) where TValue : notnull =>
            owner.VideoChoice(Catalog.ParameterResourceKeys[parameter], keys, settings => get(read(settings)),
                (settings, value) => update(settings, set(read(settings), value)));
    }
}
