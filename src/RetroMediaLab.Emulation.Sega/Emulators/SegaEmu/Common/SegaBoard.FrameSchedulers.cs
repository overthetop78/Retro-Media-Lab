using System.IO;
using RetroMediaLab.Chips.Common.Memory;
using RetroMediaLab.Chips.CPU.Motorola.M68000.Execution;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Instruction;
using RetroMediaLab.Chips.Controllers.Audio.YX5200;
using RetroMediaLab.Chips.Controllers.Sega.Input;
using RetroMediaLab.Chips.Video.Sega.VDP;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Cartridge;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Common.Audio;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.MegaCd.Hardware;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Media;
using BlipBuffer = RetroMediaLab.Chips.Audio.Common.Resampling.BandLimitedBuffer;
using Cdd = RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Media.CdDrive;
using Pcm = RetroMediaLab.Chips.Audio.Ricoh.RF5C164.Rf5c164;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Common;

internal abstract partial class SegaBoard
{
    protected void ExecuteMegaDriveFrame(int do_skip)
    {
        unchecked
        {
            int start = default;
            int end = default;
            int line = default;
            mcycles_vdp = unchecked((uint)(0));
            fifo_cycles[(int)(0)] = unchecked((uint)(0));
            fifo_cycles[(int)(1)] = unchecked((uint)(0));
            fifo_cycles[(int)(2)] = unchecked((uint)(0));
            fifo_cycles[(int)(3)] = unchecked((uint)(0));
            if (((viewport_changed & 2) != 0)) {
            int old_interlaced = unchecked((int)(interlaced));
            interlaced = unchecked((byte)(((unchecked((int)(reg[(int)(12)])) & 2) >> 1)));
            if ((old_interlaced != unchecked((int)(interlaced)))) {
            im2_flag = (((unchecked((int)(reg[(int)(12)])) & 6) == 6) ? (byte)1 : (byte)0);
            odd_frame = interlaced;
            viewport_changed = 5;
            if (((unchecked((int)(reg[(int)(1)])) & 4) != 0)) {
            SelectMode5Renderers();
            }
            } else {
            viewport_changed &= (~2);
            }
            if (((unchecked((int)(reg[(int)(1)])) & 4) != 0)) {
            if (((unchecked((int)(reg[(int)(1)])) & 8) != 0)) {
            viewport_h = 240;
            viewport_y = (((overscan & 1) * 24) * unchecked((int)(vdp_pal)));
            } else {
            viewport_h = 224;
            viewport_y = ((overscan & 1) * (8 + (24 * unchecked((int)(vdp_pal)))));
            }
            } else {
            viewport_h = 192;
            viewport_y = (((overscan & 1) * 24) * (unchecked((int)(vdp_pal)) + 1));
            }
            viewport_w = (256 + ((unchecked((int)(reg[(int)(12)])) & 1) << 6));
            if ((viewport_h != viewport_oh)) {
            viewport_oh = viewport_h;
            viewport_changed |= 1;
            }
            }
            if ((viewport_y != 0)) {
            blank_line(viewport_h, (-viewport_x), (viewport_w + (2 * viewport_x)));
            }
            status &= unchecked((ushort)(65261));
            status |= unchecked((ushort)(8));
            if ((interlaced != 0)) {
            odd_frame ^= unchecked((byte)(1));
            status |= unchecked((ushort)((unchecked((int)(odd_frame)) << 4)));
            }
            if ((dma_length != 0)) {
            vdp_dma_update(unchecked((uint)(0)));
            }
            input_refresh();
            if ((unchecked((int)(h_counter)) == 0)) {
            hint_pending = unchecked((byte)(16));
            if (((unchecked((int)(reg[(int)(0)])) & 16) != 0)) {
            m68k_update_irq(unchecked((uint)(4)));
            }
            }
            osd_input_update();
            if ((unchecked((int)(v_counter)) != viewport_h)) {
            v_counter = unchecked((ushort)(viewport_h));
            m68k_run(vint_cycle);
            if ((unchecked((int)(zstate)) == 1)) {
            z80_run(vint_cycle);
            }
            status |= unchecked((ushort)(128));
            vint_pending = unchecked((byte)(32));
            if (((unchecked((int)(reg[(int)(1)])) & 32) != 0)) {
            m68k_set_irq(unchecked((uint)(6)));
            }
            z80.irq_state = 1;
            }
            m68k_run(unchecked((uint)(3420)));
            if ((unchecked((int)(zstate)) == 1)) {
            z80_run(unchecked((uint)(3420)));
            }
            z80.irq_state = 0;
            if ((HasSvp != 0)) {
            ssp1601_run(unchecked((int)(SVP_cycles)));
            }
            mcycles_vdp = unchecked((uint)(3420));
            line = (viewport_h + 1);
            start = (unchecked((int)(lines_per_frame)) - viewport_y);
            end = (viewport_h + viewport_y);
            do {
            v_counter = unchecked((ushort)(line));
            if (((line < end) || (line >= start))) {
            blank_line(line, (-viewport_x), (viewport_w + (2 * viewport_x)));
            }
            input_refresh();
            m68k_run((mcycles_vdp + unchecked((uint)(3420))));
            if ((unchecked((int)(zstate)) == 1)) {
            z80_run((mcycles_vdp + unchecked((uint)(3420))));
            }
            if ((HasSvp != 0)) {
            ssp1601_run(unchecked((int)(SVP_cycles)));
            }
            mcycles_vdp += unchecked((uint)(3420));
            } while (((++line) < (unchecked((int)(lines_per_frame)) - 1)));
            v_counter = unchecked((ushort)(line));
            if ((viewport_y != 0)) {
            blank_line(line, (-viewport_x), (viewport_w + (2 * viewport_x)));
            }
            h_counter = reg[(int)(10)];
            status &= unchecked((ushort)((~8)));
            if ((dma_length != 0)) {
            vdp_dma_update(mcycles_vdp);
            }
            if (((unchecked((int)(reg[(int)(1)])) & 64) != 0)) {
            parse_satb((-1));
            }
            input_refresh();
            m68k_run((mcycles_vdp + unchecked((uint)(3420))));
            if ((unchecked((int)(zstate)) == 1)) {
            z80_run((mcycles_vdp + unchecked((uint)(3420))));
            }
            if ((HasSvp != 0)) {
            ssp1601_run(unchecked((int)(SVP_cycles)));
            }
            mcycles_vdp += unchecked((uint)(3420));
            line = 0;
            do {
            v_counter = unchecked((ushort)(line));
            if ((dma_length != 0)) {
            vdp_dma_update(mcycles_vdp);
            }
            if ((!(do_skip != 0))) {
            render_line(line);
            }
            input_refresh();
            if ((unchecked((int)(h_counter)) == 0)) {
            h_counter = reg[(int)(10)];
            hint_pending = unchecked((byte)(16));
            if (((unchecked((int)(reg[(int)(0)])) & 16) != 0)) {
            m68k_update_irq(unchecked((uint)(4)));
            }
            } else {
            h_counter--;
            }
            m68k_run((mcycles_vdp + unchecked((uint)(3420))));
            if ((unchecked((int)(zstate)) == 1)) {
            z80_run((mcycles_vdp + unchecked((uint)(3420))));
            }
            if ((HasSvp != 0)) {
            ssp1601_run(unchecked((int)(SVP_cycles)));
            }
            mcycles_vdp += unchecked((uint)(3420));
            } while (((++line) < viewport_h));
            if ((viewport_w != viewport_ow)) {
            viewport_ow = viewport_w;
            viewport_changed |= 1;
            }
            input_end_frame(mcycles_vdp);
            main.refresh_cycles -= unchecked((int)(mcycles_vdp));
            main.cycles -= unchecked((int)(mcycles_vdp));
            z80.cycles -= mcycles_vdp;
            dma_endCycles = unchecked((uint)(0));
        }
    }
    protected void ExecuteMegaCdFrame(int do_skip)
    {
        unchecked
        {
            int start = default;
            int end = default;
            int line = default;
            mcycles_vdp = unchecked((uint)(0));
            cd_cycles = unchecked((uint)(0));
            fifo_cycles[(int)(0)] = unchecked((uint)(0));
            fifo_cycles[(int)(1)] = unchecked((uint)(0));
            fifo_cycles[(int)(2)] = unchecked((uint)(0));
            fifo_cycles[(int)(3)] = unchecked((uint)(0));
            if (((viewport_changed & 2) != 0)) {
            int old_interlaced = unchecked((int)(interlaced));
            interlaced = unchecked((byte)(((unchecked((int)(reg[(int)(12)])) & 2) >> 1)));
            if ((old_interlaced != unchecked((int)(interlaced)))) {
            im2_flag = (((unchecked((int)(reg[(int)(12)])) & 6) == 6) ? (byte)1 : (byte)0);
            odd_frame = interlaced;
            viewport_changed = 5;
            if (((unchecked((int)(reg[(int)(1)])) & 4) != 0)) {
            SelectMode5Renderers();
            }
            } else {
            viewport_changed &= (~2);
            }
            if (((unchecked((int)(reg[(int)(1)])) & 4) != 0)) {
            if (((unchecked((int)(reg[(int)(1)])) & 8) != 0)) {
            viewport_h = 240;
            viewport_y = (((overscan & 1) * 24) * unchecked((int)(vdp_pal)));
            } else {
            viewport_h = 224;
            viewport_y = ((overscan & 1) * (8 + (24 * unchecked((int)(vdp_pal)))));
            }
            } else {
            viewport_h = 192;
            viewport_y = (((overscan & 1) * 24) * (unchecked((int)(vdp_pal)) + 1));
            }
            viewport_w = (256 + ((unchecked((int)(reg[(int)(12)])) & 1) << 6));
            if ((viewport_h != viewport_oh)) {
            viewport_oh = viewport_h;
            viewport_changed |= 1;
            }
            }
            if ((viewport_y != 0)) {
            blank_line(viewport_h, (-viewport_x), (viewport_w + (2 * viewport_x)));
            }
            status &= unchecked((ushort)(65261));
            status |= unchecked((ushort)(8));
            if ((interlaced != 0)) {
            odd_frame ^= unchecked((byte)(1));
            status |= unchecked((ushort)((unchecked((int)(odd_frame)) << 4)));
            }
            if ((dma_length != 0)) {
            vdp_dma_update(unchecked((uint)(0)));
            }
            input_refresh();
            if ((unchecked((int)(h_counter)) == 0)) {
            hint_pending = unchecked((byte)(16));
            if (((unchecked((int)(reg[(int)(0)])) & 16) != 0)) {
            m68k_update_irq(unchecked((uint)(4)));
            }
            }
            osd_input_update();
            if ((unchecked((int)(v_counter)) != viewport_h)) {
            v_counter = unchecked((ushort)(viewport_h));
            m68k_run(vint_cycle);
            if ((unchecked((int)(zstate)) == 1)) {
            z80_run(vint_cycle);
            }
            status |= unchecked((ushort)(128));
            vint_pending = unchecked((byte)(32));
            if (((unchecked((int)(reg[(int)(1)])) & 32) != 0)) {
            m68k_set_irq(unchecked((uint)(6)));
            }
            z80.irq_state = 1;
            }
            scd_update(unchecked((uint)(3420)));
            if ((unchecked((int)(zstate)) == 1)) {
            z80_run(unchecked((uint)(3420)));
            }
            z80.irq_state = 0;
            mcycles_vdp = unchecked((uint)(3420));
            line = (viewport_h + 1);
            start = (unchecked((int)(lines_per_frame)) - viewport_y);
            end = (viewport_h + viewport_y);
            do {
            v_counter = unchecked((ushort)(line));
            if (((line < end) || (line >= start))) {
            blank_line(line, (-viewport_x), (viewport_w + (2 * viewport_x)));
            }
            input_refresh();
            scd_update((mcycles_vdp + unchecked((uint)(3420))));
            if ((unchecked((int)(zstate)) == 1)) {
            z80_run((mcycles_vdp + unchecked((uint)(3420))));
            }
            mcycles_vdp += unchecked((uint)(3420));
            } while (((++line) < (unchecked((int)(lines_per_frame)) - 1)));
            v_counter = unchecked((ushort)(line));
            if ((viewport_y != 0)) {
            blank_line(line, (-viewport_x), (viewport_w + (2 * viewport_x)));
            }
            h_counter = reg[(int)(10)];
            status &= unchecked((ushort)((~8)));
            if ((dma_length != 0)) {
            vdp_dma_update(mcycles_vdp);
            }
            if (((unchecked((int)(reg[(int)(1)])) & 64) != 0)) {
            parse_satb((-1));
            }
            input_refresh();
            scd_update((mcycles_vdp + unchecked((uint)(3420))));
            if ((unchecked((int)(zstate)) == 1)) {
            z80_run((mcycles_vdp + unchecked((uint)(3420))));
            }
            mcycles_vdp += unchecked((uint)(3420));
            line = 0;
            do {
            v_counter = unchecked((ushort)(line));
            if ((dma_length != 0)) {
            vdp_dma_update(mcycles_vdp);
            }
            if ((!(do_skip != 0))) {
            render_line(line);
            }
            input_refresh();
            if ((unchecked((int)(h_counter)) == 0)) {
            h_counter = reg[(int)(10)];
            hint_pending = unchecked((byte)(16));
            if (((unchecked((int)(reg[(int)(0)])) & 16) != 0)) {
            m68k_update_irq(unchecked((uint)(4)));
            }
            } else {
            h_counter--;
            }
            scd_update((mcycles_vdp + unchecked((uint)(3420))));
            if ((unchecked((int)(zstate)) == 1)) {
            z80_run((mcycles_vdp + unchecked((uint)(3420))));
            }
            mcycles_vdp += unchecked((uint)(3420));
            } while (((++line) < viewport_h));
            if ((viewport_w != viewport_ow)) {
            viewport_ow = viewport_w;
            viewport_changed |= 1;
            }
            scd_end_frame(cd_cycles);
            input_end_frame(mcycles_vdp);
            main.refresh_cycles -= unchecked((int)(mcycles_vdp));
            main.cycles -= unchecked((int)(mcycles_vdp));
            z80.cycles -= mcycles_vdp;
            dma_endCycles = unchecked((uint)(0));
        }
    }
    protected void ExecuteEightBitFrame(int do_skip)
    {
        unchecked
        {
            int start = default;
            int end = default;
            int line = default;
            mcycles_vdp = unchecked((uint)(0));
            fifo_cycles[(int)(0)] = unchecked((uint)(0));
            fifo_cycles[(int)(1)] = unchecked((uint)(0));
            fifo_cycles[(int)(2)] = unchecked((uint)(0));
            fifo_cycles[(int)(3)] = unchecked((uint)(0));
            if (((viewport_changed & 2) != 0)) {
            viewport_changed &= (~2);
            if (((unchecked((int)(system_hw)) & 128) != 0)) {
            int old_interlaced = unchecked((int)(interlaced));
            interlaced = unchecked((byte)(((unchecked((int)(reg[(int)(12)])) & 2) >> 1)));
            if ((old_interlaced != unchecked((int)(interlaced)))) {
            im2_flag = (((unchecked((int)(reg[(int)(12)])) & 6) == 6) ? (byte)1 : (byte)0);
            odd_frame = interlaced;
            viewport_changed = 5;
            if (((unchecked((int)(reg[(int)(1)])) & 4) != 0)) {
            SelectMode5Renderers();
            }
            }
            if (((unchecked((int)(reg[(int)(1)])) & 4) != 0)) {
            if (((unchecked((int)(reg[(int)(1)])) & 8) != 0)) {
            viewport_h = 240;
            viewport_y = (((overscan & 1) * 24) * unchecked((int)(vdp_pal)));
            } else {
            viewport_h = 224;
            viewport_y = ((overscan & 1) * (8 + (24 * unchecked((int)(vdp_pal)))));
            }
            } else {
            viewport_h = 192;
            viewport_y = (((overscan & 1) * 24) * (unchecked((int)(vdp_pal)) + 1));
            }
            } else {
            int mode = ((unchecked((int)(reg[(int)(0)])) & 6) | (unchecked((int)(reg[(int)(1)])) & 24));
            if ((mode == 14)) {
            viewport_h = 240;
            } else if ((mode == 22)) {
            viewport_h = 224;
            } else {
            viewport_h = 192;
            }
            if (((overscan & 1) != 0)) {
            viewport_y = (((240 + (48 * unchecked((int)(vdp_pal)))) - viewport_h) >> 1);
            } else {
            if (((unchecked((int)(system_hw)) == 64) && (!(gg_extra != 0)))) {
            viewport_y = ((144 - viewport_h) / 2);
            } else {
            viewport_y = 0;
            }
            }
            }
            viewport_w = (256 + ((unchecked((int)(reg[(int)(12)])) & 1) << 6));
            if ((viewport_h != viewport_oh)) {
            viewport_oh = viewport_h;
            viewport_changed |= 1;
            }
            }
            v_counter = unchecked((ushort)(viewport_h));
            if ((viewport_y > 0)) {
            blank_line(unchecked((int)(v_counter)), (-viewport_x), (viewport_w + (2 * viewport_x)));
            }
            if (((unchecked((int)(system_hw)) & 128) != 0)) {
            status &= unchecked((ushort)(237));
            status |= unchecked((ushort)(8));
            if ((interlaced != 0)) {
            odd_frame ^= unchecked((byte)(1));
            status |= unchecked((ushort)((unchecked((int)(odd_frame)) << 4)));
            }
            if ((dma_length != 0)) {
            vdp_dma_update(unchecked((uint)(0)));
            }
            }
            input_refresh();
            if ((unchecked((int)(h_counter)) == 0)) {
            hint_pending = unchecked((byte)(16));
            if (((unchecked((int)(reg[(int)(0)])) & 16) != 0)) {
            if (((z80.cycles % unchecked((uint)(3420))) == unchecked((uint)(0)))) {
            z80_run((z80.cycles + unchecked((uint)(1))));
            }
            z80.irq_state = 1;
            }
            }
            osd_input_update();
            z80_run(unchecked((uint)(3420)));
            if ((unchecked((int)(v_counter)) == viewport_h)) {
            status |= unchecked((ushort)(128));
            vint_pending = unchecked((byte)(32));
            if (((unchecked((int)(reg[(int)(1)])) & 32) != 0)) {
            z80.irq_state = 1;
            }
            }
            mcycles_vdp = unchecked((uint)(3420));
            line = (viewport_h + 1);
            start = (unchecked((int)(lines_per_frame)) - viewport_y);
            end = (viewport_h + viewport_y);
            do {
            v_counter = unchecked((ushort)(line));
            if (((line < end) || (line >= start))) {
            if (((unchecked((int)(system_hw)) < 128) && (line > (unchecked((int)(lines_per_frame)) - 16)))) {
            if (((unchecked((int)(reg[(int)(1)])) & 64) != 0)) {
            render_obj(((line - unchecked((int)(lines_per_frame))) & 1));
            }
            parse_satb((line - unchecked((int)(lines_per_frame))));
            }
            blank_line(line, (-viewport_x), (viewport_w + (2 * viewport_x)));
            }
            input_refresh();
            z80_run((mcycles_vdp + unchecked((uint)(3420))));
            mcycles_vdp += unchecked((uint)(3420));
            } while (((++line) < (unchecked((int)(lines_per_frame)) - 1)));
            v_counter = unchecked((ushort)(line));
            if ((viewport_y > 0)) {
            if ((unchecked((int)(system_hw)) < 128)) {
            if (((unchecked((int)(reg[(int)(1)])) & 64) != 0)) {
            render_obj(1);
            }
            }
            blank_line(line, (-viewport_x), (viewport_w + (2 * viewport_x)));
            }
            h_counter = reg[(int)(10)];
            if ((unchecked((int)(system_hw)) != 64)) {
            if (((unchecked((int)(input_pad[(int)(0)])) & 128) != 0)) {
            if ((!(pause_b != 0))) {
            pause_b = unchecked((byte)(1));
            z80_set_nmi_line(unchecked((uint)(1)));
            z80_set_nmi_line(unchecked((uint)(0)));
            }
            } else {
            pause_b = unchecked((byte)(0));
            }
            }
            do_skip |= ((unchecked((int)(work_ram[(int)(8187)])) & unchecked((int)(cart.special))) & 1);
            if (((unchecked((int)(system_hw)) & 128) != 0)) {
            status &= unchecked((ushort)((~8)));
            if ((dma_length != 0)) {
            vdp_dma_update(mcycles_vdp);
            }
            if (((unchecked((int)(reg[(int)(1)])) & 64) != 0)) {
            parse_satb((-1));
            }
            } else {
            parse_satb((-1));
            }
            input_refresh();
            z80_run((mcycles_vdp + unchecked((uint)(3420))));
            mcycles_vdp += unchecked((uint)(3420));
            vscroll = unchecked((ushort)(reg[(int)(9)]));
            line = 0;
            do {
            if ((dma_length != 0)) {
            vdp_dma_update(mcycles_vdp);
            }
            if ((unchecked((int)(v_counter)) != line)) {
            v_counter = unchecked((ushort)(line));
            if ((!(do_skip != 0))) {
            render_line(line);
            }
            }
            input_refresh();
            if ((unchecked((int)(h_counter)) == 0)) {
            h_counter = reg[(int)(10)];
            hint_pending = unchecked((byte)(16));
            if (((unchecked((int)(reg[(int)(0)])) & 16) != 0)) {
            if (((z80.cycles % unchecked((uint)(3420))) == unchecked((uint)(0)))) {
            z80_run((z80.cycles + unchecked((uint)(1))));
            }
            z80.irq_state = 1;
            }
            } else {
            h_counter--;
            }
            z80_run((mcycles_vdp + unchecked((uint)(3420))));
            mcycles_vdp += unchecked((uint)(3420));
            } while (((++line) < viewport_h));
            if ((viewport_w != viewport_ow)) {
            viewport_ow = viewport_w;
            viewport_changed |= 1;
            }
            input_end_frame(mcycles_vdp);
            z80.cycles -= mcycles_vdp;
        }
    }
}
