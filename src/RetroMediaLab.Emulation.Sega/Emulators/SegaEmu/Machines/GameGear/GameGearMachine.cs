using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Constants;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Contracts;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Boards;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.GameGear;

internal sealed class GameGearMachine(MachineResources resources) : SegaBoard(SystemHardware.GameGear,resources)
{
    protected override void InitializeMachineBus() => InitializeEightBitMapping();
    protected override void ConfigureZ80Ports() => UseGameGearPorts();
    protected override Action<int> CreateFrameScheduler() => CreateEightBitFrameScheduler();
    protected override bool AcceptsHardware(byte hardware) => hardware is SystemHardware.GameGear or SystemHardware.GameGearMasterSystem;
}
