using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Enums;
using T = RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Models.Tms9918Timing;

namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Services;

public sealed partial class Tms9918A
{
    private void ProduceRasterPixel()
    {
        var current = _raster;
        int dot = current.Dot, line = current.Line;
        if (dot == 0) BeginSpriteLine(line);
        if ((dot & 1) == 0) SelectVramSlot(dot, line);
        bool text = _registers.UsesTextTiming;
        int activeLeft = text ? T.TextLeft : 0;
        int activeWidth = text ? T.TextWidth : T.ActiveWidth;
        bool active = line < T.ActiveLines && dot >= activeLeft && dot < activeLeft + activeWidth;
        bool verticalBlank = line is >= T.VerticalBlankStart and < T.VerticalBlankEnd;
        bool horizontalBlank = dot is >= T.HorizontalBlankStart and < T.HorizontalBlankEnd;
        bool blank = verticalBlank || horizontalBlank;
        bool verticalSync = line is >= T.VerticalSyncStart and < T.VerticalSyncEnd;
        bool horizontalSync = dot is >= T.HorizontalSyncStart and < T.HorizontalSyncEnd;
        bool burst = !verticalSync && dot is >= T.ColorBurstStart and < T.ColorBurstEnd;
        byte colour = blank ? (byte)0 : _registers.BackdropColor;
        bool known = blank || !active || !_registers.DisplayEnabled;
        if (active && _registers.DisplayEnabled)
        {
            int width = text ? 6 : 8;
            if ((dot - activeLeft) % width == 0) _background.BeginCell();
            var pixel = _background.ShiftPixel(_registers);
            colour = pixel.Colour;
            known = pixel.Known;
        }
        bool external = _registers.ExternalVideoEnabled && (blank || (colour == 0 && known));
        var sprite = SpritePixel(dot, line);
        if (active && _registers.DisplayEnabled && !text)
        {
            if (sprite.Colour != 0) { colour = sprite.Colour; known = sprite.Known; }
            else known &= sprite.Known;
            external = _registers.ExternalVideoEnabled && (blank || (colour == 0 && known));
        }
        var signal = new Tms9918VideoSignal(TimeNanoseconds, dot, line, current.Frame,
            active, horizontalSync, verticalSync, blank, burst, colour, known, external,
            external ? ExternalOutputVoltage : null);
        VideoSignal = signal;

        int nextDot = dot + 1, nextLine = line;
        long frame = current.Frame;
        if (nextDot == T.DotsPerLine)
        {
            nextDot = 0;
            if (++nextLine == T.LinesPerFrame) { nextLine = 0; frame = checked(frame + 1); }
        }
        _raster = new(nextDot, nextLine, frame);
        // Le premier pixel qui suit la zone active matérialise sa fin ; F
        // n'attend pas la fin de la présentation de l'image par l'application.
        if (dot == 0 && line == T.ActiveLines) EndOfActiveDisplay();
        bool wasPublishing = _publishing;
        _publishing = true;
        try
        {
            if (dot == 0 && line == 0) FrameStarted?.Invoke(TimeNanoseconds);
            VideoSignalProduced?.Invoke(signal);
        }
        finally { _publishing = wasPublishing; }
    }
}
