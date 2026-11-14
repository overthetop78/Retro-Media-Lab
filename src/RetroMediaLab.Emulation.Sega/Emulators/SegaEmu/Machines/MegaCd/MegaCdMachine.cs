using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Common;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.MegaCd;

internal sealed class MegaCdMachine(MachineResources resources) : SegaBoard(SystemHardware.MegaCd,resources)
{
    protected override void InitializeMachineBus() => InitializeMegaDriveMapping();
    protected override void InitializeSixteenBitDevices() => InitializeMegaCdDevices();
    protected override void ConfigureZ80Ports() => UsePowerBasePorts();
    protected override void ExecuteMachineFrame(int skip) => ExecuteMegaCdFrame(skip);
    protected override bool AcceptsHardware(byte hardware) => hardware == SystemHardware.MegaCd;
}
