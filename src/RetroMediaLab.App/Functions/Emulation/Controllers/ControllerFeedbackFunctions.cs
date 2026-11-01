using RetroMediaLab.App.Services.Input.GameInput;
using RetroMediaLab.Emulation.Enums;

namespace RetroMediaLab.App.Functions.Emulation.Controllers;

internal static class ControllerFeedbackFunctions
{
    internal static GameInputRumbleMotors PhysicalMotor(EmulationControllerMotor motor) => motor switch
    {
        EmulationControllerMotor.LowFrequency => GameInputRumbleMotors.LowFrequency,
        EmulationControllerMotor.HighFrequency => GameInputRumbleMotors.HighFrequency,
        EmulationControllerMotor.LeftTrigger => GameInputRumbleMotors.LeftTrigger,
        EmulationControllerMotor.RightTrigger => GameInputRumbleMotors.RightTrigger,
        _ => GameInputRumbleMotors.None
    };
}
