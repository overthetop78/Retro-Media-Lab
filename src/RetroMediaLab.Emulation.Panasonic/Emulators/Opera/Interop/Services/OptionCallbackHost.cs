using RetroMediaLab.Emulation.Interfaces;
using CommonCoreHostFunctions = RetroMediaLab.Emulation.Panasonic.Common.Functions.CoreHostFunctions;
using RetroMediaLab.Emulation.Services;

namespace RetroMediaLab.Emulation.Panasonic.Emulators.Opera.Interop.Services;

internal static class OptionCallbackHost
{
    /// <summary>Crée le propriétaire partagé avec les descriptions et validations des options du backend.</summary>
    internal static IEmulationVariableOptionHost<CoreOption> Create(IReadOnlyDictionary<string, string> values) => CommonCoreHostFunctions.NativeResources.CreateVariableOptionHost<CoreOption>(values,
        static (key, name, defaultValue, current, choices) => new CoreOption(
                key,
                name,
                null,
                null,
                defaultValue,
                current,
                choices.Select(value => new CoreOptionValue(value, value)).ToArray(),
                IsVisible: key is not (RetroMediaLab.Emulation.Panasonic.Emulators.Opera.Constants.RuntimeOptionConstants.Bios or RetroMediaLab.Emulation.Panasonic.Emulators.Opera.Constants.RuntimeOptionConstants.Font)
            ),
        static (option, value) => option.Values.Any(choice => choice.Value == value),
        static (option, value) => option with { CurrentValue = value });
}
