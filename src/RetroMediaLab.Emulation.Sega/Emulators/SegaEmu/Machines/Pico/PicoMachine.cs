using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Constants;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Contracts;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Boards;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.Pico;

internal sealed class PicoMachine(MachineResources resources) : SegaBoard(SystemHardware.Pico,resources)
{
    protected override void InitializeMachineBus() => InitializeMegaDriveMapping();
    protected override void ConfigureSixteenBitIo() => ConfigurePicoIo();
    protected override void ConfigureZ80Ports() => UsePowerBasePorts();
    protected override Action<int> CreateFrameScheduler() => CreateMegaDriveFrameScheduler();
    protected override bool AcceptsHardware(byte hardware) => hardware == SystemHardware.Pico;
}
