using System.IO;
using RetroMediaLab.Chips.Common.Memory;
using RetroMediaLab.Chips.CPU.Motorola.M68000.Execution;
using RetroMediaLab.Chips.Controllers.Sega.CD;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Media;
using Pcm = RetroMediaLab.Chips.Audio.Ricoh.RF5C164.Rf5c164;
using BlipBuffer = RetroMediaLab.Chips.Audio.Common.Resampling.BandLimitedBuffer;
using System.Buffers.Binary;


namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.MegaCd.Hardware;

internal sealed partial class Scd
{
    private static int FixedContextSize => 512 + 14 + Gfx.ContextSize + Cdc.ContextSize + CdDrive.ContextSize + Pcm.ContextSize + 0x80000 + 0x40000 + 24 + 2 + Cpu68000<Scd>.ContextSize;
    public int ContextSize => FixedContextSize + (cd_boot == 0 ? 0 : _host.MdCartridgeContextSize);
    private static void Put32(Span<byte> state, ref int offset, uint value)
    { BinaryPrimitives.WriteUInt32LittleEndian(state[offset..], value); offset += 4; }
    private static uint Get32(ReadOnlySpan<byte> state, ref int offset)
    { uint value = BinaryPrimitives.ReadUInt32LittleEndian(state[offset..]); offset += 4; return value; }
    private static void Put16(Span<byte> state, ref int offset, ushort value)
    { BinaryPrimitives.WriteUInt16LittleEndian(state[offset..], value); offset += 2; }
    private static ushort Get16(ReadOnlySpan<byte> state, ref int offset)
    { ushort value = BinaryPrimitives.ReadUInt16LittleEndian(state[offset..]); offset += 2; return value; }
    private static void SavePolling(Span<byte> state, ref int offset, in Cpu68000State cpu)
    {
        Put32(state, ref offset, cpu.poll_pc); Put32(state, ref offset, cpu.poll_cycle); Put32(state, ref offset, cpu.poll_detected);
    }
    private static void LoadPolling(ReadOnlySpan<byte> state, ref int offset, ref Cpu68000State cpu)
    {
        cpu.poll_pc = Get32(state, ref offset); cpu.poll_cycle = Get32(state, ref offset); cpu.poll_detected = Get32(state, ref offset);
    }
    public int SaveContext(Span<byte> state)
    {
        if (state.Length < ContextSize) throw new ArgumentException("SCD context buffer is too short.", nameof(state));
        int offset = 0;
        foreach (Register16 register in scd_regs) Put16(state, ref offset, register.Word);
        Put32(state, ref offset, cd_cycles); Put32(state, ref offset, unchecked((uint)stopwatch)); Put32(state, ref offset, unchecked((uint)timer));
        state[offset++] = pending; state[offset++] = dmna;
        offset += _gfx.SaveContext(state[offset..]); offset += _cdc.SaveContext(state[offset..]);
        offset += _cdd.SaveContext(state[offset..]); offset += _pcm.SaveContext(state[offset..]);
        prg_ram.CopyTo(state[offset..]); offset += prg_ram.Length;
        byte[] wordRam = (scd_regs[0x03 >> 1].Low & 4) != 0 ? _wordRam1M : word_ram_2M;
        wordRam.CopyTo(state[offset..]); offset += wordRam.Length;
        SavePolling(state, ref offset, main); SavePolling(state, ref offset, sub);
        Put16(state, ref offset, BinaryPrimitives.ReadUInt16LittleEndian(memory_map[cd_boot].Data.Span[0x72..]));
        offset += _cpu.SaveContext(state[offset..]);
        if (cd_boot != 0) offset += _host.SaveMdCartridgeContext(state[offset..]);
        return offset;
    }
    public int LoadContext(ReadOnlySpan<byte> state, string version)
    {
        bool previous = version.Length > 15 && version[11] == '1' && version[13] == '7' && version[15] == '5';
        if (state.Length < ContextSize - (previous ? 8 : 0)) throw new ArgumentException("SCD context buffer is too short.", nameof(state));
        int offset = 0;
        for (int i = 0; i < scd_regs.Length; i++) scd_regs[i].Word = Get16(state, ref offset);
        cd_cycles = Get32(state, ref offset); stopwatch = unchecked((int)Get32(state, ref offset)); timer = unchecked((int)Get32(state, ref offset));
        pending = state[offset++]; dmna = state[offset++];
        offset += _gfx.LoadContext(state[offset..]); offset += _cdc.LoadContext(state[offset..]);
        offset += _cdd.LoadContext(state[offset..], version); offset += _pcm.LoadContext(state[offset..]);
        state.Slice(offset, prg_ram.Length).CopyTo(prg_ram); offset += prg_ram.Length;
        byte[] wordRam = (scd_regs[0x03 >> 1].Low & 4) != 0 ? _wordRam1M : word_ram_2M;
        state.Slice(offset, wordRam.Length).CopyTo(wordRam); offset += wordRam.Length;
        RestoreMemoryMap();
        LoadPolling(state, ref offset, ref main); LoadPolling(state, ref offset, ref sub);
        BinaryPrimitives.WriteUInt16LittleEndian(memory_map[cd_boot].Data.Span[0x72..], Get16(state, ref offset));
        offset += _cpu.LoadContext(state[offset..]);
        if (cd_boot != 0) offset += _host.LoadMdCartridgeContext(state[offset..]);
        return offset;
    }
}
