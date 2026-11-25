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
            config => config.Vector, (config, vector) => config with { Vector = vector });
        return VideoGroup("Emulation.Video.Screen.Vector",
            VideoChoice(EmulationResourceKeys.VideoTechnology, Catalog.VectorPhosphorResourceKeys,
                config => config.Vector.PhosphorColor,
                (config, value) => config with { Vector = config.Vector with { PhosphorColor = value } }),
            VideoNumber(Catalog.ParameterResourceKeys[Catalog.VectorBeamWidth], config => config.Vector.BeamWidth,
                (config, value) => config with { Vector = config.Vector with { BeamWidth = value } },
                EmulationVectorVideoConfiguration.MinimumBeamWidth, EmulationVectorVideoConfiguration.MaximumBeamWidth),
            fields.Number(Catalog.CrtCurvature, c => c.Curvature, (c, v) => c with { Curvature = v },
                EmulationVideoProcessingLimits.CrtCurvatureMaximum))
            with { IsVisible = () => _video.VideoProcessing.DisplayTechnology == EmulationVideoDisplayTechnology.Vector };
    }

    private Entry OsdVfdSettings()
    {
        var fields = new VideoTechnologyFields<EmulationVfdVideoConfiguration>(this,
            s => s.Vfd, (s, c) => s with { Vfd = c });
        return VideoGroup(EmulationResourceKeys.VideoTechnologyVfd,
            fields.Choice(Catalog.VfdColor, Catalog.VfdColorResourceKeys, c => c.Color, (c, v) => c with { Color = v }),
            fields.Choice(Catalog.VfdDefinition, Catalog.LedMatrixDefinitionResourceKeys, c => c.Definition, (c, v) => c with { Definition = v }),
            fields.Choice(Catalog.VfdShape, Catalog.DotMatrixShapeResourceKeys, c => c.Shape, (c, v) => c with { Shape = v }))
            with { IsVisible = () => _video.VideoProcessing.DisplayTechnology == EmulationVideoDisplayTechnology.Vfd };
    }

    private Entry OsdLedMatrixSettings()
    {
        var fields = new VideoTechnologyFields<EmulationLedMatrixVideoConfiguration>(this,
            s => s.LedMatrix, (s, c) => s with { LedMatrix = c });
        return VideoGroup(EmulationResourceKeys.VideoTechnologyLedMatrix,
            fields.Choice(Catalog.LedMatrixColor, Catalog.LedMatrixColorResourceKeys, c => c.Color, (c, v) => c with { Color = v }),
            fields.Choice(Catalog.LedMatrixDefinition, Catalog.LedMatrixDefinitionResourceKeys, c => c.Definition, (c, v) => c with { Definition = v }),
            fields.Choice(Catalog.LedMatrixShape, Catalog.LedMatrixShapeResourceKeys, c => c.Shape, (c, v) => c with { Shape = v }))
            with { IsVisible = () => _video.VideoProcessing.DisplayTechnology == EmulationVideoDisplayTechnology.LedMatrix };
    }

    private Entry OsdDotMatrixSettings()
    {
        var fields = new VideoTechnologyFields<EmulationDotMatrixVideoConfiguration>(this,
            s => s.DotMatrix, (s, c) => s with { DotMatrix = c });
        return VideoGroup(EmulationResourceKeys.VideoTechnologyDotMatrix,
            fields.Choice(Catalog.DotMatrixPalette, Catalog.DotMatrixPaletteResourceKeys, c => c.Palette, (c, v) => c with { Palette = v }),
            fields.Choice(Catalog.DotMatrixDefinition, Catalog.LedMatrixDefinitionResourceKeys, c => c.Definition, (c, v) => c with { Definition = v }),
            fields.Choice(Catalog.DotMatrixShape, Catalog.DotMatrixShapeResourceKeys, c => c.Shape, (c, v) => c with { Shape = v }),
            fields.Choice(Catalog.DotMatrixTechnology, Catalog.DotMatrixTechnologyResourceKeys, c => c.Technology, (c, v) => c with { Technology = v }))
            with { IsVisible = () => _video.VideoProcessing.DisplayTechnology == EmulationVideoDisplayTechnology.DotMatrix };
    }

    private Entry OsdSegmentDisplaySettings()
    {
        var fields = new VideoTechnologyFields<EmulationSegmentDisplayVideoConfiguration>(this,
            s => s.SegmentDisplay, (s, c) => s with { SegmentDisplay = c });
        Entry Color(EmulationSegmentDisplayTechnology technology) =>
            fields.Choice(Catalog.SegmentDisplayColor, Catalog.SegmentColors(technology), c => c.Color, (c, v) => c with { Color = v })
                with { IsVisible = () => _video.VideoProcessing.SegmentDisplay.Technology == technology };
        return VideoGroup(EmulationResourceKeys.VideoTechnologySegmentDisplay,
            fields.Choice(Catalog.SegmentDisplayLayout, Catalog.SegmentDisplayLayoutResourceKeys, c => c.Layout, (c, v) => c with { Layout = v }),
            fields.Choice(Catalog.SegmentDisplayTechnology, Catalog.SegmentDisplayTechnologyResourceKeys, c => c.Technology, (c, v) => c with { Technology = v }),
            Color(EmulationSegmentDisplayTechnology.Lcd), Color(EmulationSegmentDisplayTechnology.Led), Color(EmulationSegmentDisplayTechnology.Vfd))
            with { IsVisible = () => _video.VideoProcessing.DisplayTechnology == EmulationVideoDisplayTechnology.SegmentDisplay };
    }

    private Entry OsdEPaperSettings()
    {
        var fields = new VideoTechnologyFields<EmulationEPaperVideoConfiguration>(this,
            s => s.EPaper, (s, c) => s with { EPaper = c });
        return VideoGroup(EmulationResourceKeys.VideoTechnologyEPaper,
            fields.Choice(Catalog.EPaperColorMode, Catalog.EPaperColorModeResourceKeys, c => c.ColorMode, (c, v) => c with { ColorMode = v }),
            fields.Choice(Catalog.EPaperDefinition, Catalog.LcdDefinitionResourceKeys, c => c.Definition, (c, v) => c with { Definition = v }))
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
