using System.Buffers.Binary;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

internal sealed partial class Vdp
{
    internal const int ContextSize = 0x400 + 0x10000 + 0x80 + 0x80 + 0x20 + 2 + 2 + 1 + 1 + 2 + 4 + 4 + 8 + 1 + 1 + 1 + 4 + 1 + 2 + 4;

    public int SaveContext(Span<byte> state)
    {
        if (state.Length < ContextSize) throw new ArgumentException("VDP context buffer is too small.", nameof(state));
        var writer = new ContextWriter(state);
        writer.Write(sat); writer.Write(vram); writer.Write(cram); writer.Write(vsram); writer.Write(reg);
        writer.Write(addr); writer.Write(addr_latch); writer.Write(code); writer.Write(pending); writer.Write(status);
        writer.Write(dmafill); writer.Write(fifo_idx);
        foreach (ushort value in fifo) writer.Write(value);
        writer.Write(h_counter); writer.Write(hint_pending); writer.Write(vint_pending);
        writer.Write(dma_length); writer.Write(dma_type); writer.Write(dma_src); writer.Write(cached_write);
        return writer.Position;
    }

    public int LoadContext(ReadOnlySpan<byte> state)
    {
        if (state.Length < ContextSize) throw new ArgumentException("VDP context buffer is too small.", nameof(state));
        var reader = new ContextReader(state);
        reader.Read(sat); reader.Read(vram); reader.Read(cram); reader.Read(vsram);
        Span<byte> restored = stackalloc byte[32];
        reader.Read(restored);
        if (system_hw < SYSTEM_MD)
        {
            if (system_hw >= SYSTEM_MARKIII)
            {
                for (int i = 0; i < 16; i++)
                {
                    pending = 1;
                    addr_latch = restored[i];
                    vdp_sms_ctrl_w((uint)(0x80 | i));
                }
            }
            else
            {
                restored[..8].CopyTo(reg);
                render_bg = render_bg_modes[((reg[0] & 2) | (reg[1] & 0x18)) >> 1];
            }
        }
        else
        {
            reg[1] = (byte)((reg[1] & ~4) | (restored[1] & 4));
            for (int i = 0; i < 32; i++) vdp_reg_w((uint)i, restored[i], 0);
        }
        addr = reader.ReadUInt16(); addr_latch = reader.ReadUInt16(); code = reader.ReadByte(); pending = reader.ReadByte(); status = reader.ReadUInt16();
        dmafill = reader.ReadInt32(); fifo_idx = reader.ReadInt32() & 3;
        for (int i = 0; i < fifo.Length; i++) fifo[i] = reader.ReadUInt16();
        h_counter = reader.ReadByte(); hint_pending = reader.ReadByte(); vint_pending = reader.ReadByte();
        dma_length = reader.ReadUInt32(); dma_type = reader.ReadByte(); dma_src = reader.ReadUInt16(); cached_write = reader.ReadInt32();
        fifo_byte_access = (code & 15) < 3 ? 1 : 0;
        if ((system_hw & SYSTEM_MD) != 0) status = (ushort)((status & ~1) | vdp_pal);
        if ((reg[1] & 4) != 0)
        {
            bg_list_index = 0x800;
            color_update_m5(0, BinaryPrimitives.ReadUInt16LittleEndian(cram.AsSpan(border << 1, 2)));
            for (int i = 1; i < 0x40; i++) color_update_m5(i, BinaryPrimitives.ReadUInt16LittleEndian(cram.AsSpan(i << 1, 2)));
            update_bg_pattern_cache = update_bg_pattern_cache_m5;
            parse_satb = parse_satb_m5;
            render_bg = (reg[11] & 4) != 0 ? (config.enhanced_vscroll != 0 ? render_bg_m5_vs_enhanced : render_bg_m5_vs) : render_bg_m5;
            render_obj = (reg[12] & 8) != 0 ? render_obj_m5_ste : render_obj_m5;
            vdp_68k_data_w = vdp_68k_data_w_m5; vdp_z80_data_w = vdp_z80_data_w_m5;
            vdp_68k_data_r = vdp_68k_data_r_m5; vdp_z80_data_r = vdp_z80_data_r_m5;
            if ((reg[0] & 2) == 0) hvc_latch = 0;
            vc_max = vc_table[((reg[1] >> 2) & 3) * 2 + vdp_pal];
            bitmap.viewport.changed |= 2;
        }
        else
        {
            bg_list_index = 0x200;
            for (int i = 0; i < 0x20; i++) color_update_m4(i, BinaryPrimitives.ReadUInt16LittleEndian(cram.AsSpan(i << 1, 2)));
            color_update_m4(0x40, BinaryPrimitives.ReadUInt16LittleEndian(cram.AsSpan((0x10 | (border & 15)) << 1, 2)));
        }
        for (int i = 0; i < bg_list_index; i++) { bg_name_list[i] = (ushort)i; bg_name_dirty[i] = 255; }
        return reader.Position;
    }

    private ref struct ContextWriter(Span<byte> state)
    {
        private Span<byte> _state = state;
        public int Position;
        public void Write(scoped ReadOnlySpan<byte> value) { value.CopyTo(_state[Position..]); Position += value.Length; }
        public void Write(byte value) => _state[Position++] = value;
        public void Write(ushort value) { BinaryPrimitives.WriteUInt16LittleEndian(_state[Position..], value); Position += 2; }
        public void Write(uint value) { BinaryPrimitives.WriteUInt32LittleEndian(_state[Position..], value); Position += 4; }
        public void Write(int value) { BinaryPrimitives.WriteInt32LittleEndian(_state[Position..], value); Position += 4; }
    }
    private ref struct ContextReader(ReadOnlySpan<byte> state)
    {
        private ReadOnlySpan<byte> _state = state;
        public int Position;
        public void Read(scoped Span<byte> value) { _state.Slice(Position, value.Length).CopyTo(value); Position += value.Length; }
        public byte ReadByte() => _state[Position++];
        public ushort ReadUInt16() { ushort value = BinaryPrimitives.ReadUInt16LittleEndian(_state[Position..]); Position += 2; return value; }
        public uint ReadUInt32() { uint value = BinaryPrimitives.ReadUInt32LittleEndian(_state[Position..]); Position += 4; return value; }
        public int ReadInt32() { int value = BinaryPrimitives.ReadInt32LittleEndian(_state[Position..]); Position += 4; return value; }
    }
}
