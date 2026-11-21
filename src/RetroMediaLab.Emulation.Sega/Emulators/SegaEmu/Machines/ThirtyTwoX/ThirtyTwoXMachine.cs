using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Boards;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Constants;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Contracts;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.ThirtyTwoX;

internal sealed class ThirtyTwoXMachine : SegaBoard
{
    internal ThirtyTwoXMachine(MachineResources resources) : base(SystemHardware.MegaDrive, Enable(resources)) { }
    private static MachineResources Enable(MachineResources resources) { resources.Configuration.Enable32X = true; return resources; }
    protected override void InitializeMachineBus() => InitializeMegaDriveMapping();
    protected override void ConfigureZ80Ports() => UsePowerBasePorts();
    protected override void InitializeSixteenBitDevices() { if (Hardware == SystemHardware.MegaCd) InitializeMegaCdDevices(); else base.InitializeSixteenBitDevices(); }
    protected override Action<int> CreateFrameScheduler() => Hardware == SystemHardware.MegaCd ? CreateMegaCdFrameScheduler() : CreateMegaDriveFrameScheduler();
    protected override bool AcceptsHardware(byte hardware) => hardware is SystemHardware.MegaDrive or SystemHardware.MegaCd;
}
