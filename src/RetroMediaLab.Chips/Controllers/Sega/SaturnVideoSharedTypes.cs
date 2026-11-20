// Translated from Yabause vidshared.h.
// SPDX-License-Identifier: GPL-2.0-or-later

using static RetroMediaLab.Chips.Controllers.Sega.SaturnMemoryAccess;

namespace RetroMediaLab.Chips.Controllers.Sega;

internal static class SaturnVideoSharedTypes
{
    internal struct Clipping
    {
        internal int XStart, YStart, XEnd, YEnd;
    }

    internal struct Vdp2Draw
    {
        internal int CellWidth, CellHeight;
        internal int FlipFunction;
        internal int Priority;
        internal int ColorOffsetRed, ColorOffsetGreen, ColorOffsetBlue;
        internal int LineScreen;
        internal int CellWidthBits, CellHeightBits;
        internal int MapWidth;
        internal int PlaneWidth, PlaneWidthBits, PlaneHeight, PlaneHeightBits;
        internal int PageWidth, PageWidthBits;
        internal int PatternWidth, PatternWidthBits;
        internal int PatternDataSize, PatternDataSizeBits;
        internal int SpecialFunction, SpecialColorFunction;
        internal int SpecialColorMode, SpecialCode;
        internal uint Address, CharacterAddress, PaletteAddress;
        internal int ColorNumber;
        internal bool IsBitmap;
        internal ushort SupplementData;
        internal int AuxiliaryMode;
        internal bool Enabled;
        internal int X, Y, Alpha;
        internal int ColorOffset;
        internal bool TransparencyEnabled;
        internal int SpecialPriorityMode;
        internal SaturnTitanLayer Layer;
        internal SaturnTitanShadow ShadowType;
        internal bool ShadowEnabled;
        internal float CoordinateIncrementX, CoordinateIncrementY;
        internal unsafe delegate*<ref Vdp2Draw, int, in SaturnVdp2Registers, void> PlaneAddr;
        internal unsafe delegate*<in Vdp2Draw, uint, uint> PostPixelFetchCalc;
        internal unsafe delegate*<ref Vdp2Draw, ref SaturnVideoSoftware.ScreenInfo, int, ReadOnlySpan<SaturnVdp2Registers>, void> LoadLineParams;
        internal int RotateNumber, RotateMode;
        internal int MosaicXMask, MosaicYMask;
        internal int LineScroll;
        internal uint LineScrollTable, LineIncrement;
        internal int WindowControl, LineWindow;
        internal bool VerticalScroll;
        internal uint VerticalScrollTable;
        internal int VerticalScrollIncrement;
        internal PatternPipeline PreviousPattern, CurrentPattern;
    }

    internal struct PatternPipeline
    {
        internal uint PaletteAddress, CharacterAddress;
        internal int FlipFunction;
    }

    internal static void CalcPlaneAddr(ref Vdp2Draw info, uint address)
    {
        int shift = info.PlaneHeight + info.PlaneWidth - 2;
        int scale = info.PlaneHeight * info.PlaneWidth;

        if (info.PatternDataSize == 1)
        {
            if (info.PatternWidth == 1)
                info.Address = ((address & 0x3F) >> shift) * (uint)(scale * 0x2000);
            else
                info.Address = (address >> shift) * (uint)(scale * 0x800);
        }
        else
        {
            if (info.PatternWidth == 1)
                info.Address = ((address & 0x1F) >> shift) * (uint)(scale * 0x4000);
            else
                info.Address = ((address & 0x7F) >> shift) * (uint)(scale * 0x1000);
        }
    }

    internal static void ReadBitmapSize(ref Vdp2Draw info, int bitmap, int mask)
    {
        switch (bitmap & mask)
        {
            case 0:
                info.CellWidth = 512;
                info.CellWidthBits = 9;
                info.CellHeight = 256;
                info.CellHeightBits = 8;
                break;
            case 1:
                info.CellWidth = 512;
                info.CellWidthBits = 9;
                info.CellHeight = 512;
                info.CellHeightBits = 9;
                break;
            case 2:
                info.CellWidth = 1024;
                info.CellWidthBits = 10;
                info.CellHeight = 256;
                info.CellHeightBits = 8;
                break;
            case 3:
                info.CellWidth = 1024;
                info.CellWidthBits = 10;
                info.CellHeight = 512;
                info.CellHeightBits = 9;
                break;
        }
    }

    internal static void ReadPlaneSize(ref Vdp2Draw info, int value)
    {
        switch (value & 3)
        {
            case 0:
                info.PlaneWidth = info.PlaneHeight = 1;
                info.PlaneWidthBits = info.PlaneHeightBits = 0;
                break;
            case 1:
                info.PlaneWidth = 2;
                info.PlaneWidthBits = 1;
                info.PlaneHeight = 1;
                info.PlaneHeightBits = 0;
                break;
            case 3:
                info.PlaneWidth = info.PlaneHeight = 2;
                info.PlaneWidthBits = info.PlaneHeightBits = 1;
                break;
            default:
                info.PlaneWidth = info.PlaneHeight = 1;
                info.PlaneWidthBits = info.PlaneHeightBits = 0;
                break;
        }
    }

    internal static void ReadPatternData(ref Vdp2Draw info, ushort patternNameControl, int characterControlWidth)
    {
        if ((patternNameControl & 0x8000) != 0)
        {
            info.PatternDataSize = 1;
            info.PatternDataSizeBits = 0;
        }
        else
        {
            info.PatternDataSize = 2;
            info.PatternDataSizeBits = 1;
        }

        if (characterControlWidth != 0)
        {
            info.PatternWidth = 2;
            info.PatternWidthBits = 1;
        }
        else
        {
            info.PatternWidth = 1;
            info.PatternWidthBits = 0;
        }

        info.PageWidth = 64 >> info.PatternWidthBits;
        info.PageWidthBits = 6 - info.PatternWidthBits;
        info.CellWidth = info.CellHeight = 8;
        info.CellWidthBits = info.CellHeightBits = 3;
        info.SupplementData = (ushort)(patternNameControl & 0x3FF);
        info.AuxiliaryMode = (patternNameControl & 0x4000) >> 14;
    }

    internal static void ReadMosaicData(ref Vdp2Draw info, int mask, in SaturnVdp2Registers registers)
    {
        if ((registers.MZCTL & mask) != 0)
        {
            info.MosaicXMask = ((registers.MZCTL >> 8) & 0xF) + 1;
            info.MosaicYMask = (registers.MZCTL >> 12) + 1;
        }
        else
        {
            info.MosaicXMask = 1;
            info.MosaicYMask = 1;
        }
    }

    internal static void ReadLineScrollData(ref Vdp2Draw info, int mask, uint table)
    {
        if ((mask & 0xE) != 0)
        {
            info.LineScroll = (mask >> 1) & 7;
            info.LineScrollTable = (table & 0x7FFFE) << 1;
            info.LineIncrement = 1u << ((mask >> 4) & 3);
        }
        else
        {
            info.LineScroll = 0;
            info.LineIncrement = 0;
        }
    }

    internal struct SpritePixelInfo
    {
        internal bool NormalShadow, MsbShadow;
        internal int Priority, ColorCalculation;
    }

    internal struct RotationParameters
    {
        internal int Xst, Yst, Zst;
        internal int DeltaXst, DeltaYst, DeltaX, DeltaY;
        internal int A, B, C, D, E, F;
        internal int Px, Py, Pz, Cx, Cy, Cz;
        internal int Mx, My, Kx, Ky;
        internal int Xp, Yp, Dx, Dy;
        internal uint KAst;
        internal int DeltaKAst, DeltaKAx;
        internal int CoefficientEnabled, ScreenOver, CoefficientDataSize, CoefficientMode;
        internal uint CoefficientTableAddress;
        internal int Msb, LineScreen;
    }

    internal const int FractionBits = 16;

    internal static ushort ToUnsignedFixedInteger(int value) => unchecked((ushort)(value >> FractionBits));

    internal static int ToFixed(int value) => value * (1 << FractionBits);

    internal static float FixedToFloat(int value) => (float)value / (1 << FractionBits);

    internal static int MultiplyFixed(int left, int right) =>
        unchecked((int)((long)left * right >> FractionBits));

    internal static void GenerateRotatedVarFixed(in RotationParameters p, out int xmul, out int ymul,
        out int c, out int f)
    {
        xmul = p.Xst - p.Px;
        ymul = p.Yst - p.Py;
        c = MultiplyFixed(p.C, p.Zst - p.Pz);
        f = MultiplyFixed(p.F, p.Zst - p.Pz);
    }

    internal static int GenerateRotatedXPosFixed(in RotationParameters p, int x, int xmul, int ymul, int c)
    {
        int xsp = MultiplyFixed(p.A, xmul) + MultiplyFixed(p.B, ymul) + c;
        return ToUnsignedFixedInteger(MultiplyFixed(p.Kx, xsp + MultiplyFixed(p.Dx, ToFixed(x))) + p.Xp);
    }

    internal static int GenerateRotatedYPosFixed(in RotationParameters p, int x, int xmul, int ymul, int f)
    {
        int ysp = MultiplyFixed(p.D, xmul) + MultiplyFixed(p.E, ymul) + f;
        return ToUnsignedFixedInteger(MultiplyFixed(p.Ky, ysp + MultiplyFixed(p.Dy, ToFixed(x))) + p.Yp);
    }

    internal static void CalculateRotationValuesFixed(ref RotationParameters p)
    {
        p.Xp = MultiplyFixed(p.A, p.Px - p.Cx) + MultiplyFixed(p.B, p.Py - p.Cy) +
            MultiplyFixed(p.C, p.Pz - p.Cz) + p.Cx + p.Mx;
        p.Yp = MultiplyFixed(p.D, p.Px - p.Cx) + MultiplyFixed(p.E, p.Py - p.Cy) +
            MultiplyFixed(p.F, p.Pz - p.Cz) + p.Cy + p.My;
        p.Dx = MultiplyFixed(p.A, p.DeltaX) + MultiplyFixed(p.B, p.DeltaY);
        p.Dy = MultiplyFixed(p.D, p.DeltaX) + MultiplyFixed(p.E, p.DeltaY);
    }

    internal static bool IsScreenRotatedFixed(in RotationParameters p) =>
        p.DeltaXst == 0 && p.DeltaYst == ToFixed(1) && p.DeltaX == ToFixed(1) && p.DeltaY == 0 &&
        p.A == ToFixed(1) && p.B == 0 && p.C == 0 && p.D == 0 && p.E == ToFixed(1) && p.F == 0;

    internal static void ReadCoefficientFixed(ref RotationParameters parameter, uint address,
        ReadOnlySpan<byte> coefficientRam, bool fromColorRam)
    {
        switch (parameter.CoefficientMode)
        {
            case 0:
                parameter.Kx = parameter.Ky = SaturnVideoShared.ReadCoefficientMode0To2Fixed(ref parameter, address, coefficientRam, fromColorRam);
                break;
            case 1:
                parameter.Kx = SaturnVideoShared.ReadCoefficientMode0To2Fixed(ref parameter, address, coefficientRam, fromColorRam);
                break;
            case 2:
                parameter.Ky = SaturnVideoShared.ReadCoefficientMode0To2Fixed(ref parameter, address, coefficientRam, fromColorRam);
                break;
            case 3:
                if (parameter.CoefficientDataSize == 2)
                {
                    uint value = SaturnVideoShared.ReadCoefficientWord(coefficientRam, address, fromColorRam);
                    parameter.Msb = (int)((value >> 15) & 1);
                    parameter.Xp = unchecked((int)((value & 0x7FFF) |
                        ((value & 0x4000) != 0 ? 0xFFFFC000u : 0)) * 16384);
                }
                else
                {
                    uint value = SaturnVideoShared.ReadCoefficientLong(coefficientRam, address, fromColorRam);
                    parameter.Msb = (int)((value >> 31) & 1);
                    parameter.LineScreen = (int)((value >> 24) & 0x7F);
                    parameter.Xp = unchecked((int)((value & 0x007FFFFF) |
                        ((value & 0x00800000) != 0 ? 0xFF800000u : 0)) * 256);
                }
                break;
        }
    }

    internal static SpritePixelInfo GetSpritePixelInfo(int type, ref ushort pixel)
    {
        SpritePixelInfo info = default;
        switch (type)
        {
            case 0x0:
                info.Priority = pixel >> 14;
                info.ColorCalculation = (pixel >> 11) & 0x7;
                pixel &= 0x7FF;
                info.NormalShadow = (pixel == 0x7FE);
                break;
            case 0x1:
                info.Priority = pixel >> 13;
                info.ColorCalculation = (pixel >> 11) & 0x3;
                pixel &= 0x7FF;
                info.NormalShadow = (pixel == 0x7FE);
                break;
            case 0x2:
                info.MsbShadow = (pixel >> 15) != 0;
                info.Priority = (pixel >> 14) & 0x1;
                info.ColorCalculation = (pixel >> 11) & 0x7;
                pixel &= 0x7FF;
                info.NormalShadow = (pixel == 0x7FE);
                break;
            case 0x3:
                info.MsbShadow = (pixel >> 15) != 0;
                info.Priority = (pixel >> 13) & 0x3;
                info.ColorCalculation = (pixel >> 11) & 0x3;
                pixel &= 0x7FF;
                info.NormalShadow = (pixel == 0x7FE);
                break;
            case 0x4:
                info.MsbShadow = (pixel >> 15) != 0;
                info.Priority = (pixel >> 13) & 0x3;
                info.ColorCalculation = (pixel >> 10) & 0x7;
                pixel &= 0x3FF;
                info.NormalShadow = (pixel == 0x3FE);
                break;
            case 0x5:
                info.MsbShadow = (pixel >> 15) != 0;
                info.Priority = (pixel >> 12) & 0x7;
                info.ColorCalculation = (pixel >> 11) & 0x1;
                pixel &= 0x7FF;
                info.NormalShadow = (pixel == 0x7FE);
                break;
            case 0x6:
                info.MsbShadow = (pixel >> 15) != 0;
                info.Priority = (pixel >> 12) & 0x7;
                info.ColorCalculation = (pixel >> 10) & 0x3;
                pixel &= 0x3FF;
                info.NormalShadow = (pixel == 0x3FE);
                break;
            case 0x7:
                info.MsbShadow = (pixel >> 15) != 0;
                info.Priority = (pixel >> 12) & 0x7;
                info.ColorCalculation = (pixel >> 9) & 0x7;
                pixel &= 0x1FF;
                info.NormalShadow = (pixel == 0x1FE);
                break;
            case 0x8:
                info.Priority = (pixel >> 7) & 0x1;
                pixel &= 0x7F;
                info.NormalShadow = (pixel == 0x7E);
                break;
            case 0x9:
                info.Priority = (pixel >> 7) & 0x1;
                info.ColorCalculation = (pixel >> 6) & 0x1;
                pixel &= 0x3F;
                info.NormalShadow = (pixel == 0x3E);
                break;
            case 0xA:
                info.Priority = (pixel >> 6) & 0x3;
                pixel &= 0x3F;
                info.NormalShadow = (pixel == 0x3E);
                break;
            case 0xB:
                info.ColorCalculation = (pixel >> 6) & 0x3;
                pixel &= 0x3F;
                info.NormalShadow = (pixel == 0x3E);
                break;
            case 0xC:
                info.Priority = (pixel >> 7) & 0x1;
                info.NormalShadow = (pixel == 0xFE);
                break;
            case 0xD:
                info.Priority = (pixel >> 7) & 0x1;
                info.ColorCalculation = (pixel >> 6) & 0x1;
                info.NormalShadow = (pixel == 0xFE);
                break;
            case 0xE:
                info.Priority = (pixel >> 6) & 0x3;
                info.NormalShadow = (pixel == 0xFE);
                break;
            case 0xF:
                info.ColorCalculation = (pixel >> 6) & 0x3;
                info.NormalShadow = (pixel == 0xFE);
                break;
        }
        return info;
    }

    private static void ReadWindowCoordinates(int number, ref Clipping clip, in SaturnVdp2Registers registers)
    {
        if (number == 0)
        {
            clip.XStart = registers.WPSX0;
            clip.YStart = registers.WPSY0 & 0x1FF;
            clip.XEnd = registers.WPEX0;
            clip.YEnd = registers.WPEY0 & 0x1FF;
        }
        else
        {
            clip.XStart = registers.WPSX1;
            clip.YStart = registers.WPSY1 & 0x1FF;
            clip.XEnd = registers.WPEX1;
            clip.YEnd = registers.WPEY1 & 0x1FF;
        }

        switch ((registers.TVMD >> 1) & 3)
        {
            case 0:
                clip.XStart = (clip.XStart >> 1) & 0x1FF;
                clip.XEnd = (clip.XEnd >> 1) & 0x1FF;
                break;
            case 1:
                clip.XStart &= 0x3FF;
                clip.XEnd &= 0x3FF;
                break;
            case 2:
                clip.XStart &= 0x1FF;
                clip.XEnd &= 0x1FF;
                break;
            case 3:
                clip.XStart = (clip.XStart & 0x3FF) >> 1;
                clip.XEnd = (clip.XEnd & 0x3FF) >> 1;
                break;
        }
    }

    internal static void ReadWindowData(int control, Span<Clipping> clips, in SaturnVdp2Registers registers)
    {
        clips[0] = default;
        clips[1] = default;
        if ((control & 2) != 0)
            ReadWindowCoordinates(0, ref clips[0], registers);
        if ((control & 8) != 0)
            ReadWindowCoordinates(1, ref clips[1], registers);
    }

    internal static int ReadLineWindowData(int control, ref uint window0, ref uint window1, in SaturnVdp2Registers registers)
    {
        int lineWindow = 0;
        if ((control & 2) != 0 && (registers.LWTA0.All & 0x80000000) != 0)
        {
            lineWindow |= 1;
            window0 = (registers.LWTA0.All & 0x7FFFE) << 1;
        }
        if ((control & 8) != 0 && (registers.LWTA1.All & 0x80000000) != 0)
        {
            lineWindow |= 2;
            window1 = (registers.LWTA1.All & 0x7FFFE) << 1;
        }
        return lineWindow;
    }

    private static void ReadOneLineWindowClip(ref Clipping clip, ref uint address,
        ReadOnlySpan<byte> ram, in SaturnVdp2Registers registers)
    {
        clip.XStart = T1ReadWord(ram, address & 0x7FFFF);
        address += 2;
        clip.XEnd = T1ReadWord(ram, address & 0x7FFFF);
        address += 2;
        if (clip.XEnd == 0xFFFF)
        {
            clip.XStart = 0;
            clip.XEnd = 0;
            return;
        }
        clip.XStart &= 0x3FF;
        clip.XEnd &= 0x3FF;

        switch ((registers.TVMD >> 1) & 3)
        {
            case 0:
                clip.XStart = (clip.XStart >> 1) & 0x1FF;
                clip.XEnd = (clip.XEnd >> 1) & 0x1FF;
                break;
            case 1:
                clip.XStart &= 0x3FF;
                clip.XEnd &= 0x3FF;
                break;
            case 2:
                clip.XStart &= 0x1FF;
                clip.XEnd &= 0x1FF;
                break;
            case 3:
                clip.XStart = (clip.XStart & 0x3FF) >> 1;
                clip.XEnd = (clip.XEnd & 0x3FF) >> 1;
                break;
        }
    }

    internal static void ReadLineWindowClip(int lineWindow, Span<Clipping> clips,
        ref uint window0, ref uint window1, ReadOnlySpan<byte> ram, in SaturnVdp2Registers registers)
    {
        if (lineWindow != 0)
        {
            if ((lineWindow & 1) != 0)
                ReadOneLineWindowClip(ref clips[0], ref window0, ram, registers);
            if ((lineWindow & 2) != 0)
                ReadOneLineWindowClip(ref clips[1], ref window1, ram, registers);
        }
    }
}
