using System.Buffers.Binary;
using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Chips.Audio.Yamaha.YM2413.Instruction;

public sealed partial class Ym2413
{
    private sealed class YM2413_OPLL_SLOT
    {
        public uint ar;
        public uint dr;
        public uint rr;
        public byte KSR;
        public byte ksl;
        public byte ksr;
        public byte mul;
        public uint phase;
        public uint freq;
        public byte fb_shift;
        public readonly int[] op1_out = new int[2];
        public byte eg_type;
        public byte state;
        public uint TL;
        public int TLL;
        public int volume;
        public uint sl;
        public byte eg_sh_dp;
        public byte eg_sel_dp;
        public byte eg_sh_ar;
        public ushort eg_sel_ar;
        public byte eg_sh_dr;
        public byte eg_sel_dr;
        public byte eg_sh_rr;
        public byte eg_sel_rr;
        public byte eg_sh_rs;
        public byte eg_sel_rs;
        public uint key;
        public uint AMmask;
        public byte vib;
        public uint wavetable;
    }
    private sealed class YM2413_OPLL_CH
    {
        public readonly YM2413_OPLL_SLOT[] SLOT = new YM2413_OPLL_SLOT[2];
        public uint block_fnum;
        public uint fc;
        public uint ksl_base;
        public byte kcode;
        public byte sus;
        public YM2413_OPLL_CH()
        {
            for (int i=0;i<2;i++) SLOT[i] = new();
        }
    }
    private sealed class YM2413
    {
        public readonly YM2413_OPLL_CH[] P_CH = new YM2413_OPLL_CH[9];
        public readonly byte[] instvol_r = new byte[9];
        public uint eg_cnt;
        public uint eg_timer;
        public uint eg_timer_add;
        public uint eg_timer_overflow;
        public byte rhythm;
        public uint lfo_am_cnt;
        public uint lfo_am_inc;
        public uint lfo_pm_cnt;
        public uint lfo_pm_inc;
        public uint noise_rng;
        public uint noise_p;
        public uint noise_f;
        public readonly byte[] inst_tab = new byte[152];
        public readonly uint[] fn_tab = new uint[1024];
        public byte address;
        public byte status;
        public double clock;
        public int rate;
        public YM2413()
        {
            for (int i=0;i<9;i++) P_CH[i] = new();
        }
    }
}
