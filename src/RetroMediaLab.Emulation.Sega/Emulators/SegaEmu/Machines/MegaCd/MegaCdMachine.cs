using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Constants;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Contracts;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Boards;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.MegaCd;

internal sealed class MegaCdMachine : SegaBoard
{
    internal MegaCdMachine(MachineResources resources, bool enable32X = false)
        : base(SystemHardware.MegaCd, Configure(resources, enable32X)) { }

    private static MachineResources Configure(MachineResources resources, bool enable32X)
    {
        resources.Configuration.Enable32X |= enable32X;
        resources.Configuration.add_on = 1;
        return resources;
    }

    protected override void InitializeMachineBus() => InitializeMegaDriveMapping();
    protected override void InitializeSixteenBitDevices() => InitializeMegaCdDevices();
    protected override void ConfigureZ80Ports() => UsePowerBasePorts();
    protected override Action<int> CreateFrameScheduler() => CreateMegaCdFrameScheduler();
    protected override bool AcceptsHardware(byte hardware) => hardware == SystemHardware.MegaCd;
}
