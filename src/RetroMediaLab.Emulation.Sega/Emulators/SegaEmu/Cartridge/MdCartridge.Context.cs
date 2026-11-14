using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Cartridge;

internal sealed partial class MdCartridge
{
    private const int SvpContextSize = 0x800 + 0x20000 + 1280;
    public int ContextSize => 68 + (_svp is not null ? SvpContextSize : 0)
        + ((cart.special & 0x10) != 0 ? _bus.MegaSdContextSize : 0)
        + (memory_map[0].read8 == mapper_flash_r8 ? FlashCfi.ContextSize : 0)
        + ((cart.special & 0x20) != 0 ? _bus.Yx5200ContextSize : 0);
    public int SaveContext(Span<byte> state)
    {
        if (state.Length < ContextSize) throw new ArgumentException("MD cartridge context buffer is too short.", nameof(state));
        return md_cart_context_save(new ArrayPointer<byte>(state));
    }
    public int LoadContext(Span<byte> state)
    {
        if (state.Length < ContextSize) throw new ArgumentException("MD cartridge context buffer is too short.", nameof(state));
        return md_cart_context_load(new ArrayPointer<byte>(state));
    }
    private static int SaveChunk(ArrayPointer<byte> destination, ArrayPointer<byte> source, int length)
    {
        source.Bytes(length).CopyTo(destination.Bytes(length)); return length;
    }
    private static int LoadChunk(ArrayPointer<byte> source, ArrayPointer<byte> destination, int length) => SaveChunk(destination, source, length);
    private int SaveSvp(ArrayPointer<byte> state)
    {
        Span<byte> data = state.Bytes(SvpContextSize);
        _svp!.InstructionRamRom.Span[..0x800].CopyTo(data);
        _svp.Dram.Span.CopyTo(data[0x800..]);
        _svp.ProcessorState.Span.CopyTo(data[(0x800 + 0x20000)..]);
        return SvpContextSize;
    }
    private int LoadSvp(ArrayPointer<byte> state)
    {
        Span<byte> data = state.Bytes(SvpContextSize);
        data[..0x800].CopyTo(_svp!.InstructionRamRom.Span);
        data.Slice(0x800, 0x20000).CopyTo(_svp.Dram.Span);
        data.Slice(0x800 + 0x20000, 1280).CopyTo(_svp.ProcessorState.Span);
        _svp.RestoreProcessorState();
        return SvpContextSize;
    }
    private int flash_cfi_context_save(ArrayPointer<byte> state) => _flash.SaveContext(state.Bytes(FlashCfi.ContextSize));
    private int flash_cfi_context_load(ArrayPointer<byte> state) => _flash.LoadContext(state.Bytes(FlashCfi.ContextSize));
    private int megasd_context_save(ArrayPointer<byte> state) => _bus.SaveMegaSdContext(state.Bytes(_bus.MegaSdContextSize));
    private int megasd_context_load(ArrayPointer<byte> state) => _bus.LoadMegaSdContext(state.Bytes(_bus.MegaSdContextSize));
    private int yx5200_context_save(ArrayPointer<byte> state) => _bus.SaveYx5200Context(state.Bytes(_bus.Yx5200ContextSize));
    private int yx5200_context_load(ArrayPointer<byte> state) => _bus.LoadYx5200Context(state.Bytes(_bus.Yx5200ContextSize));
}
