using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Interfaces;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Models;

namespace RetroMediaLab.Chips.EmuChip.Video.TexasInstruments.TMS99xx.TMS9918A.Services;

/// <summary>Cœur propriétaire en cours : interface CPU raccordée aux registres
/// physiques. Aucun tableau de VRAM privé ni ordonnanceur de machine.</summary>
public sealed partial class EmuTms9918A : ITms9918A
{
    private Tms9918Registers _registers;
    private Tms9918Status _status;
    private Tms9918CpuProtocol _protocol;
    private Tms9918CpuMemoryPort _memory;
    private Tms9918CpuWriteBus _writeBus;
    private Tms9918StatusReadBus _statusReadBus;
    private readonly Tms9918CpuTiming _cpuTiming;
    private Tms9918CpuPins _cpuPins;
    private long _dataDue = -1, _nextDue = -1;
    private bool _dataTargetDriven;
    private bool _readMode;
    private bool _readQualified;
    private bool _reset;
    private long _resetStarted;
    private bool _publishing;
    private long _announcedDeadline = -1;
    private bool _qualified = true;
    private Tms9918ClockRegisters _clock;
    private Tms9918RasterRegisters _raster;
    private bool _deliveringClock;

    private long _time;
    public long TimeNanoseconds => _time;
    public long? NextEventNanoseconds
    {
        get
        {
            long next = _nextDue;
            if (_projectedRead) next = next < 0 ? ObservedVramDue : Math.Min(next, ObservedVramDue);
            return next < 0 ? null : next;
        }
    }
    public Tms9918CpuOutputs CpuOutputs { get; private set; }
    public bool IsCpuInterfaceQualified => _qualified && !_writeBus.Capture().QualificationLost;
    public Tms9918ClockState ClockState => _clock;
    public Tms9918RasterState RasterState => _raster;
    // The complete public contract is an observation, not per-pixel transport.
    public Tms9918VideoSignal VideoSignal
    {
        get
        {
            _videoSignalMaterializations++;
            byte flags = _videoFlags;
            return new(_videoTime, _videoDot, _videoLine, _videoFrame,
                (flags & ActivePixel) != 0, (flags & HorizontalSyncPixel) != 0,
                (flags & VerticalSyncPixel) != 0, (flags & BlankingPixel) != 0,
                (flags & BurstPixel) != 0, _videoColour, (flags & KnownPixel) != 0,
                (flags & ExternalPixel) != 0, _videoVoltage);
        }
        private set
        {
            _videoTime = value.TimeNanoseconds; _videoDot = value.Dot; _videoLine = value.Line;
            _videoFrame = value.Frame; _videoColour = value.Colour; _videoVoltage = value.ExternalOutputVoltage;
            _videoFlags = (byte)((value.Active ? ActivePixel : 0)
                | (value.HorizontalSync ? HorizontalSyncPixel : 0) | (value.VerticalSync ? VerticalSyncPixel : 0)
                | (value.Blanking ? BlankingPixel : 0) | (value.ColorBurst ? BurstPixel : 0)
                | (value.ColourKnown ? KnownPixel : 0) | (value.ExternalVideo ? ExternalPixel : 0));
        }
    }
    public Action<Tms9918ClockOutput>? CpuClockChanged { get; set; }
    public Action<Tms9918ClockOutput>? GromClockChanged { get; set; }
    public Action<Tms9918VideoSignal>? VideoSignalProduced { get; set; }
    public Action<long>? FrameStarted { get; set; }
    public Action<Tms9918CpuOutputs>? CpuOutputsChanged { get; set; }
    private Action<long?>? _deadlineObserver;
    public Action<long?>? NextEventNanosecondsChanged
    {
        get => _deadlineObserver;
        set { if (value is not null) WithdrawProjectedRead(); _deadlineObserver = value; }
    }

    internal bool HasCpuVramRequest => _memory.HasPendingRequest;
    internal Tms9918CpuVramRequest CpuVramRequest => _memory.CurrentRequest;
    internal Tms9918RegisterState RegisterState => _registers.Capture();
    internal Tms9918CpuMemoryState CpuMemoryState => _memory.Capture();

    public EmuTms9918A(Tms9918InitialState initial, Tms9918CpuTiming timing)
    {
        _vramPinDispatch = PublishVramPins;
        ArgumentNullException.ThrowIfNull(timing);
        _cpuTiming = timing;
        if (initial.Clock.HalfEdgePhase is < 0 or >= 12 || initial.Clock.HalfEdges < 0
            || initial.Clock.GromHalfEdgePhase is < 0 or >= 48
            || initial.Raster.Dot is < 0 or >= Tms9918Timing.DotsPerLine
            || initial.Raster.Line is < 0 or >= Tms9918Timing.LinesPerFrame || initial.Raster.Frame < 0)
            throw new ArgumentException("Invalid TMS9918 clock/raster seed.");
        _clock = initial.Clock;
        _raster = initial.Raster;
        _registers = new(initial.Registers);
        _status = new(initial.Status);
        _protocol = new(initial.Control);
        _memory = new(initial.Memory);
        _cpuPins = initial.CpuPins;
        _writeBus = new(initial.CpuPins, 0) { WriteCaptured = AcceptCpuWrite };
        _statusReadBus = new(new(0, initial.CpuPins, false, false), _status, _protocol)
            { StatusAcknowledged = _ => PublishCpuOutputs() };
        CpuOutputs = new(0, 0, false, false, !_status.InterruptAsserted(_registers.InterruptEnabled));
        _qualified = initial.CpuPins.CsrHigh && initial.CpuPins.CswHigh;
        _vramBus = new(new(0, new(0, false, true, true, true), 0, false, 0, null))
        {
            ReadCaptured = CaptureVramRead,
        };
    }

    public void AdvanceTo(long timeNanoseconds)
        => AdvanceClockEventsTo(timeNanoseconds);

    private bool AdvanceClockEventsTo(long timeNanoseconds)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(timeNanoseconds, _time);
        if (_publishing && timeNanoseconds != _time)
            throw new InvalidOperationException("Cannot advance TMS9918A time from a pin callback.");
        while (_nextDue >= 0 && _nextDue <= timeNanoseconds)
        {
            long due = _nextDue;
            if (_drivingMasterSource && due >= _masterBoundary && due > _time)
            {
                ResolveVramReadInputs(_masterBoundary, inclusive: false);
                return false;
            }
            ResolveVramReadInputs(due, inclusive: false);
            _time = due;
            if (_vramWriteEnd == due)
            {
                _vramWriteEnd = -1;
                RefreshNextDeadline();
                _vramBus.ApplyOutputs(due, VramPins with { ReadWriteHigh = true });
            }
            if (_vramDue == due) AdvanceVramSequence();
            if (_dataDue == due)
            {
                SetDataDeadline(-1);
                if (_dataTargetDriven && !_cpuPins.CsrHigh && _cpuPins.CswHigh && !_reset)
                {
                    byte value = _readMode ? _status.Value : _memory.Data;
                    PublishCpuOutputs(value, true, _readQualified && (_readMode || _memory.DataKnown));
                }
                else PublishCpuOutputs(CpuOutputs.Data, false, false);
            }
            PublishDeadline();
            // Explicit DOUT delivery used AdvanceTo(date) before changing the
            // input. Preserve that ordering: local output phases and their
            // observers run first, then the represented inputs at this date.
            ResolveVramReadInputs(due);
        }
        if (_drivingMasterSource && timeNanoseconds >= _masterBoundary)
        {
            if (_masterBoundary > _time) ResolveVramReadInputs(_masterBoundary, inclusive: false);
            else ResolveVramReadInputs(_time);
            return false;
        }
        ResolveVramReadInputs(timeNanoseconds);
        _time = timeNanoseconds;
        return true;
    }

    private void PublishCpuOutputs(byte? data = null, bool? driven = null, bool? known = null)
    {
        PublishCpuBoundary();
        var previous = CpuOutputs;
        var next = new Tms9918CpuOutputs(_time, data ?? previous.Data,
            driven ?? previous.DataDriven, known ?? previous.DataKnown,
            !_status.InterruptAsserted(_registers.InterruptEnabled));
        // Une date seule n'est pas une transition électrique.
        if (previous.Data == next.Data && previous.DataDriven == next.DataDriven
            && previous.DataKnown == next.DataKnown && previous.IntHigh == next.IntHigh) return;
        CpuOutputs = next;
        bool wasPublishing = _publishing;
        _publishing = true;
        try { CpuOutputsChanged?.Invoke(next); }
        finally { _publishing = wasPublishing; }
    }

    private void PublishDeadline()
    {
        PublishCpuBoundary();
        if (_announcedDeadline == _nextDue) return;
        _announcedDeadline = _nextDue;
        bool wasPublishing = _publishing;
        _publishing = true;
        try { NextEventNanosecondsChanged?.Invoke(_announcedDeadline < 0 ? null : _announcedDeadline); }
        finally { _publishing = wasPublishing; }
    }

    private void SetDataDeadline(long due)
    {
        _dataDue = due;
        RefreshNextDeadline();
    }

    private void SetVramDeadline(long due)
    {
        _vramDue = due;
        RefreshNextDeadline();
    }

    private void RefreshNextDeadline()
    {
        long next = _dataDue;
        if (_vramDue >= 0 && (next < 0 || _vramDue < next)) next = _vramDue;
        if (_vramWriteEnd >= 0 && (next < 0 || _vramWriteEnd < next)) next = _vramWriteEnd;
        _nextDue = next;
    }

    internal void EndOfActiveDisplay()
    {
        _status.EndOfActiveDisplay();
        PublishCpuOutputs();
    }

    internal void CompleteCpuVramRead(byte data) => _memory.CompleteRead(data);
    internal void CompleteCpuVramWrite() => _memory.CompleteWrite();

    private static long? Earliest(long? first, long? second) => first is null ? second
        : second is null ? first : Math.Min(first.Value, second.Value);
}
