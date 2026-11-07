using RetroMediaLab.App.Contracts.Services.Input;
using RetroMediaLab.App.Functions.Input.Controllers;
using RetroMediaLab.App.Functions.Input.Keyboard;
using RetroMediaLab.Emulation;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace RetroMediaLab.App.Services.Input.GameInput;

internal static class GameInputPhysicalInputReader
{
    internal static GameInputPhysicalState Read(
        IGameInput? gameInput,
        IReadOnlyCollection<GameInputDeviceEntry> devices,
        IDictionary<string, GameInputMouseState> previousMouse,
        Func<GameInputDeviceEntry, EmulationControllerState> readController,
        Func<Exception, bool> isInteropFailure,
        Action<object?> release)
    {
        var keys = new HashSet<EmulationKey>();
        var deltaX = 0L;
        var deltaY = 0L;
        var wheelX = 0L;
        var wheelY = 0L;
        var mouseButtons = (GameInputMouseButtons)0;
        var hasPointerReading = false;
        foreach (var entry in devices)
        {
            if ((entry.InputKinds & GameInputKind.Keyboard) != 0)
                ReadKeyboard(gameInput, entry, keys, isInteropFailure, release);
            if ((entry.InputKinds & GameInputKind.Mouse) != 0)
                hasPointerReading |= ReadMouse(gameInput, entry, previousMouse, ref deltaX, ref deltaY, ref wheelX, ref wheelY, ref mouseButtons, isInteropFailure, release);
        }

        var pointer = new EmulationPointerState(Clamp(deltaX), Clamp(deltaY), Clamp(wheelY),
            mouseButtons.HasFlag(GameInputMouseButtons.Left),
            mouseButtons.HasFlag(GameInputMouseButtons.Right),
            mouseButtons.HasFlag(GameInputMouseButtons.Middle),
            mouseButtons.HasFlag(GameInputMouseButtons.Button4),
            mouseButtons.HasFlag(GameInputMouseButtons.Button5), Clamp(wheelX));
        var controllers = devices.Where(entry => entry.IsController).OrderBy(entry => entry.Id, StringComparer.Ordinal).ToArray();
        var descriptors = controllers.Select(entry => entry.Descriptor).ToArray();
        var controllerStates = controllers.Select(readController).Concat(RawGameControllerFallback.ReadAll(descriptors)).Select(ControllerAnalogDeadZoneFunctions.ApplyConfigured).ToArray();
        return new GameInputPhysicalState(keys, pointer, controllerStates) { HasPointerReading = hasPointerReading };
    }

    private static unsafe void ReadKeyboard(IGameInput? gameInput, GameInputDeviceEntry entry, HashSet<EmulationKey> keys, Func<Exception, bool> isInteropFailure, Action<object?> release)
    {
        if (gameInput is null) return;
        IGameInputReading? reading = null;
        try
        {
            if (gameInput.GetCurrentReading(GameInputKind.Keyboard, entry.DevicePointer, out reading) < 0 ||
                reading is null) return;
            var count = checked((int)reading.GetKeyCount());
            var states = new GameInputKeyState[count];
            fixed (GameInputKeyState* pointer = states)
                reading.GetKeyState((uint)count, (IntPtr)pointer);
            foreach (var state in states)
                if (EmulationKeyMapper.TryMap(KeyInterop.KeyFromVirtualKey(state.VirtualKey), out var key))
                    keys.Add(key);
        }
        catch (Exception exception) when (isInteropFailure(exception)) { }
        finally { release(reading); }
    }

    private static bool ReadMouse(
        IGameInput? gameInput,
        GameInputDeviceEntry entry,
        IDictionary<string, GameInputMouseState> previousMouse,
        ref long deltaX,
        ref long deltaY,
        ref long wheelX,
        ref long wheelY,
        ref GameInputMouseButtons buttons,
        Func<Exception, bool> isInteropFailure,
        Action<object?> release)
    {
        if (gameInput is null) { previousMouse.Remove(entry.Id); return false; }
        IGameInputReading? reading = null;
        var valid = false;
        try
        {
            if (gameInput.GetCurrentReading(GameInputKind.Mouse, entry.DevicePointer, out reading) < 0 ||
                reading is null || !reading.GetMouseState(out var state)) return false;
            valid = true;
            buttons |= state.Buttons;
            if (previousMouse.TryGetValue(entry.Id, out var previous))
            {
                deltaX += state.PositionX - previous.PositionX;
                deltaY += state.PositionY - previous.PositionY;
                wheelX += state.WheelX - previous.WheelX;
                wheelY += state.WheelY - previous.WheelY;
            }
            previousMouse[entry.Id] = state;
            return true;
        }
        catch (Exception exception) when (isInteropFailure(exception)) { return false; }
        finally
        {
            // A recovered source starts a new baseline; do not replay movements handled by Windows.
            if (!valid) previousMouse.Remove(entry.Id);
            release(reading);
        }
    }

    private static int Clamp(long value) => (int)Math.Clamp(value, int.MinValue, int.MaxValue);
}
