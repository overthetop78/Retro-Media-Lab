namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMaster.Common.Services;

internal sealed class CompactJoystick
{
    private const byte Fire = 0x01;
    private const byte Left = 0x02;
    private const byte Down = 0x04;
    private const byte Up = 0x08;
    private const byte Right = 0x10;
    private const int Threshold = short.MaxValue / 2;
    internal byte Port { get; private set; } = byte.MaxValue;
    internal void SetInput(IReadOnlyList<EmulationControllerState> controllers)
    {
        var controller = controllers.FirstOrDefault() ?? EmulationControllerState.Empty;
        Port = byte.MaxValue;
        if (controller.LeftX > Threshold) Port &= unchecked((byte)~Right);
        if (controller.LeftX < -Threshold) Port &= unchecked((byte)~Left);
        if (controller.LeftY > Threshold) Port &= unchecked((byte)~Down);
        if (controller.LeftY < -Threshold) Port &= unchecked((byte)~Up);
        var fire = EmulationSdk.InputProcessing.ControllerButtonIndex(EmulationControllerCommandIds.A);
        if ((controller.Buttons & (1u << fire)) != 0) Port &= unchecked((byte)~Fire);
    }
}
