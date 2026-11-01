using CommonControllerFunctions = RetroMediaLab.Emulation.Bandai.Common.Functions.ControllerFunctions;
using RetroMediaLab.Emulation.Bandai.Emulators.Common.Interop.Constants;
using RetroMediaLab.Emulation.Bandai.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Bandai.Emulators.Common.Interop.Services;
namespace RetroMediaLab.Emulation.Bandai.Emulators.Common.Interop.Functions;

internal static class ControllerPortFunctions
{
    internal static void Configure(ExternalCoreExports exports, ExternalHostCallbacks callbacks, MachineConfiguration configuration)
    {
        ConfigurePort(exports, callbacks, configuration, ControllerConstants.FirstPort, configuration.Input.Controllers?.FirstOrDefault()?.Peripheral ?? PeripheralCategory.Automatic);
        callbacks.ConfigureInput(configuration.Input);
    }

    internal static void ConfigurePort(ExternalCoreExports exports, ExternalHostCallbacks callbacks, MachineConfiguration configuration, int port, PeripheralCategory peripheral)
    {
        if (port != ControllerConstants.FirstPort)
            throw new ArgumentOutOfRangeException(nameof(port), ErrorMessages.InvalidControllerPort);
        if (!CommonControllerFunctions.Peripherals(configuration.Model).Contains(peripheral))
            throw new ArgumentException(ErrorMessages.UnsupportedControllerDevice, nameof(peripheral));
        exports.SetControllerPortDevice(checked((uint)port), peripheral == PeripheralCategory.None ? CoreLifecycleConstants.NoDevice : InputConstants.JoypadDevice);
    }
}
