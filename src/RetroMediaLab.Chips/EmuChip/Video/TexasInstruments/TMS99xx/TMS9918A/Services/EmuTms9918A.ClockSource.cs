using RetroMediaLab.Chips.Logic.Common.Contracts;
using RetroMediaLab.Chips.EmuChip.Common.Services;
using T = RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Models.Tms9918Timing;

namespace RetroMediaLab.Chips.EmuChip.Video.TexasInstruments.TMS99xx.TMS9918A.Services;

public sealed partial class EmuTms9918A
{
    private EmuClockInput _masterInput;
    private bool _hasMasterSource, _drivingMasterSource;
    private long _masterBoundary = long.MaxValue;
    private long _clockInputBoundary = long.MaxValue;
    private long? _announcedCpuBoundary;
    private long? _irqPrediction;
    private long _framePredictionNumber = -1, _framePredictionTime;
    private long _masterSourceVisits;
    public long MasterSourceVisits => _masterSourceVisits;

    private void InvalidateClockPredictions()
    {
        _irqPrediction = null;
        _framePredictionNumber = -1;
    }

    public Action<RationalClockSource?>? CpuClockSourceChanged { get; set; }
    public Action<long?>? NextCpuEventNanosecondsChanged { get; set; }
    public Action<long>? ClockInputBoundaryReached { get; set; }

    /// <summary>A prediction received from the connected input wires, not an
    /// event queue or a clock owned by the VDP. Cancellation replaces it too.</summary>
    public void SetClockInputBoundary(long? timeNanoseconds)
    {
        if (timeNanoseconds is { } time)
            ArgumentOutOfRangeException.ThrowIfLessThan(time, _time);
        _clockInputBoundary = timeNanoseconds ?? long.MaxValue;
    }
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
            long? next = _dataDue < 0 ? null : _dataDue;
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
        var input = new EmuClockInput(source);
        ArgumentOutOfRangeException.ThrowIfLessThan(input.NextTime, _time);
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
        ArgumentOutOfRangeException.ThrowIfLessThan(boundaryExclusive, _time);
        _masterBoundary = Math.Min(_masterBoundary, boundaryExclusive);
    }

    public int AdvanceMasterClockSourceUntil(long boundaryExclusive)
    {
        if (!_hasMasterSource) throw new InvalidOperationException("No master clock source is connected.");
        if (_drivingMasterSource || _publishing || _deliveringClock)
            throw new InvalidOperationException("Recursive master clock advance.");
        ArgumentOutOfRangeException.ThrowIfLessThan(boundaryExclusive, _time);
        int consumed = 0;
        _masterBoundary = boundaryExclusive;
        _drivingMasterSource = true;
        try
        {
            while (consumed < int.MaxValue
                && (_masterInput.NextTime < _masterBoundary || _clockInputBoundary < _masterBoundary))
            {
                _masterSourceVisits++;
                if (_clockInputBoundary < _masterBoundary && _clockInputBoundary <= _masterInput.NextTime)
                {
                    if (ClockInputBoundaryReached is not { } deliver) break;
                    long inputDate = _clockInputBoundary;
                    deliver(inputDate);
                    if (_clockInputBoundary <= inputDate)
                        throw new InvalidOperationException("The connected input boundary was not consumed or advanced.");
                    continue;
                }
                long boundary = Math.Min(_masterBoundary, _clockInputBoundary);
                if (_nextDue >= 0 && _nextDue <= _masterInput.NextTime && _nextDue < boundary)
                {
                    if (!AdvanceClockEventsTo(_nextDue)) break;
                    continue;
                }
                if (_nextDue >= 0) boundary = Math.Min(boundary, _nextDue);
                int firstPixel = HalfEdgesToPixel;
                int possiblePixels = RasterProgramPrefixPixels();
                int maximum = possiblePixels > 1 ? firstPixel + 4 * (possiblePixels - 1) : firstPixel;
                // A RAM-load observer must see all earlier pixels, not only
                // the state preceding the entire grouped portion.
                if (possiblePixels > 1 && _ramLoadActive)
                    maximum = (int)Math.Min(maximum, Math.Max(0, _ramLoadHalfEdge - _clock.HalfEdges - 1));
                if (maximum < firstPixel + 4) { possiblePixels = 0; maximum = firstPixel; }
                int count = Math.Min(maximum, int.MaxValue - consumed);
                if (CpuClockChanged is not null) count = Math.Min(count, 3 - _clock.HalfEdgePhase % 3);
                if (GromClockChanged is not null) count = Math.Min(count, 24 - _clock.GromHalfEdgePhase % 24);
                if (_ramLoadActive) count = (int)Math.Min(count, Math.Max(1, _ramLoadHalfEdge - _clock.HalfEdges));
                if (_clock.HalfEdges > long.MaxValue - count)
                    count = Math.Min(count, _masterInput.EdgesBefore(boundary));
                _ = checked(_clock.HalfEdges + count);
                // Preserve the source cursor only for a potential multi-pixel
                // portion. Its final cut can end between two pixel edges.
                EmuClockInput pixelInput = possiblePixels > 1 ? _masterInput : default;
                count = _masterInput.ConsumeBefore(count, boundary, out long date);
                if (count == 0) continue;
                // All pending outputs and connected inputs bounded the prefix
                // above. There can be no event to discover between these dates.
                int pixels = possiblePixels > 1 && count >= firstPixel ? 1 + (count - firstPixel) / 4 : 0;
                if (pixels != 0)
                {
                    int pixelEdges = firstPixel + 4 * (pixels - 1), tail = count - pixelEdges;
                    _time = tail == 0 ? date : pixelInput.TimeAfterEdges(pixelEdges - 1);
                    AdvanceMasterHalfEdges(pixelEdges, pixels);
                    if (tail != 0) { _time = date; AdvanceMasterHalfEdges(tail); }
                }
                else { _time = date; AdvanceMasterHalfEdges(count); }
                consumed += count;
            }
        }
        finally
        {
            ResolveVramReadInputs(_time);
            _drivingMasterSource = false;
            _masterBoundary = long.MaxValue;
        }
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
