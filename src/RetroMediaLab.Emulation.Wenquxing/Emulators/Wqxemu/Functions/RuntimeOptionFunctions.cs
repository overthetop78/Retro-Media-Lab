using RetroMediaLab.Emulation.Interfaces;
using CommonCoreHostFunctions = RetroMediaLab.Emulation.Wenquxing.Common.Functions.CoreHostFunctions;
using RetroMediaLab.Emulation.Wenquxing.Emulators.Wqxemu.Constants;
using RetroMediaLab.Emulation.Wenquxing.Emulators.Wqxemu.Dictionaries;

namespace RetroMediaLab.Emulation.Wenquxing.Emulators.Wqxemu.Functions;

internal static class RuntimeOptionFunctions
{
    internal static IEmulationVariableOptionHost<CoreOption> CreateCallbackHost(IReadOnlyDictionary<string, string> values) =>
        CommonCoreHostFunctions.NativeResources.CreateVariableOptionHost<CoreOption>(values,
        static (key, name, defaultValue, current, choices) => new CoreOption(key, name, null, null, defaultValue, current, choices.Select(value => new CoreOptionValue(value, value)).ToArray(), IsVisible: key != RetroMediaLab.Emulation.Wenquxing.Emulators.Wqxemu.Constants.RuntimeOptionConstants.Model),
        static (option, value) => option.Values.Any(choice => choice.Value == value),
        static (option, value) => option with { CurrentValue = value });

    internal static IReadOnlyDictionary<string, string> Prepare(MachineConfiguration configuration)
    {
        ConfigurationFunctions.Validate(configuration);
        return new Dictionary<string, string>(configuration.Options, StringComparer.Ordinal) { [RuntimeOptionConstants.Model] = ModelDictionary.NativeValues[configuration.Model].ToUpperInvariant() };
    }
}