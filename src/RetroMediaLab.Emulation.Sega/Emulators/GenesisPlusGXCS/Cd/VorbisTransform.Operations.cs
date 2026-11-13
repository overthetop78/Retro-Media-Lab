using System.Buffers.Binary;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cd;

internal sealed partial class VorbisTransform
{
    private void mdct_butterfly_8(CPointer<int> x)
    {
        unchecked
        {
            int r0 = (x[(int)(4)] + x[(int)(0)]);
            int r1 = (x[(int)(4)] - x[(int)(0)]);
            int r2 = (x[(int)(5)] + x[(int)(1)]);
            int r3 = (x[(int)(5)] - x[(int)(1)]);
            int r4 = (x[(int)(6)] + x[(int)(2)]);
            int r5 = (x[(int)(6)] - x[(int)(2)]);
            int r6 = (x[(int)(7)] + x[(int)(3)]);
            int r7 = (x[(int)(7)] - x[(int)(3)]);
            x[(int)(0)] = (r5 + r3);
            x[(int)(1)] = (r7 - r1);
            x[(int)(2)] = (r5 - r3);
            x[(int)(3)] = (r7 + r1);
            x[(int)(4)] = (r4 - r0);
            x[(int)(5)] = (r6 - r2);
            x[(int)(6)] = (r4 + r0);
            x[(int)(7)] = (r6 + r2);
            ;
        }
    }
    private void mdct_butterfly_16(CPointer<int> x)
    {
        unchecked
        {
            int r0 = default;
            int r1 = default;
            r0 = (x[(int)(0)] - x[(int)(8)]);
            x[(int)(8)] += x[(int)(0)];
            r1 = (x[(int)(1)] - x[(int)(9)]);
            x[(int)(9)] += x[(int)(1)];
            x[(int)(0)] = MULT31((r0 + r1), 1518500250);
            x[(int)(1)] = MULT31((r1 - r0), 1518500250);
            ;
            r0 = (x[(int)(10)] - x[(int)(2)]);
            x[(int)(10)] += x[(int)(2)];
            r1 = (x[(int)(3)] - x[(int)(11)]);
            x[(int)(11)] += x[(int)(3)];
            x[(int)(2)] = r1;
            x[(int)(3)] = r0;
            ;
            r0 = (x[(int)(12)] - x[(int)(4)]);
            x[(int)(12)] += x[(int)(4)];
            r1 = (x[(int)(13)] - x[(int)(5)]);
            x[(int)(13)] += x[(int)(5)];
            x[(int)(4)] = MULT31((r0 - r1), 1518500250);
            x[(int)(5)] = MULT31((r0 + r1), 1518500250);
            ;
            r0 = (x[(int)(14)] - x[(int)(6)]);
            x[(int)(14)] += x[(int)(6)];
            r1 = (x[(int)(15)] - x[(int)(7)]);
            x[(int)(15)] += x[(int)(7)];
            x[(int)(6)] = r0;
            x[(int)(7)] = r1;
            ;
            mdct_butterfly_8(x);
            mdct_butterfly_8((x + 8));
        }
    }
    private void mdct_butterfly_32(CPointer<int> x)
    {
        unchecked
        {
            int r0 = default;
            int r1 = default;
            r0 = (x[(int)(30)] - x[(int)(14)]);
            x[(int)(30)] += x[(int)(14)];
            r1 = (x[(int)(31)] - x[(int)(15)]);
            x[(int)(31)] += x[(int)(15)];
            x[(int)(14)] = r0;
            x[(int)(15)] = r1;
            ;
            r0 = (x[(int)(28)] - x[(int)(12)]);
            x[(int)(28)] += x[(int)(12)];
            r1 = (x[(int)(29)] - x[(int)(13)]);
            x[(int)(29)] += x[(int)(13)];
            {
            (x + (int)(12))[0] = (MULT31(r0, 1984016189) - MULT31(r1, 821806413));
            (x + (int)(13))[0] = (MULT31(r1, 1984016189) + MULT31(r0, 821806413));
            }
            ;
            ;
            r0 = (x[(int)(26)] - x[(int)(10)]);
            x[(int)(26)] += x[(int)(10)];
            r1 = (x[(int)(27)] - x[(int)(11)]);
            x[(int)(27)] += x[(int)(11)];
            x[(int)(10)] = MULT31((r0 - r1), 1518500250);
            x[(int)(11)] = MULT31((r0 + r1), 1518500250);
            ;
            r0 = (x[(int)(24)] - x[(int)(8)]);
            x[(int)(24)] += x[(int)(8)];
            r1 = (x[(int)(25)] - x[(int)(9)]);
            x[(int)(25)] += x[(int)(9)];
            {
            (x + (int)(8))[0] = (MULT31(r0, 821806413) - MULT31(r1, 1984016189));
            (x + (int)(9))[0] = (MULT31(r1, 821806413) + MULT31(r0, 1984016189));
            }
            ;
            ;
            r0 = (x[(int)(22)] - x[(int)(6)]);
            x[(int)(22)] += x[(int)(6)];
            r1 = (x[(int)(7)] - x[(int)(23)]);
            x[(int)(23)] += x[(int)(7)];
            x[(int)(6)] = r1;
            x[(int)(7)] = r0;
            ;
            r0 = (x[(int)(4)] - x[(int)(20)]);
            x[(int)(20)] += x[(int)(4)];
            r1 = (x[(int)(5)] - x[(int)(21)]);
            x[(int)(21)] += x[(int)(5)];
            {
            (x + (int)(4))[0] = (MULT31(r0, 821806413) + MULT31(r1, 1984016189));
            (x + (int)(5))[0] = (MULT31(r1, 821806413) - MULT31(r0, 1984016189));
            }
            ;
            ;
            r0 = (x[(int)(2)] - x[(int)(18)]);
            x[(int)(18)] += x[(int)(2)];
            r1 = (x[(int)(3)] - x[(int)(19)]);
            x[(int)(19)] += x[(int)(3)];
            x[(int)(2)] = MULT31((r1 + r0), 1518500250);
            x[(int)(3)] = MULT31((r1 - r0), 1518500250);
            ;
            r0 = (x[(int)(0)] - x[(int)(16)]);
            x[(int)(16)] += x[(int)(0)];
            r1 = (x[(int)(1)] - x[(int)(17)]);
            x[(int)(17)] += x[(int)(1)];
            {
            (x + (int)(0))[0] = (MULT31(r0, 1984016189) + MULT31(r1, 821806413));
            (x + (int)(1))[0] = (MULT31(r1, 1984016189) - MULT31(r0, 821806413));
            }
            ;
            ;
            mdct_butterfly_16(x);
            mdct_butterfly_16((x + 16));
        }
    }
    private void mdct_butterfly_generic(CPointer<int> x, int points, int step)
    {
        unchecked
        {
            CPointer<int> T = new CPointer<int>(sincos_lookup0);
            CPointer<int> x1 = ((x + points) - 8);
            CPointer<int> x2 = ((x + (points >> 1)) - 8);
            int r0 = default;
            int r1 = default;
            do {
            r0 = (x1[(int)(6)] - x2[(int)(6)]);
            x1[(int)(6)] += x2[(int)(6)];
            r1 = (x2[(int)(7)] - x1[(int)(7)]);
            x1[(int)(7)] += x2[(int)(7)];
            {
            (x2 + (int)(6))[0] = (MULT31(r1, T[(int)(0)]) + MULT31(r0, T[(int)(1)]));
            (x2 + (int)(7))[0] = (MULT31(r0, T[(int)(0)]) - MULT31(r1, T[(int)(1)]));
            }
            ;
            T += step;
            r0 = (x1[(int)(4)] - x2[(int)(4)]);
            x1[(int)(4)] += x2[(int)(4)];
            r1 = (x2[(int)(5)] - x1[(int)(5)]);
            x1[(int)(5)] += x2[(int)(5)];
            {
            (x2 + (int)(4))[0] = (MULT31(r1, T[(int)(0)]) + MULT31(r0, T[(int)(1)]));
            (x2 + (int)(5))[0] = (MULT31(r0, T[(int)(0)]) - MULT31(r1, T[(int)(1)]));
            }
            ;
            T += step;
            r0 = (x1[(int)(2)] - x2[(int)(2)]);
            x1[(int)(2)] += x2[(int)(2)];
            r1 = (x2[(int)(3)] - x1[(int)(3)]);
            x1[(int)(3)] += x2[(int)(3)];
            {
            (x2 + (int)(2))[0] = (MULT31(r1, T[(int)(0)]) + MULT31(r0, T[(int)(1)]));
            (x2 + (int)(3))[0] = (MULT31(r0, T[(int)(0)]) - MULT31(r1, T[(int)(1)]));
            }
            ;
            T += step;
            r0 = (x1[(int)(0)] - x2[(int)(0)]);
            x1[(int)(0)] += x2[(int)(0)];
            r1 = (x2[(int)(1)] - x1[(int)(1)]);
            x1[(int)(1)] += x2[(int)(1)];
            {
            (x2 + (int)(0))[0] = (MULT31(r1, T[(int)(0)]) + MULT31(r0, T[(int)(1)]));
            (x2 + (int)(1))[0] = (MULT31(r0, T[(int)(0)]) - MULT31(r1, T[(int)(1)]));
            }
            ;
            T += step;
            x1 -= 8;
            x2 -= 8;
            } while ((T < (new CPointer<int>(sincos_lookup0) + 1024)));
            do {
            r0 = (x1[(int)(6)] - x2[(int)(6)]);
            x1[(int)(6)] += x2[(int)(6)];
            r1 = (x1[(int)(7)] - x2[(int)(7)]);
            x1[(int)(7)] += x2[(int)(7)];
            {
            (x2 + (int)(6))[0] = (MULT31(r0, T[(int)(0)]) - MULT31(r1, T[(int)(1)]));
            (x2 + (int)(7))[0] = (MULT31(r1, T[(int)(0)]) + MULT31(r0, T[(int)(1)]));
            }
            ;
            T -= step;
            r0 = (x1[(int)(4)] - x2[(int)(4)]);
            x1[(int)(4)] += x2[(int)(4)];
            r1 = (x1[(int)(5)] - x2[(int)(5)]);
            x1[(int)(5)] += x2[(int)(5)];
            {
            (x2 + (int)(4))[0] = (MULT31(r0, T[(int)(0)]) - MULT31(r1, T[(int)(1)]));
            (x2 + (int)(5))[0] = (MULT31(r1, T[(int)(0)]) + MULT31(r0, T[(int)(1)]));
            }
            ;
            T -= step;
            r0 = (x1[(int)(2)] - x2[(int)(2)]);
            x1[(int)(2)] += x2[(int)(2)];
            r1 = (x1[(int)(3)] - x2[(int)(3)]);
            x1[(int)(3)] += x2[(int)(3)];
            {
            (x2 + (int)(2))[0] = (MULT31(r0, T[(int)(0)]) - MULT31(r1, T[(int)(1)]));
            (x2 + (int)(3))[0] = (MULT31(r1, T[(int)(0)]) + MULT31(r0, T[(int)(1)]));
            }
            ;
            T -= step;
            r0 = (x1[(int)(0)] - x2[(int)(0)]);
            x1[(int)(0)] += x2[(int)(0)];
            r1 = (x1[(int)(1)] - x2[(int)(1)]);
            x1[(int)(1)] += x2[(int)(1)];
            {
            (x2 + (int)(0))[0] = (MULT31(r0, T[(int)(0)]) - MULT31(r1, T[(int)(1)]));
            (x2 + (int)(1))[0] = (MULT31(r1, T[(int)(0)]) + MULT31(r0, T[(int)(1)]));
            }
            ;
            T -= step;
            x1 -= 8;
            x2 -= 8;
            } while ((T > new CPointer<int>(sincos_lookup0)));
            do {
            r0 = (x2[(int)(6)] - x1[(int)(6)]);
            x1[(int)(6)] += x2[(int)(6)];
            r1 = (x2[(int)(7)] - x1[(int)(7)]);
            x1[(int)(7)] += x2[(int)(7)];
            {
            (x2 + (int)(6))[0] = (MULT31(r0, T[(int)(0)]) + MULT31(r1, T[(int)(1)]));
            (x2 + (int)(7))[0] = (MULT31(r1, T[(int)(0)]) - MULT31(r0, T[(int)(1)]));
            }
            ;
            T += step;
            r0 = (x2[(int)(4)] - x1[(int)(4)]);
            x1[(int)(4)] += x2[(int)(4)];
            r1 = (x2[(int)(5)] - x1[(int)(5)]);
            x1[(int)(5)] += x2[(int)(5)];
            {
            (x2 + (int)(4))[0] = (MULT31(r0, T[(int)(0)]) + MULT31(r1, T[(int)(1)]));
            (x2 + (int)(5))[0] = (MULT31(r1, T[(int)(0)]) - MULT31(r0, T[(int)(1)]));
            }
            ;
            T += step;
            r0 = (x2[(int)(2)] - x1[(int)(2)]);
            x1[(int)(2)] += x2[(int)(2)];
            r1 = (x2[(int)(3)] - x1[(int)(3)]);
            x1[(int)(3)] += x2[(int)(3)];
            {
            (x2 + (int)(2))[0] = (MULT31(r0, T[(int)(0)]) + MULT31(r1, T[(int)(1)]));
            (x2 + (int)(3))[0] = (MULT31(r1, T[(int)(0)]) - MULT31(r0, T[(int)(1)]));
            }
            ;
            T += step;
            r0 = (x2[(int)(0)] - x1[(int)(0)]);
            x1[(int)(0)] += x2[(int)(0)];
            r1 = (x2[(int)(1)] - x1[(int)(1)]);
            x1[(int)(1)] += x2[(int)(1)];
            {
            (x2 + (int)(0))[0] = (MULT31(r0, T[(int)(0)]) + MULT31(r1, T[(int)(1)]));
            (x2 + (int)(1))[0] = (MULT31(r1, T[(int)(0)]) - MULT31(r0, T[(int)(1)]));
            }
            ;
            T += step;
            x1 -= 8;
            x2 -= 8;
            } while ((T < (new CPointer<int>(sincos_lookup0) + 1024)));
            do {
            r0 = (x1[(int)(6)] - x2[(int)(6)]);
            x1[(int)(6)] += x2[(int)(6)];
            r1 = (x2[(int)(7)] - x1[(int)(7)]);
            x1[(int)(7)] += x2[(int)(7)];
            {
            (x2 + (int)(6))[0] = (MULT31(r1, T[(int)(0)]) - MULT31(r0, T[(int)(1)]));
            (x2 + (int)(7))[0] = (MULT31(r0, T[(int)(0)]) + MULT31(r1, T[(int)(1)]));
            }
            ;
            T -= step;
            r0 = (x1[(int)(4)] - x2[(int)(4)]);
            x1[(int)(4)] += x2[(int)(4)];
            r1 = (x2[(int)(5)] - x1[(int)(5)]);
            x1[(int)(5)] += x2[(int)(5)];
            {
            (x2 + (int)(4))[0] = (MULT31(r1, T[(int)(0)]) - MULT31(r0, T[(int)(1)]));
            (x2 + (int)(5))[0] = (MULT31(r0, T[(int)(0)]) + MULT31(r1, T[(int)(1)]));
            }
            ;
            T -= step;
            r0 = (x1[(int)(2)] - x2[(int)(2)]);
            x1[(int)(2)] += x2[(int)(2)];
            r1 = (x2[(int)(3)] - x1[(int)(3)]);
            x1[(int)(3)] += x2[(int)(3)];
            {
            (x2 + (int)(2))[0] = (MULT31(r1, T[(int)(0)]) - MULT31(r0, T[(int)(1)]));
            (x2 + (int)(3))[0] = (MULT31(r0, T[(int)(0)]) + MULT31(r1, T[(int)(1)]));
            }
            ;
            T -= step;
            r0 = (x1[(int)(0)] - x2[(int)(0)]);
            x1[(int)(0)] += x2[(int)(0)];
            r1 = (x2[(int)(1)] - x1[(int)(1)]);
            x1[(int)(1)] += x2[(int)(1)];
            {
            (x2 + (int)(0))[0] = (MULT31(r1, T[(int)(0)]) - MULT31(r0, T[(int)(1)]));
            (x2 + (int)(1))[0] = (MULT31(r0, T[(int)(0)]) + MULT31(r1, T[(int)(1)]));
            }
            ;
            T -= step;
            x1 -= 8;
            x2 -= 8;
            } while ((T > new CPointer<int>(sincos_lookup0)));
        }
    }
    private void mdct_butterflies(CPointer<int> x, int points, int shift)
    {
        unchecked
        {
            int stages = (8 - shift);
            int i = default;
            int j = default;
            for (i = 0; ((--stages) > 0); i++) {
            for (j = 0; (j < (1 << i)); j++) mdct_butterfly_generic((x + ((points >> i) * j)), (points >> i), (4 << (i + shift)));
            }
            for (j = 0; (j < points); j += 32) mdct_butterfly_32((x + j));
        }
    }
    private int bitrev12(int x)
    {
        unchecked
        {
            return ((unchecked((int)(bitrev[(int)((x >> 8))])) | (unchecked((int)(bitrev[(int)(((x & 240) >> 4))])) << 4)) | (unchecked((int)(bitrev[(int)((x & 15))])) << 8));
        }
    }
    private void mdct_bitreverse(CPointer<int> x, int n, int step, int shift)
    {
        unchecked
        {
            int bit = 0;
            CPointer<int> w0 = x;
            CPointer<int> w1 = (x = (w0 + (n >> 1)));
            CPointer<int> T = ((step >= 4) ? (new CPointer<int>(sincos_lookup0) + (step >> 1)) : new CPointer<int>(sincos_lookup1));
            CPointer<int> Ttop = (T + 1024);
            int r2 = default;
            do {
            int r3 = bitrev12((bit++));
            CPointer<int> x0 = ((x + ((r3 ^ 4095) >> shift)) - 1);
            CPointer<int> x1 = (x + (r3 >> shift));
            int r0 = (x0[(int)(0)] + x1[(int)(0)]);
            int r1 = (x1[(int)(1)] - x0[(int)(1)]);
            {
            r2 = (MULT32(r0, T[(int)(1)]) + MULT32(r1, T[(int)(0)]));
            r3 = (MULT32(r1, T[(int)(1)]) - MULT32(r0, T[(int)(0)]));
            }
            ;
            T += step;
            w1 -= 4;
            r0 = ((x0[(int)(1)] + x1[(int)(1)]) >> 1);
            r1 = ((x0[(int)(0)] - x1[(int)(0)]) >> 1);
            w0[(int)(0)] = (r0 + r2);
            w0[(int)(1)] = (r1 + r3);
            w1[(int)(2)] = (r0 - r2);
            w1[(int)(3)] = (r3 - r1);
            r3 = bitrev12((bit++));
            x0 = ((x + ((r3 ^ 4095) >> shift)) - 1);
            x1 = (x + (r3 >> shift));
            r0 = (x0[(int)(0)] + x1[(int)(0)]);
            r1 = (x1[(int)(1)] - x0[(int)(1)]);
            {
            r2 = (MULT32(r0, T[(int)(1)]) + MULT32(r1, T[(int)(0)]));
            r3 = (MULT32(r1, T[(int)(1)]) - MULT32(r0, T[(int)(0)]));
            }
            ;
            T += step;
            r0 = ((x0[(int)(1)] + x1[(int)(1)]) >> 1);
            r1 = ((x0[(int)(0)] - x1[(int)(0)]) >> 1);
            w0[(int)(2)] = (r0 + r2);
            w0[(int)(3)] = (r1 + r3);
            w1[(int)(0)] = (r0 - r2);
            w1[(int)(1)] = (r3 - r1);
            w0 += 4;
            } while ((T < Ttop));
            do {
            int r3 = bitrev12((bit++));
            CPointer<int> x0 = ((x + ((r3 ^ 4095) >> shift)) - 1);
            CPointer<int> x1 = (x + (r3 >> shift));
            int r0 = (x0[(int)(0)] + x1[(int)(0)]);
            int r1 = (x1[(int)(1)] - x0[(int)(1)]);
            T -= step;
            {
            r2 = (MULT32(r0, T[(int)(0)]) + MULT32(r1, T[(int)(1)]));
            r3 = (MULT32(r1, T[(int)(0)]) - MULT32(r0, T[(int)(1)]));
            }
            ;
            w1 -= 4;
            r0 = ((x0[(int)(1)] + x1[(int)(1)]) >> 1);
            r1 = ((x0[(int)(0)] - x1[(int)(0)]) >> 1);
            w0[(int)(0)] = (r0 + r2);
            w0[(int)(1)] = (r1 + r3);
            w1[(int)(2)] = (r0 - r2);
            w1[(int)(3)] = (r3 - r1);
            r3 = bitrev12((bit++));
            x0 = ((x + ((r3 ^ 4095) >> shift)) - 1);
            x1 = (x + (r3 >> shift));
            r0 = (x0[(int)(0)] + x1[(int)(0)]);
            r1 = (x1[(int)(1)] - x0[(int)(1)]);
            T -= step;
            {
            r2 = (MULT32(r0, T[(int)(0)]) + MULT32(r1, T[(int)(1)]));
            r3 = (MULT32(r1, T[(int)(0)]) - MULT32(r0, T[(int)(1)]));
            }
            ;
            r0 = ((x0[(int)(1)] + x1[(int)(1)]) >> 1);
            r1 = ((x0[(int)(0)] - x1[(int)(0)]) >> 1);
            w0[(int)(2)] = (r0 + r2);
            w0[(int)(3)] = (r1 + r3);
            w1[(int)(0)] = (r0 - r2);
            w1[(int)(1)] = (r3 - r1);
            w0 += 4;
            } while ((w0 < w1));
        }
    }
    private void mdct_backward(int n, CPointer<int> sampleInput, CPointer<int> sampleOutput)
    {
        unchecked
        {
            int n2 = (n >> 1);
            int n4 = (n >> 2);
            CPointer<int> iX = default;
            CPointer<int> oX = default;
            CPointer<int> T = default;
            CPointer<int> V = default;
            int shift = default;
            int step = default;
            for (shift = 6; (!((n & (1 << shift)) != 0)); shift++) ;
            shift = (13 - shift);
            step = (2 << shift);
            iX = ((sampleInput + n2) - 7);
            oX = ((sampleOutput + n2) + n4);
            T = new CPointer<int>(sincos_lookup0);
            do {
            oX -= 4;
            {
            (oX + (int)(2))[0] = (MULT31(iX[(int)(4)], T[(int)(0)]) + MULT31(iX[(int)(6)], T[(int)(1)]));
            (oX + (int)(3))[0] = (MULT31(iX[(int)(6)], T[(int)(0)]) - MULT31(iX[(int)(4)], T[(int)(1)]));
            }
            ;
            T += step;
            {
            (oX + (int)(0))[0] = (MULT31(iX[(int)(0)], T[(int)(0)]) + MULT31(iX[(int)(2)], T[(int)(1)]));
            (oX + (int)(1))[0] = (MULT31(iX[(int)(2)], T[(int)(0)]) - MULT31(iX[(int)(0)], T[(int)(1)]));
            }
            ;
            T += step;
            iX -= 8;
            } while ((iX >= (sampleInput + n4)));
            do {
            oX -= 4;
            {
            (oX + (int)(2))[0] = (MULT31(iX[(int)(4)], T[(int)(1)]) + MULT31(iX[(int)(6)], T[(int)(0)]));
            (oX + (int)(3))[0] = (MULT31(iX[(int)(6)], T[(int)(1)]) - MULT31(iX[(int)(4)], T[(int)(0)]));
            }
            ;
            T -= step;
            {
            (oX + (int)(0))[0] = (MULT31(iX[(int)(0)], T[(int)(1)]) + MULT31(iX[(int)(2)], T[(int)(0)]));
            (oX + (int)(1))[0] = (MULT31(iX[(int)(2)], T[(int)(1)]) - MULT31(iX[(int)(0)], T[(int)(0)]));
            }
            ;
            T -= step;
            iX -= 8;
            } while ((iX >= sampleInput));
            iX = ((sampleInput + n2) - 8);
            oX = ((sampleOutput + n2) + n4);
            T = new CPointer<int>(sincos_lookup0);
            do {
            T += step;
            {
            (oX + (int)(0))[0] = (MULT31(iX[(int)(6)], T[(int)(0)]) - MULT31(iX[(int)(4)], T[(int)(1)]));
            (oX + (int)(1))[0] = (MULT31(iX[(int)(4)], T[(int)(0)]) + MULT31(iX[(int)(6)], T[(int)(1)]));
            }
            ;
            T += step;
            {
            (oX + (int)(2))[0] = (MULT31(iX[(int)(2)], T[(int)(0)]) - MULT31(iX[(int)(0)], T[(int)(1)]));
            (oX + (int)(3))[0] = (MULT31(iX[(int)(0)], T[(int)(0)]) + MULT31(iX[(int)(2)], T[(int)(1)]));
            }
            ;
            iX -= 8;
            oX += 4;
            } while ((iX >= (sampleInput + n4)));
            do {
            T -= step;
            {
            (oX + (int)(0))[0] = (MULT31(iX[(int)(6)], T[(int)(1)]) - MULT31(iX[(int)(4)], T[(int)(0)]));
            (oX + (int)(1))[0] = (MULT31(iX[(int)(4)], T[(int)(1)]) + MULT31(iX[(int)(6)], T[(int)(0)]));
            }
            ;
            T -= step;
            {
            (oX + (int)(2))[0] = (MULT31(iX[(int)(2)], T[(int)(1)]) - MULT31(iX[(int)(0)], T[(int)(0)]));
            (oX + (int)(3))[0] = (MULT31(iX[(int)(0)], T[(int)(1)]) + MULT31(iX[(int)(2)], T[(int)(0)]));
            }
            ;
            iX -= 8;
            oX += 4;
            } while ((iX >= sampleInput));
            mdct_butterflies((sampleOutput + n2), n2, shift);
            mdct_bitreverse(sampleOutput, n, step, shift);
            step >>= 2;
            {
            CPointer<int> oX1 = ((sampleOutput + n2) + n4);
            CPointer<int> oX2 = ((sampleOutput + n2) + n4);
            CPointer<int> iX__1 = sampleOutput;
            switch (step) {
            default:
            {
            T = ((step >= 4) ? (new CPointer<int>(sincos_lookup0) + (step >> 1)) : new CPointer<int>(sincos_lookup1));
            do {
            oX1 -= 4;
            {
            (oX1 + (int)(3))[0] = (MULT31(iX__1[(int)(0)], T[(int)(0)]) + MULT31((-iX__1[(int)(1)]), T[(int)(1)]));
            (oX2 + (int)(0))[0] = (MULT31((-iX__1[(int)(1)]), T[(int)(0)]) - MULT31(iX__1[(int)(0)], T[(int)(1)]));
            }
            ;
            T += step;
            {
            (oX1 + (int)(2))[0] = (MULT31(iX__1[(int)(2)], T[(int)(0)]) + MULT31((-iX__1[(int)(3)]), T[(int)(1)]));
            (oX2 + (int)(1))[0] = (MULT31((-iX__1[(int)(3)]), T[(int)(0)]) - MULT31(iX__1[(int)(2)], T[(int)(1)]));
            }
            ;
            T += step;
            {
            (oX1 + (int)(1))[0] = (MULT31(iX__1[(int)(4)], T[(int)(0)]) + MULT31((-iX__1[(int)(5)]), T[(int)(1)]));
            (oX2 + (int)(2))[0] = (MULT31((-iX__1[(int)(5)]), T[(int)(0)]) - MULT31(iX__1[(int)(4)], T[(int)(1)]));
            }
            ;
            T += step;
            {
            (oX1 + (int)(0))[0] = (MULT31(iX__1[(int)(6)], T[(int)(0)]) + MULT31((-iX__1[(int)(7)]), T[(int)(1)]));
            (oX2 + (int)(3))[0] = (MULT31((-iX__1[(int)(7)]), T[(int)(0)]) - MULT31(iX__1[(int)(6)], T[(int)(1)]));
            }
            ;
            T += step;
            oX2 += 4;
            iX__1 += 8;
            } while ((iX__1 < oX1));
            break;
            }
            case 1:
            {
            int t0 = default;
            int t1 = default;
            int v0 = default;
            int v1 = default;
            T = new CPointer<int>(sincos_lookup0);
            V = new CPointer<int>(sincos_lookup1);
            t0 = ((T++)[0] >> 1);
            t1 = ((T++)[0] >> 1);
            do {
            oX1 -= 4;
            t0 += (v0 = ((V++)[0] >> 1));
            t1 += (v1 = ((V++)[0] >> 1));
            {
            (oX1 + (int)(3))[0] = (MULT31(iX__1[(int)(0)], t0) + MULT31((-iX__1[(int)(1)]), t1));
            (oX2 + (int)(0))[0] = (MULT31((-iX__1[(int)(1)]), t0) - MULT31(iX__1[(int)(0)], t1));
            }
            ;
            v0 += (t0 = ((T++)[0] >> 1));
            v1 += (t1 = ((T++)[0] >> 1));
            {
            (oX1 + (int)(2))[0] = (MULT31(iX__1[(int)(2)], v0) + MULT31((-iX__1[(int)(3)]), v1));
            (oX2 + (int)(1))[0] = (MULT31((-iX__1[(int)(3)]), v0) - MULT31(iX__1[(int)(2)], v1));
            }
            ;
            t0 += (v0 = ((V++)[0] >> 1));
            t1 += (v1 = ((V++)[0] >> 1));
            {
            (oX1 + (int)(1))[0] = (MULT31(iX__1[(int)(4)], t0) + MULT31((-iX__1[(int)(5)]), t1));
            (oX2 + (int)(2))[0] = (MULT31((-iX__1[(int)(5)]), t0) - MULT31(iX__1[(int)(4)], t1));
            }
            ;
            v0 += (t0 = ((T++)[0] >> 1));
            v1 += (t1 = ((T++)[0] >> 1));
            {
            (oX1 + (int)(0))[0] = (MULT31(iX__1[(int)(6)], v0) + MULT31((-iX__1[(int)(7)]), v1));
            (oX2 + (int)(3))[0] = (MULT31((-iX__1[(int)(7)]), v0) - MULT31(iX__1[(int)(6)], v1));
            }
            ;
            oX2 += 4;
            iX__1 += 8;
            } while ((iX__1 < oX1));
            break;
            }
            case 0:
            {
            int t0 = default;
            int t1 = default;
            int v0 = default;
            int v1 = default;
            int q0 = default;
            int q1 = default;
            T = new CPointer<int>(sincos_lookup0);
            V = new CPointer<int>(sincos_lookup1);
            t0 = (T++)[0];
            t1 = (T++)[0];
            do {
            oX1 -= 4;
            v0 = (V++)[0];
            v1 = (V++)[0];
            t0 += (q0 = ((v0 - t0) >> 2));
            t1 += (q1 = ((v1 - t1) >> 2));
            {
            (oX1 + (int)(3))[0] = (MULT31(iX__1[(int)(0)], t0) + MULT31((-iX__1[(int)(1)]), t1));
            (oX2 + (int)(0))[0] = (MULT31((-iX__1[(int)(1)]), t0) - MULT31(iX__1[(int)(0)], t1));
            }
            ;
            t0 = (v0 - q0);
            t1 = (v1 - q1);
            {
            (oX1 + (int)(2))[0] = (MULT31(iX__1[(int)(2)], t0) + MULT31((-iX__1[(int)(3)]), t1));
            (oX2 + (int)(1))[0] = (MULT31((-iX__1[(int)(3)]), t0) - MULT31(iX__1[(int)(2)], t1));
            }
            ;
            t0 = (T++)[0];
            t1 = (T++)[0];
            v0 += (q0 = ((t0 - v0) >> 2));
            v1 += (q1 = ((t1 - v1) >> 2));
            {
            (oX1 + (int)(1))[0] = (MULT31(iX__1[(int)(4)], v0) + MULT31((-iX__1[(int)(5)]), v1));
            (oX2 + (int)(2))[0] = (MULT31((-iX__1[(int)(5)]), v0) - MULT31(iX__1[(int)(4)], v1));
            }
            ;
            v0 = (t0 - q0);
            v1 = (t1 - q1);
            {
            (oX1 + (int)(0))[0] = (MULT31(iX__1[(int)(6)], v0) + MULT31((-iX__1[(int)(7)]), v1));
            (oX2 + (int)(3))[0] = (MULT31((-iX__1[(int)(7)]), v0) - MULT31(iX__1[(int)(6)], v1));
            }
            ;
            oX2 += 4;
            iX__1 += 8;
            } while ((iX__1 < oX1));
            break;
            }
            }
            iX__1 = ((sampleOutput + n2) + n4);
            oX1 = (sampleOutput + n4);
            oX2 = oX1;
            do {
            oX1 -= 4;
            iX__1 -= 4;
            oX2[(int)(0)] = (-(oX1[(int)(3)] = iX__1[(int)(3)]));
            oX2[(int)(1)] = (-(oX1[(int)(2)] = iX__1[(int)(2)]));
            oX2[(int)(2)] = (-(oX1[(int)(1)] = iX__1[(int)(1)]));
            oX2[(int)(3)] = (-(oX1[(int)(0)] = iX__1[(int)(0)]));
            oX2 += 4;
            } while ((oX2 < iX__1));
            iX__1 = ((sampleOutput + n2) + n4);
            oX1 = ((sampleOutput + n2) + n4);
            oX2 = (sampleOutput + n2);
            do {
            oX1 -= 4;
            oX1[(int)(0)] = iX__1[(int)(3)];
            oX1[(int)(1)] = iX__1[(int)(2)];
            oX1[(int)(2)] = iX__1[(int)(1)];
            oX1[(int)(3)] = iX__1[(int)(0)];
            iX__1 += 4;
            } while ((oX1 > oX2));
            }
        }
    }
}
