using RetroMediaLab.Chips.Logic.Common.Contracts;
using RetroMediaLab.Chips.Logic.Common.Services;
using T = RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Models.Tms9918Timing;

namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Services;

public sealed partial class Tms9918A
{
    private RationalClockInput _masterInput;
    private bool _hasMasterSource, _drivingMasterSource;
    private long _masterBoundary = long.MaxValue;
    private long? _announcedCpuBoundary;
    private long? _irqPrediction;
    private long _framePredictionNumber = -1, _framePredictionTime;

    private void InvalidateClockPredictions()
    {
        _irqPrediction = null;
        _framePredictionNumber = -1;
    }

    public Action<RationalClockSource?>? CpuClockSourceChanged { get; set; }
    public Action<long?>? NextCpuEventNanosecondsChanged { get; set; }
    public RationalClockSource? CpuClockSource => !_hasMasterSource ? null
        : _masterInput.Divide(3, 2 - _clock.HalfEdgePhase % 3, !_clock.CpuClockHigh);

    private int HalfEdgesToPixel => (_clock.PixelClockHigh ? 4 : 2) - _clock.HalfEdgePhase % 2;

    public long? NextFrameBoundaryNanoseconds
    {
        get
        {
            if (!_hasMasterSource || _reset) return null;
            if (_framePredictionNumber != _raster.Frame)
            {
                _framePredictionTime = _masterInput.TimeAfterEdges(HalfEdgesToPixel - 1
                    + 4 * (T.DotsPerLine * (T.LinesPerFrame - _raster.Line) - _raster.Dot - 1));
                _framePredictionNumber = _raster.Frame;
            }
            return _framePredictionTime;
        }
    }

    public long? NextCpuEventNanoseconds
    {
        get
        {
            long? next = _dataDue;
            if (!_hasMasterSource || _reset) return next;
            int pixels;
            if (_externalVideo.SyncActive) pixels = 0;
            else if (!_registers.InterruptEnabled || (_status.Value & 0x80) != 0)
            { _irqPrediction = null; return next; }
            else
            {
                if (_irqPrediction is { } cached) return Earliest(next, cached);
                pixels = T.ActiveLines * T.DotsPerLine - (_raster.Line * T.DotsPerLine + _raster.Dot);
                if (pixels < 0) pixels += T.LinesPerFrame * T.DotsPerLine;
            }
            long date = _masterInput.TimeAfterEdges(HalfEdgesToPixel - 1 + 4 * pixels);
            if (!_externalVideo.SyncActive) _irqPrediction = date;
            return Earliest(next, date);
        }
    }

    public void ReceiveMasterClockSource(RationalClockSource source)
    {
        if (_publishing || _deliveringClock || _drivingMasterSource)
            throw new InvalidOperationException("Recursive master clock source.");
        var input = new RationalClockInput(source);
        ArgumentOutOfRangeException.ThrowIfLessThan(input.NextTime, TimeNanoseconds);
        if (input.NextHigh == _clock.MasterHigh)
            throw new ArgumentException("The first received edge must change the master clock.", nameof(source));
        _ = input.Divide(3, 2 - _clock.HalfEdgePhase % 3, !_clock.CpuClockHigh);
        _masterInput = input;
        _hasMasterSource = true;
        InvalidateClockPredictions();
        CpuClockSourceChanged?.Invoke(CpuClockSource);
        PublishCpuBoundary();
    }

    private void DetachMasterClockSource()
    {
        if (!_hasMasterSource) return;
        _hasMasterSource = false;
        InvalidateClockPredictions();
        CpuClockSourceChanged?.Invoke(null);
        PublishCpuBoundary();
    }

    public void LimitMasterClockSourceBefore(long boundaryExclusive)
    {
        if (!_drivingMasterSource) return;
        ArgumentOutOfRangeException.ThrowIfLessThan(boundaryExclusive, TimeNanoseconds);
        _masterBoundary = Math.Min(_masterBoundary, boundaryExclusive);
    }

    public int AdvanceMasterClockSourceUntil(long boundaryExclusive)
    {
        if (!_hasMasterSource) throw new InvalidOperationException("No master clock source is connected.");
        if (_drivingMasterSource || _publishing || _deliveringClock)
            throw new InvalidOperationException("Recursive master clock advance.");
        ArgumentOutOfRangeException.ThrowIfLessThan(boundaryExclusive, TimeNanoseconds);
        int consumed = 0;
        _masterBoundary = boundaryExclusive;
        _drivingMasterSource = true;
        try
        {
            while (_masterInput.NextTime < _masterBoundary && consumed < int.MaxValue)
            {
                int count = Math.Min(HalfEdgesToPixel, int.MaxValue - consumed);
                if (CpuClockChanged is not null) count = Math.Min(count, 3 - _clock.HalfEdgePhase % 3);
                if (GromClockChanged is not null) count = Math.Min(count, 24 - _clock.GromHalfEdgePhase % 24);
                if (_ramLoadActive) count = (int)Math.Min(count, Math.Max(1, _ramLoadHalfEdge - _clock.HalfEdges));
                count = Math.Min(count, _masterInput.EdgesBefore(_masterBoundary));
                long date = _masterInput.TimeAfterEdges(count - 1);
                if (NextEventNanoseconds is { } due && due <= date)
                {
                    count = Math.Min(count, _masterInput.EdgesBefore(due));
                    if (count == 0)
                    {
                        if (!AdvanceClockEventsTo(due)) break;
                        continue;
                    }
                    date = _masterInput.TimeAfterEdges(count - 1);
                }
                if (!AdvanceClockEventsTo(date) || date >= _masterBoundary) break;
                _ = checked(_clock.HalfEdges + count);
                _masterInput.Advance(count);
                AdvanceMasterHalfEdges(count);
                consumed += count;
            }
        }
        finally { _drivingMasterSource = false; _masterBoundary = long.MaxValue; }
        return consumed;
    }

    private void PublishCpuBoundary()
    {
        long? next = NextCpuEventNanoseconds;
        if (_announcedCpuBoundary == next) return;
        _announcedCpuBoundary = next;
        NextCpuEventNanosecondsChanged?.Invoke(next);
    }
}
