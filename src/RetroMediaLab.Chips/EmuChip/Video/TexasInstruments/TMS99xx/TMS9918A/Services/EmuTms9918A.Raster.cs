using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;
using T = RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Models.Tms9918Timing;

namespace RetroMediaLab.Chips.EmuChip.Video.TexasInstruments.TMS99xx.TMS9918A.Services;

public sealed partial class EmuTms9918A
{
    private const byte ActivePixel = 1, HorizontalSyncPixel = 2, VerticalSyncPixel = 4,
        BlankingPixel = 8, BurstPixel = 16, KnownPixel = 32, ExternalPixel = 64;
    private static readonly byte[] GraphicsDotFlags = CreateDotFlags(false), TextDotFlags = CreateDotFlags(true),
        LineFlags = CreateLineFlags();
    private long _videoTime, _videoFrame;
    private int _videoDot, _videoLine;
    private byte _videoColour, _videoFlags;
    private double? _videoVoltage;
    private byte[]? _rasterColours;
    private bool[]? _rasterKnowledge;
    private long _rasterSamplesProduced, _rasterDirectWrites, _videoSignalMaterializations, _rasterScalarDeliveries;
    public long RasterSamplesProduced => _rasterSamplesProduced;
    public long RasterDirectWrites => _rasterDirectWrites;
    public long VideoSignalMaterializations => _videoSignalMaterializations;
    public long RasterScalarDeliveries => _rasterScalarDeliveries;
    public Action<long>? RasterCompleted { get; set; }

    /// <summary>Connects borrowed native colour-index and knowledge buffers.
    /// They remain owned by the receiver; disconnecting neither clears nor frees them.
    /// Every visible coordinate is written at its actual production time, including
    /// the text-mode side borders. No video or VRAM state is moved into the buffers.</summary>
    public void SetRasterOutput(byte[]? colours, bool[]? knowledge)
    {
        if (colours is null || knowledge is null)
        {
            if (colours is not null || knowledge is not null)
                throw new ArgumentException("Both raster buffers must be connected or disconnected together.");
        }
        else if (colours.Length != T.ActiveWidth * T.ActiveLines || knowledge.Length != colours.Length)
            throw new ArgumentException("Raster buffers must have the native 256 by 192 geometry.");
        _rasterColours = colours; _rasterKnowledge = knowledge;
    }

    private static byte[] CreateDotFlags(bool text)
    {
        var flags = new byte[T.DotsPerLine];
        int left = text ? T.TextLeft : 0, width = text ? T.TextWidth : T.ActiveWidth;
        for (int dot = 0; dot < flags.Length; dot++)
            flags[dot] = (byte)((dot >= left && dot < left + width ? ActivePixel : 0)
                | (dot is >= T.HorizontalSyncStart and < T.HorizontalSyncEnd ? HorizontalSyncPixel : 0)
                | (dot is >= T.HorizontalBlankStart and < T.HorizontalBlankEnd ? BlankingPixel : 0)
                | (dot is >= T.ColorBurstStart and < T.ColorBurstEnd ? BurstPixel : 0));
        return flags;
    }

    private static byte[] CreateLineFlags()
    {
        var flags = new byte[T.LinesPerFrame];
        for (int line = 0; line < flags.Length; line++)
            flags[line] = (byte)((line < T.ActiveLines ? ActivePixel : 0)
                | (line is >= T.VerticalSyncStart and < T.VerticalSyncEnd ? VerticalSyncPixel : 0)
                | (line is >= T.VerticalBlankStart and < T.VerticalBlankEnd ? BlankingPixel : 0));
        return flags;
    }

    private long _constantRasterRuns, _constantRasterPixels;
    public long ConstantRasterRuns => _constantRasterRuns;
    public long ConstantRasterPixels => _constantRasterPixels;

    private long _rasterProgramRuns, _rasterProgramPixels;
    public long RasterProgramRuns => _rasterProgramRuns;
    public long RasterProgramPixels => _rasterProgramPixels;

    private int RasterProgramPrefixPixels()
    {
        if (_reset || _externalVideo.SyncActive || _memory.HasPendingRequest || _cpuVramInService
            || _raster.Dot == 0 || VideoSignalProduced is not null
            || CpuClockChanged is not null || GromClockChanged is not null) return 0;
        int dot = _raster.Dot, line = _raster.Line;
        // During these two tail lines the ordinary schedule mixes refresh and
        // sprite preparation. Keep each selected slot explicit there.
        if (_registers.DisplayEnabled && line >= 260) return 0;
        bool refresh = !_registers.DisplayEnabled || line >= T.ActiveLines;
        int count = Tms9918VramSchedule.QuietPixelsAt(dot, _registers.Mode, refresh);
        if (count < 2) return 0;
        if (!_registers.DisplayEnabled) return count;
        count = Math.Min(count, 8);
        if (dot < 310) count = Math.Min(count, 310 - dot);
        int left = _registers.UsesTextTiming ? T.TextLeft : 0;
        int right = left + (_registers.UsesTextTiming ? T.TextWidth : T.ActiveWidth);
        if (line < T.ActiveLines && dot < right)
        {
            if (dot < left) count = Math.Min(count, left - dot);
            else
            {
                int width = _registers.UsesTextTiming ? 6 : 8;
                count = Math.Min(count, width - (dot - left) % width);
            }
        }
        return count;
    }

    private void ProduceRasterProgramPixels(int count)
    {
        int firstDot = _raster.Dot, line = _raster.Line, lastDot = firstDot + count - 1;
        var dots = _registers.UsesTextTiming ? TextDotFlags : GraphicsDotFlags;
        byte vertical = LineFlags[line];
        if (!_registers.DisplayEnabled)
        {
            byte horizontal = dots[lastDot];
            byte constantFlags = (byte)(((horizontal | vertical) & ~ActivePixel) | KnownPixel);
            if ((horizontal & vertical & ActivePixel) != 0) constantFlags |= ActivePixel;
            if ((vertical & VerticalSyncPixel) != 0) constantFlags &= unchecked((byte)~BurstPixel);
            bool blank = (constantFlags & BlankingPixel) != 0;
            byte constantColour = blank ? (byte)0 : _registers.BackdropColor;
            bool external = _registers.ExternalVideoEnabled && (blank || constantColour == 0);
            if (external) constantFlags |= ExternalPixel;
            _videoTime = _time; _videoDot = lastDot; _videoLine = line; _videoFrame = _raster.Frame;
            _videoColour = constantColour; _videoFlags = constantFlags;
            _videoVoltage = external ? ExternalOutputVoltage : null;
            _raster = new(lastDot + 1, line, _raster.Frame);
            _rasterSamplesProduced += count; _rasterProgramRuns++; _rasterProgramPixels += count;
            _constantRasterRuns++; _constantRasterPixels += count;
            if (line < T.ActiveLines && firstDot < T.ActiveWidth && _rasterColours is { } buffer)
            {
                int length = Math.Min(count, T.ActiveWidth - firstDot), offset = line * T.ActiveWidth + firstDot;
                Array.Fill(buffer, _registers.BackdropColor, offset, length);
                Array.Fill(_rasterKnowledge!, true, offset, length); _rasterDirectWrites += length;
            }
            return;
        }
        bool active = (dots[firstDot] & vertical & ActivePixel) != 0;
        ulong background = 0;
        byte backgroundKnown = (byte)((1 << count) - 1);
        if (active && _registers.DisplayEnabled)
        {
            int left = _registers.UsesTextTiming ? T.TextLeft : 0, width = _registers.UsesTextTiming ? 6 : 8;
            if ((firstDot - left) % width == 0) _background.BeginCell();
            (background, backgroundKnown) = _background.ShiftPixels(_registers, count);
        }
        var sprites = SpritePixels(firstDot, line, count);
        byte colour = 0, flags = 0;
        bool known = false;
        for (int offset = 0; offset < count; offset++)
        {
            int dot = firstDot + offset;
            byte horizontal = dots[dot];
            flags = (byte)((horizontal | vertical) & ~ActivePixel);
            if ((horizontal & vertical & ActivePixel) != 0) flags |= ActivePixel;
            if ((vertical & VerticalSyncPixel) != 0) flags &= unchecked((byte)~BurstPixel);
            bool blank = (flags & BlankingPixel) != 0;
            colour = blank ? (byte)0 : _registers.BackdropColor;
            known = blank || !active || !_registers.DisplayEnabled;
            if (active && _registers.DisplayEnabled)
            {
                colour = (byte)(background >> (offset * 8));
                known = (backgroundKnown & (1 << offset)) != 0;
                if (!_registers.UsesTextTiming)
                {
                    byte sprite = (byte)(sprites.Colours >> (offset * 8));
                    bool spriteKnown = (sprites.Known & (1 << offset)) != 0;
                    if (sprite != 0) { colour = sprite; known = spriteKnown; }
                    else known &= spriteKnown;
                }
            }
            bool external = _registers.ExternalVideoEnabled && (blank || (colour == 0 && known));
            if (known) flags |= KnownPixel;
            if (external) flags |= ExternalPixel;
            if (dot < T.ActiveWidth && line < T.ActiveLines && _rasterColours is { } target)
            {
                int address = line * T.ActiveWidth + dot;
                target[address] = colour; _rasterKnowledge![address] = known; _rasterDirectWrites++;
            }
        }
        _videoTime = _time; _videoDot = lastDot; _videoLine = line; _videoFrame = _raster.Frame;
        _videoColour = colour; _videoFlags = flags;
        _videoVoltage = (flags & ExternalPixel) != 0 ? ExternalOutputVoltage : null;
        _raster = new(lastDot + 1, line, _raster.Frame);
        _rasterSamplesProduced += count; _rasterProgramRuns++; _rasterProgramPixels += count;
        if (!_registers.DisplayEnabled) { _constantRasterRuns++; _constantRasterPixels += count; }
    }

    private void ProduceRasterPixel()
    {
        var current = _raster;
        int dot = current.Dot, line = current.Line;
        if (dot == 0) BeginSpriteLine(line);
        if ((dot & 1) == 0) SelectVramSlot(dot, line);
        bool text = _registers.UsesTextTiming;
        byte horizontal = (text ? TextDotFlags : GraphicsDotFlags)[dot], vertical = LineFlags[line];
        byte flags = (byte)((horizontal | vertical) & ~ActivePixel);
        bool active = (horizontal & vertical & ActivePixel) != 0;
        if (active) flags |= ActivePixel;
        if ((vertical & VerticalSyncPixel) != 0) flags &= unchecked((byte)~BurstPixel);
        bool blank = (flags & BlankingPixel) != 0;
        byte colour = blank ? (byte)0 : _registers.BackdropColor;
        bool known = blank || !active || !_registers.DisplayEnabled;
        if (active && _registers.DisplayEnabled)
        {
            int activeLeft = text ? T.TextLeft : 0, width = text ? 6 : 8;
            if ((dot - activeLeft) % width == 0) _background.BeginCell();
            var pixel = _background.ShiftPixel(_registers);
            colour = pixel.Colour; known = pixel.Known;
        }
        var sprite = SpritePixel(dot, line);
        if (active && _registers.DisplayEnabled && !text)
        {
            if (sprite.Colour != 0) { colour = sprite.Colour; known = sprite.Known; }
            else known &= sprite.Known;
        }
        bool external = _registers.ExternalVideoEnabled && (blank || (colour == 0 && known));
        if (known) flags |= KnownPixel;
        if (external) flags |= ExternalPixel;
        // These fields are the latest physical sample. The scalar contract is
        // reconstructed only when an observer actually requests it.
        _videoTime = _time; _videoDot = dot; _videoLine = line; _videoFrame = current.Frame;
        _videoColour = colour; _videoFlags = flags;
        double? voltage = external ? ExternalOutputVoltage : null;
        _videoVoltage = voltage;

        int nextDot = dot + 1, nextLine = line;
        long frame = current.Frame;
        if (nextDot == T.DotsPerLine)
        {
            nextDot = 0;
            if (++nextLine == T.LinesPerFrame) { nextLine = 0; frame = checked(frame + 1); }
        }
        _raster = new(nextDot, nextLine, frame);
        if (dot == 0 && line == T.ActiveLines) EndOfActiveDisplay();
        // A FrameStarted observer sees the new signal and raster but, just as
        // before, the receiver has not consumed this pixel yet.
        if (dot == 0 && line == 0 && FrameStarted is { } started)
        {
            bool publishing = _publishing; _publishing = true;
            try { started(_time); }
            finally { _publishing = publishing; }
        }
        _rasterSamplesProduced++;
        if (dot < T.ActiveWidth && line < T.ActiveLines && _rasterColours is { } target)
        {
            int offset = line * T.ActiveWidth + dot;
            target[offset] = colour; _rasterKnowledge![offset] = known;
            _rasterDirectWrites++;
        }
        if (dot == T.DotsPerLine - 1 && line == T.LinesPerFrame - 1 && RasterCompleted is { } completed)
        {
            bool publishing = _publishing; _publishing = true;
            try { completed(_time); }
            finally { _publishing = publishing; }
        }
        // No contract construction, callback, or publication guard in the
        // normal buffer-only delivery. Scalar observers retain exact dates.
        if (VideoSignalProduced is { } observer)
        {
            bool publishing = _publishing; _publishing = true;
            try
            {
                _rasterScalarDeliveries++; _videoSignalMaterializations++;
                observer(new(_time, dot, line, current.Frame, active,
                    (flags & HorizontalSyncPixel) != 0, (flags & VerticalSyncPixel) != 0,
                    blank, (flags & BurstPixel) != 0, colour, known, external, voltage));
            }
            finally { _publishing = publishing; }
        }
    }

    private void RepublishExternalRasterSignal()
    {
        _rasterSamplesProduced++;
        if (_videoDot is >= 0 and < T.ActiveWidth && _videoLine is >= 0 and < T.ActiveLines && _rasterColours is { } target)
        {
            int offset = _videoLine * T.ActiveWidth + _videoDot;
            target[offset] = _videoColour; _rasterKnowledge![offset] = (_videoFlags & KnownPixel) != 0;
            _rasterDirectWrites++;
        }
        if (_videoDot == T.DotsPerLine - 1 && _videoLine == T.LinesPerFrame - 1) RasterCompleted?.Invoke(_time);
        if (VideoSignalProduced is { } observer) { _rasterScalarDeliveries++; observer(VideoSignal); }
    }
}
