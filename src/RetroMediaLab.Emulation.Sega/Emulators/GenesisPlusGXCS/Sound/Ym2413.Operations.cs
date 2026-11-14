using System.Buffers.Binary;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Sound;

internal sealed partial class Ym2413
{
    private void advance_lfo()
    {
        unchecked
        {
            ym2413.lfo_am_cnt += ym2413.lfo_am_inc;
            if ((ym2413.lfo_am_cnt >= unchecked((uint)((210 << 24))))) ym2413.lfo_am_cnt -= unchecked((uint)((210 << 24)));
            LFO_AM = unchecked((uint)((unchecked((int)(lfo_am_table[(int)((ym2413.lfo_am_cnt >> 24))])) >> 1)));
            ym2413.lfo_pm_cnt += ym2413.lfo_pm_inc;
            LFO_PM = unchecked((int)(((ym2413.lfo_pm_cnt >> 24) & unchecked((uint)(7)))));
        }
    }
    private void advance()
    {
        unchecked
        {
            CPointer<YM2413_OPLL_CH> CH = default;
            CPointer<YM2413_OPLL_SLOT> op = default;
            uint i = default;
            ym2413.eg_timer += ym2413.eg_timer_add;
            while ((ym2413.eg_timer >= ym2413.eg_timer_overflow)) {
            ym2413.eg_timer -= ym2413.eg_timer_overflow;
            ym2413.eg_cnt++;
            for (i = unchecked((uint)(0)); (i < unchecked((uint)((9 * 2)))); i++) {
            CH = new CPointer<YM2413_OPLL_CH>(ym2413.P_CH, (int)((i >> 1)));
            op = new CPointer<YM2413_OPLL_SLOT>(CH[0].SLOT, (int)((i & unchecked((uint)(1)))));
            switch (op[0].state) {
            case 5:
            if (((op[0].volume & (~3)) == (((1 << (10 - 3)) - 1) & (~3)))) {
            op[0].state = unchecked((byte)(4));
            if (((op[0].ar + unchecked((uint)(op[0].ksr))) >= unchecked((uint)((16 + 60))))) {
            op[0].volume = 0;
            }
            if (((i & unchecked((uint)(1))) != 0)) {
            CH[0].SLOT[(int)(0)].phase = (CH[0].SLOT[(int)(1)].phase = unchecked((uint)(0)));
            }
            } else if ((!((ym2413.eg_cnt & unchecked((uint)(((1 << unchecked((int)(op[0].eg_sh_dp))) - 1)))) != 0))) {
            op[0].volume += unchecked((int)(eg_inc[(int)((unchecked((uint)(op[0].eg_sel_dp)) + ((ym2413.eg_cnt >> unchecked((int)(op[0].eg_sh_dp))) & unchecked((uint)(15)))))]));
            }
            break;
            case 4:
            if ((op[0].volume == 0)) {
            op[0].state = unchecked((byte)(3));
            } else if ((!((ym2413.eg_cnt & unchecked((uint)((((1 << unchecked((int)(op[0].eg_sh_ar))) - 1) & (~3))))) != 0))) {
            op[0].volume += (((~op[0].volume) * unchecked((int)(eg_mul[(int)((unchecked((uint)(op[0].eg_sel_ar)) + ((ym2413.eg_cnt >> unchecked((int)(op[0].eg_sh_ar))) & unchecked((uint)(15)))))]))) >> 4);
            }
            break;
            case 3:
            if ((unchecked((uint)((op[0].volume & (~7)))) == op[0].sl)) {
            op[0].state = unchecked((byte)(2));
            } else if ((!((ym2413.eg_cnt & unchecked((uint)(((1 << unchecked((int)(op[0].eg_sh_dr))) - 1)))) != 0))) {
            op[0].volume += unchecked((int)(eg_inc[(int)((unchecked((uint)(op[0].eg_sel_dr)) + ((ym2413.eg_cnt >> unchecked((int)(op[0].eg_sh_dr))) & unchecked((uint)(15)))))]));
            if (((op[0].volume & (~3)) == (((1 << (10 - 3)) - 1) & (~3)))) {
            op[0].state = unchecked((byte)(0));
            }
            }
            break;
            case 2:
            if ((op[0].eg_type != 0)) {
            
            } else {
            if ((!((ym2413.eg_cnt & unchecked((uint)(((1 << unchecked((int)(op[0].eg_sh_rr))) - 1)))) != 0))) {
            op[0].volume += unchecked((int)(eg_inc[(int)((unchecked((uint)(op[0].eg_sel_rr)) + ((ym2413.eg_cnt >> unchecked((int)(op[0].eg_sh_rr))) & unchecked((uint)(15)))))]));
            if (((op[0].volume & (~3)) == (((1 << (10 - 3)) - 1) & (~3)))) {
            op[0].state = unchecked((byte)(0));
            }
            }
            }
            break;
            case 1:
            if ((((i & unchecked((uint)(1))) != 0) || (((unchecked((int)(ym2413.rhythm)) & 32) != 0) && (i >= unchecked((uint)(12)))))) {
            if ((op[0].eg_type != 0)) {
            if ((CH[0].sus != 0)) {
            if ((!((ym2413.eg_cnt & unchecked((uint)(((1 << unchecked((int)(op[0].eg_sh_rs))) - 1)))) != 0))) {
            op[0].volume += unchecked((int)(eg_inc[(int)((unchecked((uint)(op[0].eg_sel_rs)) + ((ym2413.eg_cnt >> unchecked((int)(op[0].eg_sh_rs))) & unchecked((uint)(15)))))]));
            if (((op[0].volume & (~3)) == (((1 << (10 - 3)) - 1) & (~3)))) {
            op[0].state = unchecked((byte)(0));
            }
            }
            } else {
            if ((!((ym2413.eg_cnt & unchecked((uint)(((1 << unchecked((int)(op[0].eg_sh_rr))) - 1)))) != 0))) {
            op[0].volume += unchecked((int)(eg_inc[(int)((unchecked((uint)(op[0].eg_sel_rr)) + ((ym2413.eg_cnt >> unchecked((int)(op[0].eg_sh_rr))) & unchecked((uint)(15)))))]));
            if (((op[0].volume & (~3)) == (((1 << (10 - 3)) - 1) & (~3)))) {
            op[0].state = unchecked((byte)(0));
            }
            }
            }
            } else {
            if ((!((ym2413.eg_cnt & unchecked((uint)(((1 << unchecked((int)(op[0].eg_sh_rs))) - 1)))) != 0))) {
            op[0].volume += unchecked((int)(eg_inc[(int)((unchecked((uint)(op[0].eg_sel_rs)) + ((ym2413.eg_cnt >> unchecked((int)(op[0].eg_sh_rs))) & unchecked((uint)(15)))))]));
            if (((op[0].volume & (~3)) == (((1 << (10 - 3)) - 1) & (~3)))) {
            op[0].state = unchecked((byte)(0));
            }
            }
            }
            }
            break;
            case 0:
            op[0].volume = ((1 << (10 - 3)) - 1);
            break;
            default:
            break;
            }
            }
            }
            for (i = unchecked((uint)(0)); (i < unchecked((uint)((9 * 2)))); i++) {
            CH = new CPointer<YM2413_OPLL_CH>(ym2413.P_CH, (int)((i / unchecked((uint)(2)))));
            op = new CPointer<YM2413_OPLL_SLOT>(CH[0].SLOT, (int)((i & unchecked((uint)(1)))));
            if ((op[0].vib != 0)) {
            byte block = default;
            uint fnum_lfo = (unchecked((uint)(8)) * ((CH[0].block_fnum & unchecked((uint)(448))) >> 6));
            uint block_fnum = (CH[0].block_fnum * unchecked((uint)(2)));
            int lfo_fn_table_index_offset = unchecked((int)(lfo_pm_table[(int)((unchecked((uint)(LFO_PM)) + fnum_lfo))]));
            if ((lfo_fn_table_index_offset != 0)) {
            block_fnum += unchecked((uint)(lfo_fn_table_index_offset));
            block = unchecked((byte)(((block_fnum & unchecked((uint)(7168))) >> 10)));
            op[0].phase += ((ym2413.fn_tab[(int)((block_fnum & unchecked((uint)(1023))))] >> (7 - unchecked((int)(block)))) * unchecked((uint)(op[0].mul)));
            } else {
            op[0].phase += op[0].freq;
            }
            } else {
            op[0].phase += op[0].freq;
            }
            }
            ym2413.noise_p += ym2413.noise_f;
            i = (ym2413.noise_p >> 16);
            ym2413.noise_p &= unchecked((uint)(((1 << 16) - 1)));
            while ((i != 0)) {
            if (((ym2413.noise_rng & unchecked((uint)(1))) != 0)) ym2413.noise_rng ^= unchecked((uint)(8389378));
            ym2413.noise_rng >>= 1;
            i--;
            }
        }
    }
    private int op_calc(uint phase, uint env, int pm, uint wave_tab)
    {
        unchecked
        {
            uint p = ((env << 5) + sin_tab[(int)((wave_tab + unchecked((uint)(((unchecked((int)(((phase & unchecked((uint)((~((1 << 16) - 1))))) + unchecked((uint)((pm << 17)))))) >> 16) & ((1 << 10) - 1))))))]);
            if ((p >= unchecked((uint)(((11 * 2) * 256))))) return 0;
            return tl_tab[(int)(p)];
        }
    }
    private int op_calc1(uint phase, uint env, int pm, uint wave_tab)
    {
        unchecked
        {
            uint p = ((env << 5) + sin_tab[(int)((wave_tab + unchecked((uint)(((unchecked((int)(((phase & unchecked((uint)((~((1 << 16) - 1))))) + unchecked((uint)(pm))))) >> 16) & ((1 << 10) - 1))))))]);
            if ((p >= unchecked((uint)(((11 * 2) * 256))))) return 0;
            return tl_tab[(int)(p)];
        }
    }
    private void chan_calc(CPointer<YM2413_OPLL_CH> CH)
    {
        unchecked
        {
            CPointer<YM2413_OPLL_SLOT> SLOT = default;
            uint env = default;
            int sampleOutput = default;
            int phase_modulation = default;
            SLOT = new CPointer<YM2413_OPLL_SLOT>(CH[0].SLOT, (int)(0));
            env = ((unchecked((int)(SLOT[0].state)) != 0) ? ((unchecked((uint)(SLOT[0].TLL)) + unchecked((uint)(SLOT[0].volume))) + (LFO_AM & SLOT[0].AMmask)) : unchecked((uint)((((11 * 2) * 256) >> 5))));
            sampleOutput = (SLOT[0].op1_out[(int)(0)] + SLOT[0].op1_out[(int)(1)]);
            SLOT[0].op1_out[(int)(0)] = SLOT[0].op1_out[(int)(1)];
            phase_modulation = SLOT[0].op1_out[(int)(0)];
            SLOT[0].op1_out[(int)(1)] = 0;
            if ((env < unchecked((uint)((((11 * 2) * 256) >> 5))))) {
            if ((!(SLOT[0].fb_shift != 0))) sampleOutput = 0;
            SLOT[0].op1_out[(int)(1)] = op_calc1(SLOT[0].phase, env, (sampleOutput << unchecked((int)(SLOT[0].fb_shift))), SLOT[0].wavetable);
            }
            SLOT++;
            env = ((unchecked((int)(SLOT[0].state)) != 0) ? ((unchecked((uint)(SLOT[0].TLL)) + unchecked((uint)(SLOT[0].volume))) + (LFO_AM & SLOT[0].AMmask)) : unchecked((uint)((((11 * 2) * 256) >> 5))));
            if ((env < unchecked((uint)((((11 * 2) * 256) >> 5))))) {
            output[(int)(0)] += op_calc(SLOT[0].phase, env, phase_modulation, SLOT[0].wavetable);
            }
        }
    }
    private void rhythm_calc(CPointer<YM2413_OPLL_CH> CH, uint noise)
    {
        unchecked
        {
            CPointer<YM2413_OPLL_SLOT> SLOT = default;
            int sampleOutput = default;
            uint env = default;
            int phase_modulation = default;
            SLOT = new CPointer<YM2413_OPLL_SLOT>(CH[(int)(6)].SLOT, (int)(0));
            env = ((unchecked((int)(SLOT[0].state)) != 0) ? ((unchecked((uint)(SLOT[0].TLL)) + unchecked((uint)(SLOT[0].volume))) + (LFO_AM & SLOT[0].AMmask)) : unchecked((uint)((((11 * 2) * 256) >> 5))));
            sampleOutput = (SLOT[0].op1_out[(int)(0)] + SLOT[0].op1_out[(int)(1)]);
            SLOT[0].op1_out[(int)(0)] = SLOT[0].op1_out[(int)(1)];
            phase_modulation = SLOT[0].op1_out[(int)(0)];
            SLOT[0].op1_out[(int)(1)] = 0;
            if ((env < unchecked((uint)((((11 * 2) * 256) >> 5))))) {
            if ((!(SLOT[0].fb_shift != 0))) sampleOutput = 0;
            SLOT[0].op1_out[(int)(1)] = op_calc1(SLOT[0].phase, env, (sampleOutput << unchecked((int)(SLOT[0].fb_shift))), SLOT[0].wavetable);
            }
            SLOT++;
            env = ((unchecked((int)(SLOT[0].state)) != 0) ? ((unchecked((uint)(SLOT[0].TLL)) + unchecked((uint)(SLOT[0].volume))) + (LFO_AM & SLOT[0].AMmask)) : unchecked((uint)((((11 * 2) * 256) >> 5))));
            if ((env < unchecked((uint)((((11 * 2) * 256) >> 5))))) output[(int)(1)] += (op_calc(SLOT[0].phase, env, phase_modulation, SLOT[0].wavetable)) * _channelVolumes[6] / 100;
            env = ((unchecked((int)(new CPointer<YM2413_OPLL_SLOT>(CH[(int)(7)].SLOT, (int)(0))[0].state)) != 0) ? ((unchecked((uint)(new CPointer<YM2413_OPLL_SLOT>(CH[(int)(7)].SLOT, (int)(0))[0].TLL)) + unchecked((uint)(new CPointer<YM2413_OPLL_SLOT>(CH[(int)(7)].SLOT, (int)(0))[0].volume))) + (LFO_AM & new CPointer<YM2413_OPLL_SLOT>(CH[(int)(7)].SLOT, (int)(0))[0].AMmask)) : unchecked((uint)((((11 * 2) * 256) >> 5))));
            if ((env < unchecked((uint)((((11 * 2) * 256) >> 5))))) {
            byte bit7 = unchecked((byte)((((CH[(int)(7)].SLOT[(int)(0)].phase >> 16) >> 7) & unchecked((uint)(1)))));
            byte bit3 = unchecked((byte)((((CH[(int)(7)].SLOT[(int)(0)].phase >> 16) >> 3) & unchecked((uint)(1)))));
            byte bit2 = unchecked((byte)((((CH[(int)(7)].SLOT[(int)(0)].phase >> 16) >> 2) & unchecked((uint)(1)))));
            byte res1 = unchecked((byte)(((unchecked((int)(bit2)) ^ unchecked((int)(bit7))) | unchecked((int)(bit3)))));
            uint phase = unchecked((uint)(((res1 != 0) ? (512 | (208 >> 2)) : 208)));
            byte bit5e = unchecked((byte)((((CH[(int)(8)].SLOT[(int)(1)].phase >> 16) >> 5) & unchecked((uint)(1)))));
            byte bit3e = unchecked((byte)((((CH[(int)(8)].SLOT[(int)(1)].phase >> 16) >> 3) & unchecked((uint)(1)))));
            byte res2 = unchecked((byte)((unchecked((int)(bit3e)) | unchecked((int)(bit5e)))));
            if ((res2 != 0)) phase = unchecked((uint)((512 | (208 >> 2))));
            if (((phase & unchecked((uint)(512))) != 0)) {
            if ((noise != 0)) phase = unchecked((uint)((512 | 208)));
            } else {
            if ((noise != 0)) phase = unchecked((uint)((208 >> 2)));
            }
            output[(int)(1)] += (op_calc((phase << 16), env, 0, CH[(int)(7)].SLOT[(int)(0)].wavetable)) * _channelVolumes[7] / 100;
            }
            env = ((unchecked((int)(new CPointer<YM2413_OPLL_SLOT>(CH[(int)(7)].SLOT, (int)(1))[0].state)) != 0) ? ((unchecked((uint)(new CPointer<YM2413_OPLL_SLOT>(CH[(int)(7)].SLOT, (int)(1))[0].TLL)) + unchecked((uint)(new CPointer<YM2413_OPLL_SLOT>(CH[(int)(7)].SLOT, (int)(1))[0].volume))) + (LFO_AM & new CPointer<YM2413_OPLL_SLOT>(CH[(int)(7)].SLOT, (int)(1))[0].AMmask)) : unchecked((uint)((((11 * 2) * 256) >> 5))));
            if ((env < unchecked((uint)((((11 * 2) * 256) >> 5))))) {
            byte bit8 = unchecked((byte)((((CH[(int)(7)].SLOT[(int)(0)].phase >> 16) >> 8) & unchecked((uint)(1)))));
            uint phase = unchecked((uint)(((bit8 != 0) ? 512 : 256)));
            if ((noise != 0)) phase ^= unchecked((uint)(256));
            output[(int)(1)] += (op_calc((phase << 16), env, 0, CH[(int)(7)].SLOT[(int)(1)].wavetable)) * _channelVolumes[7] / 100;
            }
            env = ((unchecked((int)(new CPointer<YM2413_OPLL_SLOT>(CH[(int)(8)].SLOT, (int)(0))[0].state)) != 0) ? ((unchecked((uint)(new CPointer<YM2413_OPLL_SLOT>(CH[(int)(8)].SLOT, (int)(0))[0].TLL)) + unchecked((uint)(new CPointer<YM2413_OPLL_SLOT>(CH[(int)(8)].SLOT, (int)(0))[0].volume))) + (LFO_AM & new CPointer<YM2413_OPLL_SLOT>(CH[(int)(8)].SLOT, (int)(0))[0].AMmask)) : unchecked((uint)((((11 * 2) * 256) >> 5))));
            if ((env < unchecked((uint)((((11 * 2) * 256) >> 5))))) output[(int)(1)] += (op_calc(CH[(int)(8)].SLOT[(int)(0)].phase, env, 0, CH[(int)(8)].SLOT[(int)(0)].wavetable)) * _channelVolumes[8] / 100;
            env = ((unchecked((int)(new CPointer<YM2413_OPLL_SLOT>(CH[(int)(8)].SLOT, (int)(1))[0].state)) != 0) ? ((unchecked((uint)(new CPointer<YM2413_OPLL_SLOT>(CH[(int)(8)].SLOT, (int)(1))[0].TLL)) + unchecked((uint)(new CPointer<YM2413_OPLL_SLOT>(CH[(int)(8)].SLOT, (int)(1))[0].volume))) + (LFO_AM & new CPointer<YM2413_OPLL_SLOT>(CH[(int)(8)].SLOT, (int)(1))[0].AMmask)) : unchecked((uint)((((11 * 2) * 256) >> 5))));
            if ((env < unchecked((uint)((((11 * 2) * 256) >> 5))))) {
            byte bit7 = unchecked((byte)((((CH[(int)(7)].SLOT[(int)(0)].phase >> 16) >> 7) & unchecked((uint)(1)))));
            byte bit3 = unchecked((byte)((((CH[(int)(7)].SLOT[(int)(0)].phase >> 16) >> 3) & unchecked((uint)(1)))));
            byte bit2 = unchecked((byte)((((CH[(int)(7)].SLOT[(int)(0)].phase >> 16) >> 2) & unchecked((uint)(1)))));
            byte res1 = unchecked((byte)(((unchecked((int)(bit2)) ^ unchecked((int)(bit7))) | unchecked((int)(bit3)))));
            uint phase = unchecked((uint)(((res1 != 0) ? 768 : 256)));
            byte bit5e = unchecked((byte)((((CH[(int)(8)].SLOT[(int)(1)].phase >> 16) >> 5) & unchecked((uint)(1)))));
            byte bit3e = unchecked((byte)((((CH[(int)(8)].SLOT[(int)(1)].phase >> 16) >> 3) & unchecked((uint)(1)))));
            byte res2 = unchecked((byte)((unchecked((int)(bit3e)) | unchecked((int)(bit5e)))));
            if ((res2 != 0)) phase = unchecked((uint)(768));
            output[(int)(1)] += (op_calc((phase << 16), env, 0, CH[(int)(8)].SLOT[(int)(1)].wavetable)) * _channelVolumes[8] / 100;
            }
        }
    }
    private int init_tables()
    {
        unchecked
        {
            int i = default;
            int x = default;
            int n = default;
            double o = default;
            double m = default;
            for (x = 0; (x < 256); x++) {
            m = (unchecked((double)((1 << 16))) / pow(unchecked((double)(2)), ((unchecked((double)((x + 1))) * ((128.0 / unchecked((double)((1 << 10)))) / 4.0)) / 8.0)));
            m = floor(m);
            n = unchecked((int)(m));
            n >>= 4;
            if (((n & 1) != 0)) n = ((n >> 1) + 1); else n = (n >> 1);
            tl_tab[(int)(((x * 2) + 0))] = n;
            tl_tab[(int)(((x * 2) + 1))] = (-tl_tab[(int)(((x * 2) + 0))]);
            for (i = 1; (i < 11); i++) {
            tl_tab[(int)((((x * 2) + 0) + ((i * 2) * 256)))] = (tl_tab[(int)(((x * 2) + 0))] >> i);
            tl_tab[(int)((((x * 2) + 1) + ((i * 2) * 256)))] = (-tl_tab[(int)((((x * 2) + 0) + ((i * 2) * 256)))]);
            }
            }
            for (i = 0; (i < (1 << 10)); i++) {
            m = sin(((unchecked((double)(((i * 2) + 1))) * M_PI) / unchecked((double)((1 << 10)))));
            if ((m > 0.0)) o = ((unchecked((double)(8)) * log((1.0 / m))) / log(unchecked((double)(2)))); else o = ((unchecked((double)(8)) * log(((-1.0) / m))) / log(unchecked((double)(2))));
            o = (o / ((128.0 / unchecked((double)((1 << 10)))) / unchecked((double)(4))));
            n = unchecked((int)((2.0 * o)));
            if (((n & 1) != 0)) n = ((n >> 1) + 1); else n = (n >> 1);
            sin_tab[(int)(i)] = unchecked((uint)(((n * 2) + ((m >= 0.0) ? 0 : 1))));
            if (((i & (1 << (10 - 1))) != 0)) sin_tab[(int)(((1 * (1 << 10)) + i))] = unchecked((uint)(((11 * 2) * 256))); else sin_tab[(int)(((1 * (1 << 10)) + i))] = sin_tab[(int)(i)];
            }
            return 1;
        }
    }
    private void OPLL_initalize()
    {
        unchecked
        {
            int i = default;
            double freqbase = 1.0;
            for (i = 0; (i < 1024); i++) {
            ym2413.fn_tab[(int)(i)] = unchecked((uint)((((unchecked((double)(i)) * unchecked((double)(64))) * freqbase) * unchecked((double)((1 << (16 - 10)))))));
            }
            ym2413.lfo_am_inc = unchecked((uint)((((1.0 / 64.0) * unchecked((double)((1 << 24)))) * freqbase)));
            ym2413.lfo_pm_inc = unchecked((uint)((((1.0 / 1024.0) * unchecked((double)((1 << 24)))) * freqbase)));
            ym2413.noise_f = unchecked((uint)((((1.0 / 1.0) * unchecked((double)((1 << 16)))) * freqbase)));
            ym2413.eg_timer_add = unchecked((uint)((unchecked((double)((1 << 16))) * freqbase)));
            ym2413.eg_timer_overflow = unchecked((uint)((1 * (1 << 16))));
        }
    }
    private void KEY_ON(CPointer<YM2413_OPLL_SLOT> SLOT, uint key_set)
    {
        unchecked
        {
            if ((!(SLOT[0].key != 0))) {
            SLOT[0].state = unchecked((byte)(5));
            }
            SLOT[0].key |= key_set;
        }
    }
    private void KEY_OFF(CPointer<YM2413_OPLL_SLOT> SLOT, uint key_clr)
    {
        unchecked
        {
            if ((SLOT[0].key != 0)) {
            SLOT[0].key &= key_clr;
            if ((!(SLOT[0].key != 0))) {
            SLOT[0].state = unchecked((byte)((((SLOT[0].volume & (~3)) == (((1 << (10 - 3)) - 1) & (~3))) ? 0 : 1)));
            }
            }
        }
    }
    private void CALC_FCSLOT(CPointer<YM2413_OPLL_CH> CH, CPointer<YM2413_OPLL_SLOT> SLOT)
    {
        unchecked
        {
            int ksr = default;
            uint SLOT_rs = default;
            uint SLOT_dp = default;
            SLOT[0].freq = (CH[0].fc * unchecked((uint)(SLOT[0].mul)));
            ksr = (unchecked((int)(CH[0].kcode)) >> unchecked((int)(SLOT[0].KSR)));
            if ((unchecked((int)(SLOT[0].ksr)) != ksr)) {
            SLOT[0].ksr = unchecked((byte)(ksr));
            if (((SLOT[0].ar + unchecked((uint)(SLOT[0].ksr))) >= unchecked((uint)((16 + 60))))) {
            SLOT[0].eg_sh_ar = unchecked((byte)(13));
            SLOT[0].eg_sel_ar = unchecked((ushort)((0 * 16)));
            } else if (((SLOT[0].ar + unchecked((uint)(SLOT[0].ksr))) >= unchecked((uint)((16 + 48))))) {
            SLOT[0].eg_sh_ar = unchecked((byte)(0));
            SLOT[0].eg_sel_ar = unchecked((ushort)((unchecked((int)(eg_rate_select[(int)((SLOT[0].ar + unchecked((uint)(SLOT[0].ksr))))])) + (4 * 16))));
            } else {
            SLOT[0].eg_sh_ar = eg_rate_shift[(int)((SLOT[0].ar + unchecked((uint)(SLOT[0].ksr))))];
            SLOT[0].eg_sel_ar = unchecked((ushort)(eg_rate_select[(int)((SLOT[0].ar + unchecked((uint)(SLOT[0].ksr))))]));
            }
            SLOT[0].eg_sh_dr = eg_rate_shift[(int)((SLOT[0].dr + unchecked((uint)(SLOT[0].ksr))))];
            SLOT[0].eg_sel_dr = eg_rate_select[(int)((SLOT[0].dr + unchecked((uint)(SLOT[0].ksr))))];
            SLOT[0].eg_sh_rr = eg_rate_shift[(int)((SLOT[0].rr + unchecked((uint)(SLOT[0].ksr))))];
            SLOT[0].eg_sel_rr = eg_rate_select[(int)((SLOT[0].rr + unchecked((uint)(SLOT[0].ksr))))];
            }
            if ((CH[0].sus != 0)) SLOT_rs = unchecked((uint)((16 + (5 << 2)))); else SLOT_rs = unchecked((uint)((16 + (7 << 2))));
            SLOT[0].eg_sh_rs = eg_rate_shift[(int)((SLOT_rs + unchecked((uint)(SLOT[0].ksr))))];
            SLOT[0].eg_sel_rs = eg_rate_select[(int)((SLOT_rs + unchecked((uint)(SLOT[0].ksr))))];
            SLOT_dp = unchecked((uint)((16 + (12 << 2))));
            SLOT[0].eg_sh_dp = eg_rate_shift[(int)((SLOT_dp + unchecked((uint)(SLOT[0].ksr))))];
            SLOT[0].eg_sel_dp = eg_rate_select[(int)((SLOT_dp + unchecked((uint)(SLOT[0].ksr))))];
        }
    }
    private void set_mul(int slot, int v)
    {
        unchecked
        {
            CPointer<YM2413_OPLL_CH> CH = new CPointer<YM2413_OPLL_CH>(ym2413.P_CH, (int)((slot / 2)));
            CPointer<YM2413_OPLL_SLOT> SLOT = new CPointer<YM2413_OPLL_SLOT>(CH[0].SLOT, (int)((slot & 1)));
            SLOT[0].mul = mul_tab[(int)((v & 15))];
            SLOT[0].KSR = unchecked((byte)((((v & 16) != 0) ? 0 : 2)));
            SLOT[0].eg_type = unchecked((byte)((v & 32)));
            SLOT[0].vib = unchecked((byte)((v & 64)));
            SLOT[0].AMmask = unchecked((uint)((((v & 128) != 0) ? (~0) : 0)));
            CALC_FCSLOT(CH, SLOT);
        }
    }
    private void set_ksl_tl(int chan, int v)
    {
        unchecked
        {
            CPointer<YM2413_OPLL_CH> CH = new CPointer<YM2413_OPLL_CH>(ym2413.P_CH, (int)(chan));
            CPointer<YM2413_OPLL_SLOT> SLOT = new CPointer<YM2413_OPLL_SLOT>(CH[0].SLOT, (int)(0));
            int ksl = (v >> 6);
            SLOT[0].ksl = unchecked((byte)(((ksl != 0) ? (3 - ksl) : 31)));
            SLOT[0].TL = unchecked((uint)(((v & 63) << ((10 - 2) - 7))));
            SLOT[0].TLL = unchecked((int)((SLOT[0].TL + (CH[0].ksl_base >> unchecked((int)(SLOT[0].ksl))))));
        }
    }
    private void set_ksl_wave_fb(int chan, int v)
    {
        unchecked
        {
            CPointer<YM2413_OPLL_CH> CH = new CPointer<YM2413_OPLL_CH>(ym2413.P_CH, (int)(chan));
            CPointer<YM2413_OPLL_SLOT> SLOT = new CPointer<YM2413_OPLL_SLOT>(CH[0].SLOT, (int)(0));
            SLOT[0].wavetable = unchecked((uint)((((v & 8) >> 3) * (1 << 10))));
            SLOT[0].fb_shift = unchecked((byte)((((v & 7) != 0) ? ((v & 7) + 8) : 0)));
            SLOT = new CPointer<YM2413_OPLL_SLOT>(CH[0].SLOT, (int)(1));
            SLOT[0].wavetable = unchecked((uint)((((v & 16) >> 4) * (1 << 10))));
            v >>= 6;
            SLOT[0].ksl = unchecked((byte)(((v != 0) ? (3 - v) : 31)));
            SLOT[0].TLL = unchecked((int)((SLOT[0].TL + (CH[0].ksl_base >> unchecked((int)(SLOT[0].ksl))))));
        }
    }
    private void set_ar_dr(int slot, int v)
    {
        unchecked
        {
            CPointer<YM2413_OPLL_CH> CH = new CPointer<YM2413_OPLL_CH>(ym2413.P_CH, (int)((slot / 2)));
            CPointer<YM2413_OPLL_SLOT> SLOT = new CPointer<YM2413_OPLL_SLOT>(CH[0].SLOT, (int)((slot & 1)));
            SLOT[0].ar = unchecked((uint)((((v >> 4) != 0) ? (16 + ((v >> 4) << 2)) : 0)));
            if (((SLOT[0].ar + unchecked((uint)(SLOT[0].ksr))) >= unchecked((uint)((16 + 60))))) {
            SLOT[0].eg_sh_ar = unchecked((byte)(13));
            SLOT[0].eg_sel_ar = unchecked((ushort)((0 * 16)));
            } else if (((SLOT[0].ar + unchecked((uint)(SLOT[0].ksr))) >= unchecked((uint)((16 + 48))))) {
            SLOT[0].eg_sh_ar = unchecked((byte)(0));
            SLOT[0].eg_sel_ar = unchecked((ushort)((unchecked((int)(eg_rate_select[(int)((SLOT[0].ar + unchecked((uint)(SLOT[0].ksr))))])) + (4 * 16))));
            } else {
            SLOT[0].eg_sh_ar = eg_rate_shift[(int)((SLOT[0].ar + unchecked((uint)(SLOT[0].ksr))))];
            SLOT[0].eg_sel_ar = unchecked((ushort)(eg_rate_select[(int)((SLOT[0].ar + unchecked((uint)(SLOT[0].ksr))))]));
            }
            SLOT[0].dr = unchecked((uint)((((v & 15) != 0) ? (16 + ((v & 15) << 2)) : 0)));
            SLOT[0].eg_sh_dr = eg_rate_shift[(int)((SLOT[0].dr + unchecked((uint)(SLOT[0].ksr))))];
            SLOT[0].eg_sel_dr = eg_rate_select[(int)((SLOT[0].dr + unchecked((uint)(SLOT[0].ksr))))];
        }
    }
    private void set_sl_rr(int slot, int v)
    {
        unchecked
        {
            CPointer<YM2413_OPLL_CH> CH = new CPointer<YM2413_OPLL_CH>(ym2413.P_CH, (int)((slot / 2)));
            CPointer<YM2413_OPLL_SLOT> SLOT = new CPointer<YM2413_OPLL_SLOT>(CH[0].SLOT, (int)((slot & 1)));
            SLOT[0].sl = sl_tab[(int)((v >> 4))];
            SLOT[0].rr = unchecked((uint)((((v & 15) != 0) ? (16 + ((v & 15) << 2)) : 0)));
            SLOT[0].eg_sh_rr = eg_rate_shift[(int)((SLOT[0].rr + unchecked((uint)(SLOT[0].ksr))))];
            SLOT[0].eg_sel_rr = eg_rate_select[(int)((SLOT[0].rr + unchecked((uint)(SLOT[0].ksr))))];
        }
    }
    private void load_instrument(uint chan, uint slot, CPointer<byte> inst)
    {
        unchecked
        {
            set_mul(unchecked((int)(slot)), unchecked((int)(inst[(int)(0)])));
            set_mul(unchecked((int)((slot + unchecked((uint)(1))))), unchecked((int)(inst[(int)(1)])));
            set_ksl_tl(unchecked((int)(chan)), unchecked((int)(inst[(int)(2)])));
            set_ksl_wave_fb(unchecked((int)(chan)), unchecked((int)(inst[(int)(3)])));
            set_ar_dr(unchecked((int)(slot)), unchecked((int)(inst[(int)(4)])));
            set_ar_dr(unchecked((int)((slot + unchecked((uint)(1))))), unchecked((int)(inst[(int)(5)])));
            set_sl_rr(unchecked((int)(slot)), unchecked((int)(inst[(int)(6)])));
            set_sl_rr(unchecked((int)((slot + unchecked((uint)(1))))), unchecked((int)(inst[(int)(7)])));
        }
    }
    private void update_instrument_zero(byte r)
    {
        unchecked
        {
            CPointer<byte> inst = (new CPointer<byte>(ym2413.inst_tab, (int)(0) * 8) + (int)(0));
            uint chan = default;
            uint chan_max = unchecked((uint)(9));
            if (((unchecked((int)(ym2413.rhythm)) & 32) != 0)) chan_max = unchecked((uint)(6));
            switch ((unchecked((int)(r)) & 7)) {
            case 0:
            for (chan = unchecked((uint)(0)); (chan < chan_max); chan++) {
            if (((unchecked((int)(ym2413.instvol_r[(int)(chan)])) & 240) == 0)) {
            set_mul(unchecked((int)((chan * unchecked((uint)(2))))), unchecked((int)(inst[(int)(0)])));
            }
            }
            break;
            case 1:
            for (chan = unchecked((uint)(0)); (chan < chan_max); chan++) {
            if (((unchecked((int)(ym2413.instvol_r[(int)(chan)])) & 240) == 0)) {
            set_mul(unchecked((int)(((chan * unchecked((uint)(2))) + unchecked((uint)(1))))), unchecked((int)(inst[(int)(1)])));
            }
            }
            break;
            case 2:
            for (chan = unchecked((uint)(0)); (chan < chan_max); chan++) {
            if (((unchecked((int)(ym2413.instvol_r[(int)(chan)])) & 240) == 0)) {
            set_ksl_tl(unchecked((int)(chan)), unchecked((int)(inst[(int)(2)])));
            }
            }
            break;
            case 3:
            for (chan = unchecked((uint)(0)); (chan < chan_max); chan++) {
            if (((unchecked((int)(ym2413.instvol_r[(int)(chan)])) & 240) == 0)) {
            set_ksl_wave_fb(unchecked((int)(chan)), unchecked((int)(inst[(int)(3)])));
            }
            }
            break;
            case 4:
            for (chan = unchecked((uint)(0)); (chan < chan_max); chan++) {
            if (((unchecked((int)(ym2413.instvol_r[(int)(chan)])) & 240) == 0)) {
            set_ar_dr(unchecked((int)((chan * unchecked((uint)(2))))), unchecked((int)(inst[(int)(4)])));
            }
            }
            break;
            case 5:
            for (chan = unchecked((uint)(0)); (chan < chan_max); chan++) {
            if (((unchecked((int)(ym2413.instvol_r[(int)(chan)])) & 240) == 0)) {
            set_ar_dr(unchecked((int)(((chan * unchecked((uint)(2))) + unchecked((uint)(1))))), unchecked((int)(inst[(int)(5)])));
            }
            }
            break;
            case 6:
            for (chan = unchecked((uint)(0)); (chan < chan_max); chan++) {
            if (((unchecked((int)(ym2413.instvol_r[(int)(chan)])) & 240) == 0)) {
            set_sl_rr(unchecked((int)((chan * unchecked((uint)(2))))), unchecked((int)(inst[(int)(6)])));
            }
            }
            break;
            case 7:
            for (chan = unchecked((uint)(0)); (chan < chan_max); chan++) {
            if (((unchecked((int)(ym2413.instvol_r[(int)(chan)])) & 240) == 0)) {
            set_sl_rr(unchecked((int)(((chan * unchecked((uint)(2))) + unchecked((uint)(1))))), unchecked((int)(inst[(int)(7)])));
            }
            }
            break;
            }
        }
    }
    private void OPLLWriteReg(int r, int v)
    {
        unchecked
        {
            CPointer<YM2413_OPLL_CH> CH = default;
            CPointer<YM2413_OPLL_SLOT> SLOT = default;
            r &= 255;
            v &= 255;
            switch ((r & 240)) {
            case 0:
            {
            switch ((r & 15)) {
            case 0:
            case 1:
            case 2:
            case 3:
            case 4:
            case 5:
            case 6:
            case 7:
            {
            new CPointer<byte>(ym2413.inst_tab, (int)(0) * 8)[(int)(r)] = unchecked((byte)(v));
            update_instrument_zero(unchecked((byte)(r)));
            break;
            }
            case 14:
            {
            if (((v & 32) != 0)) {
            if (((unchecked((int)(ym2413.rhythm)) & 32) == 0)) {
            load_instrument(unchecked((uint)(6)), unchecked((uint)(12)), (new CPointer<byte>(ym2413.inst_tab, (int)(16) * 8) + (int)(0)));
            load_instrument(unchecked((uint)(7)), unchecked((uint)(14)), (new CPointer<byte>(ym2413.inst_tab, (int)(17) * 8) + (int)(0)));
            CH = new CPointer<YM2413_OPLL_CH>(ym2413.P_CH, (int)(7));
            SLOT = new CPointer<YM2413_OPLL_SLOT>(CH[0].SLOT, (int)(0));
            SLOT[0].TL = unchecked((uint)((((unchecked((int)(ym2413.instvol_r[(int)(7)])) >> 4) << 2) << ((10 - 2) - 7))));
            SLOT[0].TLL = unchecked((int)((SLOT[0].TL + (CH[0].ksl_base >> unchecked((int)(SLOT[0].ksl))))));
            load_instrument(unchecked((uint)(8)), unchecked((uint)(16)), (new CPointer<byte>(ym2413.inst_tab, (int)(18) * 8) + (int)(0)));
            CH = new CPointer<YM2413_OPLL_CH>(ym2413.P_CH, (int)(8));
            SLOT = new CPointer<YM2413_OPLL_SLOT>(CH[0].SLOT, (int)(0));
            SLOT[0].TL = unchecked((uint)((((unchecked((int)(ym2413.instvol_r[(int)(8)])) >> 4) << 2) << ((10 - 2) - 7))));
            SLOT[0].TLL = unchecked((int)((SLOT[0].TL + (CH[0].ksl_base >> unchecked((int)(SLOT[0].ksl))))));
            }
            if (((v & 16) != 0)) {
            KEY_ON(new CPointer<YM2413_OPLL_SLOT>(ym2413.P_CH[(int)(6)].SLOT, (int)(0)), unchecked((uint)(2)));
            KEY_ON(new CPointer<YM2413_OPLL_SLOT>(ym2413.P_CH[(int)(6)].SLOT, (int)(1)), unchecked((uint)(2)));
            } else {
            KEY_OFF(new CPointer<YM2413_OPLL_SLOT>(ym2413.P_CH[(int)(6)].SLOT, (int)(0)), unchecked((uint)((~2))));
            KEY_OFF(new CPointer<YM2413_OPLL_SLOT>(ym2413.P_CH[(int)(6)].SLOT, (int)(1)), unchecked((uint)((~2))));
            }
            if (((v & 1) != 0)) KEY_ON(new CPointer<YM2413_OPLL_SLOT>(ym2413.P_CH[(int)(7)].SLOT, (int)(0)), unchecked((uint)(2))); else KEY_OFF(new CPointer<YM2413_OPLL_SLOT>(ym2413.P_CH[(int)(7)].SLOT, (int)(0)), unchecked((uint)((~2))));
            if (((v & 8) != 0)) KEY_ON(new CPointer<YM2413_OPLL_SLOT>(ym2413.P_CH[(int)(7)].SLOT, (int)(1)), unchecked((uint)(2))); else KEY_OFF(new CPointer<YM2413_OPLL_SLOT>(ym2413.P_CH[(int)(7)].SLOT, (int)(1)), unchecked((uint)((~2))));
            if (((v & 4) != 0)) KEY_ON(new CPointer<YM2413_OPLL_SLOT>(ym2413.P_CH[(int)(8)].SLOT, (int)(0)), unchecked((uint)(2))); else KEY_OFF(new CPointer<YM2413_OPLL_SLOT>(ym2413.P_CH[(int)(8)].SLOT, (int)(0)), unchecked((uint)((~2))));
            if (((v & 2) != 0)) KEY_ON(new CPointer<YM2413_OPLL_SLOT>(ym2413.P_CH[(int)(8)].SLOT, (int)(1)), unchecked((uint)(2))); else KEY_OFF(new CPointer<YM2413_OPLL_SLOT>(ym2413.P_CH[(int)(8)].SLOT, (int)(1)), unchecked((uint)((~2))));
            } else {
            if (((unchecked((int)(ym2413.rhythm)) & 32) != 0)) {
            load_instrument(unchecked((uint)(6)), unchecked((uint)(12)), (new CPointer<byte>(ym2413.inst_tab, (int)((unchecked((int)(ym2413.instvol_r[(int)(6)])) >> 4)) * 8) + (int)(0)));
            load_instrument(unchecked((uint)(7)), unchecked((uint)(14)), (new CPointer<byte>(ym2413.inst_tab, (int)((unchecked((int)(ym2413.instvol_r[(int)(7)])) >> 4)) * 8) + (int)(0)));
            load_instrument(unchecked((uint)(8)), unchecked((uint)(16)), (new CPointer<byte>(ym2413.inst_tab, (int)((unchecked((int)(ym2413.instvol_r[(int)(8)])) >> 4)) * 8) + (int)(0)));
            }
            KEY_OFF(new CPointer<YM2413_OPLL_SLOT>(ym2413.P_CH[(int)(6)].SLOT, (int)(0)), unchecked((uint)((~2))));
            KEY_OFF(new CPointer<YM2413_OPLL_SLOT>(ym2413.P_CH[(int)(6)].SLOT, (int)(1)), unchecked((uint)((~2))));
            KEY_OFF(new CPointer<YM2413_OPLL_SLOT>(ym2413.P_CH[(int)(7)].SLOT, (int)(0)), unchecked((uint)((~2))));
            KEY_OFF(new CPointer<YM2413_OPLL_SLOT>(ym2413.P_CH[(int)(7)].SLOT, (int)(1)), unchecked((uint)((~2))));
            KEY_OFF(new CPointer<YM2413_OPLL_SLOT>(ym2413.P_CH[(int)(8)].SLOT, (int)(0)), unchecked((uint)((~2))));
            KEY_OFF(new CPointer<YM2413_OPLL_SLOT>(ym2413.P_CH[(int)(8)].SLOT, (int)(1)), unchecked((uint)((~2))));
            }
            ym2413.rhythm = unchecked((byte)((v & 63)));
            break;
            }
            }
            break;
            }
            case 16:
            case 32:
            {
            int block_fnum = default;
            int chan = (r & 15);
            if ((chan >= 9)) chan -= 9;
            CH = new CPointer<YM2413_OPLL_CH>(ym2413.P_CH, (int)(chan));
            if (((r & 16) != 0)) {
            block_fnum = unchecked((int)(((CH[0].block_fnum & unchecked((uint)(3840))) | unchecked((uint)(v)))));
            } else {
            block_fnum = unchecked((int)((unchecked((uint)(((v & 15) << 8))) | (CH[0].block_fnum & unchecked((uint)(255))))));
            if (((v & 16) != 0)) {
            KEY_ON(new CPointer<YM2413_OPLL_SLOT>(CH[0].SLOT, (int)(0)), unchecked((uint)(1)));
            KEY_ON(new CPointer<YM2413_OPLL_SLOT>(CH[0].SLOT, (int)(1)), unchecked((uint)(1)));
            } else {
            KEY_OFF(new CPointer<YM2413_OPLL_SLOT>(CH[0].SLOT, (int)(0)), unchecked((uint)((~1))));
            KEY_OFF(new CPointer<YM2413_OPLL_SLOT>(CH[0].SLOT, (int)(1)), unchecked((uint)((~1))));
            }
            CH[0].sus = unchecked((byte)((v & 32)));
            }
            if ((CH[0].block_fnum != unchecked((uint)(block_fnum)))) {
            byte block = default;
            CH[0].block_fnum = unchecked((uint)(block_fnum));
            CH[0].kcode = unchecked((byte)(((block_fnum & 3840) >> 8)));
            CH[0].ksl_base = ksl_tab[(int)((block_fnum >> 5))];
            block_fnum = (block_fnum * 2);
            block = unchecked((byte)(((block_fnum & 7168) >> 10)));
            CH[0].fc = (ym2413.fn_tab[(int)((block_fnum & 1023))] >> (7 - unchecked((int)(block))));
            CH[0].SLOT[(int)(0)].TLL = unchecked((int)((CH[0].SLOT[(int)(0)].TL + (CH[0].ksl_base >> unchecked((int)(CH[0].SLOT[(int)(0)].ksl))))));
            CH[0].SLOT[(int)(1)].TLL = unchecked((int)((CH[0].SLOT[(int)(1)].TL + (CH[0].ksl_base >> unchecked((int)(CH[0].SLOT[(int)(1)].ksl))))));
            CALC_FCSLOT(CH, new CPointer<YM2413_OPLL_SLOT>(CH[0].SLOT, (int)(0)));
            CALC_FCSLOT(CH, new CPointer<YM2413_OPLL_SLOT>(CH[0].SLOT, (int)(1)));
            }
            break;
            }
            case 48:
            {
            int chan = (r & 15);
            if ((chan >= 9)) chan -= 9;
            CH = new CPointer<YM2413_OPLL_CH>(ym2413.P_CH, (int)(chan));
            SLOT = new CPointer<YM2413_OPLL_SLOT>(CH[0].SLOT, (int)(1));
            SLOT[0].TL = unchecked((uint)((((v & 15) << 2) << ((10 - 2) - 7))));
            SLOT[0].TLL = unchecked((int)((SLOT[0].TL + (CH[0].ksl_base >> unchecked((int)(SLOT[0].ksl))))));
            if (((chan >= 6) && ((unchecked((int)(ym2413.rhythm)) & 32) != 0))) {
            if ((chan >= 7)) {
            SLOT = new CPointer<YM2413_OPLL_SLOT>(CH[0].SLOT, (int)(0));
            SLOT[0].TL = unchecked((uint)((((v >> 4) << 2) << ((10 - 2) - 7))));
            SLOT[0].TLL = unchecked((int)((SLOT[0].TL + (CH[0].ksl_base >> unchecked((int)(SLOT[0].ksl))))));
            }
            } else {
            if (((unchecked((int)(ym2413.instvol_r[(int)(chan)])) & 240) != (v & 240))) {
            ym2413.instvol_r[(int)(chan)] = unchecked((byte)(v));
            load_instrument(unchecked((uint)(chan)), unchecked((uint)((chan * 2))), (new CPointer<byte>(ym2413.inst_tab, (int)((v >> 4)) * 8) + (int)(0)));
            }
            }
            break;
            }
            default:
            break;
            }
        }
    }
    private void YM2413ResetChip()
    {
        unchecked
        {
            int c = default;
            int s = default;
            int i = default;
            ym2413.eg_timer = unchecked((uint)(0));
            ym2413.eg_cnt = unchecked((uint)(0));
            ym2413.noise_rng = unchecked((uint)(1));
            for (i = 0; (i < 19); i++) {
            for (c = 0; (c < 8); c++) {
            new CPointer<byte>(ym2413.inst_tab, (int)(i) * 8)[(int)(c)] = new CPointer<byte>(table, (int)(i) * 8)[(int)(c)];
            }
            }
            OPLLWriteReg(15, 0);
            for (i = 63; (i >= 16); i--) OPLLWriteReg(i, 0);
            for (c = 0; (c < 9); c++) {
            CPointer<YM2413_OPLL_CH> CH = new CPointer<YM2413_OPLL_CH>(ym2413.P_CH, (int)(c));
            for (s = 0; (s < 2); s++) {
            CH[0].SLOT[(int)(s)].wavetable = unchecked((uint)(0));
            CH[0].SLOT[(int)(s)].state = unchecked((byte)(0));
            CH[0].SLOT[(int)(s)].volume = ((1 << (10 - 3)) - 1);
            }
            }
        }
    }
    private void YM2413Write(uint a, uint v)
    {
        unchecked
        {
            if ((!((a & unchecked((uint)(2))) != 0))) {
            if ((!((a & unchecked((uint)(1))) != 0))) {
            ym2413.address = unchecked((byte)((v & unchecked((uint)(255)))));
            } else {
            OPLLWriteReg(unchecked((int)(ym2413.address)), unchecked((int)(v)));
            }
            } else {
            ym2413.status = unchecked((byte)((v & unchecked((uint)(1)))));
            }
        }
    }
    private uint YM2413Read()
    {
        unchecked
        {
            return unchecked((uint)((248 | unchecked((int)(ym2413.status)))));
        }
    }
    private void YM2413Update(CPointer<int> buffer, int length)
    {
        unchecked
        {
            int i = default;
            int sampleOutput = default;
            for (i = 0; (i < length); i++) {
            output[(int)(0)] = 0;
            output[(int)(1)] = 0;
            advance_lfo();
            MixChannel(0);
            MixChannel(1);
            MixChannel(2);
            MixChannel(3);
            MixChannel(4);
            MixChannel(5);
            if ((!((unchecked((int)(ym2413.rhythm)) & 32) != 0))) {
            MixChannel(6);
            MixChannel(7);
            MixChannel(8);
            } else {
            rhythm_calc(new CPointer<YM2413_OPLL_CH>(ym2413.P_CH, (int)(0)), ((ym2413.noise_rng >> 0) & unchecked((uint)(1))));
            }
            sampleOutput = (((output[(int)(0)] + (output[(int)(1)] * 2)) * 2) * unchecked((int)(ym2413.status)));
            (buffer++)[0] = sampleOutput;
            (buffer++)[0] = sampleOutput;
            advance();
            }
        }
    }
}
