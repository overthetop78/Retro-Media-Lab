using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Cartridge;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.MegaCd.Hardware;

internal sealed partial class Scd
{
    private Func<uint, uint> _nativeWordRead8 = null!, _nativeWordRead16 = null!;
    private Action<uint, uint> _nativeWordWrite8 = null!, _nativeWordWrite16 = null!, _nativeProgramWrite8 = null!, _nativeProgramWrite16 = null!;
    private void InitializeNativeMemoryDelegates()
    {
        // Compare delegate target/method identities without constructing a new
        // method-group delegate for every bank at every mapping refresh.
        _nativeWordRead8 = word_ram_s68k_read_byte; _nativeWordRead16 = word_ram_s68k_read_word;
        _nativeWordWrite8 = word_ram_s68k_write_byte; _nativeWordWrite16 = word_ram_s68k_write_word;
        _nativeProgramWrite8 = prg_ram_write_byte; _nativeProgramWrite16 = prg_ram_write_word;
    }
    internal int NativeWriteProtection => scd_regs[1].High << 9;
    private CartridgeMemoryBank NativeReadBank(int page)
    {
        var bank = sub_memory_map[page];
        return bank.read8 == _nativeWordRead8 && bank.read16 == _nativeWordRead16 ? sub_memory_map[page & 15] : bank;
    }
    private CartridgeMemoryBank NativeWriteBank(int page)
    {
        var bank = sub_memory_map[page];
        return bank.write8 == _nativeWordWrite8 && bank.write16 == _nativeWordWrite16 ? sub_memory_map[page & 15] : bank;
    }
    internal void GetNativeMemory(int page, out ReadOnlyMemory<byte> read, out Memory<byte> write)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(page); ArgumentOutOfRangeException.ThrowIfGreaterThan(page, 255);
        var readBank = NativeReadBank(page); var writeBank = NativeWriteBank(page);
        read = readBank.read8 is null && readBank.read16 is null && readBank.Data.Length >= 65536 ? readBank.Data[..65536] : default;
        write = writeBank.write8 is null && writeBank.write16 is null && writeBank.Data.Length >= 65536 ? writeBank.Data[..65536] : default;
    }
    internal void GetNativeProtectedMemory(int page, out Memory<byte> memory, out int minimumOffset)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(page); ArgumentOutOfRangeException.ThrowIfGreaterThan(page, 255);
        var bank = NativeWriteBank(page);
        if (bank.write8 != _nativeProgramWrite8 || bank.write16 != _nativeProgramWrite16)
        { memory = default; minimumOffset = 0; return; }
        int physicalBase = (page & 7) << 16;
        memory = prg_ram.AsMemory(physicalBase, 65536);
        minimumOffset = Math.Clamp(NativeWriteProtection - physicalBase, 0, 65536);
    }
}
