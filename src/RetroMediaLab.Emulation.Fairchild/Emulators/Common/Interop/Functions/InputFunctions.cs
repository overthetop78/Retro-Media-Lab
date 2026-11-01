using CommonControllerFunctions = RetroMediaLab.Emulation.Fairchild.Common.Functions.ControllerFunctions;
using RetroMediaLab.Emulation.Fairchild.Emulators.Common.Interop.Constants;
namespace RetroMediaLab.Emulation.Fairchild.Emulators.Common.Interop.Functions;

internal static class NativeInputFunctions
{
    internal static short State(EmulationInputSnapshot snapshot, uint port, uint device, uint index, uint id)
    {
        if (device != InputConstants.JoypadDevice || index != InputConstants.PrimaryIndex || port >= snapshot.Controllers.Count)
            return InputConstants.InactiveState;
        var controller = snapshot.Controllers[checked((int)port)];
        return CommonControllerFunctions.NativeJoypadState(controller.Buttons, id, InputConstants.MaximumJoypadButtonCount);
    }
}
