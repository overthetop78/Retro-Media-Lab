using RetroMediaLab.App.Constants.Input.GameInput;
using RetroMediaLab.Emulation.Contracts;

namespace RetroMediaLab.App.Controllers.Emulation.Input;

internal sealed class OsdControllerReleaseFilter
{
    private readonly Dictionary<(string Device, int Index), EmulationControllerState> _held = [];

    internal void Clear() => _held.Clear();

    internal void Capture(IReadOnlyList<EmulationControllerState> controllers)
    {
        _held.Clear();
        for (var index = 0; index < controllers.Count; index++)
        {
            var controller = controllers[index];
            var held = StillHeld(controller, controller);
            if (HasInput(held)) _held[Identity(controller, index)] = held;
        }
    }

    internal IReadOnlyList<EmulationControllerState> Filter(IReadOnlyList<EmulationControllerState> controllers)
    {
        if (_held.Count == 0) return controllers;
        var output = controllers.ToArray();
        var connected = new HashSet<(string Device, int Index)>();
        for (var index = 0; index < output.Length; index++)
        {
            var controller = output[index];
            var id = Identity(controller, index);
            connected.Add(id);
            if (!_held.TryGetValue(id, out var previous)) continue;
            var held = StillHeld(previous, controller);
            if (!HasInput(held))
            {
                _held.Remove(id);
                continue;
            }
            _held[id] = held;
            output[index] = controller with
            {
                Buttons = controller.Buttons & ~held.Buttons,
                LeftX = held.LeftX == 0 ? controller.LeftX : (short)0,
                LeftY = held.LeftY == 0 ? controller.LeftY : (short)0,
                RightX = held.RightX == 0 ? controller.RightX : (short)0,
                RightY = held.RightY == 0 ? controller.RightY : (short)0,
                LeftTrigger = held.LeftTrigger == 0 ? controller.LeftTrigger : (short)0,
                RightTrigger = held.RightTrigger == 0 ? controller.RightTrigger : (short)0,
                Controls = held.Controls.Count == 0 ? controller.Controls : new EmulationControllerControls(
                    controller.Controls.Select(control => held.Controls.ContainsKey(control.Key)
                        ? new(control.Key, IsRawAxis(control.Key) ? .5f : 0f) : control))
            };
        }
        foreach (var id in _held.Keys.Where(id => !connected.Contains(id)).ToArray()) _held.Remove(id);
        return output;
    }

    private static EmulationControllerState StillHeld(EmulationControllerState previous, EmulationControllerState current) =>
        EmulationControllerState.Empty with
        {
            Buttons = previous.Buttons & current.Buttons,
            LeftX = HeldAxis(previous.LeftX, current.LeftX),
            LeftY = HeldAxis(previous.LeftY, current.LeftY),
            RightX = HeldAxis(previous.RightX, current.RightX),
            RightY = HeldAxis(previous.RightY, current.RightY),
            LeftTrigger = HeldAxis(previous.LeftTrigger, current.LeftTrigger),
            RightTrigger = HeldAxis(previous.RightTrigger, current.RightTrigger),
            Controls = new EmulationControllerControls(current.Controls.Where(control =>
                previous.Controls.TryGetValue(control.Key, out var value) && Direction(control.Key, control.Value) is var direction
                && direction != 0 && direction == Direction(control.Key, value)))
        };

    private static bool HasInput(EmulationControllerState state) => state.Buttons != 0 || state.Controls.Count != 0
        || state.LeftX != 0 || state.LeftY != 0 || state.RightX != 0 || state.RightY != 0
        || state.LeftTrigger != 0 || state.RightTrigger != 0;

    private static short HeldAxis(short previous, short current) =>
        Math.Abs((int)current) > short.MaxValue / 2 && Math.Sign(previous) == Math.Sign(current) ? current : (short)0;

    private static int Direction(string name, float value) => IsRawAxis(name)
        ? value > .75f ? 1 : value < .25f ? -1 : 0
        : value > .5f ? 1 : 0;

    private static bool IsRawAxis(string name) =>
        name.StartsWith(GameInputControllerMappingConstants.AxisControlPrefix, StringComparison.OrdinalIgnoreCase)
        && int.TryParse(name.AsSpan(GameInputControllerMappingConstants.AxisControlPrefix.Length), out _);

    private static (string Device, int Index) Identity(EmulationControllerState controller, int position) =>
        (controller.DeviceId, controller.DeviceId.Length == 0 ? position : -1);
}
