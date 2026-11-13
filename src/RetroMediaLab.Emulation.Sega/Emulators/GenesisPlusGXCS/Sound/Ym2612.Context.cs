using System.Buffers.Binary;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Sound;

internal sealed partial class Ym2612
{
    private const int NativeContextSize = 3576;
    public const int ContextSize = NativeContextSize + 48;
    private static void WriteFM_SLOT(FM_SLOT value, Span<byte> state)
    {
        state[8] = unchecked((byte)value.KSR);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(12), value.ar);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(16), value.d1r);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(20), value.d2r);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(24), value.rr);
        state[28] = unchecked((byte)value.ksr);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(32), value.mul);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(36), value.phase);
        BinaryPrimitives.WriteInt32LittleEndian(state.Slice(40), value.Incr);
        state[44] = unchecked((byte)value.state);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(48), value.tl);
        BinaryPrimitives.WriteInt32LittleEndian(state.Slice(52), value.volume);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(56), value.sl);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(60), value.vol_out);
        state[64] = unchecked((byte)value.eg_sh_ar);
        state[65] = unchecked((byte)value.eg_sel_ar);
        state[66] = unchecked((byte)value.eg_sh_d1r);
        state[67] = unchecked((byte)value.eg_sel_d1r);
        state[68] = unchecked((byte)value.eg_sh_d2r);
        state[69] = unchecked((byte)value.eg_sel_d2r);
        state[70] = unchecked((byte)value.eg_sh_rr);
        state[71] = unchecked((byte)value.eg_sel_rr);
        state[72] = unchecked((byte)value.ssg);
        state[73] = unchecked((byte)value.ssgn);
        state[74] = unchecked((byte)value.key);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(76), value.AMmask);
    }
    private static void ReadFM_SLOT(FM_SLOT value, ReadOnlySpan<byte> state)
    {
        value.KSR = unchecked((byte)state[8]);
        value.ar = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(12));
        value.d1r = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(16));
        value.d2r = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(20));
        value.rr = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(24));
        value.ksr = unchecked((byte)state[28]);
        value.mul = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(32));
        value.phase = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(36));
        value.Incr = BinaryPrimitives.ReadInt32LittleEndian(state.Slice(40));
        value.state = unchecked((byte)state[44]);
        value.tl = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(48));
        value.volume = BinaryPrimitives.ReadInt32LittleEndian(state.Slice(52));
        value.sl = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(56));
        value.vol_out = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(60));
        value.eg_sh_ar = unchecked((byte)state[64]);
        value.eg_sel_ar = unchecked((byte)state[65]);
        value.eg_sh_d1r = unchecked((byte)state[66]);
        value.eg_sel_d1r = unchecked((byte)state[67]);
        value.eg_sh_d2r = unchecked((byte)state[68]);
        value.eg_sel_d2r = unchecked((byte)state[69]);
        value.eg_sh_rr = unchecked((byte)state[70]);
        value.eg_sel_rr = unchecked((byte)state[71]);
        value.ssg = unchecked((byte)state[72]);
        value.ssgn = unchecked((byte)state[73]);
        value.key = unchecked((byte)state[74]);
        value.AMmask = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(76));
    }
    private static void WriteFM_CH(FM_CH value, Span<byte> state)
    {
        for (int i=0;i<4;i++)
        {
        WriteFM_SLOT(value.SLOT[i], state.Slice(0+i*80, 80));
        }
        state[320] = unchecked((byte)value.ALGO);
        state[321] = unchecked((byte)value.FB);
        for (int i=0;i<2;i++)
        {
        BinaryPrimitives.WriteInt32LittleEndian(state.Slice(324+i*4), value.op1_out[i]);
        }
        BinaryPrimitives.WriteInt32LittleEndian(state.Slice(376), value.mem_value);
        BinaryPrimitives.WriteInt32LittleEndian(state.Slice(380), value.pms);
        state[384] = unchecked((byte)value.ams);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(388), value.fc);
        state[392] = unchecked((byte)value.kcode);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(396), value.block_fnum);
    }
    private static void ReadFM_CH(FM_CH value, ReadOnlySpan<byte> state)
    {
        for (int i=0;i<4;i++)
        {
        ReadFM_SLOT(value.SLOT[i], state.Slice(0+i*80, 80));
        }
        value.ALGO = unchecked((byte)state[320]);
        value.FB = unchecked((byte)state[321]);
        for (int i=0;i<2;i++)
        {
        value.op1_out[i] = BinaryPrimitives.ReadInt32LittleEndian(state.Slice(324+i*4));
        }
        value.mem_value = BinaryPrimitives.ReadInt32LittleEndian(state.Slice(376));
        value.pms = BinaryPrimitives.ReadInt32LittleEndian(state.Slice(380));
        value.ams = unchecked((byte)state[384]);
        value.fc = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(388));
        value.kcode = unchecked((byte)state[392]);
        value.block_fnum = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(396));
    }
    private static void WriteFM_ST(FM_ST value, Span<byte> state)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(state.Slice(0), value.address);
        state[2] = unchecked((byte)value.status);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(4), value.mode);
        state[8] = unchecked((byte)value.fn_h);
        BinaryPrimitives.WriteInt32LittleEndian(state.Slice(12), value.TA);
        BinaryPrimitives.WriteInt32LittleEndian(state.Slice(16), value.TAL);
        BinaryPrimitives.WriteInt32LittleEndian(state.Slice(20), value.TAC);
        BinaryPrimitives.WriteInt32LittleEndian(state.Slice(24), value.TB);
        BinaryPrimitives.WriteInt32LittleEndian(state.Slice(28), value.TBL);
        BinaryPrimitives.WriteInt32LittleEndian(state.Slice(32), value.TBC);
        for (int i=0;i<256;i++)
        {
        BinaryPrimitives.WriteInt32LittleEndian(state.Slice(36+i*4), value.dt_tab[i]);
        }
    }
    private static void ReadFM_ST(FM_ST value, ReadOnlySpan<byte> state)
    {
        value.address = BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(0));
        value.status = unchecked((byte)state[2]);
        value.mode = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(4));
        value.fn_h = unchecked((byte)state[8]);
        value.TA = BinaryPrimitives.ReadInt32LittleEndian(state.Slice(12));
        value.TAL = BinaryPrimitives.ReadInt32LittleEndian(state.Slice(16));
        value.TAC = BinaryPrimitives.ReadInt32LittleEndian(state.Slice(20));
        value.TB = BinaryPrimitives.ReadInt32LittleEndian(state.Slice(24));
        value.TBL = BinaryPrimitives.ReadInt32LittleEndian(state.Slice(28));
        value.TBC = BinaryPrimitives.ReadInt32LittleEndian(state.Slice(32));
        for (int i=0;i<256;i++)
        {
        value.dt_tab[i] = BinaryPrimitives.ReadInt32LittleEndian(state.Slice(36+i*4));
        }
    }
    private static void WriteFM_3SLOT(FM_3SLOT value, Span<byte> state)
    {
        for (int i=0;i<3;i++)
        {
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(0+i*4), value.fc[i]);
        }
        state[12] = unchecked((byte)value.fn_h);
        for (int i=0;i<3;i++)
        {
        state[13+i*1] = unchecked((byte)value.kcode[i]);
        }
        for (int i=0;i<3;i++)
        {
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(16+i*4), value.block_fnum[i]);
        }
        state[28] = unchecked((byte)value.key_csm);
    }
    private static void ReadFM_3SLOT(FM_3SLOT value, ReadOnlySpan<byte> state)
    {
        for (int i=0;i<3;i++)
        {
        value.fc[i] = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(0+i*4));
        }
        value.fn_h = unchecked((byte)state[12]);
        for (int i=0;i<3;i++)
        {
        value.kcode[i] = unchecked((byte)state[13+i*1]);
        }
        for (int i=0;i<3;i++)
        {
        value.block_fnum[i] = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(16+i*4));
        }
        value.key_csm = unchecked((byte)state[28]);
    }
    private static void WriteFM_OPN(FM_OPN value, Span<byte> state)
    {
        WriteFM_ST(value.ST, state.Slice(0, 1060));
        WriteFM_3SLOT(value.SL3, state.Slice(1060, 32));
        for (int i=0;i<12;i++)
        {
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(1092+i*4), value.pan[i]);
        }
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(1140), value.eg_cnt);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(1144), value.eg_timer);
        state[1148] = unchecked((byte)value.lfo_cnt);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(1152), value.lfo_timer);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(1156), value.lfo_timer_overflow);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(1160), value.LFO_AM);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(1164), value.LFO_PM);
    }
    private static void ReadFM_OPN(FM_OPN value, ReadOnlySpan<byte> state)
    {
        ReadFM_ST(value.ST, state.Slice(0, 1060));
        ReadFM_3SLOT(value.SL3, state.Slice(1060, 32));
        for (int i=0;i<12;i++)
        {
        value.pan[i] = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(1092+i*4));
        }
        value.eg_cnt = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(1140));
        value.eg_timer = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(1144));
        value.lfo_cnt = unchecked((byte)state[1148]);
        value.lfo_timer = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(1152));
        value.lfo_timer_overflow = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(1156));
        value.LFO_AM = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(1160));
        value.LFO_PM = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(1164));
    }
    private static void WriteYM2612(YM2612 value, Span<byte> state)
    {
        for (int i=0;i<6;i++)
        {
        WriteFM_CH(value.CH[i], state.Slice(0+i*400, 400));
        }
        state[2400] = unchecked((byte)value.dacen);
        BinaryPrimitives.WriteInt32LittleEndian(state.Slice(2404), value.dacout);
        WriteFM_OPN(value.OPN, state.Slice(2408, 1168));
    }
    private static void ReadYM2612(YM2612 value, ReadOnlySpan<byte> state)
    {
        for (int i=0;i<6;i++)
        {
        ReadFM_CH(value.CH[i], state.Slice(0+i*400, 400));
        }
        value.dacen = unchecked((byte)state[2400]);
        value.dacout = BinaryPrimitives.ReadInt32LittleEndian(state.Slice(2404));
        ReadFM_OPN(value.OPN, state.Slice(2408, 1168));
    }
}
