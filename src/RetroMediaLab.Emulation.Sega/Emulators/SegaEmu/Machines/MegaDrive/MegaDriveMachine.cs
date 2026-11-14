using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Common;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.MegaDrive;

internal sealed class MegaDriveMachine(MachineResources resources) : SegaBoard(SystemHardware.MegaDrive,resources)
{
    protected override void InitializeMachineBus()
    {
        if (Hardware == SystemHardware.PowerBaseConverter) InitializeEightBitMapping(); else InitializeMegaDriveMapping();
    }
    protected override void ConfigureZ80Ports() => UsePowerBasePorts();
    protected override void InitializeSixteenBitDevices()
    {
        if (Hardware == SystemHardware.MegaCd) InitializeMegaCdDevices(); else base.InitializeSixteenBitDevices();
    }
    protected override void ExecuteMachineFrame(int skip)
    {
        if (Hardware == SystemHardware.PowerBaseConverter) ExecuteEightBitFrame(skip);
        else if (Hardware == SystemHardware.MegaCd) ExecuteMegaCdFrame(skip);
        else ExecuteMegaDriveFrame(skip);
    }
    protected override bool AcceptsHardware(byte hardware) => hardware is SystemHardware.MegaDrive or SystemHardware.PowerBaseConverter or SystemHardware.MegaCd;
}
