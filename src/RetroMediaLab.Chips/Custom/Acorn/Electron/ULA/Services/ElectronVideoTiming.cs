using C = RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Constants.ElectronVideoTimingConstants;

namespace RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Services;

/// <summary>
/// Electron field counters and nominal digital sync. A picture contains two unequal fields;
/// the 20 ms RTC and vertical sync retain their phase across the field boundary.
/// </summary>
internal sealed class ElectronVideoTiming
{
    private int _line;
    private int _dot;

    private int _pictureClock;
    internal int PictureClock => _pictureClock;
    internal bool LongField => _pictureClock >= C.ShortFieldClocks;
    internal int FieldClock => _line * C.ClocksPerLine + _dot;
    internal int Line => _line;
    internal int Dot => _dot;
    private long _fields;
    internal long Fields => _fields;
    private bool _fieldEnded;
    internal bool FieldEnded => _fieldEnded;
    private bool _rtcEdge;
    internal bool RtcEdge => _rtcEdge;
    private bool _displayEndEdge;
    internal bool DisplayEndEdge => _displayEndEdge;
    internal bool HorizontalSync => _dot >= C.HorizontalSyncStart && _dot < C.HorizontalSyncEnd;
    internal bool CompositeSync
    {
        get
        {
            var phase = _pictureClock % C.RtcPeriod;
            return HorizontalSync || (phase >= C.VerticalSyncFirstEdge
                && phase < C.VerticalSyncFirstEdge + C.VerticalSyncLength);
        }
    }

    internal void Reset()
    {
        _line = _dot = _pictureClock = 0;
        _fields = 0;
        _fieldEnded = _rtcEdge = _displayEndEdge = false;
    }

    internal void Tick(bool spaced) => Advance(1, spaced);

    // All IRQ and field edges are divisible by eight. Batching the CPU reference period
    // preserves those edges without executing eight equivalent counter updates.
    internal void TickCpuClock(bool spaced) => Advance(C.MasterClocksPerCpuClock, spaced);

    private void Advance(int clocks, bool spaced)
    {
        var previous = _pictureClock;
        var next = previous + clocks;
        var displayEnd = spaced ? C.TextDisplayEnd : C.GraphicsDisplayEnd;
        _rtcEdge = (previous < C.RtcFirstEdge && next >= C.RtcFirstEdge)
            || (previous < C.RtcFirstEdge + C.RtcPeriod && next >= C.RtcFirstEdge + C.RtcPeriod);
        _displayEndEdge = (previous < displayEnd && next >= displayEnd)
            || (previous < displayEnd + C.ShortFieldClocks && next >= displayEnd + C.ShortFieldClocks);
        _fieldEnded = (previous < C.ShortFieldClocks && next >= C.ShortFieldClocks) || next >= C.PictureClocks;
        _dot += clocks;
        if (_dot >= C.ClocksPerLine)
        {
            _dot -= C.ClocksPerLine;
            _line++;
        }
        if (_fieldEnded)
        {
            _line = 0;
            _fields++;
        }
        _pictureClock = next >= C.PictureClocks ? next - C.PictureClocks : next;
    }

}
