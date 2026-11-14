using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Chips.Video.Sega.VDP;

public sealed partial class Vdp
{
    public sealed class Configuration
    {
        public int gg_extra, render, lcd, enhanced_vscroll_limit, overscan, bios, enhanced_vscroll, ntsc;
        public int PixelDepth = 32;
        public bool DisableSpriteLimit;
        public NtscSetup MdNtscSetup = NtscFilter.Composite(true), SmsNtscSetup = NtscFilter.Composite(false);
    }
    private struct Viewport { public int x, y, w, h, ow, oh, changed; }
    private struct Bitmap { public byte[] data; public int width, height, pitch; public Viewport viewport; }
    private readonly Configuration config;
    private readonly IVdpBus _bus;
    private Bitmap bitmap;
    public const byte SYSTEM_SG = 0x01, SYSTEM_SGII = 0x02, SYSTEM_SGII_RAM_EXT = 0x03,
        SYSTEM_MARKIII = 0x10, SYSTEM_SMS = 0x20, SYSTEM_SMS2 = 0x21, SYSTEM_GG = 0x40,
        SYSTEM_GGMS = 0x41, SYSTEM_MD = 0x80, SYSTEM_PBC = 0x81, SYSTEM_PICO = 0x82, SYSTEM_MCD = 0x84;
    public const int MCYCLES_PER_LINE = 3420;
    public readonly byte[] reg = new byte[0x20];
    public readonly byte[] sat = new byte[0x400];
    public readonly byte[] vram = new byte[0x10000];
    public readonly byte[] cram = new byte[0x80];
    public readonly byte[] vsram = new byte[0x80];
    public readonly byte[] bg_name_dirty = new byte[0x800];
    public readonly ushort[] bg_name_list = new ushort[0x800];
    public readonly uint[] fifo_cycles = new uint[4];
    public byte hint_pending, vint_pending, dma_type, hscroll_mask, playfield_shift, playfield_col_mask;
    public byte odd_frame, im2_flag, interlaced, vdp_pal, h_counter;
    public ushort status, ntab, ntbb, ntwb, satb, hscb, bg_list_index, playfield_row_mask;
    public ushort vscroll, v_counter, vc_max, lines_per_frame, max_sprite_pixels;
    public uint dma_length, dma_endCycles, hvc_latch, vint_cycle;
    public byte system_hw;
    private readonly byte[] bg_pattern_cache = new byte[0x80000];
    private readonly byte[] name_lut = new byte[0x400];
    private readonly uint[] bp_lut = new uint[0x10000];
    private readonly byte[] lut = new byte[6 * 0x10000];
    private readonly uint[] pixel = new uint[0x100];
    private readonly uint[] pixel_lut = new uint[3 * 0x200];
    private readonly uint[] pixel_lut_m4 = new uint[0x40];
    private readonly byte[] linebuf = new byte[2 * 0x200];
    private const int SpriteBufferStride = 80;
    private readonly ObjectInfo[] obj_info = new ObjectInfo[2 * SpriteBufferStride];
    private readonly byte[] object_count = new byte[2];
    private readonly Clip[] clip = new Clip[2];
    private byte spr_ovr;
    public ushort spr_col;
    public int ViewportWidth { get => bitmap.viewport.w; set => bitmap.viewport.w = value; }
    public int ViewportHeight { get => bitmap.viewport.h; set => bitmap.viewport.h = value; }
    public int ViewportX { get => bitmap.viewport.x; set => bitmap.viewport.x = value; }
    public int ViewportY { get => bitmap.viewport.y; set => bitmap.viewport.y = value; }
    public int PreviousViewportWidth { get => bitmap.viewport.ow; set => bitmap.viewport.ow = value; }
    public int PreviousViewportHeight { get => bitmap.viewport.oh; set => bitmap.viewport.oh = value; }
    public int ViewportChanged { get => bitmap.viewport.changed; set => bitmap.viewport.changed = value; }
    public ReadOnlyMemory<byte> HorizontalCounterTable => hctab;
    private Action<int> render_bg = null!, render_obj = null!, parse_satb = null!, update_bg_pattern_cache = null!;
    private Action<uint> vdp_68k_data_w = null!, vdp_z80_data_w = null!, set_irq_line = null!, set_irq_line_delay = null!;
    private Func<uint> vdp_68k_data_r = null!, vdp_z80_data_r = null!;
    private readonly Action<uint>[] dma_func;
    private readonly Action<int>[] render_bg_modes;
    private static readonly byte[] cycle2hc32 = cycle2hc32_data.ToArray(), cycle2hc40 = cycle2hc40_data.ToArray();
    private static readonly int[] fifo_timing_h32 = fifo_timing_h32_data.ToArray(), fifo_timing_h40 = fifo_timing_h40_data.ToArray();
    private byte[] hctab = cycle2hc32;
    private int[] fifo_timing = fifo_timing_h32;
    private byte border, pending, code;
    private ushort addr, addr_latch, sat_base_mask, sat_addr_mask, dma_src;
    private int dmafill, cached_write, fifo_idx, fifo_byte_access, hblank_start_cycle, hblank_end_cycle;
    private readonly ushort[] fifo = new ushort[4];
    private const int ASSERT_LINE = 1, CLEAR_LINE = 0, SMS_CYCLE_OFFSET = 530, HW_3D_GLASSES = 1;
    private const uint M68K_INT_ACK_AUTOVECTOR = 0xffffffff;
    private int MainCpuCycles { get => _bus.MainCycles; set => _bus.MainCycles = value; }
    private uint Z80CpuCycles { get => _bus.Z80Cycles; set => _bus.Z80Cycles = value; }
    private byte Z80IrqState { get => _bus.Z80IrqState; set => _bus.Z80IrqState = value; }
    private byte zstate { get => _bus.Z80BusState; set => _bus.Z80BusState = value; }
    private uint mcycles_vdp => _bus.VideoCycles;
    private byte system_bios => _bus.SystemBios;
    private uint CdBootBank => _bus.CdBootBank;
    private uint CartridgeSpecial => _bus.CartridgeSpecial;
    private bool HasSvp => _bus.HasSvp;
    private byte[] work_ram => _bus.WorkRam;

    private struct ObjectInfo { public ushort ypos, xpos, attr, size; }
    private struct Clip { public byte left, right, enable; }

    public Vdp(byte hardware, IVdpBus bus, Configuration configuration, byte[] framebuffer, int width, int height, int pitch)
    {
        system_hw = hardware;
        _bus = bus;
        config = configuration;
        bitmap = new Bitmap { data = framebuffer, width = width, height = height, pitch = pitch, viewport = new Viewport { w = 256, h = 192 } };
        dma_func = [vdp_dma_68k_ext, vdp_dma_68k_ext, vdp_dma_68k_ext, vdp_dma_68k_ext,
            vdp_dma_68k_ram, vdp_dma_68k_io, vdp_dma_68k_ram, vdp_dma_68k_ram,
            vdp_dma_fill, vdp_dma_fill, vdp_dma_fill, vdp_dma_fill,
            vdp_dma_copy, vdp_dma_copy, vdp_dma_copy, vdp_dma_copy];
        render_bg_modes = [render_bg_m0, render_bg_m2, render_bg_m4, render_bg_m4,
            render_bg_m3, render_bg_m3x, render_bg_m4, render_bg_m4,
            render_bg_m1, render_bg_m1x, render_bg_m4, render_bg_m4,
            render_bg_inv, render_bg_inv, render_bg_m4, render_bg_m4];
        InitializeRendererTables();
        _paletteDepth = EffectivePixelDepth;
        if (config.ntsc != 0) InitializeNtsc();
    }

    public void Initialize(byte pal) { vdp_pal = pal; vdp_init(); }
    public void Reset() { render_reset(); Array.Clear(_palette16); vdp_reset(); }
    public void RenderLine(int line) => render_line(line);
    public void BlankLine(int line, int offset, int width) => blank_line(line, offset, width);
    public void ParseSprites(int line) => parse_satb(line);
    public void RenderObjects(int line) => render_obj(line);
    public void SelectMode5Renderers()
    {
        if (im2_flag != 0)
        {
            parse_satb = parse_satb_m5_im2;
            render_bg = (reg[11] & 4) != 0 ? render_bg_m5_im2_vs : render_bg_m5_im2;
            render_obj = (reg[12] & 8) != 0 ? render_obj_m5_im2_ste : render_obj_m5_im2;
        }
        else
        {
            parse_satb = parse_satb_m5;
            render_bg = (reg[11] & 4) != 0 ? render_bg_m5_vs : render_bg_m5;
            render_obj = (reg[12] & 8) != 0 ? render_obj_m5_ste : render_obj_m5;
        }
    }
    public void UpdateDma(uint cycles) => vdp_dma_update(cycles);
    public void WriteMainControl(uint value) => vdp_68k_ctrl_w(value);
    public uint ReadMainControl(uint cycles) => vdp_68k_ctrl_r(cycles);
    public void WriteMainData(uint value) => vdp_68k_data_w(value);
    public uint ReadMainData() => vdp_68k_data_r();
    public void WriteZ80Control(uint value) => vdp_z80_ctrl_w(value);
    public void WriteSmsControl(uint value) => vdp_sms_ctrl_w(value);
    public void WriteTmsControl(uint value) => vdp_tms_ctrl_w(value);
    public uint ReadZ80Control(uint cycles) => vdp_z80_ctrl_r(cycles);
    public void WriteZ80Data(uint value) => vdp_z80_data_w(value);
    public uint ReadZ80Data() => vdp_z80_data_r();
    public uint ReadCounters(uint cycles) => vdp_hvc_r(cycles);
    public void WriteTest(uint value) => vdp_test_w(value);
    public int AcknowledgeInterrupt(int level) => vdp_68k_irq_ack(level);
    private void m68k_set_irq(uint level) => _bus.SetMainIrq(level);
    private void m68k_set_irq_delay(uint level) => _bus.SetMainIrqDelay(level);
    private void z80_set_irq_line(uint level) => _bus.SetZ80Irq(level);
    private int m68k_cycles() => _bus.MainInstructionCycles;
    private uint io_68k_read(uint register) => _bus.ReadIo(register);

    private void vdp_dma_68k_ext(uint length)
    {
        unchecked
        {
            uint source = (uint)((reg[23] << 17) | (dma_src << 1));
            do
            {
                ushort data = (ushort)_bus.ReadDmaWord(source);
                source += 2;
                source = (uint)(reg[23] << 17) | (source & 0x1ffff);
                vdp_bus_w(data);
            }
            while (--length != 0);
            dma_src = (ushort)((source >> 1) & 0xffff);
        }
    }

    private void InitializeRendererTables()
    {
        for (uint bx = 0; bx < 256; bx++)
            for (uint ax = 0; ax < 256; ax++)
            {
                int index = (int)(bx << 8 | ax);
                lut[index] = (byte)make_lut_bg(bx, ax);
                lut[0x10000 + index] = (byte)make_lut_bgobj(bx, ax);
                lut[0x20000 + index] = (byte)make_lut_bg_ste(bx, ax);
                lut[0x30000 + index] = (byte)make_lut_obj(bx, ax);
                lut[0x40000 + index] = (byte)make_lut_bgobj_ste(bx, ax);
                lut[0x50000 + index] = (byte)make_lut_bgobj_m4(bx, ax);
            }
        InitializePalette();
        for (int i = 0; i < 0x400; i++)
        {
            int column = i & 3, row = i >> 2 & 3;
            int height = i >> 4 & 3, width = i >> 6 & 3;
            if (row > height || column > width) { name_lut[i] = 255; continue; }
            if ((i & 0x100) != 0) column = width - column;
            if ((i & 0x200) != 0) row = height - row;
            name_lut[i] = (byte)(row + column * (height + 1));
        }
        for (int low = 0; low < 256; low++)
            for (int high = 0; high < 256; high++)
            {
                uint value = 0;
                for (int x = 0; x < 8; x++)
                {
                    if ((high & (0x80 >> x)) != 0) value |= 8u << (x << 2);
                    if ((low & (0x80 >> x)) != 0) value |= 4u << (x << 2);
                }
                bp_lut[high << 8 | low] = value;
            }
    }

    private void window_clip(uint data, uint sw)
    {
        int position = (int)data & 31;
        int planeA = (int)(data >> 7 & 1);
        int window = planeA ^ 1;
        sw = 16 + (sw << 2);
        if (position != 0)
        {
            if (position > sw)
            {
                clip[window].left = 0;
                clip[window].right = (byte)sw;
                clip[window].enable = 1;
                clip[planeA].enable = 0;
            }
            else
            {
                clip[window].left = 0;
                clip[planeA].right = (byte)sw;
                clip[planeA].left = clip[window].right = (byte)position;
                clip[0].enable = clip[1].enable = 1;
            }
        }
        else
        {
            clip[planeA].left = 0;
            clip[planeA].right = (byte)sw;
            clip[planeA].enable = 1;
            clip[window].enable = 0;
        }
    }

    private static void memset(ArrayPointer<byte> destination, int value, uint length) => destination.Bytes(unchecked((int)length)).Fill(unchecked((byte)value));
    private static void memcpy(ArrayPointer<byte> destination, ArrayPointer<byte> source, uint length) => source.Bytes(unchecked((int)length)).CopyTo(destination.Bytes(unchecked((int)length)));
    private static int abs(int value) => value < 0 ? unchecked(-value) : value;
}
