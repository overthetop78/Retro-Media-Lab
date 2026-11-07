using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.ModelA.Constants.ModelAIoConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Functions;

/// <summary>Équipements enfichés sur les emplacements réels de la carte Model A.</summary>
internal static class ModelAIoFunctions
{
    internal static bool IsModel(MachineConfiguration configuration) => configuration.Model == MachineModel.BbcMicroModelA;
    internal static bool UserPort(MachineConfiguration configuration) => IsModel(configuration) && Enabled(configuration, C.UserPort);
    internal static bool AnalogPort(MachineConfiguration configuration) => IsModel(configuration) && Enabled(configuration, C.AnalogPort);
    private static bool Enabled(MachineConfiguration configuration, string key) => bool.Parse(configuration.Options.GetValueOrDefault(key, bool.FalseString));

    internal static void Validate(MachineConfiguration configuration)
    {
        foreach (var key in new[] { C.UserPort, C.AnalogPort })
            if (configuration.Options.TryGetValue(key, out var value)
                && (!bool.TryParse(value, out var enabled) || enabled && !IsModel(configuration)))
                throw new ArgumentException(ErrorMessages.OptionValueInvalidFormat, nameof(configuration));
    }

    internal static IEnumerable<EmulationSettingsBlock> Describe(MachineConfiguration configuration)
    {
        if (!IsModel(configuration)) yield break;
        yield return new(C.Group, EmulationMachineTab.Extensions, C.Group,
        [Field(C.UserPort, C.UserPortLabel), Field(C.AnalogPort, C.AnalogPortLabel)]);

        EmulationSettingsField Field(string key, string label) => new(key, EmulationMachineTab.Extensions, C.Group,
            label, EmulationSettingsEditor.Toggle, Enabled(configuration, key).ToString(),
            EnabledValue: bool.TrueString, DisabledValue: bool.FalseString, RequiresRestart: true, RefreshSettingsOnChange: true);
    }
}
