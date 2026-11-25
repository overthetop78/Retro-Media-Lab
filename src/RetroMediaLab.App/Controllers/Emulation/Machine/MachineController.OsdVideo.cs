using System.Globalization;
using RetroMediaLab.App.Constants.Localization;
using RetroMediaLab.App.Constants.Machine;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Dictionaries.Video;
using RetroMediaLab.App.Functions.Emulation.Machine;
using RetroMediaLab.App.Services.Emulation;
using RetroMediaLab.Emulation;
using RetroMediaLab.VideoPresentation.Services;
using RetroMediaLab.VideoPresentation.Windows.Osd;
using ImageControls = RetroMediaLab.VideoPresentation.Enums.EmulationImageAdjustmentControls;
using Entry = RetroMediaLab.VideoPresentation.Windows.Osd.FullscreenOsdWindow.Entry;

namespace RetroMediaLab.App.Controllers.Emulation.Machine;

internal sealed partial class MachineController
{
    private Entry OsdVideoSettings() =>
        new(LocExtension.Get("Emulation.Shortcut.OsdVideoSettings"), Children: EditableVideoRows(
        [
            VideoGroup(EmulationResourceKeys.VideoRetroMediaLabProcessing, OsdRendererChoice()),
            .. Enum.GetValues<EmulationVideoScreenType>().Select(OsdScreenAdjustments),
            VideoGroup(EmulationResourceKeys.VideoRetroMediaLabProcessing,
                VideoChoice(EmulationResourceKeys.VideoTechnology, EmulationVideoProcessingCatalog.ScreenTypeResourceKeys,
                    EmulationVideoProcessingConfigurationFunctions.ScreenType,
                    EmulationVideoProcessingConfigurationFunctions.WithScreenType)),
            OsdCrtSettings(),
            OsdLcdSettings(led: false),
            OsdLcdSettings(led: true),
            OsdOledSettings(),
            OsdPlasmaSettings(),
            OsdVectorSettings(),
            OsdVfdSettings(),
            OsdLedMatrixSettings(),
            OsdDotMatrixSettings(),
            OsdSegmentDisplaySettings(),
            OsdEPaperSettings(),
            OsdProjectionSettings(),
            VideoGroup(EmulationResourceKeys.VideoRetroMediaLabProcessing,
                VideoChoice(EmulationResourceKeys.VideoSampling, EmulationVideoProcessingCatalog.SamplingResourceKeys,
                    settings => settings.Sampling, (settings, value) => settings with { Sampling = value })
                    with { IsVisible = () => EmulationVideoProcessingConfigurationFunctions.UsesHostDisplay(_video.VideoProcessing) }),
            .. OsdModuleGroups(EmulationMachineTab.Video, EmulationVideoSettingsSection.Display),
            VideoGroup(EmulationResourceKeys.VideoRestorationSettings,
                    VideoChoice(EmulationResourceKeys.VideoParameterDedithering, VideoRestorationChoices.Dedithering,
                        settings => VideoRestorationChoices.NearestDeditheringValue(settings.Restoration.Dedithering),
                        (settings, draft) => settings with { Restoration = settings.Restoration with { Dedithering = draft } })
                        with { IsVisible = () => EmulationVideoProcessingConfigurationFunctions.SupportsDedithering(_video.VideoProcessing) },
                    RestorationIntensity(EmulationResourceKeys.VideoParameterDebanding, value => value.Debanding,
                        (value, draft) => value with { Debanding = draft })
                        with { IsVisible = () => EmulationVideoProcessingConfigurationFunctions.UsesHostDisplay(_video.VideoProcessing) },
                    RestorationIntensity(EmulationResourceKeys.VideoParameterFineDetails, value => value.DetailRecovery,
                        (value, draft) => value with { DetailRecovery = draft })
                        with { IsVisible = () => EmulationVideoProcessingConfigurationFunctions.UsesHostDisplay(_video.VideoProcessing) },
                    VideoChoice(EmulationResourceKeys.VideoParameterDeinterlacing, EmulationVideoProcessingCatalog.DeinterlacingResourceKeys,
                        settings => settings.Restoration.Deinterlacing,
                        (settings, draft) => settings with { Restoration = settings.Restoration with { Deinterlacing = draft } })
                        with { IsVisible = () => EmulationVideoProcessingConfigurationFunctions.SupportsDeinterlacing(_video.VideoProcessing) }),
            .. OsdModuleGroups(EmulationMachineTab.Video, EmulationVideoSettingsSection.Image),
            .. OsdModuleGroups(EmulationMachineTab.Video, EmulationVideoSettingsSection.Effects)
        ]), Glyph: MachineCommandGlyphConstants.Renderer, SeparatorBefore: true, FullHeight: true);

    internal static Entry[] EditableVideoRows(IReadOnlyList<Entry> groups)
    {
        var rows = new List<Entry>();
        AddRows(groups, null, null);
        return rows.ToArray();

        void AddRows(IReadOnlyList<Entry> entries, Entry? section, Func<bool>? parentVisible)
        {
            foreach (var entry in entries)
            {
                bool Visible() => parentVisible?.Invoke() != false && entry.IsVisible?.Invoke() != false;
                if (entry.Children is { } children)
                    AddRows(children, entry, Visible);
                else if (entry.Value is not null)
                    rows.Add(entry with
                    {
                        Section = section?.Label,
                        IsVisible = () => Visible() && entry.IsEnabled?.Invoke() != false
                    });
            }
        }
    }

    private static Entry VideoGroup(string resourceKey, params Entry[] children) =>
        new(LocExtension.Get(resourceKey), Children: children, FullHeight: true);

    private Entry RestorationIntensity(string label, Func<EmulationImageRestorationConfiguration, int> read,
        Func<EmulationImageRestorationConfiguration, int, EmulationImageRestorationConfiguration> update) =>
        VideoNumber(label, settings => read(settings.Restoration),
            (settings, value) => settings with { Restoration = update(settings.Restoration, value) },
            EmulationVideoProcessingLimits.IntensityMinimum, EmulationVideoProcessingLimits.IntensityMaximum);

    private Entry VideoNumber(string label, Func<EmulationVideoProcessingConfiguration, int> read,
        Func<EmulationVideoProcessingConfiguration, int, EmulationVideoProcessingConfiguration> update,
        int minimum, int maximum, bool signed = false, Func<int, string>? format = null, bool showSlider = true)
    {
        EmulationVideoProcessingConfiguration WithValue(int value) => update(_video.VideoProcessing, value);
        return new(LocExtension.Get(label), Value: new(
            () => read(_video.VideoProcessing),
            format ?? (value => value.ToString(signed ? "+0;-0;0" : "0", CultureInfo.CurrentCulture)),
            (value, direction) => Math.Clamp(value + direction, minimum, maximum),
            value => _video.SetVideoProcessing(WithValue(value)),
            value => SaveOsdVideoProfileAsync(profile => profile with
            {
                Processing = update(profile.Processing ?? new EmulationVideoProcessingConfiguration(), value)
            }),
            Range: showSlider ? (minimum, maximum) : null));
    }

    private Entry OsdScreenAdjustments(EmulationVideoScreenType screen) =>
        VideoGroup(EmulationResourceKeys.VideoGeneralSettings,
            Adjustment(screen, ImageControls.Brightness, EmulationResourceKeys.VideoParameterBrightness, value => value.Brightness,
                (value, draft) => value with { Brightness = draft }),
            Adjustment(screen, ImageControls.Contrast, EmulationResourceKeys.VideoParameterContrast, value => value.Contrast,
                (value, draft) => value with { Contrast = draft }),
            Adjustment(screen, ImageControls.Saturation, EmulationResourceKeys.VideoParameterSaturation, value => value.Saturation,
                (value, draft) => value with { Saturation = draft }, EmulationImageAdjustmentFunctions.FormatSaturation),
            Adjustment(screen, ImageControls.Sharpness, EmulationResourceKeys.VideoParameterSharpness, value => value.Sharpness,
                (value, draft) => value with { Sharpness = draft }),
            OsdGamma(screen))
        with { IsVisible = () => EmulationVideoProcessingConfigurationFunctions.ScreenType(_video.VideoProcessing) == screen };

    private Entry OsdGamma(EmulationVideoScreenType screen)
    {
        var curves = EmulationImageAdjustmentFunctions.GammaCurves.ToArray();
        var original = LocExtension.Get(EmulationResourceKeys.VideoGammaOriginal);
        return VideoNumber(EmulationResourceKeys.VideoParameterGamma,
            settings => Math.Max(0, Array.IndexOf(curves,
                EmulationVideoProcessingConfigurationFunctions.ImageAdjustments(settings, screen).Gamma)),
            (settings, index) => EmulationVideoProcessingConfigurationFunctions.WithImageAdjustments(settings, screen,
                adjustments => adjustments with { Gamma = curves[index] }),
            0, curves.Length - 1,
            format: index => EmulationImageAdjustmentFunctions.FormatGamma(curves[index], original), showSlider: false)
            with { IsVisible = () => ShowsImageControl(ImageControls.Gamma) };
    }

    private Entry Adjustment(EmulationVideoScreenType screen, ImageControls control, string label, Func<EmulationImageAdjustments, int> read,
        Func<EmulationImageAdjustments, int, EmulationImageAdjustments> update, Func<int, string>? format = null) =>
        VideoNumber(label, settings => read(EmulationVideoProcessingConfigurationFunctions.ImageAdjustments(settings, screen)),
            (settings, value) => EmulationVideoProcessingConfigurationFunctions.WithImageAdjustments(settings, screen,
                adjustments => update(adjustments, value)),
            EmulationVideoProcessingLimits.AdjustmentMinimum, EmulationVideoProcessingLimits.AdjustmentMaximum,
            format: format ?? EmulationImageAdjustmentFunctions.FormatSigned)
            with { IsVisible = () => ShowsImageControl(control) };

    private bool ShowsImageControl(ImageControls control) =>
        EmulationVideoProcessingConfigurationFunctions.AdjustmentControls(_video.VideoProcessing).HasFlag(control);

    private FullscreenOsdWindow.Entry VideoChoice<T>(string label, IReadOnlyDictionary<T, string> keys,
        Func<EmulationVideoProcessingConfiguration, T> read, Func<EmulationVideoProcessingConfiguration, T, EmulationVideoProcessingConfiguration> update)
        where T : notnull =>
        OsdVideoChoice(label, keys, () => read(_video.VideoProcessing),
            value => SaveOsdVideoAsync(update(_video.VideoProcessing, value)));

    private static Entry OsdVideoChoice<T>(string label, IReadOnlyDictionary<T, string> keys,
        Func<T> read, Func<T, Task> save)
        where T : notnull
    {
        var choices = keys.Keys.ToArray();
        return new(LocExtension.Get(label), Value: new(
            () => Math.Max(0, Array.IndexOf(choices, read())),
            index => LocExtension.Get(keys[choices[index]]),
            (index, direction) => Math.Clamp(index + direction, 0, choices.Length - 1),
            Preview: null,
            Save: index => save(choices[index])));
    }

    private async Task SaveOsdVideoAsync(EmulationVideoProcessingConfiguration settings)
    {
        await SaveOsdVideoProfileAsync(profile => profile with { Processing = settings });
        if (!_disposed) _video.SetVideoProcessing(settings);
    }

    private Entry OsdRendererChoice() =>
        OsdVideoChoice(EmulationResourceKeys.VideoRenderingSettings, EmulationVideoProcessingCatalog.RendererResourceKeys,
            () => EmulationVideoPresentationProfiles.Store.Get(_options.ModuleId, _options.ConfigurationId).Renderer,
            SaveOsdRendererAsync);

    private async Task SaveOsdRendererAsync(EmulationVideoRenderer renderer)
    {
        await SaveOsdVideoProfileAsync(profile => profile with { Renderer = renderer });
        if (_disposed) return;
        ApplyVideoRenderer(renderer);
        if (_options.Module is IEmulationVideoSettingsManager && _osdConfiguration is { } configuration)
        {
            var module = _options.Module;
            var updated = EmulationVideoConfigurationFunctions.Apply(module, configuration, renderer);
            await ApplyConfigurationAsync(updated);
            await ApplyRuntimeOptionsAsync(module.RuntimeOptions(updated));
        }
    }

    private Task SaveOsdVideoProfileAsync(Func<EmulationVideoPresentationProfile, EmulationVideoPresentationProfile> update) =>
        SaveOsdVideoProfileAsync(EmulationVideoPresentationProfiles.Store, _options.ModuleId, _options.ConfigurationId, update);

    internal static async Task SaveOsdVideoProfileAsync(VideoPresentationProfileStore store, string moduleId,
        Guid configurationId, Func<EmulationVideoPresentationProfile, EmulationVideoPresentationProfile> update)
    {
        var previous = store.Get(moduleId, configurationId);
        var next = update(previous);
        if (next == previous) return;
        store.Set(moduleId, configurationId, next);
        try { await store.SaveAsync(moduleId, configurationId); }
        catch
        {
            store.Set(moduleId, configurationId, previous);
            throw;
        }
    }
}
