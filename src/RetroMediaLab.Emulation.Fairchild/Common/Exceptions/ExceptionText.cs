using RetroMediaLab.Emulation.Interfaces;
using CommonCoreHostFunctions = RetroMediaLab.Emulation.Fairchild.Common.Functions.CoreHostFunctions;
using System.Globalization;


namespace RetroMediaLab.Emulation.Fairchild.Common.Exceptions;

internal static class ExceptionText
{
    private static readonly IEmulationModuleLocalization Localization = CommonCoreHostFunctions.CreateLocalization(typeof(ExceptionText).Assembly, "RetroMediaLab.Emulation.Fairchild.Resources.Emulation");

    internal static string Get(string resourceKey, params object[] arguments)
    {
        var value = Localization.TryGetString(resourceKey, CultureInfo.CurrentUICulture, out var text) ? text : resourceKey;
        return arguments.Length == 0 ? value : string.Format(CultureInfo.CurrentCulture, value, arguments);
    }
}
