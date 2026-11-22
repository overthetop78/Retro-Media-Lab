using RetroMediaLab.App.Constants.Input.Bindings;
using RetroMediaLab.App.Constants.Input.Controllers;
using RetroMediaLab.App.Functions.Input.Bindings;
using RetroMediaLab.App.Services.Input.GameInput;
using RetroMediaLab.App.Services.Input.Midi;
using RetroMediaLab.Emulation;

namespace RetroMediaLab.App.Controllers.Emulation.Input;

internal sealed class ControllerBindingCapture
{
    private IReadOnlyList<EmulationControllerState> _controllerBaseline = GameInputControllerReader.ReadAll();
    private IReadOnlyList<GameInputLiveState> _controllerDetailedBaseline = GameInputControllerReader.ReadAllDetailedStates();
    private readonly IReadOnlyList<EmulationControllerState> _controllerAxisBaseline;
    private readonly IReadOnlyList<GameInputLiveState> _controllerDetailedAxisBaseline;

    internal ControllerBindingCapture()
    {
        _controllerAxisBaseline = _controllerBaseline;
        _controllerDetailedAxisBaseline = _controllerDetailedBaseline;
    }

    internal void Poll(HashSet<string> pressedSources, HashSet<string> capturedSources,
        Func<string, bool> captureSource, Action<string> deviceCaptured)
    {
        var states = GameInputControllerReader.ReadAll();
        var detailedStates = GameInputControllerReader.ReadAllDetailedStates();
        foreach (var source in pressedSources.Where(source => source.StartsWith(InputBindingSyntaxConstants.ControllerPrefix, StringComparison.OrdinalIgnoreCase)
            && !MidiShortcutInput.IsMidiSource(source)).ToArray())
        {
            var device = EmulationSdk.InputProcessing.ParseControllerDeviceId(source);
            var state = EmulationSdk.InputProcessing.ResolveController(device, states, -1);
            var value = EmulationSdk.InputProcessing.ControllerSourceValue(source, state);
            var detailed = detailedStates.FirstOrDefault(item => item.DeviceId == device);
            var name = source[(source.LastIndexOf(':') + 1)..];
            if (detailed is not null && name.StartsWith("Button", StringComparison.OrdinalIgnoreCase) && int.TryParse(name[6..], out var index))
                value = detailed.Controls.FirstOrDefault(control => control.Type == GameInputControlType.Button && control.Index == index - (device?.StartsWith("rawgamecontroller:", StringComparison.OrdinalIgnoreCase) == true ? 1 : 0))?.Value ?? value;
            if (value < .2f) pressedSources.Remove(source);
        }
        foreach (var state in states)
        {
            var baselineState = _controllerBaseline.FirstOrDefault(item => string.Equals(item.DeviceId, state.DeviceId, StringComparison.OrdinalIgnoreCase)) ?? EmulationControllerState.Empty;
            var pressed = state.Buttons & ~baselineState.Buttons;
            if (pressed != 0)
            {
                foreach (var index in Enumerable.Range(0, ControllerInputConstants.ModernButtonSources.Length).Where(candidate => (pressed & (1u << candidate)) != 0))
                {
                    var source = ControllerInputConstants.ModernButtonSources[index][InputBindingSyntaxConstants.ControllerPrefix.Length..];
                    if (!ControllerBindingCaptureFunctions.ShouldCaptureStandardControl(state, source)) continue;
                    captureSource(InputBindingSyntax.Controller(state.DeviceId, source));
                    deviceCaptured(state.DeviceId);
                }
            }
            foreach (var generic in state.Controls.Where(control => !control.Key.StartsWith("Axis", StringComparison.OrdinalIgnoreCase) && control.Value > .5f && baselineState.Controls.GetValueOrDefault(control.Key) <= .5f))
            {
                captureSource(InputBindingSyntax.Controller(state.DeviceId, generic.Key));
                deviceCaptured(state.DeviceId);
            }
            var axisBaseline = _controllerAxisBaseline.FirstOrDefault(item => string.Equals(item.DeviceId, state.DeviceId, StringComparison.OrdinalIgnoreCase)) ?? EmulationControllerState.Empty;
            var genericAxis = ControllerBindingCaptureFunctions.NewlyMovedGenericAxis(state, axisBaseline);
            if (genericAxis is not null)
            {
                captureSource(InputBindingSyntax.Controller(state.DeviceId, genericAxis));
                deviceCaptured(state.DeviceId);
            }
            var direction = ControllerBindingCaptureFunctions.NewlyMovedDirection(state, baselineState);
            if (direction is null || !ControllerBindingCaptureFunctions.ShouldCaptureStandardControl(state, direction[InputBindingSyntaxConstants.ControllerPrefix.Length..])) continue;
            captureSource(InputBindingSyntax.Controller(state.DeviceId, direction[InputBindingSyntaxConstants.ControllerPrefix.Length..]));
            deviceCaptured(state.DeviceId);
        }
        foreach (var state in detailedStates)
        {
            if (capturedSources.Any(source => string.Equals(EmulationSdk.InputProcessing.ParseControllerDeviceId(source), state.DeviceId, StringComparison.OrdinalIgnoreCase))
                && states.Any(controller => controller.DeviceId == state.DeviceId && (controller.Buttons != 0 || controller.Controls.Count > 0))) continue;
            var baseline = _controllerDetailedBaseline.FirstOrDefault(item => string.Equals(item.DeviceId, state.DeviceId, StringComparison.OrdinalIgnoreCase)) ?? GameInputLiveState.Empty(state.DeviceId);
            var axisBaseline = _controllerDetailedAxisBaseline.FirstOrDefault(item => string.Equals(item.DeviceId, state.DeviceId, StringComparison.OrdinalIgnoreCase)) ?? GameInputLiveState.Empty(state.DeviceId);
            foreach (var source in ControllerBindingCaptureFunctions.NewlyActivatedDetailedControls(state, baseline, axisBaseline))
            {
                captureSource(InputBindingSyntax.Controller(state.DeviceId, source));
                deviceCaptured(state.DeviceId);
            }
        }
        _controllerBaseline = states;
        _controllerDetailedBaseline = detailedStates;
    }
}
