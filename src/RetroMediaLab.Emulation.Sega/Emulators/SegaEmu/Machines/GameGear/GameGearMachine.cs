using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Common;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.GameGear;

internal sealed class GameGearMachine(MachineResources resources) : SegaBoard(SystemHardware.GameGear,resources)
{
    protected override void InitializeMachineBus() => InitializeEightBitMapping();
    protected override void ConfigureZ80Ports() => UseGameGearPorts();
    protected override void ExecuteMachineFrame(int skip) => ExecuteEightBitFrame(skip);
    protected override bool AcceptsHardware(byte hardware) => hardware is SystemHardware.GameGear or SystemHardware.GameGearMasterSystem;
}
