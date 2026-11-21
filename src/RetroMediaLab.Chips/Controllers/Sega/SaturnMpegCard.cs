// Copyright 2014-2016 James Laird-Wah
// Copyright 2004-2006, 2013 Theo Berkau
// Translated from Yabause mpeg_card.c.
// SPDX-License-Identifier: GPL-2.0-or-later

using System.Diagnostics;
using RetroMediaLab.Chips.CPU.Hitachi.SH1;

namespace RetroMediaLab.Chips.Controllers.Sega;

public sealed class SaturnMpegCard(Action<int>? assertTioca = null, Action<int>? assertTiocb = null)
{
    public void ConnectSh1(SaturnSh7034 sh1)
    {
        assertTioca = sh1.AssertTioca;
        assertTiocb = sh1.AssertTiocb;
    }

    private const uint RamMask = 0x3FFFF;
    private const int FrameBufferWidth = 704;
    private const int FrameBufferHeight = 480;

    private struct Registers
    {
        internal ushort Reg00;
        internal ushort WindowOffsetX;
        internal ushort WindowOffsetY;
        internal ushort WindowSizeX;
        internal ushort WindowSizeY;
        internal ushort FrameBufferX;
        internal ushort FrameBufferY;
        internal ushort ZoomX;
        internal ushort ZoomY;
        internal ushort BorderColor;
        internal ushort Reg14;
        internal ushort Reg18;
        internal ushort Reg1A;
        internal ushort MosaicBlur;
        internal ushort Reg1E;
        internal ushort Reg20;
        internal ushort Reg22;
        internal ushort Reg32;
        internal ushort Reg34;
        internal ushort Reg3E;
        internal ushort Reg80000;
        internal ushort Reg80008;
    }

    private Registers _registers;
    private readonly ushort[] _ram = new ushort[0x40000];
    private uint _ramPointer;
    private readonly uint[] _frameBuffer = new uint[FrameBufferWidth * FrameBufferHeight];

    public void WriteWord(uint address, ushort data)
    {
        switch (address & 0xFFFFF)
        {
            case 0x0: _registers.Reg00 = data; return;
            case 0x2: _registers.WindowOffsetX = data; return;
            case 0x4: _registers.WindowOffsetY = data; return;
            case 0x6: _registers.WindowSizeX = data; return;
            case 0x8: _registers.WindowSizeY = data; return;
            case 0xA: _registers.FrameBufferX = data; return;
            case 0xC: _registers.FrameBufferY = data; return;
            case 0xE: _registers.ZoomX = data; return;
            case 0x10: _registers.ZoomY = data; return;
            case 0x12: _registers.BorderColor = data; return;
            case 0x14: _registers.Reg14 = data; return;
            case 0x18: _registers.Reg18 = data; return;
            case 0x1A: _registers.Reg1A = data; return;
            case 0x1C: _registers.MosaicBlur = data; return;
            case 0x1E: _registers.Reg1E = data; return;
            case 0x20: _registers.Reg20 = data; return;
            case 0x22: _registers.Reg22 = data; return;
            case 0x30: _ramPointer = (uint)data << 2; return;
            case 0x32: _registers.Reg32 = data; return;
            case 0x34: _registers.Reg34 = data; return;
            case 0x36:
                _ramPointer &= RamMask;
                _ram[_ramPointer++] = data;
                return;
            case 0x3E: _registers.Reg3E = data; return;
            case 0x80000: _registers.Reg80000 = data; return;
            case 0x80008: _registers.Reg80008 = data; return;
        }
        Debug.Fail("Unknown MPEG card register.");
    }

    public ushort ReadWord(uint address)
    {
        switch (address & 0xFFFFF)
        {
            case 0:
                return (ushort)((_registers.Reg00 & ~0x10) | (1 << 8) | (1 << 13));
            case 2:
                return _registers.WindowOffsetX;
            case 0x34:
                return 1;
            case 0x36:
                _ramPointer &= RamMask;
                return _ram[_ramPointer++];
            case 0x80000:
                return _registers.Reg80000;
            case 0x80008:
                return _registers.Reg80008;
        }
        return 0;
    }

    public void SetAudioDataTransferInterrupt() => assertTioca?.Invoke(0);

    public void SetVideoDataTransferInterrupt() => assertTioca?.Invoke(1);

    public void SetAudioInterrupt() => assertTioca?.Invoke(2);

    private static ReadOnlySpan<ushort> MosaicMasks =>
    [
        0xFFFE, 0xFFFC, 0xFFF8, 0xFFF0, 0xFFE0, 0xFFC0, 0xFF80, 0xFF00,
        0xFE00, 0xFC00, 0xF800, 0xF000, 0xE000, 0xC000, 0x8000, 0xFFFF
    ];

    private void Render(in SaturnVdp2Registers registers, SaturnVideoSoftware video)
    {
        int priority = (registers.PRINA >> 8) & 7;
        int windowX = (_registers.WindowOffsetX - 0x96) >> 1;
        int windowY = (_registers.WindowOffsetY - 0x26) >> 1;
        int frameBufferOffsetX = _registers.FrameBufferX >> 1;
        int frameBufferOffsetY = _registers.FrameBufferY >> 1;
        float frameBufferX = frameBufferOffsetX;
        float frameBufferY = frameBufferOffsetY;
        int lastX = windowX + (_registers.WindowSizeX >> 1);
        int lastY = windowY + (_registers.WindowSizeY >> 1);
        float frameBufferIncrementX = 1;
        float frameBufferIncrementY = 1;
        int mosaicX = (_registers.MosaicBlur >> 8) & 0xF;
        int mosaicY = (_registers.MosaicBlur >> 12) & 0xF;

        if (windowX < 0)
            windowX = 0;
        if (windowY < 0)
            windowY = 0;

        for (int y = windowY; y < lastY; y++)
        {
            frameBufferX = frameBufferOffsetX;
            for (int x = windowX; x < lastX; x++)
            {
                int sampleX = (int)frameBufferX & MosaicMasks[mosaicX];
                int sampleY = (int)frameBufferY & MosaicMasks[mosaicY];
                uint offset = (uint)(sampleY * FrameBufferWidth + sampleX);
                if (offset < FrameBufferWidth * FrameBufferHeight)
                {
                    uint pixel = _frameBuffer[offset];
                    if (x < FrameBufferWidth && y < FrameBufferHeight)
                        video.PutExternalBackgroundPixel(priority, x, y, pixel);
                }
                frameBufferX += frameBufferIncrementX;
            }
            frameBufferY += frameBufferIncrementY;
        }
    }

    public void SetVideoInterrupt(in SaturnVdp2Registers registers, SaturnVideoSoftware video)
    {
        assertTiocb?.Invoke(2);
        if ((registers.EXTEN & 1) != 0)
            Render(registers, video);
    }

    public void SetAllInterrupts(in SaturnVdp2Registers registers, SaturnVideoSoftware video)
    {
        SetAudioDataTransferInterrupt();
        SetVideoDataTransferInterrupt();
        SetVideoInterrupt(registers, video);
    }

    public void Initialize()
    {
        _registers = default;
        _ramPointer = 0;
        Array.Clear(_ram);
        Array.Clear(_frameBuffer);
        for (int y = 0; y < FrameBufferHeight; y++)
            for (int x = 0; x < FrameBufferWidth; x++)
                _frameBuffer[y * FrameBufferWidth + x] = 0xFF00FF00;
        for (int y = 0; y < 240; y++)
            for (int x = 8; x < 320; x++)
                _frameBuffer[y * FrameBufferWidth + x] = 0xFF0000FF;
        _registers.Reg34 = 1;
    }
}
