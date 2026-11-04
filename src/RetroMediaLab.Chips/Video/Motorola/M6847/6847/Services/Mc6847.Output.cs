using RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Enums;
using C = RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Constants.Mc6847Constants;

namespace RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Services;

public sealed partial class Mc6847
{
    public Mc6847Colour Colour { get; private set; }
    public int LuminanceMillivolts { get; private set; }
    public int ChromaAMillivolts { get; private set; }
    public int ChromaBMillivolts { get; private set; }
    public int ChromaBiasMillivolts => C.ChromaBiasMillivolts;

    private void UpdateOutput()
    {
        var graphics = (_mode & Mc6847Mode.Graphics) != 0;
        var colourSet = (_mode & Mc6847Mode.ColourSet) != 0;
        Colour = Active && _displayValid && _cellDot < _pixels.PixelCount
            ? _pixels.GetPixel(_cellDot)
            : graphics ? colourSet ? Mc6847Colour.Buff : Mc6847Colour.Green : Mc6847Colour.Black;
        var verticalBlank = _timing.Scanline < C.VerticalBlankLines || _timing.Scanline >= C.RetraceLineStart;
        var horizontalBlank = _timing.HalfClock < C.BorderHalfClockStart;
        var serration = _timing.Scanline >= C.VerticalSyncLineStart;
        var equalising = _timing.Scanline < C.EqualisingLines || (_timing.Scanline >= C.RetraceLineStart && !serration);
        var sync = serration ? _timing.HalfClock < C.HalfClocksPerLine - C.HorizontalSyncEnd
            : equalising ? _timing.HalfClock < C.EqualisingPulseHalfClocks : !HorizontalSync;
        if (verticalBlank || horizontalBlank)
        {
            LuminanceMillivolts = sync ? C.LuminanceSyncMillivolts : C.LuminanceBlankMillivolts;
            ChromaAMillivolts = ChromaBMillivolts = C.ChromaBiasMillivolts;
        }
        else SetColourLevels(Colour);
        var burstSuppressed = graphics && colourSet && (_mode & Mc6847Mode.GraphicsMode0) != 0;
        if (!verticalBlank && !burstSuppressed && _timing.HalfClock >= C.BurstHalfClockStart && _timing.HalfClock < C.BurstHalfClockEnd)
            ChromaBMillivolts = C.ChromaBurstMillivolts;
    }

    private void SetColourLevels(Mc6847Colour colour)
    {
        LuminanceMillivolts = colour switch
        {
            Mc6847Colour.Black or Mc6847Colour.DarkGreen or Mc6847Colour.DarkOrange => C.LuminanceBlackMillivolts,
            Mc6847Colour.Yellow or Mc6847Colour.Buff => C.LuminanceHighMillivolts,
            Mc6847Colour.Blue or Mc6847Colour.Red => C.LuminanceLowMillivolts,
            _ => C.LuminanceMediumMillivolts
        };
        ChromaAMillivolts = colour switch
        {
            Mc6847Colour.Green or Mc6847Colour.DarkGreen or Mc6847Colour.Cyan => C.ChromaLowMillivolts,
            Mc6847Colour.Red or Mc6847Colour.Magenta or Mc6847Colour.Orange or Mc6847Colour.BrightOrange or Mc6847Colour.DarkOrange => C.ChromaHighMillivolts,
            _ => C.ChromaBiasMillivolts
        };
        ChromaBMillivolts = colour switch
        {
            Mc6847Colour.Green or Mc6847Colour.DarkGreen or Mc6847Colour.Yellow or Mc6847Colour.Orange or Mc6847Colour.BrightOrange or Mc6847Colour.DarkOrange => C.ChromaLowMillivolts,
            Mc6847Colour.Blue or Mc6847Colour.Magenta => C.ChromaHighMillivolts,
            _ => C.ChromaBiasMillivolts
        };
    }
}
