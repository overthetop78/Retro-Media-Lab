using RetroMediaLab.App.Constants.Rendering.Emulation;
using RetroMediaLab.App.Functions.Rendering.Emulation;
using RetroMediaLab.App.Interfaces.Rendering.Emulation;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;

namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal sealed partial class SoftwareEmulationVideoProcessingPipeline : IEmulationVideoProcessingPipeline
{
    public void ResetTemporalHistory()
    {
        _displayConnection.ResetHistory();
        _signalTimestamp = TimeSpan.Zero;
        _signalSequence = 0;
        ResetHistory();
        _plasmaPersistence.Reset();
        ResetVectorProcessing();
        ResetSegmentDisplayHistory();
        ResetEPaperHistory();
    }

    private void ApplyFixedPixelTemporal(EmulationVideoProcessingConfiguration configuration, float[] colors, int width, int height, TimeSpan timestamp)
    {
        if (!EmulationVideoProcessingConfigurationFunctions.UsesLcdPanel(configuration))
        {
            ResetHistory();
            return;
        }

        var compatibleHistory = _fixedPixelHistory is not null && _historyWidth == width && _historyHeight == height && timestamp >= _historyTimestamp;
        if (compatibleHistory)
        {
            var elapsedMilliseconds = Math.Max(0.001, (timestamp - _historyTimestamp).TotalMilliseconds);
            if (EmulationVideoProcessingConfigurationFunctions.UsesLcdPanel(configuration))
            {
                var panel = EmulationVideoProcessingConfigurationFunctions.LcdPanel(configuration).PanelType;
                var response = FilterFixedPixelResponse.BlendFactor(FilterLcdDisplay.ResponseTime(panel), elapsedMilliseconds);
                for (var index = 0; index < colors.Length; index += 3)
                {
                    if (panel == EmulationLcdPanelType.Va)
                    {
                        var previous = MathF.Max(_fixedPixelHistory![index], MathF.Max(_fixedPixelHistory[index + 1], _fixedPixelHistory[index + 2]));
                        var current = MathF.Max(colors[index], MathF.Max(colors[index + 1], colors[index + 2]));
                        response = FilterFixedPixelResponse.BlendFactor(FilterLcdDisplay.ResponseTime(panel, previous, current), elapsedMilliseconds);
                    }
                    for (var channel = index; channel < index + 3; channel++)
                        colors[channel] = FilterFixedPixelResponse.Apply(_fixedPixelHistory![channel], colors[channel], response);
                }
            }
            else
            {
                var response = FilterFixedPixelResponse.BlendFactor(configuration.FixedPixel.ResponseTimeMilliseconds, elapsedMilliseconds);
                for (var index = 0; index < colors.Length; index++)
                {
                    var responded = FilterFixedPixelResponse.Apply(_fixedPixelHistory![index], colors[index], response);
                    colors[index] = FilterFixedPixelPersistence.Apply(responded, _fixedPixelHistory[index], configuration.FixedPixel.PersistenceIntensity);
                }
            }
        }

        _fixedPixelHistory = colors.ToArray();
        _historyWidth = width;
        _historyHeight = height;
        _historyTimestamp = timestamp;
    }

    private void ResetHistory()
    {
        _fixedPixelHistory = null;
        _historyWidth = 0;
        _historyHeight = 0;
        _historyTimestamp = TimeSpan.Zero;
    }

    private void ApplySegmentDisplayTemporal(EmulationVideoProcessingConfiguration configuration, float[] colors, int width, int height, TimeSpan timestamp)
    {
        if (configuration.DisplayTechnology != EmulationVideoDisplayTechnology.SegmentDisplay)
        {
            ResetSegmentDisplayHistory();
            return;
        }
        var compatibleHistory = _segmentDisplayHistory is not null && _segmentDisplayHistoryWidth == width && _segmentDisplayHistoryHeight == height && timestamp >= _segmentDisplayHistoryTimestamp;
        if (compatibleHistory)
        {
            var elapsedMilliseconds = Math.Max(0.001, (timestamp - _segmentDisplayHistoryTimestamp).TotalMilliseconds);
            var response = FilterSegmentDisplayResponse.BlendFactor(configuration.SegmentDisplay.ResponseTimeMilliseconds, elapsedMilliseconds);
            var persistence = FilterSegmentDisplayPersistence.Decay(configuration.SegmentDisplay.PersistenceMilliseconds, elapsedMilliseconds);
            for (var index = 0; index < colors.Length; index++)
            {
                var previous = _segmentDisplayHistory![index];
                var target = colors[index];
                colors[index] = target >= previous ? Lerp(previous, target, response) : MathF.Max(target, previous * persistence);
            }
        }
        _segmentDisplayHistory = colors.ToArray();
        _segmentDisplayHistoryWidth = width;
        _segmentDisplayHistoryHeight = height;
        _segmentDisplayHistoryTimestamp = timestamp;
    }

    private void ResetSegmentDisplayHistory()
    {
        _segmentDisplayHistory = null;
        _segmentDisplayHistoryWidth = 0;
        _segmentDisplayHistoryHeight = 0;
        _segmentDisplayHistoryTimestamp = TimeSpan.Zero;
    }

    private void ApplyEPaperTemporal(EmulationVideoProcessingConfiguration configuration, float[] colors, int width, int height, TimeSpan timestamp)
    {
        if (configuration.DisplayTechnology != EmulationVideoDisplayTechnology.EPaper)
        {
            ResetEPaperHistory();
            return;
        }

        if (_ePaperAdjustments != configuration.Adjustments)
        {
            _ePaperClock.Reset();
            _ePaperAdjustments = configuration.Adjustments;
        }

        var output = colors.AsSpan(0, checked(width * height * 3));
        var refresh = _ePaperClock.Advance(configuration.EPaper, width, height, timestamp);
        if (refresh.CaptureHistory)
        {
            if (_ePaperHistory.Length != output.Length)
                _ePaperHistory = new float[output.Length];
            output.CopyTo(_ePaperHistory);
        }
        if (!refresh.HasPrevious)
            return;

        var previous = _ePaperHistory;
        var surface = FilterEPaper.Surface(configuration.EPaper.ColorMode);
        for (var i = 0; i < output.Length; i += 3)
        {
            var dr = output[i] - previous[i];
            var dg = output[i + 1] - previous[i + 1];
            var db = output[i + 2] - previous[i + 2];
            var invert = dr * dr + dg * dg + db * db > .0001f ? refresh.Inversion : 0f;
            output[i] = RefreshPigment(previous[i], output[i], refresh.CurrentWeight, invert, surface.X + surface.W);
            output[i + 1] = RefreshPigment(previous[i + 1], output[i + 1], refresh.CurrentWeight, invert, surface.Y + surface.W);
            output[i + 2] = RefreshPigment(previous[i + 2], output[i + 2], refresh.CurrentWeight, invert, surface.Z + surface.W);
        }
    }

    private static float RefreshPigment(float old, float current, float progress, float inversion, float paper)
    {
        var pigment = Lerp(old, current, progress);
        return Lerp(pigment, Math.Clamp(paper - pigment, 0f, 1f), inversion);
    }

    private void ResetEPaperHistory() => _ePaperClock.Reset();

}
