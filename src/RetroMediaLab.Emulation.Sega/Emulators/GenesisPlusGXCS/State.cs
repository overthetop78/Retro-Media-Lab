using System.Buffers.Binary;
using System.Text;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cpu;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS;

internal sealed partial class SystemCore
{
    public const string StateVersion = "GENPLUS-GX 1.7.6";
    private static int Z80ContextSize => IntPtr.Size == 8 ? 88 : 76;
    public int StateSize => 16 + (SystemHardware.IsSixteenBit(system_hw) ? 0x12000 + 5 + 90 : 0x2000) +
        16 + Vdp.ContextSize + _sound.ContextSize + Z80ContextSize +
        (system_hw == SystemHardware.MegaCd ? 4 + _cd.ContextSize : SystemHardware.IsSixteenBit(system_hw) ? _md.ContextSize : _sms.ContextSize);

    public int SaveState(Span<byte> state)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (state.Length < StateSize) throw new ArgumentException("State buffer is too short.", nameof(state));
        int offset = 0;
        WriteStateBytes(state, ref offset, "GENPLUS-GX 1.7.6"u8);
        bool md = SystemHardware.IsSixteenBit(system_hw);
        WriteStateBytes(state, ref offset, work_ram.AsSpan(0, md ? 65536 : 8192));
        if (md)
        {
            WriteStateBytes(state, ref offset, zram); state[offset++] = zstate;
            PutState32(state, ref offset, zbank);
        }
        WriteStateBytes(state, ref offset, _input.IoRegisters.Span);
        offset += _vdp.SaveContext(state[offset..]);
        offset += _sound.SaveContext(state[offset..]);
        if (md)
        {
            for (int i = 0; i < 16; i++) PutState32(state, ref offset, _mainCpu.GetRegister((M68kRegister)i));
            PutState32(state, ref offset, _mainCpu.GetRegister(M68kRegister.PC));
            BinaryPrimitives.WriteUInt16LittleEndian(state[offset..], (ushort)_mainCpu.GetRegister(M68kRegister.SR)); offset += 2;
            PutState32(state, ref offset, _mainCpu.GetRegister(M68kRegister.USP));
            PutState32(state, ref offset, _mainCpu.GetRegister(M68kRegister.ISP));
            PutState32(state, ref offset, unchecked((uint)main.cycles));
            PutState32(state, ref offset, main.int_level); PutState32(state, ref offset, main.stopped);
        }
        SaveZ80(state.Slice(offset, Z80ContextSize)); offset += Z80ContextSize;
        if (system_hw == SystemHardware.MegaCd)
        {
            WriteStateBytes(state, ref offset, "SCD!"u8);
            offset += _cd.SaveContext(state[offset..]);
        }
        else offset += md ? _md.SaveContext(state[offset..]) : _sms.SaveContext(state[offset..]);
        return offset;
    }

    public int LoadState(Span<byte> state)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (state.Length < 16 || !state[..11].SequenceEqual("GENPLUS-GX "u8) || state[11] < '1' || state[13] < '7' || state[15] < '5') return 0;
        string version = Encoding.ASCII.GetString(state[..16]);
        int offset = 16;
        bool completed = false;
        try
        {
            Reset();
            for (int i = 0xc0; i < 0xe0; i += 8)
            {
                memory_map[i].read8 = vdp_read_byte; memory_map[i].read16 = vdp_read_word;
                memory_map[i].write8 = vdp_write_byte; memory_map[i].write16 = vdp_write_word;
                zbank_memory_map[i].read = zbank_read_vdp; zbank_memory_map[i].write = zbank_write_vdp;
            }
            bool md = SystemHardware.IsSixteenBit(system_hw);
            ReadStateBytes(state, ref offset, work_ram.AsSpan(0, md ? 65536 : 8192));
            if (md)
            {
                ReadStateBytes(state, ref offset, zram); zstate = state[offset++];
                zbank = GetState32(state, ref offset);
                memory_map[0xa0].read8 = zstate == 3 ? z80_read_byte : m68k_read_bus_8;
                memory_map[0xa0].read16 = zstate == 3 ? z80_read_word : m68k_read_bus_16;
                memory_map[0xa0].write8 = zstate == 3 ? z80_write_byte : m68k_unused_8_w;
                memory_map[0xa0].write16 = zstate == 3 ? z80_write_word : m68k_unused_16_w;
            }
            ReadStateBytes(state, ref offset, _input.IoRegisters.Span);
            _input.IoRegisters.Span[0] = md ? (byte)(region_code | 0x20 | config.bios & 1) : (byte)(0x80 | region_code >> 1);
            offset += _vdp.LoadContext(state[offset..]);
            offset += _sound.LoadContext(state[offset..]);
            psg_config(0, psg_preamp, md ? 0xffu : _input.IoRegisters.Span[6]);
            if (md)
            {
                for (int i = 0; i < 16; i++) _mainCpu.SetRegister((M68kRegister)i, GetState32(state, ref offset));
                _mainCpu.SetRegister(M68kRegister.PC, GetState32(state, ref offset));
                _mainCpu.SetRegister(M68kRegister.SR, BinaryPrimitives.ReadUInt16LittleEndian(state[offset..])); offset += 2;
                _mainCpu.SetRegister(M68kRegister.USP, GetState32(state, ref offset));
                _mainCpu.SetRegister(M68kRegister.ISP, GetState32(state, ref offset));
                main.cycles = unchecked((int)GetState32(state, ref offset));
                main.int_level = GetState32(state, ref offset); main.stopped = GetState32(state, ref offset);
            }
            LoadZ80(state.Slice(offset, Z80ContextSize)); offset += Z80ContextSize;
            if (system_hw == SystemHardware.MegaCd)
            {
                if (!state.Slice(offset, 4).SequenceEqual("SCD!"u8)) { completed = true; return 0; }
                offset += 4;
                offset += _cd.LoadContext(state[offset..], version);
            }
            else if (md) offset += _md.LoadContext(state[offset..]);
            else
            {
                offset += _sms.LoadContext(state[offset..]);
                _sms.Switch(unchecked((byte)~_input.IoRegisters.Span[14]));
            }
            completed = true;
            return offset;
        }
        finally { if (!completed) Dispose(); }
    }

    private void SaveZ80(Span<byte> state)
    {
        state.Clear();
        int offset = 0;
        PutPair(state, ref offset, z80.pc); PutPair(state, ref offset, z80.sp); PutPair(state, ref offset, z80.af);
        PutPair(state, ref offset, z80.bc); PutPair(state, ref offset, z80.de); PutPair(state, ref offset, z80.hl);
        PutPair(state, ref offset, z80.ix); PutPair(state, ref offset, z80.iy); PutPair(state, ref offset, z80.wz);
        PutPair(state, ref offset, z80.af2); PutPair(state, ref offset, z80.bc2); PutPair(state, ref offset, z80.de2); PutPair(state, ref offset, z80.hl2);
        state[offset++] = (byte)z80.r; state[offset++] = (byte)z80.r2;
        state[offset++] = (byte)z80.iff1; state[offset++] = (byte)z80.iff2;
        state[offset++] = (byte)z80.halt; state[offset++] = (byte)z80.im; state[offset++] = (byte)z80.i;
        state[offset++] = (byte)z80.nmi_state; state[offset++] = (byte)z80.nmi_pending;
        state[offset++] = (byte)z80.irq_state; state[offset++] = (byte)z80.after_ei;
        offset = (offset + 3) & ~3; PutState32(state, ref offset, z80.cycles);
    }
    private void LoadZ80(ReadOnlySpan<byte> state)
    {
        int offset = 0;
        z80.pc = GetPair(state, ref offset); z80.sp = GetPair(state, ref offset); z80.af = GetPair(state, ref offset);
        z80.bc = GetPair(state, ref offset); z80.de = GetPair(state, ref offset); z80.hl = GetPair(state, ref offset);
        z80.ix = GetPair(state, ref offset); z80.iy = GetPair(state, ref offset); z80.wz = GetPair(state, ref offset);
        z80.af2 = GetPair(state, ref offset); z80.bc2 = GetPair(state, ref offset); z80.de2 = GetPair(state, ref offset); z80.hl2 = GetPair(state, ref offset);
        z80.r = state[offset++]; z80.r2 = state[offset++]; z80.iff1 = state[offset++]; z80.iff2 = state[offset++];
        z80.halt = state[offset++]; z80.im = state[offset++]; z80.i = state[offset++]; z80.nmi_state = state[offset++];
        z80.nmi_pending = state[offset++]; z80.irq_state = state[offset++]; z80.after_ei = state[offset++];
        offset = (offset + 3) & ~3; z80.cycles = GetState32(state, ref offset);
        z80.irq_callback = z80_irq_callback; z80.daisy = null;
    }
    private static void PutPair(Span<byte> state, ref int offset, Z80Pair value) => PutState32(state, ref offset, unchecked((uint)value.Data));
    private static Z80Pair GetPair(ReadOnlySpan<byte> state, ref int offset) => new() { Data = unchecked((int)GetState32(state, ref offset)) };
    private static void PutState32(Span<byte> state, ref int offset, uint value)
    { BinaryPrimitives.WriteUInt32LittleEndian(state[offset..], value); offset += 4; }
    private static uint GetState32(ReadOnlySpan<byte> state, ref int offset)
    { uint value = BinaryPrimitives.ReadUInt32LittleEndian(state[offset..]); offset += 4; return value; }
    private static void WriteStateBytes(Span<byte> state, ref int offset, ReadOnlySpan<byte> bytes)
    { bytes.CopyTo(state[offset..]); offset += bytes.Length; }
    private static void ReadStateBytes(ReadOnlySpan<byte> state, ref int offset, Span<byte> bytes)
    { state.Slice(offset, bytes.Length).CopyTo(bytes); offset += bytes.Length; }
}
