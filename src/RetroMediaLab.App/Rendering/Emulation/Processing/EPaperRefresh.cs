using RetroMediaLab.VideoPresentation.Contracts;
using RetroMediaLab.VideoPresentation.Enums;

namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal readonly record struct EPaperRefresh(bool CaptureHistory, bool HasPrevious, float CurrentWeight, float Inversion);

internal sealed class EPaperRefreshClock
{
    private EmulationEPaperVideoConfiguration? _panel;
    private (int Width, int Height) _size;
    private TimeSpan _timestamp;
    private TimeSpan _refreshStarted;
    private int _refreshCount;

    internal void Reset() => _panel = null;

    internal EPaperRefresh Advance(EmulationEPaperVideoConfiguration panel, int width, int height, TimeSpan timestamp)
    {
        if (_panel != panel || _size != (width, height) || timestamp < _timestamp)
        {
            _panel = panel;
            _size = (width, height);
            _timestamp = timestamp;
            _refreshStarted = timestamp;
            _refreshCount = 0;
            return new(true, false, 1f, 0f);
        }

        var (interval, ghost, fullEvery) = panel.ColorMode switch
        {
            EmulationEPaperColorMode.Monochrome => (270, .28f, 6),
            EmulationEPaperColorMode.Grayscale16 => (390, .34f, 6),
            EmulationEPaperColorMode.TricolorRed or EmulationEPaperColorMode.TricolorYellow => (720, .24f, 4),
            EmulationEPaperColorMode.Color4096 => (450, .31f, 6),
            _ => (960, .24f, 4)
        };
        _timestamp = timestamp;
        var elapsed = (timestamp - _refreshStarted).TotalMilliseconds;
        var captureHistory = elapsed >= interval;
        if (captureHistory)
        {
            _refreshStarted = timestamp;
            _refreshCount = _refreshCount % fullEvery + 1;
            elapsed = 0;
        }

        var fullRefresh = _refreshCount == fullEvery;
        const float inversionHalfDuration = 20f;
        var inversion = fullRefresh
            ? Math.Clamp(1f - Math.Abs((float)elapsed - inversionHalfDuration) / inversionHalfDuration, 0f, 1f)
            : 0f;
        // The live image has its own cadence. Only the retained pigment trace ages here.
        var fade = Math.Clamp(((float)elapsed / interval - .70f) / .30f, 0f, 1f);
        var residual = ghost * (1f - fade * fade * (3f - 2f * fade));
        if (fullRefresh)
        {
            var inversionDuration = inversionHalfDuration * 2f;
            residual *= Math.Clamp(((float)elapsed - inversionDuration) / inversionDuration, 0f, 1f);
        }
        return new(captureHistory, elapsed > 0, 1f - residual, inversion * .35f);
    }
}
