using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Common;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.MarkIII;

internal sealed class MarkIIIMachine(MachineResources resources) : SegaBoard(SystemHardware.MarkIII,resources)
{
    protected override void InitializeMachineBus() => InitializeEightBitMapping();
    protected override void ConfigureZ80Ports() => UseMarkIIIPorts();
    protected override void ExecuteMachineFrame(int skip) => ExecuteEightBitFrame(skip);
    protected override bool AcceptsHardware(byte hardware) => hardware == SystemHardware.MarkIII;
}
