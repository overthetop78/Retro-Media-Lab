// Copyright 2003-2004 Guillaume Duhamel
// Copyright 2004-2008 Theo Berkau
// Copyright 2006 Fabien Coulon
// Copyright 2015 R. Danbrook
// Translated from Yabause vidsoft.c.
// SPDX-License-Identifier: GPL-2.0-or-later

using System.Buffers.Binary;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using static RetroMediaLab.Chips.Controllers.Sega.SaturnMemoryAccess;
using static RetroMediaLab.Chips.Controllers.Sega.SaturnVideoShared;
using static RetroMediaLab.Chips.Controllers.Sega.SaturnVideoSharedTypes;

namespace RetroMediaLab.Chips.Controllers.Sega;

public sealed unsafe class SaturnVideoSoftware : ISaturnVdp1Video, IDisposable
{
    private byte[] _backFrameBuffer = new byte[0x40000];
    private byte[] _frontFrameBuffer = new byte[0x40000];
    private readonly byte[] _vdp1ThreadRam = new byte[0x80000];
    private readonly byte[] _vdp1ThreadBackFrameBuffer = new byte[0x40000];
    private readonly object _vdp1ThreadLock = new();
    private readonly Thread? _vdp1Thread;
    private AutoResetEvent? _vdp1WakeEvent;
    private SaturnVdp1Registers _vdp1ThreadRegisters;
    private SaturnVdp1? _vdp1ThreadChip;
    private ExceptionDispatchInfo? _vdp1ThreadFailure;
    private bool _vdp1ThreadEnabled;
    private bool _vdp1ThreadNeedsDraw;
    private bool _vdp1ThreadDrawFinished = true;
    private bool _vdp1ThreadStopping;
    private readonly object _layerThreadLock = new();
    private readonly Thread?[] _layerThreads = new Thread?[6];
    private readonly AutoResetEvent?[] _layerWakeEvents = new AutoResetEvent?[6];
    private readonly bool[] _layerDrawFinished = new bool[6];
    private readonly SaturnVdp2Registers[] _layerLines = new SaturnVdp2Registers[270];
    private readonly byte[] _layerRam = new byte[0x80000];
    private readonly byte[] _layerColorRam = new byte[0x1000];
    private readonly SaturnVdp2CellScrollData[] _layerCellData = new SaturnVdp2CellScrollData[270];
    private SaturnVdp2Registers _layerRegisters;
    private ushort _layerVdp1Tvmr;
    private int _layerVdp1Width;
    private int _layerVdp1Height;
    private int _layerVdp1PixelSize;
    private bool _layerVdp1DisplayEnabled;
    private ExceptionDispatchInfo? _layerThreadFailure;
    private int _layerColorMode, _layerDisplayToggle, _numLayerThreads;
    private bool _layerOddFrame, _layerThreadsStopping;
    private readonly byte[] _spriteWindowMask = new byte[704 * 512];
    private int _width;
    private int _height;
    private int _interlace;
    private int _pixelSize;
    private int _currentPixel;
    private int _currentPixelIsVisible;
    private int _characterWidth;
    private int _characterHeight;
    private ColorParameters _leftColumnColor;
    private ushort _gouraudA;
    private ushort _gouraudB;
    private ushort _gouraudC;
    private ushort _gouraudD;
    private readonly int[] _xLeft = new int[1000];
    private readonly int[] _yLeft = new int[1000];
    private readonly int[] _xRight = new int[1000];
    private readonly int[] _yRight = new int[1000];
    private readonly SaturnTitan _titan;
    private readonly uint[] _displayBuffer = new uint[704 * 512];
    private int _vdp2Width = 320;
    private int _vdp2Height = 224;
    private bool _vdp2Interlace;
    internal int Rbg0Width { get; private set; } = 320;
    internal int Rbg0Height { get; private set; }
    internal bool Vdp2HorizontalHighResolution { get; private set; }

    public SaturnVideoSoftware()
    {
        ReadVdp1FrameBuffer = ReadFrameBuffer;
        WriteVdp1FrameBuffer = WriteFrameBuffer;
        Array.Fill(_layerDrawFinished, true);

        bool initialized = false;
        try
        {
            _titan = new SaturnTitan();

            _vdp1WakeEvent = new AutoResetEvent(false);
            var vdp1Thread = new Thread(VidsoftVdp1Thread) { IsBackground = true };
            vdp1Thread.Start();
            _vdp1Thread = vdp1Thread;

            ReadOnlySpan<SaturnTitanLayer> layers =
            [
                SaturnTitanLayer.Rbg0,
                SaturnTitanLayer.Nbg0,
                SaturnTitanLayer.Nbg1,
                SaturnTitanLayer.Nbg2,
                SaturnTitanLayer.Nbg3,
                SaturnTitanLayer.Sprite
            ];
            foreach (SaturnTitanLayer layer in layers)
            {
                _layerWakeEvents[(int)layer] = new AutoResetEvent(false);
                var thread = new Thread(() => VidsoftLayerThread(layer)) { IsBackground = true };
                thread.Start();
                _layerThreads[(int)layer] = thread;
            }
            initialized = true;
        }
        finally
        {
            if (!initialized)
                Dispose();
        }
    }

    public void Dispose()
    {
        try
        {
            lock (_layerThreadLock)
            {
                _layerThreadsStopping = true;
                foreach (AutoResetEvent? wakeEvent in _layerWakeEvents)
                    wakeEvent?.Set();
            }
            lock (_vdp1ThreadLock)
            {
                _vdp1ThreadStopping = true;
                _vdp1WakeEvent?.Set();
            }
        }
        finally
        {
            try
            {
                foreach (Thread? thread in _layerThreads)
                    thread?.Join();
            }
            finally
            {
                try
                {
                    for (int index = 0; index < _layerWakeEvents.Length; index++)
                    {
                        _layerWakeEvents[index]?.Dispose();
                        _layerWakeEvents[index] = null;
                    }
                }
                finally
                {
                    try
                    {
                        _vdp1Thread?.Join();
                    }
                    finally
                    {
                        try
                        {
                            _vdp1WakeEvent?.Dispose();
                            _vdp1WakeEvent = null;
                        }
                        finally
                        {
                            _titan?.Dispose();
                        }
                    }
                }
            }
        }
    }

    public Func<int, uint, uint>? ReadVdp1FrameBuffer { get; }
    public Action<int, uint, uint>? WriteVdp1FrameBuffer { get; }
    public ReadOnlySpan<byte> FrontFrameBuffer => _frontFrameBuffer;
    public ReadOnlySpan<uint> DisplayBuffer => _displayBuffer.AsSpan(0, _vdp2Width * _vdp2Height);
    public int DisplayWidth => _vdp2Width;
    public int DisplayHeight => _vdp2Height;
    public int? DisplayFieldParity { get; private set; }

    internal static void GetInterlaceInfo(bool interlace, bool oddFrame, out int startLine, out int lineIncrement)
    {
        if (interlace)
        {
            startLine = oddFrame ? 1 : 0;
            lineIncrement = 2;
        }
        else
        {
            startLine = 0;
            lineIncrement = 1;
        }
    }

    private static uint ColSatToYab16(int alpha, uint color) => BitConverter.IsLittleEndian
        ? ((uint)alpha << 24) | ((color & 0x1F) << 3) | ((color & 0x3E0) << 6) | ((color & 0x7C00) << 9)
        : (uint)alpha | ((color & 0x7C00) << 1) | ((color & 0x3E0) << 14) | ((color & 0x1F) << 27);

    private static uint ColSatToYab32(int alpha, uint color) => BitConverter.IsLittleEndian
        ? ((uint)alpha << 24) | (color & 0xFFFFFF)
        : ((color & 0xFF) << 24) | ((color & 0xFF00) << 8) | ((color & 0xFF0000) >> 8) | (uint)alpha;

    private static uint OffsetChannel(uint color, int offset) => (uint)Math.Clamp((int)color + offset, 0, 255);

    private static uint AddColorOffset(uint color, int red, int green, int blue)
    {
        if (BitConverter.IsLittleEndian)
            return OffsetChannel(color & 0xFF, red) | (OffsetChannel((color >> 8) & 0xFF, green) << 8) |
                (OffsetChannel((color >> 16) & 0xFF, blue) << 16) | (color & 0xFF000000);

        return (color & 0xFF) | (OffsetChannel((color >> 8) & 0xFF, blue) << 8) |
            (OffsetChannel((color >> 16) & 0xFF, green) << 16) | (OffsetChannel(color >> 24, red) << 24);
    }

    private static int SignExtendColorOffset(ushort value) =>
        (value & 0x100) != 0 ? (value & 0xFF) | unchecked((int)0xFFFFFF00) : value & 0xFF;

    private readonly ref struct Vdp2DrawInput(
        SaturnVdp2Registers registers, ReadOnlySpan<SaturnVdp2Registers> lines, ReadOnlySpan<byte> ram,
        ReadOnlySpan<byte> colorRam, ReadOnlySpan<SaturnVdp2CellScrollData> cellData,
        int colorMode, int displayToggle, bool oddFrame)
    {
        internal readonly SaturnVdp2Registers Registers = registers;
        internal readonly ReadOnlySpan<SaturnVdp2Registers> Lines = lines;
        internal readonly ReadOnlySpan<byte> Ram = ram;
        internal readonly ReadOnlySpan<byte> ColorRam = colorRam;
        internal readonly ReadOnlySpan<SaturnVdp2CellScrollData> CellData = cellData;
        internal readonly int ColorMode = colorMode;
        internal readonly int DisplayToggle = displayToggle;
        internal readonly bool OddFrame = oddFrame;
    }

    private static uint ColorRamGetColor(SaturnVdp2 vdp2, uint address) =>
        ColorRamGetColor(vdp2.ColorMode, vdp2.ColorRam, address);

    private static uint ColorRamGetColor(int colorMode, ReadOnlySpan<byte> colorRam, uint address)
    {
        switch (colorMode)
        {
            case 0:
            {
                address <<= 1;
                uint value = T2ReadWord(colorRam, address & 0xFFF);
                return ((value & 0x1F) << 3) | ((value & 0x03E0) << 6) | ((value & 0x7C00) << 9) | ((value & 0x8000) << 16);
            }
            case 1:
            {
                address <<= 1;
                uint value = T2ReadWord(colorRam, address & 0xFFF);
                return ((value & 0x1F) << 3) | ((value & 0x03E0) << 6) | ((value & 0x7C00) << 9) | ((value & 0x8000) << 16);
            }
            case 2:
                return T2ReadLong(colorRam, (address << 2) & 0xFFF);
            default:
                return 0;
        }
    }

    private static void Vdp2PatternAddr(ref Vdp2Draw info, in SaturnVdp2Registers registers, ReadOnlySpan<byte> ram)
    {
        switch (info.PatternDataSize)
        {
            case 1:
            {
                uint pattern = T1ReadWord(ram, info.Address & 0x7FFFF);
                info.Address += 2;
                DecodeVdp2PatternWord(ref info, pattern);
                break;
            }
            case 2:
            {
                uint address = info.Address & 0x7FFFF;
                uint first = T1ReadWord(ram, address);
                uint second = T1ReadWord(ram, address + 2);
                info.Address += 4;
                info.CharacterAddress = second & 0x7FFF;
                info.FlipFunction = (int)((first & 0xC000) >> 14);
                switch (info.ColorNumber)
                {
                    case 0: info.PaletteAddress = (first & 0x7F) << 4; break;
                    default: info.PaletteAddress = (first & 0x70) << 4; break;
                }
                info.SpecialFunction = (int)((first & 0x2000) >> 13);
                info.SpecialColorFunction = (int)((first & 0x1000) >> 12);
                break;
            }
        }

        FinishVdp2PatternAddress(ref info, registers);
    }

    private static void DecodeVdp2PatternWord(ref Vdp2Draw info, uint pattern)
    {
        uint supplement = info.SupplementData;
        info.SpecialFunction = (int)((supplement >> 9) & 1);
        info.SpecialColorFunction = (int)((supplement >> 8) & 1);
        switch (info.ColorNumber)
        {
            case 0: info.PaletteAddress = ((pattern & 0xF000) >> 8) | ((supplement & 0xE0) << 3); break;
            default: info.PaletteAddress = (pattern & 0x7000) >> 4; break;
        }

        switch (info.AuxiliaryMode)
        {
            case 0:
                info.FlipFunction = (int)((pattern & 0xC00) >> 10);
                switch (info.PatternWidth)
                {
                    case 1: info.CharacterAddress = (pattern & 0x3FF) | ((supplement & 0x1F) << 10); break;
                    case 2: info.CharacterAddress = ((pattern & 0x3FF) << 2) | (supplement & 3) | ((supplement & 0x1C) << 10); break;
                }
                break;
            case 1:
                info.FlipFunction = 0;
                switch (info.PatternWidth)
                {
                    case 1: info.CharacterAddress = (pattern & 0xFFF) | ((supplement & 0x1C) << 10); break;
                    case 2: info.CharacterAddress = ((pattern & 0xFFF) << 2) | (supplement & 3) | ((supplement & 0x10) << 10); break;
                }
                break;
        }
    }

    private static void FinishVdp2PatternAddress(ref Vdp2Draw info, in SaturnVdp2Registers registers)
    {
        if ((registers.VRSIZE & 0x8000) == 0)
            info.CharacterAddress &= 0x3FFF;
        info.CharacterAddress *= 0x20;
        if (info.SpecialPriorityMode == 1)
            info.Priority = (info.Priority & 0xE) | (info.SpecialFunction & 1);
    }

    private static uint DoNothing(in Vdp2Draw info, uint pixel) => pixel;

    private static uint DoColorOffset(in Vdp2Draw info, uint pixel) =>
        AddColorOffset(pixel, info.ColorOffsetRed, info.ColorOffsetGreen, info.ColorOffsetBlue);

    private static void ReadVdp2ColorOffset(in SaturnVdp2Registers registers, ref Vdp2Draw info, int mask)
    {
        if ((registers.CLOFEN & mask) != 0)
        {
            if ((registers.CLOFSL & mask) != 0)
            {
                info.ColorOffsetRed = SignExtendColorOffset(registers.COBR);
                info.ColorOffsetGreen = SignExtendColorOffset(registers.COBG);
                info.ColorOffsetBlue = SignExtendColorOffset(registers.COBB);
            }
            else
            {
                info.ColorOffsetRed = SignExtendColorOffset(registers.COAR);
                info.ColorOffsetGreen = SignExtendColorOffset(registers.COAG);
                info.ColorOffsetBlue = SignExtendColorOffset(registers.COAB);
            }
            info.PostPixelFetchCalc = &DoColorOffset;
        }
        else
            info.PostPixelFetchCalc = &DoNothing;
    }

    private static bool Vdp2FetchPixel(in Vdp2Draw info, int x, int y, ref uint color, ref uint dot,
        ReadOnlySpan<byte> ram, int characterAddress, int paletteAddress, int colorMode, ReadOnlySpan<byte> colorRam)
    {
        switch (info.ColorNumber)
        {
            case 0:
                dot = T1ReadByte(ram, (uint)((characterAddress + ((y * info.CellWidth) + x) / 2) & 0x7FFFF));
                if ((x & 1) == 0)
                    dot >>= 4;
                if ((dot & 0xF) == 0 && info.TransparencyEnabled)
                    return false;
                color = ColorRamGetColor(colorMode, colorRam, (uint)info.ColorOffset + ((uint)paletteAddress | (dot & 0xF)));
                return true;
            case 1:
                dot = T1ReadByte(ram, (uint)((characterAddress + (y * info.CellWidth) + x) & 0x7FFFF));
                if ((dot & 0xFF) == 0 && info.TransparencyEnabled)
                    return false;
                color = ColorRamGetColor(colorMode, colorRam, (uint)info.ColorOffset + ((uint)paletteAddress | (dot & 0xFF)));
                return true;
            case 2:
                dot = T1ReadWord(ram, (uint)((characterAddress + ((y * info.CellWidth) + x) * 2) & 0x7FFFF));
                if (dot == 0 && info.TransparencyEnabled)
                    return false;
                color = ColorRamGetColor(colorMode, colorRam, (uint)info.ColorOffset + dot);
                return true;
            case 3:
                dot = T1ReadWord(ram, (uint)((characterAddress + ((y * info.CellWidth) + x) * 2) & 0x7FFFF));
                if ((dot & 0x8000) == 0 && info.TransparencyEnabled)
                    return false;
                color = ColSatToYab16(0, dot);
                return true;
            case 4:
                dot = T1ReadLong(ram, (uint)((characterAddress + ((y * info.CellWidth) + x) * 4) & 0x7FFFF));
                if ((dot & 0x80000000) == 0 && info.TransparencyEnabled)
                    return false;
                color = ColSatToYab32(0, dot);
                return true;
            default:
                return false;
        }
    }

    private int TestWindow(int control, int enableMask, int insideMask, in Clipping clip, int x, int y)
    {
        if ((control & enableMask) != 0)
        {
            if ((control & insideMask) != 0)
            {
                if (x < clip.XStart || x > clip.XEnd || y < clip.YStart || y > clip.YEnd)
                    return 0;
            }
            else
            {
                if (x >= clip.XStart && x <= clip.XEnd && y >= clip.YStart && y <= clip.YEnd)
                    return 0;
                if (clip.YEnd > _vdp2Height && x >= clip.XStart && x <= clip.XEnd)
                    return 0;
            }
            return 1;
        }
        return 3;
    }

    private int TestSpriteWindow(int control, int x, int y)
    {
        int address = y * _vdp2Width + x;
        if (address >= 704 * 512)
            return 0;

        int mask = _spriteWindowMask[address];
        if ((control & 0x20) != 0)
        {
            if ((control & 0x10) != 0)
            {
                if (mask == 0)
                    return 0;
            }
            else if (mask != 0)
                return 0;

            return 1;
        }
        return 3;
    }

    private static int WindowLogic(int control, int window0, int window1)
    {
        if ((control & 0x80) == 0x80)
            return window0 != 0 || window1 != 0 ? 1 : 0;
        return window0 != 0 && window1 != 0 ? 1 : 0;
    }

    private int TestBothWindow(int control, ReadOnlySpan<Clipping> clips, int x, int y)
    {
        int window0 = TestWindow(control, 0x2, 0x1, clips[0], x, y);
        int window1 = TestWindow(control, 0x8, 0x4, clips[1], x, y);
        int sprite = TestSpriteWindow(control, x, y);
        return CombineWindowResults(control, window0, window1, sprite);
    }

    private static int CombineWindowResults(int control, int window0, int window1, int sprite)
    {
        if ((control & 0x2A) == 0)
            return (control & 0x80) == 0x80 ? 0 : 1;
        if ((window1 & 2) != 0 && (sprite & 2) != 0)
            return window0 & 1;
        if ((window0 & 2) != 0 && (sprite & 2) != 0)
            return window1 & 1;
        if ((sprite & 2) != 0)
            return WindowLogic(control, window0, window1);
        if ((window1 & 2) != 0 && (window0 & 2) != 0)
            return sprite & 1;
        if ((control & 0x2A) == 0x22)
            return WindowLogic(control, window0, sprite);
        if ((control & 0x2A) == 0x28)
            return WindowLogic(control, window1, sprite);
        if ((control & 0x2A) == 0x2A)
        {
            if ((control & 0x80) == 0x80)
                return window0 != 0 || window1 != 0 || sprite != 0 ? 1 : 0;
            return window0 != 0 && window1 != 0 && sprite != 0 ? 1 : 0;
        }
        return 1;
    }

    // Mednafen WinPieces partitions a line at the window transitions.
    private static int PrepareWindowPieces(int width, int visibility, ReadOnlySpan<Clipping> visibleClips,
        int color, ReadOnlySpan<Clipping> colorClips, Span<int> ends)
    {
        int count = 0;
        AddWindowEnds(width, visibility, visibleClips, ends, ref count);
        AddWindowEnds(width, color, colorClips, ends, ref count);
        ends[count++] = width;
        ends[..count].Sort();
        return count;
    }

    private static void AddWindowEnds(int width, int control, ReadOnlySpan<Clipping> clips, Span<int> ends, ref int count)
    {
        for (int index = 0; index < 2; index++)
        {
            if ((control & (2 << (index * 2))) == 0)
                continue;
            ends[count++] = Math.Clamp(clips[index].XStart, 0, width);
            ends[count++] = Math.Clamp(clips[index].XEnd + 1, 0, width);
        }
    }

    private int GetWindowPieceDecision(int control, ReadOnlySpan<Clipping> clips, int x, int y)
    {
        int window0 = TestWindow(control, 0x2, 0x1, clips[0], x, y);
        int window1 = TestWindow(control, 0x8, 0x4, clips[1], x, y);
        if ((control & 0x20) == 0)
            return CombineWindowResults(control, window0, window1, 3) * 3;
        int spriteOff = CombineWindowResults(control, window0, window1, 0);
        int spriteOn = CombineWindowResults(control, window0, window1, 1);
        return spriteOff | (spriteOn << 1);
    }

    private bool WindowPieceAllows(int decision, int control, int x, int y) =>
        decision == 3 || (decision != 0 && ((decision >> TestSpriteWindow(control, x, y)) & 1) != 0);

    [System.Runtime.CompilerServices.InlineArray(16)]
    internal struct PlaneAddressTable
    {
        private uint _address;
    }

    internal struct ScreenInfo
    {
        internal int PagePixelWidth, PagePixelWidthBits, PagePixelWidthMask;
        internal int PlanePixelWidth, PlanePixelWidthBits, PlanePixelWidthMask;
        internal int PlanePixelHeight, PlanePixelHeightBits, PlanePixelHeightMask;
        internal int ScreenWidth, ScreenHeight;
        internal int OldCellX, OldCellY, OldCellCheck;
        internal int XMask, YMask;
        internal PlaneAddressTable PlaneTable;
    }

    private static void GeneratePlaneAddrTable(ref Vdp2Draw info, ref PlaneAddressTable table,
        delegate*<ref Vdp2Draw, int, in SaturnVdp2Registers, void> planeAddress, in SaturnVdp2Registers registers)
    {
        for (int plane = 0; plane < info.MapWidth * info.MapWidth; plane++)
        {
            planeAddress(ref info, plane, in registers);
            table[plane] = info.Address;
        }
    }

    private static void Vdp2MapCalcXY(ref Vdp2Draw info, ref int x, ref int y, ref ScreenInfo screen,
        in SaturnVdp2Registers registers, ReadOnlySpan<byte> ram, bool badCycle)
    {
        int pageSizeBits = info.PageWidthBits * 2;
        int cellBits = 2 + info.PatternWidth;
        int cellCheck = ((y >> cellBits) << 16) | (x >> cellBits);
        if (cellCheck != screen.OldCellCheck)
        {
            screen.OldCellX = x >> cellBits;
            screen.OldCellY = y >> cellBits;
            screen.OldCellCheck = (screen.OldCellY << 16) | screen.OldCellX;

            int plane = ((y >> screen.PlanePixelHeightBits) * info.MapWidth) + (x >> screen.PlanePixelWidthBits);
            x &= screen.PlanePixelWidthMask;
            y &= screen.PlanePixelHeightMask;

            info.Address = screen.PlaneTable[plane];
            info.Address += (uint)(((((y >> screen.PagePixelWidthBits) << pageSizeBits) << info.PlaneWidthBits)
                + ((x >> screen.PagePixelWidthBits) << pageSizeBits)
                + (((y & screen.PagePixelWidthMask) >> cellBits) << info.PageWidthBits)
                + ((x & screen.PagePixelWidthMask) >> cellBits)) << (info.PatternDataSizeBits + 1));

            Vdp2PatternAddr(ref info, registers, ram);
            info.PreviousPattern = info.CurrentPattern;
            info.CurrentPattern.PaletteAddress = info.PaletteAddress;
            info.CurrentPattern.CharacterAddress = info.CharacterAddress;
            info.CurrentPattern.FlipFunction = info.FlipFunction;
        }

        int flipFunction = badCycle ? info.PreviousPattern.FlipFunction : info.FlipFunction;
        FlipVdp2Cell(info.PatternWidth, flipFunction, ref x, ref y);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private static void FlipVdp2Cell(int patternWidth, int flipFunction, ref int x, ref int y)
    {
        if (patternWidth == 1)
        {
            x &= 8 - 1;
            y &= 8 - 1;
            switch (flipFunction & 3)
            {
                case 0: break;
                case 1: x = 8 - 1 - x; break;
                case 2: y = 8 - 1 - y; break;
                case 3:
                    x = 8 - 1 - x;
                    y = 8 - 1 - y;
                    break;
            }
        }
        else
        {
            if (flipFunction != 0)
            {
                y &= 16 - 1;
                if ((flipFunction & 2) != 0)
                {
                    if ((y & 8) == 0)
                        y = 8 - 1 - y + 16;
                    else
                        y = 16 - 1 - y;
                }
                else if ((y & 8) != 0)
                    y += 8;

                if ((flipFunction & 1) != 0)
                {
                    if ((x & 8) == 0)
                        y += 8;
                    x &= 8 - 1;
                    x = 8 - 1 - x;
                }
                else if ((x & 8) != 0)
                {
                    y += 8;
                    x &= 8 - 1;
                }
                else
                    x &= 8 - 1;
            }
            else
            {
                y &= 16 - 1;
                if ((y & 8) != 0)
                    y += 8;
                if ((x & 8) != 0)
                    y += 8;
                x &= 8 - 1;
            }
        }
    }

    // Mednafen vdp2_render.cpp, TileFetch::Fetch uses the one-word OVPNR pattern
    // when a rotated tile lies outside the selected map and PlaneOver == 1.
    private static bool TryMapOverPattern(ref Vdp2Draw info, ref int x, ref int y,
        ref ScreenInfo screen, in SaturnVdp2Registers registers, ushort? overPattern)
    {
        if (overPattern is not ushort pattern || ((x & ~screen.XMask) | (y & ~screen.YMask)) == 0)
            return false;

        DecodeVdp2PatternWord(ref info, pattern);
        FinishVdp2PatternAddress(ref info, registers);
        FlipVdp2Cell(info.PatternWidth, info.FlipFunction, ref x, ref y);
        screen.OldCellCheck = -1;
        return true;
    }

    // Mednafen vdp2_render.cpp, TileFetch::Setup and TileFetch::Fetch.
    private static bool IsOutsideTransparentRotationMap(int screenOver, int x, int y, in ScreenInfo screen)
    {
        int xMask = screenOver == 3 ? 511 : screen.XMask;
        int yMask = screenOver == 3 ? 511 : screen.YMask;
        return ((x & ~xMask) | (y & ~yMask)) != 0;
    }

    private static void SetupScreenVars(ref Vdp2Draw info, ref ScreenInfo screen, delegate*<ref Vdp2Draw, int, in SaturnVdp2Registers, void> planeAddress,
        in SaturnVdp2Registers registers)
    {
        if (!info.IsBitmap)
        {
            screen.PagePixelWidth = 64 * 8;
            screen.PagePixelWidthBits = 9;
            screen.PagePixelWidthMask = 511;

            screen.PlanePixelWidth = info.PlaneWidth * screen.PagePixelWidth;
            screen.PlanePixelWidthBits = 8 + info.PlaneWidth;
            screen.PlanePixelWidthMask = (1 << screen.PlanePixelWidthBits) - 1;
            screen.PlanePixelHeight = info.PlaneHeight * screen.PagePixelWidth;
            screen.PlanePixelHeightBits = 8 + info.PlaneHeight;
            screen.PlanePixelHeightMask = (1 << screen.PlanePixelHeightBits) - 1;
            screen.ScreenWidth = info.MapWidth * screen.PlanePixelWidth;
            screen.ScreenHeight = info.MapWidth * screen.PlanePixelHeight;
            screen.OldCellX = -1;
            screen.OldCellY = -1;
            screen.OldCellCheck = -1;
            screen.XMask = screen.ScreenWidth - 1;
            screen.YMask = screen.ScreenHeight - 1;
            GeneratePlaneAddrTable(ref info, ref screen.PlaneTable, planeAddress, registers);
        }
        else
        {
            screen.PagePixelWidth = 0;
            screen.PagePixelWidthBits = 0;
            screen.PagePixelWidthMask = 0;
            screen.PlanePixelWidth = 0;
            screen.PlanePixelWidthBits = 0;
            screen.PlanePixelWidthMask = 0;
            screen.PlanePixelHeight = 0;
            screen.PlanePixelHeightBits = 0;
            screen.PlanePixelHeightMask = 0;
            screen.ScreenWidth = 0;
            screen.ScreenHeight = 0;
            screen.OldCellX = 0;
            screen.OldCellY = 0;
            screen.OldCellCheck = 0;
            screen.XMask = info.CellWidth - 1;
            screen.YMask = info.CellHeight - 1;
        }
    }

    private static int GetAlpha(in Vdp2Draw info, uint color, uint dot)
    {
        if ((info.SpecialColorMode == 1 || info.SpecialColorMode == 2) && (info.SpecialColorFunction & 1) == 0)
            return 0x3F;
        else if (info.SpecialColorMode == 2)
        {
            if ((info.SpecialCode & (1 << (int)((dot & 0xF) >> 1))) == 0)
                return 0x3F;
        }
        else if (info.SpecialColorMode == 3 && (color & 0x80000000) == 0)
            return 0x3F;

        return info.Alpha;
    }

    private static bool PixelIsSpecialPriority(int specialCode, uint dot)
    {
        dot &= 0xF;

        if ((specialCode & 0x01) != 0 && (dot == 0 || dot == 1))
            return true;
        if ((specialCode & 0x02) != 0 && (dot == 2 || dot == 3))
            return true;
        if ((specialCode & 0x04) != 0 && (dot == 4 || dot == 5))
            return true;
        if ((specialCode & 0x08) != 0 && (dot == 6 || dot == 7))
            return true;
        if ((specialCode & 0x10) != 0 && (dot == 8 || dot == 9))
            return true;
        if ((specialCode & 0x20) != 0 && (dot == 0xA || dot == 0xB))
            return true;
        if ((specialCode & 0x40) != 0 && (dot == 0xC || dot == 0xD))
            return true;
        if ((specialCode & 0x80) != 0 && (dot == 0xE || dot == 0xF))
            return true;

        return false;
    }

    private readonly bool[] _badCycleSetting = new bool[6];
    private static readonly int[] MosaicTable = InitializeMosaicTable();

    private static int[] InitializeMosaicTable()
    {
        var table = new int[16 * 1024];
        for (int i = 0; i < 16; i++)
        {
            int size = i + 1;
            for (int j = 0; j < 1024; j++)
                table[i * 1024 + j] = j / size * size;
        }
        return table;
    }

    private void Vdp2DrawScroll(ref Vdp2Draw info, ReadOnlySpan<SaturnVdp2Registers> lines,
        in SaturnVdp2Registers registers, ReadOnlySpan<byte> ram, ReadOnlySpan<SaturnVdp2CellScrollData> cellData,
        in Vdp2DrawInput input, ushort? overPattern = null, int screenOver = 0)
    {
        Span<Clipping> clips = stackalloc Clipping[2];
        Span<Clipping> colorCalculationClips = stackalloc Clipping[2];
        Span<int> windowEnds = stackalloc int[9];
        Span<uint> lineScrollXTable = stackalloc uint[512];
        Span<uint> lineScrollYTable = stackalloc uint[512];
        Span<float> lineZoomTable = stackalloc float[512];
        lineScrollXTable.Clear();
        lineScrollYTable.Clear();
        lineZoomTable.Clear();
        ScreenInfo screen = default;
        bool badCycle = _badCycleSetting[(int)info.Layer];
        bool transparentOutside = screenOver >= 2;
        bool wrapCoordinates = !transparentOutside && overPattern is null;
        int outputY = 0;
        int verticalCellScrollCount = 0;

        SetupScreenVars(ref info, ref screen, info.PlaneAddr, registers);
        int scrollY = info.Y;

        clips.Clear();
        ReadWindowData(info.WindowControl, clips, registers);
        uint lineWindow0 = 0;
        uint lineWindow1 = 0;
        info.LineWindow = ReadLineWindowData(info.WindowControl, ref lineWindow0, ref lineWindow1, registers);
        uint lineWindowBase0 = lineWindow0;
        uint lineWindowBase1 = lineWindow1;
        int colorControl = registers.WCTLD >> 8;
        ReadWindowData(colorControl, colorCalculationClips, registers);
        ReadOnlySpan<int> mosaicX = MosaicTable.AsSpan((info.MosaicXMask - 1) * 1024, 1024);
        ReadOnlySpan<int> mosaicY = MosaicTable.AsSpan((info.MosaicYMask - 1) * 1024, 1024);
        GetInterlaceInfo(_vdp2Interlace, input.OddFrame, out int startLine, out int lineIncrement);

        if ((registers.SCRCTL & 1) != 0)
            verticalCellScrollCount++;
        if ((registers.SCRCTL & 0x100) != 0)
            verticalCellScrollCount++;
        for (int j = startLine; j < _vdp2Height; j++)
        {
            if (info.LineScroll != 0)
            {
                bool needIncrement = j != 0 && ((uint)(j + 1) % info.LineIncrement) == 0;
                if ((info.LineScroll & 1) != 0)
                {
                    lineScrollXTable[j] = (T1ReadLong(ram, info.LineScrollTable & 0x7FFFF) >> 16) & 0x7FF;
                    if (needIncrement)
                        info.LineScrollTable += 4;
                }
                if ((info.LineScroll & 2) != 0)
                {
                    lineScrollYTable[j] = unchecked((uint)((T1ReadWord(ram, info.LineScrollTable & 0x7FFFF) & 0x7FF) + scrollY));
                    if (needIncrement)
                        info.LineScrollTable += 4;
                }
                if ((info.LineScroll & 4) != 0)
                {
                    lineZoomTable[j] = (T1ReadLong(ram, info.LineScrollTable & 0x7FFFF) & 0x7FF00) / 65536.0f;
                    if (needIncrement)
                        info.LineScrollTable += 4;
                }
            }
        }

        for (int j = startLine; j < _vdp2Height; j += lineIncrement)
        {
            int y;
            int lineScrollX = 0;
            if (info.LineScroll != 0)
            {
                if ((info.LineScroll & 1) != 0)
                    lineScrollX = unchecked((int)lineScrollXTable[j]);
                if ((info.LineScroll & 2) != 0)
                {
                    info.Y = unchecked((int)lineScrollYTable[j]);
                    y = info.Y;
                }
                else
                    y = (int)(info.Y + info.CoordinateIncrementY * mosaicY[j]);

                if ((info.LineScroll & 4) != 0)
                    info.CoordinateIncrementX = lineZoomTable[j];
            }
            else
                y = (int)(info.Y + info.CoordinateIncrementY * mosaicY[j]);

            if (_vdp2Interlace)
            {
                lineWindow0 = lineWindowBase0 + (uint)(j * 4);
                lineWindow1 = lineWindowBase1 + (uint)(j * 4);
            }
            ReadLineWindowClip(info.LineWindow, clips, ref lineWindow0, ref lineWindow1, ram, registers);
            if (wrapCoordinates)
                y &= screen.YMask;

            if (info.VerticalScroll && !Vdp2HorizontalHighResolution)
            {
                uint scrollValue = 0;
                int line = _vdp2Interlace ? j / 2 : j;
                if (verticalCellScrollCount == 1)
                    scrollValue = cellData[line][0] >> 16;
                else
                {
                    if (info.Layer == SaturnTitanLayer.Nbg0)
                        scrollValue = cellData[line][0] >> 16;
                    else if (info.Layer == SaturnTitanLayer.Nbg1)
                        scrollValue = cellData[line][1] >> 16;
                }
                y = (y + (int)scrollValue) & 0x1FF;
            }

            int originalY = y;
            if (_vdp2Interlace)
                info.LoadLineParams(ref info, ref screen, j / 2, lines);
            else
                info.LoadLineParams(ref info, ref screen, j, lines);
            if (!info.Enabled)
                continue;

            int pieceCount = PrepareWindowPieces(_vdp2Width, info.WindowControl, clips,
                colorControl, colorCalculationClips, windowEnds);
            int i = 0;
            for (int piece = 0; piece < pieceCount; piece++)
            {
                int end = windowEnds[piece];
                if (i == end)
                    continue;

                int visibility = GetWindowPieceDecision(info.WindowControl, clips, i, j);
                if (visibility == 0)
                {
                    i = end;
                    continue;
                }

                int colorDecision = GetWindowPieceDecision(colorControl, colorCalculationClips, i, j);
                for (; i < end; i++)
                {
                    if (visibility != 3 && !WindowPieceAllows(visibility, info.WindowControl, i, j))
                        continue;

                    int x = (int)(info.X + mosaicX[i] * info.CoordinateIncrementX);
                    if (wrapCoordinates)
                        x &= screen.XMask;
                    if (lineScrollX != 0)
                    {
                        x += lineScrollX;
                        if (!transparentOutside)
                            x &= 0x3FF;
                    }
                    if (transparentOutside)
                    {
                        if (IsOutsideTransparentRotationMap(screenOver, x, originalY, screen))
                            continue;

                        x &= screen.XMask;
                        y = originalY & screen.YMask;
                    }
                    if (!info.IsBitmap)
                    {
                        if (!transparentOutside)
                            y = originalY;
                        if (overPattern is null || !TryMapOverPattern(ref info, ref x, ref y, ref screen, registers, overPattern))
                            Vdp2MapCalcXY(ref info, ref x, ref y, ref screen, registers, ram, badCycle);
                    }

                    int characterAddress = unchecked((int)(badCycle ? info.PreviousPattern.CharacterAddress : info.CharacterAddress));
                    int paletteAddress = unchecked((int)(badCycle ? info.PreviousPattern.PaletteAddress : info.PaletteAddress));
                    uint color = 0;
                    uint dot = 0;
                    if (!Vdp2FetchPixel(info, x, y, ref color, ref dot, ram, characterAddress, paletteAddress,
                        input.ColorMode, input.ColorRam))
                        continue;

                    int priority = info.Priority;
                    if (info.SpecialPriorityMode == 2)
                    {
                        priority = info.Priority & 0xE;
                        if ((info.SpecialFunction & 1) != 0 && PixelIsSpecialPriority(info.SpecialCode, dot))
                            priority |= 1;
                    }

                    int alpha;
                    if (colorDecision != 3 && !WindowPieceAllows(colorDecision, colorControl, i, j))
                        alpha = 0x3F;
                    else
                        alpha = GetAlpha(info, color, dot);

                    _titan.PutPixel(priority, i, outputY, info.PostPixelFetchCalc(in info, ColSatToYab32(alpha, color)),
                        info.LineScreen, info.Layer, info.ShadowEnabled, info.ShadowType);
                }
            }
            outputY++;
        }
    }

    private void Rbg0PutHiresPixel(in Vdp2Draw info, uint color, uint dot, int x, int y)
    {
        uint pixel = info.PostPixelFetchCalc(in info, ColSatToYab32(GetAlpha(info, color, dot), color));
        int position = x * 2;
        _titan.PutPixel(info.Priority, position, y, pixel, info.LineScreen, info.Layer, info.ShadowEnabled, info.ShadowType);
        _titan.PutPixel(info.Priority, position + 1, y, pixel, info.LineScreen, info.Layer, info.ShadowEnabled, info.ShadowType);
    }

    private void Rbg0PutPixel(in Vdp2Draw info, uint color, uint dot, int x, int y)
    {
        if (Vdp2HorizontalHighResolution)
            Rbg0PutHiresPixel(info, color, dot, x, y);
        else
            _titan.PutPixel(info.Priority, x, y, info.PostPixelFetchCalc(in info, ColSatToYab32(GetAlpha(info, color, dot), color)),
                info.LineScreen, info.Layer, info.ShadowEnabled, info.ShadowType);
    }

    private static bool CheckBanks(in SaturnVdp2Registers registers, int value)
    {
        if (((registers.RAMCTL >> 0) & 3) == value)
            return false;
        if (((registers.RAMCTL >> 2) & 3) == value)
            return false;
        if (((registers.RAMCTL >> 4) & 3) == value)
            return false;
        if (((registers.RAMCTL >> 6) & 3) == value)
            return false;
        return true;
    }

    private static bool Rbg0CheckRam(in SaturnVdp2Registers registers)
    {
        if (((registers.RAMCTL >> 8) & 3) == 3 && CheckBanks(registers, 1))
            return true;
        return false;
    }

    private void Vdp2DrawRotationFixed(ref Vdp2Draw info, Span<RotationParameters> parameters,
        ReadOnlySpan<SaturnVdp2Registers> lines, in SaturnVdp2Registers registers, ReadOnlySpan<byte> ram,
        ReadOnlySpan<SaturnVdp2CellScrollData> cellData, in Vdp2DrawInput input)
    {
        ScreenInfo screen = default;
        ref RotationParameters parameter = ref parameters[info.RotateNumber];
        Span<Clipping> clips = stackalloc Clipping[2];
        clips.Clear();
        ReadWindowData(info.WindowControl, clips, registers);
        uint lineWindow0 = 0;
        uint lineWindow1 = 0;
        info.LineWindow = ReadLineWindowData(info.WindowControl, ref lineWindow0, ref lineWindow1, registers);
        parameter = ReadRotationTableFixed(info.RotateNumber, registers, ram);
        ushort primaryOverPattern = info.RotateNumber == 0 ? registers.OVPNRA : registers.OVPNRB;
        ushort? overPattern = parameter.ScreenOver == 1 && !info.IsBitmap ? primaryOverPattern : null;

        if (parameter.CoefficientEnabled == 0)
        {
            if (IsScreenRotatedFixed(parameter))
            {
                info.X = ToUnsignedFixedInteger(MultiplyFixed(parameter.Kx, parameter.Xst - parameter.Px) + parameter.Px + parameter.Mx);
                info.Y = ToUnsignedFixedInteger(MultiplyFixed(parameter.Ky, parameter.Yst - parameter.Py) + parameter.Py + parameter.My);
                info.CoordinateIncrementX = FixedToFloat(parameter.Kx);
                info.CoordinateIncrementY = FixedToFloat(parameter.Ky);
            }
            else
            {
                GenerateRotatedVarFixed(parameter, out int xmul, out int ymul, out int c, out int f);
                CalculateRotationValuesFixed(ref parameter);
                SetupScreenVars(ref info, ref screen, info.PlaneAddr, registers);

                for (int j = 0; j < _vdp2Height; j++)
                {
                    info.LoadLineParams(ref info, ref screen, j, lines);
                    ReadLineWindowClip(info.LineWindow, clips, ref lineWindow0, ref lineWindow1, ram, registers);
                    for (int i = 0; i < Rbg0Width; i++)
                    {
                        if (TestBothWindow(info.WindowControl, clips, i, j) == 0)
                            continue;

                        int x = GenerateRotatedXPosFixed(parameter, i, xmul, ymul, c);
                        int y = GenerateRotatedYPosFixed(parameter, i, xmul, ymul, f);
                        if (parameter.ScreenOver >= 2 && IsOutsideTransparentRotationMap(parameter.ScreenOver, x, y, screen))
                            continue;

                        if (overPattern is null || !TryMapOverPattern(ref info, ref x, ref y, ref screen, registers, overPattern))
                        {
                            x &= screen.XMask;
                            y &= screen.YMask;
                            if (!info.IsBitmap)
                                Vdp2MapCalcXY(ref info, ref x, ref y, ref screen, registers, ram, false);
                        }

                        uint color = 0;
                        uint dot = 0;
                        if (!Vdp2FetchPixel(info, x, y, ref color, ref dot, ram,
                            (int)info.CharacterAddress, (int)info.PaletteAddress, input.ColorMode, input.ColorRam))
                            continue;

                        Rbg0PutPixel(info, color, dot, i, j);
                    }
                    xmul += parameter.DeltaXst;
                    ymul += parameter.DeltaYst;
                }
                return;
            }
        }
        else
        {
            int secondNumber = 1 - info.RotateNumber;
            ref RotationParameters second = ref parameters[secondNumber];
            ScreenInfo secondScreen = default;
            Span<Clipping> parameterWindow = stackalloc Clipping[2];
            bool useParameterWindow = false;
            bool hasSecond = false;
            int parameterLineWindow = 0;
            uint parameterWindow0 = 0;
            uint parameterWindow1 = 0;

            if ((registers.RPMD & 3) == 2)
                hasSecond = true;
            else if ((registers.RPMD & 3) == 3)
            {
                ReadWindowData(registers.WCTLD, parameterWindow, registers);
                parameterLineWindow = ReadLineWindowData(registers.WCTLD, ref parameterWindow0,
                    ref parameterWindow1, registers);
                useParameterWindow = true;
                hasSecond = true;
            }

            GenerateRotatedVarFixed(parameter, out int xmul, out int ymul, out int c, out int f);
            CalculateRotationValuesFixed(ref parameter);
            SetupScreenVars(ref info, ref screen,
                info.RotateNumber == 0 ? &Vdp2ParameterAPlaneAddr : &Vdp2ParameterBPlaneAddr, registers);
            uint coefficientX = 0;
            uint coefficientY = 0;
            uint coefficientFractionX = 0;
            uint coefficientFractionY = 0;

            int xmul2 = 0;
            int ymul2 = 0;
            int c2 = 0;
            int f2 = 0;
            uint secondCoefficientX = 0;
            uint secondCoefficientY = 0;
            uint secondCoefficientFractionX = 0;
            uint secondCoefficientFractionY = 0;
            Vdp2Draw secondInfo = info;

            if (hasSecond)
            {
                second = ReadRotationTableFixed(secondNumber, registers, ram);
                GenerateRotatedVarFixed(second, out xmul2, out ymul2, out c2, out f2);
                CalculateRotationValuesFixed(ref second);
                if (!secondInfo.IsBitmap)
                    ReadPlaneSize(ref secondInfo, registers.PLSZ >> (8 + 4 * secondNumber));
                SetupScreenVars(ref secondInfo, ref secondScreen,
                    secondNumber == 0 ? &Vdp2ParameterAPlaneAddr : &Vdp2ParameterBPlaneAddr, registers);
            }

            ushort secondaryOverPattern = secondNumber == 0 ? registers.OVPNRA : registers.OVPNRB;
            ushort? secondOverPattern = hasSecond && second.ScreenOver == 1 && !info.IsBitmap ? secondaryOverPattern : null;

            bool coefficientsInColorRam = (registers.RAMCTL & 0x8000) != 0;
            ReadOnlySpan<byte> coefficientRam = coefficientsInColorRam ? input.ColorRam[0x800..] : ram;
            if (!coefficientsInColorRam && Rbg0CheckRam(registers))
            {
                if (parameter.CoefficientEnabled != 0 && parameter.CoefficientMode == 0)
                    parameter.DeltaKAx = 0;
                if (hasSecond && second.CoefficientEnabled != 0 && second.CoefficientMode == 0)
                    second.DeltaKAx = 0;
            }

            uint lineAddress = 0;
            uint lineStep = 0;
            if (info.LineScreen != 0)
            {
                if (info.RotateNumber == 0 && (registers.KTCTL & 0x10) != 0)
                    info.LineScreen = 2;
                else if ((registers.KTCTL & 0x1000) != 0)
                    info.LineScreen = 3;
                secondInfo.LineScreen = info.LineScreen;

                if ((registers.VRSIZE & 0x8000) != 0)
                    lineAddress = (registers.LCTA.All & 0x7FFFF) << 1;
                else
                    lineAddress = (registers.LCTA.All & 0x3FFFF) << 1;

                lineStep = (registers.LCTA.High & 0x8000) != 0 ? 2u : 0u;
            }

            for (int j = 0; j < Rbg0Height; j++)
            {
                if (parameter.DeltaKAx == 0)
                {
                    uint index = coefficientY + (ushort)(coefficientFractionY >> FractionBits);
                    ReadCoefficientFixed(ref parameter, unchecked(parameter.CoefficientTableAddress +
                        index * (uint)parameter.CoefficientDataSize), coefficientRam, coefficientsInColorRam);
                }
                if (hasSecond && second.CoefficientEnabled != 0 && second.DeltaKAx == 0)
                {
                    uint index = secondCoefficientY + (ushort)(secondCoefficientFractionY >> FractionBits);
                    ReadCoefficientFixed(ref second, unchecked(second.CoefficientTableAddress +
                        index * (uint)second.CoefficientDataSize), coefficientRam, coefficientsInColorRam);
                }

                if (info.LineScreen > 1)
                {
                    uint lineColorAddress = ((uint)T1ReadWord(ram, lineAddress & 0x7FFFF) & 0x780) | (uint)parameter.LineScreen;
                    uint lineColor = ColorRamGetColor(input.ColorMode, input.ColorRam, lineColorAddress);
                    lineAddress += lineStep;
                    _titan.PutLineHLine(info.LineScreen, j, ColSatToYab32(0x3F, lineColor));
                }

                info.LoadLineParams(ref info, ref screen, j, lines);
                if (hasSecond && !info.IsBitmap)
                    secondInfo.LoadLineParams(ref secondInfo, ref secondScreen, j, lines);
                ReadLineWindowClip(info.LineWindow, clips, ref lineWindow0, ref lineWindow1, ram, registers);
                if (useParameterWindow)
                    ReadLineWindowClip(parameterLineWindow, parameterWindow, ref parameterWindow0,
                        ref parameterWindow1, ram, registers);

                for (int i = 0; i < Rbg0Width; i++)
                {
                    if (parameter.DeltaKAx != 0)
                    {
                        uint index = unchecked(coefficientY + coefficientX +
                            ((coefficientFractionX + coefficientFractionY) >> FractionBits));
                        ReadCoefficientFixed(ref parameter, unchecked(parameter.CoefficientTableAddress +
                            index * (uint)parameter.CoefficientDataSize), coefficientRam, coefficientsInColorRam);
                        coefficientX = unchecked(coefficientX + (uint)(parameter.DeltaKAx >> FractionBits));
                        coefficientFractionX += (uint)(parameter.DeltaKAx & 0xFFFF);
                    }
                    if (hasSecond && second.CoefficientEnabled != 0 && second.DeltaKAx != 0)
                    {
                        uint index = unchecked(secondCoefficientY + secondCoefficientX +
                            ((secondCoefficientFractionX + secondCoefficientFractionY) >> FractionBits));
                        ReadCoefficientFixed(ref second, unchecked(second.CoefficientTableAddress +
                            index * (uint)second.CoefficientDataSize), coefficientRam, coefficientsInColorRam);
                        secondCoefficientX = unchecked(secondCoefficientX + (uint)(second.DeltaKAx >> FractionBits));
                        secondCoefficientFractionX += (uint)(second.DeltaKAx & 0xFFFF);
                    }

                    if (TestBothWindow(info.WindowControl, clips, i, j) == 0)
                        continue;

                    int x;
                    int y;
                    bool useSecond = (!useParameterWindow && parameter.Msb != 0) ||
                        (useParameterWindow && TestBothWindow(registers.WCTLD, parameterWindow, i, j) == 0);
                    ref Vdp2Draw pixelInfo = ref (useSecond && !info.IsBitmap ? ref secondInfo : ref info);
                    if (useSecond)
                    {
                        if (!hasSecond || (second.CoefficientEnabled != 0 && second.Msb != 0))
                            continue;

                        x = GenerateRotatedXPosFixed(second, i, xmul2, ymul2, c2);
                        y = GenerateRotatedYPosFixed(second, i, xmul2, ymul2, f2);
                        switch (second.ScreenOver)
                        {
                            case 1:
                                if (TryMapOverPattern(ref pixelInfo, ref x, ref y, ref secondScreen, registers, secondOverPattern))
                                    goto FetchRotationPixel;
                                goto case 0;
                            case 0:
                                x &= secondScreen.XMask;
                                y &= secondScreen.YMask;
                                break;
                            case 2:
                            case 3:
                                if (IsOutsideTransparentRotationMap(second.ScreenOver, x, y, secondScreen))
                                    continue;
                                goto case 0;
                        }
                        if (!info.IsBitmap)
                            Vdp2MapCalcXY(ref pixelInfo, ref x, ref y, ref secondScreen, registers, ram, false);
                    }
                    else if (parameter.Msb != 0)
                        continue;
                    else
                    {
                        x = GenerateRotatedXPosFixed(parameter, i, xmul, ymul, c);
                        y = GenerateRotatedYPosFixed(parameter, i, xmul, ymul, f);
                        switch (parameter.ScreenOver)
                        {
                            case 1:
                                if (TryMapOverPattern(ref info, ref x, ref y, ref screen, registers, overPattern))
                                    goto FetchRotationPixel;
                                goto case 0;
                            case 0:
                                x &= screen.XMask;
                                y &= screen.YMask;
                                break;
                            case 2:
                            case 3:
                                if (IsOutsideTransparentRotationMap(parameter.ScreenOver, x, y, screen))
                                    continue;
                                goto case 0;
                        }
                        if (!info.IsBitmap)
                            Vdp2MapCalcXY(ref info, ref x, ref y, ref screen, registers, ram, false);
                    }

                FetchRotationPixel:
                    uint color = 0;
                    uint dot = 0;
                    if (!Vdp2FetchPixel(pixelInfo, x, y, ref color, ref dot, ram,
                        (int)pixelInfo.CharacterAddress, (int)pixelInfo.PaletteAddress, input.ColorMode, input.ColorRam))
                        continue;

                    Rbg0PutPixel(pixelInfo, color, dot, i, j);
                }

                xmul += parameter.DeltaXst;
                ymul += parameter.DeltaYst;
                coefficientX = 0;
                coefficientFractionX = 0;
                coefficientY = unchecked(coefficientY + (uint)(parameter.DeltaKAst >> FractionBits));
                coefficientFractionY += (uint)(parameter.DeltaKAst & 0xFFFF);

                if (hasSecond)
                {
                    xmul2 += second.DeltaXst;
                    ymul2 += second.DeltaYst;
                    if (second.CoefficientEnabled != 0)
                    {
                        secondCoefficientX = 0;
                        secondCoefficientFractionX = 0;
                        secondCoefficientY = unchecked(secondCoefficientY + (uint)(second.DeltaKAst >> FractionBits));
                        secondCoefficientFractionY += (uint)(second.DeltaKAst & 0xFFFF);
                    }
                }
            }
            return;
        }

        Vdp2DrawScroll(ref info, lines, registers, ram, cellData, input, overPattern, parameter.ScreenOver);
    }

    private void DrawBackScreen(SaturnVdp2 vdp2)
    {
        ref readonly SaturnVdp2Registers registers = ref vdp2.Registers;
        if ((registers.TVMD & 0x8000) == 0 && (registers.TVMD & 0x100) == 0)
        {
            for (int line = 0; line < _vdp2Height; line++)
                _titan.PutBackHLine(line, ColSatToYab32(0x3F, 0));
        }
        else
        {
            Vdp2Draw info = default;
            ReadVdp2ColorOffset(registers, ref info, 1 << 5);

            uint screenAddress;
            if ((registers.VRSIZE & 0x8000) != 0)
                screenAddress = (uint)(((registers.BKTAU & 7) << 16) | registers.BKTAL) * 2;
            else
                screenAddress = (uint)(((registers.BKTAU & 3) << 16) | registers.BKTAL) * 2;

            if ((registers.BKTAU & 0x8000) != 0)
            {
                for (int line = 0; line < _vdp2Height; line++)
                {
                    ushort dot = vdp2.RamReadWord(screenAddress);
                    screenAddress += 2;
                    _titan.PutBackHLine(line, info.PostPixelFetchCalc(in info, ColSatToYab16(0x3F, dot)));
                }
            }
            else
            {
                ushort dot = vdp2.RamReadWord(screenAddress);
                for (int line = 0; line < _vdp2Height; line++)
                    _titan.PutBackHLine(line, info.PostPixelFetchCalc(in info, ColSatToYab16(0x3F, dot)));
            }
        }
    }

    private void DrawLineScreen(SaturnVdp2 vdp2)
    {
        ref readonly SaturnVdp2Registers registers = ref vdp2.Registers;
        if (registers.LNCLEN == 0)
            return;

        uint screenAddress;
        if ((registers.VRSIZE & 0x8000) != 0)
            screenAddress = (registers.LCTA.All & 0x7FFFF) << 1;
        else
            screenAddress = (registers.LCTA.All & 0x3FFFF) << 1;
        int alpha = (registers.CCRLB & 0x1F) << 1;

        if ((registers.LCTA.High & 0x8000) != 0)
        {
            for (int line = 0; line < _vdp2Height; line++)
            {
                uint color = (uint)vdp2.RamReadWord(screenAddress) & 0x7FF;
                uint dot = ColorRamGetColor(vdp2, color);
                screenAddress += 2;
                _titan.PutLineHLine(1, line, ColSatToYab32(alpha, dot));
            }
        }
        else
        {
            uint color = (uint)vdp2.RamReadWord(screenAddress) & 0x7FF;
            uint dot = ColorRamGetColor(vdp2, color);
            for (int line = 0; line < _vdp2Height; line++)
                _titan.PutLineHLine(1, line, ColSatToYab32(alpha, dot));
        }
    }

    private static void LoadLineParamsNbg0(ref Vdp2Draw info, ref ScreenInfo screen,
        int line, ReadOnlySpan<SaturnVdp2Registers> lines)
    {
        ReadOnlySpan<SaturnVdp2Registers> restored = SaturnVdp2.RestoreRegisters(line, lines);
        if (restored.IsEmpty)
            return;
        ref readonly SaturnVdp2Registers registers = ref restored[0];
        ReadVdp2ColorOffset(registers, ref info, 0x1);
        info.SpecialPriorityMode = registers.SFPRMD & 3;
        info.Enabled = (registers.BGON & 1) != 0 || (registers.BGON & 0x20) != 0;
        GeneratePlaneAddrTable(ref info, ref screen.PlaneTable, info.PlaneAddr, registers);
    }

    private void Vdp2DrawNbg0(in Vdp2DrawInput input)
    {
        ref readonly SaturnVdp2Registers registers = ref input.Registers;
        Vdp2Draw info = default;
        info.Layer = SaturnTitanLayer.Nbg0;
        info.ShadowEnabled = (registers.SDCTL & 1) != 0;
        Span<RotationParameters> parameters = stackalloc RotationParameters[2];
        int enabled = 0;
        info.PlaneAddr = &Vdp2Nbg0PlaneAddr;

        if ((registers.BGON & 0x20) != 0)
        {
            enabled = registers.BGON & 0x20;
            parameters[1] = ReadRotationTableFixed(1, registers, input.Ram);
            if (info.IsBitmap = (registers.CHCTLA & 2) != 0)
            {
                ReadBitmapSize(ref info, registers.CHCTLA >> 2, 3);
                info.CharacterAddress = (uint)((registers.MPOFR & 0x70) * 0x2000);
                info.PaletteAddress = (uint)((registers.BMPNA & 7) << 8);
                info.FlipFunction = 0;
                info.SpecialFunction = 0;
                info.SpecialColorFunction = (registers.BMPNA & 0x10) >> 4;
            }
            else
            {
                info.MapWidth = 4;
                ReadPlaneSize(ref info, registers.PLSZ >> 12);
                ReadPatternData(ref info, registers.PNCN0, registers.CHCTLA & 1);
            }
            info.RotateNumber = 1;
            info.RotateMode = 0;
            info.PlaneAddr = &Vdp2ParameterBPlaneAddr;
        }
        else if ((registers.BGON & 1) != 0)
        {
            enabled = registers.BGON & 1;
            if (info.IsBitmap = (registers.CHCTLA & 2) != 0)
            {
                ReadBitmapSize(ref info, registers.CHCTLA >> 2, 3);
                info.X = registers.SCXIN0 & 0x7FF;
                info.Y = registers.SCYIN0 & 0x7FF;
                info.CharacterAddress = (uint)((registers.MPOFN & 7) * 0x20000);
                info.PaletteAddress = (uint)((registers.BMPNA & 7) << 8);
                info.FlipFunction = 0;
                info.SpecialFunction = 0;
                info.SpecialColorFunction = (registers.BMPNA & 0x10) >> 4;
            }
            else
            {
                info.MapWidth = 2;
                ReadPlaneSize(ref info, registers.PLSZ);
                info.X = registers.SCXIN0 & 0x7FF;
                info.Y = registers.SCYIN0 & 0x7FF;
                ReadPatternData(ref info, registers.PNCN0, registers.CHCTLA & 1);
            }
            info.CoordinateIncrementX = (registers.ZMXN0.All & 0x7FF00) / 65536.0f;
            info.CoordinateIncrementY = (registers.ZMYN0.All & 0x7FF00) / 65536.0f;
        }
        info.Enabled = enabled != 0;

        info.TransparencyEnabled = (registers.BGON & 0x100) == 0;
        info.SpecialPriorityMode = registers.SFPRMD & 3;
        info.ColorNumber = (registers.CHCTLA & 0x70) >> 4;

        if ((registers.CCCTL & 0x201) != 0)
            info.Alpha = ((~registers.CCRNA & 0x1F) << 1) + 1;
        else
            info.Alpha = 0x3F;
        if ((registers.CCCTL & 0x201) == 0x201)
            info.Alpha |= 0x80;
        else if ((registers.CCCTL & 0x101) == 0x101)
            info.Alpha |= 0x80;
        info.SpecialColorMode = registers.SFCCMD & 3;
        if ((registers.SFSEL & 1) != 0)
            info.SpecialCode = registers.SFCODE >> 8;
        else
            info.SpecialCode = registers.SFCODE & 0xFF;
        info.LineScreen = 0;
        if ((registers.LNCLEN & 1) != 0)
            info.LineScreen = 1;

        info.ColorOffset = (registers.CRAOFA & 7) << 8;
        ReadVdp2ColorOffset(registers, ref info, 0x1);
        info.Priority = registers.PRINA & 7;

        if ((enabled & input.DisplayToggle) == 0)
            return;

        ReadMosaicData(ref info, 0x1, registers);
        ReadLineScrollData(ref info, registers.SCRCTL & 0xFF, registers.LSTA0.All);
        if ((registers.SCRCTL & 1) != 0)
        {
            info.VerticalScroll = true;
            info.VerticalScrollTable = (registers.VCSTA.All & 0x7FFFE) << 1;
            if ((registers.SCRCTL & 0x100) != 0)
                info.VerticalScrollIncrement = 8;
            else
                info.VerticalScrollIncrement = 4;
        }
        else
            info.VerticalScroll = false;
        info.WindowControl = registers.WCTLA;

        info.LoadLineParams = &LoadLineParamsNbg0;
        if (enabled == 1)
            Vdp2DrawScroll(ref info, input.Lines, registers, input.Ram, input.CellData,
                input);
        else
            Vdp2DrawRotationFixed(ref info, parameters, input.Lines, registers, input.Ram, input.CellData,
                input);
    }

    private static void LoadLineParamsNbg1(ref Vdp2Draw info, ref ScreenInfo screen,
        int line, ReadOnlySpan<SaturnVdp2Registers> lines)
    {
        ReadOnlySpan<SaturnVdp2Registers> restored = SaturnVdp2.RestoreRegisters(line, lines);
        if (restored.IsEmpty)
            return;
        ref readonly SaturnVdp2Registers registers = ref restored[0];
        ReadVdp2ColorOffset(registers, ref info, 0x2);
        info.SpecialPriorityMode = (registers.SFPRMD >> 2) & 3;
        info.Enabled = (registers.BGON & 0x2) != 0;
        GeneratePlaneAddrTable(ref info, ref screen.PlaneTable, info.PlaneAddr, registers);
    }

    private void Vdp2DrawNbg1(in Vdp2DrawInput input)
    {
        ref readonly SaturnVdp2Registers registers = ref input.Registers;
        Vdp2Draw info = default;
        info.Layer = SaturnTitanLayer.Nbg1;
        info.ShadowEnabled = ((registers.SDCTL >> 1) & 1) != 0;

        info.Enabled = (registers.BGON & 0x2) != 0;
        info.TransparencyEnabled = (registers.BGON & 0x200) == 0;
        info.SpecialPriorityMode = (registers.SFPRMD >> 2) & 3;
        info.ColorNumber = (registers.CHCTLA & 0x3000) >> 12;

        if (info.IsBitmap = (registers.CHCTLA & 0x200) != 0)
        {
            ReadBitmapSize(ref info, registers.CHCTLA >> 10, 3);
            info.X = registers.SCXIN1 & 0x7FF;
            info.Y = registers.SCYIN1 & 0x7FF;
            info.CharacterAddress = (uint)(((registers.MPOFN & 0x70) >> 4) * 0x20000);
            info.PaletteAddress = (uint)(registers.BMPNA & 0x700);
            info.FlipFunction = 0;
            info.SpecialFunction = 0;
            info.SpecialColorFunction = (registers.BMPNA & 0x1000) >> 12;
        }
        else
        {
            info.MapWidth = 2;
            ReadPlaneSize(ref info, registers.PLSZ >> 2);
            info.X = registers.SCXIN1 & 0x7FF;
            info.Y = registers.SCYIN1 & 0x7FF;
            ReadPatternData(ref info, registers.PNCN1, registers.CHCTLA & 0x100);
        }

        if ((registers.CCCTL & 0x202) != 0)
            info.Alpha = ((~registers.CCRNA & 0x1F00) >> 7) + 1;
        else
            info.Alpha = 0x3F;
        if ((registers.CCCTL & 0x202) == 0x202)
            info.Alpha |= 0x80;
        else if ((registers.CCCTL & 0x102) == 0x102)
            info.Alpha |= 0x80;
        info.SpecialColorMode = (registers.SFCCMD >> 2) & 3;
        if ((registers.SFSEL & 0x2) != 0)
            info.SpecialCode = registers.SFCODE >> 8;
        else
            info.SpecialCode = registers.SFCODE & 0xFF;
        info.LineScreen = 0;
        if ((registers.LNCLEN & 0x2) != 0)
            info.LineScreen = 1;

        info.ColorOffset = (registers.CRAOFA & 0x70) << 4;
        ReadVdp2ColorOffset(registers, ref info, 0x2);
        info.CoordinateIncrementX = (registers.ZMXN1.All & 0x7FF00) / 65536.0f;
        info.CoordinateIncrementY = (registers.ZMYN1.All & 0x7FF00) / 65536.0f;
        info.Priority = (registers.PRINA >> 8) & 7;

        if (!info.Enabled || (input.DisplayToggle & 0x2) == 0 ||
            ((registers.BGON & 1) != 0 && ((registers.CHCTLA & 0x70) >> 4) == 4))
            return;

        ReadMosaicData(ref info, 0x2, registers);
        ReadLineScrollData(ref info, registers.SCRCTL >> 8, registers.LSTA1.All);
        if ((registers.SCRCTL & 0x100) != 0)
        {
            info.VerticalScroll = true;
            if ((registers.SCRCTL & 1) != 0)
            {
                info.VerticalScrollTable = 4 + ((registers.VCSTA.All & 0x7FFFE) << 1);
                info.VerticalScrollIncrement = 8;
            }
            else
            {
                info.VerticalScrollTable = (registers.VCSTA.All & 0x7FFFE) << 1;
                info.VerticalScrollIncrement = 4;
            }
        }
        else
            info.VerticalScroll = false;
        info.WindowControl = registers.WCTLA >> 8;

        info.PlaneAddr = &Vdp2Nbg1PlaneAddr;
        info.LoadLineParams = &LoadLineParamsNbg1;
        Vdp2DrawScroll(ref info, input.Lines, registers, input.Ram, input.CellData, input);
    }

    private static void LoadLineParamsNbg2(ref Vdp2Draw info, ref ScreenInfo screen,
        int line, ReadOnlySpan<SaturnVdp2Registers> lines)
    {
        ReadOnlySpan<SaturnVdp2Registers> restored = SaturnVdp2.RestoreRegisters(line, lines);
        if (restored.IsEmpty)
            return;
        ref readonly SaturnVdp2Registers registers = ref restored[0];
        ReadVdp2ColorOffset(registers, ref info, 0x4);
        info.SpecialPriorityMode = (registers.SFPRMD >> 4) & 3;
        info.Enabled = (registers.BGON & 0x4) != 0;
        GeneratePlaneAddrTable(ref info, ref screen.PlaneTable, info.PlaneAddr, registers);
    }

    private void Vdp2DrawNbg2(in Vdp2DrawInput input)
    {
        ref readonly SaturnVdp2Registers registers = ref input.Registers;
        Vdp2Draw info = default;
        info.Layer = SaturnTitanLayer.Nbg2;
        info.ShadowEnabled = ((registers.SDCTL >> 2) & 1) != 0;
        info.Enabled = (registers.BGON & 0x4) != 0;
        info.TransparencyEnabled = (registers.BGON & 0x400) == 0;
        info.SpecialPriorityMode = (registers.SFPRMD >> 4) & 3;
        info.ColorNumber = (registers.CHCTLB & 2) >> 1;
        info.MapWidth = 2;

        ReadPlaneSize(ref info, registers.PLSZ >> 4);
        info.X = registers.SCXN2 & 0x7FF;
        info.Y = registers.SCYN2 & 0x7FF;
        ReadPatternData(ref info, registers.PNCN2, registers.CHCTLB & 1);

        if ((registers.CCCTL & 0x204) != 0)
            info.Alpha = ((~registers.CCRNB & 0x1F) << 1) + 1;
        else
            info.Alpha = 0x3F;
        if ((registers.CCCTL & 0x204) == 0x204)
            info.Alpha |= 0x80;
        else if ((registers.CCCTL & 0x104) == 0x104)
            info.Alpha |= 0x80;
        info.SpecialColorMode = (registers.SFCCMD >> 4) & 3;
        if ((registers.SFSEL & 4) != 0)
            info.SpecialCode = registers.SFCODE >> 8;
        else
            info.SpecialCode = registers.SFCODE & 0xFF;
        info.LineScreen = 0;
        if ((registers.LNCLEN & 4) != 0)
            info.LineScreen = 1;

        info.ColorOffset = registers.CRAOFA & 0x700;
        ReadVdp2ColorOffset(registers, ref info, 0x4);
        info.CoordinateIncrementX = info.CoordinateIncrementY = 1;
        info.Priority = registers.PRINB & 7;

        if (!info.Enabled || (input.DisplayToggle & 4) == 0 ||
            ((registers.BGON & 1) != 0 && ((registers.CHCTLA & 0x70) >> 4) >= 2))
            return;

        ReadMosaicData(ref info, 0x4, registers);
        info.LineScroll = 0;
        info.VerticalScroll = false;
        info.WindowControl = registers.WCTLB;
        info.IsBitmap = false;

        info.PlaneAddr = &Vdp2Nbg2PlaneAddr;
        info.LoadLineParams = &LoadLineParamsNbg2;
        Vdp2DrawScroll(ref info, input.Lines, registers, input.Ram, input.CellData, input);
    }

    private static void LoadLineParamsNbg3(ref Vdp2Draw info, ref ScreenInfo screen,
        int line, ReadOnlySpan<SaturnVdp2Registers> lines)
    {
        ReadOnlySpan<SaturnVdp2Registers> restored = SaturnVdp2.RestoreRegisters(line, lines);
        if (restored.IsEmpty)
            return;
        ref readonly SaturnVdp2Registers registers = ref restored[0];
        ReadVdp2ColorOffset(registers, ref info, 0x8);
        info.SpecialPriorityMode = (registers.SFPRMD >> 6) & 3;
        info.Enabled = (registers.BGON & 0x8) != 0;
        GeneratePlaneAddrTable(ref info, ref screen.PlaneTable, info.PlaneAddr, registers);
    }

    private void Vdp2DrawNbg3(in Vdp2DrawInput input)
    {
        ref readonly SaturnVdp2Registers registers = ref input.Registers;
        Vdp2Draw info = default;
        info.Layer = SaturnTitanLayer.Nbg3;
        info.ShadowEnabled = ((registers.SDCTL >> 3) & 1) != 0;
        info.Enabled = (registers.BGON & 0x8) != 0;
        info.TransparencyEnabled = (registers.BGON & 0x800) == 0;
        info.SpecialPriorityMode = (registers.SFPRMD >> 6) & 3;
        info.ColorNumber = (registers.CHCTLB & 0x20) >> 5;
        info.MapWidth = 2;

        ReadPlaneSize(ref info, registers.PLSZ >> 6);
        info.X = registers.SCXN3 & 0x7FF;
        info.Y = registers.SCYN3 & 0x7FF;
        ReadPatternData(ref info, registers.PNCN3, registers.CHCTLB & 0x10);

        if ((registers.CCCTL & 0x208) != 0)
            info.Alpha = ((~registers.CCRNB & 0x1F00) >> 7) + 1;
        else
            info.Alpha = 0x3F;
        if ((registers.CCCTL & 0x208) == 0x208)
            info.Alpha |= 0x80;
        else if ((registers.CCCTL & 0x108) == 0x108)
            info.Alpha |= 0x80;
        info.SpecialColorMode = (registers.SFCCMD >> 6) & 3;
        if ((registers.SFSEL & 8) != 0)
            info.SpecialCode = registers.SFCODE >> 8;
        else
            info.SpecialCode = registers.SFCODE & 0xFF;
        info.LineScreen = 0;
        if ((registers.LNCLEN & 8) != 0)
            info.LineScreen = 1;

        info.ColorOffset = (registers.CRAOFA & 0x7000) >> 4;
        ReadVdp2ColorOffset(registers, ref info, 0x8);
        info.CoordinateIncrementX = info.CoordinateIncrementY = 1;
        info.Priority = (registers.PRINB >> 8) & 7;

        if (!info.Enabled || (input.DisplayToggle & 8) == 0 ||
            ((registers.BGON & 1) != 0 && ((registers.CHCTLA & 0x70) >> 4) == 4) ||
            ((registers.BGON & 2) != 0 && ((registers.CHCTLA & 0x3000) >> 12) >= 2))
            return;

        ReadMosaicData(ref info, 0x8, registers);
        info.LineScroll = 0;
        info.VerticalScroll = false;
        info.WindowControl = registers.WCTLB >> 8;
        info.IsBitmap = false;

        info.PlaneAddr = &Vdp2Nbg3PlaneAddr;
        info.LoadLineParams = &LoadLineParamsNbg3;
        Vdp2DrawScroll(ref info, input.Lines, registers, input.Ram, input.CellData, input);
    }

    private static void LoadLineParamsRbg0(ref Vdp2Draw info, ref ScreenInfo screen,
        int line, ReadOnlySpan<SaturnVdp2Registers> lines)
    {
        ReadOnlySpan<SaturnVdp2Registers> restored = SaturnVdp2.RestoreRegisters(line, lines);
        if (restored.IsEmpty)
            return;
        ref readonly SaturnVdp2Registers registers = ref restored[0];
        ReadVdp2ColorOffset(registers, ref info, 0x10);
        info.SpecialPriorityMode = (registers.SFPRMD >> 8) & 3;
    }

    private void Vdp2DrawRbg0(in Vdp2DrawInput input)
    {
        ref readonly SaturnVdp2Registers registers = ref input.Registers;
        Vdp2Draw info = default;
        Span<RotationParameters> parameters = stackalloc RotationParameters[2];
        info.Layer = SaturnTitanLayer.Rbg0;
        info.ShadowEnabled = ((registers.SDCTL >> 4) & 1) != 0;
        int enabled = registers.BGON & 0x10;
        info.Enabled = enabled != 0;
        info.Priority = registers.PRIR & 7;
        if ((enabled & input.DisplayToggle) == 0)
            return;
        info.TransparencyEnabled = (registers.BGON & 0x1000) == 0;
        info.SpecialPriorityMode = (registers.SFPRMD >> 8) & 3;
        info.ColorNumber = (registers.CHCTLB & 0x7000) >> 12;

        switch (registers.RPMD & 3)
        {
            case 0:
                info.RotateNumber = 0;
                info.RotateMode = 0;
                info.PlaneAddr = &Vdp2ParameterAPlaneAddr;
                break;
            case 1:
                info.RotateNumber = 1;
                info.RotateMode = 0;
                info.PlaneAddr = &Vdp2ParameterBPlaneAddr;
                break;
            case 2:
            case 3:
            default:
                info.RotateNumber = 0;
                info.RotateMode = 1 + (registers.RPMD & 1);
                info.PlaneAddr = &Vdp2ParameterAPlaneAddr;
                break;
        }
        parameters[info.RotateNumber] = ReadRotationTableFixed(info.RotateNumber, registers, input.Ram);

        if (info.IsBitmap = (registers.CHCTLB & 0x200) != 0)
        {
            ReadBitmapSize(ref info, registers.CHCTLB >> 10, 1);
            if (info.RotateNumber == 0)
                info.CharacterAddress = (uint)((registers.MPOFR & 7) * 0x20000);
            else
                info.CharacterAddress = (uint)((registers.MPOFR & 0x70) * 0x2000);

            info.PaletteAddress = (uint)((registers.BMPNB & 7) << 8);
            info.FlipFunction = 0;
            info.SpecialFunction = 0;
            info.SpecialColorFunction = (registers.BMPNB & 0x10) >> 4;
        }
        else
        {
            info.MapWidth = 4;
            if (info.RotateNumber == 0)
                ReadPlaneSize(ref info, registers.PLSZ >> 8);
            else
                ReadPlaneSize(ref info, registers.PLSZ >> 12);
            ReadPatternData(ref info, registers.PNCR, registers.CHCTLB & 0x100);
        }

        if ((registers.CCCTL & 0x210) != 0)
            info.Alpha = ((~registers.CCRR & 0x1F) << 1) + 1;
        else
            info.Alpha = 0x3F;
        if ((registers.CCCTL & 0x210) == 0x210)
            info.Alpha |= 0x80;
        else if ((registers.CCCTL & 0x110) == 0x110)
            info.Alpha |= 0x80;
        info.SpecialColorMode = (registers.SFCCMD >> 8) & 3;
        if ((registers.SFSEL & 0x10) != 0)
            info.SpecialCode = registers.SFCODE >> 8;
        else
            info.SpecialCode = registers.SFCODE & 0xFF;
        info.LineScreen = 0;
        if ((registers.LNCLEN & 0x10) != 0)
            info.LineScreen = 1;

        info.ColorOffset = (registers.CRAOFB & 7) << 8;
        ReadVdp2ColorOffset(registers, ref info, 0x10);
        info.CoordinateIncrementX = info.CoordinateIncrementY = 1;

        ReadMosaicData(ref info, 0x10, registers);
        info.LineScroll = 0;
        info.VerticalScroll = false;
        info.WindowControl = registers.WCTLC;

        info.LoadLineParams = &LoadLineParamsRbg0;
        Vdp2DrawRotationFixed(ref info, parameters, input.Lines, registers, input.Ram, input.CellData, input);
    }

    public void StartVdp2Draw(SaturnVdp2 vdp2)
    {
        SaturnTitanBlend blend = SaturnTitanBlend.Top;
        if ((vdp2.Registers.CCCTL & 0x100) != 0)
            blend = SaturnTitanBlend.Add;
        else if ((vdp2.Registers.CCCTL & 0x200) != 0)
            blend = SaturnTitanBlend.Bottom;
        _titan.SetBlendingMode(blend);
        DrawBackScreen(vdp2);
        DrawLineScreen(vdp2);
        ref readonly SaturnVdp2Registers registers = ref vdp2.Registers;
        _badCycleSetting[(int)SaturnTitanLayer.Nbg3] =
            registers.CYCA0L == 0x5566 && registers.CYCA0U == 0x47FF &&
            registers.CYCA1L == 0xFFFF && registers.CYCA1U == 0xFFFF &&
            registers.CYCB0L == 0x12FF && registers.CYCB0U == 0x03FF &&
            registers.CYCB1L == 0xFFFF && registers.CYCB1U == 0xFFFF;
    }

    public void SetVdp2Resolution(ushort tvMode)
    {
        switch (tvMode & 7)
        {
            case 0: Rbg0Width = _vdp2Width = 320; break;
            case 1: Rbg0Width = _vdp2Width = 352; break;
            case 2: _vdp2Width = 640; Rbg0Width = 320; break;
            case 3: _vdp2Width = 704; Rbg0Width = 352; break;
            case 4: Rbg0Width = _vdp2Width = 320; break;
            case 5: Rbg0Width = _vdp2Width = 352; break;
            case 6: _vdp2Width = 640; Rbg0Width = 320; break;
            case 7: _vdp2Width = 704; Rbg0Width = 352; break;
        }

        Vdp2HorizontalHighResolution = _vdp2Width == 704 || _vdp2Width == 640;

        switch ((tvMode >> 4) & 3)
        {
            case 0: Rbg0Height = _vdp2Height = 224; break;
            case 1: Rbg0Height = _vdp2Height = 240; break;
            case 2: Rbg0Height = _vdp2Height = 256; break;
            default: break;
        }

        switch ((tvMode >> 6) & 3)
        {
            case 3:
                _vdp2Height *= 2;
                _vdp2Interlace = true;
                break;
            case 2:
            case 0:
            default:
                _vdp2Interlace = false;
                break;
        }

        _titan.SetResolution(_vdp2Width, _vdp2Height);
    }

    public void DisplayVdp2Off()
    {
        _titan.Erase(_vdp2Interlace);
    }

    internal void PutExternalBackgroundPixel(int priority, int x, int y, uint pixel) =>
        _titan.PutPixel(priority, x, y, pixel, 0, SaturnTitanLayer.Nbg1, false, SaturnTitanShadow.None);

    public void SetNumLayerThreads(int count) => _numLayerThreads = count;

    public void SetNumPriorityThreads(int count) => _titan.SetNumPriorityThreads(count);

    private void VidsoftLayerThread(SaturnTitanLayer layer)
    {
        int index = (int)layer;
        AutoResetEvent wakeEvent = _layerWakeEvents[index]!;
        while (true)
        {
            wakeEvent.WaitOne();
            lock (_layerThreadLock)
            {
                if (_layerThreadsStopping)
                {
                    Volatile.Write(ref _layerDrawFinished[index], true);
                    Monitor.PulseAll(_layerThreadLock);
                    return;
                }
            }

            try
            {
                Vdp2DrawInput input = new(_layerRegisters, _layerLines, _layerRam, _layerColorRam,
                    _layerCellData, _layerColorMode, _layerDisplayToggle, _layerOddFrame);
                switch (layer)
                {
                    case SaturnTitanLayer.Nbg0: Vdp2DrawNbg0(input); break;
                    case SaturnTitanLayer.Nbg1: Vdp2DrawNbg1(input); break;
                    case SaturnTitanLayer.Nbg2: Vdp2DrawNbg2(input); break;
                    case SaturnTitanLayer.Nbg3: Vdp2DrawNbg3(input); break;
                    case SaturnTitanLayer.Rbg0: Vdp2DrawRbg0(input); break;
                    case SaturnTitanLayer.Sprite:
                        VidsoftDrawSprite(input, _layerVdp1Tvmr, _layerVdp1DisplayEnabled,
                            _layerVdp1Width, _layerVdp1Height, _layerVdp1PixelSize);
                        break;
                }
            }
            catch (Exception exception)
            {
                lock (_layerThreadLock)
                    _layerThreadFailure ??= ExceptionDispatchInfo.Capture(exception);
            }
            finally
            {
                lock (_layerThreadLock)
                {
                    Volatile.Write(ref _layerDrawFinished[index], true);
                    Monitor.PulseAll(_layerThreadLock);
                }
            }
        }
    }

    private void WakeLayerThread(SaturnTitanLayer layer)
    {
        lock (_layerThreadLock)
        {
            int index = (int)layer;
            Volatile.Write(ref _layerDrawFinished[index], false);
            _layerWakeEvents[index]!.Set();
        }
    }

    private void WaitForLayerThread(SaturnTitanLayer layer)
    {
        int index = (int)layer;
        if (!Volatile.Read(ref _layerDrawFinished[index]))
        {
            lock (_layerThreadLock)
            {
                while (!_layerDrawFinished[index])
                    Monitor.Wait(_layerThreadLock);
            }
        }
    }

    private delegate void DrawVdp2Layer(in Vdp2DrawInput input);

    private void VidsoftStartLayerThread(ReadOnlySpan<int> layerPriority, ReadOnlySpan<int> drawPriorityZero,
        ref int dispatched, SaturnTitanLayer layer, DrawVdp2Layer drawLayer, in Vdp2DrawInput input)
    {
        int index = (int)layer;
        if (layerPriority[index] > 0 || drawPriorityZero[index] != 0)
        {
            if (dispatched < _numLayerThreads)
            {
                WakeLayerThread(layer);
                dispatched++;
            }
            else
                drawLayer(input);
        }
    }

    private static bool IsSpriteWindowEnabled(ushort control) =>
        (control & (1 << 13)) != 0 || (control & (1 << 5)) != 0;

    private static bool CanUseSpriteThread(in SaturnVdp2Registers registers)
    {
        if ((registers.SPCTL & (1 << 4)) == 0)
            return true;
        if (IsSpriteWindowEnabled(registers.WCTLA) || IsSpriteWindowEnabled(registers.WCTLB) ||
            IsSpriteWindowEnabled(registers.WCTLC) || IsSpriteWindowEnabled(registers.WCTLD))
            return false;
        return true;
    }

    internal void DrawVdp2Screens(SaturnVdp2 vdp2, SaturnVdp1 vdp1)
    {
        Span<int> drawPriorityZero = stackalloc int[6];
        Span<int> layerPriority = stackalloc int[6];
        drawPriorityZero.Clear();
        layerPriority.Clear();
        int dispatched = 0;
        ref readonly SaturnVdp2Registers registers = ref vdp2.Registers;
        SetVdp2Resolution(registers.TVMD);
        layerPriority[(int)SaturnTitanLayer.Nbg0] = registers.PRINA & 7;
        layerPriority[(int)SaturnTitanLayer.Nbg1] = (registers.PRINA >> 8) & 7;
        layerPriority[(int)SaturnTitanLayer.Nbg2] = registers.PRINB & 7;
        layerPriority[(int)SaturnTitanLayer.Nbg3] = (registers.PRINB >> 8) & 7;
        layerPriority[(int)SaturnTitanLayer.Rbg0] = registers.PRIR & 7;
        _titan.Erase(_vdp2Interlace);

        if ((registers.SFPRMD & 0x3FF) != 0)
        {
            drawPriorityZero[(int)SaturnTitanLayer.Nbg0] = registers.SFPRMD & 3;
            drawPriorityZero[(int)SaturnTitanLayer.Nbg1] = (registers.SFPRMD >> 2) & 3;
            drawPriorityZero[(int)SaturnTitanLayer.Nbg2] = (registers.SFPRMD >> 4) & 3;
            drawPriorityZero[(int)SaturnTitanLayer.Nbg3] = (registers.SFPRMD >> 6) & 3;
            drawPriorityZero[(int)SaturnTitanLayer.Rbg0] = (registers.SFPRMD >> 8) & 3;
        }

        ushort vdp1Tvmr = vdp1.Registers.TVMR;
        bool vdp1DisplayEnabled = vdp1.DisplayEnabled;
        if (_numLayerThreads > 0)
        {
            vdp2.Lines.CopyTo(_layerLines);
            _layerRegisters = registers;
            vdp2.Ram.CopyTo(_layerRam);
            vdp2.ColorRam.CopyTo(_layerColorRam);
            vdp2.CellScrollData.CopyTo(_layerCellData);
            _layerColorMode = vdp2.ColorMode;
            _layerDisplayToggle = vdp2.DisplayToggle;
            _layerOddFrame = vdp2.OddFrame;
            _layerVdp1Tvmr = vdp1Tvmr;
            _layerVdp1DisplayEnabled = vdp1DisplayEnabled;
            _layerVdp1Width = _width;
            _layerVdp1Height = _height;
            _layerVdp1PixelSize = _pixelSize;
        }

        Vdp2DrawInput input = new(registers, vdp2.Lines, vdp2.Ram, vdp2.ColorRam,
            vdp2.CellScrollData, vdp2.ColorMode, vdp2.DisplayToggle, vdp2.OddFrame);
        if (CanUseSpriteThread(registers) && _numLayerThreads > 0)
        {
            WakeLayerThread(SaturnTitanLayer.Sprite);
            dispatched++;
        }
        else
            VidsoftDrawSprite(input, vdp1Tvmr, vdp1DisplayEnabled, _width, _height, _pixelSize);

        if (_numLayerThreads > 0)
        {
            VidsoftStartLayerThread(layerPriority, drawPriorityZero, ref dispatched, SaturnTitanLayer.Nbg0, Vdp2DrawNbg0, input);
            VidsoftStartLayerThread(layerPriority, drawPriorityZero, ref dispatched, SaturnTitanLayer.Rbg0, Vdp2DrawRbg0, input);
            VidsoftStartLayerThread(layerPriority, drawPriorityZero, ref dispatched, SaturnTitanLayer.Nbg1, Vdp2DrawNbg1, input);
            VidsoftStartLayerThread(layerPriority, drawPriorityZero, ref dispatched, SaturnTitanLayer.Nbg2, Vdp2DrawNbg2, input);
            VidsoftStartLayerThread(layerPriority, drawPriorityZero, ref dispatched, SaturnTitanLayer.Nbg3, Vdp2DrawNbg3, input);
        }
        else
        {
            Vdp2DrawNbg0(input);
            Vdp2DrawNbg1(input);
            Vdp2DrawNbg2(input);
            Vdp2DrawNbg3(input);
            Vdp2DrawRbg0(input);
        }
    }

    private static void LoadLineParamsSprite(ref Vdp2Draw info, int line, ReadOnlySpan<SaturnVdp2Registers> lines)
    {
        ReadOnlySpan<SaturnVdp2Registers> restored = SaturnVdp2.RestoreRegisters(line, lines);
        if (restored.IsEmpty)
            return;
        ReadVdp2ColorOffset(restored[0], ref info, 0x40);
    }

    private void VidsoftDrawSprite(in Vdp2DrawInput input, ushort vdp1Tvmr, bool vdp1DisplayEnabled,
        int framebufferWidth, int framebufferHeight, int framebufferPixelSize)
    {
        ref readonly SaturnVdp2Registers registers = ref input.Registers;
        bool rotated = (vdp1Tvmr & 2) != 0;
        bool colorMode = (registers.SPCTL & 0x20) != 0;
        bool spriteWindowEnabled = (registers.SPCTL & 0x10) != 0;
        Vdp2Draw info = default;
        int framebufferY = 0;
        if (spriteWindowEnabled)
            Array.Clear(_spriteWindowMask);

        if (vdp1DisplayEnabled && framebufferPixelSize != 0 && (registers.TVMD & 0x8000) != 0)
        {
            ReadOnlySpan<ushort> framebufferWords = MemoryMarshal.Cast<byte, ushort>((ReadOnlySpan<byte>)_frontFrameBuffer);
            int colorCondition = (registers.SPCTL >> 12) & 3;
            int colorThreshold = (registers.SPCTL >> 8) & 7;
            Span<int> priority = stackalloc int[8];
            Span<int> colorCalc = stackalloc int[8];
            priority[0] = registers.PRISA & 7;
            priority[1] = (registers.PRISA >> 8) & 7;
            priority[2] = registers.PRISB & 7;
            priority[3] = (registers.PRISB >> 8) & 7;
            priority[4] = registers.PRISC & 7;
            priority[5] = (registers.PRISC >> 8) & 7;
            priority[6] = registers.PRISD & 7;
            priority[7] = (registers.PRISD >> 8) & 7;
            colorCalc[0] = ((~registers.CCRSA & 0x1F) << 1) + 1;
            colorCalc[1] = ((~registers.CCRSA >> 7) & 0x3E) + 1;
            colorCalc[2] = ((~registers.CCRSB & 0x1F) << 1) + 1;
            colorCalc[3] = ((~registers.CCRSB >> 7) & 0x3E) + 1;
            colorCalc[4] = ((~registers.CCRSC & 0x1F) << 1) + 1;
            colorCalc[5] = ((~registers.CCRSC >> 7) & 0x3E) + 1;
            colorCalc[6] = ((~registers.CCRSD & 0x1F) << 1) + 1;
            colorCalc[7] = ((~registers.CCRSD >> 7) & 0x3E) + 1;

            uint spriteColorOffset = (uint)((registers.CRAOFB & 0x70) << 4);
            int spriteType = registers.SPCTL & 0xF;
            ReadVdp2ColorOffset(registers, ref info, 0x40);

            int windowControl = registers.WCTLC >> 8;
            Span<Clipping> clips = stackalloc Clipping[2];
            clips.Clear();
            ReadWindowData(windowControl, clips, registers);
            uint lineWindow0 = 0;
            uint lineWindow1 = 0;
            int lineWindow = ReadLineWindowData(windowControl, ref lineWindow0, ref lineWindow1, registers);
            Span<Clipping> colorClips = stackalloc Clipping[2];
            bool colorCalculationEnabled = (registers.CCCTL & 0x40) != 0;
            int colorControl = colorCalculationEnabled ? registers.WCTLD >> 8 : 0;
            ReadWindowData(colorControl, colorClips, registers);
            Span<int> windowEnds = stackalloc int[9];

            RotationParameters parameter = default;
            if (rotated)
                parameter = ReadRotationTableFixed(0, registers, input.Ram);

            info.Layer = SaturnTitanLayer.Sprite;
            info.LineScreen = (registers.LNCLEN >> 5) & 1;
            GetInterlaceInfo(_vdp2Interlace, input.OddFrame, out int startLine, out int lineIncrement);
            int outputY = 0;
            for (int j = startLine; j < _vdp2Height; j += lineIncrement)
            {
                float framebufferReadoutPosition = 0;
                ReadLineWindowClip(lineWindow, clips, ref lineWindow0, ref lineWindow1, input.Ram, registers);
                if (_vdp2Interlace)
                    LoadLineParamsSprite(ref info, j / 2, input.Lines);
                else
                    LoadLineParamsSprite(ref info, j, input.Lines);

                int y;
                if (_vdp2Interlace)
                    y = framebufferY++;
                else
                    y = j;

                // Use the same line pieces as Mednafen's ApplyWin for the sprite layer.
                int pieceCount = PrepareWindowPieces(_vdp2Width, windowControl, clips, colorControl, colorClips, windowEnds);
                int i = 0;
                for (int piece = 0; piece < pieceCount; piece++)
                {
                    int end = windowEnds[piece];
                    if (i == end)
                        continue;

                    int visibility = GetWindowPieceDecision(windowControl, clips, i, j);
                    if (!spriteWindowEnabled && visibility == 0)
                    {
                        i = end;
                        continue;
                    }
                    int colorDecision = colorCalculationEnabled
                        ? GetWindowPieceDecision(colorControl, colorClips, i, j)
                        : 0;
                    for (; i < end; i++)
                    {
                        info.ShadowType = SaturnTitanShadow.None;
                        if (!spriteWindowEnabled && visibility != 3 && !WindowPieceAllows(visibility, windowControl, i, j))
                            continue;

                        int x;
                        if (rotated)
                        {
                            x = ToUnsignedFixedInteger(parameter.Xst + i * parameter.DeltaX + j * parameter.DeltaXst) & (framebufferWidth - 1);
                            y = ToUnsignedFixedInteger(parameter.Yst + i * parameter.DeltaY + j * parameter.DeltaYst) & (framebufferHeight - 1);
                        }
                        else if (framebufferWidth == 1024 && Vdp2HorizontalHighResolution)
                        {
                            x = (int)framebufferReadoutPosition;
                            framebufferReadoutPosition += 1;
                        }
                        else if (framebufferWidth == 512 && Vdp2HorizontalHighResolution)
                        {
                            x = (int)framebufferReadoutPosition;
                            framebufferReadoutPosition += 0.5f;
                        }
                        else if (framebufferWidth == 1024 && !Vdp2HorizontalHighResolution)
                        {
                            x = (int)framebufferReadoutPosition;
                            framebufferReadoutPosition += 2;
                        }
                        else
                            x = i;

                        if (framebufferPixelSize == 2)
                        {
                            ushort pixel = framebufferWords[y * framebufferWidth + x];
                            if (pixel == 0)
                                continue;
                            else if ((pixel & 0x8000) != 0 && colorMode)
                            {
                                int alpha = 0x3F;
                                if (colorCalculationEnabled && WindowPieceAllows(colorDecision, colorControl, i, j))
                                {
                                    switch (colorCondition)
                                    {
                                        case 0:
                                            if (priority[0] <= colorThreshold)
                                            {
                                                alpha = colorCalc[0];
                                                if ((registers.CCCTL & 0x300) != 0) alpha |= 0x80;
                                            }
                                            break;
                                        case 1:
                                            if (priority[0] == colorThreshold)
                                            {
                                                alpha = colorCalc[0];
                                                if ((registers.CCCTL & 0x300) != 0) alpha |= 0x80;
                                            }
                                            break;
                                        case 2:
                                            if (priority[0] >= colorThreshold)
                                            {
                                                alpha = colorCalc[0];
                                                if ((registers.CCCTL & 0x300) != 0) alpha |= 0x80;
                                            }
                                            break;
                                        case 3:
                                            alpha = colorCalc[0];
                                            if ((registers.CCCTL & 0x300) != 0) alpha |= 0x80;
                                            break;
                                    }
                                }

                                if (pixel != 0x8000 || spriteType < 2 || (spriteType < 8 && !spriteWindowEnabled))
                                    _titan.PutPixel(priority[0], i, outputY, info.PostPixelFetchCalc(in info, ColSatToYab16(alpha, pixel)),
                                        info.LineScreen, info.Layer, info.ShadowEnabled, info.ShadowType);
                            }
                            else
                            {
                                int alpha = 0x3F;
                                SpritePixelInfo sprite = GetSpritePixelInfo(spriteType, ref pixel);
                                if (sprite.NormalShadow)
                                {
                                    info.ShadowType = SaturnTitanShadow.Normal;
                                    _titan.PutPixel(priority[sprite.Priority], i, outputY, ColSatToYab16(0x3F, 0),
                                        info.LineScreen, info.Layer, info.ShadowEnabled, info.ShadowType);
                                    continue;
                                }

                                uint dot = ColorRamGetColor(input.ColorMode, input.ColorRam, spriteColorOffset + pixel);
                                if (colorCalculationEnabled && WindowPieceAllows(colorDecision, colorControl, i, j))
                                {
                                    bool transparent = false;
                                    switch (colorCondition)
                                    {
                                        case 0: if (priority[sprite.Priority] <= colorThreshold) transparent = true; break;
                                        case 1: if (priority[sprite.Priority] == colorThreshold) transparent = true; break;
                                        case 2: if (priority[sprite.Priority] >= colorThreshold) transparent = true; break;
                                        case 3: if ((dot & 0x80000000) != 0) transparent = true; break;
                                    }
                                    if ((registers.CCCTL & 0x200) != 0)
                                    {
                                        alpha = colorCalc[sprite.ColorCalculation];
                                        if (transparent) alpha |= 0x80;
                                    }
                                    else if (transparent)
                                    {
                                        alpha = colorCalc[sprite.ColorCalculation];
                                        if ((registers.CCCTL & 0x100) != 0) alpha |= 0x80;
                                    }
                                }
                                if (sprite.MsbShadow)
                                {
                                    if (spriteWindowEnabled)
                                    {
                                        _spriteWindowMask[y * _vdp2Width + x] = 1;
                                        info.ShadowType = SaturnTitanShadow.Msb;
                                    }
                                    else
                                        info.ShadowType = SaturnTitanShadow.Msb;

                                    if (pixel == 0)
                                    {
                                        _titan.PutPixel(priority[sprite.Priority], i, outputY, info.PostPixelFetchCalc(in info, ColSatToYab32(alpha, 0)),
                                            info.LineScreen, info.Layer, info.ShadowEnabled, info.ShadowType);
                                        continue;
                                    }
                                }
                                if (spriteWindowEnabled && visibility != 3 && !WindowPieceAllows(visibility, windowControl, i, j))
                                    continue;

                                _titan.PutPixel(priority[sprite.Priority], i, outputY, info.PostPixelFetchCalc(in info, ColSatToYab32(alpha, dot)),
                                    info.LineScreen, info.Layer, info.ShadowEnabled, info.ShadowType);
                            }
                        }
                        else
                        {
                            ushort pixel = _frontFrameBuffer[y * framebufferWidth + x];
                            if (pixel != 0)
                            {
                                int alpha = 0x3F;
                                SpritePixelInfo sprite = GetSpritePixelInfo(spriteType, ref pixel);
                                if (sprite.NormalShadow)
                                {
                                    info.ShadowType = SaturnTitanShadow.Normal;
                                    _titan.PutPixel(priority[sprite.Priority], i, outputY, ColSatToYab16(0x3F, 0),
                                        info.LineScreen, info.Layer, info.ShadowEnabled, info.ShadowType);
                                    continue;
                                }

                                uint dot = ColorRamGetColor(input.ColorMode, input.ColorRam, spriteColorOffset + pixel);
                                if (colorCalculationEnabled && WindowPieceAllows(colorDecision, colorControl, i, j))
                                {
                                    bool transparent = false;
                                    switch (colorCondition)
                                    {
                                        case 0: if (priority[sprite.Priority] <= colorThreshold) transparent = true; break;
                                        case 1: if (priority[sprite.Priority] == colorThreshold) transparent = true; break;
                                        case 2: if (priority[sprite.Priority] >= colorThreshold) transparent = true; break;
                                        case 3: if ((dot & 0x80000000) != 0) transparent = true; break;
                                    }
                                    if ((registers.CCCTL & 0x200) != 0)
                                    {
                                        alpha = colorCalc[sprite.ColorCalculation];
                                        if (transparent) alpha |= 0x80;
                                    }
                                    else if (transparent)
                                    {
                                        alpha = colorCalc[sprite.ColorCalculation];
                                        if ((registers.CCCTL & 0x100) != 0) alpha |= 0x80;
                                    }
                                }

                                _titan.PutPixel(priority[sprite.Priority], i, outputY, info.PostPixelFetchCalc(in info, ColSatToYab32(alpha, dot)),
                                    info.LineScreen, info.Layer, info.ShadowEnabled, info.ShadowType);
                            }
                        }
                    }
                }
                outputY++;
            }
        }
    }

    public void EndVdp2Draw(SaturnVdp2 vdp2, SaturnVdp1 vdp1)
    {
        if (_numLayerThreads > 0)
        {
            WaitForLayerThread(SaturnTitanLayer.Nbg0);
            WaitForLayerThread(SaturnTitanLayer.Nbg1);
            WaitForLayerThread(SaturnTitanLayer.Nbg2);
            WaitForLayerThread(SaturnTitanLayer.Nbg3);
            WaitForLayerThread(SaturnTitanLayer.Rbg0);
            WaitForLayerThread(SaturnTitanLayer.Sprite);
            _layerThreadFailure?.Throw();
        }
        var oddFrame = vdp2.OddFrame;
        _titan.Render(_displayBuffer, vdp2.Registers, _vdp2Interlace, oddFrame);
        DisplayFieldParity = _vdp2Interlace ? (oddFrame ? 1 : 0) : null;
        SwapFrameBuffer(vdp1);
    }

    public void ResetVdp1(ref SaturnVdp1Registers registers)
    {
        VidsoftWaitForVdp1Thread();
        registers.UserClipX1 = registers.SystemClipX1 = 0;
        registers.UserClipY1 = registers.SystemClipY1 = 0;
        registers.UserClipX2 = registers.SystemClipX2 = 512;
        registers.UserClipY2 = registers.SystemClipY2 = 256;
    }

    private void DrawStartBody(SaturnVdp1 vdp1, in SaturnVdp1Registers registers, byte[] backFrameBuffer)
    {
        _interlace = (registers.FBCR & 8) != 0 ? 2 : 1;
        if ((registers.TVMR & 1) != 0)
        {
            if ((registers.TVMR & 2) != 0)
            {
                _width = 512;
                _height = 512;
            }
            else
            {
                _width = 1024;
                _height = 256;
            }
            _pixelSize = 1;
        }
        else
        {
            _width = 512;
            _height = 256;
            _pixelSize = 2;
        }

        EraseFrameBuffer(vdp1, registers, backFrameBuffer);
    }

    public void StartVdp1Draw(SaturnVdp1 vdp1)
    {
        if (_vdp1ThreadEnabled)
        {
            VidsoftWaitForVdp1Thread();

            vdp1.Ram.AsSpan().CopyTo(_vdp1ThreadRam);
            _vdp1ThreadRegisters = vdp1.Registers;
            _backFrameBuffer.AsSpan().CopyTo(_vdp1ThreadBackFrameBuffer);
            DrawStartBody(vdp1, _vdp1ThreadRegisters, _vdp1ThreadBackFrameBuffer);

            lock (_vdp1ThreadLock)
            {
                _vdp1ThreadChip = vdp1;
                Volatile.Write(ref _vdp1ThreadDrawFinished, false);
                _vdp1ThreadNeedsDraw = true;
                _vdp1WakeEvent!.Set();
            }

            vdp1.FakeDrawCommands(vdp1.Ram, ref vdp1.Registers);
        }
        else
        {
            DrawStartBody(vdp1, vdp1.Registers, _backFrameBuffer);
            vdp1.DrawCommands(vdp1.Ram, ref vdp1.Registers, _backFrameBuffer);
        }
    }

    private void VidsoftVdp1Thread()
    {
        AutoResetEvent wakeEvent = _vdp1WakeEvent!;
        while (true)
        {
            wakeEvent.WaitOne();
            SaturnVdp1 vdp1;
            lock (_vdp1ThreadLock)
            {
                if (!_vdp1ThreadNeedsDraw)
                    return;

                _vdp1ThreadNeedsDraw = false;
                vdp1 = _vdp1ThreadChip!;
            }

            try
            {
                vdp1.DrawCommands(_vdp1ThreadRam, ref _vdp1ThreadRegisters, _vdp1ThreadBackFrameBuffer);
                _vdp1ThreadBackFrameBuffer.AsSpan().CopyTo(_backFrameBuffer);
            }
            catch (Exception exception)
            {
                _vdp1ThreadFailure = ExceptionDispatchInfo.Capture(exception);
            }
            finally
            {
                lock (_vdp1ThreadLock)
                {
                    _vdp1ThreadChip = null;
                    Volatile.Write(ref _vdp1ThreadDrawFinished, true);
                    Monitor.PulseAll(_vdp1ThreadLock);
                    if (_vdp1ThreadStopping)
                        wakeEvent.Set();
                }
            }
        }
    }

    private void VidsoftWaitForVdp1Thread()
    {
        if (_vdp1ThreadEnabled)
        {
            if (!Volatile.Read(ref _vdp1ThreadDrawFinished))
            {
                lock (_vdp1ThreadLock)
                {
                    while (!_vdp1ThreadDrawFinished)
                        Monitor.Wait(_vdp1ThreadLock);
                }
            }
            _vdp1ThreadFailure?.Throw();
        }
    }

    public void SetVdp1ThreadEnable(bool enabled)
    {
        if (!enabled)
            VidsoftWaitForVdp1Thread();
        _vdp1ThreadEnabled = enabled;
    }

    public void EndVdp1Draw() { }

    private static int ReadPattern16(uint address, uint offset, byte[] ram)
    {
        int dot = T1ReadByte(ram, (address + (offset >> 1)) & 0x7FFFF);
        if ((offset & 1) == 0)
            dot >>= 4;
        else
            dot &= 0xF;
        return dot;
    }

    private static int ReadPattern256(uint address, uint offset, byte[] ram) =>
        T1ReadByte(ram, (address + offset) & 0x7FFFF) & 0xFF;

    private static int ReadPattern64k(uint address, uint offset, byte[] ram) =>
        T1ReadWord(ram, (address + 2 * offset) & 0x7FFFF);

    private struct ColorParameters
    {
        internal double Red;
        internal double Green;
        internal double Blue;
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private bool GetPixel<T>(int column, in DrawLineData data, byte[] ram) where T : struct, ITextureFormat
    {
        uint address = data.RowAddress;
        int colorBank = data.ColorBank;
        bool drawTransparent = data.DrawTransparent;
        bool endCodes = data.EndCodes;
        uint offset = unchecked((uint)(data.ColumnStart + column * data.ColumnDirection));

        switch (T.Mode)
        {
            case 0:
                _currentPixel = ReadPattern16(address, offset, ram);
                if (endCodes && _currentPixel == 0xF)
                    return true;
                if (_currentPixel != 0 || drawTransparent)
                    _currentPixel = (colorBank & 0xFFF0) | _currentPixel;
                _currentPixelIsVisible = 0xF;
                break;
            case 1:
                _currentPixel = ReadPattern16(address, offset, ram);
                if (endCodes && _currentPixel == 0xF)
                    return true;
                if (_currentPixel != 0 || drawTransparent)
                    _currentPixel = T1ReadWord(ram, ((uint)_currentPixel * 2 + ((uint)colorBank << 3)) & 0x7FFFF);
                _currentPixelIsVisible = 0xFFFF;
                break;
            case 2:
                _currentPixel = ReadPattern256(address, offset, ram);
                if (endCodes && _currentPixel == 0xFF)
                    return true;
                _currentPixel &= 0x3F;
                if (_currentPixel != 0 || drawTransparent)
                    _currentPixel = (colorBank & 0xFFC0) | _currentPixel;
                _currentPixelIsVisible = 0x3F;
                break;
            case 3:
                _currentPixel = ReadPattern256(address, offset, ram);
                if (endCodes && _currentPixel == 0xFF)
                    return true;
                _currentPixel &= 0x7F;
                if (_currentPixel != 0 || drawTransparent)
                    _currentPixel = (colorBank & 0xFF80) | _currentPixel;
                _currentPixelIsVisible = 0x7F;
                break;
            case 4:
                _currentPixel = ReadPattern256(address, offset, ram);
                if (endCodes && _currentPixel == 0xFF)
                    return true;
                _currentPixelIsVisible = 0xFF;
                if (_currentPixel != 0 || drawTransparent)
                    _currentPixel = (colorBank & 0xFF00) | _currentPixel;
                break;
            case 5:
            case 6:
                _currentPixel = T.Mode == 5 ? ReadPattern64k(address, offset, ram) : T1ReadWord(ram, 0);
                if (endCodes && (_currentPixel & 0xC000) == 0x4000)
                    return true;
                if (_currentPixel < 0x4000 && !drawTransparent)
                    _currentPixel = 0;
                _currentPixelIsVisible = 0xFFFF;
                break;
        }

        return false;
    }

    private static int GouraudAdjust(int color, int tableValue)
    {
        color += tableValue - 0x10;
        if (color < 0)
            color = 0;
        if (color > 0x1F)
            color = 0x1F;
        return color;
    }

    private struct LineClipping
    {
        internal readonly int MinX, MinY, MaxX, MaxY;
        internal readonly int UserMinX, UserMinY, UserMaxX, UserMaxY;
        internal readonly bool RejectUserArea;
        internal readonly int LineParity;
        // Corresponds to the drawn_ac flag in Mednafen vdp1_common.h.
        internal bool HasEnteredWindow { get; private set; }

        internal bool IsOutsideWindow(int x, int y)
        {
            bool outside = x < MinX || x > MaxX || y < MinY || y > MaxY;
            HasEnteredWindow |= !outside;
            return outside;
        }

        internal LineClipping(int interlace, in SaturnVdp1Registers registers, ushort mode)
        {
            LineParity = interlace == 2 ? (registers.FBCR >> 2) & 1 : -1;
            bool userClip = (mode & 0x0400) != 0;
            RejectUserArea = (mode & 0x0600) == 0x0600;
            bool restrictToUserArea = userClip && !RejectUserArea;
            MinX = restrictToUserArea ? registers.UserClipX1 : 0;
            MinY = restrictToUserArea ? registers.UserClipY1 : 0;
            MaxX = restrictToUserArea ? Math.Min(registers.UserClipX2, registers.SystemClipX2) : registers.SystemClipX2;
            MaxY = restrictToUserArea ? Math.Min(registers.UserClipY2, registers.SystemClipY2) : registers.SystemClipY2;
            UserMinX = registers.UserClipX1;
            UserMinY = registers.UserClipY1;
            UserMaxX = registers.UserClipX2;
            UserMaxY = registers.UserClipY2;
        }
    }

    private void PutPixel8(int x, int y, in LineClipping clip, in SaturnVdp1Command command, byte[] frameBuffer)
    {
        int y2 = y / _interlace;
        int pixel = y2 * _width + x;
        bool mesh = (command.CMDPMOD & 0x100) != 0;
        bool drawTransparent = (command.CMDPMOD & 0x40) != 0;
        if ((uint)pixel >= 0x40000)
            return;
        if (clip.LineParity >= 0 && (y & 1) != clip.LineParity)
            return;

        _currentPixel &= 0xFF;
        // Mednafen PlotPixel uses the draw coordinate for the mesh.
        if (mesh && ((x ^ y) & 1) != 0)
            return;
        if (clip.RejectUserArea && x >= clip.UserMinX && x <= clip.UserMaxX &&
            y >= clip.UserMinY && y <= clip.UserMaxY)
            return;
        if (!drawTransparent && ((_currentPixel & _currentPixelIsVisible) == 0 || _currentPixel == 0))
            return;

        if ((command.CMDPMOD & 0x8000) != 0)
        {
            // Mednafen PlotPixel reads the lower half in rotated 8-bit mode.
            int sourcePixel = _width == 512 ? pixel & 0x1FFFF : pixel;
            int msb = (x & 1) == 0 ? 0x80 : 0;
            frameBuffer[pixel] = (byte)(frameBuffer[sourcePixel] | msb);
        }
        else
            frameBuffer[pixel] = (byte)_currentPixel;
    }

    private static int BlendHalfRgb555(int destination, int source)
    {
        int sum = (destination & 0x7FFF) + (source & 0x7FFF);
        return (sum - ((destination ^ source) & 0x0421)) >> 1;
    }

    private static int PackGouraudColor(int red, int green, int blue) =>
        0x8000 | (red & 0x1F) | ((green & 0x1F) << 5) | ((blue & 0x1F) << 10);

    private void PutPixel(int x, int y, in LineClipping clip, in SaturnVdp1Command command, Span<ushort> frameBuffer)
    {
        bool mesh = (command.CMDPMOD & 0x100) != 0;
        bool drawTransparent = (command.CMDPMOD & 0x40) != 0;
        int originalY = y;
        if (clip.LineParity >= 0 && (y & 1) != clip.LineParity)
            return;

        y /= _interlace;
        int pixel = y * _width + x;
        if ((uint)pixel >= 0x20000)
            return;
        if (mesh && ((x ^ originalY) & 1) != 0)
            return;
        if (clip.RejectUserArea && x >= clip.UserMinX && x <= clip.UserMaxX &&
            originalY >= clip.UserMinY && originalY <= clip.UserMaxY)
            return;

        if (!drawTransparent && (_currentPixel & _currentPixelIsVisible) == 0)
            return;

        ref ushort framebufferPixel = ref frameBuffer[pixel];
        if ((command.CMDPMOD & 0x8000) != 0)
        {
            framebufferPixel |= 0x8000;
            return;
        }

        switch (command.CMDPMOD & 7)
        {
            case 0:
                if (_currentPixel != 0 || drawTransparent)
                    framebufferPixel = unchecked((ushort)_currentPixel);
                break;
            case 1:
            {
                int destination = framebufferPixel;
                if ((destination & 0x8000) != 0)
                    framebufferPixel = (ushort)(((destination & 0x7BDE) >> 1) | 0x8000);
                break;
            }
            case 2:
                framebufferPixel = (ushort)(((_currentPixel & ~0x8421) >> 1) | 0x8000);
                break;
            case 3:
            {
                int destination = framebufferPixel;
                int color = (destination & 0x8000) != 0
                    ? BlendHalfRgb555(destination, _currentPixel) | 0x8000
                    : _currentPixel;
                framebufferPixel = unchecked((ushort)color);
                break;
            }
            case 4:
            {
                int colorMode = (command.CMDPMOD >> 3) & 7;
                if (colorMode != 5 && colorMode != 1 &&
                    (int)_leftColumnColor.Green == 16 && (int)_leftColumnColor.Blue == 16)
                {
                    int offset = Math.Max(0, (int)(_leftColumnColor.Red - 16));
                    _currentPixel += offset;
                    framebufferPixel = unchecked((ushort)_currentPixel);
                    break;
                }

                int color = PackGouraudColor(
                    GouraudAdjust(_currentPixel & 0x1F, (int)_leftColumnColor.Red),
                    GouraudAdjust((_currentPixel >> 5) & 0x1F, (int)_leftColumnColor.Green),
                    GouraudAdjust((_currentPixel >> 10) & 0x1F, (int)_leftColumnColor.Blue));
                framebufferPixel = (ushort)color;
                break;
            }
            case 5:
                framebufferPixel = 0;
                break;
            case 6:
            case 7:
            {
                int color = PackGouraudColor(
                    GouraudAdjust(_currentPixel & 0x1F, (int)_leftColumnColor.Red),
                    GouraudAdjust((_currentPixel >> 5) & 0x1F, (int)_leftColumnColor.Green),
                    GouraudAdjust((_currentPixel >> 10) & 0x1F, (int)_leftColumnColor.Blue));
                color = (color & 0x7FFF) | (_currentPixel & 0x8000);
                if ((command.CMDPMOD & 1) == 0)
                    color = ((color & 0x7BDE) >> 1) | (color & 0x8000);
                else if ((framebufferPixel & 0x8000) != 0)
                    color = (color + framebufferPixel - ((color ^ framebufferPixel) & 0x8421)) >> 1;
                framebufferPixel = (ushort)color;
                break;
            }
        }
    }

    // With greedy traversal, each axis step contributes one pixel before the endpoint.
    private static int CountLinePixels(int x1, int y1, int x2, int y2)
    {
        int dx = Math.Abs(x2 - x1);
        int dy = Math.Abs(y2 - y1);
        return dx > 999 || dy > 999 ? int.MaxValue : dx + dy + 1;
    }

    private static unsafe int IterateOverLine<T>(
        int x1, int y1, int x2, int y2, bool greedy, ref T state,
        delegate*<int, int, int, ref T, in SaturnVdp1Registers, in SaturnVdp1Command, byte[], byte[], int> callback,
        in SaturnVdp1Registers registers, in SaturnVdp1Command command, byte[] ram, byte[] frameBuffer)
        where T : allows ref struct
    {
        int accumulator = 0;
        int index = 0;
        int dx = x2 - x1;
        int dy = y2 - y1;
        int ax = dx >= 0 ? 1 : -1;
        int ay = dy >= 0 ? 1 : -1;
        if (Math.Abs(dx) > 999 || Math.Abs(dy) > 999)
            return int.MaxValue;

        if (Math.Abs(dx) > Math.Abs(dy))
        {
            if (ax != ay)
                dx = -dx;

            for (; x1 != x2; x1 += ax, index++)
            {
                if (callback(x1, y1, index, ref state, in registers, in command, ram, frameBuffer) != 0)
                    return index + 1;
                accumulator += dy;
                if (Math.Abs(accumulator) >= Math.Abs(dx))
                {
                    accumulator -= dx;
                    y1 += ay;
                    if (greedy)
                    {
                        index++;
                        if (ax == ay)
                        {
                            if (callback(x1 + ax, y1 - ay, index, ref state, in registers, in command, ram, frameBuffer) != 0)
                                return index + 1;
                        }
                        else
                        {
                            if (callback(x1, y1, index, ref state, in registers, in command, ram, frameBuffer) != 0)
                                return index + 1;
                        }
                    }
                }
            }

            callback(x2, y2, index, ref state, in registers, in command, ram, frameBuffer);
            index++;
        }
        else
        {
            if (ax != ay)
                dy = -dy;

            for (; y1 != y2; y1 += ay, index++)
            {
                if (callback(x1, y1, index, ref state, in registers, in command, ram, frameBuffer) != 0)
                    return index + 1;
                accumulator += dx;
                if (Math.Abs(accumulator) >= Math.Abs(dy))
                {
                    accumulator -= dy;
                    x1 += ax;
                    if (greedy)
                    {
                        index++;
                        if (ay == ax)
                        {
                            if (callback(x1, y1, index, ref state, in registers, in command, ram, frameBuffer) != 0)
                                return index + 1;
                        }
                        else
                        {
                            if (callback(x1 - ax, y1 + ay, index, ref state, in registers, in command, ram, frameBuffer) != 0)
                                return index + 1;
                        }
                    }
                }
            }

            callback(x2, y2, index, ref state, in registers, in command, ram, frameBuffer);
            index++;
        }

        return index;
    }

    private interface ITextureFormat
    {
        static abstract int Mode { get; }
    }

    private readonly struct Bank16 : ITextureFormat { public static int Mode => 0; }
    private readonly struct Lookup16 : ITextureFormat { public static int Mode => 1; }
    private readonly struct Bank64 : ITextureFormat { public static int Mode => 2; }
    private readonly struct Bank128 : ITextureFormat { public static int Mode => 3; }
    private readonly struct Bank256 : ITextureFormat { public static int Mode => 4; }
    private readonly struct DirectColor : ITextureFormat { public static int Mode => 5; }
    private readonly struct ProhibitedColor : ITextureFormat { public static int Mode => 6; }

    private ref struct DrawLineData
    {
        internal SaturnVideoSoftware Video;
        internal Span<ushort> FramebufferWords;
        internal LineClipping Clip;
        internal uint RowAddress;
        internal int ColumnStart;
        internal int ColumnDirection;
        internal int ColorBank;
        internal bool DrawTransparent;
        internal bool EndCodes;
        internal double TextureStep;
        internal ColorParameters ColorStep;
        internal int EndCodesDetected;
        internal int PreviousStep;
    }

    private static int DrawLineCallback<T>(int x, int y, int index, ref DrawLineData data,
        in SaturnVdp1Registers registers, in SaturnVdp1Command command, byte[] ram, byte[] frameBuffer)
        where T : struct, ITextureFormat
    {
        SaturnVideoSoftware video = data.Video;
        bool outside = data.Clip.IsOutsideWindow(x, y);
        if (outside && data.Clip.HasEnteredWindow)
            return -1;

        video._leftColumnColor.Red += data.ColorStep.Red;
        video._leftColumnColor.Green += data.ColorStep.Green;
        video._leftColumnColor.Blue += data.ColorStep.Blue;
        int currentStep = (int)(index * data.TextureStep);
        if (video.GetPixel<T>(currentStep, data, ram))
        {
            if (currentStep != data.PreviousStep)
            {
                data.PreviousStep = currentStep;
                data.EndCodesDetected++;
            }
        }
        else if (!outside)
        {
            if (video._pixelSize == 2)
                video.PutPixel(x, y, data.Clip, command, data.FramebufferWords);
            else
                video.PutPixel8(x, y, data.Clip, command, frameBuffer);
        }

        return data.EndCodesDetected == 2 ? -1 : 0;
    }

    private static bool IsVdp1Textured(in SaturnVdp1Command command) =>
        (command.CMDCTRL & 7) is not (4 or 5 or 6);

    private ref struct SolidLineData
    {
        internal SaturnVideoSoftware Video;
        internal Span<ushort> FramebufferWords;
        internal ColorParameters ColorStep;
        internal LineClipping Clip;
        internal int VisibilityMask;
    }

    private static int DrawSolidPixel(int x, int y, int index, ref SolidLineData data,
        in SaturnVdp1Registers registers, in SaturnVdp1Command command, byte[] ram, byte[] frameBuffer)
    {
        SaturnVideoSoftware video = data.Video;
        bool outside = data.Clip.IsOutsideWindow(x, y);
        if (outside && data.Clip.HasEnteredWindow)
            return -1;

        video._leftColumnColor.Red += data.ColorStep.Red;
        video._leftColumnColor.Green += data.ColorStep.Green;
        video._leftColumnColor.Blue += data.ColorStep.Blue;
        video._currentPixel = command.CMDCOLR;
        video._currentPixelIsVisible = data.VisibilityMask;
        if (outside)
            return 0;

        if (video._pixelSize == 2)
            video.PutPixel(x, y, data.Clip, command, data.FramebufferWords);
        else
            video.PutPixel8(x, y, data.Clip, command, frameBuffer);

        return 0;
    }

    private unsafe int DrawLine(int x1, int y1, int x2, int y2, bool greedy, double lineNumber, double textureStep,
        ColorParameters step, in SaturnVdp1Registers registers, in SaturnVdp1Command command, byte[] ram, byte[] frameBuffer)
    {
        // Mednafen SetupDrawLine reduces a pre-clipped segment to its first point.
        if ((command.CMDPMOD & 0x0800) == 0)
        {
            bool userClipInside = (command.CMDPMOD & 0x0600) == 0x0400;
            int minX = userClipInside ? registers.UserClipX1 : 0;
            int minY = userClipInside ? registers.UserClipY1 : 0;
            int maxX = userClipInside ? registers.UserClipX2 : registers.SystemClipX2;
            int maxY = userClipInside ? registers.UserClipY2 : registers.SystemClipY2;
            if ((x1 < minX && x2 < minX) || (x1 > maxX && x2 > maxX) ||
                (y1 < minY && y2 < minY) || (y1 > maxY && y2 > maxY))
            {
                x2 = x1;
                y2 = y1;
            }
        }

        LineClipping clip = new(_interlace, registers, command.CMDPMOD);
        // Mednafen uses a typed draw framebuffer in PlotPixel.
        Span<ushort> framebufferWords = _pixelSize == 2 ? MemoryMarshal.Cast<byte, ushort>(frameBuffer.AsSpan()) : default;
        if (!IsVdp1Textured(command))
        {
            int colorMode = (command.CMDPMOD >> 3) & 7;
            SolidLineData solid = new()
            {
                Video = this,
                FramebufferWords = framebufferWords,
                ColorStep = step,
                Clip = clip,
                VisibilityMask = colorMode switch
                {
                    0 => 0xF,
                    2 => 0x3F,
                    3 => 0x7F,
                    4 => 0xFF,
                    1 or 5 or 6 => 0xFFFF,
                    _ => _currentPixelIsVisible
                }
            };
            return IterateOverLine(x1, y1, x2, y2, greedy, ref solid, &DrawSolidPixel, registers, command, ram, frameBuffer);
        }

        int line = (int)lineNumber;
        if ((command.CMDCTRL & 0x20) != 0)
            line = _characterHeight - line - 1;
        bool flipHorizontal = (command.CMDCTRL & 0x10) != 0;
        int mode = (command.CMDPMOD >> 3) & 7;
        int rowBytes = mode switch
        {
            0 or 1 => _characterWidth >> 1,
            5 or 6 => _characterWidth * 2,
            _ => _characterWidth
        };
        DrawLineData data = new()
        {
            Video = this,
            FramebufferWords = framebufferWords,
            Clip = clip,
            RowAddress = unchecked(((uint)command.CMDSRCA << 3) + (uint)(line * rowBytes)),
            ColumnStart = flipHorizontal ? _characterWidth - 1 : 0,
            ColumnDirection = flipHorizontal ? -1 : 1,
            ColorBank = command.CMDCOLR,
            DrawTransparent = (command.CMDPMOD & 0x40) != 0,
            EndCodes = (command.CMDPMOD & 0x80) == 0,
            TextureStep = textureStep,
            ColorStep = step,
            EndCodesDetected = 0,
            PreviousStep = 123456789
        };
        delegate*<int, int, int, ref DrawLineData, in SaturnVdp1Registers, in SaturnVdp1Command, byte[], byte[], int>
            callback = mode switch
            {
                0 => &DrawLineCallback<Bank16>,
                1 => &DrawLineCallback<Lookup16>,
                2 => &DrawLineCallback<Bank64>,
                3 => &DrawLineCallback<Bank128>,
                4 => &DrawLineCallback<Bank256>,
                5 => &DrawLineCallback<DirectColor>,
                _ => &DrawLineCallback<ProhibitedColor>
            };
        return IterateOverLine(x1, y1, x2, y2, greedy, ref data, callback, registers, command, ram, frameBuffer);
    }

    private static double Interpolate(double start, double end, int steps) =>
        steps == 0 ? 1 : (end - start) / steps;

    private void GouraudTable(byte[] ram, in SaturnVdp1Command command)
    {
        uint address = (uint)command.CMDGRDA << 3;
        _gouraudA = T1ReadWord(ram, address);
        _gouraudB = T1ReadWord(ram, address + 2);
        _gouraudC = T1ReadWord(ram, address + 4);
        _gouraudD = T1ReadWord(ram, address + 6);
    }

    private readonly record struct LineCoordinates(int[] X, int[] Y);

    private static int StoreLineCoords(int x, int y, int index, ref LineCoordinates coordinates,
        in SaturnVdp1Registers registers, in SaturnVdp1Command command, byte[] ram, byte[] frameBuffer)
    {
        coordinates.X[index] = x;
        coordinates.Y[index] = y;
        return 0;
    }

    private bool IsPreClipped(short tlX, short tlY, short blX, short blY,
        short trX, short trY, short brX, short brY, in SaturnVdp1Registers registers)
    {
        int yLimit = registers.SystemClipY2;
        if (_interlace != 0)
            yLimit *= 2;

        if (tlX < 0 && blX < 0 && trX < 0 && brX < 0)
            return true;
        if (tlX > registers.SystemClipX2 && blX > registers.SystemClipX2 &&
            trX > registers.SystemClipX2 && brX > registers.SystemClipX2)
            return true;
        if (tlY < 0 && blY < 0 && trY < 0 && brY < 0)
            return true;
        if (tlY > yLimit && blY > yLimit && trY > yLimit && brY > yLimit)
            return true;

        return false;
    }

    private static ColorParameters UnpackColor(ushort color) => new()
    {
        Red = color & 0x1F,
        Green = (color >> 5) & 0x1F,
        Blue = (color >> 10) & 0x1F
    };

    private unsafe void DrawQuad(short tlX, short tlY, short blX, short blY, short trX, short trY,
        short brX, short brY, byte[] ram, in SaturnVdp1Registers registers,
        in SaturnVdp1Command command, byte[] frameBuffer)
    {
        ColorParameters leftStep = default;
        ColorParameters rightStep = default;
        double leftLineStep = 1;
        double rightLineStep = 1;
        Span<ColorParameters> colors = stackalloc ColorParameters[4];
        if (IsPreClipped(tlX, tlY, blX, blY, trX, trY, brX, brY, registers))
            return;

        _characterWidth = ((command.CMDSIZE >> 8) & 0x3F) * 8;
        _characterHeight = command.CMDSIZE & 0xFF;
        LineCoordinates coordinates = new(_xLeft, _yLeft);
        int totalLeft = IterateOverLine(tlX, tlY, blX, blY, false, ref coordinates, &StoreLineCoords, registers, command, ram, frameBuffer);
        coordinates = new(_xRight, _yRight);
        int totalRight = IterateOverLine(trX, trY, brX, brY, false, ref coordinates, &StoreLineCoords, registers, command, ram, frameBuffer);
        if (totalLeft == int.MaxValue || totalRight == int.MaxValue)
            return;

        int total = totalLeft > totalRight ? totalLeft : totalRight;
        if ((command.CMDPMOD & 4) != 0)
        {
            GouraudTable(ram, command);
            colors[0] = UnpackColor(_gouraudA);
            colors[1] = UnpackColor(_gouraudD);
            colors[2] = UnpackColor(_gouraudB);
            colors[3] = UnpackColor(_gouraudC);

            leftStep.Red = Interpolate(colors[0].Red, colors[1].Red, total);
            leftStep.Green = Interpolate(colors[0].Green, colors[1].Green, total);
            leftStep.Blue = Interpolate(colors[0].Blue, colors[1].Blue, total);
            rightStep.Red = Interpolate(colors[2].Red, colors[3].Red, total);
            rightStep.Green = Interpolate(colors[2].Green, colors[3].Green, total);
            rightStep.Blue = Interpolate(colors[2].Blue, colors[3].Blue, total);
        }

        if (total == totalLeft && totalLeft != totalRight)
        {
            leftLineStep = 1;
            rightLineStep = (double)totalRight / totalLeft;
        }
        else if (totalLeft != totalRight)
        {
            rightLineStep = 1;
            leftLineStep = (double)totalLeft / totalRight;
        }

        bool textured = IsVdp1Textured(command);
        bool gouraud = (command.CMDPMOD & 4) != 0;
        for (int index = 0; index < total; index++)
        {
            ColorParameters leftToRightStep = default;
            int left = (int)(index * leftLineStep);
            int right = (int)(index * rightLineStep);
            if (!textured && !gouraud)
            {
                DrawLine(_xLeft[left], _yLeft[left], _xRight[right], _yRight[right], true, 0, 0,
                    leftToRightStep, registers, command, ram, frameBuffer);
                continue;
            }

            int lineLength = CountLinePixels(_xLeft[left], _yLeft[left], _xRight[right], _yRight[right]);
            double xTextureStep = Interpolate(0, _characterWidth, lineLength);
            double yTextureStep = Interpolate(0, _characterHeight, total);

            if ((command.CMDPMOD & 4) != 0)
            {
                _leftColumnColor.Red = colors[0].Red + leftStep.Red * index;
                _leftColumnColor.Green = colors[0].Green + leftStep.Green * index;
                _leftColumnColor.Blue = colors[0].Blue + leftStep.Blue * index;
                ColorParameters rightColumnColor = new()
                {
                    Red = colors[2].Red + rightStep.Red * index,
                    Green = colors[2].Green + rightStep.Green * index,
                    Blue = colors[2].Blue + rightStep.Blue * index
                };
                leftToRightStep.Red = Interpolate(_leftColumnColor.Red, rightColumnColor.Red, lineLength);
                leftToRightStep.Green = Interpolate(_leftColumnColor.Green, rightColumnColor.Green, lineLength);
                leftToRightStep.Blue = Interpolate(_leftColumnColor.Blue, rightColumnColor.Blue, lineLength);
            }

            DrawLine(_xLeft[left], _yLeft[left], _xRight[right], _yRight[right], true, yTextureStep * index,
                xTextureStep, leftToRightStep, registers, command, ram, frameBuffer);
        }
    }

    public void NormalSpriteDraw(byte[] ram, ref SaturnVdp1Registers registers, byte[] backFrameBuffer)
    {
        SaturnVdp1Command command = SaturnVdp1.ReadCommand(registers.Address, ram);
        short left = unchecked((short)(command.CMDXA + registers.LocalX));
        short top = unchecked((short)(command.CMDYA + registers.LocalY));
        int width = ((command.CMDSIZE >> 8) & 0x3F) * 8;
        int height = command.CMDSIZE & 0xFF;
        short right = unchecked((short)(left + width - 1));
        short bottom = unchecked((short)(top + height - 1));
        DrawQuad(left, top, left, bottom, right, top, right, bottom, ram, registers, command, backFrameBuffer);
    }

    public void ScaledSpriteDraw(byte[] ram, ref SaturnVdp1Registers registers, byte[] backFrameBuffer)
    {
        SaturnVdp1Command command = SaturnVdp1.ReadCommand(registers.Address, ram);
        int x = command.CMDXA + registers.LocalX;
        int y = command.CMDYA + registers.LocalY;
        int width;
        int height;

        switch ((command.CMDCTRL >> 8) & 0xF)
        {
            case 0x5: // Upper-left
                width = command.CMDXB + 1;
                height = command.CMDYB + 1;
                break;
            case 0x6: // Upper-center
                width = command.CMDXB;
                height = command.CMDYB;
                x -= width / 2;
                width++;
                height++;
                break;
            case 0x7: // Upper-right
                width = command.CMDXB;
                height = command.CMDYB;
                x -= width;
                width++;
                height++;
                break;
            case 0x9: // Center-left
                width = command.CMDXB;
                height = command.CMDYB;
                y -= height / 2;
                width++;
                height++;
                break;
            case 0xA: // Center-center
                width = command.CMDXB;
                height = command.CMDYB;
                x -= width / 2;
                y -= height / 2;
                width++;
                height++;
                break;
            case 0xB: // Center-right
                width = command.CMDXB;
                height = command.CMDYB;
                x -= width;
                y -= height / 2;
                width++;
                height++;
                break;
            case 0xD: // Lower-left
                width = command.CMDXB;
                height = command.CMDYB;
                y -= height;
                width++;
                height++;
                break;
            case 0xE: // Lower-center
                width = command.CMDXB;
                height = command.CMDYB;
                x -= width / 2;
                y -= height;
                width++;
                height++;
                break;
            case 0xF: // Lower-right
                width = command.CMDXB;
                height = command.CMDYB;
                x -= width;
                y -= height;
                width++;
                height++;
                break;
            default: // Two coordinates
                width = command.CMDXC - x + registers.LocalX + 1;
                height = command.CMDYC - y + registers.LocalY + 1;
                break;
        }

        short left = unchecked((short)x);
        short top = unchecked((short)y);
        short right = unchecked((short)(width + x - 1));
        short bottom = unchecked((short)(height + y - 1));
        DrawQuad(left, top, left, bottom, right, top, right, bottom, ram, registers, command, backFrameBuffer);
    }

    public void DistortedSpriteDraw(byte[] ram, ref SaturnVdp1Registers registers, byte[] backFrameBuffer)
    {
        SaturnVdp1Command command = SaturnVdp1.ReadCommand(registers.Address, ram);
        short xa = unchecked((short)(command.CMDXA + registers.LocalX));
        short ya = unchecked((short)(command.CMDYA + registers.LocalY));
        short xb = unchecked((short)(command.CMDXB + registers.LocalX));
        short yb = unchecked((short)(command.CMDYB + registers.LocalY));
        short xc = unchecked((short)(command.CMDXC + registers.LocalX));
        short yc = unchecked((short)(command.CMDYC + registers.LocalY));
        short xd = unchecked((short)(command.CMDXD + registers.LocalX));
        short yd = unchecked((short)(command.CMDYD + registers.LocalY));
        DrawQuad(xa, ya, xd, yd, xb, yb, xc, yc, ram, registers, command, backFrameBuffer);
    }

    public void PolygonDraw(byte[] ram, ref SaturnVdp1Registers registers, byte[] backFrameBuffer) =>
        DistortedSpriteDraw(ram, ref registers, backFrameBuffer);

    private ColorParameters GouraudLineSetup(int length, ushort table1, ushort table2)
    {
        ColorParameters first = UnpackColor(table1);
        ColorParameters second = UnpackColor(table2);
        ColorParameters step = new()
        {
            Red = Interpolate(first.Red, second.Red, length),
            Green = Interpolate(first.Green, second.Green, length),
            Blue = Interpolate(first.Blue, second.Blue, length)
        };
        _leftColumnColor = first;
        return step;
    }

    public unsafe void PolylineDraw(byte[] ram, ref SaturnVdp1Registers registers, byte[] backFrameBuffer)
    {
        SaturnVdp1Command command = SaturnVdp1.ReadCommand(registers.Address, ram);
        Span<int> x = stackalloc int[4];
        Span<int> y = stackalloc int[4];
        x[0] = registers.LocalX + command.CMDXA;
        y[0] = registers.LocalY + command.CMDYA;
        x[1] = registers.LocalX + command.CMDXB;
        y[1] = registers.LocalY + command.CMDYB;
        x[2] = registers.LocalX + command.CMDXC;
        y[2] = registers.LocalY + command.CMDYC;
        x[3] = registers.LocalX + command.CMDXD;
        y[3] = registers.LocalY + command.CMDYD;
        // Mednafen vdp1_line.cpp reads the current command's colors only in Gouraud mode.
        bool gouraud = (command.CMDPMOD & 4) != 0;
        if (gouraud)
            GouraudTable(ram, command);

        ColorParameters step = gouraud
            ? GouraudLineSetup(CountLinePixels(x[0], y[0], x[1], y[1]), _gouraudA, _gouraudB)
            : default;
        DrawLine(x[0], y[0], x[1], y[1], false, 0, 0, step, registers, command, ram, backFrameBuffer);

        step = gouraud
            ? GouraudLineSetup(CountLinePixels(x[1], y[1], x[2], y[2]), _gouraudB, _gouraudC)
            : default;
        DrawLine(x[1], y[1], x[2], y[2], false, 0, 0, step, registers, command, ram, backFrameBuffer);

        step = gouraud
            ? GouraudLineSetup(CountLinePixels(x[2], y[2], x[3], y[3]), _gouraudD, _gouraudC)
            : default;
        DrawLine(x[3], y[3], x[2], y[2], false, 0, 0, step, registers, command, ram, backFrameBuffer);

        step = gouraud
            ? GouraudLineSetup(CountLinePixels(x[3], y[3], x[0], y[0]), _gouraudA, _gouraudD)
            : default;
        DrawLine(x[0], y[0], x[3], y[3], false, 0, 0, step, registers, command, ram, backFrameBuffer);
    }

    public unsafe void LineDraw(byte[] ram, ref SaturnVdp1Registers registers, byte[] backFrameBuffer)
    {
        SaturnVdp1Command command = SaturnVdp1.ReadCommand(registers.Address, ram);
        int x1 = registers.LocalX + command.CMDXA;
        int y1 = registers.LocalY + command.CMDYA;
        int x2 = registers.LocalX + command.CMDXB;
        int y2 = registers.LocalY + command.CMDYB;

        ColorParameters step = default;
        if ((command.CMDPMOD & 4) != 0)
        {
            GouraudTable(ram, command);
            step = GouraudLineSetup(CountLinePixels(x1, y1, x2, y2), _gouraudA, _gouraudB);
            (step.Blue, step.Green) = (step.Green, step.Blue);
        }
        DrawLine(x1, y1, x2, y2, false, 0, 0, step, registers, command, ram, backFrameBuffer);
    }

    public void UserClipping(byte[] ram, ref SaturnVdp1Registers registers)
    {
        registers.UserClipX1 = T1ReadWord(ram, registers.Address + 0xC);
        registers.UserClipY1 = T1ReadWord(ram, registers.Address + 0xE);
        registers.UserClipX2 = T1ReadWord(ram, registers.Address + 0x14);
        registers.UserClipY2 = T1ReadWord(ram, registers.Address + 0x16);
    }

    public void SystemClipping(byte[] ram, ref SaturnVdp1Registers registers)
    {
        registers.SystemClipX1 = 0;
        registers.SystemClipY1 = 0;
        registers.SystemClipX2 = T1ReadWord(ram, registers.Address + 0x14);
        registers.SystemClipY2 = T1ReadWord(ram, registers.Address + 0x16);
    }

    public void LocalCoordinate(byte[] ram, ref SaturnVdp1Registers registers)
    {
        registers.LocalX = unchecked((short)T1ReadWord(ram, registers.Address + 0xC));
        registers.LocalY = unchecked((short)T1ReadWord(ram, registers.Address + 0xE));
    }

    private uint ReadFrameBuffer(int type, uint address)
    {
        VidsoftWaitForVdp1Thread();
        switch (type)
        {
            case 0:
                return T1ReadByte(_backFrameBuffer, address);
            case 1:
            {
                ushort value = T1ReadWord(_backFrameBuffer, address);
                if (BitConverter.IsLittleEndian)
                    value = BinaryPrimitives.ReverseEndianness(value);
                return value;
            }
            case 2:
            {
                // Mednafen's B-bus DMA reads the upper word before the lower word.
                uint upper = T2ReadWord(_backFrameBuffer, address & 0x3FFFE);
                uint lower = T2ReadWord(_backFrameBuffer, (address + 2) & 0x3FFFE);
                return (upper << 16) | lower;
            }
            default:
                return 0;
        }
    }

    private void WriteFrameBuffer(int type, uint address, uint value)
    {
        VidsoftWaitForVdp1Thread();
        switch (type)
        {
            case 0:
                T1WriteByte(_backFrameBuffer, address, unchecked((byte)value));
                break;
            case 1:
            {
                ushort word = unchecked((ushort)value);
                if (BitConverter.IsLittleEndian)
                    word = BinaryPrimitives.ReverseEndianness(word);
                T1WriteWord(_backFrameBuffer, address, word);
                break;
            }
            case 2:
                if (BitConverter.IsLittleEndian)
                    value = BinaryPrimitives.ReverseEndianness(value);
                value = ((value & 0xFFFF) << 16) | ((value & 0xFFFF0000) >> 16);
                T1WriteLong(_backFrameBuffer, address, value);
                break;
        }
    }

    private void SwapVdp1Buffers()
    {
        byte[] temporary = _frontFrameBuffer;
        _frontFrameBuffer = _backFrameBuffer;
        _backFrameBuffer = temporary;
    }

    private void SwapFrameBuffer(SaturnVdp1 vdp1)
    {
        if ((vdp1.Registers.FBCR & 2) == 0 || vdp1.ManualFrameBufferChange)
        {
            VidsoftWaitForVdp1Thread();
            SwapVdp1Buffers();
            vdp1.ManualFrameBufferChange = false;
        }
    }

    private void EraseFrameBuffer(SaturnVdp1 vdp1, in SaturnVdp1Registers registers, byte[] frameBuffer)
    {
        if ((registers.FBCR & 2) != 0 && !vdp1.ManualFrameBufferErase)
            return;

        int height = Math.Min((registers.EWRR & 0x1FF) + 1, _height);
        int width = Math.Min(((registers.EWRR >> 6) & 0x3F8) + 8, _width);
        int xStart = (registers.EWLR >> 6) & 0x1F8;
        int yStart = registers.EWLR & 0x1FF;
        if (_pixelSize == 2)
        {
            Span<ushort> framebufferWords = MemoryMarshal.Cast<byte, ushort>(frameBuffer.AsSpan());
            for (int y = yStart; y < height; y++)
                for (int x = xStart; x < width; x++)
                    framebufferWords[y * _width + x] = registers.EWDR;
        }
        else
        {
            width = (registers.EWRR >> 9) * 16;
            for (int y = yStart; y < height; y++)
            {
                for (int x = xStart; x < width; x++)
                {
                    int position = y * _width + x;
                    if (position < 0x3FFFF)
                        frameBuffer[position] = unchecked((byte)registers.EWDR);
                }
            }
        }
        vdp1.ManualFrameBufferErase = false;
    }
}
