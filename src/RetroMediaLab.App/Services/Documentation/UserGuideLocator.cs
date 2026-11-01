using RetroMediaLab.App.Functions.Localization;

namespace RetroMediaLab.App.Services.Documentation;

internal static class UserGuideLocator
{
    internal const string WikiUrl = "https://github.com/overthetop78/Retro-Media-Lab/wiki";

    internal static string GetUrl(string cultureName)
    {
        var language = UiLanguageResolver.Resolve(cultureName, null);
        return $"{WikiUrl}/{language}-Guide";
    }
}
