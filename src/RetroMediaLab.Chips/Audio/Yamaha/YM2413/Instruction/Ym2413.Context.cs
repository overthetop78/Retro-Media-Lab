using System.Buffers.Binary;
using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Chips.Audio.Yamaha.YM2413.Instruction;

public sealed partial class Ym2413
{
    public const int ContextSize = 5984;
    private static void WriteYM2413_OPLL_SLOT(YM2413_OPLL_SLOT value, Span<byte> state)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(0), value.ar);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(4), value.dr);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(8), value.rr);
        state[12] = unchecked((byte)value.KSR);
        state[13] = unchecked((byte)value.ksl);
        state[14] = unchecked((byte)value.ksr);
        state[15] = unchecked((byte)value.mul);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(16), value.phase);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(20), value.freq);
        state[24] = unchecked((byte)value.fb_shift);
        for (int i=0;i<2;i++) {
        BinaryPrimitives.WriteInt32LittleEndian(state.Slice(28+i*4), value.op1_out[i]);
        }
        state[36] = unchecked((byte)value.eg_type);
        state[37] = unchecked((byte)value.state);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(40), value.TL);
        BinaryPrimitives.WriteInt32LittleEndian(state.Slice(44), value.TLL);
        BinaryPrimitives.WriteInt32LittleEndian(state.Slice(48), value.volume);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(52), value.sl);
        state[56] = unchecked((byte)value.eg_sh_dp);
        state[57] = unchecked((byte)value.eg_sel_dp);
        state[58] = unchecked((byte)value.eg_sh_ar);
        BinaryPrimitives.WriteUInt16LittleEndian(state.Slice(60), value.eg_sel_ar);
        state[62] = unchecked((byte)value.eg_sh_dr);
        state[63] = unchecked((byte)value.eg_sel_dr);
        state[64] = unchecked((byte)value.eg_sh_rr);
        state[65] = unchecked((byte)value.eg_sel_rr);
        state[66] = unchecked((byte)value.eg_sh_rs);
        state[67] = unchecked((byte)value.eg_sel_rs);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(68), value.key);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(72), value.AMmask);
        state[76] = unchecked((byte)value.vib);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(80), value.wavetable);
    }
    private static void ReadYM2413_OPLL_SLOT(YM2413_OPLL_SLOT value, ReadOnlySpan<byte> state)
    {
        value.ar = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(0));
        value.dr = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(4));
        value.rr = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(8));
        value.KSR = unchecked((byte)state[12]);
        value.ksl = unchecked((byte)state[13]);
        value.ksr = unchecked((byte)state[14]);
        value.mul = unchecked((byte)state[15]);
        value.phase = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(16));
        value.freq = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(20));
        value.fb_shift = unchecked((byte)state[24]);
        for (int i=0;i<2;i++) {
        value.op1_out[i] = BinaryPrimitives.ReadInt32LittleEndian(state.Slice(28+i*4));
        }
        value.eg_type = unchecked((byte)state[36]);
        value.state = unchecked((byte)state[37]);
        value.TL = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(40));
        value.TLL = BinaryPrimitives.ReadInt32LittleEndian(state.Slice(44));
        value.volume = BinaryPrimitives.ReadInt32LittleEndian(state.Slice(48));
        value.sl = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(52));
        value.eg_sh_dp = unchecked((byte)state[56]);
        value.eg_sel_dp = unchecked((byte)state[57]);
        value.eg_sh_ar = unchecked((byte)state[58]);
        value.eg_sel_ar = BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(60));
        value.eg_sh_dr = unchecked((byte)state[62]);
        value.eg_sel_dr = unchecked((byte)state[63]);
        value.eg_sh_rr = unchecked((byte)state[64]);
        value.eg_sel_rr = unchecked((byte)state[65]);
        value.eg_sh_rs = unchecked((byte)state[66]);
        value.eg_sel_rs = unchecked((byte)state[67]);
        value.key = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(68));
        value.AMmask = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(72));
        value.vib = unchecked((byte)state[76]);
        value.wavetable = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(80));
    }
    private static void WriteYM2413_OPLL_CH(YM2413_OPLL_CH value, Span<byte> state)
    {
        for (int i=0;i<2;i++) {
        WriteYM2413_OPLL_SLOT(value.SLOT[i], state.Slice(0+i*84, 84));
        }
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(168), value.block_fnum);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(172), value.fc);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(176), value.ksl_base);
        state[180] = unchecked((byte)value.kcode);
        state[181] = unchecked((byte)value.sus);
    }
    private static void ReadYM2413_OPLL_CH(YM2413_OPLL_CH value, ReadOnlySpan<byte> state)
    {
        for (int i=0;i<2;i++) {
        ReadYM2413_OPLL_SLOT(value.SLOT[i], state.Slice(0+i*84, 84));
        }
        value.block_fnum = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(168));
        value.fc = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(172));
        value.ksl_base = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(176));
        value.kcode = unchecked((byte)state[180]);
        value.sus = unchecked((byte)state[181]);
    }
    private static void WriteYM2413(YM2413 value, Span<byte> state)
    {
        for (int i=0;i<9;i++) {
        WriteYM2413_OPLL_CH(value.P_CH[i], state.Slice(0+i*184, 184));
        }
        for (int i=0;i<9;i++) {
        state[1656+i*1] = unchecked((byte)value.instvol_r[i]);
        }
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(1668), value.eg_cnt);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(1672), value.eg_timer);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(1676), value.eg_timer_add);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(1680), value.eg_timer_overflow);
        state[1684] = unchecked((byte)value.rhythm);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(1688), value.lfo_am_cnt);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(1692), value.lfo_am_inc);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(1696), value.lfo_pm_cnt);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(1700), value.lfo_pm_inc);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(1704), value.noise_rng);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(1708), value.noise_p);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(1712), value.noise_f);
        for (int i=0;i<152;i++) {
        state[1716+i*1] = unchecked((byte)value.inst_tab[i]);
        }
        for (int i=0;i<1024;i++) {
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(1868+i*4), value.fn_tab[i]);
        }
        state[5964] = unchecked((byte)value.address);
        state[5965] = unchecked((byte)value.status);
        BinaryPrimitives.WriteInt64LittleEndian(state.Slice(5968), BitConverter.DoubleToInt64Bits(value.clock));
        BinaryPrimitives.WriteInt32LittleEndian(state.Slice(5976), value.rate);
    }
    private static void ReadYM2413(YM2413 value, ReadOnlySpan<byte> state)
    {
        for (int i=0;i<9;i++) {
        ReadYM2413_OPLL_CH(value.P_CH[i], state.Slice(0+i*184, 184));
        }
        for (int i=0;i<9;i++) {
        value.instvol_r[i] = unchecked((byte)state[1656+i*1]);
        }
        value.eg_cnt = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(1668));
        value.eg_timer = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(1672));
        value.eg_timer_add = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(1676));
        value.eg_timer_overflow = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(1680));
        value.rhythm = unchecked((byte)state[1684]);
        value.lfo_am_cnt = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(1688));
        value.lfo_am_inc = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(1692));
        value.lfo_pm_cnt = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(1696));
        value.lfo_pm_inc = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(1700));
        value.noise_rng = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(1704));
        value.noise_p = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(1708));
        value.noise_f = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(1712));
        for (int i=0;i<152;i++) {
        value.inst_tab[i] = unchecked((byte)state[1716+i*1]);
        }
        for (int i=0;i<1024;i++) {
        value.fn_tab[i] = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(1868+i*4));
        }
        value.address = unchecked((byte)state[5964]);
        value.status = unchecked((byte)state[5965]);
        value.clock = BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(state.Slice(5968)));
        value.rate = BinaryPrimitives.ReadInt32LittleEndian(state.Slice(5976));
    }
}
