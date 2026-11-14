using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Chips.Audio.Yamaha.YM2612.Instruction;

public sealed partial class Ym2612
{
    private sealed class FM_SLOT
    {
        public int DetuneIndex;
        private readonly int[] _detune;
        public ArrayPointer<int> DT { get => new(_detune, DetuneIndex * 32); set => DetuneIndex = (value - new ArrayPointer<int>(_detune)) >> 5; }
        public byte KSR;
        public uint ar;
        public uint d1r;
        public uint d2r;
        public uint rr;
        public byte ksr;
        public uint mul;
        public uint phase;
        public int Incr;
        public byte state;
        public uint tl;
        public int volume;
        public uint sl;
        public uint vol_out;
        public byte eg_sh_ar;
        public byte eg_sel_ar;
        public byte eg_sh_d1r;
        public byte eg_sel_d1r;
        public byte eg_sh_d2r;
        public byte eg_sel_d2r;
        public byte eg_sh_rr;
        public byte eg_sel_rr;
        public byte ssg;
        public byte ssgn;
        public byte key;
        public uint AMmask;
        public FM_SLOT(int[] detune) { _detune = detune; }
    }
    private sealed class FM_CH
    {
        public readonly FM_SLOT[] SLOT = new FM_SLOT[4];
        public byte ALGO;
        public byte FB;
        public readonly int[] op1_out = new int[2];
        public int connect1Index;
        public ArrayPointer<int> connect1 => connect1Index < 0 ? default : new(_signals, connect1Index);
        public int connect3Index;
        public ArrayPointer<int> connect3 => connect3Index < 0 ? default : new(_signals, connect3Index);
        public int connect2Index;
        public ArrayPointer<int> connect2 => connect2Index < 0 ? default : new(_signals, connect2Index);
        public int connect4Index;
        public ArrayPointer<int> connect4 => connect4Index < 0 ? default : new(_signals, connect4Index);
        public int mem_connectIndex;
        public ArrayPointer<int> mem_connect => mem_connectIndex < 0 ? default : new(_signals, mem_connectIndex);
        public int mem_value;
        public int pms;
        public byte ams;
        public uint fc;
        public byte kcode;
        public uint block_fnum;
        private readonly int[] _signals;
        public FM_CH(int[] detune, int[] signals) { _signals = signals; for (int i=0;i<4;i++) SLOT[i] = new FM_SLOT(detune); }
    }
    private sealed class FM_ST
    {
        public ushort address;
        public byte status;
        public uint mode;
        public byte fn_h;
        public int TA;
        public int TAL;
        public int TAC;
        public int TB;
        public int TBL;
        public int TBC;
        public readonly int[] dt_tab = new int[256];
    }
    private sealed class FM_3SLOT
    {
        public readonly uint[] fc = new uint[3];
        public byte fn_h;
        public readonly byte[] kcode = new byte[3];
        public readonly uint[] block_fnum = new uint[3];
        public byte key_csm;
    }
    private sealed class FM_OPN
    {
        public readonly FM_ST ST = new();
        public readonly FM_3SLOT SL3 = new();
        public readonly uint[] pan = new uint[12];
        public uint eg_cnt;
        public uint eg_timer;
        public byte lfo_cnt;
        public uint lfo_timer;
        public uint lfo_timer_overflow;
        public uint LFO_AM;
        public uint LFO_PM;
    }
    private sealed class YM2612
    {
        public readonly FM_CH[] CH = new FM_CH[6];
        public byte dacen;
        public int dacout;
        public readonly FM_OPN OPN = new();
        public YM2612(int[] signals) { for (int i=0;i<6;i++) CH[i] = new FM_CH(OPN.ST.dt_tab, signals); }
    }
}
