using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Constants;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Contracts;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Boards;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.MarkIII;

internal sealed class MarkIIIMachine(MachineResources resources) : SegaBoard(SystemHardware.MarkIII,resources)
{
    protected override void InitializeMachineBus() => InitializeEightBitMapping();
    protected override void ConfigureZ80Ports() => UseMarkIIIPorts();
    protected override Action<int> CreateFrameScheduler() => CreateEightBitFrameScheduler();
    protected override bool AcceptsHardware(byte hardware) => hardware == SystemHardware.MarkIII;
}
