using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal partial class Sh4Interpreter
{
    private static int GetN(uint opcode)
    {
        return (int)((opcode >> 8) & 0xF);
    }
    private static int GetM(uint opcode)
    {
        return (int)((opcode >> 4) & 0xF);
    }

    private static double GetDoubleN(Sh4Context context, uint opcode)
    {
        return context.GetDouble((int)((opcode >> 9) & 7));
    }
    private static double GetDoubleM(Sh4Context context, uint opcode)
    {
        return context.GetDouble((int)((opcode >> 5) & 7));
    }
    private static void SetDoubleN(Sh4Context context, uint opcode, double value)
    {
        context.SetDouble((int)((opcode >> 9) & 7), value);
    }

    internal partial void Execute_i1111_nnnn_mmmm_0000(Sh4Context context, uint opcode)
    {
        if (!context.Fpscr.Pr)
        {
            int n = GetN(opcode);
            int m = GetM(opcode);
            context.FloatingRegisters[n] += context.FloatingRegisters[m];
            context.FloatingRegisters[n] = Sh4FloatingPoint.FixNaN(context.FloatingRegisters[n]);
        }
        else
        {
            double d = GetDoubleN(context, opcode) + GetDoubleM(context, opcode);
            d = Sh4FloatingPoint.FixNaN64(d);
            SetDoubleN(context, opcode, d);
        }
    }

    internal partial void Execute_i1111_nnnn_mmmm_0001(Sh4Context context, uint opcode)
    {
        if (!context.Fpscr.Pr)
        {
            int n = GetN(opcode);
            int m = GetM(opcode);

            context.FloatingRegisters[n] -= context.FloatingRegisters[m];
            context.FloatingRegisters[n] = Sh4FloatingPoint.FixNaN(context.FloatingRegisters[n]);
        }
        else
        {
            double d = GetDoubleN(context, opcode) - GetDoubleM(context, opcode);
            d = Sh4FloatingPoint.FixNaN64(d);
            SetDoubleN(context, opcode, d);
        }
    }

    internal partial void Execute_i1111_nnnn_mmmm_0010(Sh4Context context, uint opcode)
    {
        if (!context.Fpscr.Pr)
        {
            int n = GetN(opcode);
            int m = GetM(opcode);
            context.FloatingRegisters[n] *= context.FloatingRegisters[m];
            context.FloatingRegisters[n] = Sh4FloatingPoint.FixNaN(context.FloatingRegisters[n]);
        }
        else
        {
            double d = GetDoubleN(context, opcode) * GetDoubleM(context, opcode);
            d = Sh4FloatingPoint.FixNaN64(d);
            SetDoubleN(context, opcode, d);
        }
    }

    internal partial void Execute_i1111_nnnn_mmmm_0011(Sh4Context context, uint opcode)
    {
        if (!context.Fpscr.Pr)
        {
            int n = GetN(opcode);
            int m = GetM(opcode);

            context.FloatingRegisters[n] /= context.FloatingRegisters[m];

            context.FloatingRegisters[n] = Sh4FloatingPoint.FixNaN(context.FloatingRegisters[n]);
        }
        else
        {
            double d = GetDoubleN(context, opcode) / GetDoubleM(context, opcode);
            d = Sh4FloatingPoint.FixNaN64(d);
            SetDoubleN(context, opcode, d);
        }
    }

    internal partial void Execute_i1111_nnnn_mmmm_0100(Sh4Context context, uint opcode)
    {
        if (!context.Fpscr.Pr)
        {
            int n = GetN(opcode);
            int m = GetM(opcode);

            context.StatusRegister.T = context.FloatingRegisters[m] == context.FloatingRegisters[n];
        }
        else
        {
            context.StatusRegister.T = GetDoubleN(context, opcode) == GetDoubleM(context, opcode);
        }
    }

    internal partial void Execute_i1111_nnnn_mmmm_0101(Sh4Context context, uint opcode)
    {
        if (!context.Fpscr.Pr)
        {
            int n = GetN(opcode);
            int m = GetM(opcode);

            if (context.FloatingRegisters[n] > context.FloatingRegisters[m])
                context.StatusRegister.T = true;
            else
                context.StatusRegister.T = false;
        }
        else
        {
            context.StatusRegister.T = GetDoubleN(context, opcode) > GetDoubleM(context, opcode);
        }
    }

    internal partial void Execute_i1111_nnnn_mmmm_0110(Sh4Context context, uint opcode)
    {
        if (!context.Fpscr.Sz)
        {
            int n = GetN(opcode);
            int m = GetM(opcode);

            context.FloatingBits(n) = context.ReadMemory32(context.Registers[m] + context.Registers[0]);
        }
        else
        {
            int n = GetN(opcode)>>1;
            int m = GetM(opcode);
            if (((opcode >> 8) & 1) == 0)
                context.DoubleBits(n) = context.ReadMemory64(context.Registers[m] + context.Registers[0]);
            else
                context.ExtendedDoubleBits(n) = context.ReadMemory64(context.Registers[m] + context.Registers[0]);
        }
    }

    internal partial void Execute_i1111_nnnn_mmmm_0111(Sh4Context context, uint opcode)
    {
        if (!context.Fpscr.Sz)
        {
            int n = GetN(opcode);
            int m = GetM(opcode);

            context.WriteMemory32(context.Registers[0] + context.Registers[n], context.FloatingBits(m));
        }
        else
        {
            int n = GetN(opcode);
            int m = GetM(opcode)>>1;
            if (((opcode >> 4) & 0x1) == 0)
                context.WriteMemory64(context.Registers[n] + context.Registers[0], context.DoubleBits(m));
            else
                context.WriteMemory64(context.Registers[n] + context.Registers[0], context.ExtendedDoubleBits(m));
        }
    }

    internal partial void Execute_i1111_nnnn_mmmm_1000(Sh4Context context, uint opcode)
    {
        if (!context.Fpscr.Sz)
        {
            int n = GetN(opcode);
            int m = GetM(opcode);
            context.FloatingBits(n) = context.ReadMemory32(context.Registers[m]);
        }
        else
        {
            int n = GetN(opcode)>>1;
            int m = GetM(opcode);
            if (((opcode >> 8) & 1) == 0)
                context.DoubleBits(n) = context.ReadMemory64(context.Registers[m]);
            else
                context.ExtendedDoubleBits(n) = context.ReadMemory64(context.Registers[m]);
        }
    }

    internal partial void Execute_i1111_nnnn_mmmm_1001(Sh4Context context, uint opcode)
    {
        if (!context.Fpscr.Sz)
        {
            int n = GetN(opcode);
            int m = GetM(opcode);

            context.FloatingBits(n) = context.ReadMemory32(context.Registers[m]);
            context.Registers[m] += 4;
        }
        else
        {
            int n = GetN(opcode)>>1;
            int m = GetM(opcode);
            if (((opcode >> 8) & 1) == 0)
                context.DoubleBits(n) = context.ReadMemory64(context.Registers[m]);
            else
                context.ExtendedDoubleBits(n) = context.ReadMemory64(context.Registers[m]);
            context.Registers[m] += 8;
        }
    }

    internal partial void Execute_i1111_nnnn_mmmm_1010(Sh4Context context, uint opcode)
    {
        if (!context.Fpscr.Sz)
        {
            int n = GetN(opcode);
            int m = GetM(opcode);
            context.WriteMemory32(context.Registers[n], context.FloatingBits(m));
        }
        else
        {
            int n = GetN(opcode);
            int m = GetM(opcode)>>1;

            if (((opcode >> 4) & 0x1) == 0)
                context.WriteMemory64(context.Registers[n], context.DoubleBits(m));
            else
                context.WriteMemory64(context.Registers[n], context.ExtendedDoubleBits(m));
        }
    }

    internal partial void Execute_i1111_nnnn_mmmm_1011(Sh4Context context, uint opcode)
    {
        if (!context.Fpscr.Sz)
        {
            int n = GetN(opcode);
            int m = GetM(opcode);

            uint address = context.Registers[n] - 4;

            context.WriteMemory32(address, context.FloatingBits(m));

            context.Registers[n] = address;
        }
        else
        {
            int n = GetN(opcode);
            int m = GetM(opcode)>>1;

            uint address = context.Registers[n] - 8;
            if (((opcode >> 4) & 0x1) == 0)
                context.WriteMemory64(address, context.DoubleBits(m));
            else
                context.WriteMemory64(address, context.ExtendedDoubleBits(m));

            context.Registers[n] = address;
        }
    }

    internal partial void Execute_i1111_nnnn_mmmm_1100(Sh4Context context, uint opcode)
    {
        if (!context.Fpscr.Sz)
        {
            int n = GetN(opcode);
            int m = GetM(opcode);
            context.FloatingRegisters[n] = context.FloatingRegisters[m];
        }
        else
        {
            int n = GetN(opcode)>>1;
            int m = GetM(opcode)>>1;
            switch ((opcode >> 4) & 0x11)
            {
                case 0x00:

                    context.DoubleBits(n) = context.DoubleBits(m);
                    break;

                case 0x01:

                    context.DoubleBits(n) = context.ExtendedDoubleBits(m);
                    break;

                case 0x10:

                    context.ExtendedDoubleBits(n) = context.DoubleBits(m);
                    break;

                case 0x11:

                    context.ExtendedDoubleBits(n) = context.ExtendedDoubleBits(m);
                    break;
            }
        }
    }

    internal partial void Execute_i1111_nnnn_0101_1101(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);

        if (!context.Fpscr.Pr)
            context.FloatingBits(n) &= 0x7FFFFFFF;
        else
            context.FloatingBits(n & 0xE) &= 0x7FFFFFFF;

    }

    internal partial void Execute_i1111_nnn0_1111_1101(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode) & 0xE;

        if (!context.Fpscr.Pr)
        {
            uint phase = context.Fpul & 0xFFFF;

#if NATIVE_FSCA
                float rads = phase / (65536.0f / 2) * MathF.PI;

                context.FloatingRegisters[n + 0] = MathF.Sin(rads);
                context.FloatingRegisters[n + 1] = MathF.Cos(rads);

                context.FloatingRegisters[n] = Sh4FloatingPoint.FixNaN(context.FloatingRegisters[n]);
                context.FloatingRegisters[n + 1] = Sh4FloatingPoint.FixNaN(context.FloatingRegisters[n + 1]);
#else
                (context.FloatingRegisters[n], context.FloatingRegisters[n + 1]) = Sh4FscaTable.GetPair((int)phase);
#endif

        }
        else
            Unimplemented("FSCA : Double precision mode");
    }

    internal partial void Execute_i1111_nnnn_0111_1101(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        if (!context.Fpscr.Pr)
        {
            context.FloatingRegisters[n] = 1.f / MathF.Sqrt(context.FloatingRegisters[n]);
            context.FloatingRegisters[n] = Sh4FloatingPoint.FixNaN(context.FloatingRegisters[n]);
        }
        else
            Unimplemented("FSRRA : Double precision mode");
    }

    internal partial void Execute_i1111_nnnn_1011_1101(Sh4Context context, uint opcode)
    {

        if (context.Fpscr.Pr)
        {
            context.Fpul = BitConverter.SingleToUInt32Bits((float)GetDoubleN(context, opcode));
        }
        else
        {
            Unimplemented("FCNVDS: Single precision mode");
        }
    }

    internal partial void Execute_i1111_nnnn_1010_1101(Sh4Context context, uint opcode)
    {
        if (context.Fpscr.Pr)
        {
            SetDoubleN(context, opcode, BitConverter.UInt32BitsToSingle(context.Fpul));
        }
        else
        {
            Unimplemented("FCNVSD: Single precision mode");
        }
    }

    internal partial void Execute_i1111_nnmm_1110_1101(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode) & 0xC;
        int m = (GetN(opcode) & 0x3) << 2;
        if (!context.Fpscr.Pr)
        {
            double idp = (double)context.FloatingRegisters[n + 0] * context.FloatingRegisters[m + 0];
            idp += (double)context.FloatingRegisters[n + 1] * context.FloatingRegisters[m + 1];
            idp += (double)context.FloatingRegisters[n + 2] * context.FloatingRegisters[m + 2];
            idp += (double)context.FloatingRegisters[n + 3] * context.FloatingRegisters[m + 3];

            context.FloatingRegisters[n + 3] = Sh4FloatingPoint.FixNaN((float)idp);
        }
        else {
            Unimplemented("FIPR with FPSCR.PR=1");
        }
    }

    internal partial void Execute_i1111_nnnn_1000_1101(Sh4Context context, uint opcode)
    {
        if (context.Fpscr.Pr)
            return;

        int n = GetN(opcode);

        context.FloatingRegisters[n] = 0.0f;

    }

    internal partial void Execute_i1111_nnnn_1001_1101(Sh4Context context, uint opcode)
    {
        if (context.Fpscr.Pr)
            return;

        int n = GetN(opcode);

        context.FloatingRegisters[n] = 1.0f;
    }

    internal partial void Execute_i1111_nnnn_0001_1101(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);

        context.Fpul = context.FloatingBits(n);
    }

    internal partial void Execute_i1111_nnnn_0000_1101(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        context.FloatingBits(n) = context.Fpul;
    }

    internal partial void Execute_i1111_nnnn_0010_1101(Sh4Context context, uint opcode)
    {
        if (!context.Fpscr.Pr)
        {
            int n = GetN(opcode);
            context.FloatingRegisters[n] = (float)(int)context.Fpul;
        }
        else
        {
            SetDoubleN(context, opcode, (double)(int)context.Fpul);
        }
    }

    internal partial void Execute_i1111_nnnn_0100_1101(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        context.FloatingBits(n) ^= 0x80000000;
    }

    internal partial void Execute_i1111_1011_1111_1101(Sh4Context context, uint opcode)
    {
         context.Fpscr.Fr = !context.Fpscr.Fr;

        context.UpdateFpscr();
    }

    internal partial void Execute_i1111_0011_1111_1101(Sh4Context context, uint opcode)
    {
        context.Fpscr.Sz = !context.Fpscr.Sz;
    }

    internal partial void Execute_i1111_nnnn_0110_1101(Sh4Context context, uint opcode)
    {
        if (!context.Fpscr.Pr)
        {
            int n = GetN(opcode);

            context.FloatingRegisters[n] = MathF.Sqrt(context.FloatingRegisters[n]);
            context.FloatingRegisters[n] = Sh4FloatingPoint.FixNaN(context.FloatingRegisters[n]);
        }
        else
        {
            SetDoubleN(context, opcode, Sh4FloatingPoint.FixNaN64(Math.Sqrt(GetDoubleN(context, opcode))));
        }
    }

    internal partial void Execute_i1111_nnnn_0011_1101(Sh4Context context, uint opcode)
    {
        if (!context.Fpscr.Pr)
        {
            int n = GetN(opcode);
            if (float.IsNaN(context.FloatingRegisters[n])) {
                context.Fpul = 0x80000000;
            }
            else
            {
                context.Fpul = TruncateToHostInt32(context.FloatingRegisters[n]);
                if (unchecked((int)context.Fpul) > 0x7fffff80)
                    context.Fpul = 0x7fffffff;

                else if (IsX86 && context.Fpul == 0x80000000 && context.FloatingRegisters[n] > 0)
                    context.Fpul--;

            }
        }
        else
        {
            double f = GetDoubleN(context, opcode);
            if (double.IsNaN(f)) {
                context.Fpul = 0x80000000;
            }
            else
            {
                context.Fpul = TruncateToHostInt32(f);

                if (IsX86 && context.Fpul == 0x80000000 && f > 0)
                    context.Fpul--;

            }
        }
    }

    internal partial void Execute_i1111_nnnn_mmmm_1110(Sh4Context context, uint opcode)
    {
        if (!context.Fpscr.Pr)
        {
            int n = GetN(opcode);
            int m = GetM(opcode);

            context.FloatingRegisters[n] = MathF.FusedMultiplyAdd(context.FloatingRegisters[0], context.FloatingRegisters[m], context.FloatingRegisters[n]);
            context.FloatingRegisters[n] = Sh4FloatingPoint.FixNaN(context.FloatingRegisters[n]);
        }
        else
        {
            Unimplemented("fmac <DREG_0>,<DREG_M>,<DREG_N>");
        }
    }

    internal partial void Execute_i1111_nn01_1111_1101(Sh4Context context, uint opcode)
    {

        int n = GetN(opcode) & 0xC;

        if (!context.Fpscr.Pr)
        {
            double v1 = (double)context.ExtendedFloatingRegisters[0]  * context.FloatingRegisters[n + 0] +
                        (double)context.ExtendedFloatingRegisters[4]  * context.FloatingRegisters[n + 1] +
                        (double)context.ExtendedFloatingRegisters[8]  * context.FloatingRegisters[n + 2] +
                        (double)context.ExtendedFloatingRegisters[12] * context.FloatingRegisters[n + 3];

            double v2 = (double)context.ExtendedFloatingRegisters[1]  * context.FloatingRegisters[n + 0] +
                        (double)context.ExtendedFloatingRegisters[5]  * context.FloatingRegisters[n + 1] +
                        (double)context.ExtendedFloatingRegisters[9]  * context.FloatingRegisters[n + 2] +
                        (double)context.ExtendedFloatingRegisters[13] * context.FloatingRegisters[n + 3];

            double v3 = (double)context.ExtendedFloatingRegisters[2]  * context.FloatingRegisters[n + 0] +
                        (double)context.ExtendedFloatingRegisters[6]  * context.FloatingRegisters[n + 1] +
                        (double)context.ExtendedFloatingRegisters[10] * context.FloatingRegisters[n + 2] +
                        (double)context.ExtendedFloatingRegisters[14] * context.FloatingRegisters[n + 3];

            double v4 = (double)context.ExtendedFloatingRegisters[3]  * context.FloatingRegisters[n + 0] +
                        (double)context.ExtendedFloatingRegisters[7]  * context.FloatingRegisters[n + 1] +
                        (double)context.ExtendedFloatingRegisters[11] * context.FloatingRegisters[n + 2] +
                        (double)context.ExtendedFloatingRegisters[15] * context.FloatingRegisters[n + 3];

            context.FloatingRegisters[n + 0] = Sh4FloatingPoint.FixNaN((float)v1);
            context.FloatingRegisters[n + 1] = Sh4FloatingPoint.FixNaN((float)v2);
            context.FloatingRegisters[n + 2] = Sh4FloatingPoint.FixNaN((float)v3);
            context.FloatingRegisters[n + 3] = Sh4FloatingPoint.FixNaN((float)v4);
        }
        else {
            Unimplemented("FTRV with FPSCR.PR=1");
        }
    }

    private static bool IsX86 => RuntimeInformation.ProcessArchitecture is Architecture.X86 or Architecture.X64;

    private static uint TruncateToHostInt32(float value)
    {
        if (Sse.IsSupported)
            return unchecked((uint)Sse.ConvertToInt32WithTruncation(Vector128.CreateScalarUnsafe(value)));
        return TruncateToHostInt32((double)value);
    }

    private static uint TruncateToHostInt32(double value)
    {
        if (Sse2.IsSupported)
            return unchecked((uint)Sse2.ConvertToInt32WithTruncation(Vector128.CreateScalarUnsafe(value)));
        if (value < int.MinValue || (IsX86 && value >= 2147483648.0))
            return 0x80000000;
        if (value >= 2147483648.0)
            return 0x7FFFFFFF;
        return unchecked((uint)(int)value);
    }

    private static void Unimplemented(string operation) =>
        Trace.TraceWarning($"Unimplemented SH4 FPU op: {operation}");
}
