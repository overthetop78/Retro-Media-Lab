using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Constants;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Contracts;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Boards;

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
    protected override Action<int> CreateFrameScheduler()
    {
        if (Hardware == SystemHardware.PowerBaseConverter) return CreateEightBitFrameScheduler();
        else if (Hardware == SystemHardware.MegaCd) return CreateMegaCdFrameScheduler();
        else return CreateMegaDriveFrameScheduler();
    }
    protected override bool AcceptsHardware(byte hardware) => hardware is SystemHardware.MegaDrive or SystemHardware.PowerBaseConverter or SystemHardware.MegaCd;
}
