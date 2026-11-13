using RetroMediaLab.Chips.Memory.Fujitsu.MB81xx.MB8118.Contracts;

namespace RetroMediaLab.Chips.Memory.Fujitsu.MB81xx.MB8118.Services;

public sealed partial class Mb8118
{
    private long _receivedRasFall, _receivedCasFall, _receivedRasRise, _receivedCasRise;
    private byte _receivedColumn;
    private bool _receivedColumnKnown;
    private bool _hasReadCycle, _readResponseRepresented;
    private int _readInputStep;
    private long _readResponseTime, _readReleaseTime;
    private bool? _receivedColumnData;

    private long _nextResponseTime = -1, _readFirstResponseTime;

    private void RefreshResponseTime()
    {
        long next=_outputDue;
        if(_hasReadCycle)
        {
            long potential=_readResponseRepresented ? _readReleaseTime : _readFirstResponseTime;
            if(next<0 || potential<next)next=potential;
        }
        _nextResponseTime=next;
    }
    public void ReceiveReadCycle(Mb8118ReadCycle cycle)
        => ReceiveReadCycle(in cycle, cycle.RowData, cycle.ColumnData);

    public void ReceiveReadCycle(in Mb8118ReadCycle cycle, bool? rowData, bool? columnData)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(cycle.RowTime, _time);
        if (cycle.Row > 127 || cycle.Column > 127 || cycle.RasFall < cycle.RowTime
            || cycle.CasFall < cycle.RasFall || cycle.RasRise < cycle.CasFall || cycle.CasRise < cycle.RasRise)
            throw new ArgumentException("Invalid multiplexed read waveform.", nameof(cycle));
        _ = checked(cycle.CasRise + _timing.MaximumOutputDelay);
        if (_publishing) throw new InvalidOperationException("Cannot replace a DRAM waveform during publication.");
        if (_hasReadCycle ? _receivedCasRise > cycle.RowTime : !_pins.RasHigh || !_pins.CasHigh || !_pins.WriteHigh)
            throw new InvalidOperationException("A complete read waveform requires a released RAS/CAS/WE input boundary.");
        AdvanceCore(cycle.RowTime, true);
        _receivedRasFall = cycle.RasFall; _receivedCasFall = cycle.CasFall;
        _receivedRasRise = cycle.RasRise; _receivedCasRise = cycle.CasRise;
        _receivedColumn = cycle.Column; _receivedColumnKnown = cycle.ColumnKnown;
        _receivedColumnData = columnData;
        _readResponseTime = Math.Max(checked(cycle.RasFall + _timing.RasAccess), checked(cycle.CasFall + _timing.CasAccess));
        _readReleaseTime = checked(cycle.CasRise + _timing.OutputOff);
        _readFirstResponseTime = Math.Min(_readResponseTime,_readReleaseTime);
        _readInputStep = 1;
        _hasReadCycle = true;
        _readResponseRepresented = false;
        RefreshResponseTime();
        ApplyLevels(new(cycle.Row, cycle.RowKnown, true, true, true, rowData));
        // A strictly future RAS cannot mutate a phase at RowTime. Still
        // publish the newly represented response, and retain the full resolver
        // for coincident inputs or an already due propagation.
        if (_receivedRasFall == cycle.RowTime || _outputDue >= 0 && _outputDue <= cycle.RowTime)
            AdvanceCore(cycle.RowTime, true);
        else PublishDeadline();
    }

    /// <summary>Materializes the producer's already committed prefix through
    /// this date, then withdraws only the remaining future description.</summary>
    public void CancelReadCycle(long timeNanoseconds)
    {
        AdvanceCore(timeNanoseconds, true);
        _hasReadCycle = false;
        RefreshResponseTime();
        PublishDeadline();
    }

    private void AdvanceCore(long timeNanoseconds, bool includeBoundaryInput)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(timeNanoseconds, _time);
        if (_publishing)
        {
            if (timeNanoseconds != _time) throw new InvalidOperationException("Cannot advance DRAM from an output callback.");
            return;
        }
        // The received waveform has ordered phases. Enter at its current
        // position and stop at the first future input. Output callbacks may
        // withdraw the waveform, so retain the ownership check at each phase.
        if (_hasReadCycle)
        {
            switch (_readInputStep)
            {
                case 1:
                    if (!Reaches(_receivedRasFall, timeNanoseconds, includeBoundaryInput)) break;
                    if (_outputDue >= 0 && _outputDue <= _receivedRasFall) ResolveReadOutputTo(_receivedRasFall);
                    if (!_hasReadCycle) break;
                    _time = _receivedRasFall;
                    _readInputStep = 2; _pins.RasHigh = false;
                    BeginRow(_time, _pins);
                    goto case 2;
                case 2:
                    if (!Reaches(_receivedCasFall, timeNanoseconds, includeBoundaryInput)) break;
                    if (_outputDue >= 0 && _outputDue <= _receivedCasFall) ResolveReadOutputTo(_receivedCasFall);
                    if (!_hasReadCycle) break;
                    _time = _receivedCasFall;
                    ChangeAddressData(_time, _receivedColumn, _receivedColumnKnown, _receivedColumnData);
                    _readInputStep = 3; _pins.CasHigh = false;
                    BeginColumn(_time, _pins);
                    goto case 3;
                case 3:
                    if (!Reaches(_receivedRasRise, timeNanoseconds, includeBoundaryInput)) break;
                    if (_outputDue >= 0 && _outputDue <= _receivedRasRise) ResolveReadOutputTo(_receivedRasRise);
                    if (!_hasReadCycle) break;
                    _time = _receivedRasRise;
                    _readInputStep = 4; _pins.RasHigh = true;
                    EndRow(_time);
                    goto case 4;
                case 4:
                    if (!Reaches(_receivedCasRise, timeNanoseconds, includeBoundaryInput)) break;
                    if (_outputDue >= 0 && _outputDue <= _receivedCasRise) ResolveReadOutputTo(_receivedCasRise);
                    if (!_hasReadCycle) break;
                    _time = _receivedCasRise;
                    _hasReadCycle = false; _pins.CasHigh = true;
                    EndColumn(_time);
                    break;
            }
        }
        if (_outputDue >= 0 && _outputDue <= timeNanoseconds) ResolveReadOutputTo(timeNanoseconds);
        _time = timeNanoseconds;
        if (_hasReadCycle && !_readResponseRepresented && timeNanoseconds >= _readResponseTime)
        { _readResponseRepresented = true; RefreshResponseTime(); }
        PublishDeadline();
    }

    private static bool Reaches(long phase, long until, bool inclusive)
        => phase < until || inclusive && phase == until;

    private void ResolveReadOutputTo(long until)
    {
        while (_outputDue >= 0 && _outputDue <= until)
        {
            _time = _outputDue;
            if (_hasReadCycle && _time >= _readResponseTime) _readResponseRepresented = true;
            _outputDue = -1; _output = _target;
            RefreshResponseTime();
            _publishing = true;
            try { _outputChanged?.Invoke(_output); }
            finally { _publishing = false; PublishDeadline(); }
        }
    }
}
