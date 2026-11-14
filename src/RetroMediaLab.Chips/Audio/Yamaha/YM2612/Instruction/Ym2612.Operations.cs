using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Chips.Audio.Yamaha.YM2612.Instruction;

public sealed partial class Ym2612
{
    private void FM_KEYON(ArrayPointer<FM_CH> CH, int s)
    {
        unchecked
        {
            ArrayPointer<FM_SLOT> SLOT = new ArrayPointer<FM_SLOT>(CH[0].SLOT, (int)(s));
            if (((!(SLOT[0].key != 0)) && (!(ym2612.OPN.SL3.key_csm != 0)))) {
            SLOT[0].phase = unchecked((uint)(0));
            SLOT[0].ssgn = unchecked((byte)(0));
            if (((SLOT[0].ar + unchecked((uint)(SLOT[0].ksr))) < unchecked((uint)(94)))) {
            SLOT[0].state = unchecked((byte)(((SLOT[0].volume <= 0) ? ((SLOT[0].sl == unchecked((uint)(0))) ? 2 : 3) : 4)));
            } else {
            SLOT[0].volume = 0;
            SLOT[0].state = unchecked((byte)(((SLOT[0].sl == unchecked((uint)(0))) ? 2 : 3)));
            }
            if ((((unchecked((int)(SLOT[0].ssg)) & 8) != 0) && ((unchecked((int)(SLOT[0].ssgn)) ^ (unchecked((int)(SLOT[0].ssg)) & 4)) != 0))) SLOT[0].vol_out = ((unchecked((uint)((512 - SLOT[0].volume))) & unchecked((uint)(((1 << 10) - 1)))) + SLOT[0].tl); else SLOT[0].vol_out = (unchecked((uint)(SLOT[0].volume)) + SLOT[0].tl);
            }
            SLOT[0].key = unchecked((byte)(1));
        }
    }
    private void FM_KEYOFF(ArrayPointer<FM_CH> CH, int s)
    {
        unchecked
        {
            ArrayPointer<FM_SLOT> SLOT = new ArrayPointer<FM_SLOT>(CH[0].SLOT, (int)(s));
            if (((SLOT[0].key != 0) && (!(ym2612.OPN.SL3.key_csm != 0)))) {
            if ((unchecked((int)(SLOT[0].state)) > 1)) {
            SLOT[0].state = unchecked((byte)(1));
            if (((unchecked((int)(SLOT[0].ssg)) & 8) != 0)) {
            if (((unchecked((int)(SLOT[0].ssgn)) ^ (unchecked((int)(SLOT[0].ssg)) & 4)) != 0)) SLOT[0].volume = ((512 - SLOT[0].volume) & ((1 << 10) - 1));
            if ((SLOT[0].volume >= 512)) {
            SLOT[0].volume = ((1 << 10) - 1);
            SLOT[0].state = unchecked((byte)(0));
            }
            SLOT[0].vol_out = (unchecked((uint)(SLOT[0].volume)) + SLOT[0].tl);
            }
            }
            }
            SLOT[0].key = unchecked((byte)(0));
        }
    }
    private void FM_KEYON_CSM(ArrayPointer<FM_CH> CH, int s)
    {
        unchecked
        {
            ArrayPointer<FM_SLOT> SLOT = new ArrayPointer<FM_SLOT>(CH[0].SLOT, (int)(s));
            if (((!(SLOT[0].key != 0)) && (!(ym2612.OPN.SL3.key_csm != 0)))) {
            SLOT[0].phase = unchecked((uint)(0));
            SLOT[0].ssgn = unchecked((byte)(0));
            if (((SLOT[0].ar + unchecked((uint)(SLOT[0].ksr))) < unchecked((uint)(94)))) {
            SLOT[0].state = unchecked((byte)(((SLOT[0].volume <= 0) ? ((SLOT[0].sl == unchecked((uint)(0))) ? 2 : 3) : 4)));
            } else {
            SLOT[0].volume = 0;
            SLOT[0].state = unchecked((byte)(((SLOT[0].sl == unchecked((uint)(0))) ? 2 : 3)));
            }
            if ((((unchecked((int)(SLOT[0].ssg)) & 8) != 0) && ((unchecked((int)(SLOT[0].ssgn)) ^ (unchecked((int)(SLOT[0].ssg)) & 4)) != 0))) SLOT[0].vol_out = ((unchecked((uint)((512 - SLOT[0].volume))) & unchecked((uint)(((1 << 10) - 1)))) + SLOT[0].tl); else SLOT[0].vol_out = (unchecked((uint)(SLOT[0].volume)) + SLOT[0].tl);
            }
        }
    }
    private void FM_KEYOFF_CSM(ArrayPointer<FM_CH> CH, int s)
    {
        unchecked
        {
            ArrayPointer<FM_SLOT> SLOT = new ArrayPointer<FM_SLOT>(CH[0].SLOT, (int)(s));
            if ((!(SLOT[0].key != 0))) {
            if ((unchecked((int)(SLOT[0].state)) > 1)) {
            SLOT[0].state = unchecked((byte)(1));
            if (((unchecked((int)(SLOT[0].ssg)) & 8) != 0)) {
            if (((unchecked((int)(SLOT[0].ssgn)) ^ (unchecked((int)(SLOT[0].ssg)) & 4)) != 0)) SLOT[0].volume = ((512 - SLOT[0].volume) & ((1 << 10) - 1));
            if ((SLOT[0].volume >= 512)) {
            SLOT[0].volume = ((1 << 10) - 1);
            SLOT[0].state = unchecked((byte)(0));
            }
            SLOT[0].vol_out = (unchecked((uint)(SLOT[0].volume)) + SLOT[0].tl);
            }
            }
            }
        }
    }
    private void CSMKeyControll(ArrayPointer<FM_CH> CH)
    {
        unchecked
        {
            FM_KEYON_CSM(CH, 0);
            FM_KEYON_CSM(CH, 2);
            FM_KEYON_CSM(CH, 1);
            FM_KEYON_CSM(CH, 3);
            ym2612.OPN.SL3.key_csm = unchecked((byte)(1));
        }
    }
    private void INTERNAL_TIMER_A()
    {
        unchecked
        {
            if (((ym2612.OPN.ST.mode & unchecked((uint)(1))) != 0)) {
            ym2612.OPN.ST.TAC--;
            if ((ym2612.OPN.ST.TAC <= 0)) {
            if (((ym2612.OPN.ST.mode & unchecked((uint)(4))) != 0)) ym2612.OPN.ST.status |= unchecked((byte)(1));
            ym2612.OPN.ST.TAC = ym2612.OPN.ST.TAL;
            if (((ym2612.OPN.ST.mode & unchecked((uint)(192))) == unchecked((uint)(128)))) CSMKeyControll(new ArrayPointer<FM_CH>(ym2612.CH, (int)(2)));
            }
            }
        }
    }
    private void INTERNAL_TIMER_B(int step)
    {
        unchecked
        {
            if (((ym2612.OPN.ST.mode & unchecked((uint)(2))) != 0)) {
            ym2612.OPN.ST.TBC -= step;
            if ((ym2612.OPN.ST.TBC <= 0)) {
            if (((ym2612.OPN.ST.mode & unchecked((uint)(8))) != 0)) ym2612.OPN.ST.status |= unchecked((byte)(2));
            do {
            ym2612.OPN.ST.TBC += ym2612.OPN.ST.TBL;
            } while ((ym2612.OPN.ST.TBC <= 0));
            }
            }
        }
    }
    private void set_timers(int v)
    {
        unchecked
        {
            if ((((ym2612.OPN.ST.mode ^ unchecked((uint)(v))) & unchecked((uint)(192))) != 0)) {
            ym2612.CH[(int)(2)].SLOT[(int)(0)].Incr = (-1);
            if ((((v & 192) != 128) && (ym2612.OPN.SL3.key_csm != 0))) {
            FM_KEYOFF_CSM(new ArrayPointer<FM_CH>(ym2612.CH, (int)(2)), 0);
            FM_KEYOFF_CSM(new ArrayPointer<FM_CH>(ym2612.CH, (int)(2)), 2);
            FM_KEYOFF_CSM(new ArrayPointer<FM_CH>(ym2612.CH, (int)(2)), 1);
            FM_KEYOFF_CSM(new ArrayPointer<FM_CH>(ym2612.CH, (int)(2)), 3);
            ym2612.OPN.SL3.key_csm = unchecked((byte)(0));
            }
            }
            if ((((v & 1) != 0) && (!((ym2612.OPN.ST.mode & unchecked((uint)(1))) != 0)))) ym2612.OPN.ST.TAC = ym2612.OPN.ST.TAL;
            if ((((v & 2) != 0) && (!((ym2612.OPN.ST.mode & unchecked((uint)(2))) != 0)))) ym2612.OPN.ST.TBC = ym2612.OPN.ST.TBL;
            ym2612.OPN.ST.status &= unchecked((byte)(((~v) >> 4)));
            ym2612.OPN.ST.mode = unchecked((uint)(v));
        }
    }
    private void set_det_mul(ArrayPointer<FM_CH> CH, ArrayPointer<FM_SLOT> SLOT, int v)
    {
        unchecked
        {
            SLOT[0].mul = unchecked((uint)((((v & 15) != 0) ? ((v & 15) * 2) : 1)));
            SLOT[0].DT = new ArrayPointer<int>(ym2612.OPN.ST.dt_tab, (int)(((v >> 4) & 7)) * 32);
            CH[0].SLOT[(int)(0)].Incr = (-1);
        }
    }
    private void set_tl(ArrayPointer<FM_SLOT> SLOT, int v)
    {
        unchecked
        {
            SLOT[0].tl = unchecked((uint)(((v & 127) << (10 - 7))));
            if (((((unchecked((int)(SLOT[0].ssg)) & 8) != 0) && ((unchecked((int)(SLOT[0].ssgn)) ^ (unchecked((int)(SLOT[0].ssg)) & 4)) != 0)) && (unchecked((int)(SLOT[0].state)) > 1))) SLOT[0].vol_out = ((unchecked((uint)((512 - SLOT[0].volume))) & unchecked((uint)(((1 << 10) - 1)))) + SLOT[0].tl); else SLOT[0].vol_out = (unchecked((uint)(SLOT[0].volume)) + SLOT[0].tl);
        }
    }
    private void set_ar_ksr(ArrayPointer<FM_CH> CH, ArrayPointer<FM_SLOT> SLOT, int v)
    {
        unchecked
        {
            byte old_KSR = SLOT[0].KSR;
            SLOT[0].ar = unchecked((uint)((((v & 31) != 0) ? (32 + ((v & 31) << 1)) : 0)));
            SLOT[0].KSR = unchecked((byte)((3 - (v >> 6))));
            if ((unchecked((int)(SLOT[0].KSR)) != unchecked((int)(old_KSR)))) {
            CH[0].SLOT[(int)(0)].Incr = (-1);
            }
            if (((SLOT[0].ar + unchecked((uint)(SLOT[0].ksr))) < unchecked((uint)((32 + 62))))) {
            SLOT[0].eg_sh_ar = eg_rate_shift[(int)((SLOT[0].ar + unchecked((uint)(SLOT[0].ksr))))];
            SLOT[0].eg_sel_ar = eg_rate_select[(int)((SLOT[0].ar + unchecked((uint)(SLOT[0].ksr))))];
            } else {
            SLOT[0].eg_sh_ar = unchecked((byte)(0));
            SLOT[0].eg_sel_ar = unchecked((byte)((18 * 8)));
            }
        }
    }
    private void set_dr(ArrayPointer<FM_SLOT> SLOT, int v)
    {
        unchecked
        {
            SLOT[0].d1r = unchecked((uint)((((v & 31) != 0) ? (32 + ((v & 31) << 1)) : 0)));
            SLOT[0].eg_sh_d1r = eg_rate_shift[(int)((SLOT[0].d1r + unchecked((uint)(SLOT[0].ksr))))];
            SLOT[0].eg_sel_d1r = eg_rate_select[(int)((SLOT[0].d1r + unchecked((uint)(SLOT[0].ksr))))];
        }
    }
    private void set_sr(ArrayPointer<FM_SLOT> SLOT, int v)
    {
        unchecked
        {
            SLOT[0].d2r = unchecked((uint)((((v & 31) != 0) ? (32 + ((v & 31) << 1)) : 0)));
            SLOT[0].eg_sh_d2r = eg_rate_shift[(int)((SLOT[0].d2r + unchecked((uint)(SLOT[0].ksr))))];
            SLOT[0].eg_sel_d2r = eg_rate_select[(int)((SLOT[0].d2r + unchecked((uint)(SLOT[0].ksr))))];
        }
    }
    private void set_sl_rr(ArrayPointer<FM_SLOT> SLOT, int v)
    {
        unchecked
        {
            SLOT[0].sl = sl_table[(int)((v >> 4))];
            if (((unchecked((int)(SLOT[0].state)) == 3) && (SLOT[0].volume >= unchecked((int)(SLOT[0].sl))))) SLOT[0].state = unchecked((byte)(2));
            SLOT[0].rr = unchecked((uint)((34 + ((v & 15) << 2))));
            SLOT[0].eg_sh_rr = eg_rate_shift[(int)((SLOT[0].rr + unchecked((uint)(SLOT[0].ksr))))];
            SLOT[0].eg_sel_rr = eg_rate_select[(int)((SLOT[0].rr + unchecked((uint)(SLOT[0].ksr))))];
        }
    }
    private void advance_lfo()
    {
        unchecked
        {
            if ((ym2612.OPN.lfo_timer_overflow != 0)) {
            ym2612.OPN.lfo_timer++;
            if ((ym2612.OPN.lfo_timer >= ym2612.OPN.lfo_timer_overflow)) {
            ym2612.OPN.lfo_timer = unchecked((uint)(0));
            ym2612.OPN.lfo_cnt = unchecked((byte)(((unchecked((int)(ym2612.OPN.lfo_cnt)) + 1) & 127)));
            if ((unchecked((int)(ym2612.OPN.lfo_cnt)) < 64)) ym2612.OPN.LFO_AM = unchecked((uint)(((unchecked((int)(ym2612.OPN.lfo_cnt)) ^ 63) << 1))); else ym2612.OPN.LFO_AM = unchecked((uint)(((unchecked((int)(ym2612.OPN.lfo_cnt)) & 63) << 1)));
            ym2612.OPN.LFO_PM = unchecked((uint)((unchecked((int)(ym2612.OPN.lfo_cnt)) >> 2)));
            }
            }
        }
    }
    private void advance_eg_channels(ArrayPointer<FM_CH> CH, uint eg_cnt)
    {
        unchecked
        {
            uint i = unchecked((uint)(6));
            uint j = default;
            ArrayPointer<FM_SLOT> SLOT = default;
            do {
            SLOT = new ArrayPointer<FM_SLOT>(CH[0].SLOT, (int)(0));
            j = unchecked((uint)(4));
            do {
            switch (SLOT[0].state) {
            case 4:
            {
            if ((!((eg_cnt & unchecked((uint)(((1 << unchecked((int)(SLOT[0].eg_sh_ar))) - 1)))) != 0))) {
            SLOT[0].volume += (((~SLOT[0].volume) * unchecked((int)(eg_inc[(int)((unchecked((uint)(SLOT[0].eg_sel_ar)) + ((eg_cnt >> unchecked((int)(SLOT[0].eg_sh_ar))) & unchecked((uint)(7)))))]))) >> 4);
            if ((SLOT[0].volume <= 0)) {
            SLOT[0].volume = 0;
            SLOT[0].state = unchecked((byte)(((SLOT[0].sl == unchecked((uint)(0))) ? 2 : 3)));
            }
            if ((((unchecked((int)(SLOT[0].ssg)) & 8) != 0) && ((unchecked((int)(SLOT[0].ssgn)) ^ (unchecked((int)(SLOT[0].ssg)) & 4)) != 0))) SLOT[0].vol_out = ((unchecked((uint)((512 - SLOT[0].volume))) & unchecked((uint)(((1 << 10) - 1)))) + SLOT[0].tl); else SLOT[0].vol_out = (unchecked((uint)(SLOT[0].volume)) + SLOT[0].tl);
            }
            break;
            }
            case 3:
            {
            if ((!((eg_cnt & unchecked((uint)(((1 << unchecked((int)(SLOT[0].eg_sh_d1r))) - 1)))) != 0))) {
            if (((unchecked((int)(SLOT[0].ssg)) & 8) != 0)) {
            if ((SLOT[0].volume < 512)) {
            SLOT[0].volume += (4 * unchecked((int)(eg_inc[(int)((unchecked((uint)(SLOT[0].eg_sel_d1r)) + ((eg_cnt >> unchecked((int)(SLOT[0].eg_sh_d1r))) & unchecked((uint)(7)))))])));
            if (((unchecked((int)(SLOT[0].ssgn)) ^ (unchecked((int)(SLOT[0].ssg)) & 4)) != 0)) SLOT[0].vol_out = ((unchecked((uint)((512 - SLOT[0].volume))) & unchecked((uint)(((1 << 10) - 1)))) + SLOT[0].tl); else SLOT[0].vol_out = (unchecked((uint)(SLOT[0].volume)) + SLOT[0].tl);
            }
            } else {
            SLOT[0].volume += unchecked((int)(eg_inc[(int)((unchecked((uint)(SLOT[0].eg_sel_d1r)) + ((eg_cnt >> unchecked((int)(SLOT[0].eg_sh_d1r))) & unchecked((uint)(7)))))]));
            SLOT[0].vol_out = (unchecked((uint)(SLOT[0].volume)) + SLOT[0].tl);
            }
            if ((SLOT[0].volume >= unchecked((int)(SLOT[0].sl)))) SLOT[0].state = unchecked((byte)(2));
            }
            break;
            }
            case 2:
            {
            if ((!((eg_cnt & unchecked((uint)(((1 << unchecked((int)(SLOT[0].eg_sh_d2r))) - 1)))) != 0))) {
            if (((unchecked((int)(SLOT[0].ssg)) & 8) != 0)) {
            if ((SLOT[0].volume < 512)) {
            SLOT[0].volume += (4 * unchecked((int)(eg_inc[(int)((unchecked((uint)(SLOT[0].eg_sel_d2r)) + ((eg_cnt >> unchecked((int)(SLOT[0].eg_sh_d2r))) & unchecked((uint)(7)))))])));
            if (((unchecked((int)(SLOT[0].ssgn)) ^ (unchecked((int)(SLOT[0].ssg)) & 4)) != 0)) SLOT[0].vol_out = ((unchecked((uint)((512 - SLOT[0].volume))) & unchecked((uint)(((1 << 10) - 1)))) + SLOT[0].tl); else SLOT[0].vol_out = (unchecked((uint)(SLOT[0].volume)) + SLOT[0].tl);
            }
            } else {
            SLOT[0].volume += unchecked((int)(eg_inc[(int)((unchecked((uint)(SLOT[0].eg_sel_d2r)) + ((eg_cnt >> unchecked((int)(SLOT[0].eg_sh_d2r))) & unchecked((uint)(7)))))]));
            if ((SLOT[0].volume >= ((1 << 10) - 1))) SLOT[0].volume = ((1 << 10) - 1);
            SLOT[0].vol_out = (unchecked((uint)(SLOT[0].volume)) + SLOT[0].tl);
            }
            }
            break;
            }
            case 1:
            {
            if ((!((eg_cnt & unchecked((uint)(((1 << unchecked((int)(SLOT[0].eg_sh_rr))) - 1)))) != 0))) {
            if (((unchecked((int)(SLOT[0].ssg)) & 8) != 0)) {
            if ((SLOT[0].volume < 512)) SLOT[0].volume += (4 * unchecked((int)(eg_inc[(int)((unchecked((uint)(SLOT[0].eg_sel_rr)) + ((eg_cnt >> unchecked((int)(SLOT[0].eg_sh_rr))) & unchecked((uint)(7)))))])));
            if ((SLOT[0].volume >= 512)) {
            SLOT[0].volume = ((1 << 10) - 1);
            SLOT[0].state = unchecked((byte)(0));
            }
            } else {
            SLOT[0].volume += unchecked((int)(eg_inc[(int)((unchecked((uint)(SLOT[0].eg_sel_rr)) + ((eg_cnt >> unchecked((int)(SLOT[0].eg_sh_rr))) & unchecked((uint)(7)))))]));
            if ((SLOT[0].volume >= ((1 << 10) - 1))) {
            SLOT[0].volume = ((1 << 10) - 1);
            SLOT[0].state = unchecked((byte)(0));
            }
            }
            SLOT[0].vol_out = (unchecked((uint)(SLOT[0].volume)) + SLOT[0].tl);
            }
            break;
            }
            }
            SLOT++;
            } while (((--j) != 0));
            CH++;
            } while (((--i) != 0));
        }
    }
    private void update_ssg_eg_channels(ArrayPointer<FM_CH> CH)
    {
        unchecked
        {
            uint i = unchecked((uint)(6));
            uint j = default;
            ArrayPointer<FM_SLOT> SLOT = default;
            do {
            j = unchecked((uint)(4));
            SLOT = new ArrayPointer<FM_SLOT>(CH[0].SLOT, (int)(0));
            do {
            if (((((unchecked((int)(SLOT[0].ssg)) & 8) != 0) && (SLOT[0].volume >= 512)) && (unchecked((int)(SLOT[0].state)) > 1))) {
            if (((unchecked((int)(SLOT[0].ssg)) & 1) != 0)) {
            if (((unchecked((int)(SLOT[0].ssg)) & 2) != 0)) SLOT[0].ssgn = unchecked((byte)(4));
            if (((unchecked((int)(SLOT[0].state)) != 4) && (!((unchecked((int)(SLOT[0].ssgn)) ^ (unchecked((int)(SLOT[0].ssg)) & 4)) != 0)))) SLOT[0].volume = ((1 << 10) - 1);
            } else {
            if (((unchecked((int)(SLOT[0].ssg)) & 2) != 0)) SLOT[0].ssgn ^= unchecked((byte)(4)); else SLOT[0].phase = unchecked((uint)(0));
            if ((unchecked((int)(SLOT[0].state)) != 4)) {
            if (((SLOT[0].ar + unchecked((uint)(SLOT[0].ksr))) < unchecked((uint)(94)))) {
            SLOT[0].state = unchecked((byte)(((SLOT[0].volume <= 0) ? ((SLOT[0].sl == unchecked((uint)(0))) ? 2 : 3) : 4)));
            } else {
            SLOT[0].volume = 0;
            SLOT[0].state = unchecked((byte)(((SLOT[0].sl == unchecked((uint)(0))) ? 2 : 3)));
            }
            }
            }
            if (((unchecked((int)(SLOT[0].ssgn)) ^ (unchecked((int)(SLOT[0].ssg)) & 4)) != 0)) SLOT[0].vol_out = ((unchecked((uint)((512 - SLOT[0].volume))) & unchecked((uint)(((1 << 10) - 1)))) + SLOT[0].tl); else SLOT[0].vol_out = (unchecked((uint)(SLOT[0].volume)) + SLOT[0].tl);
            }
            SLOT++;
            } while (((--j) != 0));
            CH++;
            } while (((--i) != 0));
        }
    }
    private void update_phase_lfo_slot(ArrayPointer<FM_SLOT> SLOT, uint pm, byte kc, uint fc)
    {
        unchecked
        {
            int lfo_fn_offset = lfo_pm_table[(int)((((fc & unchecked((uint)(2032))) << 4) + pm))];
            if ((lfo_fn_offset != 0)) {
            byte blk = unchecked((byte)((fc >> 11)));
            fc = (((fc << 1) + unchecked((uint)(lfo_fn_offset))) & unchecked((uint)(4095)));
            fc = ((((fc << unchecked((int)(blk))) >> 2) + unchecked((uint)(SLOT[0].DT[(int)(kc)]))) & unchecked((uint)(((1 << 17) - 1))));
            SLOT[0].phase += ((fc * SLOT[0].mul) >> 1);
            } else {
            SLOT[0].phase += unchecked((uint)(SLOT[0].Incr));
            }
        }
    }
    private void update_phase_lfo_channel(ArrayPointer<FM_CH> CH)
    {
        unchecked
        {
            uint fc = CH[0].block_fnum;
            int lfo_fn_offset = lfo_pm_table[(int)(((((fc & unchecked((uint)(2032))) << 4) + unchecked((uint)(CH[0].pms))) + ym2612.OPN.LFO_PM))];
            if ((lfo_fn_offset != 0)) {
            uint finc = default;
            byte blk = unchecked((byte)((fc >> 11)));
            byte kc = CH[0].kcode;
            fc = (((fc << 1) + unchecked((uint)(lfo_fn_offset))) & unchecked((uint)(4095)));
            fc = ((fc << unchecked((int)(blk))) >> 2);
            finc = ((fc + unchecked((uint)(CH[0].SLOT[(int)(0)].DT[(int)(kc)]))) & unchecked((uint)(((1 << 17) - 1))));
            CH[0].SLOT[(int)(0)].phase += ((finc * CH[0].SLOT[(int)(0)].mul) >> 1);
            finc = ((fc + unchecked((uint)(CH[0].SLOT[(int)(2)].DT[(int)(kc)]))) & unchecked((uint)(((1 << 17) - 1))));
            CH[0].SLOT[(int)(2)].phase += ((finc * CH[0].SLOT[(int)(2)].mul) >> 1);
            finc = ((fc + unchecked((uint)(CH[0].SLOT[(int)(1)].DT[(int)(kc)]))) & unchecked((uint)(((1 << 17) - 1))));
            CH[0].SLOT[(int)(1)].phase += ((finc * CH[0].SLOT[(int)(1)].mul) >> 1);
            finc = ((fc + unchecked((uint)(CH[0].SLOT[(int)(3)].DT[(int)(kc)]))) & unchecked((uint)(((1 << 17) - 1))));
            CH[0].SLOT[(int)(3)].phase += ((finc * CH[0].SLOT[(int)(3)].mul) >> 1);
            } else {
            CH[0].SLOT[(int)(0)].phase += unchecked((uint)(CH[0].SLOT[(int)(0)].Incr));
            CH[0].SLOT[(int)(2)].phase += unchecked((uint)(CH[0].SLOT[(int)(2)].Incr));
            CH[0].SLOT[(int)(1)].phase += unchecked((uint)(CH[0].SLOT[(int)(1)].Incr));
            CH[0].SLOT[(int)(3)].phase += unchecked((uint)(CH[0].SLOT[(int)(3)].Incr));
            }
        }
    }
    private void refresh_fc_eg_slot(ArrayPointer<FM_SLOT> SLOT, uint fc, uint kc)
    {
        unchecked
        {
            fc += unchecked((uint)(SLOT[0].DT[(int)(kc)]));
            fc &= unchecked((uint)(((1 << 17) - 1)));
            SLOT[0].Incr = unchecked((int)(((fc * SLOT[0].mul) >> 1)));
            kc = (kc >> unchecked((int)(SLOT[0].KSR)));
            if ((unchecked((uint)(SLOT[0].ksr)) != kc)) {
            SLOT[0].ksr = unchecked((byte)(kc));
            if (((SLOT[0].ar + kc) < unchecked((uint)((32 + 62))))) {
            SLOT[0].eg_sh_ar = eg_rate_shift[(int)((SLOT[0].ar + kc))];
            SLOT[0].eg_sel_ar = eg_rate_select[(int)((SLOT[0].ar + kc))];
            } else {
            SLOT[0].eg_sh_ar = unchecked((byte)(0));
            SLOT[0].eg_sel_ar = unchecked((byte)((18 * 8)));
            }
            SLOT[0].eg_sh_d1r = eg_rate_shift[(int)((SLOT[0].d1r + kc))];
            SLOT[0].eg_sel_d1r = eg_rate_select[(int)((SLOT[0].d1r + kc))];
            SLOT[0].eg_sh_d2r = eg_rate_shift[(int)((SLOT[0].d2r + kc))];
            SLOT[0].eg_sel_d2r = eg_rate_select[(int)((SLOT[0].d2r + kc))];
            SLOT[0].eg_sh_rr = eg_rate_shift[(int)((SLOT[0].rr + kc))];
            SLOT[0].eg_sel_rr = eg_rate_select[(int)((SLOT[0].rr + kc))];
            }
        }
    }
    private void refresh_fc_eg_chan(ArrayPointer<FM_CH> CH)
    {
        unchecked
        {
            if ((CH[0].SLOT[(int)(0)].Incr == (-1))) {
            int fc = unchecked((int)(CH[0].fc));
            int kc = unchecked((int)(CH[0].kcode));
            refresh_fc_eg_slot(new ArrayPointer<FM_SLOT>(CH[0].SLOT, (int)(0)), unchecked((uint)(fc)), unchecked((uint)(kc)));
            refresh_fc_eg_slot(new ArrayPointer<FM_SLOT>(CH[0].SLOT, (int)(2)), unchecked((uint)(fc)), unchecked((uint)(kc)));
            refresh_fc_eg_slot(new ArrayPointer<FM_SLOT>(CH[0].SLOT, (int)(1)), unchecked((uint)(fc)), unchecked((uint)(kc)));
            refresh_fc_eg_slot(new ArrayPointer<FM_SLOT>(CH[0].SLOT, (int)(3)), unchecked((uint)(fc)), unchecked((uint)(kc)));
            }
        }
    }
    private int op_calc(uint phase, uint env, uint pm, uint opmask)
    {
        unchecked
        {
            uint p = ((env << 3) + sin_tab[(int)((((phase >> 10) + (pm >> 1)) & unchecked((uint)(((1 << 10) - 1)))))]);
            if ((p >= unchecked((uint)(((13 * 2) * 256))))) return 0;
            return unchecked((int)((unchecked((uint)(tl_tab[(int)(p)])) & opmask)));
        }
    }
    private int op_calc1(uint phase, uint env, uint pm, uint opmask)
    {
        unchecked
        {
            uint p = ((env << 3) + sin_tab[(int)((((phase >> 10) + pm) & unchecked((uint)(((1 << 10) - 1)))))]);
            if ((p >= unchecked((uint)(((13 * 2) * 256))))) return 0;
            return unchecked((int)((unchecked((uint)(tl_tab[(int)(p)])) & opmask)));
        }
    }
    private void chan_calc(ArrayPointer<FM_CH> CH, int num)
    {
        unchecked
        {
            do {
            int output = 0;
            uint AM = (ym2612.OPN.LFO_AM >> unchecked((int)(CH[0].ams)));
            uint eg_out = (new ArrayPointer<FM_SLOT>(CH[0].SLOT, (int)(0))[0].vol_out + (AM & new ArrayPointer<FM_SLOT>(CH[0].SLOT, (int)(0))[0].AMmask));
            ArrayPointer<uint> mask = new ArrayPointer<uint>(op_mask, (int)(CH[0].ALGO) * 4);
            m2 = (c1 = (c2 = (mem = 0)));
            CH[0].mem_connect[0] = CH[0].mem_value;
            if ((eg_out < unchecked((uint)((((13 * 2) * 256) >> 3))))) {
            if ((unchecked((int)(CH[0].FB)) < 10)) output = ((CH[0].op1_out[(int)(0)] + CH[0].op1_out[(int)(1)]) >> unchecked((int)(CH[0].FB)));
            output = op_calc1(CH[0].SLOT[(int)(0)].phase, eg_out, unchecked((uint)(output)), mask[(int)(0)]);
            }
            CH[0].op1_out[(int)(0)] = CH[0].op1_out[(int)(1)];
            CH[0].op1_out[(int)(1)] = output;
            if ((!(!CH[0].connect1.IsNull))) {
            mem = (c1 = (c2 = output));
            } else {
            CH[0].connect1[0] = output;
            }
            eg_out = (new ArrayPointer<FM_SLOT>(CH[0].SLOT, (int)(1))[0].vol_out + (AM & new ArrayPointer<FM_SLOT>(CH[0].SLOT, (int)(1))[0].AMmask));
            if ((eg_out < unchecked((uint)((((13 * 2) * 256) >> 3))))) CH[0].connect3[0] += op_calc(CH[0].SLOT[(int)(1)].phase, eg_out, unchecked((uint)(m2)), mask[(int)(2)]);
            eg_out = (new ArrayPointer<FM_SLOT>(CH[0].SLOT, (int)(2))[0].vol_out + (AM & new ArrayPointer<FM_SLOT>(CH[0].SLOT, (int)(2))[0].AMmask));
            if ((eg_out < unchecked((uint)((((13 * 2) * 256) >> 3))))) CH[0].connect2[0] += op_calc(CH[0].SLOT[(int)(2)].phase, eg_out, unchecked((uint)(c1)), mask[(int)(1)]);
            eg_out = (new ArrayPointer<FM_SLOT>(CH[0].SLOT, (int)(3))[0].vol_out + (AM & new ArrayPointer<FM_SLOT>(CH[0].SLOT, (int)(3))[0].AMmask));
            if ((eg_out < unchecked((uint)((((13 * 2) * 256) >> 3))))) CH[0].connect4[0] += op_calc(CH[0].SLOT[(int)(3)].phase, eg_out, unchecked((uint)(c2)), mask[(int)(3)]);
            CH[0].mem_value = mem;
            if ((CH[0].pms != 0)) {
            if ((((ym2612.OPN.ST.mode & unchecked((uint)(192))) != 0) && CH.Same(new ArrayPointer<FM_CH>(ym2612.CH, (int)(2))))) {
            byte kc = ym2612.CH[(int)(2)].kcode;
            uint pm = (unchecked((uint)(ym2612.CH[(int)(2)].pms)) + ym2612.OPN.LFO_PM);
            update_phase_lfo_slot(new ArrayPointer<FM_SLOT>(ym2612.CH[(int)(2)].SLOT, (int)(0)), pm, kc, ym2612.OPN.SL3.block_fnum[(int)(1)]);
            update_phase_lfo_slot(new ArrayPointer<FM_SLOT>(ym2612.CH[(int)(2)].SLOT, (int)(2)), pm, kc, ym2612.OPN.SL3.block_fnum[(int)(2)]);
            update_phase_lfo_slot(new ArrayPointer<FM_SLOT>(ym2612.CH[(int)(2)].SLOT, (int)(1)), pm, kc, ym2612.OPN.SL3.block_fnum[(int)(0)]);
            update_phase_lfo_slot(new ArrayPointer<FM_SLOT>(ym2612.CH[(int)(2)].SLOT, (int)(3)), pm, kc, ym2612.CH[(int)(2)].block_fnum);
            } else {
            update_phase_lfo_channel(CH);
            }
            } else {
            CH[0].SLOT[(int)(0)].phase += unchecked((uint)(CH[0].SLOT[(int)(0)].Incr));
            CH[0].SLOT[(int)(2)].phase += unchecked((uint)(CH[0].SLOT[(int)(2)].Incr));
            CH[0].SLOT[(int)(1)].phase += unchecked((uint)(CH[0].SLOT[(int)(1)].Incr));
            CH[0].SLOT[(int)(3)].phase += unchecked((uint)(CH[0].SLOT[(int)(3)].Incr));
            }
            CH++;
            } while (((--num) != 0));
        }
    }
    private void OPNWriteMode(int r, int v)
    {
        unchecked
        {
            byte c = default;
            ArrayPointer<FM_CH> CH = default;
            switch (r) {
            case 33:
            break;
            case 34:
            if (((v & 8) != 0)) {
            ym2612.OPN.lfo_timer_overflow = lfo_samples_per_step[(int)((v & 7))];
            } else {
            ym2612.OPN.lfo_timer_overflow = unchecked((uint)(0));
            ym2612.OPN.lfo_timer = unchecked((uint)(0));
            ym2612.OPN.lfo_cnt = unchecked((byte)(0));
            ym2612.OPN.LFO_PM = unchecked((uint)(0));
            ym2612.OPN.LFO_AM = unchecked((uint)(126));
            }
            break;
            case 36:
            ym2612.OPN.ST.TA = ((ym2612.OPN.ST.TA & 3) | (v << 2));
            ym2612.OPN.ST.TAL = (1024 - ym2612.OPN.ST.TA);
            break;
            case 37:
            ym2612.OPN.ST.TA = ((ym2612.OPN.ST.TA & 1020) | (v & 3));
            ym2612.OPN.ST.TAL = (1024 - ym2612.OPN.ST.TA);
            break;
            case 38:
            ym2612.OPN.ST.TB = v;
            ym2612.OPN.ST.TBL = ((256 - v) << 4);
            break;
            case 39:
            set_timers(v);
            break;
            case 40:
            c = unchecked((byte)((v & 3)));
            if ((unchecked((int)(c)) == 3)) break;
            if (((v & 4) != 0)) c += unchecked((byte)(3));
            CH = new ArrayPointer<FM_CH>(ym2612.CH, (int)(c));
            if (((v & 16) != 0)) FM_KEYON(CH, 0); else FM_KEYOFF(CH, 0);
            if (((v & 32) != 0)) FM_KEYON(CH, 2); else FM_KEYOFF(CH, 2);
            if (((v & 64) != 0)) FM_KEYON(CH, 1); else FM_KEYOFF(CH, 1);
            if (((v & 128) != 0)) FM_KEYON(CH, 3); else FM_KEYOFF(CH, 3);
            break;
            }
        }
    }
    private void OPNWriteReg(int r, int v)
    {
        unchecked
        {
            ArrayPointer<FM_CH> CH = default;
            ArrayPointer<FM_SLOT> SLOT = default;
            byte c = unchecked((byte)((r & 3)));
            if ((unchecked((int)(c)) == 3)) return;
            if ((r >= 256)) c += unchecked((byte)(3));
            CH = new ArrayPointer<FM_CH>(ym2612.CH, (int)(c));
            SLOT = new ArrayPointer<FM_SLOT>(CH[0].SLOT, (int)(((r >> 2) & 3)));
            switch ((r & 240)) {
            case 48:
            set_det_mul(CH, SLOT, v);
            break;
            case 64:
            set_tl(SLOT, v);
            break;
            case 80:
            set_ar_ksr(CH, SLOT, v);
            break;
            case 96:
            set_dr(SLOT, v);
            SLOT[0].AMmask = unchecked((uint)((((v & 128) != 0) ? (~0) : 0)));
            break;
            case 112:
            set_sr(SLOT, v);
            break;
            case 128:
            set_sl_rr(SLOT, v);
            break;
            case 144:
            SLOT[0].ssg = unchecked((byte)((v & 15)));
            if ((unchecked((int)(SLOT[0].state)) > 1)) {
            if ((((unchecked((int)(SLOT[0].ssg)) & 8) != 0) && ((unchecked((int)(SLOT[0].ssgn)) ^ (unchecked((int)(SLOT[0].ssg)) & 4)) != 0))) SLOT[0].vol_out = ((unchecked((uint)((512 - SLOT[0].volume))) & unchecked((uint)(((1 << 10) - 1)))) + SLOT[0].tl); else SLOT[0].vol_out = (unchecked((uint)(SLOT[0].volume)) + SLOT[0].tl);
            }
            break;
            case 160:
            switch (((r >> 2) & 3)) {
            case 0:
            {
            uint fn = ((unchecked((uint)((unchecked((int)(ym2612.OPN.ST.fn_h)) & 7))) << 8) + unchecked((uint)(v)));
            byte blk = unchecked((byte)((unchecked((int)(ym2612.OPN.ST.fn_h)) >> 3)));
            CH[0].kcode = unchecked((byte)(((unchecked((int)(blk)) << 2) | unchecked((int)(opn_fktable[(int)((fn >> 7))])))));
            CH[0].fc = ((fn << unchecked((int)(blk))) >> 1);
            CH[0].block_fnum = (unchecked((uint)((unchecked((int)(blk)) << 11))) | fn);
            CH[0].SLOT[(int)(0)].Incr = (-1);
            break;
            }
            case 1:
            ym2612.OPN.ST.fn_h = unchecked((byte)((v & 63)));
            break;
            case 2:
            if ((r < 256)) {
            uint fn = ((unchecked((uint)((unchecked((int)(ym2612.OPN.SL3.fn_h)) & 7))) << 8) + unchecked((uint)(v)));
            byte blk = unchecked((byte)((unchecked((int)(ym2612.OPN.SL3.fn_h)) >> 3)));
            ym2612.OPN.SL3.kcode[(int)(c)] = unchecked((byte)(((unchecked((int)(blk)) << 2) | unchecked((int)(opn_fktable[(int)((fn >> 7))])))));
            ym2612.OPN.SL3.fc[(int)(c)] = ((fn << unchecked((int)(blk))) >> 1);
            ym2612.OPN.SL3.block_fnum[(int)(c)] = (unchecked((uint)((unchecked((int)(blk)) << 11))) | fn);
            ym2612.CH[(int)(2)].SLOT[(int)(0)].Incr = (-1);
            }
            break;
            case 3:
            if ((r < 256)) ym2612.OPN.SL3.fn_h = unchecked((byte)((v & 63)));
            break;
            }
            break;
            case 176:
            switch (((r >> 2) & 3)) {
            case 0:
            {
            CH[0].ALGO = unchecked((byte)((v & 7)));
            CH[0].FB = unchecked((byte)((10 - ((v >> 3) & 7))));
            setup_connection(CH, unchecked((int)(c)));
            break;
            }
            case 1:
            CH[0].pms = ((v & 7) * 32);
            CH[0].ams = lfo_ams_depth_shift[(int)(((v >> 4) & 3))];
            ym2612.OPN.pan[(int)((unchecked((int)(c)) * 2))] = (((v & 128) != 0) ? 4294967295u : unchecked((uint)(0)));
            ym2612.OPN.pan[(int)(((unchecked((int)(c)) * 2) + 1))] = (((v & 64) != 0) ? 4294967295u : unchecked((uint)(0)));
            break;
            }
            break;
            }
        }
    }
    private void reset_channels(ArrayPointer<FM_CH> CH, int num)
    {
        unchecked
        {
            int c = default;
            int s = default;
            for (c = 0; (c < num); c++) {
            CH[(int)(c)].mem_value = 0;
            CH[(int)(c)].op1_out[(int)(0)] = 0;
            CH[(int)(c)].op1_out[(int)(1)] = 0;
            for (s = 0; (s < 4); s++) {
            CH[(int)(c)].SLOT[(int)(s)].Incr = (-1);
            CH[(int)(c)].SLOT[(int)(s)].key = unchecked((byte)(0));
            CH[(int)(c)].SLOT[(int)(s)].phase = unchecked((uint)(0));
            CH[(int)(c)].SLOT[(int)(s)].ssgn = unchecked((byte)(0));
            CH[(int)(c)].SLOT[(int)(s)].state = unchecked((byte)(0));
            CH[(int)(c)].SLOT[(int)(s)].volume = ((1 << 10) - 1);
            CH[(int)(c)].SLOT[(int)(s)].vol_out = unchecked((uint)(((1 << 10) - 1)));
            }
            }
        }
    }
    private void init_tables()
    {
        unchecked
        {
            int d = default;
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
            n <<= 2;
            tl_tab[(int)(((x * 2) + 0))] = n;
            tl_tab[(int)(((x * 2) + 1))] = (-tl_tab[(int)(((x * 2) + 0))]);
            for (i = 1; (i < 13); i++) {
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
            }
            for (i = 0; (i < 8); i++) {
            byte fnum = default;
            for (fnum = unchecked((byte)(0)); (unchecked((int)(fnum)) < 128); fnum++) {
            byte value = default;
            byte step = default;
            uint offset_depth = unchecked((uint)(i));
            uint offset_fnum_bit = default;
            uint bit_tmp = default;
            for (step = unchecked((byte)(0)); (unchecked((int)(step)) < 8); step++) {
            value = unchecked((byte)(0));
            for (bit_tmp = unchecked((uint)(0)); (bit_tmp < unchecked((uint)(7))); bit_tmp++) {
            if (((unchecked((int)(fnum)) & (1 << unchecked((int)(bit_tmp)))) != 0)) {
            offset_fnum_bit = (bit_tmp * unchecked((uint)(8)));
            value += new ArrayPointer<byte>(lfo_pm_output, (int)((offset_fnum_bit + offset_depth)) * 8)[(int)(step)];
            }
            }
            lfo_pm_table[(int)((((((unchecked((int)(fnum)) * 32) * 8) + (i * 32)) + unchecked((int)(step))) + 0))] = unchecked((int)(value));
            lfo_pm_table[(int)((((((unchecked((int)(fnum)) * 32) * 8) + (i * 32)) + (unchecked((int)(step)) ^ 7)) + 8))] = unchecked((int)(value));
            lfo_pm_table[(int)((((((unchecked((int)(fnum)) * 32) * 8) + (i * 32)) + unchecked((int)(step))) + 16))] = (-unchecked((int)(value)));
            lfo_pm_table[(int)((((((unchecked((int)(fnum)) * 32) * 8) + (i * 32)) + (unchecked((int)(step)) ^ 7)) + 24))] = (-unchecked((int)(value)));
            }
            }
            }
            for (d = 0; (d <= 3); d++) {
            for (i = 0; (i <= 31); i++) {
            new ArrayPointer<int>(ym2612.OPN.ST.dt_tab, (int)(d) * 32)[(int)(i)] = unchecked((int)(dt_tab[(int)(((d * 32) + i))]));
            new ArrayPointer<int>(ym2612.OPN.ST.dt_tab, (int)((d + 4)) * 32)[(int)(i)] = (-new ArrayPointer<int>(ym2612.OPN.ST.dt_tab, (int)(d) * 32)[(int)(i)]);
            }
            }
            for (i = 0; (i < 8); i++) {
            for (d = 0; (d < 4); d++) {
            new ArrayPointer<uint>(op_mask, (int)(i) * 4)[(int)(d)] = 4294967295u;
            }
            }
        }
    }
    private void YM2612ResetChip()
    {
        unchecked
        {
            int i = default;
            ym2612.OPN.eg_timer = unchecked((uint)(0));
            ym2612.OPN.eg_cnt = unchecked((uint)(0));
            ym2612.OPN.lfo_timer_overflow = unchecked((uint)(0));
            ym2612.OPN.lfo_timer = unchecked((uint)(0));
            ym2612.OPN.lfo_cnt = unchecked((byte)(0));
            ym2612.OPN.LFO_AM = unchecked((uint)(126));
            ym2612.OPN.LFO_PM = unchecked((uint)(0));
            ym2612.OPN.ST.TAC = 0;
            ym2612.OPN.ST.TBC = 0;
            ym2612.OPN.SL3.key_csm = unchecked((byte)(0));
            ym2612.dacen = unchecked((byte)(0));
            ym2612.dacout = 0;
            set_timers(48);
            ym2612.OPN.ST.TB = 0;
            ym2612.OPN.ST.TBL = (256 << 4);
            ym2612.OPN.ST.TA = 0;
            ym2612.OPN.ST.TAL = 1024;
            reset_channels(new ArrayPointer<FM_CH>(ym2612.CH, (int)(0)), 6);
            for (i = 182; (i >= 180); i--) {
            OPNWriteReg(i, 192);
            OPNWriteReg((i | 256), 192);
            }
            for (i = 178; (i >= 48); i--) {
            OPNWriteReg(i, 0);
            OPNWriteReg((i | 256), 0);
            }
        }
    }
    private void YM2612Write(uint a, uint v)
    {
        unchecked
        {
            v &= unchecked((uint)(255));
            switch (a) {
            case 0:
            ym2612.OPN.ST.address = unchecked((ushort)(v));
            break;
            case 2:
            ym2612.OPN.ST.address = unchecked((ushort)((v | unchecked((uint)(256)))));
            break;
            default:
            {
            int addr = unchecked((int)(ym2612.OPN.ST.address));
            switch ((addr & 496)) {
            case 32:
            switch (addr) {
            case 42:
            ym2612.dacout = ((unchecked((int)(v)) - 128) << 6);
            break;
            case 43:
            ym2612.dacen = unchecked((byte)((v & unchecked((uint)(128)))));
            break;
            default:
            OPNWriteMode(addr, unchecked((int)(v)));
            break;
            }
            break;
            default:
            OPNWriteReg(addr, unchecked((int)(v)));
            break;
            }
            break;
            }
            }
        }
    }
    private uint YM2612Read()
    {
        unchecked
        {
            return unchecked((uint)(ym2612.OPN.ST.status));
        }
    }
    private void YM2612Update(ArrayPointer<int> buffer, int length)
    {
        unchecked
        {
            int i = default;
            int lt = default;
            int rt = default;
            refresh_fc_eg_chan(new ArrayPointer<FM_CH>(ym2612.CH, (int)(0)));
            refresh_fc_eg_chan(new ArrayPointer<FM_CH>(ym2612.CH, (int)(1)));
            if ((!((ym2612.OPN.ST.mode & unchecked((uint)(192))) != 0))) {
            refresh_fc_eg_chan(new ArrayPointer<FM_CH>(ym2612.CH, (int)(2)));
            } else {
            if ((ym2612.CH[(int)(2)].SLOT[(int)(0)].Incr == (-1))) {
            refresh_fc_eg_slot(new ArrayPointer<FM_SLOT>(ym2612.CH[(int)(2)].SLOT, (int)(0)), ym2612.OPN.SL3.fc[(int)(1)], unchecked((uint)(ym2612.OPN.SL3.kcode[(int)(1)])));
            refresh_fc_eg_slot(new ArrayPointer<FM_SLOT>(ym2612.CH[(int)(2)].SLOT, (int)(2)), ym2612.OPN.SL3.fc[(int)(2)], unchecked((uint)(ym2612.OPN.SL3.kcode[(int)(2)])));
            refresh_fc_eg_slot(new ArrayPointer<FM_SLOT>(ym2612.CH[(int)(2)].SLOT, (int)(1)), ym2612.OPN.SL3.fc[(int)(0)], unchecked((uint)(ym2612.OPN.SL3.kcode[(int)(0)])));
            refresh_fc_eg_slot(new ArrayPointer<FM_SLOT>(ym2612.CH[(int)(2)].SLOT, (int)(3)), ym2612.CH[(int)(2)].fc, unchecked((uint)(ym2612.CH[(int)(2)].kcode)));
            }
            }
            refresh_fc_eg_chan(new ArrayPointer<FM_CH>(ym2612.CH, (int)(3)));
            refresh_fc_eg_chan(new ArrayPointer<FM_CH>(ym2612.CH, (int)(4)));
            refresh_fc_eg_chan(new ArrayPointer<FM_CH>(ym2612.CH, (int)(5)));
            for (i = 0; (i < length); i++) {
            out_fm[(int)(0)] = 0;
            out_fm[(int)(1)] = 0;
            out_fm[(int)(2)] = 0;
            out_fm[(int)(3)] = 0;
            out_fm[(int)(4)] = 0;
            out_fm[(int)(5)] = 0;
            update_ssg_eg_channels(new ArrayPointer<FM_CH>(ym2612.CH, (int)(0)));
            if ((!(ym2612.dacen != 0))) {
            chan_calc(new ArrayPointer<FM_CH>(ym2612.CH, (int)(0)), 6);
            } else {
            out_fm[(int)(5)] = ym2612.dacout;
            chan_calc(new ArrayPointer<FM_CH>(ym2612.CH, (int)(0)), 5);
            }
            advance_lfo();
            ym2612.OPN.eg_timer++;
            if ((ym2612.OPN.eg_timer >= unchecked((uint)(3)))) {
            ym2612.OPN.eg_timer = unchecked((uint)(0));
            ym2612.OPN.eg_cnt++;
            if ((ym2612.OPN.eg_cnt == unchecked((uint)(4096)))) ym2612.OPN.eg_cnt = unchecked((uint)(1));
            advance_eg_channels(new ArrayPointer<FM_CH>(ym2612.CH, (int)(0)), ym2612.OPN.eg_cnt);
            }
            if ((out_fm[(int)(0)] > 8191)) out_fm[(int)(0)] = 8191; else if ((out_fm[(int)(0)] < (-8192))) out_fm[(int)(0)] = (-8192);
            if ((out_fm[(int)(1)] > 8191)) out_fm[(int)(1)] = 8191; else if ((out_fm[(int)(1)] < (-8192))) out_fm[(int)(1)] = (-8192);
            if ((out_fm[(int)(2)] > 8191)) out_fm[(int)(2)] = 8191; else if ((out_fm[(int)(2)] < (-8192))) out_fm[(int)(2)] = (-8192);
            if ((out_fm[(int)(3)] > 8191)) out_fm[(int)(3)] = 8191; else if ((out_fm[(int)(3)] < (-8192))) out_fm[(int)(3)] = (-8192);
            if ((out_fm[(int)(4)] > 8191)) out_fm[(int)(4)] = 8191; else if ((out_fm[(int)(4)] < (-8192))) out_fm[(int)(4)] = (-8192);
            if ((out_fm[(int)(5)] > 8191)) out_fm[(int)(5)] = 8191; else if ((out_fm[(int)(5)] < (-8192))) out_fm[(int)(5)] = (-8192);
            lt = unchecked((int)((unchecked((uint)(out_fm[(int)(0)])) & ym2612.OPN.pan[(int)(0)])));
            rt = unchecked((int)((unchecked((uint)(out_fm[(int)(0)])) & ym2612.OPN.pan[(int)(1)])));
            lt += unchecked((int)((unchecked((uint)(out_fm[(int)(1)])) & ym2612.OPN.pan[(int)(2)])));
            rt += unchecked((int)((unchecked((uint)(out_fm[(int)(1)])) & ym2612.OPN.pan[(int)(3)])));
            lt += unchecked((int)((unchecked((uint)(out_fm[(int)(2)])) & ym2612.OPN.pan[(int)(4)])));
            rt += unchecked((int)((unchecked((uint)(out_fm[(int)(2)])) & ym2612.OPN.pan[(int)(5)])));
            lt += unchecked((int)((unchecked((uint)(out_fm[(int)(3)])) & ym2612.OPN.pan[(int)(6)])));
            rt += unchecked((int)((unchecked((uint)(out_fm[(int)(3)])) & ym2612.OPN.pan[(int)(7)])));
            lt += unchecked((int)((unchecked((uint)(out_fm[(int)(4)])) & ym2612.OPN.pan[(int)(8)])));
            rt += unchecked((int)((unchecked((uint)(out_fm[(int)(4)])) & ym2612.OPN.pan[(int)(9)])));
            lt += unchecked((int)((unchecked((uint)(out_fm[(int)(5)])) & ym2612.OPN.pan[(int)(10)])));
            rt += unchecked((int)((unchecked((uint)(out_fm[(int)(5)])) & ym2612.OPN.pan[(int)(11)])));
            if ((chip_type == YM2612_DISCRETE)) {
            int i__1 = default;
            for (i__1 = 0; (i__1 < 6); i__1++) {
            if ((out_fm[(int)(i__1)] < 0)) {
            lt -= unchecked((int)(((unchecked((uint)(4)) - (ym2612.OPN.pan[(int)(((2 * i__1) + 0))] & unchecked((uint)(1)))) << 5)));
            rt -= unchecked((int)(((unchecked((uint)(4)) - (ym2612.OPN.pan[(int)(((2 * i__1) + 1))] & unchecked((uint)(1)))) << 5)));
            } else {
            lt += (4 << 5);
            rt += (4 << 5);
            }
            }
            }
            (buffer++)[0] = lt;
            (buffer++)[0] = rt;
            ym2612.OPN.SL3.key_csm <<= 1;
            INTERNAL_TIMER_A();
            if (((unchecked((int)(ym2612.OPN.SL3.key_csm)) & 2) != 0)) {
            FM_KEYOFF_CSM(new ArrayPointer<FM_CH>(ym2612.CH, (int)(2)), 0);
            FM_KEYOFF_CSM(new ArrayPointer<FM_CH>(ym2612.CH, (int)(2)), 2);
            FM_KEYOFF_CSM(new ArrayPointer<FM_CH>(ym2612.CH, (int)(2)), 1);
            FM_KEYOFF_CSM(new ArrayPointer<FM_CH>(ym2612.CH, (int)(2)), 3);
            ym2612.OPN.SL3.key_csm = unchecked((byte)(0));
            }
            }
            INTERNAL_TIMER_B(length);
        }
    }
    private void YM2612Config(int type)
    {
        unchecked
        {
            chip_type = type;
            if ((chip_type < YM2612_ENHANCED)) {
            new ArrayPointer<uint>(op_mask, (int)(0) * 4)[(int)(3)] = 4294967264u;
            new ArrayPointer<uint>(op_mask, (int)(1) * 4)[(int)(3)] = 4294967264u;
            new ArrayPointer<uint>(op_mask, (int)(2) * 4)[(int)(3)] = 4294967264u;
            new ArrayPointer<uint>(op_mask, (int)(3) * 4)[(int)(3)] = 4294967264u;
            new ArrayPointer<uint>(op_mask, (int)(4) * 4)[(int)(1)] = 4294967264u;
            new ArrayPointer<uint>(op_mask, (int)(4) * 4)[(int)(3)] = 4294967264u;
            new ArrayPointer<uint>(op_mask, (int)(5) * 4)[(int)(1)] = 4294967264u;
            new ArrayPointer<uint>(op_mask, (int)(5) * 4)[(int)(2)] = 4294967264u;
            new ArrayPointer<uint>(op_mask, (int)(5) * 4)[(int)(3)] = 4294967264u;
            new ArrayPointer<uint>(op_mask, (int)(6) * 4)[(int)(1)] = 4294967264u;
            new ArrayPointer<uint>(op_mask, (int)(6) * 4)[(int)(2)] = 4294967264u;
            new ArrayPointer<uint>(op_mask, (int)(6) * 4)[(int)(3)] = 4294967264u;
            new ArrayPointer<uint>(op_mask, (int)(7) * 4)[(int)(0)] = 4294967264u;
            new ArrayPointer<uint>(op_mask, (int)(7) * 4)[(int)(1)] = 4294967264u;
            new ArrayPointer<uint>(op_mask, (int)(7) * 4)[(int)(2)] = 4294967264u;
            new ArrayPointer<uint>(op_mask, (int)(7) * 4)[(int)(3)] = 4294967264u;
            } else {
            new ArrayPointer<uint>(op_mask, (int)(0) * 4)[(int)(3)] = 4294967295u;
            new ArrayPointer<uint>(op_mask, (int)(1) * 4)[(int)(3)] = 4294967295u;
            new ArrayPointer<uint>(op_mask, (int)(2) * 4)[(int)(3)] = 4294967295u;
            new ArrayPointer<uint>(op_mask, (int)(3) * 4)[(int)(3)] = 4294967295u;
            new ArrayPointer<uint>(op_mask, (int)(4) * 4)[(int)(1)] = 4294967295u;
            new ArrayPointer<uint>(op_mask, (int)(4) * 4)[(int)(3)] = 4294967295u;
            new ArrayPointer<uint>(op_mask, (int)(5) * 4)[(int)(1)] = 4294967295u;
            new ArrayPointer<uint>(op_mask, (int)(5) * 4)[(int)(2)] = 4294967295u;
            new ArrayPointer<uint>(op_mask, (int)(5) * 4)[(int)(3)] = 4294967295u;
            new ArrayPointer<uint>(op_mask, (int)(6) * 4)[(int)(1)] = 4294967295u;
            new ArrayPointer<uint>(op_mask, (int)(6) * 4)[(int)(2)] = 4294967295u;
            new ArrayPointer<uint>(op_mask, (int)(6) * 4)[(int)(3)] = 4294967295u;
            new ArrayPointer<uint>(op_mask, (int)(7) * 4)[(int)(0)] = 4294967295u;
            new ArrayPointer<uint>(op_mask, (int)(7) * 4)[(int)(1)] = 4294967295u;
            new ArrayPointer<uint>(op_mask, (int)(7) * 4)[(int)(2)] = 4294967295u;
            new ArrayPointer<uint>(op_mask, (int)(7) * 4)[(int)(3)] = 4294967295u;
            }
        }
    }
}
