using RetroMediaLab.Emulation.Sony.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Sony.Emulators.Common.Interop.Factories;
using RetroMediaLab.Emulation.Sony.Emulators.BeetlePsxHw.Constants;
using ToggleValues = RetroMediaLab.Emulation.Sony.Emulators.Common.Interop.Constants.ToggleValueConstants;

namespace RetroMediaLab.Emulation.Sony.Emulators.BeetlePsxHw.Factories;

internal sealed class BeetlePsxHwMachineFactory : CoreMachineFactory
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.BeetlePsxHw;
    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
    public override int GetControllerPortCount(MachineConfiguration configuration) => base.GetControllerPortCount(configuration) + AdditionalControllers(OptionConstants.EnableMultitapPort1Key, configuration) + AdditionalControllers(OptionConstants.EnableMultitapPort2Key, configuration);

    private static int AdditionalControllers(string key, MachineConfiguration configuration) => configuration.Options?.GetValueOrDefault(key) == ToggleValues.Enabled ? ControllerPortConstants.AdditionalControllersPerMultitap : ControllerPortConstants.NoAdditionalControllers;
}
