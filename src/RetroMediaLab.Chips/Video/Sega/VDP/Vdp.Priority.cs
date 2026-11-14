using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Chips.Video.Sega.VDP;

public sealed partial class Vdp
{
    private uint make_lut_bg(uint bx, uint ax)
    {
        unchecked
        {
            int bf = unchecked((int)((bx & unchecked((uint)(127)))));
            int bp = unchecked((int)((bx & unchecked((uint)(64)))));
            int b = unchecked((int)((bx & unchecked((uint)(15)))));
            int af = unchecked((int)((ax & unchecked((uint)(127)))));
            int ap = unchecked((int)((ax & unchecked((uint)(64)))));
            int a = unchecked((int)((ax & unchecked((uint)(15)))));
            int c = ((ap != 0) ? ((a != 0) ? af : bf) : ((bp != 0) ? ((b != 0) ? bf : af) : ((a != 0) ? af : bf)));
            if (((c & 15) == 0)) c &= 128;
            return unchecked((uint)(c));
        }
    }
    private uint make_lut_bg_ste(uint bx, uint ax)
    {
        unchecked
        {
            int bf = unchecked((int)((bx & unchecked((uint)(127)))));
            int bp = unchecked((int)((bx & unchecked((uint)(64)))));
            int b = unchecked((int)((bx & unchecked((uint)(15)))));
            int af = unchecked((int)((ax & unchecked((uint)(127)))));
            int ap = unchecked((int)((ax & unchecked((uint)(64)))));
            int a = unchecked((int)((ax & unchecked((uint)(15)))));
            int c = ((ap != 0) ? ((a != 0) ? af : bf) : ((bp != 0) ? ((b != 0) ? bf : af) : ((a != 0) ? af : bf)));
            c |= ((ap | bp) << 1);
            if (((c & 15) == 0)) c &= 128;
            return unchecked((uint)(c));
        }
    }
    private uint make_lut_obj(uint bx, uint sx)
    {
        unchecked
        {
            int c = default;
            int bf = unchecked((int)((bx & unchecked((uint)(127)))));
            int bs = unchecked((int)((bx & unchecked((uint)(128)))));
            int sf = unchecked((int)((sx & unchecked((uint)(127)))));
            if (((sx & unchecked((uint)(15))) == unchecked((uint)(0)))) return bx;
            c = ((bs != 0) ? bf : sf);
            if (((c & 15) == 0)) c &= 192;
            return unchecked((uint)((c | 128)));
        }
    }
    private uint make_lut_bgobj(uint bx, uint sx)
    {
        unchecked
        {
            int c = default;
            int bf = unchecked((int)((bx & unchecked((uint)(63)))));
            int bs = unchecked((int)((bx & unchecked((uint)(128)))));
            int bp = unchecked((int)((bx & unchecked((uint)(64)))));
            int b = unchecked((int)((bx & unchecked((uint)(15)))));
            int sf = unchecked((int)((sx & unchecked((uint)(63)))));
            int sp = unchecked((int)((sx & unchecked((uint)(64)))));
            int s = unchecked((int)((sx & unchecked((uint)(15)))));
            if ((s == 0)) return bx;
            if ((bs != 0)) return bx;
            c = ((sp != 0) ? sf : ((bp != 0) ? ((b != 0) ? bf : sf) : sf));
            if (((c & 15) == 0)) c &= 128;
            return unchecked((uint)((c | 128)));
        }
    }
    private uint make_lut_bgobj_ste(uint bx, uint sx)
    {
        unchecked
        {
            int c = default;
            int bf = unchecked((int)((bx & unchecked((uint)(63)))));
            int bp = unchecked((int)((bx & unchecked((uint)(64)))));
            int b = unchecked((int)((bx & unchecked((uint)(15)))));
            int bi = unchecked((int)(((bx & unchecked((uint)(128))) >> 1)));
            int sf = unchecked((int)((sx & unchecked((uint)(63)))));
            int sp = unchecked((int)((sx & unchecked((uint)(64)))));
            int s = unchecked((int)((sx & unchecked((uint)(15)))));
            int si = (sp | bi);
            if ((sp != 0)) {
            if ((s != 0)) {
            if (((sf & 62) == 62)) {
            if (((sf & 1) != 0)) {
            c = (bf | 0);
            } else {
            c = (((bx & unchecked((uint)(128))) != 0) ? (bf | 128) : (bf | 64));
            }
            } else {
            if ((((sf == 14) || (sf == 30)) || (sf == 46))) {
            c = (sf | 64);
            } else {
            c = (sf | si);
            }
            }
            } else {
            c = (bf | bi);
            }
            } else {
            if ((bp != 0)) {
            if ((b != 0)) {
            c = (bf | bi);
            } else {
            if ((s != 0)) {
            if (((sf & 62) == 62)) {
            if (((sf & 1) != 0)) {
            c = (bf | 0);
            } else {
            c = (((bx & unchecked((uint)(128))) != 0) ? (bf | 128) : (bf | 64));
            }
            } else {
            if ((((sf == 14) || (sf == 30)) || (sf == 46))) {
            c = (sf | 64);
            } else {
            c = (sf | si);
            }
            }
            } else {
            c = (bf | bi);
            }
            }
            } else {
            if ((s != 0)) {
            if (((sf & 62) == 62)) {
            if (((sf & 1) != 0)) {
            c = (bf | 0);
            } else {
            c = (((bx & unchecked((uint)(128))) != 0) ? (bf | 128) : (bf | 64));
            }
            } else {
            if ((((sf == 14) || (sf == 30)) || (sf == 46))) {
            c = (sf | 64);
            } else {
            c = (sf | si);
            }
            }
            } else {
            c = (bf | bi);
            }
            }
            }
            if (((c & 15) == 0)) c &= 192;
            return unchecked((uint)(c));
        }
    }
    private uint make_lut_bgobj_m4(uint bx, uint sx)
    {
        unchecked
        {
            int c = default;
            int bf = unchecked((int)((bx & unchecked((uint)(63)))));
            int bs = unchecked((int)((bx & unchecked((uint)(128)))));
            int bp = unchecked((int)((bx & unchecked((uint)(32)))));
            int b = unchecked((int)((bx & unchecked((uint)(15)))));
            int s = unchecked((int)((sx & unchecked((uint)(15)))));
            int sf = (s | 16);
            if ((s == 0)) return bx;
            if ((bs != 0)) return bx;
            c = ((bp != 0) ? ((b != 0) ? bf : sf) : sf);
            return unchecked((uint)((c | 128)));
        }
    }
}
