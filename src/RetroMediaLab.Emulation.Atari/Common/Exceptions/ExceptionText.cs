using RetroMediaLab.Emulation.Interfaces;
using CommonCoreHostFunctions = RetroMediaLab.Emulation.Atari.Common.Functions.CoreHostFunctions;
using System.Globalization;
using RetroMediaLab.Emulation.Atari.Modules;

namespace RetroMediaLab.Emulation.Atari.Common.Exceptions;

internal static class ExceptionText
{
    private static readonly IEmulationModuleLocalization Localization = CommonCoreHostFunctions.CreateLocalization(typeof(AtariEmulationModule).Assembly, "RetroMediaLab.Emulation.Atari.Resources.Emulation");

    internal static string Get(string resourceKey, params object[] arguments)
    {
        var value = Localization.TryGetString(resourceKey, CultureInfo.CurrentUICulture, out var text) ? text : resourceKey;
        return arguments.Length == 0 ? value : string.Format(CultureInfo.CurrentCulture, value, arguments);
    }
}
