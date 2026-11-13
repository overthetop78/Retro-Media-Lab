using System.Buffers.Binary;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cd;

internal sealed partial class VorbisFloor0
{
    private int vorbis_invsqlook_i(int a, int e)
    {
        unchecked
        {
            int i = ((a & 32767) >> (10 - 1));
            int d = (a & 1023);
            int val = (INVSQ_LOOKUP_I[(int)(i)] - ((INVSQ_LOOKUP_IDel[(int)(i)] * d) >> 10));
            val *= ADJUST_SQRT2[(int)((e & 1))];
            e = ((e >> 1) + 21);
            return (val >> e);
        }
    }
    private int vorbis_fromdBlook_i(int a)
    {
        unchecked
        {
            int i = ((-a) >> (12 - 3));
            if ((i < 0)) return 2147483647;
            if ((i >= (35 << 5))) return 0;
            return (FROMdB_LOOKUP[(int)((i >> 5))] * FROMdB2_LOOKUP[(int)((i & 31))]);
        }
    }
    private int vorbis_coslook_i(int a)
    {
        unchecked
        {
            int i = (a >> 9);
            int d = (a & 511);
            return (COS_LOOKUP_I[(int)(i)] - ((d * (COS_LOOKUP_I[(int)(i)] - COS_LOOKUP_I[(int)((i + 1))])) >> 9));
        }
    }
    private int vorbis_coslook2_i(int a)
    {
        unchecked
        {
            a = (a & 131071);
            if ((a > 65536)) a = (131072 - a);
            {
            int i = (a >> 9);
            int d = (a & 511);
            a = (((COS_LOOKUP_I[(int)(i)] << 9) - (d * (COS_LOOKUP_I[(int)(i)] - COS_LOOKUP_I[(int)((i + 1))]))) >> ((9 - 14) + 14));
            }
            return a;
        }
    }
    private int toBARK(int n)
    {
        unchecked
        {
            int i = default;
            for (i = 0; (i < 27); i++) if (((n >= barklook[(int)(i)]) && (n < barklook[(int)((i + 1))]))) break;
            if ((i == 27)) {
            return (27 << 15);
            } else {
            int gap = (barklook[(int)((i + 1))] - barklook[(int)(i)]);
            int del = (n - barklook[(int)(i)]);
            return ((i << 15) + ((del << 15) / gap));
            }
        }
    }
    private void vorbis_lsp_to_curve(CPointer<int> curve, CPointer<int> map, int n, int ln, CPointer<int> lsp, int m, int amp, int ampoffset, CPointer<int> icos)
    {
        unchecked
        {
            int i = default;
            int ampoffseti = (ampoffset * 4096);
            int ampi = amp;
            Span<int> ilsp = stackalloc int[(int)(256)];
            for (i = 0; (i < m); i++) {
            int val = MULT32(lsp[(int)(i)], 5340354);
            if (((val < 0) || ((val >> 9) >= 128))) {
            ClearCurve(curve, n);
            return;
            }
            ilsp[(int)(i)] = vorbis_coslook_i(val);
            }
            i = 0;
            while ((i < n)) {
            int j = default;
            int k = map[(int)(i)];
            uint pi = unchecked((uint)(46341));
            uint qi = unchecked((uint)(46341));
            int qexp = 0;
            int shift = default;
            int wi = icos[(int)(k)];
            qi *= unchecked((uint)(labs((ilsp[(int)(0)] - wi))));
            pi *= unchecked((uint)(labs((ilsp[(int)(1)] - wi))));
            for (j = 3; (j < m); j += 2) {
            if ((!((shift = unchecked((int)(MLOOP_1[(int)(((pi | qi) >> 25))]))) != 0))) if ((!((shift = unchecked((int)(MLOOP_2[(int)(((pi | qi) >> 19))]))) != 0))) shift = unchecked((int)(MLOOP_3[(int)(((pi | qi) >> 16))]));
            qi = ((qi >> shift) * unchecked((uint)(labs((ilsp[(int)((j - 1))] - wi)))));
            pi = ((pi >> shift) * unchecked((uint)(labs((ilsp[(int)(j)] - wi)))));
            qexp += shift;
            }
            if ((!((shift = unchecked((int)(MLOOP_1[(int)(((pi | qi) >> 25))]))) != 0))) if ((!((shift = unchecked((int)(MLOOP_2[(int)(((pi | qi) >> 19))]))) != 0))) shift = unchecked((int)(MLOOP_3[(int)(((pi | qi) >> 16))]));
            if (((m & 1) != 0)) {
            qi = ((qi >> shift) * unchecked((uint)(labs((ilsp[(int)((j - 1))] - wi)))));
            pi = ((pi >> shift) << 14);
            qexp += shift;
            if ((!((shift = unchecked((int)(MLOOP_1[(int)(((pi | qi) >> 25))]))) != 0))) if ((!((shift = unchecked((int)(MLOOP_2[(int)(((pi | qi) >> 19))]))) != 0))) shift = unchecked((int)(MLOOP_3[(int)(((pi | qi) >> 16))]));
            pi >>= shift;
            qi >>= shift;
            qexp += (shift - (14 * ((m + 1) >> 1)));
            pi = ((pi * pi) >> 16);
            qi = ((qi * qi) >> 16);
            qexp = ((qexp * 2) + m);
            pi *= unchecked((uint)(((1 << 14) - ((wi * wi) >> 14))));
            qi += (pi >> 14);
            } else {
            pi >>= shift;
            qi >>= shift;
            qexp += (shift - (7 * m));
            pi = ((pi * pi) >> 16);
            qi = ((qi * qi) >> 16);
            qexp = ((qexp * 2) + m);
            pi *= unchecked((uint)(((1 << 14) - wi)));
            qi *= unchecked((uint)(((1 << 14) + wi)));
            qi = ((qi + pi) >> 14);
            }
            if (((qi & 4294901760u) != 0)) {
            qi >>= 1;
            qexp++;
            } else while (((qi != 0) && (!((qi & unchecked((uint)(32768))) != 0)))) {
            qi <<= 1;
            qexp--;
            }
            amp = vorbis_fromdBlook_i(((ampi * vorbis_invsqlook_i(unchecked((int)(qi)), qexp)) - ampoffseti));
            curve[(int)(i)] = MULT31_SHIFT15(curve[(int)(i)], amp);
            while ((map[(int)((++i))] == k)) curve[(int)(i)] = MULT31_SHIFT15(curve[(int)(i)], amp);
            }
        }
    }
}
