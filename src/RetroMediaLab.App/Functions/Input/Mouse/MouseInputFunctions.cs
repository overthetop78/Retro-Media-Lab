using System.Windows.Input;
using RetroMediaLab.App.Constants.Input.Windows;
using RetroMediaLab.App.Functions.Input.Bindings;
using RetroMediaLab.App.Services.Input.GameInput;
using RetroMediaLab.Emulation.Contracts;

namespace RetroMediaLab.App.Functions.Input.Mouse;

internal static class MouseInputFunctions
{
    internal static string? BindingSource(MouseButton button)
    {
        var source = button switch
        {
            MouseButton.Left => "Left",
            MouseButton.Right => "Right",
            MouseButton.Middle => "Middle",
            MouseButton.XButton1 => "XButton1",
            MouseButton.XButton2 => "XButton2",
            _ => null
        };
        return source is null ? null : InputBindingSyntax.Mouse(source);
    }

    internal static string? WheelBindingSource(int delta, bool horizontal = false)
    {
        if (delta == WindowsInputMessages.NeutralWheelDelta) return null;
        return InputBindingSyntax.Mouse(horizontal
            ? (delta > 0 ? "WheelRight" : "WheelLeft")
            : (delta > 0 ? "WheelUp" : "WheelDown"));
    }

    internal static int WheelDelta(IntPtr wParam) =>
        unchecked((short)((wParam.ToInt64() >> WindowsInputMessages.WheelHighWordShift) & WindowsInputMessages.UnsignedWordMask));

    internal static GameInputMouseButtons PressedButtons(EmulationPointerState pointer) =>
        (pointer.Left ? GameInputMouseButtons.Left : 0)
        | (pointer.Right ? GameInputMouseButtons.Right : 0)
        | (pointer.Middle ? GameInputMouseButtons.Middle : 0)
        | (pointer.ExtendedButton1 ? GameInputMouseButtons.Button4 : 0)
        | (pointer.ExtendedButton2 ? GameInputMouseButtons.Button5 : 0);

    internal static EmulationPointerState ConsumeSource(EmulationPointerState pointer, string source,
        ref GameInputMouseButtons consumed)
    {
        if (!source.StartsWith("Mouse:", StringComparison.OrdinalIgnoreCase)) return pointer;
        var control = source["Mouse:".Length..].ToUpperInvariant();
        consumed |= control switch
        {
            "LEFT" => GameInputMouseButtons.Left,
            "RIGHT" => GameInputMouseButtons.Right,
            "MIDDLE" => GameInputMouseButtons.Middle,
            "XBUTTON1" => GameInputMouseButtons.Button4,
            "XBUTTON2" => GameInputMouseButtons.Button5,
            _ => GameInputMouseButtons.None
        };
        return control switch
        {
            "WHEELUP" when pointer.Wheel > 0 => pointer with { Wheel = 0 },
            "WHEELDOWN" when pointer.Wheel < 0 => pointer with { Wheel = 0 },
            "WHEELLEFT" when pointer.HorizontalWheel < 0 => pointer with { HorizontalWheel = 0 },
            "WHEELRIGHT" when pointer.HorizontalWheel > 0 => pointer with { HorizontalWheel = 0 },
            _ => pointer
        };
    }

    internal static EmulationPointerState WithoutButtons(EmulationPointerState pointer, GameInputMouseButtons buttons) =>
        buttons == GameInputMouseButtons.None ? pointer : pointer with
        {
            Left = pointer.Left && !buttons.HasFlag(GameInputMouseButtons.Left),
            Right = pointer.Right && !buttons.HasFlag(GameInputMouseButtons.Right),
            Middle = pointer.Middle && !buttons.HasFlag(GameInputMouseButtons.Middle),
            ExtendedButton1 = pointer.ExtendedButton1 && !buttons.HasFlag(GameInputMouseButtons.Button4),
            ExtendedButton2 = pointer.ExtendedButton2 && !buttons.HasFlag(GameInputMouseButtons.Button5)
        };
}
