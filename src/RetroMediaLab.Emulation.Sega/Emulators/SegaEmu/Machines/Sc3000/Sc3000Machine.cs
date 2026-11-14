using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Common;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.Sc3000;

internal sealed class Sc3000Machine(MachineResources resources) : SegaBoard(SystemHardware.Sg1000,resources)
{
    protected override void InitializeMachineBus() => InitializeEightBitMapping();
    protected override void ConfigureZ80Ports() => UseSg1000Ports();
    protected override void ExecuteMachineFrame(int skip) => ExecuteEightBitFrame(skip);
    protected override bool AcceptsHardware(byte hardware) => hardware is SystemHardware.Sg1000 or SystemHardware.Sg1000II or SystemHardware.Sg1000IIExtendedRam;
}
