using CommonControllerFunctions = RetroMediaLab.Emulation.Apple.Common.Functions.ControllerFunctions;
using RetroMediaLab.Emulation.Apple.Emulators.Common.Interop.Constants;
using RetroMediaLab.Emulation.Apple.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Apple.Emulators.Common.Interop.Services;
using RetroMediaLab.Emulation.Interop;
using MiniVmacConstants = RetroMediaLab.Emulation.Apple.Emulators.MiniVmac.Constants.EmulatorConstants;

namespace RetroMediaLab.Emulation.Apple.Emulators.Common.Interop.Functions;

internal static class ControllerPortFunctions
{
    internal static void Configure(ExternalCoreExports exports, ExternalHostCallbacks callbacks, MachineConfiguration configuration)
    {
        if (configuration.Core == Emulator.MiniVmac)
            exports.SetControllerPortDevice(InputConstants.PrimaryPort, MiniVmacConstants.KeyboardDevice);
        else
            for (var port = ControllerPortConstants.MinimumControllerPort;
                 port < ConfigurationFunctions.ControllerPortCount(configuration.Model); port++)
                ConfigurePort(exports, callbacks, configuration, port, configuration.Input.Controllers?.FirstOrDefault(binding => binding.Port == port)?.Peripheral ?? PeripheralCategory.Automatic);
        callbacks.ConfigureInput(configuration.Input);
    }

    internal static void ConfigurePort(ExternalCoreExports exports, ExternalHostCallbacks callbacks, MachineConfiguration configuration, int port, PeripheralCategory peripheral)
    {
        if (port < ControllerPortConstants.MinimumControllerPort
            || port >= ConfigurationFunctions.ControllerPortCount(configuration.Model))
            throw new ArgumentOutOfRangeException(nameof(port), ErrorMessages.InvalidControllerPort);
        if (!CommonControllerFunctions.Peripherals(configuration.Model).Contains(peripheral))
            throw new ArgumentException(ErrorMessages.UnsupportedControllerDevice, nameof(peripheral));
        var device = peripheral switch
        {
            PeripheralCategory.None => CoreLifecycleConstants.NoDevice,
            PeripheralCategory.Mouse => InputConstants.MouseDevice,
            PeripheralCategory.Keyboard => InputConstants.KeyboardDevice,
            PeripheralCategory.Automatic or PeripheralCategory.AnalogJoystick => InputConstants.AnalogDevice,
            _ => throw new ArgumentOutOfRangeException(nameof(peripheral))
        };
        exports.SetControllerPortDevice(checked((uint)port), device);
    }
}
