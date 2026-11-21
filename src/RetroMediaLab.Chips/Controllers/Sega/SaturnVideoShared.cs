// Translated from Yabause vidshared.c.
// SPDX-License-Identifier: GPL-2.0-or-later

using static RetroMediaLab.Chips.Controllers.Sega.SaturnMemoryAccess;
using static RetroMediaLab.Chips.Controllers.Sega.SaturnVideoSharedTypes;

namespace RetroMediaLab.Chips.Controllers.Sega;

internal static class SaturnVideoShared
{
    internal static void Vdp2Nbg0PlaneAddr(ref Vdp2Draw info, int plane, in SaturnVdp2Registers registers)
    {
        uint offset = (uint)((registers.MPOFN & 7) << 6);
        uint address = 0;
        switch (plane)
        {
            case 0: address = offset | (uint)(registers.MPABN0 & 0xFF); break;
            case 1: address = offset | (uint)(registers.MPABN0 >> 8); break;
            case 2: address = offset | (uint)(registers.MPCDN0 & 0xFF); break;
            case 3: address = offset | (uint)(registers.MPCDN0 >> 8); break;
        }
        CalcPlaneAddr(ref info, address);
    }

    internal static void Vdp2Nbg1PlaneAddr(ref Vdp2Draw info, int plane, in SaturnVdp2Registers registers)
    {
        uint offset = (uint)((registers.MPOFN & 0x70) << 2);
        uint address = 0;
        switch (plane)
        {
            case 0: address = offset | (uint)(registers.MPABN1 & 0xFF); break;
            case 1: address = offset | (uint)(registers.MPABN1 >> 8); break;
            case 2: address = offset | (uint)(registers.MPCDN1 & 0xFF); break;
            case 3: address = offset | (uint)(registers.MPCDN1 >> 8); break;
        }
        CalcPlaneAddr(ref info, address);
    }

    internal static void Vdp2Nbg2PlaneAddr(ref Vdp2Draw info, int plane, in SaturnVdp2Registers registers)
    {
        uint offset = (uint)((registers.MPOFN & 0x700) >> 2);
        uint address = 0;
        switch (plane)
        {
            case 0: address = offset | (uint)(registers.MPABN2 & 0xFF); break;
            case 1: address = offset | (uint)(registers.MPABN2 >> 8); break;
            case 2: address = offset | (uint)(registers.MPCDN2 & 0xFF); break;
            case 3: address = offset | (uint)(registers.MPCDN2 >> 8); break;
        }
        CalcPlaneAddr(ref info, address);
    }

    internal static void Vdp2Nbg3PlaneAddr(ref Vdp2Draw info, int plane, in SaturnVdp2Registers registers)
    {
        uint offset = (uint)((registers.MPOFN & 0x7000) >> 6);
        uint address = 0;
        switch (plane)
        {
            case 0: address = offset | (uint)(registers.MPABN3 & 0xFF); break;
            case 1: address = offset | (uint)(registers.MPABN3 >> 8); break;
            case 2: address = offset | (uint)(registers.MPCDN3 & 0xFF); break;
            case 3: address = offset | (uint)(registers.MPCDN3 >> 8); break;
        }
        CalcPlaneAddr(ref info, address);
    }

    internal static void Vdp2ParameterAPlaneAddr(ref Vdp2Draw info, int plane, in SaturnVdp2Registers registers)
    {
        uint offset = (uint)((registers.MPOFR & 7) << 6);
        uint address = 0;
        switch (plane)
        {
            case 0: address = offset | (uint)(registers.MPABRA & 0xFF); break;
            case 1: address = offset | (uint)(registers.MPABRA >> 8); break;
            case 2: address = offset | (uint)(registers.MPCDRA & 0xFF); break;
            case 3: address = offset | (uint)(registers.MPCDRA >> 8); break;
            case 4: address = offset | (uint)(registers.MPEFRA & 0xFF); break;
            case 5: address = offset | (uint)(registers.MPEFRA >> 8); break;
            case 6: address = offset | (uint)(registers.MPGHRA & 0xFF); break;
            case 7: address = offset | (uint)(registers.MPGHRA >> 8); break;
            case 8: address = offset | (uint)(registers.MPIJRA & 0xFF); break;
            case 9: address = offset | (uint)(registers.MPIJRA >> 8); break;
            case 10: address = offset | (uint)(registers.MPKLRA & 0xFF); break;
            case 11: address = offset | (uint)(registers.MPKLRA >> 8); break;
            case 12: address = offset | (uint)(registers.MPMNRA & 0xFF); break;
            case 13: address = offset | (uint)(registers.MPMNRA >> 8); break;
            case 14: address = offset | (uint)(registers.MPOPRA & 0xFF); break;
            case 15: address = offset | (uint)(registers.MPOPRA >> 8); break;
        }
        CalcPlaneAddr(ref info, address);
    }

    internal static void Vdp2ParameterBPlaneAddr(ref Vdp2Draw info, int plane, in SaturnVdp2Registers registers)
    {
        uint offset = (uint)((registers.MPOFR & 0x70) << 2);
        uint address = 0;
        switch (plane)
        {
            case 0: address = offset | (uint)(registers.MPABRB & 0xFF); break;
            case 1: address = offset | (uint)(registers.MPABRB >> 8); break;
            case 2: address = offset | (uint)(registers.MPCDRB & 0xFF); break;
            case 3: address = offset | (uint)(registers.MPCDRB >> 8); break;
            case 4: address = offset | (uint)(registers.MPEFRB & 0xFF); break;
            case 5: address = offset | (uint)(registers.MPEFRB >> 8); break;
            case 6: address = offset | (uint)(registers.MPGHRB & 0xFF); break;
            case 7: address = offset | (uint)(registers.MPGHRB >> 8); break;
            case 8: address = offset | (uint)(registers.MPIJRB & 0xFF); break;
            case 9: address = offset | (uint)(registers.MPIJRB >> 8); break;
            case 10: address = offset | (uint)(registers.MPKLRB & 0xFF); break;
            case 11: address = offset | (uint)(registers.MPKLRB >> 8); break;
            case 12: address = offset | (uint)(registers.MPMNRB & 0xFF); break;
            case 13: address = offset | (uint)(registers.MPMNRB >> 8); break;
            case 14: address = offset | (uint)(registers.MPOPRB & 0xFF); break;
            case 15: address = offset | (uint)(registers.MPOPRB >> 8); break;
        }
        CalcPlaneAddr(ref info, address);
    }

    internal static int ReadCoefficientMode0To2Fixed(ref RotationParameters parameter, uint address,
        ReadOnlySpan<byte> coefficientRam, bool fromColorRam)
    {
        if (parameter.CoefficientDataSize == 2)
        {
            uint value = ReadCoefficientWord(coefficientRam, address, fromColorRam);
            parameter.Msb = (int)((value >> 15) & 1);
            return unchecked((int)((value & 0x7FFF) | ((value & 0x4000) != 0 ? 0xFFFFC000u : 0)) * 64);
        }
        else
        {
            uint value = ReadCoefficientLong(coefficientRam, address, fromColorRam);
            parameter.LineScreen = (int)((value >> 24) & 0x7F);
            parameter.Msb = (int)((value >> 31) & 1);
            return unchecked((int)((value & 0x00FFFFFF) | ((value & 0x00800000) != 0 ? 0xFF800000u : 0)));
        }
    }

    internal static ushort ReadCoefficientWord(ReadOnlySpan<byte> coefficientRam, uint address, bool fromColorRam) =>
        fromColorRam ? T2ReadWord(coefficientRam, address & 0x7FE) : T1ReadWord(coefficientRam, address & 0x7FFFE);

    internal static uint ReadCoefficientLong(ReadOnlySpan<byte> coefficientRam, uint address, bool fromColorRam) =>
        fromColorRam ? T2ReadLong(coefficientRam, address & 0x7FC) : T1ReadLong(coefficientRam, address & 0x7FFFC);

    internal static RotationParameters ReadRotationTableFixed(int which, in SaturnVdp2Registers registers,
        ReadOnlySpan<byte> ram)
    {
        RotationParameters parameters = default;
        uint address = registers.RPTA.All << 1;
        if (which == 0)
        {
            address &= 0x000FFF7C;
            parameters.CoefficientEnabled = registers.KTCTL & 1;
            parameters.ScreenOver = (registers.PLSZ >> 10) & 3;
        }
        else
        {
            address = (address & 0x000FFFFC) | 0x80;
            parameters.CoefficientEnabled = registers.KTCTL & 0x100;
            parameters.ScreenOver = (registers.PLSZ >> 14) & 3;
        }

        uint value;
        value = T1ReadLong(ram, address & 0x7FFFF);
        parameters.Xst = unchecked((int)((value & 0x1FFFFFC0) | ((value & 0x10000000) != 0 ? 0xF0000000u : 0)));
        address += 4;

        value = T1ReadLong(ram, address & 0x7FFFF);
        parameters.Yst = unchecked((int)((value & 0x1FFFFFC0) | ((value & 0x10000000) != 0 ? 0xF0000000u : 0)));
        address += 4;

        value = T1ReadLong(ram, address & 0x7FFFF);
        parameters.Zst = unchecked((int)((value & 0x1FFFFFC0) | ((value & 0x10000000) != 0 ? 0xF0000000u : 0)));
        address += 4;

        value = T1ReadLong(ram, address & 0x7FFFF);
        parameters.DeltaXst = unchecked((int)((value & 0x0007FFC0) | ((value & 0x00040000) != 0 ? 0xFFFC0000u : 0)));
        address += 4;

        value = T1ReadLong(ram, address & 0x7FFFF);
        parameters.DeltaYst = unchecked((int)((value & 0x0007FFC0) | ((value & 0x00040000) != 0 ? 0xFFFC0000u : 0)));
        address += 4;

        value = T1ReadLong(ram, address & 0x7FFFF);
        parameters.DeltaX = unchecked((int)((value & 0x0007FFC0) | ((value & 0x00040000) != 0 ? 0xFFFC0000u : 0)));
        address += 4;

        value = T1ReadLong(ram, address & 0x7FFFF);
        parameters.DeltaY = unchecked((int)((value & 0x0007FFC0) | ((value & 0x00040000) != 0 ? 0xFFFC0000u : 0)));
        address += 4;

        value = T1ReadLong(ram, address & 0x7FFFF);
        parameters.A = unchecked((int)((value & 0x000FFFC0) | ((value & 0x00080000) != 0 ? 0xFFF80000u : 0)));
        address += 4;

        value = T1ReadLong(ram, address & 0x7FFFF);
        parameters.B = unchecked((int)((value & 0x000FFFC0) | ((value & 0x00080000) != 0 ? 0xFFF80000u : 0)));
        address += 4;

        value = T1ReadLong(ram, address & 0x7FFFF);
        parameters.C = unchecked((int)((value & 0x000FFFC0) | ((value & 0x00080000) != 0 ? 0xFFF80000u : 0)));
        address += 4;

        value = T1ReadLong(ram, address & 0x7FFFF);
        parameters.D = unchecked((int)((value & 0x000FFFC0) | ((value & 0x00080000) != 0 ? 0xFFF80000u : 0)));
        address += 4;

        value = T1ReadLong(ram, address & 0x7FFFF);
        parameters.E = unchecked((int)((value & 0x000FFFC0) | ((value & 0x00080000) != 0 ? 0xFFF80000u : 0)));
        address += 4;

        value = T1ReadLong(ram, address & 0x7FFFF);
        parameters.F = unchecked((int)((value & 0x000FFFC0) | ((value & 0x00080000) != 0 ? 0xFFF80000u : 0)));
        address += 4;

        value = T1ReadWord(ram, address & 0x7FFFF);
        // The six viewpoint and center coordinates are signed 14-bit, as in
        // Mednafen vdp2.cpp and the floating-point table reader in Kronos vidshared.c.
        parameters.Px = unchecked((int)((value & 0x3FFF) | ((value & 0x2000) != 0 ? 0xFFFFC000u : 0))) << FractionBits;
        address += 2;

        value = T1ReadWord(ram, address & 0x7FFFF);
        parameters.Py = unchecked((int)((value & 0x3FFF) | ((value & 0x2000) != 0 ? 0xFFFFC000u : 0))) << FractionBits;
        address += 2;

        value = T1ReadWord(ram, address & 0x7FFFF);
        parameters.Pz = unchecked((int)((value & 0x3FFF) | ((value & 0x2000) != 0 ? 0xFFFFC000u : 0))) << FractionBits;
        address += 4;

        value = T1ReadWord(ram, address & 0x7FFFF);
        parameters.Cx = unchecked((int)((value & 0x3FFF) | ((value & 0x2000) != 0 ? 0xFFFFC000u : 0))) << FractionBits;
        address += 2;

        value = T1ReadWord(ram, address & 0x7FFFF);
        parameters.Cy = unchecked((int)((value & 0x3FFF) | ((value & 0x2000) != 0 ? 0xFFFFC000u : 0))) << FractionBits;
        address += 2;

        value = T1ReadWord(ram, address & 0x7FFFF);
        parameters.Cz = unchecked((int)((value & 0x3FFF) | ((value & 0x2000) != 0 ? 0xFFFFC000u : 0))) << FractionBits;
        address += 4;

        value = T1ReadLong(ram, address & 0x7FFFF);
        parameters.Mx = unchecked((int)((value & 0x3FFFFFC0) | ((value & 0x20000000) != 0 ? 0xE0000000u : 0)));
        address += 4;

        value = T1ReadLong(ram, address & 0x7FFFF);
        parameters.My = unchecked((int)((value & 0x3FFFFFC0) | ((value & 0x20000000) != 0 ? 0xE0000000u : 0)));
        address += 4;

        value = T1ReadLong(ram, address & 0x7FFFF);
        parameters.Kx = unchecked((int)((value & 0x00FFFFFF) | ((value & 0x00800000) != 0 ? 0xFF800000u : 0)));
        address += 4;

        value = T1ReadLong(ram, address & 0x7FFFF);
        parameters.Ky = unchecked((int)((value & 0x00FFFFFF) | ((value & 0x00800000) != 0 ? 0xFF800000u : 0)));
        address += 4;

        if (parameters.CoefficientEnabled != 0)
        {
            value = T1ReadLong(ram, address & 0x7FFFF);
            parameters.KAst = value & 0xFFFFFFC0;
            address += 4;

            value = T1ReadLong(ram, address & 0x7FFFF);
            parameters.DeltaKAst = unchecked((int)((value & 0x03FFFFC0) | ((value & 0x02000000) != 0 ? 0xFE000000u : 0)));
            address += 4;

            value = T1ReadLong(ram, address & 0x7FFFF);
            parameters.DeltaKAx = unchecked((int)((value & 0x03FFFFC0) | ((value & 0x02000000) != 0 ? 0xFE000000u : 0)));

            if (which == 0)
            {
                parameters.CoefficientDataSize = (registers.KTCTL & 2) != 0 ? 2 : 4;
                parameters.CoefficientTableAddress =
                    (uint)(((registers.KTAOF & 7) * 0x10000 + (ushort)(parameters.KAst >> FractionBits)) * parameters.CoefficientDataSize);
                parameters.CoefficientMode = (registers.KTCTL >> 2) & 3;
            }
            else
            {
                parameters.CoefficientDataSize = (registers.KTCTL & 0x200) != 0 ? 2 : 4;
                parameters.CoefficientTableAddress =
                    (uint)((((registers.KTAOF >> 8) & 7) * 0x10000 + (ushort)(parameters.KAst >> FractionBits)) * parameters.CoefficientDataSize);
                parameters.CoefficientMode = (registers.KTCTL >> 10) & 3;
            }
        }
        return parameters;
    }
}
