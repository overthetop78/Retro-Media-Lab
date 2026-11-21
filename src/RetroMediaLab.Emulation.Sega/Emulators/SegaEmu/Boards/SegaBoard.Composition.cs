using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Contracts;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Constants;
namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Boards;

internal abstract partial class SegaBoard
{
    // The original experimental option name is retained for existing runners;
    // The native route selects the Z80 on eight-bit machines and alongside
    // the 68000/SH-2 processors on sixteen-bit machines, including the 32X.
    private bool UseNativeZ80 => _useNative68000Blocks;
    protected SegaBoard(byte hardware, MachineResources resources)
        : this(hardware,resources.Configuration,resources.Framebuffer,resources.Width,resources.Height,resources.Pitch,
            resources.SampleRate,resources.OpenFile,resources.LoadFirmware,resources.Cancellation,resources.Gpu32XComposer,resources.UseDynamicCpuBlocks,resources.UseNative68000Blocks) { }
}
