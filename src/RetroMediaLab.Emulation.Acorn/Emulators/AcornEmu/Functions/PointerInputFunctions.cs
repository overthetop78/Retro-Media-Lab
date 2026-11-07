using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants.UserPortPointerConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Functions;

internal static class PointerInputFunctions
{
    internal static bool HasConnector(MachineConfiguration configuration) => configuration.Model is
        MachineModel.BbcMicroModelB or MachineModel.BbcMicroModelBPlus or MachineModel.BbcMicroModelBPlus128
        or MachineModel.BbcMaster128 or MachineModel.BbcMasterCompact || ModelAIoFunctions.UserPort(configuration);

    internal static bool Connected(MachineConfiguration configuration) => HasConnector(configuration)
        && bool.Parse(configuration.Options.GetValueOrDefault(C.ConnectedOption, bool.FalseString));

    internal static void Validate(MachineConfiguration configuration)
    {
        if (configuration.Options.TryGetValue(C.ConnectedOption, out var text)
            && (!bool.TryParse(text, out var connected) || connected && !HasConnector(configuration)))
            throw new ArgumentException(ErrorMessages.OptionValueInvalidFormat, nameof(configuration));
    }

    internal static IEnumerable<EmulationSettingsBlock> Describe(MachineConfiguration configuration)
    {
        if (!HasConnector(configuration)) yield break;
        yield return new(C.ConnectedOption, EmulationMachineTab.Mouse, C.Group,
        [new(C.ConnectedOption, EmulationMachineTab.Mouse, C.ConnectedOption, C.EnabledLabel,
            EmulationSettingsEditor.Toggle, Connected(configuration).ToString(), EnabledValue: bool.TrueString,
            DisabledValue: bool.FalseString, RequiresRestart: true, RefreshSettingsOnChange: true)]);
    }
}
