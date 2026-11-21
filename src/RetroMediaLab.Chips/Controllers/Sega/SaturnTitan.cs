// Copyright 2012 Guillaume Duhamel
// Translated from Yabause titan/titan.c.
// SPDX-License-Identifier: GPL-2.0-or-later

using System.Runtime.ExceptionServices;

namespace RetroMediaLab.Chips.Controllers.Sega;

internal sealed class SaturnTitan : IDisposable
{
    private struct PixelData
    {
        internal uint Pixel;
        internal byte Priority;
        internal byte LineScreen;
        internal SaturnTitanShadow ShadowType;
        internal bool ShadowEnabled;
    }

    private readonly PixelData[][] _layerBuffers = new PixelData[6][];
    private readonly uint[][] _lineScreens = new uint[4][];
    private readonly PixelData[] _backScreen = new PixelData[704 * 512];
    private readonly int[] _layerPriority = new int[6];
    private readonly object _priorityLock = new();
    private readonly Thread?[] _priorityThreads = new Thread?[5];
    private readonly AutoResetEvent?[] _priorityWakeEvents = new AutoResetEvent?[5];
    private readonly bool[] _priorityDrawFinished = new bool[5];
    private readonly (int Start, int End)[] _priorityLines = new (int, int)[5];
    private uint[]? _priorityDisplay;
    private ExceptionDispatchInfo? _priorityFailure;
    private ushort _prioritySpriteControl;
    private bool _prioritySimplified, _priorityInterlace, _priorityOddFrame, _priorityStopping;
    private int _numPriorityThreads;
    private Func<uint, uint, uint>? _blend;
    private Func<uint, bool>? _transparency;
    private int _width = 320;
    private int _height = 224;

    internal SaturnTitan()
    {
        for (int index = 0; index < 6; index++)
            _layerBuffers[index] = new PixelData[704 * 256];
        for (int index = 1; index < 4; index++)
            _lineScreens[index] = new uint[512];
        Array.Fill(_priorityDrawFinished, true);

        bool initialized = false;
        try
        {
            for (int index = 0; index < _priorityThreads.Length; index++)
            {
                int workerIndex = index;
                _priorityWakeEvents[index] = new AutoResetEvent(false);
                var thread = new Thread(() => VidsoftPriorityThread(workerIndex)) { IsBackground = true };
                thread.Start();
                _priorityThreads[index] = thread;
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
            lock (_priorityLock)
            {
                _priorityStopping = true;
                foreach (AutoResetEvent? wakeEvent in _priorityWakeEvents)
                    wakeEvent?.Set();
            }
        }
        finally
        {
            try
            {
                foreach (Thread? thread in _priorityThreads)
                    thread?.Join();
            }
            finally
            {
                for (int index = 0; index < _priorityWakeEvents.Length; index++)
                {
                    _priorityWakeEvents[index]?.Dispose();
                    _priorityWakeEvents[index] = null;
                }
                _priorityDisplay = null;
            }
        }
    }

    private static uint FixAlpha(uint pixel) => BitConverter.IsLittleEndian
        ? (((pixel & 0x3F000000) << 2) + 0x03000000) | (pixel & 0x00FFFFFF)
        : (((pixel & 0x3F) << 2) + 3) | (pixel & 0xFFFFFF00);

    private static int GetAlpha(uint pixel) => (int)((BitConverter.IsLittleEndian ? pixel >> 24 : pixel) & 0x3F);

    private static int GetRed(uint pixel) => (int)((pixel >> (BitConverter.IsLittleEndian ? 16 : 8)) & 0xFF);

    private static int GetGreen(uint pixel) => (int)((pixel >> (BitConverter.IsLittleEndian ? 8 : 16)) & 0xFF);

    private static int GetBlue(uint pixel) => (int)((BitConverter.IsLittleEndian ? pixel : pixel >> 24) & 0xFF);

    private static uint CreatePixel(int alpha, int red, int green, int blue) => BitConverter.IsLittleEndian
        ? ((uint)alpha << 24) | ((uint)red << 16) | ((uint)green << 8) | (uint)blue
        : (uint)alpha | ((uint)red << 8) | ((uint)green << 16) | ((uint)blue << 24);

    private void RenderLinesSimplified(Span<uint> display, int startLine, int endLine, bool interlace, bool oddFrame)
    {
        SaturnVideoSoftware.GetInterlaceInfo(interlace, oddFrame, out int interlaceLine, out int lineIncrement);
        Span<int> sortedLayers = stackalloc int[8];
        int layerCount = 0;

        for (int priority = 7; priority >= 0; priority--)
        {
            for (int layer = (int)SaturnTitanLayer.Rbg0; layer >= 0; layer--)
            {
                if (_layerPriority[layer] > 0 && _layerPriority[layer] == priority)
                    sortedLayers[layerCount++] = layer;
            }
        }
        sortedLayers[layerCount++] = (int)SaturnTitanLayer.Back;

        int layerY = interlace ? startLine / 2 : startLine;
        for (int y = startLine + interlaceLine; y < endLine; y += lineIncrement)
        {
            for (int x = 0; x < _width; x++)
            {
                int layerPosition = layerY * _width + x;
                int position = y * _width + x;
                display[position] = 0;

                for (int index = 0; index < layerCount; index++)
                {
                    PixelData sprite = _layerBuffers[(int)SaturnTitanLayer.Sprite][layerPosition];
                    int background = sortedLayers[index];
                    if (background == (int)SaturnTitanLayer.Back)
                    {
                        if (sprite.Pixel != 0)
                            display[position] = FixAlpha(sprite.Pixel);
                        else
                            display[position] = FixAlpha(_backScreen[y].Pixel);
                        break;
                    }
                    else if (sprite.Priority >= _layerPriority[background])
                    {
                        if (sprite.Pixel != 0)
                        {
                            display[position] = FixAlpha(sprite.Pixel);
                            break;
                        }
                    }
                    else if (_layerBuffers[background][layerPosition].Pixel != 0)
                    {
                        display[position] = FixAlpha(_layerBuffers[background][layerPosition].Pixel);
                        break;
                    }
                }
            }
            layerY++;
        }
    }

    private static uint BlendPixelsTop(uint top, uint bottom)
    {
        int alpha = (GetAlpha(top) << 2) + 3;
        int reverseAlpha = 0xFF - alpha;
        int tr = GetRed(top) * alpha / 0xFF;
        int tg = GetGreen(top) * alpha / 0xFF;
        int tb = GetBlue(top) * alpha / 0xFF;
        int br = GetRed(bottom) * reverseAlpha / 0xFF;
        int bg = GetGreen(bottom) * reverseAlpha / 0xFF;
        int bb = GetBlue(bottom) * reverseAlpha / 0xFF;
        return CreatePixel(0x3F, tr + br, tg + bg, tb + bb);
    }

    private static uint BlendPixelsBottom(uint top, uint bottom)
    {
        if ((top & 0x80000000) == 0)
            return top;

        int alpha = (GetAlpha(bottom) << 2) + 3;
        int reverseAlpha = 0xFF - alpha;
        int tr = GetRed(top) * alpha / 0xFF;
        int tg = GetGreen(top) * alpha / 0xFF;
        int tb = GetBlue(top) * alpha / 0xFF;
        int br = GetRed(bottom) * reverseAlpha / 0xFF;
        int bg = GetGreen(bottom) * reverseAlpha / 0xFF;
        int bb = GetBlue(bottom) * reverseAlpha / 0xFF;
        return CreatePixel(GetAlpha(top), tr + br, tg + bg, tb + bb);
    }

    private static uint BlendPixelsAdd(uint top, uint bottom)
    {
        int red = GetRed(top) + GetRed(bottom);
        if (red > 0xFF)
            red = 0xFF;
        int green = GetGreen(top) + GetGreen(bottom);
        if (green > 0xFF)
            green = 0xFF;
        int blue = GetBlue(top) + GetBlue(bottom);
        if (blue > 0xFF)
            blue = 0xFF;
        return CreatePixel(0x3F, red, green, blue);
    }

    private static bool TransAlpha(uint pixel) => GetAlpha(pixel) < 0x3F;

    private static bool TransBit(uint pixel) => (pixel & 0x80000000) != 0;

    private uint DigPixel(int position, int y, ushort spriteControl)
    {
        Span<PixelData> stack = stackalloc PixelData[2];
        stack.Clear();
        int stackPosition = 0;

        for (int priority = 7; priority > 0; priority--)
        {
            for (int layer = (int)SaturnTitanLayer.Sprite; layer >= 0; layer--)
            {
                if (_layerBuffers[layer][position].Priority == priority)
                {
                    stack[stackPosition++] = _layerBuffers[layer][position];
                    if (stackPosition == 2)
                        goto Finished;
                }
            }
        }
        stack[stackPosition] = _backScreen[position];

    Finished:
        ref PixelData top = ref stack[0];
        ref PixelData bottom = ref stack[1];
        if (top.LineScreen != 0)
            top.Pixel = _blend!(top.Pixel, _lineScreens[top.LineScreen][y]);

        if (top.ShadowType == SaturnTitanShadow.Msb && (top.Pixel & 0xFFFFFF) == 0)
        {
            if (bottom.ShadowEnabled)
                top.Pixel = BlendPixelsTop(0x20000000, bottom.Pixel);
            else
                top.Pixel = bottom.Pixel;
        }
        else if (top.ShadowType == SaturnTitanShadow.Msb && (top.Pixel & 0xFFFFFF) != 0)
        {
            if (_transparency!(top.Pixel))
                top.Pixel = _blend!(top.Pixel, bottom.Pixel);

            if ((spriteControl & 0x10) == 0)
                top.Pixel = BlendPixelsTop(0x20000000, top.Pixel);
        }
        else if (top.ShadowType == SaturnTitanShadow.Normal)
        {
            if (bottom.ShadowEnabled)
                top.Pixel = BlendPixelsTop(0x20000000, bottom.Pixel);
            else
                top.Pixel = bottom.Pixel;
        }
        else if (_transparency!(top.Pixel))
        {
            top.Pixel = _blend!(top.Pixel, bottom.Pixel);
        }
        return top.Pixel;
    }

    internal void Erase(bool interlace)
    {
        int height = _height;
        if (interlace)
            height /= 2;
        for (int layer = 0; layer < 6; layer++)
            Array.Clear(_layerBuffers[layer], 0, _width * height);
    }

    internal void SetResolution(int width, int height)
    {
        _width = width;
        _height = height;
    }

    internal void SetBlendingMode(SaturnTitanBlend mode)
    {
        if (mode == SaturnTitanBlend.Bottom)
        {
            _blend = BlendPixelsBottom;
            _transparency = TransBit;
        }
        else if (mode == SaturnTitanBlend.Add)
        {
            _blend = BlendPixelsAdd;
            _transparency = TransBit;
        }
        else
        {
            _blend = BlendPixelsTop;
            _transparency = TransAlpha;
        }
    }

    internal void PutBackHLine(int y, uint color)
    {
        int start = y * _width;
        for (int x = 0; x < _width; x++)
            _backScreen[start + x].Pixel = color;
    }

    internal void PutLineHLine(int lineScreen, int y, uint color)
    {
        if (lineScreen == 0)
            return;
        _lineScreens[lineScreen][y] = color;
    }

    internal void PutPixel(int priority, int x, int y, uint color, int lineScreen,
        SaturnTitanLayer layer, bool shadowEnabled, SaturnTitanShadow shadowType)
    {
        if (priority == 0)
            return;

        int position = y * _width + x;
        _layerBuffers[(int)layer][position].Pixel = color;
        _layerBuffers[(int)layer][position].Priority = (byte)priority;
        _layerBuffers[(int)layer][position].LineScreen = (byte)lineScreen;
        _layerBuffers[(int)layer][position].ShadowEnabled = shadowEnabled;
        _layerBuffers[(int)layer][position].ShadowType = shadowType;
    }

    private void RenderLines(Span<uint> display, int startLine, int endLine, ushort spriteControl, bool interlace, bool oddFrame)
    {
        SaturnVideoSoftware.GetInterlaceInfo(interlace, oddFrame, out int interlaceLine, out int lineIncrement);
        int layerY = interlace ? startLine / 2 : startLine;
        for (int y = startLine + interlaceLine; y < endLine; y += lineIncrement)
        {
            for (int x = 0; x < _width; x++)
            {
                int position = y * _width + x;
                int layerPosition = layerY * _width + x;
                display[position] = 0;
                uint dot = DigPixel(layerPosition, y, spriteControl);
                if (dot != 0)
                    display[position] = FixAlpha(dot);
            }
            layerY++;
        }
    }

    private void RenderSimplifiedCheck(Span<uint> display, int start, int end,
        bool simplified, ushort spriteControl, bool interlace, bool oddFrame)
    {
        if (simplified)
            RenderLinesSimplified(display, start, end, interlace, oddFrame);
        else
            RenderLines(display, start, end, spriteControl, interlace, oddFrame);
    }

    private void VidsoftPriorityThread(int index)
    {
        AutoResetEvent wakeEvent = _priorityWakeEvents[index]!;
        while (true)
        {
            wakeEvent.WaitOne();
            lock (_priorityLock)
            {
                if (_priorityStopping)
                {
                    Volatile.Write(ref _priorityDrawFinished[index], true);
                    Monitor.PulseAll(_priorityLock);
                    return;
                }
            }

            try
            {
                (int start, int end) = _priorityLines[index];
                RenderSimplifiedCheck(_priorityDisplay!, start, end, _prioritySimplified,
                    _prioritySpriteControl, _priorityInterlace, _priorityOddFrame);
            }
            catch (Exception exception)
            {
                lock (_priorityLock)
                    _priorityFailure ??= ExceptionDispatchInfo.Capture(exception);
            }
            finally
            {
                lock (_priorityLock)
                {
                    Volatile.Write(ref _priorityDrawFinished[index], true);
                    Monitor.PulseAll(_priorityLock);
                }
            }
        }
    }

    internal void SetNumPriorityThreads(int count)
    {
        _numPriorityThreads = count > 5 ? 5 : count;
        if (count == 2)
            _numPriorityThreads = 1;
        if (count == 4)
            _numPriorityThreads = 3;
    }

    private void StartPriorityThread(int index)
    {
        lock (_priorityLock)
        {
            Volatile.Write(ref _priorityDrawFinished[index], false);
            _priorityWakeEvents[index]!.Set();
        }
    }

    private void WaitForPriorityThread(int index)
    {
        if (!Volatile.Read(ref _priorityDrawFinished[index]))
        {
            lock (_priorityLock)
            {
                while (!_priorityDrawFinished[index])
                    Monitor.Wait(_priorityLock);
            }
        }
    }

    private void RenderThreads(uint[] display, bool simplified, ushort spriteControl, bool interlace, bool oddFrame)
    {
        int totalJobs = _numPriorityThreads + 1;
        int linesPerJob = _height / totalJobs;
        int remainder = _height % totalJobs;
        Span<int> starts = stackalloc int[6];
        Span<int> ends = stackalloc int[6];

        _priorityDisplay = display;
        _prioritySimplified = simplified;
        _prioritySpriteControl = spriteControl;
        _priorityInterlace = interlace;
        _priorityOddFrame = oddFrame;
        _priorityFailure = null;

        for (int index = 0; index < totalJobs; index++)
        {
            starts[index] = linesPerJob * index;
            ends[index] = (index + 1) * linesPerJob;
        }
        for (int index = 0; index < _numPriorityThreads; index++)
            _priorityLines[index] = (starts[index + 1], ends[index + 1]);
        _priorityLines[_numPriorityThreads - 1].End += remainder;

        try
        {
            for (int index = 0; index < _numPriorityThreads; index++)
                StartPriorityThread(index);

            RenderSimplifiedCheck(display, starts[0], ends[0], simplified, spriteControl, interlace, oddFrame);
        }
        finally
        {
            for (int index = 0; index < _numPriorityThreads; index++)
                WaitForPriorityThread(index);
            _priorityDisplay = null;
        }
        _priorityFailure?.Throw();
    }

    internal void Render(uint[] display, in SaturnVdp2Registers registers, bool interlace, bool oddFrame)
    {
        if (_transparency is null)
            return;

        bool simplified = true;
        if ((registers.CCCTL & 0x807F) != 0)
            simplified = false;
        if ((registers.SFPRMD & 0x3FF) != 0)
            simplified = false;
        if ((registers.LNCLEN & 0x1F) != 0)
            simplified = false;
        if ((registers.SDCTL & 0x13F) != 0)
            simplified = false;

        _layerPriority[(int)SaturnTitanLayer.Nbg0] = registers.PRINA & 7;
        _layerPriority[(int)SaturnTitanLayer.Nbg1] = (registers.PRINA >> 8) & 7;
        _layerPriority[(int)SaturnTitanLayer.Nbg2] = registers.PRINB & 7;
        _layerPriority[(int)SaturnTitanLayer.Nbg3] = (registers.PRINB >> 8) & 7;
        _layerPriority[(int)SaturnTitanLayer.Rbg0] = registers.PRIR & 7;
        if (_numPriorityThreads > 0)
            RenderThreads(display, simplified, registers.SPCTL, interlace, oddFrame);
        else
            RenderSimplifiedCheck(display, 0, _height, simplified, registers.SPCTL, interlace, oddFrame);
    }
}
