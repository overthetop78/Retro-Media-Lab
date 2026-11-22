using RetroMediaLab.App.Constants.Localization;

namespace RetroMediaLab.App.Dictionaries.Video;

internal static class VideoRestorationChoices
{
    internal static IReadOnlyDictionary<int, string> Dedithering { get; } = new Dictionary<int, string>
    {
        [0] = EmulationResourceKeys.VideoRestorationLevelNone,
        [33] = EmulationResourceKeys.VideoRestorationLevelLight,
        [67] = EmulationResourceKeys.VideoRestorationLevelMedium,
        [100] = EmulationResourceKeys.VideoRestorationLevelStrong
    };

    internal static int DeditheringIndex(int value) =>
        Math.Clamp((int)Math.Round(value / 100d * (Dedithering.Count - 1)), 0, Dedithering.Count - 1);

    internal static int NearestDeditheringValue(int value) => Dedithering.Keys.ElementAt(DeditheringIndex(value));
}
