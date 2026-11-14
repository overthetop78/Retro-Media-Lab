using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Common;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.MasterSystem;

internal sealed class MasterSystemMachine(MachineResources resources) : SegaBoard(SystemHardware.MasterSystemII,resources)
{
    protected override void InitializeMachineBus() => InitializeEightBitMapping();
    protected override void ConfigureZ80Ports() => UseMasterSystemPorts();
    protected override void ExecuteMachineFrame(int skip) => ExecuteEightBitFrame(skip);
    protected override bool AcceptsHardware(byte hardware) => hardware is SystemHardware.MasterSystem or SystemHardware.MasterSystemII;
}
