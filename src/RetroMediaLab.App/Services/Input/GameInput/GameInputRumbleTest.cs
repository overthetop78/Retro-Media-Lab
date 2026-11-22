namespace RetroMediaLab.App.Services.Input.GameInput;

internal static class GameInputRumbleTest
{
    internal static bool Supports(GameInputDeviceDescriptor? device, GameInputRumbleMotors motor)
    {
        var bits = (uint)motor;
        return device is not null && bits != 0 && (bits & (bits - 1)) == 0
            && device.RumbleMotors.HasFlag(motor);
    }

    internal static async Task<bool> PulseAsync(
        IGameInputControllerSource source,
        string deviceId,
        GameInputRumbleMotors motor,
        double strength,
        Func<int, Task> delay,
        Action<Exception> onStopFailed)
    {
        var intensity = (float)(strength / 100d);
        var (low, high, left, right) = motor switch
        {
            GameInputRumbleMotors.LowFrequency => (intensity, 0f, 0f, 0f),
            GameInputRumbleMotors.HighFrequency => (0f, intensity, 0f, 0f),
            GameInputRumbleMotors.LeftTrigger => (0f, 0f, intensity, 0f),
            GameInputRumbleMotors.RightTrigger => (0f, 0f, 0f, intensity),
            _ => (0f, 0f, 0f, 0f)
        };

        if (!source.SetRumble(deviceId, low, high, left, right)) return false;
        try
        {
            await delay(250);
            return true;
        }
        finally
        {
            Stop(source, deviceId, onStopFailed);
        }
    }

    internal static void Stop(IGameInputControllerSource source, string deviceId, Action<Exception> onFailure)
    {
        try { source.SetRumble(deviceId, 0f, 0f, 0f, 0f); }
        catch (Exception exception) { onFailure(exception); }
    }
}
