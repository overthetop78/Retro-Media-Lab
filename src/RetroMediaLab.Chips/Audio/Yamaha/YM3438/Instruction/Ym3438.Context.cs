using System.Buffers.Binary;
using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Chips.Audio.Yamaha.YM3438.Instruction;

public sealed partial class Ym3438
{
    public const int ContextSize = 1252;
    private static void Writeym3438_t(ym3438_t value, Span<byte> state)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(0), value.cycles);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(4), value.channel);
        BinaryPrimitives.WriteInt16LittleEndian(state.Slice(8), value.mol);
        BinaryPrimitives.WriteInt16LittleEndian(state.Slice(10), value.mor);
        BinaryPrimitives.WriteUInt16LittleEndian(state.Slice(12), value.write_data);
        state[14] = unchecked((byte)value.write_a);
        state[15] = unchecked((byte)value.write_d);
        state[16] = unchecked((byte)value.write_a_en);
        state[17] = unchecked((byte)value.write_d_en);
        state[18] = unchecked((byte)value.write_busy);
        state[19] = unchecked((byte)value.write_busy_cnt);
        state[20] = unchecked((byte)value.write_fm_address);
        state[21] = unchecked((byte)value.write_fm_data);
        BinaryPrimitives.WriteUInt16LittleEndian(state.Slice(22), value.write_fm_mode_a);
        BinaryPrimitives.WriteUInt16LittleEndian(state.Slice(24), value.address);
        state[26] = unchecked((byte)value.data);
        state[27] = unchecked((byte)value.pin_test_in);
        state[28] = unchecked((byte)value.pin_irq);
        state[29] = unchecked((byte)value.busy);
        state[30] = unchecked((byte)value.lfo_en);
        state[31] = unchecked((byte)value.lfo_freq);
        state[32] = unchecked((byte)value.lfo_pm);
        state[33] = unchecked((byte)value.lfo_am);
        state[34] = unchecked((byte)value.lfo_cnt);
        state[35] = unchecked((byte)value.lfo_inc);
        state[36] = unchecked((byte)value.lfo_quotient);
        BinaryPrimitives.WriteUInt16LittleEndian(state.Slice(38), value.pg_fnum);
        state[40] = unchecked((byte)value.pg_block);
        state[41] = unchecked((byte)value.pg_kcode);
        for (int i=0;i<24;i++) {
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(44+i*4), value.pg_inc[i]);
        }
        for (int i=0;i<24;i++) {
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(140+i*4), value.pg_phase[i]);
        }
        for (int i=0;i<24;i++) {
        state[236+i*1] = unchecked((byte)value.pg_reset[i]);
        }
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(260), value.pg_read);
        state[264] = unchecked((byte)value.eg_cycle);
        state[265] = unchecked((byte)value.eg_cycle_stop);
        state[266] = unchecked((byte)value.eg_shift);
        state[267] = unchecked((byte)value.eg_shift_lock);
        state[268] = unchecked((byte)value.eg_timer_low_lock);
        BinaryPrimitives.WriteUInt16LittleEndian(state.Slice(270), value.eg_timer);
        state[272] = unchecked((byte)value.eg_timer_inc);
        BinaryPrimitives.WriteUInt16LittleEndian(state.Slice(274), value.eg_quotient);
        state[276] = unchecked((byte)value.eg_custom_timer);
        state[277] = unchecked((byte)value.eg_rate);
        state[278] = unchecked((byte)value.eg_ksv);
        state[279] = unchecked((byte)value.eg_inc);
        state[280] = unchecked((byte)value.eg_ratemax);
        for (int i=0;i<2;i++) {
        state[281+i*1] = unchecked((byte)value.eg_sl[i]);
        }
        state[283] = unchecked((byte)value.eg_lfo_am);
        for (int i=0;i<2;i++) {
        state[284+i*1] = unchecked((byte)value.eg_tl[i]);
        }
        for (int i=0;i<24;i++) {
        state[286+i*1] = unchecked((byte)value.eg_state[i]);
        }
        for (int i=0;i<24;i++) {
        BinaryPrimitives.WriteUInt16LittleEndian(state.Slice(310+i*2), value.eg_level[i]);
        }
        for (int i=0;i<24;i++) {
        BinaryPrimitives.WriteUInt16LittleEndian(state.Slice(358+i*2), value.eg_out[i]);
        }
        for (int i=0;i<24;i++) {
        state[406+i*1] = unchecked((byte)value.eg_kon[i]);
        }
        for (int i=0;i<24;i++) {
        state[430+i*1] = unchecked((byte)value.eg_kon_csm[i]);
        }
        for (int i=0;i<24;i++) {
        state[454+i*1] = unchecked((byte)value.eg_kon_latch[i]);
        }
        for (int i=0;i<24;i++) {
        state[478+i*1] = unchecked((byte)value.eg_csm_mode[i]);
        }
        for (int i=0;i<24;i++) {
        state[502+i*1] = unchecked((byte)value.eg_ssg_enable[i]);
        }
        for (int i=0;i<24;i++) {
        state[526+i*1] = unchecked((byte)value.eg_ssg_pgrst_latch[i]);
        }
        for (int i=0;i<24;i++) {
        state[550+i*1] = unchecked((byte)value.eg_ssg_repeat_latch[i]);
        }
        for (int i=0;i<24;i++) {
        state[574+i*1] = unchecked((byte)value.eg_ssg_hold_up_latch[i]);
        }
        for (int i=0;i<24;i++) {
        state[598+i*1] = unchecked((byte)value.eg_ssg_dir[i]);
        }
        for (int i=0;i<24;i++) {
        state[622+i*1] = unchecked((byte)value.eg_ssg_inv[i]);
        }
        for (int i=0;i<2;i++) {
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(648+i*4), value.eg_read[i]);
        }
        state[656] = unchecked((byte)value.eg_read_inc);
        for (int i=0;i<12;i++) {
        BinaryPrimitives.WriteInt16LittleEndian(state.Slice(658+i*2), value.fm_op1[i]);
        }
        for (int i=0;i<6;i++) {
        BinaryPrimitives.WriteInt16LittleEndian(state.Slice(682+i*2), value.fm_op2[i]);
        }
        for (int i=0;i<24;i++) {
        BinaryPrimitives.WriteInt16LittleEndian(state.Slice(694+i*2), value.fm_out[i]);
        }
        for (int i=0;i<24;i++) {
        BinaryPrimitives.WriteUInt16LittleEndian(state.Slice(742+i*2), value.fm_mod[i]);
        }
        for (int i=0;i<6;i++) {
        BinaryPrimitives.WriteInt16LittleEndian(state.Slice(790+i*2), value.ch_acc[i]);
        }
        for (int i=0;i<6;i++) {
        BinaryPrimitives.WriteInt16LittleEndian(state.Slice(802+i*2), value.ch_out[i]);
        }
        BinaryPrimitives.WriteInt16LittleEndian(state.Slice(814), value.ch_lock);
        state[816] = unchecked((byte)value.ch_lock_l);
        state[817] = unchecked((byte)value.ch_lock_r);
        BinaryPrimitives.WriteInt16LittleEndian(state.Slice(818), value.ch_read);
        BinaryPrimitives.WriteUInt16LittleEndian(state.Slice(820), value.timer_a_cnt);
        BinaryPrimitives.WriteUInt16LittleEndian(state.Slice(822), value.timer_a_reg);
        state[824] = unchecked((byte)value.timer_a_load_lock);
        state[825] = unchecked((byte)value.timer_a_load);
        state[826] = unchecked((byte)value.timer_a_enable);
        state[827] = unchecked((byte)value.timer_a_reset);
        state[828] = unchecked((byte)value.timer_a_load_latch);
        state[829] = unchecked((byte)value.timer_a_overflow_flag);
        state[830] = unchecked((byte)value.timer_a_overflow);
        BinaryPrimitives.WriteUInt16LittleEndian(state.Slice(832), value.timer_b_cnt);
        state[834] = unchecked((byte)value.timer_b_subcnt);
        BinaryPrimitives.WriteUInt16LittleEndian(state.Slice(836), value.timer_b_reg);
        state[838] = unchecked((byte)value.timer_b_load_lock);
        state[839] = unchecked((byte)value.timer_b_load);
        state[840] = unchecked((byte)value.timer_b_enable);
        state[841] = unchecked((byte)value.timer_b_reset);
        state[842] = unchecked((byte)value.timer_b_load_latch);
        state[843] = unchecked((byte)value.timer_b_overflow_flag);
        state[844] = unchecked((byte)value.timer_b_overflow);
        for (int i=0;i<8;i++) {
        state[845+i*1] = unchecked((byte)value.mode_test_21[i]);
        }
        for (int i=0;i<8;i++) {
        state[853+i*1] = unchecked((byte)value.mode_test_2c[i]);
        }
        state[861] = unchecked((byte)value.mode_ch3);
        state[862] = unchecked((byte)value.mode_kon_channel);
        for (int i=0;i<4;i++) {
        state[863+i*1] = unchecked((byte)value.mode_kon_operator[i]);
        }
        for (int i=0;i<24;i++) {
        state[867+i*1] = unchecked((byte)value.mode_kon[i]);
        }
        state[891] = unchecked((byte)value.mode_csm);
        state[892] = unchecked((byte)value.mode_kon_csm);
        state[893] = unchecked((byte)value.dacen);
        BinaryPrimitives.WriteInt16LittleEndian(state.Slice(894), value.dacdata);
        for (int i=0;i<24;i++) {
        state[896+i*1] = unchecked((byte)value.ks[i]);
        }
        for (int i=0;i<24;i++) {
        state[920+i*1] = unchecked((byte)value.ar[i]);
        }
        for (int i=0;i<24;i++) {
        state[944+i*1] = unchecked((byte)value.sr[i]);
        }
        for (int i=0;i<24;i++) {
        state[968+i*1] = unchecked((byte)value.dt[i]);
        }
        for (int i=0;i<24;i++) {
        state[992+i*1] = unchecked((byte)value.multi[i]);
        }
        for (int i=0;i<24;i++) {
        state[1016+i*1] = unchecked((byte)value.sl[i]);
        }
        for (int i=0;i<24;i++) {
        state[1040+i*1] = unchecked((byte)value.rr[i]);
        }
        for (int i=0;i<24;i++) {
        state[1064+i*1] = unchecked((byte)value.dr[i]);
        }
        for (int i=0;i<24;i++) {
        state[1088+i*1] = unchecked((byte)value.am[i]);
        }
        for (int i=0;i<24;i++) {
        state[1112+i*1] = unchecked((byte)value.tl[i]);
        }
        for (int i=0;i<24;i++) {
        state[1136+i*1] = unchecked((byte)value.ssg_eg[i]);
        }
        for (int i=0;i<6;i++) {
        BinaryPrimitives.WriteUInt16LittleEndian(state.Slice(1160+i*2), value.fnum[i]);
        }
        for (int i=0;i<6;i++) {
        state[1172+i*1] = unchecked((byte)value.block[i]);
        }
        for (int i=0;i<6;i++) {
        state[1178+i*1] = unchecked((byte)value.kcode[i]);
        }
        for (int i=0;i<6;i++) {
        BinaryPrimitives.WriteUInt16LittleEndian(state.Slice(1184+i*2), value.fnum_3ch[i]);
        }
        for (int i=0;i<6;i++) {
        state[1196+i*1] = unchecked((byte)value.block_3ch[i]);
        }
        for (int i=0;i<6;i++) {
        state[1202+i*1] = unchecked((byte)value.kcode_3ch[i]);
        }
        state[1208] = unchecked((byte)value.reg_a4);
        state[1209] = unchecked((byte)value.reg_ac);
        for (int i=0;i<6;i++) {
        state[1210+i*1] = unchecked((byte)value.connect[i]);
        }
        for (int i=0;i<6;i++) {
        state[1216+i*1] = unchecked((byte)value.fb[i]);
        }
        for (int i=0;i<6;i++) {
        state[1222+i*1] = unchecked((byte)value.pan_l[i]);
        }
        for (int i=0;i<6;i++) {
        state[1228+i*1] = unchecked((byte)value.pan_r[i]);
        }
        for (int i=0;i<6;i++) {
        state[1234+i*1] = unchecked((byte)value.ams[i]);
        }
        for (int i=0;i<6;i++) {
        state[1240+i*1] = unchecked((byte)value.pms[i]);
        }
        state[1246] = unchecked((byte)value.status);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(1248), value.status_time);
    }
    private static void Readym3438_t(ym3438_t value, ReadOnlySpan<byte> state)
    {
        value.cycles = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(0));
        value.channel = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(4));
        value.mol = BinaryPrimitives.ReadInt16LittleEndian(state.Slice(8));
        value.mor = BinaryPrimitives.ReadInt16LittleEndian(state.Slice(10));
        value.write_data = BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(12));
        value.write_a = unchecked((byte)state[14]);
        value.write_d = unchecked((byte)state[15]);
        value.write_a_en = unchecked((byte)state[16]);
        value.write_d_en = unchecked((byte)state[17]);
        value.write_busy = unchecked((byte)state[18]);
        value.write_busy_cnt = unchecked((byte)state[19]);
        value.write_fm_address = unchecked((byte)state[20]);
        value.write_fm_data = unchecked((byte)state[21]);
        value.write_fm_mode_a = BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(22));
        value.address = BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(24));
        value.data = unchecked((byte)state[26]);
        value.pin_test_in = unchecked((byte)state[27]);
        value.pin_irq = unchecked((byte)state[28]);
        value.busy = unchecked((byte)state[29]);
        value.lfo_en = unchecked((byte)state[30]);
        value.lfo_freq = unchecked((byte)state[31]);
        value.lfo_pm = unchecked((byte)state[32]);
        value.lfo_am = unchecked((byte)state[33]);
        value.lfo_cnt = unchecked((byte)state[34]);
        value.lfo_inc = unchecked((byte)state[35]);
        value.lfo_quotient = unchecked((byte)state[36]);
        value.pg_fnum = BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(38));
        value.pg_block = unchecked((byte)state[40]);
        value.pg_kcode = unchecked((byte)state[41]);
        for (int i=0;i<24;i++) {
        value.pg_inc[i] = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(44+i*4));
        }
        for (int i=0;i<24;i++) {
        value.pg_phase[i] = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(140+i*4));
        }
        for (int i=0;i<24;i++) {
        value.pg_reset[i] = unchecked((byte)state[236+i*1]);
        }
        value.pg_read = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(260));
        value.eg_cycle = unchecked((byte)state[264]);
        value.eg_cycle_stop = unchecked((byte)state[265]);
        value.eg_shift = unchecked((byte)state[266]);
        value.eg_shift_lock = unchecked((byte)state[267]);
        value.eg_timer_low_lock = unchecked((byte)state[268]);
        value.eg_timer = BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(270));
        value.eg_timer_inc = unchecked((byte)state[272]);
        value.eg_quotient = BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(274));
        value.eg_custom_timer = unchecked((byte)state[276]);
        value.eg_rate = unchecked((byte)state[277]);
        value.eg_ksv = unchecked((byte)state[278]);
        value.eg_inc = unchecked((byte)state[279]);
        value.eg_ratemax = unchecked((byte)state[280]);
        for (int i=0;i<2;i++) {
        value.eg_sl[i] = unchecked((byte)state[281+i*1]);
        }
        value.eg_lfo_am = unchecked((byte)state[283]);
        for (int i=0;i<2;i++) {
        value.eg_tl[i] = unchecked((byte)state[284+i*1]);
        }
        for (int i=0;i<24;i++) {
        value.eg_state[i] = unchecked((byte)state[286+i*1]);
        }
        for (int i=0;i<24;i++) {
        value.eg_level[i] = BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(310+i*2));
        }
        for (int i=0;i<24;i++) {
        value.eg_out[i] = BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(358+i*2));
        }
        for (int i=0;i<24;i++) {
        value.eg_kon[i] = unchecked((byte)state[406+i*1]);
        }
        for (int i=0;i<24;i++) {
        value.eg_kon_csm[i] = unchecked((byte)state[430+i*1]);
        }
        for (int i=0;i<24;i++) {
        value.eg_kon_latch[i] = unchecked((byte)state[454+i*1]);
        }
        for (int i=0;i<24;i++) {
        value.eg_csm_mode[i] = unchecked((byte)state[478+i*1]);
        }
        for (int i=0;i<24;i++) {
        value.eg_ssg_enable[i] = unchecked((byte)state[502+i*1]);
        }
        for (int i=0;i<24;i++) {
        value.eg_ssg_pgrst_latch[i] = unchecked((byte)state[526+i*1]);
        }
        for (int i=0;i<24;i++) {
        value.eg_ssg_repeat_latch[i] = unchecked((byte)state[550+i*1]);
        }
        for (int i=0;i<24;i++) {
        value.eg_ssg_hold_up_latch[i] = unchecked((byte)state[574+i*1]);
        }
        for (int i=0;i<24;i++) {
        value.eg_ssg_dir[i] = unchecked((byte)state[598+i*1]);
        }
        for (int i=0;i<24;i++) {
        value.eg_ssg_inv[i] = unchecked((byte)state[622+i*1]);
        }
        for (int i=0;i<2;i++) {
        value.eg_read[i] = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(648+i*4));
        }
        value.eg_read_inc = unchecked((byte)state[656]);
        for (int i=0;i<12;i++) {
        value.fm_op1[i] = BinaryPrimitives.ReadInt16LittleEndian(state.Slice(658+i*2));
        }
        for (int i=0;i<6;i++) {
        value.fm_op2[i] = BinaryPrimitives.ReadInt16LittleEndian(state.Slice(682+i*2));
        }
        for (int i=0;i<24;i++) {
        value.fm_out[i] = BinaryPrimitives.ReadInt16LittleEndian(state.Slice(694+i*2));
        }
        for (int i=0;i<24;i++) {
        value.fm_mod[i] = BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(742+i*2));
        }
        for (int i=0;i<6;i++) {
        value.ch_acc[i] = BinaryPrimitives.ReadInt16LittleEndian(state.Slice(790+i*2));
        }
        for (int i=0;i<6;i++) {
        value.ch_out[i] = BinaryPrimitives.ReadInt16LittleEndian(state.Slice(802+i*2));
        }
        value.ch_lock = BinaryPrimitives.ReadInt16LittleEndian(state.Slice(814));
        value.ch_lock_l = unchecked((byte)state[816]);
        value.ch_lock_r = unchecked((byte)state[817]);
        value.ch_read = BinaryPrimitives.ReadInt16LittleEndian(state.Slice(818));
        value.timer_a_cnt = BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(820));
        value.timer_a_reg = BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(822));
        value.timer_a_load_lock = unchecked((byte)state[824]);
        value.timer_a_load = unchecked((byte)state[825]);
        value.timer_a_enable = unchecked((byte)state[826]);
        value.timer_a_reset = unchecked((byte)state[827]);
        value.timer_a_load_latch = unchecked((byte)state[828]);
        value.timer_a_overflow_flag = unchecked((byte)state[829]);
        value.timer_a_overflow = unchecked((byte)state[830]);
        value.timer_b_cnt = BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(832));
        value.timer_b_subcnt = unchecked((byte)state[834]);
        value.timer_b_reg = BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(836));
        value.timer_b_load_lock = unchecked((byte)state[838]);
        value.timer_b_load = unchecked((byte)state[839]);
        value.timer_b_enable = unchecked((byte)state[840]);
        value.timer_b_reset = unchecked((byte)state[841]);
        value.timer_b_load_latch = unchecked((byte)state[842]);
        value.timer_b_overflow_flag = unchecked((byte)state[843]);
        value.timer_b_overflow = unchecked((byte)state[844]);
        for (int i=0;i<8;i++) {
        value.mode_test_21[i] = unchecked((byte)state[845+i*1]);
        }
        for (int i=0;i<8;i++) {
        value.mode_test_2c[i] = unchecked((byte)state[853+i*1]);
        }
        value.mode_ch3 = unchecked((byte)state[861]);
        value.mode_kon_channel = unchecked((byte)state[862]);
        for (int i=0;i<4;i++) {
        value.mode_kon_operator[i] = unchecked((byte)state[863+i*1]);
        }
        for (int i=0;i<24;i++) {
        value.mode_kon[i] = unchecked((byte)state[867+i*1]);
        }
        value.mode_csm = unchecked((byte)state[891]);
        value.mode_kon_csm = unchecked((byte)state[892]);
        value.dacen = unchecked((byte)state[893]);
        value.dacdata = BinaryPrimitives.ReadInt16LittleEndian(state.Slice(894));
        for (int i=0;i<24;i++) {
        value.ks[i] = unchecked((byte)state[896+i*1]);
        }
        for (int i=0;i<24;i++) {
        value.ar[i] = unchecked((byte)state[920+i*1]);
        }
        for (int i=0;i<24;i++) {
        value.sr[i] = unchecked((byte)state[944+i*1]);
        }
        for (int i=0;i<24;i++) {
        value.dt[i] = unchecked((byte)state[968+i*1]);
        }
        for (int i=0;i<24;i++) {
        value.multi[i] = unchecked((byte)state[992+i*1]);
        }
        for (int i=0;i<24;i++) {
        value.sl[i] = unchecked((byte)state[1016+i*1]);
        }
        for (int i=0;i<24;i++) {
        value.rr[i] = unchecked((byte)state[1040+i*1]);
        }
        for (int i=0;i<24;i++) {
        value.dr[i] = unchecked((byte)state[1064+i*1]);
        }
        for (int i=0;i<24;i++) {
        value.am[i] = unchecked((byte)state[1088+i*1]);
        }
        for (int i=0;i<24;i++) {
        value.tl[i] = unchecked((byte)state[1112+i*1]);
        }
        for (int i=0;i<24;i++) {
        value.ssg_eg[i] = unchecked((byte)state[1136+i*1]);
        }
        for (int i=0;i<6;i++) {
        value.fnum[i] = BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(1160+i*2));
        }
        for (int i=0;i<6;i++) {
        value.block[i] = unchecked((byte)state[1172+i*1]);
        }
        for (int i=0;i<6;i++) {
        value.kcode[i] = unchecked((byte)state[1178+i*1]);
        }
        for (int i=0;i<6;i++) {
        value.fnum_3ch[i] = BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(1184+i*2));
        }
        for (int i=0;i<6;i++) {
        value.block_3ch[i] = unchecked((byte)state[1196+i*1]);
        }
        for (int i=0;i<6;i++) {
        value.kcode_3ch[i] = unchecked((byte)state[1202+i*1]);
        }
        value.reg_a4 = unchecked((byte)state[1208]);
        value.reg_ac = unchecked((byte)state[1209]);
        for (int i=0;i<6;i++) {
        value.connect[i] = unchecked((byte)state[1210+i*1]);
        }
        for (int i=0;i<6;i++) {
        value.fb[i] = unchecked((byte)state[1216+i*1]);
        }
        for (int i=0;i<6;i++) {
        value.pan_l[i] = unchecked((byte)state[1222+i*1]);
        }
        for (int i=0;i<6;i++) {
        value.pan_r[i] = unchecked((byte)state[1228+i*1]);
        }
        for (int i=0;i<6;i++) {
        value.ams[i] = unchecked((byte)state[1234+i*1]);
        }
        for (int i=0;i<6;i++) {
        value.pms[i] = unchecked((byte)state[1240+i*1]);
        }
        value.status = unchecked((byte)state[1246]);
        value.status_time = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(1248));
    }
}
