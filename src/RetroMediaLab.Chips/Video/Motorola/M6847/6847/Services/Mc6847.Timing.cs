namespace RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Services;

public sealed partial class Mc6847
{
    public void SetClock(bool level)
    {
        if (_timing.Clock == level) return;
        var previousLine = _timing.Scanline;
        var previousField = _timing.Field;
        var previousActive = _timing.ActiveLine;
        _timing.SetClock(level);
        if (_timing.Scanline != previousLine)
        {
            if (previousActive) _address.EndActiveLine();
            if (_timing.Field != previousField) _address.BeginField();
            _address.BeginLine();
            ResetMemoryLine();
        }
        else if (_displayValid)
        {
            _cellDot++;
        }
        ClockMemory();
        UpdateOutput();
    }
}
