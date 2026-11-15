using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Contracts;
namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Boards;

internal abstract partial class SegaBoard
{
    protected SegaBoard(byte hardware, MachineResources resources)
        : this(hardware,resources.Configuration,resources.Framebuffer,resources.Width,resources.Height,resources.Pitch,
            resources.SampleRate,resources.OpenFile,resources.LoadFirmware,resources.Cancellation) { }
}
