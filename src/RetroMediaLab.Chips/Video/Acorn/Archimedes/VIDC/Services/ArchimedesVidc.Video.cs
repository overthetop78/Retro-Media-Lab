using C = RetroMediaLab.Chips.Video.Acorn.Archimedes.VIDC.Constants.ArchimedesVidcConstants;

namespace RetroMediaLab.Chips.Video.Acorn.Archimedes.VIDC.Services;

public sealed partial class ArchimedesVidc
{
    private byte[] _drawing = [];
    private byte[] _completed = [];
    private int _drawingWidth;
    private int _drawingHeight;
    private int _completedWidth;
    private int _completedHeight;
    private int _x;
    private int _line;
    private int _fieldPixels;
    private int _fieldParity;
    private uint _videoWord;
    private int _videoBits;
    private uint _cursorWord;
    private int _cursorBits;
    private int _videoWordsFetched;
    private int _videoWordsTaken;

    public byte[] CompletedFrame => _completed;
    public int Width => _completedWidth;
    public int Height => _completedHeight;

    private void PixelClock()
    {
        if (_fieldPixels == 0)
        {
            _cursorFifo.Clear();
            CursorDmaRequest = false;
            CursorFrameStarted?.Invoke();
        }
        if (_x == 0) BeginRaster();
        if (_x == HorizontalSyncEnd)
        {
            RequestVideoData();
            if (CursorRaster && _cursorFifo.Count < C.DmaWords / 2) CursorDmaRequest = true;
        }
        var border = _x >= HorizontalBorderStart && _x < HorizontalBorderEnd &&
            _line >= VerticalBorderStart && _line < VerticalBorderEnd;
        var active = DisplayRaster && _x >= HorizontalDisplayStart && _x < HorizontalDisplayEnd;
        var color = active ? NextVideoColor() : _palette[C.BorderIndex];
        var cursorPen = NextCursorPixel();
        if (border)
        {
            if (cursorPen != 0) color = _palette[C.BorderIndex + cursorPen];
            DrawPixel(color, cursorPen);
        }
        _x++;
        _fieldPixels++;
        if (_x >= HorizontalPeriod) { _x = 0; _line++; }
        if (_fieldPixels < FieldLength) return;
        _fieldPixels = 0;
        _line = 0;
        if (!Interlaced || _fieldParity != 0)
        {
            (_drawing, _completed) = (_completed, _drawing);
            _completedWidth = _drawingWidth;
            _completedHeight = _drawingHeight;
            Frame++;
            PrepareSurface();
        }
        _fieldParity = Interlaced ? _fieldParity ^ 1 : 0;
    }

    private void PrepareSurface()
    {
        var width = HorizontalBorderEnd - HorizontalBorderStart;
        var height = VerticalBorderEnd - VerticalBorderStart;
        _drawingWidth = width > 0 ? width * (HighResolutionMonochrome ? C.PixelBytes : 1) : C.DefaultWidth;
        _drawingHeight = height > 0 ? height * (Interlaced ? C.HorizontalUnits : 1) : C.DefaultHeight;
        var length = _drawingWidth * _drawingHeight * C.PixelBytes;
        if (_drawing.Length != length) _drawing = new byte[length];
        else Array.Clear(_drawing);
    }

    private void BeginRaster()
    {
        _videoFifo.Clear();
        _videoBits = _cursorBits = _videoWordsFetched = _videoWordsTaken = 0;
        VideoDmaRequest = false;
        if (_line == VerticalDisplayStart)
        {
            VideoFrameStarted?.Invoke();
            ChangeFlyback(false);
        }
        if (_line == VerticalDisplayEnd) ChangeFlyback(true);
    }

    private void ChangeFlyback(bool high)
    {
        if (Flyback == high) return;
        Flyback = high;
        FlybackChanged?.Invoke(high);
    }

    private void RequestVideoData()
    {
        if (DisplayRaster && _videoWordsFetched < VideoWordsPerRaster && _videoFifo.Count <= C.VideoFifoWords - C.DmaWords)
            VideoDmaRequest = true;
    }

    private int NextVideoColor()
    {
        if (_videoBits == 0)
        {
            _videoFifo.TryTake(out _videoWord);
            _videoBits = C.WordBits;
            _videoWordsTaken++;
            if ((_videoWordsTaken & C.DmaRequestMask) == DmaTrigger) RequestVideoData();
        }
        var pen = (int)(_videoWord & PixelMask);
        _videoWord >>= BitsPerPixel;
        _videoBits -= BitsPerPixel;
        return BitsPerPixel == C.ByteBits ? _extendedPalette[pen] : _palette[pen];
    }

    private int NextCursorPixel()
    {
        if (!CursorRaster || _x < CursorStart || _x >= CursorStart + C.CursorPixels) return 0;
        if (_cursorBits == 0)
        {
            _cursorFifo.TryTake(out _cursorWord);
            _cursorBits = C.WordBits;
        }
        var pen = (int)(_cursorWord & ((1 << C.CursorBits) - 1));
        _cursorWord >>= C.CursorBits;
        _cursorBits -= C.CursorBits;
        return pen;
    }

    private void DrawPixel(int color, int cursorPen)
    {
        var x = _x - HorizontalBorderStart;
        var y = _line - VerticalBorderStart;
        if (Interlaced) y = y * C.HorizontalUnits + _fieldParity;
        var scale = HighResolutionMonochrome ? C.PixelBytes : 1;
        x *= scale;
        if (x < 0 || x + scale > _drawingWidth || y < 0 || y >= _drawingHeight) return;
        for (var subpixel = 0; subpixel < scale; subpixel++)
        {
            var rgb = color;
            if (HighResolutionMonochrome)
            {
                if (cursorPen != 0) rgb = cursorPen == C.CursorWhitePen ? C.WhiteRgb : 0;
                else rgb = (((color >> C.BlueOutputShift) / C.PaletteScale >> subpixel) & 1) != 0 ? C.WhiteRgb : 0;
            }
            var index = (y * _drawingWidth + x + subpixel) * C.PixelBytes;
            _drawing[index] = (byte)rgb;
            _drawing[index + 1] = (byte)(rgb >> C.GreenOutputShift);
            _drawing[index + 2] = (byte)(rgb >> C.BlueOutputShift);
            _drawing[index + 3] = byte.MaxValue;
        }
    }
}
