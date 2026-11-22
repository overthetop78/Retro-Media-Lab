using RetroMediaLab.App.Services.Input.GameInput;
using RetroMediaLab.Emulation;

namespace RetroMediaLab.App.Functions.Input.Bindings;

internal static class ControllerBindingCaptureFunctions
{
    internal static bool ShouldCaptureStandardControl(EmulationControllerState state, string source)
    {
        // Raw controls are the canonical identities when the same device also
        // exposes a mapped gamepad view. System buttons have no raw equivalent.
        if (source is "XboxButton" or "Share") return true;
        if (source.StartsWith("DPad", StringComparison.Ordinal) && state.Controls.Keys.Any(key => key.StartsWith("Switch", StringComparison.OrdinalIgnoreCase))) return false;
        var prefix = source.EndsWith("Trigger", StringComparison.Ordinal) || source.Contains("Stick", StringComparison.Ordinal) && !source.EndsWith("Click", StringComparison.Ordinal) ? "Axis" : "Button";
        return !state.Controls.Keys.Any(key => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    internal static IEnumerable<string> NewlyActivatedDetailedControls(GameInputLiveState current, GameInputLiveState baseline, GameInputLiveState? axisBaseline = null)
    {
        var rawController = current.DeviceId.StartsWith("rawgamecontroller:", StringComparison.OrdinalIgnoreCase);
        foreach (var control in current.Controls.Where(item => item.Type == GameInputControlType.Button))
        {
            var previous = baseline.Controls.FirstOrDefault(item => item.Type == control.Type && item.Index == control.Index)?.Value ?? 0f;
            if (control.Value >= .5f && previous < .5f)
                yield return $"Button{control.Index + (rawController ? 1 : 0)}";
        }
        foreach (var control in current.Controls.Where(item => item.Type == GameInputControlType.Axis))
        {
            var previous = (axisBaseline ?? baseline).Controls.FirstOrDefault(item => item.Type == control.Type && item.Index == control.Index)?.Value ?? .5f;
            var delta = control.Value - previous;
            if (delta >= .35f) yield return $"Axis{control.Index}Positive";
            if (delta <= -.35f) yield return $"Axis{control.Index}Negative";
        }
    }

    internal static string? NewlyMovedGenericAxis(EmulationControllerState current, EmulationControllerState baseline)
    {
        const float threshold = .35f;
        foreach (var control in current.Controls.Where(item => item.Key.StartsWith("Axis", StringComparison.OrdinalIgnoreCase)))
        {
            var previous = baseline.Controls.GetValueOrDefault(control.Key, .5f);
            var delta = control.Value - previous;
            if (delta >= threshold) return control.Key + "Positive";
            if (delta <= -threshold) return control.Key + "Negative";
        }
        return null;
    }

    internal static string? NewlyMovedDirection(EmulationControllerState current, EmulationControllerState baseline)
    {
        const short threshold = 14000;
        return Moved(current.LeftX, baseline.LeftX, -threshold) ? "Controller:LeftStickLeft"
            : Moved(current.LeftX, baseline.LeftX, threshold) ? "Controller:LeftStickRight"
            : Moved(current.LeftY, baseline.LeftY, -threshold) ? "Controller:LeftStickUp"
            : Moved(current.LeftY, baseline.LeftY, threshold) ? "Controller:LeftStickDown"
            : Moved(current.RightX, baseline.RightX, -threshold) ? "Controller:RightStickLeft"
            : Moved(current.RightX, baseline.RightX, threshold) ? "Controller:RightStickRight" : Moved(current.RightY, baseline.RightY, -threshold) ? "Controller:RightStickUp" : Moved(current.RightY, baseline.RightY, threshold) ? "Controller:RightStickDown" : null;
    }

    private static bool Moved(short current, short baseline, int limit) => limit < 0 ? current < limit && baseline >= limit : current > limit && baseline <= limit;
}
