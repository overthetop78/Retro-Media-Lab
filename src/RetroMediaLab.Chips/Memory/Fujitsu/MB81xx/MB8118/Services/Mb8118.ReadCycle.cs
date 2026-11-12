using RetroMediaLab.Chips.Memory.Fujitsu.MB81xx.MB8118.Contracts;

namespace RetroMediaLab.Chips.Memory.Fujitsu.MB81xx.MB8118.Services;

public sealed partial class Mb8118
{
    private Mb8118ReadCycle _receivedReadCycle;
    private bool _hasReadCycle, _readResponseRepresented;
    private int _readInputStep;
    private long _readResponseTime, _readReleaseTime;
    private bool? _receivedColumnData;

    private long NextResponseTime
    {
        get
        {
            long next = _outputDue;
            if (_hasReadCycle)
            {
                long potential = _readResponseRepresented ? _readReleaseTime : Math.Min(_readResponseTime, _readReleaseTime);
                if (next < 0 || potential < next) next = potential;
            }
            return next;
        }
    }

    public void ReceiveReadCycle(Mb8118ReadCycle cycle)
        => ReceiveReadCycle(in cycle, cycle.RowData, cycle.ColumnData);

    public void ReceiveReadCycle(in Mb8118ReadCycle cycle, bool? rowData, bool? columnData)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(cycle.RowTime, TimeNanoseconds);
        if (cycle.Row > 127 || cycle.Column > 127 || cycle.RasFall < cycle.RowTime
            || cycle.CasFall < cycle.RasFall || cycle.RasRise < cycle.CasFall || cycle.CasRise < cycle.RasRise)
            throw new ArgumentException("Invalid multiplexed read waveform.", nameof(cycle));
        _ = checked(cycle.CasRise + _timing.MaximumOutputDelay);
        if (_publishing) throw new InvalidOperationException("Cannot replace a DRAM waveform during publication.");
        if (_hasReadCycle ? _receivedReadCycle.CasRise > cycle.RowTime : !_pins.RasHigh || !_pins.CasHigh || !_pins.WriteHigh)
            throw new InvalidOperationException("A complete read waveform requires a released RAS/CAS/WE input boundary.");
        AdvanceCore(cycle.RowTime, true);
        _receivedReadCycle = cycle;
        _receivedColumnData = columnData;
        _readResponseTime = Math.Max(checked(cycle.RasFall + _timing.RasAccess), checked(cycle.CasFall + _timing.CasAccess));
        _readReleaseTime = checked(cycle.CasRise + _timing.OutputOff);
        _readInputStep = 1;
        _hasReadCycle = true;
        _readResponseRepresented = false;
        ApplyLevels(new(cycle.Row, cycle.RowKnown, true, true, true, rowData));
        AdvanceCore(cycle.RowTime, true);
    }

    /// <summary>Materializes the producer's already committed prefix through
    /// this date, then withdraws only the remaining future description.</summary>
    public void CancelReadCycle(long timeNanoseconds)
    {
        AdvanceCore(timeNanoseconds, true);
        _hasReadCycle = false;
        PublishDeadline();
    }

    private void AdvanceCore(long timeNanoseconds, bool includeBoundaryInput)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(timeNanoseconds, TimeNanoseconds);
        if (_publishing)
        {
            if (timeNanoseconds != TimeNanoseconds) throw new InvalidOperationException("Cannot advance DRAM from an output callback.");
            return;
        }
        // The input waveform is already decoded. No event search, synthetic
        // pin word or generic edge decoder is needed for these four phases.
        // Output propagation still precedes an input at the same date; a
        // callback may withdraw this waveform, so recheck ownership afterward.
        if (_hasReadCycle && _readInputStep == 1 && Reaches(_receivedReadCycle.RasFall, timeNanoseconds, includeBoundaryInput))
        {
            if (_outputDue >= 0 && _outputDue <= _receivedReadCycle.RasFall) ResolveReadOutputTo(_receivedReadCycle.RasFall);
            if (_hasReadCycle)
            {
                TimeNanoseconds = _receivedReadCycle.RasFall;
                _readInputStep = 2; _pins.RasHigh = false;
                BeginRow(TimeNanoseconds, _pins);
            }
        }
        if (_hasReadCycle && _readInputStep == 2 && Reaches(_receivedReadCycle.CasFall, timeNanoseconds, includeBoundaryInput))
        {
            if (_outputDue >= 0 && _outputDue <= _receivedReadCycle.CasFall) ResolveReadOutputTo(_receivedReadCycle.CasFall);
            if (_hasReadCycle)
            {
                TimeNanoseconds = _receivedReadCycle.CasFall;
                ChangeAddressData(TimeNanoseconds, _receivedReadCycle.Column, _receivedReadCycle.ColumnKnown, _receivedColumnData);
                _readInputStep = 3; _pins.CasHigh = false;
                BeginColumn(TimeNanoseconds, _pins);
            }
        }
        if (_hasReadCycle && _readInputStep == 3 && Reaches(_receivedReadCycle.RasRise, timeNanoseconds, includeBoundaryInput))
        {
            if (_outputDue >= 0 && _outputDue <= _receivedReadCycle.RasRise) ResolveReadOutputTo(_receivedReadCycle.RasRise);
            if (_hasReadCycle)
            {
                TimeNanoseconds = _receivedReadCycle.RasRise;
                _readInputStep = 4; _pins.RasHigh = true;
                EndRow(TimeNanoseconds);
            }
        }
        if (_hasReadCycle && _readInputStep == 4 && Reaches(_receivedReadCycle.CasRise, timeNanoseconds, includeBoundaryInput))
        {
            if (_outputDue >= 0 && _outputDue <= _receivedReadCycle.CasRise) ResolveReadOutputTo(_receivedReadCycle.CasRise);
            if (_hasReadCycle)
            {
                TimeNanoseconds = _receivedReadCycle.CasRise;
                _hasReadCycle = false; _pins.CasHigh = true;
                EndColumn(TimeNanoseconds);
            }
        }
        if (_outputDue >= 0 && _outputDue <= timeNanoseconds) ResolveReadOutputTo(timeNanoseconds);
        TimeNanoseconds = timeNanoseconds;
        if (_hasReadCycle && timeNanoseconds >= _readResponseTime) _readResponseRepresented = true;
        PublishDeadline();
    }

    private static bool Reaches(long phase, long until, bool inclusive)
        => phase < until || inclusive && phase == until;

    private void ResolveReadOutputTo(long until)
    {
        while (_outputDue >= 0 && _outputDue <= until)
        {
            TimeNanoseconds = _outputDue;
            if (_hasReadCycle && TimeNanoseconds >= _readResponseTime) _readResponseRepresented = true;
            _outputDue = -1; _output = _target;
            _publishing = true;
            try { OutputChanged?.Invoke(_output); }
            finally { _publishing = false; PublishDeadline(); }
        }
    }
}
