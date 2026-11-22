using RetroMediaLab.App.Functions.Input.Controllers;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Services.Input.GameInput;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Constants;
using RetroMediaLab.Emulation.Enums;

namespace RetroMediaLab.App.Functions.Emulation.Controllers;

internal static class ControllerOutputChoices
{
    internal sealed record RumbleDeviceChoice(string? DeviceId, string Label);
    internal sealed record OutputChoice(EmulationControllerOutputBinding? Binding, string Label);

    internal static void AssignRumbleDevice(string? deviceId, IReadOnlyList<EmulationControllerOutputDefinition> definitions, Dictionary<string, EmulationControllerOutputBinding> bindings)
    {
        foreach (var definition in definitions)
            if (deviceId is null) bindings.Remove(definition.Id);
            else bindings[definition.Id] = new(deviceId, definition.Id == EmulationControllerFeedbackCommands.Strong
                ? EmulationControllerMotor.LowFrequency : EmulationControllerMotor.HighFrequency);
    }

    internal static IReadOnlyList<RumbleDeviceChoice> RumbleDevices(IGameInputControllerSource source, string? selectedDevice)
    {
        source.StartMonitoring();
        var choices = new List<RumbleDeviceChoice> { new(null, LocExtension.Get("Controllers.None")) };
        var devices = source.GetConnectedDevices();
        for (var index = 0; index < devices.Count; index++)
            if ((devices[index].RumbleMotors & (GameInputRumbleMotors.LowFrequency | GameInputRumbleMotors.HighFrequency)) != 0)
                choices.Add(new(devices[index].Id, devices[index].GameInputDisplayName + " #" + (index + 1)));
        if (selectedDevice is not null && choices.All(choice => choice.DeviceId != selectedDevice))
            choices.Add(new(selectedDevice, selectedDevice));
        return choices;
    }

    internal static IReadOnlyList<OutputChoice> Motors(IGameInputControllerSource source, EmulationControllerOutputBinding? selected)
    {
        source.StartMonitoring();
        var choices = new List<OutputChoice> { new(null, LocExtension.Get("Controllers.None")) };
        var devices = source.GetConnectedDevices();
        for (var index = 0; index < devices.Count; index++)
        {
            var device = devices[index];
            foreach (var motor in Enum.GetValues<EmulationControllerMotor>())
                if (device.RumbleMotors.HasFlag(ControllerFeedbackFunctions.PhysicalMotor(motor)))
                    choices.Add(new(new(device.Id, motor), $"{device.GameInputDisplayName} #{index + 1} · {GameInputDisplayFormatter.EnumValue(ControllerFeedbackFunctions.PhysicalMotor(motor))}"));
        }
        if (selected is not null && !choices.Any(choice => choice.Binding == selected))
            choices.Add(new(selected, $"{selected.DeviceId} · {GameInputDisplayFormatter.EnumValue(ControllerFeedbackFunctions.PhysicalMotor(selected.Motor))}"));
        return choices;
    }
}
