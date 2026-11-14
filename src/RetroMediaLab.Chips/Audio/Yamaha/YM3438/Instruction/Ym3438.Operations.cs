using System.Buffers.Binary;
using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Chips.Audio.Yamaha.YM3438.Instruction;

public sealed partial class Ym3438
{
    private void OPN2_DoIO(ArrayPointer<ym3438_t> chip)
    {
        unchecked
        {
            chip[0].write_a_en = (((unchecked((int)(chip[0].write_a)) & 3) == 1) ? (byte)1 : (byte)0);
            chip[0].write_d_en = (((unchecked((int)(chip[0].write_d)) & 3) == 1) ? (byte)1 : (byte)0);
            chip[0].write_a <<= 1;
            chip[0].write_d <<= 1;
            chip[0].busy = chip[0].write_busy;
            chip[0].write_busy_cnt += chip[0].write_busy;
            chip[0].write_busy = ((((chip[0].write_busy != 0) && (!((unchecked((int)(chip[0].write_busy_cnt)) >> 5) != 0))) || (chip[0].write_d_en != 0)) ? (byte)1 : (byte)0);
            chip[0].write_busy_cnt &= unchecked((byte)(31));
        }
    }
    private void OPN2_DoRegWrite(ArrayPointer<ym3438_t> chip)
    {
        unchecked
        {
            uint i = default;
            uint slot = (chip[0].cycles % unchecked((uint)(12)));
            uint address = default;
            uint channel = chip[0].channel;
            if ((chip[0].write_fm_data != 0)) {
            if ((op_offset[(int)(slot)] == unchecked((uint)((unchecked((int)(chip[0].address)) & 263))))) {
            if (((unchecked((int)(chip[0].address)) & 8) != 0)) {
            slot += unchecked((uint)(12));
            }
            address = unchecked((uint)((unchecked((int)(chip[0].address)) & 240)));
            switch (address) {
            case 48:
            chip[0].multi[(int)(slot)] = unchecked((byte)((unchecked((int)(chip[0].data)) & 15)));
            if ((!(chip[0].multi[(int)(slot)] != 0))) {
            chip[0].multi[(int)(slot)] = unchecked((byte)(1));
            } else {
            chip[0].multi[(int)(slot)] <<= 1;
            }
            chip[0].dt[(int)(slot)] = unchecked((byte)(((unchecked((int)(chip[0].data)) >> 4) & 7)));
            break;
            case 64:
            chip[0].tl[(int)(slot)] = unchecked((byte)((unchecked((int)(chip[0].data)) & 127)));
            break;
            case 80:
            chip[0].ar[(int)(slot)] = unchecked((byte)((unchecked((int)(chip[0].data)) & 31)));
            chip[0].ks[(int)(slot)] = unchecked((byte)(((unchecked((int)(chip[0].data)) >> 6) & 3)));
            break;
            case 96:
            chip[0].dr[(int)(slot)] = unchecked((byte)((unchecked((int)(chip[0].data)) & 31)));
            chip[0].am[(int)(slot)] = unchecked((byte)(((unchecked((int)(chip[0].data)) >> 7) & 1)));
            break;
            case 112:
            chip[0].sr[(int)(slot)] = unchecked((byte)((unchecked((int)(chip[0].data)) & 31)));
            break;
            case 128:
            chip[0].rr[(int)(slot)] = unchecked((byte)((unchecked((int)(chip[0].data)) & 15)));
            chip[0].sl[(int)(slot)] = unchecked((byte)(((unchecked((int)(chip[0].data)) >> 4) & 15)));
            chip[0].sl[(int)(slot)] |= unchecked((byte)(((unchecked((int)(chip[0].sl[(int)(slot)])) + 1) & 16)));
            break;
            case 144:
            chip[0].ssg_eg[(int)(slot)] = unchecked((byte)((unchecked((int)(chip[0].data)) & 15)));
            break;
            default:
            break;
            }
            }
            if ((ch_offset[(int)(channel)] == unchecked((uint)((unchecked((int)(chip[0].address)) & 259))))) {
            address = unchecked((uint)((unchecked((int)(chip[0].address)) & 252)));
            switch (address) {
            case 160:
            chip[0].fnum[(int)(channel)] = unchecked((ushort)(((unchecked((int)(chip[0].data)) & 255) | ((unchecked((int)(chip[0].reg_a4)) & 7) << 8))));
            chip[0].block[(int)(channel)] = unchecked((byte)(((unchecked((int)(chip[0].reg_a4)) >> 3) & 7)));
            chip[0].kcode[(int)(channel)] = unchecked((byte)((unchecked((uint)((unchecked((int)(chip[0].block[(int)(channel)])) << 2))) | fn_note[(int)((unchecked((int)(chip[0].fnum[(int)(channel)])) >> 7))])));
            break;
            case 164:
            chip[0].reg_a4 = unchecked((byte)((unchecked((int)(chip[0].data)) & 255)));
            break;
            case 168:
            chip[0].fnum_3ch[(int)(channel)] = unchecked((ushort)(((unchecked((int)(chip[0].data)) & 255) | ((unchecked((int)(chip[0].reg_ac)) & 7) << 8))));
            chip[0].block_3ch[(int)(channel)] = unchecked((byte)(((unchecked((int)(chip[0].reg_ac)) >> 3) & 7)));
            chip[0].kcode_3ch[(int)(channel)] = unchecked((byte)((unchecked((uint)((unchecked((int)(chip[0].block_3ch[(int)(channel)])) << 2))) | fn_note[(int)((unchecked((int)(chip[0].fnum_3ch[(int)(channel)])) >> 7))])));
            break;
            case 172:
            chip[0].reg_ac = unchecked((byte)((unchecked((int)(chip[0].data)) & 255)));
            break;
            case 176:
            chip[0].connect[(int)(channel)] = unchecked((byte)((unchecked((int)(chip[0].data)) & 7)));
            chip[0].fb[(int)(channel)] = unchecked((byte)(((unchecked((int)(chip[0].data)) >> 3) & 7)));
            break;
            case 180:
            chip[0].pms[(int)(channel)] = unchecked((byte)((unchecked((int)(chip[0].data)) & 7)));
            chip[0].ams[(int)(channel)] = unchecked((byte)(((unchecked((int)(chip[0].data)) >> 4) & 3)));
            chip[0].pan_l[(int)(channel)] = unchecked((byte)(((unchecked((int)(chip[0].data)) >> 7) & 1)));
            chip[0].pan_r[(int)(channel)] = unchecked((byte)(((unchecked((int)(chip[0].data)) >> 6) & 1)));
            break;
            default:
            break;
            }
            }
            }
            if (((chip[0].write_a_en != 0) || (chip[0].write_d_en != 0))) {
            if ((chip[0].write_a_en != 0)) {
            chip[0].write_fm_data = unchecked((byte)(0));
            }
            if (((chip[0].write_fm_address != 0) && (chip[0].write_d_en != 0))) {
            chip[0].write_fm_data = unchecked((byte)(1));
            }
            if ((chip[0].write_a_en != 0)) {
            if (((unchecked((int)(chip[0].write_data)) & 240) != 0)) {
            chip[0].address = chip[0].write_data;
            chip[0].write_fm_address = unchecked((byte)(1));
            } else {
            chip[0].write_fm_address = unchecked((byte)(0));
            }
            }
            if (((chip[0].write_d_en != 0) && ((unchecked((int)(chip[0].write_data)) & 256) == 0))) {
            switch (chip[0].write_fm_mode_a) {
            case 33:
            for (i = unchecked((uint)(0)); (i < unchecked((uint)(8))); i++) {
            chip[0].mode_test_21[(int)(i)] = unchecked((byte)(((unchecked((int)(chip[0].write_data)) >> unchecked((int)(i))) & 1)));
            }
            break;
            case 34:
            if ((((unchecked((int)(chip[0].write_data)) >> 3) & 1) != 0)) {
            chip[0].lfo_en = unchecked((byte)(127));
            } else {
            chip[0].lfo_en = unchecked((byte)(0));
            }
            chip[0].lfo_freq = unchecked((byte)((unchecked((int)(chip[0].write_data)) & 7)));
            break;
            case 36:
            chip[0].timer_a_reg &= unchecked((ushort)(3));
            chip[0].timer_a_reg |= unchecked((ushort)(((unchecked((int)(chip[0].write_data)) & 255) << 2)));
            break;
            case 37:
            chip[0].timer_a_reg &= unchecked((ushort)(1020));
            chip[0].timer_a_reg |= unchecked((ushort)((unchecked((int)(chip[0].write_data)) & 3)));
            break;
            case 38:
            chip[0].timer_b_reg = unchecked((ushort)((unchecked((int)(chip[0].write_data)) & 255)));
            break;
            case 39:
            chip[0].mode_ch3 = unchecked((byte)(((unchecked((int)(chip[0].write_data)) & 192) >> 6)));
            chip[0].mode_csm = ((unchecked((int)(chip[0].mode_ch3)) == 2) ? (byte)1 : (byte)0);
            chip[0].timer_a_load = unchecked((byte)((unchecked((int)(chip[0].write_data)) & 1)));
            chip[0].timer_a_enable = unchecked((byte)(((unchecked((int)(chip[0].write_data)) >> 2) & 1)));
            chip[0].timer_a_reset = unchecked((byte)(((unchecked((int)(chip[0].write_data)) >> 4) & 1)));
            chip[0].timer_b_load = unchecked((byte)(((unchecked((int)(chip[0].write_data)) >> 1) & 1)));
            chip[0].timer_b_enable = unchecked((byte)(((unchecked((int)(chip[0].write_data)) >> 3) & 1)));
            chip[0].timer_b_reset = unchecked((byte)(((unchecked((int)(chip[0].write_data)) >> 5) & 1)));
            break;
            case 40:
            for (i = unchecked((uint)(0)); (i < unchecked((uint)(4))); i++) {
            chip[0].mode_kon_operator[(int)(i)] = unchecked((byte)(((unchecked((int)(chip[0].write_data)) >> unchecked((int)((unchecked((uint)(4)) + i)))) & 1)));
            }
            if (((unchecked((int)(chip[0].write_data)) & 3) == 3)) {
            chip[0].mode_kon_channel = unchecked((byte)(255));
            } else {
            chip[0].mode_kon_channel = unchecked((byte)(((unchecked((int)(chip[0].write_data)) & 3) + (((unchecked((int)(chip[0].write_data)) >> 2) & 1) * 3))));
            }
            break;
            case 42:
            chip[0].dacdata &= unchecked((short)(1));
            chip[0].dacdata |= unchecked((short)(((unchecked((int)(chip[0].write_data)) ^ 128) << 1)));
            break;
            case 43:
            chip[0].dacen = unchecked((byte)((unchecked((int)(chip[0].write_data)) >> 7)));
            break;
            case 44:
            for (i = unchecked((uint)(0)); (i < unchecked((uint)(8))); i++) {
            chip[0].mode_test_2c[(int)(i)] = unchecked((byte)(((unchecked((int)(chip[0].write_data)) >> unchecked((int)(i))) & 1)));
            }
            chip[0].dacdata &= unchecked((short)(510));
            chip[0].dacdata |= unchecked((short)(chip[0].mode_test_2c[(int)(3)]));
            chip[0].eg_custom_timer = (((!(chip[0].mode_test_2c[(int)(7)] != 0)) && (chip[0].mode_test_2c[(int)(6)] != 0)) ? (byte)1 : (byte)0);
            break;
            default:
            break;
            }
            }
            if ((chip[0].write_a_en != 0)) {
            chip[0].write_fm_mode_a = unchecked((ushort)((unchecked((int)(chip[0].write_data)) & 511)));
            }
            }
            if ((chip[0].write_fm_data != 0)) {
            chip[0].data = unchecked((byte)((unchecked((int)(chip[0].write_data)) & 255)));
            }
        }
    }
    private void OPN2_PhaseCalcIncrement(ArrayPointer<ym3438_t> chip)
    {
        unchecked
        {
            uint chan = chip[0].channel;
            uint slot = chip[0].cycles;
            uint fnum = unchecked((uint)(chip[0].pg_fnum));
            uint fnum_h = (fnum >> 4);
            uint fm = default;
            uint basefreq = default;
            byte lfo = chip[0].lfo_pm;
            byte lfo_l = unchecked((byte)((unchecked((int)(lfo)) & 15)));
            byte pms = chip[0].pms[(int)(chan)];
            byte dt = chip[0].dt[(int)(slot)];
            byte dt_l = unchecked((byte)((unchecked((int)(dt)) & 3)));
            byte detune = unchecked((byte)(0));
            byte block = default;
            byte note = default;
            byte sum = default;
            byte sum_h = default;
            byte sum_l = default;
            byte kcode = chip[0].pg_kcode;
            fnum <<= 1;
            if (((unchecked((int)(lfo_l)) & 8) != 0)) {
            lfo_l ^= unchecked((byte)(15));
            }
            fm = ((fnum_h >> unchecked((int)(new ArrayPointer<uint>(pg_lfo_sh1, (int)(pms) * 8)[(int)(lfo_l)]))) + (fnum_h >> unchecked((int)(new ArrayPointer<uint>(pg_lfo_sh2, (int)(pms) * 8)[(int)(lfo_l)]))));
            if ((unchecked((int)(pms)) > 5)) {
            fm <<= (unchecked((int)(pms)) - 5);
            }
            fm >>= 2;
            if (((unchecked((int)(lfo)) & 16) != 0)) {
            fnum -= fm;
            } else {
            fnum += fm;
            }
            fnum &= unchecked((uint)(4095));
            basefreq = ((fnum << unchecked((int)(chip[0].pg_block))) >> 2);
            if ((dt_l != 0)) {
            if ((unchecked((int)(kcode)) > 28)) {
            kcode = unchecked((byte)(28));
            }
            block = unchecked((byte)((unchecked((int)(kcode)) >> 2)));
            note = unchecked((byte)((unchecked((int)(kcode)) & 3)));
            sum = unchecked((byte)(((unchecked((int)(block)) + 9) + (((unchecked((int)(dt_l)) == 3) ? (int)1 : (int)0) | (unchecked((int)(dt_l)) & 2)))));
            sum_h = unchecked((byte)((unchecked((int)(sum)) >> 1)));
            sum_l = unchecked((byte)((unchecked((int)(sum)) & 1)));
            detune = unchecked((byte)((pg_detune[(int)(((unchecked((int)(sum_l)) << 2) | unchecked((int)(note))))] >> (9 - unchecked((int)(sum_h))))));
            }
            if (((unchecked((int)(dt)) & 4) != 0)) {
            basefreq -= unchecked((uint)(detune));
            } else {
            basefreq += unchecked((uint)(detune));
            }
            basefreq &= unchecked((uint)(131071));
            chip[0].pg_inc[(int)(slot)] = ((basefreq * unchecked((uint)(chip[0].multi[(int)(slot)]))) >> 1);
            chip[0].pg_inc[(int)(slot)] &= unchecked((uint)(1048575));
        }
    }
    private void OPN2_PhaseGenerate(ArrayPointer<ym3438_t> chip)
    {
        unchecked
        {
            uint slot = default;
            slot = ((chip[0].cycles + unchecked((uint)(20))) % unchecked((uint)(24)));
            if ((chip[0].pg_reset[(int)(slot)] != 0)) {
            chip[0].pg_inc[(int)(slot)] = unchecked((uint)(0));
            }
            slot = ((chip[0].cycles + unchecked((uint)(19))) % unchecked((uint)(24)));
            if (((chip[0].pg_reset[(int)(slot)] != 0) || (chip[0].mode_test_21[(int)(3)] != 0))) {
            chip[0].pg_phase[(int)(slot)] = unchecked((uint)(0));
            }
            chip[0].pg_phase[(int)(slot)] += chip[0].pg_inc[(int)(slot)];
            chip[0].pg_phase[(int)(slot)] &= unchecked((uint)(1048575));
        }
    }
    private void OPN2_EnvelopeSSGEG(ArrayPointer<ym3438_t> chip)
    {
        unchecked
        {
            uint slot = chip[0].cycles;
            byte direction = unchecked((byte)(0));
            chip[0].eg_ssg_pgrst_latch[(int)(slot)] = unchecked((byte)(0));
            chip[0].eg_ssg_repeat_latch[(int)(slot)] = unchecked((byte)(0));
            chip[0].eg_ssg_hold_up_latch[(int)(slot)] = unchecked((byte)(0));
            if (((unchecked((int)(chip[0].ssg_eg[(int)(slot)])) & 8) != 0)) {
            direction = chip[0].eg_ssg_dir[(int)(slot)];
            if (((unchecked((int)(chip[0].eg_level[(int)(slot)])) & 512) != 0)) {
            if (((unchecked((int)(chip[0].ssg_eg[(int)(slot)])) & 3) == 0)) {
            chip[0].eg_ssg_pgrst_latch[(int)(slot)] = unchecked((byte)(1));
            }
            if (((unchecked((int)(chip[0].ssg_eg[(int)(slot)])) & 1) == 0)) {
            chip[0].eg_ssg_repeat_latch[(int)(slot)] = unchecked((byte)(1));
            }
            if (((unchecked((int)(chip[0].ssg_eg[(int)(slot)])) & 3) == 2)) {
            direction ^= unchecked((byte)(1));
            }
            if (((unchecked((int)(chip[0].ssg_eg[(int)(slot)])) & 3) == 3)) {
            direction = unchecked((byte)(1));
            }
            }
            if (((chip[0].eg_kon_latch[(int)(slot)] != 0) && (((unchecked((int)(chip[0].ssg_eg[(int)(slot)])) & 7) == 5) || ((unchecked((int)(chip[0].ssg_eg[(int)(slot)])) & 7) == 3)))) {
            chip[0].eg_ssg_hold_up_latch[(int)(slot)] = unchecked((byte)(1));
            }
            direction &= chip[0].eg_kon[(int)(slot)];
            }
            chip[0].eg_ssg_dir[(int)(slot)] = direction;
            chip[0].eg_ssg_enable[(int)(slot)] = unchecked((byte)(((unchecked((int)(chip[0].ssg_eg[(int)(slot)])) >> 3) & 1)));
            chip[0].eg_ssg_inv[(int)(slot)] = unchecked((byte)(((unchecked((int)(chip[0].eg_ssg_dir[(int)(slot)])) ^ (((unchecked((int)(chip[0].ssg_eg[(int)(slot)])) >> 2) & 1) & ((unchecked((int)(chip[0].ssg_eg[(int)(slot)])) >> 3) & 1))) & unchecked((int)(chip[0].eg_kon[(int)(slot)])))));
        }
    }
    private void OPN2_EnvelopeADSR(ArrayPointer<ym3438_t> chip)
    {
        unchecked
        {
            uint slot = ((chip[0].cycles + unchecked((uint)(22))) % unchecked((uint)(24)));
            byte nkon = chip[0].eg_kon_latch[(int)(slot)];
            byte okon = chip[0].eg_kon[(int)(slot)];
            byte kon_event = default;
            byte koff_event = default;
            byte eg_off = default;
            short level = default;
            short nextlevel = unchecked((short)(0));
            short ssg_level = default;
            byte nextstate = chip[0].eg_state[(int)(slot)];
            short inc = unchecked((short)(0));
            chip[0].eg_read[(int)(0)] = unchecked((uint)(chip[0].eg_read_inc));
            chip[0].eg_read_inc = ((unchecked((int)(chip[0].eg_inc)) > 0) ? (byte)1 : (byte)0);
            chip[0].pg_reset[(int)(slot)] = ((((nkon != 0) && (!(okon != 0))) || (chip[0].eg_ssg_pgrst_latch[(int)(slot)] != 0)) ? (byte)1 : (byte)0);
            kon_event = ((((nkon != 0) && (!(okon != 0))) || ((okon != 0) && (chip[0].eg_ssg_repeat_latch[(int)(slot)] != 0))) ? (byte)1 : (byte)0);
            koff_event = (((okon != 0) && (!(nkon != 0))) ? (byte)1 : (byte)0);
            ssg_level = (level = unchecked((short)(chip[0].eg_level[(int)(slot)])));
            if ((chip[0].eg_ssg_inv[(int)(slot)] != 0)) {
            ssg_level = unchecked((short)((512 - unchecked((int)(level)))));
            ssg_level &= unchecked((short)(1023));
            }
            if ((koff_event != 0)) {
            level = ssg_level;
            }
            if ((chip[0].eg_ssg_enable[(int)(slot)] != 0)) {
            eg_off = unchecked((byte)((unchecked((int)(level)) >> 9)));
            } else {
            eg_off = (((unchecked((int)(level)) & 1008) == 1008) ? (byte)1 : (byte)0);
            }
            nextlevel = level;
            if ((kon_event != 0)) {
            nextstate = unchecked((byte)(0));
            if ((chip[0].eg_ratemax != 0)) {
            nextlevel = unchecked((short)(0));
            } else if (((((unchecked((int)(chip[0].eg_state[(int)(slot)])) == 0) && (unchecked((int)(level)) != 0)) && (chip[0].eg_inc != 0)) && (nkon != 0))) {
            inc = unchecked((short)((((~unchecked((int)(level))) << unchecked((int)(chip[0].eg_inc))) >> 5)));
            }
            } else {
            switch (chip[0].eg_state[(int)(slot)]) {
            case 0:
            if ((unchecked((int)(level)) == 0)) {
            nextstate = unchecked((byte)(1));
            } else if ((((chip[0].eg_inc != 0) && (!(chip[0].eg_ratemax != 0))) && (nkon != 0))) {
            inc = unchecked((short)((((~unchecked((int)(level))) << unchecked((int)(chip[0].eg_inc))) >> 5)));
            }
            break;
            case 1:
            if (((unchecked((int)(level)) >> 4) == (unchecked((int)(chip[0].eg_sl[(int)(1)])) << 1))) {
            nextstate = unchecked((byte)(2));
            } else if (((!(eg_off != 0)) && (chip[0].eg_inc != 0))) {
            inc = unchecked((short)((1 << (unchecked((int)(chip[0].eg_inc)) - 1))));
            if ((chip[0].eg_ssg_enable[(int)(slot)] != 0)) {
            inc <<= 2;
            }
            }
            break;
            case 2:
            case 3:
            if (((!(eg_off != 0)) && (chip[0].eg_inc != 0))) {
            inc = unchecked((short)((1 << (unchecked((int)(chip[0].eg_inc)) - 1))));
            if ((chip[0].eg_ssg_enable[(int)(slot)] != 0)) {
            inc <<= 2;
            }
            }
            break;
            default:
            break;
            }
            if ((!(nkon != 0))) {
            nextstate = unchecked((byte)(3));
            }
            }
            if ((chip[0].eg_kon_csm[(int)(slot)] != 0)) {
            nextlevel |= unchecked((short)((unchecked((int)(chip[0].eg_tl[(int)(1)])) << 3)));
            }
            if (((((!(kon_event != 0)) && (!(chip[0].eg_ssg_hold_up_latch[(int)(slot)] != 0))) && (unchecked((int)(chip[0].eg_state[(int)(slot)])) != 0)) && (eg_off != 0))) {
            nextstate = unchecked((byte)(3));
            nextlevel = unchecked((short)(1023));
            }
            nextlevel += inc;
            chip[0].eg_kon[(int)(slot)] = chip[0].eg_kon_latch[(int)(slot)];
            chip[0].eg_level[(int)(slot)] = unchecked((ushort)((unchecked((int)(unchecked((ushort)(nextlevel)))) & 1023)));
            chip[0].eg_state[(int)(slot)] = nextstate;
        }
    }
    private void OPN2_EnvelopePrepare(ArrayPointer<ym3438_t> chip)
    {
        unchecked
        {
            byte rate = default;
            byte sum = default;
            byte inc = unchecked((byte)(0));
            uint slot = chip[0].cycles;
            byte rate_sel = default;
            rate = unchecked((byte)(((unchecked((int)(chip[0].eg_rate)) << 1) + unchecked((int)(chip[0].eg_ksv)))));
            if ((unchecked((int)(rate)) > 63)) {
            rate = unchecked((byte)(63));
            }
            sum = unchecked((byte)((((unchecked((int)(rate)) >> 2) + unchecked((int)(chip[0].eg_shift_lock))) & 15)));
            if (((unchecked((int)(chip[0].eg_rate)) != 0) && (unchecked((int)(chip[0].eg_quotient)) == 2))) {
            if ((unchecked((int)(rate)) < 48)) {
            switch (sum) {
            case 12:
            inc = unchecked((byte)(1));
            break;
            case 13:
            inc = unchecked((byte)(((unchecked((int)(rate)) >> 1) & 1)));
            break;
            case 14:
            inc = unchecked((byte)((unchecked((int)(rate)) & 1)));
            break;
            default:
            break;
            }
            } else {
            inc = unchecked((byte)(((new ArrayPointer<uint>(eg_stephi, (int)((unchecked((int)(rate)) & 3)) * 4)[(int)(chip[0].eg_timer_low_lock)] + unchecked((uint)((unchecked((int)(rate)) >> 2)))) - unchecked((uint)(11)))));
            if ((unchecked((int)(inc)) > 4)) {
            inc = unchecked((byte)(4));
            }
            }
            }
            chip[0].eg_inc = inc;
            chip[0].eg_ratemax = (((unchecked((int)(rate)) >> 1) == 31) ? (byte)1 : (byte)0);
            rate_sel = chip[0].eg_state[(int)(slot)];
            if ((((chip[0].eg_kon[(int)(slot)] != 0) && (chip[0].eg_ssg_repeat_latch[(int)(slot)] != 0)) || ((!(chip[0].eg_kon[(int)(slot)] != 0)) && (chip[0].eg_kon_latch[(int)(slot)] != 0)))) {
            rate_sel = unchecked((byte)(0));
            }
            switch (rate_sel) {
            case 0:
            chip[0].eg_rate = chip[0].ar[(int)(slot)];
            break;
            case 1:
            chip[0].eg_rate = chip[0].dr[(int)(slot)];
            break;
            case 2:
            chip[0].eg_rate = chip[0].sr[(int)(slot)];
            break;
            case 3:
            chip[0].eg_rate = unchecked((byte)(((unchecked((int)(chip[0].rr[(int)(slot)])) << 1) | 1)));
            break;
            default:
            break;
            }
            chip[0].eg_ksv = unchecked((byte)((unchecked((int)(chip[0].pg_kcode)) >> (unchecked((int)(chip[0].ks[(int)(slot)])) ^ 3))));
            if ((chip[0].am[(int)(slot)] != 0)) {
            chip[0].eg_lfo_am = unchecked((byte)((unchecked((int)(chip[0].lfo_am)) >> unchecked((int)(eg_am_shift[(int)(chip[0].ams[(int)(chip[0].channel)])])))));
            } else {
            chip[0].eg_lfo_am = unchecked((byte)(0));
            }
            chip[0].eg_tl[(int)(1)] = chip[0].eg_tl[(int)(0)];
            chip[0].eg_tl[(int)(0)] = chip[0].tl[(int)(slot)];
            chip[0].eg_sl[(int)(1)] = chip[0].eg_sl[(int)(0)];
            chip[0].eg_sl[(int)(0)] = chip[0].sl[(int)(slot)];
        }
    }
    private void OPN2_EnvelopeGenerate(ArrayPointer<ym3438_t> chip)
    {
        unchecked
        {
            uint slot = ((chip[0].cycles + unchecked((uint)(23))) % unchecked((uint)(24)));
            ushort level = default;
            level = chip[0].eg_level[(int)(slot)];
            if ((chip[0].eg_ssg_inv[(int)(slot)] != 0)) {
            level = unchecked((ushort)((512 - unchecked((int)(level)))));
            }
            if ((chip[0].mode_test_21[(int)(5)] != 0)) {
            level = unchecked((ushort)(0));
            }
            level &= unchecked((ushort)(1023));
            level += unchecked((ushort)(chip[0].eg_lfo_am));
            if ((!((chip[0].mode_csm != 0) && (chip[0].channel == unchecked((uint)((2 + 1))))))) {
            level += unchecked((ushort)((unchecked((int)(chip[0].eg_tl[(int)(0)])) << 3)));
            }
            if ((unchecked((int)(level)) > 1023)) {
            level = unchecked((ushort)(1023));
            }
            chip[0].eg_out[(int)(slot)] = level;
        }
    }
    private void OPN2_UpdateLFO(ArrayPointer<ym3438_t> chip)
    {
        unchecked
        {
            if (((unchecked((uint)(chip[0].lfo_quotient)) & lfo_cycles[(int)(chip[0].lfo_freq)]) == lfo_cycles[(int)(chip[0].lfo_freq)])) {
            chip[0].lfo_quotient = unchecked((byte)(0));
            chip[0].lfo_cnt++;
            } else {
            chip[0].lfo_quotient += chip[0].lfo_inc;
            }
            chip[0].lfo_cnt &= chip[0].lfo_en;
        }
    }
    private void OPN2_FMPrepare(ArrayPointer<ym3438_t> chip)
    {
        unchecked
        {
            uint slot = ((chip[0].cycles + unchecked((uint)(6))) % unchecked((uint)(24)));
            uint channel = chip[0].channel;
            short mod = default;
            short mod1 = default;
            short mod2 = default;
            uint op = (slot / unchecked((uint)(6)));
            byte connect = chip[0].connect[(int)(channel)];
            uint prevslot = ((chip[0].cycles + unchecked((uint)(18))) % unchecked((uint)(24)));
            mod1 = (mod2 = unchecked((short)(0)));
            if ((fm_algorithm[(int)((((op * unchecked((uint)(48))) + unchecked((uint)((0 * 8)))) + unchecked((uint)(connect))))] != 0)) {
            mod2 |= new ArrayPointer<short>(chip[0].fm_op1, (int)(channel) * 2)[(int)(0)];
            }
            if ((fm_algorithm[(int)((((op * unchecked((uint)(48))) + unchecked((uint)((1 * 8)))) + unchecked((uint)(connect))))] != 0)) {
            mod1 |= new ArrayPointer<short>(chip[0].fm_op1, (int)(channel) * 2)[(int)(1)];
            }
            if ((fm_algorithm[(int)((((op * unchecked((uint)(48))) + unchecked((uint)((2 * 8)))) + unchecked((uint)(connect))))] != 0)) {
            mod1 |= chip[0].fm_op2[(int)(channel)];
            }
            if ((fm_algorithm[(int)((((op * unchecked((uint)(48))) + unchecked((uint)((3 * 8)))) + unchecked((uint)(connect))))] != 0)) {
            mod2 |= chip[0].fm_out[(int)(prevslot)];
            }
            if ((fm_algorithm[(int)((((op * unchecked((uint)(48))) + unchecked((uint)((4 * 8)))) + unchecked((uint)(connect))))] != 0)) {
            mod1 |= chip[0].fm_out[(int)(prevslot)];
            }
            mod = unchecked((short)((unchecked((int)(mod1)) + unchecked((int)(mod2)))));
            if ((op == unchecked((uint)(0)))) {
            mod = unchecked((short)((unchecked((int)(mod)) >> (10 - unchecked((int)(chip[0].fb[(int)(channel)]))))));
            if ((!(chip[0].fb[(int)(channel)] != 0))) {
            mod = unchecked((short)(0));
            }
            } else {
            mod >>= 1;
            }
            chip[0].fm_mod[(int)(slot)] = unchecked((ushort)(mod));
            slot = ((chip[0].cycles + unchecked((uint)(18))) % unchecked((uint)(24)));
            if (((slot / unchecked((uint)(6))) == unchecked((uint)(0)))) {
            new ArrayPointer<short>(chip[0].fm_op1, (int)(channel) * 2)[(int)(1)] = new ArrayPointer<short>(chip[0].fm_op1, (int)(channel) * 2)[(int)(0)];
            new ArrayPointer<short>(chip[0].fm_op1, (int)(channel) * 2)[(int)(0)] = chip[0].fm_out[(int)(slot)];
            }
            if (((slot / unchecked((uint)(6))) == unchecked((uint)(2)))) {
            chip[0].fm_op2[(int)(channel)] = chip[0].fm_out[(int)(slot)];
            }
        }
    }
    private void OPN2_ChGenerate(ArrayPointer<ym3438_t> chip)
    {
        unchecked
        {
            uint slot = ((chip[0].cycles + unchecked((uint)(18))) % unchecked((uint)(24)));
            uint channel = chip[0].channel;
            uint op = (slot / unchecked((uint)(6)));
            uint test_dac = unchecked((uint)(chip[0].mode_test_2c[(int)(5)]));
            short acc = chip[0].ch_acc[(int)(channel)];
            short add = unchecked((short)(test_dac));
            short sum = unchecked((short)(0));
            if (((op == unchecked((uint)(0))) && (!(test_dac != 0)))) {
            acc = unchecked((short)(0));
            }
            if (((fm_algorithm[(int)((((op * unchecked((uint)(48))) + unchecked((uint)((5 * 8)))) + unchecked((uint)(chip[0].connect[(int)(channel)]))))] != 0) && (!(test_dac != 0)))) {
            add += unchecked((short)((unchecked((int)(chip[0].fm_out[(int)(slot)])) >> 5)));
            }
            sum = unchecked((short)((unchecked((int)(acc)) + unchecked((int)(add)))));
            if ((unchecked((int)(sum)) > 255)) {
            sum = unchecked((short)(255));
            } else if ((unchecked((int)(sum)) < (-256))) {
            sum = unchecked((short)((-256)));
            }
            if (((op == unchecked((uint)(0))) || (test_dac != 0))) {
            chip[0].ch_out[(int)(channel)] = chip[0].ch_acc[(int)(channel)];
            }
            chip[0].ch_acc[(int)(channel)] = sum;
        }
    }
    private void OPN2_ChOutput(ArrayPointer<ym3438_t> chip)
    {
        unchecked
        {
            uint cycles = chip[0].cycles;
            uint slot = chip[0].cycles;
            uint channel = chip[0].channel;
            uint test_dac = unchecked((uint)(chip[0].mode_test_2c[(int)(5)]));
            short @out = default;
            short sign = default;
            uint out_en = default;
            chip[0].ch_read = chip[0].ch_lock;
            if ((slot < unchecked((uint)(12)))) {
            channel++;
            }
            if (((cycles & unchecked((uint)(3))) == unchecked((uint)(0)))) {
            if ((!(test_dac != 0))) {
            chip[0].ch_lock = chip[0].ch_out[(int)(channel)];
            }
            chip[0].ch_lock_l = chip[0].pan_l[(int)(channel)];
            chip[0].ch_lock_r = chip[0].pan_r[(int)(channel)];
            }
            if (((((cycles >> 2) == unchecked((uint)(1))) && (chip[0].dacen != 0)) || (test_dac != 0))) {
            @out = chip[0].dacdata;
            @out = unchecked((short)(((unchecked((uint)(@out)) & ((1u << 8) - 1u)) - (unchecked((uint)(@out)) & (1u << 8)))));
            } else {
            @out = chip[0].ch_lock;
            }
            chip[0].mol = unchecked((short)(0));
            chip[0].mor = unchecked((short)(0));
            if (((chip_type & unchecked((uint)(1))) != 0)) {
            out_en = ((((cycles & unchecked((uint)(3))) == unchecked((uint)(3))) || (test_dac != 0)) ? (uint)1 : (uint)0);
            sign = unchecked((short)((unchecked((int)(@out)) >> 8)));
            if ((unchecked((int)(@out)) >= 0)) {
            @out++;
            sign++;
            }
            if (((chip[0].ch_lock_l != 0) && (out_en != 0))) {
            chip[0].mol = @out;
            } else {
            chip[0].mol = sign;
            }
            if (((chip[0].ch_lock_r != 0) && (out_en != 0))) {
            chip[0].mor = @out;
            } else {
            chip[0].mor = sign;
            }
            chip[0].mol *= unchecked((short)(3));
            chip[0].mor *= unchecked((short)(3));
            } else {
            out_en = ((((cycles & unchecked((uint)(3))) != unchecked((uint)(0))) || (test_dac != 0)) ? (uint)1 : (uint)0);
            if (((chip[0].ch_lock_l != 0) && (out_en != 0))) {
            chip[0].mol = @out;
            }
            if (((chip[0].ch_lock_r != 0) && (out_en != 0))) {
            chip[0].mor = @out;
            }
            }
        }
    }
    private void OPN2_FMGenerate(ArrayPointer<ym3438_t> chip)
    {
        unchecked
        {
            uint slot = ((chip[0].cycles + unchecked((uint)(19))) % unchecked((uint)(24)));
            ushort phase = unchecked((ushort)(((unchecked((uint)(chip[0].fm_mod[(int)(slot)])) + (chip[0].pg_phase[(int)(slot)] >> 10)) & unchecked((uint)(1023)))));
            ushort quarter = default;
            ushort level = default;
            short output = default;
            if (((unchecked((int)(phase)) & 256) != 0)) {
            quarter = unchecked((ushort)(((unchecked((int)(phase)) ^ 255) & 255)));
            } else {
            quarter = unchecked((ushort)((unchecked((int)(phase)) & 255)));
            }
            level = logsinrom[(int)(quarter)];
            level += unchecked((ushort)((unchecked((int)(chip[0].eg_out[(int)(slot)])) << 2)));
            if ((unchecked((int)(level)) > 8191)) {
            level = unchecked((ushort)(8191));
            }
            output = unchecked((short)((((unchecked((int)(exprom[(int)(((unchecked((int)(level)) & 255) ^ 255))])) | 1024) << 2) >> (unchecked((int)(level)) >> 8))));
            if (((unchecked((int)(phase)) & 512) != 0)) {
            output = unchecked((short)((((~unchecked((int)(output))) ^ (unchecked((int)(chip[0].mode_test_21[(int)(4)])) << 13)) + 1)));
            } else {
            output = unchecked((short)((unchecked((int)(output)) ^ (unchecked((int)(chip[0].mode_test_21[(int)(4)])) << 13))));
            }
            output = unchecked((short)(((unchecked((uint)(output)) & ((1u << 13) - 1u)) - (unchecked((uint)(output)) & (1u << 13)))));
            chip[0].fm_out[(int)(slot)] = output;
        }
    }
    private void OPN2_DoTimerA(ArrayPointer<ym3438_t> chip)
    {
        unchecked
        {
            ushort time = default;
            byte load = default;
            load = chip[0].timer_a_overflow;
            if ((chip[0].cycles == unchecked((uint)(2)))) {
            load |= (((!(chip[0].timer_a_load_lock != 0)) && (chip[0].timer_a_load != 0)) ? (byte)1 : (byte)0);
            chip[0].timer_a_load_lock = chip[0].timer_a_load;
            if ((chip[0].mode_csm != 0)) {
            chip[0].mode_kon_csm = load;
            } else {
            chip[0].mode_kon_csm = unchecked((byte)(0));
            }
            }
            if ((chip[0].timer_a_load_latch != 0)) {
            time = chip[0].timer_a_reg;
            } else {
            time = chip[0].timer_a_cnt;
            }
            chip[0].timer_a_load_latch = load;
            if ((((chip[0].cycles == unchecked((uint)(1))) && (chip[0].timer_a_load_lock != 0)) || (chip[0].mode_test_21[(int)(2)] != 0))) {
            time++;
            }
            if ((chip[0].timer_a_reset != 0)) {
            chip[0].timer_a_reset = unchecked((byte)(0));
            chip[0].timer_a_overflow_flag = unchecked((byte)(0));
            } else {
            chip[0].timer_a_overflow_flag |= unchecked((byte)((unchecked((int)(chip[0].timer_a_overflow)) & unchecked((int)(chip[0].timer_a_enable)))));
            }
            chip[0].timer_a_overflow = unchecked((byte)((unchecked((int)(time)) >> 10)));
            chip[0].timer_a_cnt = unchecked((ushort)((unchecked((int)(time)) & 1023)));
        }
    }
    private void OPN2_DoTimerB(ArrayPointer<ym3438_t> chip)
    {
        unchecked
        {
            ushort time = default;
            byte load = default;
            load = chip[0].timer_b_overflow;
            if ((chip[0].cycles == unchecked((uint)(2)))) {
            load |= (((!(chip[0].timer_b_load_lock != 0)) && (chip[0].timer_b_load != 0)) ? (byte)1 : (byte)0);
            chip[0].timer_b_load_lock = chip[0].timer_b_load;
            }
            if ((chip[0].timer_b_load_latch != 0)) {
            time = chip[0].timer_b_reg;
            } else {
            time = chip[0].timer_b_cnt;
            }
            chip[0].timer_b_load_latch = load;
            if ((chip[0].cycles == unchecked((uint)(1)))) {
            chip[0].timer_b_subcnt++;
            }
            if ((((unchecked((int)(chip[0].timer_b_subcnt)) == 16) && (chip[0].timer_b_load_lock != 0)) || (chip[0].mode_test_21[(int)(2)] != 0))) {
            time++;
            }
            chip[0].timer_b_subcnt &= unchecked((byte)(15));
            if ((chip[0].timer_b_reset != 0)) {
            chip[0].timer_b_reset = unchecked((byte)(0));
            chip[0].timer_b_overflow_flag = unchecked((byte)(0));
            } else {
            chip[0].timer_b_overflow_flag |= unchecked((byte)((unchecked((int)(chip[0].timer_b_overflow)) & unchecked((int)(chip[0].timer_b_enable)))));
            }
            chip[0].timer_b_overflow = unchecked((byte)((unchecked((int)(time)) >> 8)));
            chip[0].timer_b_cnt = unchecked((ushort)((unchecked((int)(time)) & 255)));
        }
    }
    private void OPN2_KeyOn(ArrayPointer<ym3438_t> chip)
    {
        unchecked
        {
            uint slot = chip[0].cycles;
            uint chan = chip[0].channel;
            chip[0].eg_kon_latch[(int)(slot)] = chip[0].mode_kon[(int)(slot)];
            chip[0].eg_kon_csm[(int)(slot)] = unchecked((byte)(0));
            if (((chip[0].channel == unchecked((uint)(2))) && (chip[0].mode_kon_csm != 0))) {
            chip[0].eg_kon_latch[(int)(slot)] = unchecked((byte)(1));
            chip[0].eg_kon_csm[(int)(slot)] = unchecked((byte)(1));
            }
            if ((chip[0].cycles == unchecked((uint)(chip[0].mode_kon_channel)))) {
            chip[0].mode_kon[(int)(chan)] = chip[0].mode_kon_operator[(int)(0)];
            chip[0].mode_kon[(int)((chan + unchecked((uint)(12))))] = chip[0].mode_kon_operator[(int)(1)];
            chip[0].mode_kon[(int)((chan + unchecked((uint)(6))))] = chip[0].mode_kon_operator[(int)(2)];
            chip[0].mode_kon[(int)((chan + unchecked((uint)(18))))] = chip[0].mode_kon_operator[(int)(3)];
            }
        }
    }
    private void OPN2_Reset(ArrayPointer<ym3438_t> chip)
    {
        unchecked
        {
            uint i = default;
            ClearState(chip);
            for (i = unchecked((uint)(0)); (i < unchecked((uint)(24))); i++) {
            chip[0].eg_out[(int)(i)] = unchecked((ushort)(1023));
            chip[0].eg_level[(int)(i)] = unchecked((ushort)(1023));
            chip[0].eg_state[(int)(i)] = unchecked((byte)(3));
            chip[0].multi[(int)(i)] = unchecked((byte)(1));
            }
            for (i = unchecked((uint)(0)); (i < unchecked((uint)(6))); i++) {
            chip[0].pan_l[(int)(i)] = unchecked((byte)(1));
            chip[0].pan_r[(int)(i)] = unchecked((byte)(1));
            }
        }
    }
    private void OPN2_Restore(ArrayPointer<ym3438_t> chip)
    {
        unchecked
        {
            uint i = default;
            chip[0].cycles %= unchecked((uint)(24));
            chip[0].channel = (chip[0].cycles % unchecked((uint)(6)));
            chip[0].eg_timer_low_lock &= unchecked((byte)(3));
            chip[0].lfo_freq &= unchecked((byte)(7));
            chip[0].mode_test_21[(int)(0)] &= unchecked((byte)(1));
            for (i = unchecked((uint)(0)); (i < unchecked((uint)(6))); i++) {
            chip[0].connect[(int)(i)] &= unchecked((byte)(7));
            chip[0].ams[(int)(i)] &= unchecked((byte)(3));
            chip[0].pms[(int)(i)] &= unchecked((byte)(7));
            }
        }
    }
    private void OPN2_SetChipType(uint type)
    {
        unchecked
        {
            chip_type = type;
        }
    }
    private void OPN2_Clock(ArrayPointer<ym3438_t> chip, ArrayPointer<short> buffer)
    {
        unchecked
        {
            uint slot = chip[0].cycles;
            chip[0].lfo_inc = chip[0].mode_test_21[(int)(1)];
            chip[0].pg_read >>= 1;
            chip[0].eg_read[(int)(1)] >>= 1;
            chip[0].eg_cycle++;
            if (((chip[0].cycles == unchecked((uint)(1))) && (unchecked((int)(chip[0].eg_quotient)) == 2))) {
            if ((chip[0].eg_cycle_stop != 0)) {
            chip[0].eg_shift_lock = unchecked((byte)(0));
            } else {
            chip[0].eg_shift_lock = unchecked((byte)((unchecked((int)(chip[0].eg_shift)) + 1)));
            }
            chip[0].eg_timer_low_lock = unchecked((byte)((unchecked((int)(chip[0].eg_timer)) & 3)));
            }
            switch (chip[0].cycles) {
            case 0:
            chip[0].lfo_pm = unchecked((byte)((unchecked((int)(chip[0].lfo_cnt)) >> 2)));
            if (((unchecked((int)(chip[0].lfo_cnt)) & 64) != 0)) {
            chip[0].lfo_am = unchecked((byte)((unchecked((int)(chip[0].lfo_cnt)) & 63)));
            } else {
            chip[0].lfo_am = unchecked((byte)((unchecked((int)(chip[0].lfo_cnt)) ^ 63)));
            }
            chip[0].lfo_am <<= 1;
            break;
            case 1:
            chip[0].eg_quotient++;
            chip[0].eg_quotient %= unchecked((ushort)(3));
            chip[0].eg_cycle = unchecked((byte)(0));
            chip[0].eg_cycle_stop = unchecked((byte)(1));
            chip[0].eg_shift = unchecked((byte)(0));
            chip[0].eg_timer_inc |= unchecked((byte)((unchecked((int)(chip[0].eg_quotient)) >> 1)));
            chip[0].eg_timer = unchecked((ushort)((unchecked((int)(chip[0].eg_timer)) + unchecked((int)(chip[0].eg_timer_inc)))));
            chip[0].eg_timer_inc = unchecked((byte)((unchecked((int)(chip[0].eg_timer)) >> 12)));
            chip[0].eg_timer &= unchecked((ushort)(4095));
            break;
            case 2:
            chip[0].pg_read = (chip[0].pg_phase[(int)(21)] & unchecked((uint)(1023)));
            chip[0].eg_read[(int)(1)] = unchecked((uint)(chip[0].eg_out[(int)(0)]));
            break;
            case 13:
            chip[0].eg_cycle = unchecked((byte)(0));
            chip[0].eg_cycle_stop = unchecked((byte)(1));
            chip[0].eg_shift = unchecked((byte)(0));
            chip[0].eg_timer = unchecked((ushort)((unchecked((int)(chip[0].eg_timer)) + unchecked((int)(chip[0].eg_timer_inc)))));
            chip[0].eg_timer_inc = unchecked((byte)((unchecked((int)(chip[0].eg_timer)) >> 12)));
            chip[0].eg_timer &= unchecked((ushort)(4095));
            break;
            case 23:
            chip[0].lfo_inc |= unchecked((byte)(1));
            break;
            }
            chip[0].eg_timer &= unchecked((ushort)((~(unchecked((int)(chip[0].mode_test_21[(int)(5)])) << unchecked((int)(chip[0].eg_cycle))))));
            if (((((unchecked((int)(chip[0].eg_timer)) >> unchecked((int)(chip[0].eg_cycle))) | (unchecked((int)(chip[0].pin_test_in)) & unchecked((int)(chip[0].eg_custom_timer)))) & unchecked((int)(chip[0].eg_cycle_stop))) != 0)) {
            chip[0].eg_shift = chip[0].eg_cycle;
            chip[0].eg_cycle_stop = unchecked((byte)(0));
            }
            OPN2_DoIO(chip);
            OPN2_DoTimerA(chip);
            OPN2_DoTimerB(chip);
            OPN2_KeyOn(chip);
            OPN2_ChOutput(chip);
            OPN2_ChGenerate(chip);
            OPN2_FMPrepare(chip);
            OPN2_FMGenerate(chip);
            OPN2_PhaseGenerate(chip);
            OPN2_PhaseCalcIncrement(chip);
            OPN2_EnvelopeADSR(chip);
            OPN2_EnvelopeGenerate(chip);
            OPN2_EnvelopeSSGEG(chip);
            OPN2_EnvelopePrepare(chip);
            if ((chip[0].mode_ch3 != 0)) {
            switch (slot) {
            case 1:
            chip[0].pg_fnum = chip[0].fnum_3ch[(int)(1)];
            chip[0].pg_block = chip[0].block_3ch[(int)(1)];
            chip[0].pg_kcode = chip[0].kcode_3ch[(int)(1)];
            break;
            case 7:
            chip[0].pg_fnum = chip[0].fnum_3ch[(int)(0)];
            chip[0].pg_block = chip[0].block_3ch[(int)(0)];
            chip[0].pg_kcode = chip[0].kcode_3ch[(int)(0)];
            break;
            case 13:
            chip[0].pg_fnum = chip[0].fnum_3ch[(int)(2)];
            chip[0].pg_block = chip[0].block_3ch[(int)(2)];
            chip[0].pg_kcode = chip[0].kcode_3ch[(int)(2)];
            break;
            case 19:
            default:
            chip[0].pg_fnum = chip[0].fnum[(int)(((chip[0].channel + unchecked((uint)(1))) % unchecked((uint)(6))))];
            chip[0].pg_block = chip[0].block[(int)(((chip[0].channel + unchecked((uint)(1))) % unchecked((uint)(6))))];
            chip[0].pg_kcode = chip[0].kcode[(int)(((chip[0].channel + unchecked((uint)(1))) % unchecked((uint)(6))))];
            break;
            }
            } else {
            chip[0].pg_fnum = chip[0].fnum[(int)(((chip[0].channel + unchecked((uint)(1))) % unchecked((uint)(6))))];
            chip[0].pg_block = chip[0].block[(int)(((chip[0].channel + unchecked((uint)(1))) % unchecked((uint)(6))))];
            chip[0].pg_kcode = chip[0].kcode[(int)(((chip[0].channel + unchecked((uint)(1))) % unchecked((uint)(6))))];
            }
            OPN2_UpdateLFO(chip);
            OPN2_DoRegWrite(chip);
            chip[0].cycles = ((chip[0].cycles + unchecked((uint)(1))) % unchecked((uint)(24)));
            chip[0].channel = (chip[0].cycles % unchecked((uint)(6)));
            buffer[(int)(0)] = chip[0].mol;
            buffer[(int)(1)] = chip[0].mor;
            if ((chip[0].status_time != 0)) chip[0].status_time--;
        }
    }
    private void OPN2_Write(ArrayPointer<ym3438_t> chip, uint port, byte data)
    {
        unchecked
        {
            port &= unchecked((uint)(3));
            chip[0].write_data = unchecked((ushort)((((port << 7) & unchecked((uint)(256))) | unchecked((uint)(data)))));
            if (((port & unchecked((uint)(1))) != 0)) {
            chip[0].write_d |= unchecked((byte)(1));
            } else {
            chip[0].write_a |= unchecked((byte)(1));
            }
        }
    }
    private void OPN2_SetTestPin(ArrayPointer<ym3438_t> chip, uint value)
    {
        unchecked
        {
            chip[0].pin_test_in = unchecked((byte)((value & unchecked((uint)(1)))));
        }
    }
    private uint OPN2_ReadTestPin(ArrayPointer<ym3438_t> chip)
    {
        unchecked
        {
            if ((!(chip[0].mode_test_2c[(int)(7)] != 0))) {
            return unchecked((uint)(0));
            }
            return ((chip[0].cycles == unchecked((uint)(23))) ? (uint)1 : (uint)0);
        }
    }
    private uint OPN2_ReadIRQPin(ArrayPointer<ym3438_t> chip)
    {
        unchecked
        {
            return unchecked((uint)((unchecked((int)(chip[0].timer_a_overflow_flag)) | unchecked((int)(chip[0].timer_b_overflow_flag)))));
        }
    }
    private byte OPN2_Read(ArrayPointer<ym3438_t> chip, uint port)
    {
        unchecked
        {
            if ((((port & unchecked((uint)(3))) == unchecked((uint)(0))) || ((chip_type & unchecked((uint)(2))) != 0))) {
            if ((chip[0].mode_test_21[(int)(6)] != 0)) {
            uint slot = ((chip[0].cycles + unchecked((uint)(18))) % unchecked((uint)(24)));
            ushort testdata = unchecked((ushort)((((chip[0].pg_read & unchecked((uint)(1))) << 15) | ((chip[0].eg_read[(int)(chip[0].mode_test_21[(int)(0)])] & unchecked((uint)(1))) << 14))));
            if ((chip[0].mode_test_2c[(int)(4)] != 0)) {
            testdata |= unchecked((ushort)((unchecked((int)(chip[0].ch_read)) & 511)));
            } else {
            testdata |= unchecked((ushort)((unchecked((int)(chip[0].fm_out[(int)(slot)])) & 16383)));
            }
            if ((chip[0].mode_test_21[(int)(7)] != 0)) {
            chip[0].status = unchecked((byte)((unchecked((int)(testdata)) & 255)));
            } else {
            chip[0].status = unchecked((byte)((unchecked((int)(testdata)) >> 8)));
            }
            } else {
            chip[0].status = unchecked((byte)((((unchecked((int)(chip[0].busy)) << 7) | (unchecked((int)(chip[0].timer_b_overflow_flag)) << 1)) | unchecked((int)(chip[0].timer_a_overflow_flag)))));
            }
            if (((chip_type & unchecked((uint)(1))) != 0)) {
            chip[0].status_time = unchecked((uint)(300000));
            } else {
            chip[0].status_time = unchecked((uint)(40000000));
            }
            }
            if ((chip[0].status_time != 0)) {
            return chip[0].status;
            }
            return unchecked((byte)(0));
        }
    }
}
