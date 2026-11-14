using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Chips.Video.Sega.VDP;

public sealed partial class Vdp
{
    private void vdp_init()
    {
        unchecked
        {
            lines_per_frame = unchecked((ushort)(((vdp_pal != 0) ? 313 : 262)));
            if (((unchecked((int)(system_hw)) & SYSTEM_PBC) == SYSTEM_MD)) {
            set_irq_line = m68k_set_irq;
            set_irq_line_delay = m68k_set_irq_delay;
            } else {
            set_irq_line = z80_set_irq_line;
            set_irq_line_delay = z80_set_irq_line;
            }
        }
    }
    private void vdp_reset()
    {
        unchecked
        {
            int i = default;
            memset(new ArrayPointer<byte>(sat).Cast<sbyte>().Cast<byte>(), 0, unchecked((uint)(1024)));
            memset(new ArrayPointer<byte>(vram).Cast<sbyte>().Cast<byte>(), 0, unchecked((uint)(65536)));
            memset(new ArrayPointer<byte>(cram).Cast<sbyte>().Cast<byte>(), 0, unchecked((uint)(128)));
            memset(new ArrayPointer<byte>(vsram).Cast<sbyte>().Cast<byte>(), 0, unchecked((uint)(128)));
            memset(new ArrayPointer<byte>(reg).Cast<sbyte>().Cast<byte>(), 0, unchecked((uint)(32)));
            addr = unchecked((ushort)(0));
            addr_latch = unchecked((ushort)(0));
            code = unchecked((byte)(0));
            pending = unchecked((byte)(0));
            border = unchecked((byte)(0));
            hint_pending = unchecked((byte)(0));
            vint_pending = unchecked((byte)(0));
            dmafill = 0;
            dma_src = unchecked((ushort)(0));
            dma_type = unchecked((byte)(0));
            dma_length = unchecked((uint)(0));
            dma_endCycles = unchecked((uint)(0));
            odd_frame = unchecked((byte)(0));
            im2_flag = unchecked((byte)(0));
            interlaced = unchecked((byte)(0));
            fifo_idx = 0;
            cached_write = (-1);
            fifo_byte_access = 1;
            ntab = unchecked((ushort)(0));
            ntbb = unchecked((ushort)(0));
            ntwb = unchecked((ushort)(0));
            satb = unchecked((ushort)(0));
            hscb = unchecked((ushort)(0));
            vscroll = unchecked((ushort)(0));
            hscroll_mask = unchecked((byte)(0));
            playfield_shift = unchecked((byte)(6));
            playfield_col_mask = unchecked((byte)(15));
            playfield_row_mask = unchecked((ushort)(255));
            sat_base_mask = unchecked((ushort)(65024));
            sat_addr_mask = unchecked((ushort)(511));
            bg_list_index = unchecked((ushort)(0));
            memset(new ArrayPointer<byte>(bg_name_dirty).Cast<sbyte>().Cast<byte>(), 0, unchecked((uint)(2048)));
            memset(new ArrayPointer<ushort>(bg_name_list).Cast<sbyte>().Cast<byte>(), 0, unchecked((uint)(4096)));
            window_clip(unchecked((uint)(0)), unchecked((uint)(0)));
            if (((unchecked((int)(system_hw)) & SYSTEM_MD) != 0)) {
            status = unchecked((ushort)(vdp_pal));
            } else {
            status = unchecked((ushort)(0));
            }
            bitmap.viewport.w = 256;
            bitmap.viewport.h = 192;
            bitmap.viewport.ow = 256;
            bitmap.viewport.oh = 192;
            hvc_latch = unchecked((uint)(65536));
            hctab = cycle2hc32;
            vc_max = new ArrayPointer<ushort>(vc_table, (int)(0) * 2)[(int)(vdp_pal)];
            v_counter = unchecked((ushort)(bitmap.viewport.h));
            h_counter = unchecked((byte)(255));
            max_sprite_pixels = unchecked((ushort)(256));
            fifo_timing = fifo_timing_h32;
            vint_cycle = unchecked((uint)(770));
            hblank_start_cycle = 280;
            hblank_end_cycle = 860;
            if (((unchecked((int)(system_hw)) == SYSTEM_GG) && (!(config.gg_extra != 0)))) {
            bitmap.viewport.x = (((config.overscan & 2) != 0) ? 14 : (-48));
            bitmap.viewport.y = (((config.overscan & 1) != 0) ? (24 * (unchecked((int)(vdp_pal)) + 1)) : (-24));
            } else {
            bitmap.viewport.x = ((config.overscan & 2) * 7);
            bitmap.viewport.y = (((config.overscan & 1) * 24) * (unchecked((int)(vdp_pal)) + 1));
            }
            update_bg_pattern_cache = update_bg_pattern_cache_m4;
            if ((unchecked((int)(system_hw)) < SYSTEM_MD)) {
            render_bg = render_bg_m0;
            render_obj = render_obj_tms;
            parse_satb = parse_satb_tms;
            } else {
            render_bg = render_bg_m4;
            render_obj = render_obj_m4;
            parse_satb = parse_satb_m4;
            }
            vdp_68k_data_w = vdp_68k_data_w_m4;
            vdp_68k_data_r = vdp_68k_data_r_m4;
            switch (system_hw) {
            case SYSTEM_SG:
            case SYSTEM_SGII:
            case SYSTEM_SGII_RAM_EXT:
            {
            vdp_z80_data_w = vdp_z80_data_w_sg;
            vdp_z80_data_r = vdp_z80_data_r_m4;
            break;
            }
            case SYSTEM_GG:
            {
            vdp_z80_data_w = vdp_z80_data_w_gg;
            vdp_z80_data_r = vdp_z80_data_r_m4;
            break;
            }
            case SYSTEM_MARKIII:
            case SYSTEM_SMS:
            case SYSTEM_SMS2:
            case SYSTEM_GGMS:
            {
            vdp_z80_data_w = vdp_z80_data_w_ms;
            vdp_z80_data_r = vdp_z80_data_r_m4;
            break;
            }
            default:
            {
            vdp_z80_data_w = vdp_z80_data_w_m4;
            vdp_z80_data_r = vdp_z80_data_r_m4;
            break;
            }
            }
            reg[(int)(10)] = unchecked((byte)(255));
            if ((((unchecked((int)(system_hw)) & SYSTEM_SMS) != 0) && ((!((config.bios & 1) != 0)) || (!((unchecked((int)(system_bios)) & SYSTEM_SMS) != 0))))) {
            vdp_reg_w(unchecked((uint)(0)), unchecked((uint)(54)), unchecked((uint)(0)));
            vdp_reg_w(unchecked((uint)(1)), unchecked((uint)(128)), unchecked((uint)(0)));
            vdp_reg_w(unchecked((uint)(2)), unchecked((uint)(255)), unchecked((uint)(0)));
            vdp_reg_w(unchecked((uint)(3)), unchecked((uint)(255)), unchecked((uint)(0)));
            vdp_reg_w(unchecked((uint)(4)), unchecked((uint)(255)), unchecked((uint)(0)));
            vdp_reg_w(unchecked((uint)(5)), unchecked((uint)(255)), unchecked((uint)(0)));
            vdp_reg_w(unchecked((uint)(6)), unchecked((uint)(255)), unchecked((uint)(0)));
            render_bg = render_bg_m4;
            render_obj = render_obj_m4;
            parse_satb = parse_satb_m4;
            } else if (((unchecked((int)(system_hw)) & SYSTEM_GG) != 0)) {
            vdp_reg_w(unchecked((uint)(2)), unchecked((uint)(255)), unchecked((uint)(0)));
            vdp_reg_w(unchecked((uint)(3)), unchecked((uint)(255)), unchecked((uint)(0)));
            vdp_reg_w(unchecked((uint)(4)), unchecked((uint)(255)), unchecked((uint)(0)));
            vdp_reg_w(unchecked((uint)(5)), unchecked((uint)(255)), unchecked((uint)(0)));
            vdp_reg_w(unchecked((uint)(6)), unchecked((uint)(255)), unchecked((uint)(0)));
            } else if (((((unchecked((int)(system_hw)) == SYSTEM_MD) || (unchecked((int)(system_hw)) == SYSTEM_MCD)) && ((config.bios & 1) != 0)) && (!((unchecked((int)(system_bios)) & SYSTEM_MD) != 0)))) {
            vdp_reg_w(unchecked((uint)(0)), unchecked((uint)(4)), unchecked((uint)(0)));
            vdp_reg_w(unchecked((uint)(1)), unchecked((uint)(4)), unchecked((uint)(0)));
            vdp_reg_w(unchecked((uint)(12)), unchecked((uint)(129)), unchecked((uint)(0)));
            vdp_reg_w(unchecked((uint)(15)), unchecked((uint)(2)), unchecked((uint)(0)));
            }
            for (i = 0; (i < 32); i++) {
            color_update_m4(i, unchecked((uint)(0)));
            }
            color_update_m4(64, unchecked((uint)(0)));
        }
    }
    private void vdp_dma_update(uint cycles)
    {
        unchecked
        {
            int dma_cycles = default;
            int dma_bytes = default;
            uint rate = unchecked((uint)(new ArrayPointer<byte>(dma_timing, (int)(((((unchecked((int)(status)) & 8) != 0) || (!((unchecked((int)(reg[(int)(1)])) & 64) != 0))) ? 1 : 0)) * 2)[(int)((unchecked((int)(reg[(int)(12)])) & 1))]));
            rate = (rate >> (unchecked((int)(dma_type)) & 1));
            if ((unchecked((int)(dma_type)) == 0)) {
            if ((rate == unchecked((uint)(166)))) rate = unchecked((uint)(161)); else if ((rate == unchecked((uint)(204)))) rate = unchecked((uint)(198));
            } else if ((dmafill != 0)) {
            cycles += (unchecked((uint)(2)) * (unchecked((uint)(MCYCLES_PER_LINE)) / rate));
            }
            if (((unchecked((int)(status)) & 8) != 0)) {
            dma_cycles = unchecked((int)((unchecked((uint)((((unchecked((int)(lines_per_frame)) - bitmap.viewport.h) - 1) * MCYCLES_PER_LINE))) - cycles)));
            } else {
            dma_cycles = unchecked((int)(((mcycles_vdp + unchecked((uint)(MCYCLES_PER_LINE))) - cycles)));
            }
            dma_bytes = unchecked((int)(((unchecked((uint)(dma_cycles)) * rate) / unchecked((uint)(MCYCLES_PER_LINE)))));
            if ((dma_length < unchecked((uint)(dma_bytes)))) {
            dma_bytes = unchecked((int)(dma_length));
            dma_cycles = unchecked((int)((unchecked((uint)((dma_bytes * MCYCLES_PER_LINE))) / rate)));
            }
            dma_endCycles = (cycles + unchecked((uint)(dma_cycles)));
            if ((unchecked((int)(dma_type)) < 2)) {
            MainCpuCycles = unchecked((int)(dma_endCycles));
            if (((unchecked((int)(zstate)) & 4) != 0)) {
            Z80CpuCycles = dma_endCycles;
            }
            } else {
            status |= unchecked((ushort)(2));
            }
            if ((dma_bytes > 0)) {
            dma_length -= unchecked((uint)(dma_bytes));
            dma_func[(int)((unchecked((int)(reg[(int)(23)])) >> 4))](unchecked((uint)(dma_bytes)));
            if ((!(dma_length != 0))) {
            ushort end = unchecked((ushort)((((unchecked((int)(reg[(int)(21)])) + (unchecked((int)(reg[(int)(22)])) << 8)) + unchecked((int)(reg[(int)(19)]))) + (unchecked((int)(reg[(int)(20)])) << 8))));
            reg[(int)(21)] = unchecked((byte)((unchecked((int)(end)) & 255)));
            reg[(int)(22)] = unchecked((byte)((unchecked((int)(end)) >> 8)));
            reg[(int)(19)] = (reg[(int)(20)] = unchecked((byte)(0)));
            if ((cached_write >= 0)) {
            vdp_68k_ctrl_w(unchecked((uint)(cached_write)));
            cached_write = (-1);
            }
            zstate &= unchecked((byte)((~4)));
            }
            }
        }
    }
    private void vdp_68k_ctrl_w(uint data)
    {
        unchecked
        {
            if ((unchecked((int)(pending)) == 0)) {
            if ((dma_length != 0)) {
            if ((unchecked((int)(dma_type)) < 2)) {
            cached_write = unchecked((int)(data));
            return;
            }
            }
            addr = unchecked((ushort)((unchecked((uint)(addr_latch)) | (data & unchecked((uint)(16383))))));
            code = unchecked((byte)((unchecked((uint)((unchecked((int)(code)) & 60))) | ((data >> 14) & unchecked((uint)(3))))));
            if (((data & unchecked((uint)(49152))) == unchecked((uint)(32768)))) {
            vdp_reg_w(((data >> 8) & unchecked((uint)(31))), (data & unchecked((uint)(255))), unchecked((uint)(MainCpuCycles)));
            } else {
            pending = unchecked((byte)((unchecked((int)(reg[(int)(1)])) & 4)));
            }
            } else {
            pending = unchecked((byte)(0));
            addr_latch = unchecked((ushort)(((data & unchecked((uint)(3))) << 14)));
            addr = unchecked((ushort)((unchecked((int)(addr_latch)) | (unchecked((int)(addr)) & 16383))));
            code = unchecked((byte)((unchecked((uint)((unchecked((int)(code)) & 3))) | ((data >> 2) & unchecked((uint)(60))))));
            if (((unchecked((int)(code)) & 32) != 0)) {
            if (((unchecked((int)(reg[(int)(1)])) & 16) != 0)) {
            switch ((unchecked((int)(reg[(int)(23)])) >> 6)) {
            case 2:
            {
            dma_type = unchecked((byte)(2));
            dmafill = 1;
            status |= unchecked((ushort)(2));
            dma_endCycles = 4294967295u;
            break;
            }
            case 3:
            {
            dma_type = unchecked((byte)(3));
            dma_length = unchecked((uint)(((unchecked((int)(reg[(int)(20)])) << 8) | unchecked((int)(reg[(int)(19)])))));
            if ((!(dma_length != 0))) {
            dma_length = unchecked((uint)(65536));
            }
            dma_src = unchecked((ushort)(((unchecked((int)(reg[(int)(22)])) << 8) | unchecked((int)(reg[(int)(21)])))));
            vdp_dma_update(unchecked((uint)(MainCpuCycles)));
            break;
            }
            default:
            {
            dma_type = unchecked((byte)((((unchecked((int)(code)) & 6) != 0) ? 0 : 1)));
            dma_length = unchecked((uint)(((unchecked((int)(reg[(int)(20)])) << 8) | unchecked((int)(reg[(int)(19)])))));
            if ((!(dma_length != 0))) {
            dma_length = unchecked((uint)(65536));
            }
            dma_src = unchecked((ushort)(((unchecked((int)(reg[(int)(22)])) << 8) | unchecked((int)(reg[(int)(21)])))));
            if ((((unchecked((int)(system_hw)) == SYSTEM_MCD) && (unchecked((uint)((unchecked((int)(reg[(int)(23)])) & 112))) == ((CdBootBank >> 1) + unchecked((uint)(16))))) || (HasSvp && (!((unchecked((int)(reg[(int)(23)])) & 96) != 0))))) {
            addr += unchecked((ushort)(reg[(int)(15)]));
            dma_length--;
            }
            vdp_dma_update(unchecked((uint)(MainCpuCycles)));
            break;
            }
            }
            }
            }
            }
            fifo_byte_access = (((unchecked((int)(code)) & 6) != 0) ? 0 : 1);
        }
    }
    private void vdp_z80_ctrl_w(uint data)
    {
        unchecked
        {
            switch (pending) {
            case 0:
            {
            addr_latch = unchecked((ushort)(data));
            pending = unchecked((byte)(1));
            return;
            }
            case 1:
            {
            addr = unchecked((ushort)(((unchecked((uint)((unchecked((int)(addr)) & 49152))) | ((data & unchecked((uint)(63))) << 8)) | unchecked((uint)(addr_latch)))));
            code = unchecked((byte)((unchecked((uint)((unchecked((int)(code)) & 60))) | ((data >> 6) & unchecked((uint)(3))))));
            if (((unchecked((int)(code)) & 3) == 2)) {
            vdp_reg_w((data & unchecked((uint)(31))), unchecked((uint)(addr_latch)), Z80CpuCycles);
            pending = unchecked((byte)(0));
            return;
            }
            pending = unchecked((byte)(((unchecked((int)(reg[(int)(1)])) & 4) >> 1)));
            if (((!(pending != 0)) && (!((unchecked((int)(code)) & 3) != 0)))) {
            fifo[(int)(0)] = unchecked((ushort)(vram[(int)((unchecked((int)(addr)) & 16383))]));
            addr += unchecked((ushort)((unchecked((int)(reg[(int)(15)])) + 1)));
            }
            return;
            }
            case 2:
            {
            addr_latch = unchecked((ushort)(data));
            pending = unchecked((byte)(3));
            return;
            }
            case 3:
            {
            pending = unchecked((byte)(0));
            addr = unchecked((ushort)((((unchecked((int)(addr_latch)) & 3) << 14) | (unchecked((int)(addr)) & 16383))));
            code = unchecked((byte)(((unchecked((int)(code)) & 3) | ((unchecked((int)(addr_latch)) >> 2) & 60))));
            if (((unchecked((int)(code)) & 32) != 0)) {
            if (((unchecked((int)(reg[(int)(1)])) & 16) != 0)) {
            switch ((unchecked((int)(reg[(int)(23)])) >> 6)) {
            case 2:
            {
            dma_type = unchecked((byte)(2));
            dmafill = 1;
            status |= unchecked((ushort)(2));
            dma_endCycles = 4294967295u;
            break;
            }
            case 3:
            {
            dma_type = unchecked((byte)(3));
            dma_length = unchecked((uint)(((unchecked((int)(reg[(int)(20)])) << 8) | unchecked((int)(reg[(int)(19)])))));
            if ((!(dma_length != 0))) {
            dma_length = unchecked((uint)(65536));
            }
            dma_src = unchecked((ushort)(((unchecked((int)(reg[(int)(22)])) << 8) | unchecked((int)(reg[(int)(21)])))));
            vdp_dma_update(Z80CpuCycles);
            break;
            }
            default:
            {
            break;
            }
            }
            }
            }
            }
            return;
            }
        }
    }
    private void vdp_sms_ctrl_w(uint data)
    {
        unchecked
        {
            if ((unchecked((int)(pending)) == 0)) {
            addr = unchecked((ushort)((unchecked((uint)((unchecked((int)(addr)) & 16128))) | (data & unchecked((uint)(255))))));
            addr_latch = unchecked((ushort)(data));
            pending = unchecked((byte)(1));
            } else {
            code = unchecked((byte)(((data >> 6) & unchecked((uint)(3)))));
            addr = unchecked((ushort)((((data << 8) | unchecked((uint)(addr_latch))) & unchecked((uint)(16383)))));
            pending = unchecked((byte)(0));
            if ((unchecked((int)(code)) == 0)) {
            fifo[(int)(0)] = unchecked((ushort)(vram[(int)((unchecked((int)(addr)) & 16383))]));
            addr = unchecked((ushort)(((unchecked((int)(addr)) + 1) & 16383)));
            return;
            }
            if ((unchecked((int)(code)) == 2)) {
            int mode = default;
            int prev = ((unchecked((int)(reg[(int)(0)])) & 6) | (unchecked((int)(reg[(int)(1)])) & 24));
            vdp_reg_w((data & unchecked((uint)(15))), unchecked((uint)(addr_latch)), Z80CpuCycles);
            mode = ((unchecked((int)(reg[(int)(0)])) & 6) | (unchecked((int)(reg[(int)(1)])) & 24));
            prev ^= mode;
            if ((prev != 0)) {
            if ((unchecked((int)(system_hw)) > SYSTEM_SMS)) {
            int height = default;
            if ((mode == 14)) {
            height = 240;
            vc_max = new ArrayPointer<ushort>(vc_table, (int)(3) * 2)[(int)(vdp_pal)];
            } else if ((mode == 22)) {
            height = 224;
            vc_max = new ArrayPointer<ushort>(vc_table, (int)(1) * 2)[(int)(vdp_pal)];
            } else {
            height = 192;
            vc_max = new ArrayPointer<ushort>(vc_table, (int)(0) * 2)[(int)(vdp_pal)];
            }
            if ((height != bitmap.viewport.h)) {
            bitmap.viewport.changed |= 2;
            }
            }
            render_bg = render_bg_modes[(int)((mode >> 1))];
            if (((prev & 4) != 0)) {
            int i = default;
            if (((mode & 4) != 0)) {
            parse_satb = parse_satb_m4;
            render_obj = render_obj_m4;
            bg_list_index = unchecked((ushort)(512));
            for (i = 0; (i < 512); i++) {
            bg_name_list[(int)(i)] = unchecked((ushort)(i));
            bg_name_dirty[(int)(i)] = unchecked((byte)(255));
            }
            } else {
            parse_satb = parse_satb_tms;
            render_obj = render_obj_tms;
            bg_list_index = unchecked((ushort)(0));
            }
            for (i = 0; (i < 32); i++) {
            color_update_m4(i, unchecked((uint)(new ArrayPointer<byte>(cram, (int)((i << 1))).Cast<ushort>()[0])));
            }
            color_update_m4(64, unchecked((uint)(new ArrayPointer<byte>(cram, (int)(((16 | (unchecked((int)(border)) & 15)) << 1))).Cast<ushort>()[0])));
            }
            }
            }
            }
        }
    }
    private void vdp_tms_ctrl_w(uint data)
    {
        unchecked
        {
            if ((unchecked((int)(pending)) == 0)) {
            addr_latch = unchecked((ushort)(data));
            pending = unchecked((byte)(1));
            } else {
            code = unchecked((byte)(((data >> 6) & unchecked((uint)(3)))));
            addr = unchecked((ushort)((((data << 8) | unchecked((uint)(addr_latch))) & unchecked((uint)(16383)))));
            pending = unchecked((byte)(0));
            if ((unchecked((int)(code)) == 0)) {
            fifo[(int)(0)] = unchecked((ushort)(vram[(int)((unchecked((int)(addr)) & 16383))]));
            addr = unchecked((ushort)(((unchecked((int)(addr)) + 1) & 16383)));
            return;
            }
            if (((unchecked((int)(code)) & 2) != 0)) {
            data &= unchecked((uint)(7));
            vdp_reg_w(data, unchecked((uint)(addr_latch)), Z80CpuCycles);
            if ((data < unchecked((uint)(2)))) {
            render_bg = render_bg_modes[(int)((((unchecked((int)(reg[(int)(0)])) & 2) | (unchecked((int)(reg[(int)(1)])) & 24)) >> 1))];
            }
            }
            }
        }
    }
    private uint vdp_68k_ctrl_r(uint cycles)
    {
        unchecked
        {
            uint temp = default;
            cycles += unchecked((uint)(m68k_cycles()));
            if (((unchecked((int)(status)) & 2) != 0)) {
            if (((!(dma_length != 0)) && (cycles >= dma_endCycles))) {
            status &= unchecked((ushort)(65533));
            }
            }
            temp = unchecked((uint)(status));
            pending = unchecked((byte)(0));
            status &= unchecked((ushort)(65439));
            if ((cycles >= fifo_cycles[(int)(((fifo_idx + 3) & 3))])) {
            temp |= unchecked((uint)(512));
            } else if ((cycles < fifo_cycles[(int)(fifo_idx)])) {
            temp |= unchecked((uint)(256));
            }
            if ((!((unchecked((int)(reg[(int)(1)])) & 64) != 0))) {
            temp |= unchecked((uint)(8));
            }
            cycles -= mcycles_vdp;
            if (((unchecked((int)(v_counter)) == bitmap.viewport.h) && (cycles >= vint_cycle))) {
            if ((unchecked((int)(Z80IrqState)) != ASSERT_LINE)) {
            temp |= unchecked((uint)(128));
            }
            }
            if (((cycles >= unchecked((uint)(hblank_start_cycle))) && (cycles < unchecked((uint)(hblank_end_cycle))))) {
            temp |= unchecked((uint)(4));
            }
            return temp;
        }
    }
    private uint vdp_z80_ctrl_r(uint cycles)
    {
        unchecked
        {
            uint temp = default;
            if (((unchecked((int)(status)) & 2) != 0)) {
            if (((!(dma_length != 0)) && (cycles >= dma_endCycles))) {
            status &= unchecked((ushort)(253));
            }
            }
            if (((cycles - mcycles_vdp) >= unchecked((uint)(MCYCLES_PER_LINE)))) {
            if ((unchecked((int)(v_counter)) == bitmap.viewport.h)) {
            v_counter++;
            status |= unchecked((ushort)(128));
            } else {
            int line = ((unchecked((int)(v_counter)) + 1) % unchecked((int)(lines_per_frame)));
            if (((line < bitmap.viewport.h) && (!(((unchecked((uint)(work_ram[(int)(8187)])) & CartridgeSpecial) & unchecked((uint)(HW_3D_GLASSES))) != 0)))) {
            v_counter = unchecked((ushort)(line));
            render_line(line);
            }
            }
            }
            temp = unchecked((uint)(status));
            pending = unchecked((byte)(0));
            status &= unchecked((ushort)(65311));
            if (((unchecked((int)(system_hw)) & SYSTEM_MD) != 0)) {
            if ((!((unchecked((int)(reg[(int)(1)])) & 64) != 0))) {
            temp |= unchecked((uint)(8));
            }
            if (((cycles % unchecked((uint)(MCYCLES_PER_LINE))) < unchecked((uint)(588)))) {
            temp |= unchecked((uint)(4));
            }
            } else if (((unchecked((int)(reg[(int)(0)])) & 4) != 0)) {
            temp |= unchecked((uint)(31));
            }
            if ((((temp & unchecked((uint)(32))) != 0) && (unchecked((int)(v_counter)) == (unchecked((int)(spr_col)) >> 8)))) {
            if (((unchecked((int)(system_hw)) & SYSTEM_MD) != 0)) {
            if (((cycles % unchecked((uint)(MCYCLES_PER_LINE))) < unchecked((uint)(105)))) {
            status |= unchecked((ushort)(32));
            temp &= unchecked((uint)((~32)));
            }
            } else {
            byte hc = hctab[(int)((((cycles + unchecked((uint)(SMS_CYCLE_OFFSET))) + unchecked((uint)(15))) % unchecked((uint)(MCYCLES_PER_LINE))))];
            if (((unchecked((int)(hc)) < (unchecked((int)(spr_col)) & 255)) || (unchecked((int)(hc)) > 243))) {
            status |= unchecked((ushort)(32));
            temp &= unchecked((uint)((~32)));
            }
            }
            }
            hint_pending = (vint_pending = unchecked((byte)(0)));
            Z80IrqState = unchecked((byte)(CLEAR_LINE));
            return temp;
        }
    }
    private uint vdp_hvc_r(uint cycles)
    {
        unchecked
        {
            int vc = default;
            uint data = hvc_latch;
            if ((data != 0)) {
            if (((unchecked((int)(reg[(int)(1)])) & 4) != 0)) {
            return (data & unchecked((uint)(65535)));
            } else {
            data &= unchecked((uint)(255));
            }
            } else {
            data = unchecked((uint)(hctab[(int)((cycles % unchecked((uint)(MCYCLES_PER_LINE))))]));
            }
            vc = unchecked((int)(v_counter));
            if (((cycles - mcycles_vdp) >= unchecked((uint)(MCYCLES_PER_LINE)))) {
            vc = ((vc + 1) % unchecked((int)(lines_per_frame)));
            }
            if ((vc > unchecked((int)(vc_max)))) {
            vc -= unchecked((int)(lines_per_frame));
            }
            if ((interlaced != 0)) {
            vc <<= unchecked((int)(im2_flag));
            vc = ((vc & (~1)) | ((vc >> 8) & 1));
            }
            data |= unchecked((uint)(((vc & 255) << 8)));
            return data;
        }
    }
    private void vdp_test_w(uint data)
    {
        unchecked
        {

        }
    }
    private int vdp_68k_irq_ack(int int_level)
    {
        unchecked
        {
            if (((unchecked((int)(reg[(int)(1)])) & unchecked((int)(vint_pending))) != 0)) {
            vint_pending = unchecked((byte)(0));
            status &= unchecked((ushort)((~128)));
            if (((unchecked((int)(reg[(int)(0)])) & unchecked((int)(hint_pending))) != 0)) {
            m68k_set_irq(unchecked((uint)(4)));
            } else {
            m68k_set_irq(unchecked((uint)(0)));
            }
            } else {
            hint_pending = unchecked((byte)(0));
            m68k_set_irq(unchecked((uint)(0)));
            }
            return unchecked((int)(M68K_INT_ACK_AUTOVECTOR));
        }
    }
    private void vdp_reg_w(uint r, uint d, uint cycles)
    {
        unchecked
        {
            if (((!((unchecked((int)(reg[(int)(1)])) & 4) != 0)) && (r > unchecked((uint)(10))))) {
            return;
            }
            switch (r) {
            case 0:
            {
            r = (d ^ unchecked((uint)(reg[(int)(0)])));
            reg[(int)(0)] = unchecked((byte)(d));
            if (((r & unchecked((uint)(hint_pending))) != 0)) {
            if (((unchecked((int)(reg[(int)(1)])) & unchecked((int)(vint_pending))) != 0)) {
            set_irq_line(unchecked((uint)(6)));
            } else if (((d & unchecked((uint)(16))) != 0)) {
            set_irq_line_delay(unchecked((uint)(4)));
            } else {
            set_irq_line(unchecked((uint)(0)));
            }
            }
            if (((r & unchecked((uint)(4))) != 0)) {
            if (((unchecked((int)(system_hw)) & SYSTEM_MD) != 0)) {
            int i = default;
            if (((unchecked((int)(reg[(int)(1)])) & 4) != 0)) {
            color_update_m5(0, unchecked((uint)(new ArrayPointer<byte>(cram, (int)((unchecked((int)(border)) << 1))).Cast<ushort>()[0])));
            for (i = 1; (i < 64); i++) {
            color_update_m5(i, unchecked((uint)(new ArrayPointer<byte>(cram, (int)((i << 1))).Cast<ushort>()[0])));
            }
            } else {
            for (i = 0; (i < 32); i++) {
            color_update_m4(i, unchecked((uint)(new ArrayPointer<byte>(cram, (int)((i << 1))).Cast<ushort>()[0])));
            }
            color_update_m4(64, unchecked((uint)(new ArrayPointer<byte>(cram, (int)(((16 | (unchecked((int)(border)) & 15)) << 1))).Cast<ushort>()[0])));
            }
            }
            }
            if (((r & unchecked((uint)(2))) != 0)) {
            if (((unchecked((int)(system_hw)) & SYSTEM_MD) != 0)) {
            if (((unchecked((int)(reg[(int)(1)])) & 4) != 0)) {
            if (((d & unchecked((uint)(2))) != 0)) {
            hvc_latch = (vdp_hvc_r(cycles) | unchecked((uint)(65536)));
            } else {
            hvc_latch = unchecked((uint)(0));
            }
            }
            }
            }
            break;
            }
            case 1:
            {
            r = (d ^ unchecked((uint)(reg[(int)(1)])));
            reg[(int)(1)] = unchecked((byte)(d));
            if (((r & unchecked((uint)(128))) != 0)) {
            if ((unchecked((int)(system_hw)) == SYSTEM_SG)) {
            int i = default;
            memcpy((new ArrayPointer<byte>(vram) + 16384), new ArrayPointer<byte>(vram), unchecked((uint)(16384)));
            if (((d & unchecked((uint)(128))) != 0)) {
            for (i = 0; (i < 16384); i += 2) {
            (new ArrayPointer<byte>(vram) + (((i & 8255) | ((i << 6) & 4096)) | ((i >> 1) & 4032))).Cast<ushort>()[0] = ((new ArrayPointer<byte>(vram) + 16384) + i).Cast<ushort>()[0];
            }
            } else {
            for (i = 0; (i < 16384); i += 2) {
            (new ArrayPointer<byte>(vram) + (((i & 8255) | ((i >> 6) & 64)) | ((i << 1) & 8064))).Cast<ushort>()[0] = ((new ArrayPointer<byte>(vram) + 16384) + i).Cast<ushort>()[0];
            }
            }
            }
            }
            if ((((r & unchecked((uint)(64))) != 0) && (unchecked((int)(v_counter)) < bitmap.viewport.h))) {
            int offset = unchecked((int)((cycles - mcycles_vdp)));
            if ((offset <= 860)) {
            if (((d & unchecked((uint)(64))) != 0)) {
            if ((offset > 360)) {
            max_sprite_pixels = unchecked((ushort)(128));
            }
            }
            render_line(unchecked((int)(v_counter)));
            max_sprite_pixels = unchecked((ushort)((256 + ((unchecked((int)(reg[(int)(12)])) & 1) << 6))));
            } else if (((unchecked((int)(system_hw)) & SYSTEM_MD) != 0)) {
            if (((unchecked((int)(reg[(int)(12)])) & 1) != 0)) {
            offset = (((offset - 860) / 8) + 16);
            } else {
            offset = (((offset - 860) / 10) + 16);
            }
            if ((offset < bitmap.viewport.w)) {
            if (((d & unchecked((uint)(64))) != 0)) {
            render_line(unchecked((int)(v_counter)));
            blank_line(unchecked((int)(v_counter)), 0, offset);
            } else {
            blank_line(unchecked((int)(v_counter)), offset, (bitmap.viewport.w - offset));
            }
            }
            }
            }
            if (((r & unchecked((uint)(vint_pending))) != 0)) {
            if (((d & unchecked((uint)(32))) != 0)) {
            set_irq_line_delay(unchecked((uint)(6)));
            } else if (((unchecked((int)(reg[(int)(0)])) & unchecked((int)(hint_pending))) != 0)) {
            set_irq_line(unchecked((uint)(4)));
            } else {
            set_irq_line(unchecked((uint)(0)));
            }
            }
            if (((r & unchecked((uint)(8))) != 0)) {
            if (((unchecked((int)(system_hw)) & SYSTEM_MD) != 0)) {
            if (((d & unchecked((uint)(4))) != 0)) {
            bitmap.viewport.changed |= 2;
            vc_max = new ArrayPointer<ushort>(vc_table, (int)(((d >> 2) & unchecked((uint)(3)))) * 2)[(int)(vdp_pal)];
            }
            }
            }
            if (((r & unchecked((uint)(4))) != 0)) {
            if (((unchecked((int)(system_hw)) & SYSTEM_MD) != 0)) {
            int i = default;
            int j = default;
            Span<byte> temp = stackalloc byte[(int)(1024)];
            if (((d & unchecked((uint)(4))) != 0)) {
            for (i = 0; (i < 16384); i += 1024) {
            memcpy(new ArrayPointer<byte>(temp), (new ArrayPointer<byte>(vram) + i), unchecked((uint)(1024)));
            for (j = 2; (j < 1022); j += 2) {
            (((new ArrayPointer<byte>(vram) + i) + ((j << 1) & 1020)) + ((j >> 8) & 2)).Cast<ushort>()[0] = (new ArrayPointer<byte>(temp) + j).Cast<ushort>()[0];
            }
            }
            update_bg_pattern_cache = update_bg_pattern_cache_m5;
            if ((im2_flag != 0)) {
            parse_satb = parse_satb_m5_im2;
            render_bg = (((unchecked((int)(reg[(int)(11)])) & 4) != 0) ? render_bg_m5_im2_vs : render_bg_m5_im2);
            render_obj = (((unchecked((int)(reg[(int)(12)])) & 8) != 0) ? render_obj_m5_im2_ste : render_obj_m5_im2);
            } else {
            parse_satb = parse_satb_m5;
            render_bg = (((unchecked((int)(reg[(int)(11)])) & 4) != 0) ? ((config.enhanced_vscroll != 0) ? render_bg_m5_vs_enhanced : render_bg_m5_vs) : render_bg_m5);
            render_obj = (((unchecked((int)(reg[(int)(12)])) & 8) != 0) ? render_obj_m5_ste : render_obj_m5);
            }
            color_update_m5(0, unchecked((uint)(new ArrayPointer<byte>(cram, (int)((unchecked((int)(border)) << 1))).Cast<ushort>()[0])));
            for (i = 1; (i < 64); i++) {
            color_update_m5(i, unchecked((uint)(new ArrayPointer<byte>(cram, (int)((i << 1))).Cast<ushort>()[0])));
            }
            vdp_68k_data_w = vdp_68k_data_w_m5;
            vdp_z80_data_w = vdp_z80_data_w_m5;
            vdp_68k_data_r = vdp_68k_data_r_m5;
            vdp_z80_data_r = vdp_z80_data_r_m5;
            hvc_latch = unchecked((uint)(0));
            if (((unchecked((int)(reg[(int)(0)])) & 2) != 0)) {
            hvc_latch = (vdp_hvc_r(cycles) | unchecked((uint)(65536)));
            }
            bg_list_index = unchecked((ushort)(2048));
            } else {
            for (i = 0; (i < 16384); i += 1024) {
            memcpy(new ArrayPointer<byte>(temp), (new ArrayPointer<byte>(vram) + i), unchecked((uint)(1024)));
            for (j = 2; (j < 1022); j += 2) {
            (((new ArrayPointer<byte>(vram) + i) + ((j >> 1) & 510)) + ((j << 8) & 512)).Cast<ushort>()[0] = (new ArrayPointer<byte>(temp) + j).Cast<ushort>()[0];
            }
            }
            parse_satb = parse_satb_m4;
            update_bg_pattern_cache = update_bg_pattern_cache_m4;
            render_bg = render_bg_m4;
            render_obj = render_obj_m4;
            for (i = 0; (i < 32); i++) {
            color_update_m4(i, unchecked((uint)(new ArrayPointer<byte>(cram, (int)((i << 1))).Cast<ushort>()[0])));
            }
            color_update_m4(64, unchecked((uint)(new ArrayPointer<byte>(cram, (int)(((16 | (unchecked((int)(border)) & 15)) << 1))).Cast<ushort>()[0])));
            vdp_68k_data_w = vdp_68k_data_w_m4;
            vdp_z80_data_w = vdp_z80_data_w_m4;
            vdp_68k_data_r = vdp_68k_data_r_m4;
            vdp_z80_data_r = vdp_z80_data_r_m4;
            hvc_latch = (vdp_hvc_r(cycles) | unchecked((uint)(65536)));
            bg_list_index = unchecked((ushort)(512));
            }
            for (i = 0; (i < unchecked((int)(bg_list_index))); i++) {
            bg_name_list[(int)(i)] = unchecked((ushort)(i));
            bg_name_dirty[(int)(i)] = unchecked((byte)(255));
            }
            vc_max = new ArrayPointer<ushort>(vc_table, (int)(((d >> 2) & unchecked((uint)(3)))) * 2)[(int)(vdp_pal)];
            bitmap.viewport.changed |= 2;
            } else {
            reg[(int)(1)] &= unchecked((byte)((~4)));
            }
            }
            break;
            }
            case 2:
            {
            reg[(int)(2)] = unchecked((byte)(d));
            ntab = unchecked((ushort)(((d << 10) & unchecked((uint)(57344)))));
            if ((((unchecked((int)(v_counter)) < bitmap.viewport.h) && ((unchecked((int)(reg[(int)(1)])) & 64) != 0)) && (cycles <= (mcycles_vdp + unchecked((uint)(860)))))) {
            render_line(unchecked((int)(v_counter)));
            }
            break;
            }
            case 3:
            {
            reg[(int)(3)] = unchecked((byte)(d));
            if (((unchecked((int)(reg[(int)(12)])) & 1) != 0)) {
            ntwb = unchecked((ushort)(((d << 10) & unchecked((uint)(61440)))));
            } else {
            ntwb = unchecked((ushort)(((d << 10) & unchecked((uint)(63488)))));
            }
            if ((((unchecked((int)(v_counter)) < bitmap.viewport.h) && ((unchecked((int)(reg[(int)(1)])) & 64) != 0)) && (cycles <= (mcycles_vdp + unchecked((uint)(860)))))) {
            render_line(unchecked((int)(v_counter)));
            }
            break;
            }
            case 4:
            {
            reg[(int)(4)] = unchecked((byte)(d));
            ntbb = unchecked((ushort)(((d << 13) & unchecked((uint)(57344)))));
            if ((((unchecked((int)(v_counter)) < bitmap.viewport.h) && ((unchecked((int)(reg[(int)(1)])) & 64) != 0)) && (cycles <= (mcycles_vdp + unchecked((uint)(860)))))) {
            render_line(unchecked((int)(v_counter)));
            }
            break;
            }
            case 5:
            {
            reg[(int)(5)] = unchecked((byte)(d));
            satb = unchecked((ushort)(((d << 9) & unchecked((uint)(sat_base_mask)))));
            if ((((unchecked((int)(v_counter)) < bitmap.viewport.h) && ((unchecked((int)(reg[(int)(1)])) & 64) != 0)) && (cycles <= (mcycles_vdp + unchecked((uint)(860)))))) {
            parse_satb(unchecked((int)(v_counter)));
            }
            break;
            }
            case 7:
            {
            reg[(int)(7)] = unchecked((byte)(d));
            d &= unchecked((uint)(63));
            if ((d != unchecked((uint)(border)))) {
            border = unchecked((byte)(d));
            if (((unchecked((int)(reg[(int)(1)])) & 4) != 0)) {
            color_update_m5(0, unchecked((uint)(new ArrayPointer<byte>(cram, (int)((d << 1))).Cast<ushort>()[0])));
            } else {
            color_update_m4(64, unchecked((uint)(new ArrayPointer<byte>(cram, (int)(((unchecked((uint)(16)) | (d & unchecked((uint)(15)))) << 1))).Cast<ushort>()[0])));
            }
            if (((unchecked((int)(v_counter)) < bitmap.viewport.h) && (cycles <= (mcycles_vdp + unchecked((uint)(860)))))) {
            remap_line(unchecked((int)(v_counter)));
            }
            }
            break;
            }
            case 8:
            {
            if ((unchecked((int)(system_hw)) < SYSTEM_MD)) {
            cycles = (cycles + unchecked((uint)(15)));
            }
            if (((cycles - mcycles_vdp) >= unchecked((uint)(MCYCLES_PER_LINE)))) {
            int line = ((unchecked((int)(v_counter)) + 1) % unchecked((int)(lines_per_frame)));
            if (((line < bitmap.viewport.h) && (!(((unchecked((uint)(work_ram[(int)(8187)])) & CartridgeSpecial) & unchecked((uint)(HW_3D_GLASSES))) != 0)))) {
            v_counter = unchecked((ushort)(line));
            render_line(line);
            }
            }
            reg[(int)(8)] = unchecked((byte)(d));
            break;
            }
            case 11:
            {
            reg[(int)(11)] = unchecked((byte)(d));
            hscroll_mask = hscroll_mask_table[(int)((d & unchecked((uint)(3))))];
            if (((d & unchecked((uint)(4))) != 0)) {
            render_bg = ((im2_flag != 0) ? render_bg_m5_im2_vs : ((config.enhanced_vscroll != 0) ? render_bg_m5_vs_enhanced : render_bg_m5_vs));
            } else {
            render_bg = ((im2_flag != 0) ? render_bg_m5_im2 : render_bg_m5);
            }
            break;
            }
            case 12:
            {
            r = (d ^ unchecked((uint)(reg[(int)(12)])));
            reg[(int)(12)] = unchecked((byte)(d));
            if (((r & unchecked((uint)(8))) != 0)) {
            int i = default;
            color_update_m5(0, unchecked((uint)(new ArrayPointer<byte>(cram, (int)((unchecked((int)(border)) << 1))).Cast<ushort>()[0])));
            for (i = 1; (i < 64); i++) {
            color_update_m5(i, unchecked((uint)(new ArrayPointer<byte>(cram, (int)((i << 1))).Cast<ushort>()[0])));
            }
            if (((d & unchecked((uint)(8))) != 0)) {
            render_obj = ((im2_flag != 0) ? render_obj_m5_im2_ste : render_obj_m5_ste);
            } else {
            render_obj = ((im2_flag != 0) ? render_obj_m5_im2 : render_obj_m5);
            }
            }
            if (((r & unchecked((uint)(6))) != 0)) {
            bitmap.viewport.changed |= 2;
            }
            if (((r & unchecked((uint)(1))) != 0)) {
            if (((d & unchecked((uint)(1))) != 0)) {
            ntwb = unchecked((ushort)(((unchecked((int)(reg[(int)(3)])) << 10) & 61440)));
            satb = unchecked((ushort)(((unchecked((int)(reg[(int)(5)])) << 9) & 64512)));
            sat_base_mask = unchecked((ushort)(64512));
            sat_addr_mask = unchecked((ushort)(1023));
            hctab = cycle2hc40;
            window_clip(unchecked((uint)(reg[(int)(17)])), unchecked((uint)(1)));
            max_sprite_pixels = unchecked((ushort)(320));
            fifo_timing = fifo_timing_h40;
            vint_cycle = unchecked((uint)(788));
            hblank_start_cycle = 228;
            hblank_end_cycle = 872;
            } else {
            ntwb = unchecked((ushort)(((unchecked((int)(reg[(int)(3)])) << 10) & 63488)));
            satb = unchecked((ushort)(((unchecked((int)(reg[(int)(5)])) << 9) & 65024)));
            sat_base_mask = unchecked((ushort)(65024));
            sat_addr_mask = unchecked((ushort)(511));
            hctab = cycle2hc32;
            window_clip(unchecked((uint)(reg[(int)(17)])), unchecked((uint)(0)));
            max_sprite_pixels = unchecked((ushort)(256));
            fifo_timing = fifo_timing_h32;
            vint_cycle = unchecked((uint)(770));
            hblank_start_cycle = 280;
            hblank_end_cycle = 860;
            }
            if ((unchecked((int)(v_counter)) >= bitmap.viewport.h)) {
            bitmap.viewport.w = unchecked((int)(max_sprite_pixels));
            } else if ((unchecked((int)(v_counter)) <= 2)) {
            int i = default;
            bitmap.viewport.w = unchecked((int)(max_sprite_pixels));
            for (i = 0; (i <= unchecked((int)(v_counter))); i++) {
            render_line(i);
            }
            } else {
            bitmap.viewport.changed |= 2;
            }
            }
            break;
            }
            case 13:
            {
            reg[(int)(13)] = unchecked((byte)(d));
            hscb = unchecked((ushort)(((d << 10) & unchecked((uint)(64512)))));
            break;
            }
            case 16:
            {
            reg[(int)(16)] = unchecked((byte)(d));
            playfield_shift = shift_table[(int)((d & unchecked((uint)(3))))];
            playfield_col_mask = col_mask_table[(int)((d & unchecked((uint)(3))))];
            playfield_row_mask = row_mask_table[(int)(((d >> 4) & unchecked((uint)(3))))];
            break;
            }
            case 17:
            {
            reg[(int)(17)] = unchecked((byte)(d));
            window_clip(d, unchecked((uint)((unchecked((int)(reg[(int)(12)])) & 1))));
            break;
            }
            default:
            {
            reg[(int)(r)] = unchecked((byte)(d));
            break;
            }
            }
        }
    }
    private void vdp_bus_w(uint data)
    {
        unchecked
        {
            fifo[(int)(fifo_idx)] = unchecked((ushort)(data));
            fifo_idx = ((fifo_idx + 1) & 3);
            switch ((unchecked((int)(code)) & 15)) {
            case 1:
            {
            int index = (unchecked((int)(addr)) & 65534);
            ArrayPointer<ushort> p = new ArrayPointer<byte>(vram, (int)(index)).Cast<ushort>();
            if (((unchecked((int)(addr)) & 1) != 0)) {
            data = (((data >> 8) | (data << 8)) & unchecked((uint)(65535)));
            }
            if (((index & unchecked((int)(sat_base_mask))) == unchecked((int)(satb)))) {
            new ArrayPointer<byte>(sat, (int)((index & unchecked((int)(sat_addr_mask))))).Cast<ushort>()[0] = unchecked((ushort)(data));
            }
            if ((data != unchecked((uint)(p[0])))) {
            int name = default;
            p[0] = unchecked((ushort)(data));
            {
            name = ((index >> 5) & 2047);
            if ((unchecked((int)(bg_name_dirty[(int)(name)])) == 0)) {
            bg_name_list[(int)((bg_list_index++))] = unchecked((ushort)(name));
            }
            bg_name_dirty[(int)(name)] |= unchecked((byte)((1 << ((index >> 2) & 7))));
            }
            ;
            }
            break;
            }
            case 3:
            {
            ArrayPointer<ushort> p = new ArrayPointer<byte>(cram, (int)((unchecked((int)(addr)) & 126))).Cast<ushort>();
            data = ((((data & unchecked((uint)(3584))) >> 3) | ((data & unchecked((uint)(224))) >> 2)) | ((data & unchecked((uint)(14))) >> 1));
            if ((data != unchecked((uint)(p[0])))) {
            int index = ((unchecked((int)(addr)) >> 1) & 63);
            p[0] = unchecked((ushort)(data));
            if (((index & 15) != 0)) {
            color_update_m5(index, data);
            }
            if ((index == unchecked((int)(border)))) {
            color_update_m5(0, data);
            }
            if ((((unchecked((int)(v_counter)) < bitmap.viewport.h) && (unchecked((uint)(MainCpuCycles)) <= (mcycles_vdp + unchecked((uint)(860))))) && (((unchecked((int)(reg[(int)(1)])) & 64) != 0) || (index == unchecked((int)(border)))))) {
            remap_line(unchecked((int)(v_counter)));
            }
            }
            break;
            }
            case 5:
            {
            new ArrayPointer<byte>(vsram, (int)((unchecked((int)(addr)) & 126))).Cast<ushort>()[0] = unchecked((ushort)(data));
            if (((unchecked((int)(reg[(int)(11)])) & 4) != 0)) {
            if ((((unchecked((int)(v_counter)) < bitmap.viewport.h) && ((unchecked((int)(reg[(int)(1)])) & 64) != 0)) && (unchecked((uint)(MainCpuCycles)) <= (mcycles_vdp + unchecked((uint)(860)))))) {
            render_line(unchecked((int)(v_counter)));
            }
            }
            break;
            }
            default:
            {
            break;
            }
            }
            addr += unchecked((ushort)(reg[(int)(15)]));
        }
    }
    private void vdp_68k_data_w_m4(uint data)
    {
        unchecked
        {
            pending = unchecked((byte)(0));
            if (((!((unchecked((int)(status)) & 8) != 0)) && ((unchecked((int)(reg[(int)(1)])) & 64) != 0))) {
            int slot = 0;
            int cycles = MainCpuCycles;
            if ((unchecked((uint)(cycles)) < fifo_cycles[(int)(((fifo_idx + 3) & 3))])) {
            if ((unchecked((uint)(cycles)) < fifo_cycles[(int)(fifo_idx)])) {
            MainCpuCycles = unchecked((int)((((fifo_cycles[(int)(fifo_idx)] + unchecked((uint)(6))) / unchecked((uint)(7))) * unchecked((uint)(7)))));
            }
            cycles = unchecked((int)(fifo_cycles[(int)(((fifo_idx + 3) & 3))]));
            }
            cycles -= unchecked((int)(mcycles_vdp));
            while ((cycles >= fifo_timing[(int)(slot)])) slot++;
            fifo_cycles[(int)(fifo_idx)] = (mcycles_vdp + unchecked((uint)(fifo_timing[(int)((slot + fifo_byte_access))])));
            }
            if (((unchecked((int)(code)) & 2) != 0)) {
            int index = (unchecked((int)(addr)) & 31);
            ArrayPointer<ushort> p = new ArrayPointer<byte>(cram, (int)((index << 1))).Cast<ushort>();
            data = (((data & unchecked((uint)(3584))) >> 3) | (data & unchecked((uint)(63))));
            if ((data != unchecked((uint)(p[0])))) {
            p[0] = unchecked((ushort)(data));
            color_update_m4(index, data);
            if ((index == (16 | (unchecked((int)(border)) & 15)))) {
            color_update_m4(64, data);
            }
            }
            } else {
            int index = (unchecked((int)(addr)) & 16382);
            ArrayPointer<ushort> p = new ArrayPointer<byte>(vram, (int)(index)).Cast<ushort>();
            if (((unchecked((int)(addr)) & 1) != 0)) {
            data = (((data >> 8) | (data << 8)) & unchecked((uint)(65535)));
            }
            if ((data != unchecked((uint)(p[0])))) {
            int name = default;
            p[0] = unchecked((ushort)(data));
            {
            name = ((index >> 5) & 2047);
            if ((unchecked((int)(bg_name_dirty[(int)(name)])) == 0)) {
            bg_name_list[(int)((bg_list_index++))] = unchecked((ushort)(name));
            }
            bg_name_dirty[(int)(name)] |= unchecked((byte)((1 << ((index >> 2) & 7))));
            }
            ;
            }
            }
            addr += unchecked((ushort)((unchecked((int)(reg[(int)(15)])) + 1)));
        }
    }
    private void vdp_68k_data_w_m5(uint data)
    {
        unchecked
        {
            pending = unchecked((byte)(0));
            if (((!((unchecked((int)(status)) & 8) != 0)) && ((unchecked((int)(reg[(int)(1)])) & 64) != 0))) {
            int slot = 0;
            int cycles = MainCpuCycles;
            if ((unchecked((uint)(cycles)) < fifo_cycles[(int)(((fifo_idx + 3) & 3))])) {
            if ((unchecked((uint)(cycles)) < fifo_cycles[(int)(fifo_idx)])) {
            MainCpuCycles = unchecked((int)((((fifo_cycles[(int)(fifo_idx)] + unchecked((uint)(6))) / unchecked((uint)(7))) * unchecked((uint)(7)))));
            }
            cycles = unchecked((int)(fifo_cycles[(int)(((fifo_idx + 3) & 3))]));
            }
            cycles -= unchecked((int)(mcycles_vdp));
            while ((cycles >= fifo_timing[(int)(slot)])) slot++;
            fifo_cycles[(int)(fifo_idx)] = (mcycles_vdp + unchecked((uint)(fifo_timing[(int)((slot + fifo_byte_access))])));
            }
            vdp_bus_w(data);
            if ((dmafill != 0)) {
            dma_length = unchecked((uint)(((unchecked((int)(reg[(int)(20)])) << 8) | unchecked((int)(reg[(int)(19)])))));
            if ((!(dma_length != 0))) {
            dma_length = unchecked((uint)(65536));
            }
            vdp_dma_update(unchecked((uint)(MainCpuCycles)));
            dmafill = 0;
            }
        }
    }
    private uint vdp_68k_data_r_m4()
    {
        unchecked
        {
            int index = (unchecked((int)(addr)) & 16382);
            pending = unchecked((byte)(0));
            addr += unchecked((ushort)((unchecked((int)(reg[(int)(15)])) + 1)));
            return unchecked((uint)(new ArrayPointer<byte>(vram, (int)(index)).Cast<ushort>()[0]));
        }
    }
    private uint vdp_68k_data_r_m5()
    {
        unchecked
        {
            ushort data = unchecked((ushort)(0));
            pending = unchecked((byte)(0));
            switch ((unchecked((int)(code)) & 31)) {
            case 0:
            {
            data = new ArrayPointer<byte>(vram, (int)((unchecked((int)(addr)) & 65534))).Cast<ushort>()[0];
            break;
            }
            case 4:
            {
            int index = (unchecked((int)(addr)) & 126);
            if ((index >= 80)) {
            index = 0;
            }
            data = unchecked((ushort)((unchecked((int)(new ArrayPointer<byte>(vsram, (int)(index)).Cast<ushort>()[0])) & 2047)));
            data |= unchecked((ushort)((unchecked((int)(fifo[(int)(fifo_idx)])) & (~2047))));
            break;
            }
            case 8:
            {
            data = new ArrayPointer<byte>(cram, (int)((unchecked((int)(addr)) & 126))).Cast<ushort>()[0];
            data = unchecked((ushort)(((((unchecked((int)(data)) & 448) << 3) | ((unchecked((int)(data)) & 56) << 2)) | ((unchecked((int)(data)) & 7) << 1))));
            data |= unchecked((ushort)((unchecked((int)(fifo[(int)(fifo_idx)])) & (~3822))));
            break;
            }
            case 12:
            {
            data = unchecked((ushort)(vram[(int)(((unchecked((int)(addr)) ^ 1) ^ 1))]));
            data |= unchecked((ushort)((unchecked((int)(fifo[(int)(fifo_idx)])) & (~255))));
            break;
            }
            default:
            {
            break;
            }
            }
            addr += unchecked((ushort)(reg[(int)(15)]));
            return unchecked((uint)(data));
        }
    }
    private void vdp_z80_data_w_m4(uint data)
    {
        unchecked
        {
            pending = unchecked((byte)(0));
            if (((unchecked((int)(code)) & 2) != 0)) {
            int index = (unchecked((int)(addr)) & 31);
            ArrayPointer<ushort> p = new ArrayPointer<byte>(cram, (int)((index << 1))).Cast<ushort>();
            if ((data != unchecked((uint)(p[0])))) {
            p[0] = unchecked((ushort)(data));
            color_update_m4(index, data);
            if ((index == (16 | (unchecked((int)(border)) & 15)))) {
            color_update_m4(64, data);
            }
            }
            } else {
            int index = (unchecked((int)(addr)) & 16383);
            if ((data != unchecked((uint)(vram[(int)(index)])))) {
            int name = default;
            vram[(int)(index)] = unchecked((byte)(data));
            {
            name = ((index >> 5) & 2047);
            if ((unchecked((int)(bg_name_dirty[(int)(name)])) == 0)) {
            bg_name_list[(int)((bg_list_index++))] = unchecked((ushort)(name));
            }
            bg_name_dirty[(int)(name)] |= unchecked((byte)((1 << ((index >> 2) & 7))));
            }
            ;
            }
            }
            addr += unchecked((ushort)((unchecked((int)(reg[(int)(15)])) + 1)));
        }
    }
    private void vdp_z80_data_w_m5(uint data)
    {
        unchecked
        {
            pending = unchecked((byte)(0));
            fifo[(int)(fifo_idx)] = unchecked((ushort)((data << 8)));
            fifo_idx = ((fifo_idx + 1) & 3);
            switch ((unchecked((int)(code)) & 15)) {
            case 1:
            {
            int index = (unchecked((int)(addr)) ^ 1);
            if (((index & unchecked((int)(sat_base_mask))) == unchecked((int)(satb)))) {
            sat[(int)(((index & unchecked((int)(sat_addr_mask))) ^ 1))] = unchecked((byte)((data & unchecked((uint)(255)))));
            }
            if ((data != unchecked((uint)(vram[(int)((index ^ 1))])))) {
            int name = default;
            vram[(int)((index ^ 1))] = unchecked((byte)((data & unchecked((uint)(255)))));
            {
            name = ((index >> 5) & 2047);
            if ((unchecked((int)(bg_name_dirty[(int)(name)])) == 0)) {
            bg_name_list[(int)((bg_list_index++))] = unchecked((ushort)(name));
            }
            bg_name_dirty[(int)(name)] |= unchecked((byte)((1 << ((index >> 2) & 7))));
            }
            ;
            }
            break;
            }
            case 3:
            {
            ArrayPointer<ushort> p = new ArrayPointer<byte>(cram, (int)((unchecked((int)(addr)) & 126))).Cast<ushort>();
            if (((unchecked((int)(addr)) & 1) != 0)) {
            data = (unchecked((uint)((unchecked((int)(p[0])) & 63))) | ((data & unchecked((uint)(14))) << 5));
            } else {
            data = ((unchecked((uint)((unchecked((int)(p[0])) & 448))) | ((data & unchecked((uint)(14))) >> 1)) | ((data & unchecked((uint)(224))) >> 2));
            }
            if ((data != unchecked((uint)(p[0])))) {
            int index = ((unchecked((int)(addr)) >> 1) & 63);
            p[0] = unchecked((ushort)(data));
            if (((index & 15) != 0)) {
            color_update_m5(index, data);
            }
            if ((index == unchecked((int)(border)))) {
            color_update_m5(0, data);
            }
            }
            break;
            }
            case 5:
            {
            vsram[(int)((((unchecked((int)(addr)) & 127) ^ 1) ^ 1))] = unchecked((byte)((data & unchecked((uint)(255)))));
            break;
            }
            }
            addr += unchecked((ushort)(reg[(int)(15)]));
            if ((dmafill != 0)) {
            dmafill = 0;
            dma_length = unchecked((uint)(((unchecked((int)(reg[(int)(20)])) << 8) | unchecked((int)(reg[(int)(19)])))));
            if ((!(dma_length != 0))) {
            dma_length = unchecked((uint)(65536));
            }
            vdp_dma_update(Z80CpuCycles);
            }
        }
    }
    private uint vdp_z80_data_r_m4()
    {
        unchecked
        {
            uint data = unchecked((uint)(fifo[(int)(0)]));
            pending = unchecked((byte)(0));
            fifo[(int)(0)] = unchecked((ushort)(vram[(int)((unchecked((int)(addr)) & 16383))]));
            addr += unchecked((ushort)((unchecked((int)(reg[(int)(15)])) + 1)));
            return data;
        }
    }
    private uint vdp_z80_data_r_m5()
    {
        unchecked
        {
            uint data = unchecked((uint)(0));
            pending = unchecked((byte)(0));
            switch ((unchecked((int)(code)) & 31)) {
            case 0:
            {
            data = unchecked((uint)(vram[(int)(((unchecked((int)(addr)) ^ 1) ^ 1))]));
            break;
            }
            case 4:
            {
            data = unchecked((uint)(vsram[(int)((((unchecked((int)(addr)) & 127) ^ 1) ^ 1))]));
            break;
            }
            case 8:
            {
            data = unchecked((uint)(new ArrayPointer<byte>(cram, (int)((unchecked((int)(addr)) & 126))).Cast<ushort>()[0]));
            data = ((((data & unchecked((uint)(448))) << 3) | ((data & unchecked((uint)(56))) << 2)) | ((data & unchecked((uint)(7))) << 1));
            if (((unchecked((int)(addr)) & 1) != 0)) {
            data = (data >> 8);
            }
            data &= unchecked((uint)(255));
            break;
            }
            }
            addr += unchecked((ushort)(reg[(int)(15)]));
            return data;
        }
    }
    private void vdp_z80_data_w_ms(uint data)
    {
        unchecked
        {
            pending = unchecked((byte)(0));
            if ((unchecked((int)(code)) < 3)) {
            int index = default;
            if (((Z80CpuCycles - mcycles_vdp) >= unchecked((uint)(MCYCLES_PER_LINE)))) {
            int line = ((unchecked((int)(v_counter)) + 1) % unchecked((int)(lines_per_frame)));
            if (((line < bitmap.viewport.h) && (!(((unchecked((uint)(work_ram[(int)(8187)])) & CartridgeSpecial) & unchecked((uint)(HW_3D_GLASSES))) != 0)))) {
            v_counter = unchecked((ushort)(line));
            render_line(line);
            }
            }
            index = (unchecked((int)(addr)) & 16383);
            if ((data != unchecked((uint)(vram[(int)(index)])))) {
            int name = default;
            vram[(int)(index)] = unchecked((byte)(data));
            {
            name = ((index >> 5) & 2047);
            if ((unchecked((int)(bg_name_dirty[(int)(name)])) == 0)) {
            bg_name_list[(int)((bg_list_index++))] = unchecked((ushort)(name));
            }
            bg_name_dirty[(int)(name)] |= unchecked((byte)((1 << ((index >> 2) & 7))));
            }
            ;
            }
            } else {
            int index = (unchecked((int)(addr)) & 31);
            ArrayPointer<ushort> p = new ArrayPointer<byte>(cram, (int)((index << 1))).Cast<ushort>();
            if ((data != unchecked((uint)(p[0])))) {
            p[0] = unchecked((ushort)(data));
            color_update_m4(index, data);
            if ((index == (16 | (unchecked((int)(border)) & 15)))) {
            color_update_m4(64, data);
            }
            }
            }
            fifo[(int)(0)] = unchecked((ushort)(data));
            addr++;
        }
    }
    private void vdp_z80_data_w_gg(uint data)
    {
        unchecked
        {
            pending = unchecked((byte)(0));
            if ((unchecked((int)(code)) < 3)) {
            int index = default;
            if (((Z80CpuCycles - mcycles_vdp) >= unchecked((uint)(MCYCLES_PER_LINE)))) {
            int line = ((unchecked((int)(v_counter)) + 1) % unchecked((int)(lines_per_frame)));
            if (((line < bitmap.viewport.h) && (!(((unchecked((uint)(work_ram[(int)(8187)])) & CartridgeSpecial) & unchecked((uint)(HW_3D_GLASSES))) != 0)))) {
            v_counter = unchecked((ushort)(line));
            render_line(line);
            }
            }
            index = (unchecked((int)(addr)) & 16383);
            if ((data != unchecked((uint)(vram[(int)(index)])))) {
            int name = default;
            vram[(int)(index)] = unchecked((byte)(data));
            {
            name = ((index >> 5) & 2047);
            if ((unchecked((int)(bg_name_dirty[(int)(name)])) == 0)) {
            bg_name_list[(int)((bg_list_index++))] = unchecked((ushort)(name));
            }
            bg_name_dirty[(int)(name)] |= unchecked((byte)((1 << ((index >> 2) & 7))));
            }
            ;
            }
            } else {
            if (((unchecked((int)(addr)) & 1) != 0)) {
            ArrayPointer<ushort> p = new ArrayPointer<byte>(cram, (int)((unchecked((int)(addr)) & 62))).Cast<ushort>();
            data = ((data << 8) | unchecked((uint)(cached_write)));
            if ((data != unchecked((uint)(p[0])))) {
            int index = ((unchecked((int)(addr)) >> 1) & 31);
            p[0] = unchecked((ushort)(data));
            color_update_m4(index, data);
            if ((index == (16 | (unchecked((int)(border)) & 15)))) {
            color_update_m4(64, data);
            }
            }
            } else {
            cached_write = unchecked((int)(data));
            }
            }
            fifo[(int)(0)] = unchecked((ushort)(data));
            addr++;
        }
    }
    private void vdp_z80_data_w_sg(uint data)
    {
        unchecked
        {
            int index = (unchecked((int)(addr)) & 16383);
            pending = unchecked((byte)(0));
            vram[(int)(index)] = unchecked((byte)(data));
            addr++;
        }
    }
    private void vdp_dma_68k_ram(uint length)
    {
        unchecked
        {
            ushort data = default;
            uint source = unchecked((uint)(((unchecked((int)(reg[(int)(23)])) << 17) | (unchecked((int)(dma_src)) << 1))));
            do {
            data = (new ArrayPointer<byte>(work_ram) + unchecked((int)((source & unchecked((uint)(65535)))))).Cast<ushort>()[0];
            source += unchecked((uint)(2));
            source = (unchecked((uint)((unchecked((int)(reg[(int)(23)])) << 17))) | (source & unchecked((uint)(131071))));
            vdp_bus_w(unchecked((uint)(data)));
            } while (((--length) != 0));
            dma_src = unchecked((ushort)(((source >> 1) & unchecked((uint)(65535)))));
        }
    }
    private void vdp_dma_68k_io(uint length)
    {
        unchecked
        {
            ushort data = default;
            uint source = unchecked((uint)(((unchecked((int)(reg[(int)(23)])) << 17) | (unchecked((int)(dma_src)) << 1))));
            do {
            if ((source <= unchecked((uint)(10551295)))) {
            data = unchecked((ushort)((((unchecked((int)(zstate)) ^ 3) != 0) ? unchecked((int)((new ArrayPointer<byte>(work_ram) + unchecked((int)((source & unchecked((uint)(65535)))))).Cast<ushort>()[0])) : 65535)));
            } else if ((source <= unchecked((uint)(10551327)))) {
            data = unchecked((ushort)(io_68k_read(((source >> 1) & unchecked((uint)(15))))));
            data = unchecked((ushort)(((unchecked((int)(data)) << 8) | unchecked((int)(data)))));
            } else {
            data = (new ArrayPointer<byte>(work_ram) + unchecked((int)((source & unchecked((uint)(65535)))))).Cast<ushort>()[0];
            }
            source += unchecked((uint)(2));
            source = (unchecked((uint)((unchecked((int)(reg[(int)(23)])) << 17))) | (source & unchecked((uint)(131071))));
            vdp_bus_w(unchecked((uint)(data)));
            } while (((--length) != 0));
            dma_src = unchecked((ushort)(((source >> 1) & unchecked((uint)(65535)))));
        }
    }
    private void vdp_dma_copy(uint length)
    {
        unchecked
        {
            if (((unchecked((int)(code)) & 16) != 0)) {
            int name = default;
            byte data = default;
            ushort source = dma_src;
            do {
            data = vram[(int)(((unchecked((int)(source)) ^ 1) ^ 1))];
            if (((unchecked((int)(addr)) & unchecked((int)(sat_base_mask))) == unchecked((int)(satb)))) {
            sat[(int)((((unchecked((int)(addr)) & unchecked((int)(sat_addr_mask))) ^ 1) ^ 1))] = unchecked((byte)((unchecked((int)(data)) & 255)));
            }
            vram[(int)(((unchecked((int)(addr)) ^ 1) ^ 1))] = unchecked((byte)((unchecked((int)(data)) & 255)));
            {
            name = ((unchecked((int)(addr)) >> 5) & 2047);
            if ((unchecked((int)(bg_name_dirty[(int)(name)])) == 0)) {
            bg_name_list[(int)((bg_list_index++))] = unchecked((ushort)(name));
            }
            bg_name_dirty[(int)(name)] |= unchecked((byte)((1 << ((unchecked((int)(addr)) >> 2) & 7))));
            }
            ;
            source++;
            addr += unchecked((ushort)(reg[(int)(15)]));
            } while (((--length) != 0));
            dma_src = source;
            }
        }
    }
    private void vdp_dma_fill(uint length)
    {
        unchecked
        {
            switch ((unchecked((int)(code)) & 15)) {
            case 1:
            {
            int name = default;
            byte data = unchecked((byte)((unchecked((int)(fifo[(int)(((fifo_idx + 3) & 3))])) >> 8)));
            do {
            if (((unchecked((int)(addr)) & unchecked((int)(sat_base_mask))) == unchecked((int)(satb)))) {
            sat[(int)((((unchecked((int)(addr)) & unchecked((int)(sat_addr_mask))) ^ 1) ^ 1))] = unchecked((byte)((unchecked((int)(data)) & 255)));
            }
            vram[(int)(((unchecked((int)(addr)) ^ 1) ^ 1))] = unchecked((byte)((unchecked((int)(data)) & 255)));
            {
            name = ((unchecked((int)(addr)) >> 5) & 2047);
            if ((unchecked((int)(bg_name_dirty[(int)(name)])) == 0)) {
            bg_name_list[(int)((bg_list_index++))] = unchecked((ushort)(name));
            }
            bg_name_dirty[(int)(name)] |= unchecked((byte)((1 << ((unchecked((int)(addr)) >> 2) & 7))));
            }
            ;
            addr += unchecked((ushort)(reg[(int)(15)]));
            } while (((--length) != 0));
            break;
            }
            case 3:
            {
            ushort data = fifo[(int)(fifo_idx)];
            data = unchecked((ushort)(((((unchecked((int)(data)) & 3584) >> 3) | ((unchecked((int)(data)) & 224) >> 2)) | ((unchecked((int)(data)) & 14) >> 1))));
            do {
            ArrayPointer<ushort> p = new ArrayPointer<byte>(cram, (int)((unchecked((int)(addr)) & 126))).Cast<ushort>();
            if ((unchecked((int)(data)) != unchecked((int)(p[0])))) {
            int index = ((unchecked((int)(addr)) >> 1) & 63);
            p[0] = data;
            if (((index & 15) != 0)) {
            color_update_m5(index, unchecked((uint)(data)));
            }
            if ((index == unchecked((int)(border)))) {
            color_update_m5(0, unchecked((uint)(data)));
            }
            }
            addr += unchecked((ushort)(reg[(int)(15)]));
            } while (((--length) != 0));
            break;
            }
            case 5:
            {
            ushort data = fifo[(int)(fifo_idx)];
            do {
            new ArrayPointer<byte>(vsram, (int)((unchecked((int)(addr)) & 126))).Cast<ushort>()[0] = data;
            addr += unchecked((ushort)(reg[(int)(15)]));
            } while (((--length) != 0));
            break;
            }
            default:
            {
            addr += unchecked((ushort)((unchecked((uint)(reg[(int)(15)])) * length)));
            break;
            }
            }
        }
    }
}
