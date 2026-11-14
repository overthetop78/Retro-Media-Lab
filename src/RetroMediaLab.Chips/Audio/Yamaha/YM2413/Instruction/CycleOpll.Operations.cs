using System.Buffers.Binary;
using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Chips.Audio.Yamaha.YM2413.Instruction;

public sealed partial class CycleOpll
{
    private void OPLL_DoIO(ArrayPointer<opll_t> chip)
    {
        unchecked
        {
            chip[0].write_a_en = (((unchecked((int)(chip[0].write_a)) & 3) == 1) ? (byte)1 : (byte)0);
            chip[0].write_d_en = (((unchecked((int)(chip[0].write_d)) & 3) == 1) ? (byte)1 : (byte)0);
            chip[0].write_a <<= 1;
            chip[0].write_d <<= 1;
        }
    }
    private void OPLL_DoModeWrite(ArrayPointer<opll_t> chip)
    {
        unchecked
        {
            byte slot = default;
            if ((((unchecked((int)(chip[0].write_mode_address)) & 16) != 0) && (chip[0].write_d_en != 0))) {
            slot = unchecked((byte)((unchecked((int)(chip[0].write_mode_address)) & 1)));
            switch ((unchecked((int)(chip[0].write_mode_address)) & 15)) {
            case 0:
            case 1:
            chip[0].patch.multi[(int)(slot)] = unchecked((byte)((unchecked((int)(chip[0].write_data)) & 15)));
            chip[0].patch.ksr[(int)(slot)] = unchecked((byte)(((unchecked((int)(chip[0].write_data)) >> 4) & 1)));
            chip[0].patch.et[(int)(slot)] = unchecked((byte)(((unchecked((int)(chip[0].write_data)) >> 5) & 1)));
            chip[0].patch.vib[(int)(slot)] = unchecked((byte)(((unchecked((int)(chip[0].write_data)) >> 6) & 1)));
            chip[0].patch.am[(int)(slot)] = unchecked((byte)(((unchecked((int)(chip[0].write_data)) >> 7) & 1)));
            break;
            case 2:
            chip[0].patch.ksl[(int)(0)] = unchecked((byte)(((unchecked((int)(chip[0].write_data)) >> 6) & 3)));
            chip[0].patch.tl = unchecked((byte)((unchecked((int)(chip[0].write_data)) & 63)));
            break;
            case 3:
            chip[0].patch.ksl[(int)(1)] = unchecked((byte)(((unchecked((int)(chip[0].write_data)) >> 6) & 3)));
            chip[0].patch.dc = unchecked((byte)(((unchecked((int)(chip[0].write_data)) >> 4) & 1)));
            chip[0].patch.dm = unchecked((byte)(((unchecked((int)(chip[0].write_data)) >> 3) & 1)));
            chip[0].patch.fb = unchecked((byte)((unchecked((int)(chip[0].write_data)) & 7)));
            break;
            case 4:
            case 5:
            chip[0].patch.dr[(int)(slot)] = unchecked((byte)((unchecked((int)(chip[0].write_data)) & 15)));
            chip[0].patch.ar[(int)(slot)] = unchecked((byte)(((unchecked((int)(chip[0].write_data)) >> 4) & 15)));
            break;
            case 6:
            case 7:
            chip[0].patch.rr[(int)(slot)] = unchecked((byte)((unchecked((int)(chip[0].write_data)) & 15)));
            chip[0].patch.sl[(int)(slot)] = unchecked((byte)(((unchecked((int)(chip[0].write_data)) >> 4) & 15)));
            break;
            case 14:
            chip[0].rhythm = unchecked((byte)((unchecked((int)(chip[0].write_data)) & 63)));
            if ((chip[0].chip_type == unchecked((uint)(1)))) {
            chip[0].rhythm |= unchecked((byte)(32));
            }
            chip[0].rm_enable = unchecked((sbyte)(((unchecked((int)(chip[0].rm_enable)) & 127) | ((unchecked((int)(chip[0].rhythm)) << 2) & 128))));
            break;
            case 15:
            chip[0].testmode = unchecked((byte)((unchecked((int)(chip[0].write_data)) & 15)));
            break;
            }
            }
        }
    }
    private void OPLL_Reset(ArrayPointer<opll_t> chip, uint chip_type)
    {
        unchecked
        {
            uint i = default;
            ClearState(chip);
            chip[0].chip_type = chip_type;
            if ((chip_type == unchecked((uint)(1)))) {
            chip[0].rhythm = unchecked((byte)(32));
            chip[0].rm_enable = unchecked((sbyte)(128));
            }
            switch (chip_type) {
            case 1:
            chip[0].patchrom = patch_ds1001;
            break;
            case 0:
            case 2:
            default:
            chip[0].patchrom = patch_ym2413;
            break;
            }
            for (i = unchecked((uint)(0)); (i < unchecked((uint)(18))); i++) {
            chip[0].eg_state[(int)(i)] = unchecked((byte)(3));
            chip[0].eg_level[(int)(i)] = unchecked((byte)(127));
            chip[0].eg_out = unchecked((byte)(127));
            }
            chip[0].rm_select = unchecked((uint)((5 + 1)));
        }
    }
    private void OPLL_Restore(ArrayPointer<opll_t> chip)
    {
        unchecked
        {
            uint i = default;
            switch (chip[0].chip_type) {
            case 1:
            chip[0].patchrom = patch_ds1001;
            break;
            case 0:
            case 2:
            default:
            chip[0].patchrom = patch_ym2413;
            break;
            }
            chip[0].cycles %= unchecked((uint)(18));
            chip[0].eg_timer_low_lock &= unchecked((byte)(3));
            chip[0].c_multi &= unchecked((byte)(15));
            for (i = unchecked((uint)(0)); (i < unchecked((uint)(2))); i++) {
            chip[0].patch.multi[(int)(i)] &= unchecked((byte)(15));
            }
            for (i = unchecked((uint)(0)); (i < unchecked((uint)(9))); i++) {
            chip[0].fnum[(int)(i)] &= unchecked((ushort)(511));
            chip[0].inst[(int)(i)] &= unchecked((byte)(15));
            }
        }
    }
    private void OPLL_DoRegWrite(ArrayPointer<opll_t> chip)
    {
        unchecked
        {
            uint channel = default;
            if ((chip[0].write_a_en != 0)) {
            if (((unchecked((int)(chip[0].write_data)) & 192) == 0)) {
            chip[0].write_fm_address = unchecked((byte)(1));
            chip[0].address = chip[0].write_data;
            } else {
            chip[0].write_fm_address = unchecked((byte)(0));
            }
            }
            if (((chip[0].write_fm_address != 0) && (chip[0].write_d_en != 0))) {
            chip[0].data = chip[0].write_data;
            }
            if (((chip[0].write_fm_data != 0) && (!(chip[0].write_a_en != 0)))) {
            if (((unchecked((uint)((unchecked((int)(chip[0].address)) & 15))) == chip[0].cycles) && (chip[0].cycles < unchecked((uint)(16))))) {
            channel = (chip[0].cycles % unchecked((uint)(9)));
            switch ((unchecked((int)(chip[0].address)) & 240)) {
            case 16:
            chip[0].fnum[(int)(channel)] = unchecked((ushort)(((unchecked((int)(chip[0].fnum[(int)(channel)])) & 256) | unchecked((int)(chip[0].data)))));
            break;
            case 32:
            chip[0].fnum[(int)(channel)] = unchecked((ushort)(((unchecked((int)(chip[0].fnum[(int)(channel)])) & 255) | ((unchecked((int)(chip[0].data)) & 1) << 8))));
            chip[0].block[(int)(channel)] = unchecked((byte)(((unchecked((int)(chip[0].data)) >> 1) & 7)));
            chip[0].kon[(int)(channel)] = unchecked((byte)(((unchecked((int)(chip[0].data)) >> 4) & 1)));
            chip[0].son[(int)(channel)] = unchecked((byte)(((unchecked((int)(chip[0].data)) >> 5) & 1)));
            break;
            case 48:
            chip[0].vol[(int)(channel)] = unchecked((byte)((unchecked((int)(chip[0].data)) & 15)));
            chip[0].inst[(int)(channel)] = unchecked((byte)(((unchecked((int)(chip[0].data)) >> 4) & 15)));
            break;
            }
            }
            }
            if ((chip[0].write_a_en != 0)) {
            chip[0].write_fm_data = unchecked((byte)(0));
            }
            if (((chip[0].write_fm_address != 0) && (chip[0].write_d_en != 0))) {
            chip[0].write_fm_data = unchecked((byte)(1));
            }
            if ((chip[0].write_a_en != 0)) {
            if (((unchecked((int)(chip[0].write_data)) & 240) == 0)) {
            chip[0].write_mode_address = unchecked((byte)((16 | (unchecked((int)(chip[0].write_data)) & 15))));
            } else {
            chip[0].write_mode_address = unchecked((byte)(0));
            }
            }
        }
    }
    private void OPLL_PreparePatch1(ArrayPointer<opll_t> chip)
    {
        unchecked
        {
            byte instr = default;
            uint mcsel = (((chip[0].cycles + unchecked((uint)(1))) / unchecked((uint)(3))) & unchecked((uint)(1)));
            uint instr_index = unchecked((uint)(0));
            uint ch = ch_offset[(int)(chip[0].cycles)];
            ArrayPointer<opll_patch_t> patch = default;
            instr = chip[0].inst[(int)(ch)];
            if ((unchecked((int)(instr)) > 0)) {
            instr_index = unchecked((uint)(((0 + unchecked((int)(instr))) - 1)));
            }
            if ((chip[0].rm_select <= unchecked((uint)(5)))) {
            instr_index = (unchecked((uint)(15)) + chip[0].rm_select);
            }
            if (((chip[0].rm_select <= unchecked((uint)(5))) || (unchecked((int)(instr)) > 0))) {
            patch = new ArrayPointer<opll_patch_t>(chip[0].patchrom, (int)(instr_index));
            } else {
            patch = chip[0].patch_pointer;
            }
            if (((chip[0].rm_select == unchecked((uint)(1))) || (chip[0].rm_select == unchecked((uint)(2))))) {
            chip[0].c_tl = unchecked((byte)((unchecked((int)(chip[0].inst[(int)(ch)])) << 2)));
            } else if ((mcsel == unchecked((uint)(1)))) {
            chip[0].c_tl = unchecked((byte)((unchecked((int)(chip[0].vol[(int)(ch)])) << 2)));
            } else {
            chip[0].c_tl = patch[0].tl;
            }
            chip[0].c_adrr[(int)(0)] = patch[0].ar[(int)(mcsel)];
            chip[0].c_adrr[(int)(1)] = patch[0].dr[(int)(mcsel)];
            chip[0].c_adrr[(int)(2)] = patch[0].rr[(int)(mcsel)];
            chip[0].c_et = patch[0].et[(int)(mcsel)];
            chip[0].c_ksr = patch[0].ksr[(int)(mcsel)];
            chip[0].c_ksl = patch[0].ksl[(int)(mcsel)];
            chip[0].c_ksr_freq = unchecked((byte)(((unchecked((int)(chip[0].block[(int)(ch)])) << 1) | (unchecked((int)(chip[0].fnum[(int)(ch)])) >> 8))));
            chip[0].c_ksl_freq = unchecked((byte)((unchecked((int)(chip[0].fnum[(int)(ch)])) >> 5)));
            chip[0].c_ksl_block = chip[0].block[(int)(ch)];
        }
    }
    private void OPLL_PreparePatch2(ArrayPointer<opll_t> chip)
    {
        unchecked
        {
            byte instr = default;
            uint mcsel = (((chip[0].cycles + unchecked((uint)(1))) / unchecked((uint)(3))) & unchecked((uint)(1)));
            uint instr_index = unchecked((uint)(0));
            ArrayPointer<opll_patch_t> patch = default;
            instr = chip[0].inst[(int)(ch_offset[(int)(chip[0].cycles)])];
            if ((unchecked((int)(instr)) > 0)) {
            instr_index = unchecked((uint)(((0 + unchecked((int)(instr))) - 1)));
            }
            if ((chip[0].rm_select <= unchecked((uint)(5)))) {
            instr_index = (unchecked((uint)(15)) + chip[0].rm_select);
            }
            if (((chip[0].rm_select <= unchecked((uint)(5))) || (unchecked((int)(instr)) > 0))) {
            patch = new ArrayPointer<opll_patch_t>(chip[0].patchrom, (int)(instr_index));
            } else {
            patch = chip[0].patch_pointer;
            }
            chip[0].c_fnum = chip[0].fnum[(int)(ch_offset[(int)(chip[0].cycles)])];
            chip[0].c_block = unchecked((ushort)(chip[0].block[(int)(ch_offset[(int)(chip[0].cycles)])]));
            chip[0].c_multi = patch[0].multi[(int)(mcsel)];
            chip[0].c_sl = patch[0].sl[(int)(mcsel)];
            chip[0].c_fb = patch[0].fb;
            chip[0].c_vib = patch[0].vib[(int)(mcsel)];
            chip[0].c_am = patch[0].am[(int)(mcsel)];
            chip[0].c_dc <<= 1;
            chip[0].c_dm <<= 1;
            chip[0].c_dc |= patch[0].dc;
            chip[0].c_dm |= patch[0].dm;
        }
    }
    private void OPLL_PhaseGenerate(ArrayPointer<opll_t> chip)
    {
        unchecked
        {
            uint ismod = default;
            uint phase = default;
            byte rm_bit = default;
            ushort pg_out = default;
            chip[0].pg_phase[(int)(((chip[0].cycles + unchecked((uint)(17))) % unchecked((uint)(18))))] = (chip[0].pg_phase_next + chip[0].pg_inc);
            if ((((unchecked((int)(chip[0].rm_enable)) & 64) != 0) && ((chip[0].cycles == unchecked((uint)(13))) || (chip[0].cycles == unchecked((uint)(14)))))) {
            ismod = unchecked((uint)(0));
            } else {
            ismod = (((chip[0].cycles + unchecked((uint)(3))) / unchecked((uint)(3))) & unchecked((uint)(1)));
            }
            phase = chip[0].pg_phase[(int)(chip[0].cycles)];
            if (((((unchecked((int)(chip[0].testmode)) & 4) != 0) || ((ismod != 0) && ((chip[0].eg_dokon & unchecked((uint)(32768))) != 0))) || ((!(ismod != 0)) && ((chip[0].eg_dokon & unchecked((uint)(1))) != 0)))) {
            chip[0].pg_phase_next = unchecked((uint)(0));
            } else {
            chip[0].pg_phase_next = phase;
            }
            if ((chip[0].cycles == unchecked((uint)(13)))) {
            chip[0].rm_hh_bit2 = unchecked((byte)(((phase >> (2 + 9)) & unchecked((uint)(1)))));
            chip[0].rm_hh_bit3 = unchecked((byte)(((phase >> (3 + 9)) & unchecked((uint)(1)))));
            chip[0].rm_hh_bit7 = unchecked((byte)(((phase >> (7 + 9)) & unchecked((uint)(1)))));
            chip[0].rm_hh_bit8 = unchecked((byte)(((phase >> (8 + 9)) & unchecked((uint)(1)))));
            } else if (((chip[0].cycles == unchecked((uint)(17))) && ((unchecked((int)(chip[0].rm_enable)) & 128) != 0))) {
            chip[0].rm_tc_bit3 = unchecked((byte)(((phase >> (3 + 9)) & unchecked((uint)(1)))));
            chip[0].rm_tc_bit5 = unchecked((byte)(((phase >> (5 + 9)) & unchecked((uint)(1)))));
            }
            if (((unchecked((int)(chip[0].rm_enable)) & 128) != 0)) {
            switch (chip[0].cycles) {
            case 13:
            rm_bit = unchecked((byte)((((unchecked((int)(chip[0].rm_hh_bit2)) ^ unchecked((int)(chip[0].rm_hh_bit7))) | (unchecked((int)(chip[0].rm_hh_bit3)) ^ unchecked((int)(chip[0].rm_tc_bit5)))) | (unchecked((int)(chip[0].rm_tc_bit3)) ^ unchecked((int)(chip[0].rm_tc_bit5))))));
            pg_out = unchecked((ushort)((unchecked((int)(rm_bit)) << 9)));
            if (((unchecked((uint)(rm_bit)) ^ (chip[0].rm_noise & unchecked((uint)(1)))) != 0)) {
            pg_out |= unchecked((ushort)(208));
            } else {
            pg_out |= unchecked((ushort)(52));
            }
            break;
            case 16:
            pg_out = unchecked((ushort)((unchecked((uint)((unchecked((int)(chip[0].rm_hh_bit8)) << 9))) | ((unchecked((uint)(chip[0].rm_hh_bit8)) ^ (chip[0].rm_noise & unchecked((uint)(1)))) << 8))));
            break;
            case 17:
            rm_bit = unchecked((byte)((((unchecked((int)(chip[0].rm_hh_bit2)) ^ unchecked((int)(chip[0].rm_hh_bit7))) | (unchecked((int)(chip[0].rm_hh_bit3)) ^ unchecked((int)(chip[0].rm_tc_bit5)))) | (unchecked((int)(chip[0].rm_tc_bit3)) ^ unchecked((int)(chip[0].rm_tc_bit5))))));
            pg_out = unchecked((ushort)(((unchecked((int)(rm_bit)) << 9) | 256)));
            break;
            default:
            pg_out = unchecked((ushort)((phase >> 9)));
            break;
            }
            } else {
            pg_out = unchecked((ushort)((phase >> 9)));
            }
            chip[0].pg_out = pg_out;
        }
    }
    private void OPLL_PhaseCalcIncrement(ArrayPointer<opll_t> chip)
    {
        unchecked
        {
            uint freq = default;
            ushort block = default;
            freq = unchecked((uint)((unchecked((int)(chip[0].c_fnum)) << 1)));
            block = chip[0].c_block;
            if ((chip[0].c_vib != 0)) {
            switch (chip[0].lfo_vib_counter) {
            case 0:
            case 4:
            break;
            case 1:
            case 3:
            freq += (freq >> 8);
            break;
            case 2:
            freq += (freq >> 7);
            break;
            case 5:
            case 7:
            freq -= (freq >> 8);
            break;
            case 6:
            freq -= (freq >> 7);
            break;
            }
            }
            freq = ((freq << unchecked((int)(block))) >> 1);
            chip[0].pg_inc = ((freq * pg_multi[(int)(chip[0].c_multi)]) >> 1);
        }
    }
    private void OPLL_EnvelopeKSLTL(ArrayPointer<opll_t> chip)
    {
        unchecked
        {
            int ksl = default;
            ksl = unchecked((int)((eg_ksltable[(int)(chip[0].c_ksl_freq)] - unchecked((uint)(((8 - unchecked((int)(chip[0].c_ksl_block))) << 3))))));
            if ((ksl < 0)) {
            ksl = 0;
            }
            ksl <<= 1;
            if ((chip[0].c_ksl != 0)) {
            ksl = (ksl >> (3 - unchecked((int)(chip[0].c_ksl))));
            } else {
            ksl = 0;
            }
            chip[0].eg_ksltl = unchecked((ushort)((ksl + (unchecked((int)(chip[0].c_tl)) << 1))));
        }
    }
    private void OPLL_EnvelopeOutput(ArrayPointer<opll_t> chip)
    {
        unchecked
        {
            int level = unchecked((int)(chip[0].eg_level[(int)(((chip[0].cycles + unchecked((uint)(17))) % unchecked((uint)(18))))]));
            level += unchecked((int)(chip[0].eg_ksltl));
            if ((chip[0].c_am != 0)) {
            level += unchecked((int)(chip[0].lfo_am_out));
            }
            if ((level >= 128)) {
            level = 127;
            }
            if (((unchecked((int)(chip[0].testmode)) & 1) != 0)) {
            level = 0;
            }
            chip[0].eg_out = unchecked((byte)(level));
        }
    }
    private void OPLL_EnvelopeGenerate(ArrayPointer<opll_t> chip)
    {
        unchecked
        {
            byte timer_inc = default;
            byte timer_bit = default;
            byte timer_low = default;
            byte rate = default;
            byte state_rate = default;
            byte ksr = default;
            byte sum = default;
            byte rate_hi = default;
            byte rate_lo = default;
            int level = default;
            int next_level = default;
            byte zero = default;
            byte state = default;
            byte next_state = default;
            int step = default;
            int sl = default;
            uint mcsel = (((chip[0].cycles + unchecked((uint)(1))) / unchecked((uint)(3))) & unchecked((uint)(1)));
            if (((unchecked((int)(chip[0].eg_counter_state)) & 3) != 3)) {
            timer_inc = unchecked((byte)(0));
            } else if ((chip[0].cycles == unchecked((uint)(0)))) {
            timer_inc = unchecked((byte)(1));
            } else {
            timer_inc = chip[0].eg_timer_carry;
            }
            timer_low = unchecked((byte)((chip[0].eg_timer & unchecked((uint)(3)))));
            timer_bit = unchecked((byte)((chip[0].eg_timer & unchecked((uint)(1)))));
            timer_bit += timer_inc;
            chip[0].eg_timer_carry = unchecked((byte)((unchecked((int)(timer_bit)) >> 1)));
            chip[0].eg_timer = (unchecked((uint)(((unchecked((int)(timer_bit)) & 1) << 17))) | (chip[0].eg_timer >> 1));
            if (((unchecked((int)(chip[0].testmode)) & 8) != 0)) {
            chip[0].eg_timer &= unchecked((uint)(196607));
            chip[0].eg_timer |= unchecked((uint)(((unchecked((int)(chip[0].write_data)) << (16 - 2)) & 65536)));
            }
            if (((!(chip[0].eg_timer_shift_stop != 0)) && (((chip[0].eg_timer >> 16) & unchecked((uint)(1))) != 0))) {
            chip[0].eg_timer_shift = unchecked((byte)(chip[0].cycles));
            }
            if (((chip[0].cycles == unchecked((uint)(0))) && ((unchecked((int)(chip[0].eg_counter_state_prev)) & 1) == 1))) {
            chip[0].eg_timer_low_lock = timer_low;
            chip[0].eg_timer_shift_lock = chip[0].eg_timer_shift;
            if ((unchecked((int)(chip[0].eg_timer_shift_lock)) > 13)) chip[0].eg_timer_shift_lock = unchecked((byte)(0));
            chip[0].eg_timer_shift = unchecked((byte)(0));
            }
            chip[0].eg_timer_shift_stop |= unchecked((byte)(((chip[0].eg_timer >> 16) & unchecked((uint)(1)))));
            if ((chip[0].cycles == unchecked((uint)(0)))) {
            chip[0].eg_timer_shift_stop = unchecked((byte)(0));
            }
            chip[0].eg_counter_state_prev = chip[0].eg_counter_state;
            if ((chip[0].cycles == unchecked((uint)(17)))) {
            chip[0].eg_counter_state++;
            }
            level = unchecked((int)(chip[0].eg_level[(int)(((chip[0].cycles + unchecked((uint)(16))) % unchecked((uint)(18))))]));
            next_level = level;
            zero = ((level == 0) ? (byte)1 : (byte)0);
            chip[0].eg_silent = ((level == 127) ? (byte)1 : (byte)0);
            if ((((unchecked((int)(chip[0].eg_state[(int)(((chip[0].cycles + unchecked((uint)(16))) % unchecked((uint)(18))))])) != 0) && ((unchecked((int)(chip[0].eg_off)) & 2) != 0)) && (!((chip[0].eg_dokon & unchecked((uint)(2))) != 0)))) {
            next_level = 127;
            }
            if (((chip[0].eg_maxrate != 0) && ((chip[0].eg_dokon & unchecked((uint)(2))) != 0))) {
            next_level = 0;
            }
            state = chip[0].eg_state[(int)(((chip[0].cycles + unchecked((uint)(16))) % unchecked((uint)(18))))];
            next_state = unchecked((byte)(0));
            step = 0;
            sl = unchecked((int)(chip[0].eg_sl));
            switch (state) {
            case 0:
            if ((((!(chip[0].eg_maxrate != 0)) && ((unchecked((int)(chip[0].eg_kon)) & 2) != 0)) && (!(zero != 0)))) {
            int shift = ((unchecked((int)(chip[0].eg_rate_hi)) < 12) ? unchecked((int)(chip[0].eg_inc_lo)) : ((unchecked((int)(chip[0].eg_rate_hi)) - 11) + unchecked((int)(chip[0].eg_inc_hi))));
            if ((shift > 0)) {
            step = ((~level) >> (5 - shift));
            }
            }
            if ((zero != 0)) {
            next_state = unchecked((byte)(1));
            } else {
            next_state = unchecked((byte)(0));
            }
            break;
            case 1:
            if ((((!((unchecked((int)(chip[0].eg_off)) & 2) != 0)) && (!((chip[0].eg_dokon & unchecked((uint)(2))) != 0))) && ((level >> 3) != sl))) {
            byte i0 = (((unchecked((int)(chip[0].eg_rate_hi)) == 15) || ((unchecked((int)(chip[0].eg_rate_hi)) == 14) && (chip[0].eg_inc_hi != 0))) ? (byte)1 : (byte)0);
            byte i1 = ((((((((unchecked((int)(chip[0].eg_rate_hi)) == 14) && (!(chip[0].eg_inc_hi != 0))) || ((unchecked((int)(chip[0].eg_rate_hi)) == 13) && (chip[0].eg_inc_hi != 0))) || (((unchecked((int)(chip[0].eg_rate_hi)) == 13) && (!(chip[0].eg_inc_hi != 0))) && ((unchecked((int)(chip[0].eg_counter_state_prev)) & 1) != 0))) || (((unchecked((int)(chip[0].eg_rate_hi)) == 12) && (chip[0].eg_inc_hi != 0)) && ((unchecked((int)(chip[0].eg_counter_state_prev)) & 1) != 0))) || (((unchecked((int)(chip[0].eg_rate_hi)) == 12) && (!(chip[0].eg_inc_hi != 0))) && ((unchecked((int)(chip[0].eg_counter_state_prev)) & 3) == 3))) || ((chip[0].eg_inc_lo != 0) && ((unchecked((int)(chip[0].eg_counter_state_prev)) & 3) == 3))) ? (byte)1 : (byte)0);
            step = ((unchecked((int)(i0)) << 1) | unchecked((int)(i1)));
            }
            if (((level >> 3) == sl)) {
            next_state = unchecked((byte)(2));
            } else {
            next_state = unchecked((byte)(1));
            }
            break;
            case 2:
            case 3:
            if (((!((unchecked((int)(chip[0].eg_off)) & 2) != 0)) && (!((chip[0].eg_dokon & unchecked((uint)(2))) != 0)))) {
            byte i0 = (((unchecked((int)(chip[0].eg_rate_hi)) == 15) || ((unchecked((int)(chip[0].eg_rate_hi)) == 14) && (chip[0].eg_inc_hi != 0))) ? (byte)1 : (byte)0);
            byte i1 = ((((((((unchecked((int)(chip[0].eg_rate_hi)) == 14) && (!(chip[0].eg_inc_hi != 0))) || ((unchecked((int)(chip[0].eg_rate_hi)) == 13) && (chip[0].eg_inc_hi != 0))) || (((unchecked((int)(chip[0].eg_rate_hi)) == 13) && (!(chip[0].eg_inc_hi != 0))) && ((unchecked((int)(chip[0].eg_counter_state_prev)) & 1) != 0))) || (((unchecked((int)(chip[0].eg_rate_hi)) == 12) && (chip[0].eg_inc_hi != 0)) && ((unchecked((int)(chip[0].eg_counter_state_prev)) & 1) != 0))) || (((unchecked((int)(chip[0].eg_rate_hi)) == 12) && (!(chip[0].eg_inc_hi != 0))) && ((unchecked((int)(chip[0].eg_counter_state_prev)) & 3) == 3))) || ((chip[0].eg_inc_lo != 0) && ((unchecked((int)(chip[0].eg_counter_state_prev)) & 3) == 3))) ? (byte)1 : (byte)0);
            step = ((unchecked((int)(i0)) << 1) | unchecked((int)(i1)));
            }
            next_state = state;
            break;
            }
            if ((!((unchecked((int)(chip[0].eg_kon)) & 2) != 0))) {
            next_state = unchecked((byte)(3));
            }
            if (((chip[0].eg_dokon & unchecked((uint)(2))) != 0)) {
            next_state = unchecked((byte)(0));
            }
            chip[0].eg_level[(int)(((chip[0].cycles + unchecked((uint)(16))) % unchecked((uint)(18))))] = unchecked((byte)((next_level + step)));
            chip[0].eg_state[(int)(((chip[0].cycles + unchecked((uint)(16))) % unchecked((uint)(18))))] = next_state;
            rate_hi = unchecked((byte)((unchecked((int)(chip[0].eg_rate)) >> 2)));
            rate_lo = unchecked((byte)((unchecked((int)(chip[0].eg_rate)) & 3)));
            chip[0].eg_inc_hi = unchecked((byte)(new ArrayPointer<uint>(eg_stephi, (int)(rate_lo) * 4)[(int)(chip[0].eg_timer_low_lock)]));
            sum = unchecked((byte)(((unchecked((int)(chip[0].eg_timer_shift_lock)) + unchecked((int)(rate_hi))) & 15)));
            chip[0].eg_inc_lo = unchecked((byte)(0));
            if (((unchecked((int)(rate_hi)) < 12) && (!(chip[0].eg_zerorate != 0)))) {
            switch (sum) {
            case 12:
            chip[0].eg_inc_lo = unchecked((byte)(1));
            break;
            case 13:
            chip[0].eg_inc_lo = unchecked((byte)(((unchecked((int)(rate_lo)) >> 1) & 1)));
            break;
            case 14:
            chip[0].eg_inc_lo = unchecked((byte)((unchecked((int)(rate_lo)) & 1)));
            break;
            }
            }
            chip[0].eg_maxrate = ((unchecked((int)(rate_hi)) == 15) ? (byte)1 : (byte)0);
            chip[0].eg_rate_hi = rate_hi;
            chip[0].eg_kon <<= 1;
            chip[0].eg_kon |= chip[0].kon[(int)(ch_offset[(int)(chip[0].cycles)])];
            chip[0].eg_off <<= 1;
            chip[0].eg_off |= (((unchecked((int)(chip[0].eg_level[(int)(chip[0].cycles)])) >> 2) == 31) ? (byte)1 : (byte)0);
            switch (chip[0].rm_select) {
            case 0:
            case 3:
            chip[0].eg_kon |= unchecked((byte)(((unchecked((int)(chip[0].rhythm)) >> 4) & 1)));
            break;
            case 4:
            chip[0].eg_kon |= unchecked((byte)(((unchecked((int)(chip[0].rhythm)) >> 3) & 1)));
            break;
            case 2:
            chip[0].eg_kon |= unchecked((byte)(((unchecked((int)(chip[0].rhythm)) >> 2) & 1)));
            break;
            case 5:
            chip[0].eg_kon |= unchecked((byte)(((unchecked((int)(chip[0].rhythm)) >> 1) & 1)));
            break;
            case 1:
            chip[0].eg_kon |= unchecked((byte)((unchecked((int)(chip[0].rhythm)) & 1)));
            break;
            }
            rate = unchecked((byte)(0));
            chip[0].eg_dokon <<= 1;
            state_rate = chip[0].eg_state[(int)(chip[0].cycles)];
            if ((((unchecked((int)(state_rate)) == 3) && ((unchecked((int)(chip[0].eg_kon)) & 1) != 0)) && ((unchecked((int)(chip[0].eg_off)) & 1) != 0))) {
            state_rate = unchecked((byte)(0));
            chip[0].eg_dokon |= unchecked((uint)(1));
            }
            switch (state_rate) {
            case 0:
            rate = chip[0].c_adrr[(int)(0)];
            break;
            case 1:
            rate = chip[0].c_adrr[(int)(1)];
            break;
            case 2:
            if ((!(chip[0].c_et != 0))) {
            rate = chip[0].c_adrr[(int)(2)];
            }
            break;
            case 3:
            if ((chip[0].son[(int)(ch_offset[(int)(chip[0].cycles)])] != 0)) {
            rate = unchecked((byte)(5));
            } else {
            rate = chip[0].c_adrr[(int)(2)];
            }
            break;
            }
            if (((((!((unchecked((int)(chip[0].eg_kon)) & 1) != 0)) && (!(mcsel != 0))) && (chip[0].rm_select != unchecked((uint)(2)))) && (chip[0].rm_select != unchecked((uint)(1))))) {
            rate = unchecked((byte)(0));
            }
            if (((((unchecked((int)(chip[0].eg_kon)) & 1) != 0) && (unchecked((int)(chip[0].eg_state[(int)(chip[0].cycles)])) == 3)) && (!((unchecked((int)(chip[0].eg_off)) & 1) != 0)))) {
            rate = unchecked((byte)(12));
            }
            if (((((!((unchecked((int)(chip[0].eg_kon)) & 1) != 0)) && (!(chip[0].son[(int)(ch_offset[(int)(chip[0].cycles)])] != 0))) && (mcsel == unchecked((uint)(1)))) && (!(chip[0].c_et != 0)))) {
            rate = unchecked((byte)(7));
            }
            chip[0].eg_zerorate = ((unchecked((int)(rate)) == 0) ? (byte)1 : (byte)0);
            ksr = chip[0].c_ksr_freq;
            if ((!(chip[0].c_ksr != 0))) ksr >>= 2;
            chip[0].eg_rate = unchecked((byte)(((unchecked((int)(rate)) << 2) + unchecked((int)(ksr)))));
            if (((unchecked((int)(chip[0].eg_rate)) & 64) != 0)) {
            chip[0].eg_rate = unchecked((byte)((60 | (unchecked((int)(ksr)) & 3))));
            }
            chip[0].eg_sl = unchecked((ushort)(chip[0].c_sl));
        }
    }
    private void OPLL_Channel(ArrayPointer<opll_t> chip)
    {
        unchecked
        {
            short sign = default;
            short ch_out = chip[0].ch_out;
            byte ismod = unchecked((byte)(((chip[0].cycles / unchecked((uint)(3))) & unchecked((uint)(1)))));
            byte mute_m = (((ismod != 0) || (((unchecked((int)(chip[0].rm_enable)) & 64) != 0) && (((chip[0].cycles + unchecked((uint)(15))) % unchecked((uint)(18))) >= unchecked((uint)(12))))) ? (byte)1 : (byte)0);
            byte mute_r = unchecked((byte)(1));
            if ((chip[0].chip_type == unchecked((uint)(1)))) {
            chip[0].output_m = ch_out;
            if ((unchecked((int)(chip[0].output_m)) >= 0)) {
            chip[0].output_m++;
            }
            if ((mute_m != 0)) {
            chip[0].output_m = unchecked((short)(0));
            }
            chip[0].output_r = unchecked((short)(0));
            return;
            } else {
            if (((unchecked((int)(chip[0].rm_enable)) & 64) != 0)) {
            switch (chip[0].cycles) {
            case 16:
            case 17:
            case 0:
            case 1:
            case 2:
            case 3:
            case 4:
            case 5:
            case 9:
            case 10:
            mute_r = unchecked((byte)(0));
            break;
            }
            }
            if ((chip[0].chip_type == unchecked((uint)(2)))) {
            if ((mute_m != 0)) chip[0].output_m = unchecked((short)(0)); else chip[0].output_m = ch_out;
            if ((mute_r != 0)) chip[0].output_r = unchecked((short)(0)); else chip[0].output_r = ch_out;
            } else {
            sign = unchecked((short)((unchecked((int)(ch_out)) >> 8)));
            if ((unchecked((int)(ch_out)) >= 0)) {
            ch_out++;
            sign++;
            }
            if ((mute_m != 0)) chip[0].output_m = sign; else chip[0].output_m = ch_out;
            if ((mute_r != 0)) chip[0].output_r = sign; else chip[0].output_r = ch_out;
            }
            }
        }
    }
    private void OPLL_Operator(ArrayPointer<opll_t> chip)
    {
        unchecked
        {
            byte ismod1 = default;
            byte ismod2 = default;
            byte ismod3 = default;
            uint op_mod = default;
            ushort exp_shift = default;
            short output = default;
            uint level = default;
            uint phase = default;
            short routput = default;
            if ((((unchecked((int)(chip[0].rm_enable)) & 128) != 0) && ((chip[0].cycles == unchecked((uint)(15))) || (chip[0].cycles == unchecked((uint)(16)))))) {
            ismod1 = unchecked((byte)(0));
            } else {
            ismod1 = unchecked((byte)((((chip[0].cycles + unchecked((uint)(1))) / unchecked((uint)(3))) & unchecked((uint)(1)))));
            }
            if ((((unchecked((int)(chip[0].rm_enable)) & 64) != 0) && ((chip[0].cycles == unchecked((uint)(13))) || (chip[0].cycles == unchecked((uint)(14)))))) {
            ismod2 = unchecked((byte)(0));
            } else {
            ismod2 = unchecked((byte)((((chip[0].cycles + unchecked((uint)(3))) / unchecked((uint)(3))) & unchecked((uint)(1)))));
            }
            if ((((unchecked((int)(chip[0].rm_enable)) & 64) != 0) && ((chip[0].cycles == unchecked((uint)(16))) || (chip[0].cycles == unchecked((uint)(17)))))) {
            ismod3 = unchecked((byte)(0));
            } else {
            ismod3 = unchecked((byte)(((chip[0].cycles / unchecked((uint)(3))) & unchecked((uint)(1)))));
            }
            op_mod = unchecked((uint)(0));
            if ((ismod3 != 0)) {
            op_mod |= unchecked((uint)((unchecked((int)(chip[0].op_mod)) << 1)));
            }
            if (((ismod2 != 0) && (chip[0].c_fb != 0))) {
            op_mod |= unchecked((uint)((unchecked((int)(chip[0].op_fbsum)) >> (7 - unchecked((int)(chip[0].c_fb))))));
            }
            exp_shift = chip[0].op_exp_s;
            if (((chip[0].eg_silent != 0) || (((unchecked((int)(chip[0].op_neg)) & 2) != 0) && (((ismod1 != 0) ? (unchecked((int)(chip[0].c_dm)) & 4) : (unchecked((int)(chip[0].c_dc)) & 4)) != 0)))) {
            exp_shift |= unchecked((ushort)(12));
            }
            output = unchecked((short)((unchecked((int)(chip[0].op_exp_m)) >> unchecked((int)(exp_shift)))));
            if (((!(chip[0].eg_silent != 0)) && ((unchecked((int)(chip[0].op_neg)) & 2) != 0))) {
            output = unchecked((short)((~unchecked((int)(output)))));
            }
            level = unchecked((uint)((unchecked((int)(chip[0].op_logsin)) + (unchecked((int)(chip[0].eg_out)) << 4))));
            if ((level >= unchecked((uint)(4096)))) {
            level = unchecked((uint)(4095));
            }
            chip[0].op_exp_m = exprom[(int)((level & unchecked((uint)(255))))];
            chip[0].op_exp_s = unchecked((ushort)((level >> 8)));
            phase = ((op_mod + unchecked((uint)(chip[0].pg_out))) & unchecked((uint)(1023)));
            if (((phase & unchecked((uint)(256))) != 0)) {
            phase ^= unchecked((uint)(255));
            }
            chip[0].op_logsin = logsinrom[(int)((phase & unchecked((uint)(255))))];
            chip[0].op_neg <<= 1;
            chip[0].op_neg |= unchecked((byte)((phase >> 9)));
            chip[0].op_fbsum = unchecked((short)(((unchecked((int)(chip[0].op_fb1[(int)(((chip[0].cycles + unchecked((uint)(3))) % unchecked((uint)(9))))])) + unchecked((int)(chip[0].op_fb2[(int)(((chip[0].cycles + unchecked((uint)(3))) % unchecked((uint)(9))))]))) >> 1)));
            if ((ismod1 != 0)) {
            chip[0].op_fb2[(int)((chip[0].cycles % unchecked((uint)(9))))] = chip[0].op_fb1[(int)((chip[0].cycles % unchecked((uint)(9))))];
            chip[0].op_fb1[(int)((chip[0].cycles % unchecked((uint)(9))))] = output;
            }
            chip[0].op_mod = unchecked((short)((unchecked((int)(output)) & 511)));
            if ((chip[0].chip_type == unchecked((uint)(1)))) {
            routput = unchecked((short)(0));
            } else {
            switch (chip[0].cycles) {
            case 2:
            routput = chip[0].ch_out_hh;
            break;
            case 3:
            routput = chip[0].ch_out_tm;
            break;
            case 4:
            routput = chip[0].ch_out_bd;
            break;
            case 8:
            routput = chip[0].ch_out_sd;
            break;
            case 9:
            routput = chip[0].ch_out_tc;
            break;
            default:
            routput = unchecked((short)(0));
            break;
            }
            switch (chip[0].cycles) {
            case 15:
            chip[0].ch_out_hh = unchecked((short)((unchecked((int)(output)) >> 3)));
            break;
            case 16:
            chip[0].ch_out_tm = unchecked((short)((unchecked((int)(output)) >> 3)));
            break;
            case 17:
            chip[0].ch_out_bd = unchecked((short)((unchecked((int)(output)) >> 3)));
            break;
            case 0:
            chip[0].ch_out_sd = unchecked((short)((unchecked((int)(output)) >> 3)));
            break;
            case 1:
            chip[0].ch_out_tc = unchecked((short)((unchecked((int)(output)) >> 3)));
            break;
            default:
            break;
            }
            }
            if ((!((unchecked((int)(chip[0].rm_enable)) & 128) != 0))) routput = unchecked((short)(0));
            chip[0].ch_out = unchecked((short)(((ismod1 != 0) ? unchecked((int)(routput)) : (unchecked((int)(output)) >> 3))));
            // The output pipeline is seven channel slots behind the register scan.
            int outputChannel = ismod1 == 0 ? (int)((chip[0].cycles + 7) % 9) : chip[0].cycles switch {
                2 or 8 => 7, 3 or 9 => 8, _ => 6
            };
            chip[0].ch_out = (short)(chip[0].ch_out * _channelVolumes[outputChannel] / 100);
        }
    }
    private void OPLL_DoRhythm(ArrayPointer<opll_t> chip)
    {
        unchecked
        {
            byte nbit = default;
            nbit = unchecked((byte)(((chip[0].rm_noise ^ (chip[0].rm_noise >> 14)) & unchecked((uint)(1)))));
            nbit |= unchecked((byte)((((chip[0].rm_noise == unchecked((uint)(0))) ? (int)1 : (int)0) | ((unchecked((int)(chip[0].testmode)) >> 1) & 1))));
            chip[0].rm_noise = (unchecked((uint)((unchecked((int)(nbit)) << 22))) | (chip[0].rm_noise >> 1));
        }
    }
    private void OPLL_DoLFO(ArrayPointer<opll_t> chip)
    {
        unchecked
        {
            byte vib_step = default;
            byte am_inc = unchecked((byte)(0));
            byte am_bit = default;
            if ((chip[0].cycles == unchecked((uint)(17)))) {
            vib_step = unchecked((byte)((((unchecked((int)(chip[0].lfo_counter)) & 1023) + 1) >> 10)));
            chip[0].lfo_am_step = unchecked((byte)((((unchecked((int)(chip[0].lfo_counter)) & 63) + 1) >> 6)));
            vib_step |= unchecked((byte)(((unchecked((int)(chip[0].testmode)) >> 3) & 1)));
            chip[0].lfo_vib_counter += vib_step;
            chip[0].lfo_vib_counter &= unchecked((byte)(7));
            chip[0].lfo_counter++;
            }
            if ((((chip[0].lfo_am_step != 0) || ((unchecked((int)(chip[0].testmode)) & 8) != 0)) && (chip[0].cycles < unchecked((uint)(9))))) {
            am_inc = unchecked((byte)((unchecked((int)(chip[0].lfo_am_dir)) | ((chip[0].cycles == unchecked((uint)(0))) ? (int)1 : (int)0))));
            }
            if ((chip[0].cycles >= unchecked((uint)(9)))) {
            chip[0].lfo_am_car = unchecked((byte)(0));
            }
            if ((chip[0].cycles == unchecked((uint)(0)))) {
            if (((chip[0].lfo_am_dir != 0) && ((unchecked((int)(chip[0].lfo_am_counter)) & 127) == 0))) {
            chip[0].lfo_am_dir = unchecked((byte)(0));
            } else if (((!(chip[0].lfo_am_dir != 0)) && ((unchecked((int)(chip[0].lfo_am_counter)) & 105) == 105))) {
            chip[0].lfo_am_dir = unchecked((byte)(1));
            }
            }
            am_bit = unchecked((byte)((unchecked((int)(chip[0].lfo_am_counter)) & 1)));
            am_bit += unchecked((byte)((unchecked((int)(am_inc)) + unchecked((int)(chip[0].lfo_am_car)))));
            chip[0].lfo_am_car = unchecked((byte)((unchecked((int)(am_bit)) >> 1)));
            am_bit &= unchecked((byte)(1));
            chip[0].lfo_am_counter = unchecked((ushort)(((unchecked((int)(am_bit)) << 8) | (unchecked((int)(chip[0].lfo_am_counter)) >> 1))));
            if (((unchecked((int)(chip[0].testmode)) & 2) != 0)) {
            chip[0].lfo_vib_counter = unchecked((byte)(0));
            chip[0].lfo_counter = unchecked((ushort)(0));
            chip[0].lfo_am_dir = unchecked((byte)(0));
            chip[0].lfo_am_counter &= unchecked((ushort)(255));
            }
        }
    }
    private void OPLL_Clock(ArrayPointer<opll_t> chip, ArrayPointer<int> buffer)
    {
        unchecked
        {
            buffer[(int)(0)] = unchecked((int)(chip[0].output_m));
            buffer[(int)(1)] = unchecked((int)(chip[0].output_r));
            if ((chip[0].cycles == unchecked((uint)(0)))) {
            chip[0].lfo_am_out = unchecked((byte)(((unchecked((int)(chip[0].lfo_am_counter)) >> 3) & 15)));
            }
            chip[0].rm_enable >>= 1;
            OPLL_DoModeWrite(chip);
            chip[0].rm_select++;
            if ((chip[0].rm_select > unchecked((uint)(5)))) {
            chip[0].rm_select = unchecked((uint)((5 + 1)));
            }
            if (((chip[0].cycles == unchecked((uint)(11))) && ((unchecked((int)(chip[0].rm_enable)) & 128) == 128))) {
            chip[0].rm_select = unchecked((uint)(0));
            }
            OPLL_PreparePatch1(chip);
            OPLL_Channel(chip);
            OPLL_PhaseGenerate(chip);
            OPLL_Operator(chip);
            OPLL_PhaseCalcIncrement(chip);
            OPLL_EnvelopeOutput(chip);
            OPLL_EnvelopeKSLTL(chip);
            OPLL_EnvelopeGenerate(chip);
            OPLL_DoLFO(chip);
            OPLL_DoRhythm(chip);
            OPLL_PreparePatch2(chip);
            OPLL_DoRegWrite(chip);
            OPLL_DoIO(chip);
            chip[0].cycles = ((chip[0].cycles + unchecked((uint)(1))) % unchecked((uint)(18)));
        }
    }
    private void OPLL_Write(ArrayPointer<opll_t> chip, uint port, byte data)
    {
        unchecked
        {
            chip[0].write_data = data;
            if (((port & unchecked((uint)(1))) != 0)) {
            chip[0].write_d |= unchecked((byte)(1));
            } else {
            chip[0].write_a |= unchecked((byte)(1));
            }
        }
    }
}
