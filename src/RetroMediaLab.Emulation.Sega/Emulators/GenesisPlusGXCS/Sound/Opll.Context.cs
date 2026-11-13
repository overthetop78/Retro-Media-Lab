using System.Buffers.Binary;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Sound;

internal sealed partial class Opll
{
    public const int ContextSize = 392;
    private static void Writeopll_patch_t(opll_patch_t value, Span<byte> state)
    {
        state[0] = unchecked((byte)value.tl);
        state[1] = unchecked((byte)value.dc);
        state[2] = unchecked((byte)value.dm);
        state[3] = unchecked((byte)value.fb);
        for (int i=0;i<2;i++) {
        state[4+i*1] = unchecked((byte)value.am[i]);
        }
        for (int i=0;i<2;i++) {
        state[6+i*1] = unchecked((byte)value.vib[i]);
        }
        for (int i=0;i<2;i++) {
        state[8+i*1] = unchecked((byte)value.et[i]);
        }
        for (int i=0;i<2;i++) {
        state[10+i*1] = unchecked((byte)value.ksr[i]);
        }
        for (int i=0;i<2;i++) {
        state[12+i*1] = unchecked((byte)value.multi[i]);
        }
        for (int i=0;i<2;i++) {
        state[14+i*1] = unchecked((byte)value.ksl[i]);
        }
        for (int i=0;i<2;i++) {
        state[16+i*1] = unchecked((byte)value.ar[i]);
        }
        for (int i=0;i<2;i++) {
        state[18+i*1] = unchecked((byte)value.dr[i]);
        }
        for (int i=0;i<2;i++) {
        state[20+i*1] = unchecked((byte)value.sl[i]);
        }
        for (int i=0;i<2;i++) {
        state[22+i*1] = unchecked((byte)value.rr[i]);
        }
    }
    private static void Readopll_patch_t(opll_patch_t value, ReadOnlySpan<byte> state)
    {
        value.tl = unchecked((byte)state[0]);
        value.dc = unchecked((byte)state[1]);
        value.dm = unchecked((byte)state[2]);
        value.fb = unchecked((byte)state[3]);
        for (int i=0;i<2;i++) {
        value.am[i] = unchecked((byte)state[4+i*1]);
        }
        for (int i=0;i<2;i++) {
        value.vib[i] = unchecked((byte)state[6+i*1]);
        }
        for (int i=0;i<2;i++) {
        value.et[i] = unchecked((byte)state[8+i*1]);
        }
        for (int i=0;i<2;i++) {
        value.ksr[i] = unchecked((byte)state[10+i*1]);
        }
        for (int i=0;i<2;i++) {
        value.multi[i] = unchecked((byte)state[12+i*1]);
        }
        for (int i=0;i<2;i++) {
        value.ksl[i] = unchecked((byte)state[14+i*1]);
        }
        for (int i=0;i<2;i++) {
        value.ar[i] = unchecked((byte)state[16+i*1]);
        }
        for (int i=0;i<2;i++) {
        value.dr[i] = unchecked((byte)state[18+i*1]);
        }
        for (int i=0;i<2;i++) {
        value.sl[i] = unchecked((byte)state[20+i*1]);
        }
        for (int i=0;i<2;i++) {
        value.rr[i] = unchecked((byte)state[22+i*1]);
        }
    }
    private static void Writeopll_t(opll_t value, Span<byte> state)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(0), value.chip_type);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(4), value.cycles);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(8), value.slot);
        state[24] = unchecked((byte)value.write_data);
        state[25] = unchecked((byte)value.write_a);
        state[26] = unchecked((byte)value.write_d);
        state[27] = unchecked((byte)value.write_a_en);
        state[28] = unchecked((byte)value.write_d_en);
        state[29] = unchecked((byte)value.write_fm_address);
        state[30] = unchecked((byte)value.write_fm_data);
        state[31] = unchecked((byte)value.write_mode_address);
        state[32] = unchecked((byte)value.address);
        state[33] = unchecked((byte)value.data);
        state[34] = unchecked((byte)value.eg_counter_state);
        state[35] = unchecked((byte)value.eg_counter_state_prev);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(36), value.eg_timer);
        state[40] = unchecked((byte)value.eg_timer_low_lock);
        state[41] = unchecked((byte)value.eg_timer_carry);
        state[42] = unchecked((byte)value.eg_timer_shift);
        state[43] = unchecked((byte)value.eg_timer_shift_lock);
        state[44] = unchecked((byte)value.eg_timer_shift_stop);
        for (int i=0;i<18;i++) {
        state[45+i*1] = unchecked((byte)value.eg_state[i]);
        }
        for (int i=0;i<18;i++) {
        state[63+i*1] = unchecked((byte)value.eg_level[i]);
        }
        state[81] = unchecked((byte)value.eg_kon);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(84), value.eg_dokon);
        state[88] = unchecked((byte)value.eg_off);
        state[89] = unchecked((byte)value.eg_rate);
        state[90] = unchecked((byte)value.eg_maxrate);
        state[91] = unchecked((byte)value.eg_zerorate);
        state[92] = unchecked((byte)value.eg_inc_lo);
        state[93] = unchecked((byte)value.eg_inc_hi);
        state[94] = unchecked((byte)value.eg_rate_hi);
        BinaryPrimitives.WriteUInt16LittleEndian(state.Slice(96), value.eg_sl);
        BinaryPrimitives.WriteUInt16LittleEndian(state.Slice(98), value.eg_ksltl);
        state[100] = unchecked((byte)value.eg_out);
        state[101] = unchecked((byte)value.eg_silent);
        BinaryPrimitives.WriteUInt16LittleEndian(state.Slice(102), value.pg_fnum);
        state[104] = unchecked((byte)value.pg_block);
        BinaryPrimitives.WriteUInt16LittleEndian(state.Slice(106), value.pg_out);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(108), value.pg_inc);
        for (int i=0;i<18;i++) {
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(112+i*4), value.pg_phase[i]);
        }
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(184), value.pg_phase_next);
        for (int i=0;i<9;i++) {
        BinaryPrimitives.WriteInt16LittleEndian(state.Slice(188+i*2), value.op_fb1[i]);
        }
        for (int i=0;i<9;i++) {
        BinaryPrimitives.WriteInt16LittleEndian(state.Slice(206+i*2), value.op_fb2[i]);
        }
        BinaryPrimitives.WriteInt16LittleEndian(state.Slice(224), value.op_fbsum);
        BinaryPrimitives.WriteInt16LittleEndian(state.Slice(226), value.op_mod);
        state[228] = unchecked((byte)value.op_neg);
        BinaryPrimitives.WriteUInt16LittleEndian(state.Slice(230), value.op_logsin);
        BinaryPrimitives.WriteUInt16LittleEndian(state.Slice(232), value.op_exp_m);
        BinaryPrimitives.WriteUInt16LittleEndian(state.Slice(234), value.op_exp_s);
        BinaryPrimitives.WriteInt16LittleEndian(state.Slice(236), value.ch_out);
        BinaryPrimitives.WriteInt16LittleEndian(state.Slice(238), value.ch_out_hh);
        BinaryPrimitives.WriteInt16LittleEndian(state.Slice(240), value.ch_out_tm);
        BinaryPrimitives.WriteInt16LittleEndian(state.Slice(242), value.ch_out_bd);
        BinaryPrimitives.WriteInt16LittleEndian(state.Slice(244), value.ch_out_sd);
        BinaryPrimitives.WriteInt16LittleEndian(state.Slice(246), value.ch_out_tc);
        BinaryPrimitives.WriteUInt16LittleEndian(state.Slice(248), value.lfo_counter);
        state[250] = unchecked((byte)value.lfo_vib_counter);
        BinaryPrimitives.WriteUInt16LittleEndian(state.Slice(252), value.lfo_am_counter);
        state[254] = unchecked((byte)value.lfo_am_step);
        state[255] = unchecked((byte)value.lfo_am_dir);
        state[256] = unchecked((byte)value.lfo_am_car);
        state[257] = unchecked((byte)value.lfo_am_out);
        for (int i=0;i<9;i++) {
        BinaryPrimitives.WriteUInt16LittleEndian(state.Slice(258+i*2), value.fnum[i]);
        }
        for (int i=0;i<9;i++) {
        state[276+i*1] = unchecked((byte)value.block[i]);
        }
        for (int i=0;i<9;i++) {
        state[285+i*1] = unchecked((byte)value.kon[i]);
        }
        for (int i=0;i<9;i++) {
        state[294+i*1] = unchecked((byte)value.son[i]);
        }
        for (int i=0;i<9;i++) {
        state[303+i*1] = unchecked((byte)value.vol[i]);
        }
        for (int i=0;i<9;i++) {
        state[312+i*1] = unchecked((byte)value.inst[i]);
        }
        state[321] = unchecked((byte)value.rhythm);
        state[322] = unchecked((byte)value.testmode);
        Writeopll_patch_t(value.patch, state.Slice(323, 24));
        state[347] = unchecked((byte)value.c_instr);
        state[348] = unchecked((byte)value.c_op);
        state[349] = unchecked((byte)value.c_tl);
        state[350] = unchecked((byte)value.c_dc);
        state[351] = unchecked((byte)value.c_dm);
        state[352] = unchecked((byte)value.c_fb);
        state[353] = unchecked((byte)value.c_am);
        state[354] = unchecked((byte)value.c_vib);
        state[355] = unchecked((byte)value.c_et);
        state[356] = unchecked((byte)value.c_ksr);
        state[357] = unchecked((byte)value.c_ksr_freq);
        state[358] = unchecked((byte)value.c_ksl_freq);
        state[359] = unchecked((byte)value.c_ksl_block);
        state[360] = unchecked((byte)value.c_multi);
        state[361] = unchecked((byte)value.c_ksl);
        for (int i=0;i<3;i++) {
        state[362+i*1] = unchecked((byte)value.c_adrr[i]);
        }
        state[365] = unchecked((byte)value.c_sl);
        BinaryPrimitives.WriteUInt16LittleEndian(state.Slice(366), value.c_fnum);
        BinaryPrimitives.WriteUInt16LittleEndian(state.Slice(368), value.c_block);
        state[370] = unchecked((byte)value.rm_enable);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(372), value.rm_noise);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(376), value.rm_select);
        state[380] = unchecked((byte)value.rm_hh_bit2);
        state[381] = unchecked((byte)value.rm_hh_bit3);
        state[382] = unchecked((byte)value.rm_hh_bit7);
        state[383] = unchecked((byte)value.rm_hh_bit8);
        state[384] = unchecked((byte)value.rm_tc_bit3);
        state[385] = unchecked((byte)value.rm_tc_bit5);
        BinaryPrimitives.WriteInt16LittleEndian(state.Slice(386), value.output_m);
        BinaryPrimitives.WriteInt16LittleEndian(state.Slice(388), value.output_r);
    }
    private static void Readopll_t(opll_t value, ReadOnlySpan<byte> state)
    {
        value.chip_type = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(0));
        value.cycles = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(4));
        value.slot = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(8));
        value.write_data = unchecked((byte)state[24]);
        value.write_a = unchecked((byte)state[25]);
        value.write_d = unchecked((byte)state[26]);
        value.write_a_en = unchecked((byte)state[27]);
        value.write_d_en = unchecked((byte)state[28]);
        value.write_fm_address = unchecked((byte)state[29]);
        value.write_fm_data = unchecked((byte)state[30]);
        value.write_mode_address = unchecked((byte)state[31]);
        value.address = unchecked((byte)state[32]);
        value.data = unchecked((byte)state[33]);
        value.eg_counter_state = unchecked((byte)state[34]);
        value.eg_counter_state_prev = unchecked((byte)state[35]);
        value.eg_timer = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(36));
        value.eg_timer_low_lock = unchecked((byte)state[40]);
        value.eg_timer_carry = unchecked((byte)state[41]);
        value.eg_timer_shift = unchecked((byte)state[42]);
        value.eg_timer_shift_lock = unchecked((byte)state[43]);
        value.eg_timer_shift_stop = unchecked((byte)state[44]);
        for (int i=0;i<18;i++) {
        value.eg_state[i] = unchecked((byte)state[45+i*1]);
        }
        for (int i=0;i<18;i++) {
        value.eg_level[i] = unchecked((byte)state[63+i*1]);
        }
        value.eg_kon = unchecked((byte)state[81]);
        value.eg_dokon = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(84));
        value.eg_off = unchecked((byte)state[88]);
        value.eg_rate = unchecked((byte)state[89]);
        value.eg_maxrate = unchecked((byte)state[90]);
        value.eg_zerorate = unchecked((byte)state[91]);
        value.eg_inc_lo = unchecked((byte)state[92]);
        value.eg_inc_hi = unchecked((byte)state[93]);
        value.eg_rate_hi = unchecked((byte)state[94]);
        value.eg_sl = BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(96));
        value.eg_ksltl = BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(98));
        value.eg_out = unchecked((byte)state[100]);
        value.eg_silent = unchecked((byte)state[101]);
        value.pg_fnum = BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(102));
        value.pg_block = unchecked((byte)state[104]);
        value.pg_out = BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(106));
        value.pg_inc = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(108));
        for (int i=0;i<18;i++) {
        value.pg_phase[i] = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(112+i*4));
        }
        value.pg_phase_next = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(184));
        for (int i=0;i<9;i++) {
        value.op_fb1[i] = BinaryPrimitives.ReadInt16LittleEndian(state.Slice(188+i*2));
        }
        for (int i=0;i<9;i++) {
        value.op_fb2[i] = BinaryPrimitives.ReadInt16LittleEndian(state.Slice(206+i*2));
        }
        value.op_fbsum = BinaryPrimitives.ReadInt16LittleEndian(state.Slice(224));
        value.op_mod = BinaryPrimitives.ReadInt16LittleEndian(state.Slice(226));
        value.op_neg = unchecked((byte)state[228]);
        value.op_logsin = BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(230));
        value.op_exp_m = BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(232));
        value.op_exp_s = BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(234));
        value.ch_out = BinaryPrimitives.ReadInt16LittleEndian(state.Slice(236));
        value.ch_out_hh = BinaryPrimitives.ReadInt16LittleEndian(state.Slice(238));
        value.ch_out_tm = BinaryPrimitives.ReadInt16LittleEndian(state.Slice(240));
        value.ch_out_bd = BinaryPrimitives.ReadInt16LittleEndian(state.Slice(242));
        value.ch_out_sd = BinaryPrimitives.ReadInt16LittleEndian(state.Slice(244));
        value.ch_out_tc = BinaryPrimitives.ReadInt16LittleEndian(state.Slice(246));
        value.lfo_counter = BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(248));
        value.lfo_vib_counter = unchecked((byte)state[250]);
        value.lfo_am_counter = BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(252));
        value.lfo_am_step = unchecked((byte)state[254]);
        value.lfo_am_dir = unchecked((byte)state[255]);
        value.lfo_am_car = unchecked((byte)state[256]);
        value.lfo_am_out = unchecked((byte)state[257]);
        for (int i=0;i<9;i++) {
        value.fnum[i] = BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(258+i*2));
        }
        for (int i=0;i<9;i++) {
        value.block[i] = unchecked((byte)state[276+i*1]);
        }
        for (int i=0;i<9;i++) {
        value.kon[i] = unchecked((byte)state[285+i*1]);
        }
        for (int i=0;i<9;i++) {
        value.son[i] = unchecked((byte)state[294+i*1]);
        }
        for (int i=0;i<9;i++) {
        value.vol[i] = unchecked((byte)state[303+i*1]);
        }
        for (int i=0;i<9;i++) {
        value.inst[i] = unchecked((byte)state[312+i*1]);
        }
        value.rhythm = unchecked((byte)state[321]);
        value.testmode = unchecked((byte)state[322]);
        Readopll_patch_t(value.patch, state.Slice(323, 24));
        value.c_instr = unchecked((byte)state[347]);
        value.c_op = unchecked((byte)state[348]);
        value.c_tl = unchecked((byte)state[349]);
        value.c_dc = unchecked((byte)state[350]);
        value.c_dm = unchecked((byte)state[351]);
        value.c_fb = unchecked((byte)state[352]);
        value.c_am = unchecked((byte)state[353]);
        value.c_vib = unchecked((byte)state[354]);
        value.c_et = unchecked((byte)state[355]);
        value.c_ksr = unchecked((byte)state[356]);
        value.c_ksr_freq = unchecked((byte)state[357]);
        value.c_ksl_freq = unchecked((byte)state[358]);
        value.c_ksl_block = unchecked((byte)state[359]);
        value.c_multi = unchecked((byte)state[360]);
        value.c_ksl = unchecked((byte)state[361]);
        for (int i=0;i<3;i++) {
        value.c_adrr[i] = unchecked((byte)state[362+i*1]);
        }
        value.c_sl = unchecked((byte)state[365]);
        value.c_fnum = BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(366));
        value.c_block = BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(368));
        value.rm_enable = unchecked((sbyte)state[370]);
        value.rm_noise = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(372));
        value.rm_select = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(376));
        value.rm_hh_bit2 = unchecked((byte)state[380]);
        value.rm_hh_bit3 = unchecked((byte)state[381]);
        value.rm_hh_bit7 = unchecked((byte)state[382]);
        value.rm_hh_bit8 = unchecked((byte)state[383]);
        value.rm_tc_bit3 = unchecked((byte)state[384]);
        value.rm_tc_bit5 = unchecked((byte)state[385]);
        value.output_m = BinaryPrimitives.ReadInt16LittleEndian(state.Slice(386));
        value.output_r = BinaryPrimitives.ReadInt16LittleEndian(state.Slice(388));
    }
}
