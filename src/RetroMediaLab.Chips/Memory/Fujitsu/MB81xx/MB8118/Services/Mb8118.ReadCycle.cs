using RetroMediaLab.Chips.Memory.Fujitsu.MB81xx.MB8118.Contracts;
using RetroMediaLab.Chips.Logic.Common.Enums;

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
    private bool _outputsRepresented;
    private bool _preparingOutputProgram, _readOutputsDescribed;
    private bool _resolvingRepresentedPrefix;
    private long _representedOutputUntil = -1;
    private bool _readInputsPredictable, _readAddressKnown;
    private int _readAddress;

    /// <summary>Qualifies the pending received read without advancing physical
    /// time or reading cells outside their owner. The consumer must withdraw
    /// this description before changing the received input waveform.</summary>
    public bool TryDescribeReadOutputs(out Mb8118ReadOutputProgram program)
    {
        program = default;
        if (!_hasReadCycle || _readInputStep != 1 || _outputChanged is not null
            || _deadlineChanged is not null || _writeHoldUntil > _time || !_readInputsPredictable)
            return false;

        LogicPinState initial = _output, atColumn = initial;
        long previousTime = -1;
        if (_outputDue >= 0 && _outputDue <= _receivedCasFall)
        {
            previousTime = _outputDue;
            atColumn = _target;
        }
        LogicPinState value = LogicPinState.Unknown;
        if (_readAddressKnown && _startupCycles >= 8)
        {
            long refreshed = _refreshed[_readAddress & 127];
            if (refreshed >= 0 && _receivedRasFall - refreshed <= Retention)
                value = (LogicPinState)_cells[_readAddress];
        }
        long readTime = value == atColumn ? -1 : _readResponseTime;
        long releaseTime = value == LogicPinState.HighImpedance ? -1 : _readReleaseTime;
        program = new(initial, previousTime, atColumn, readTime, value, releaseTime);
        _readOutputsDescribed = true;
        return true;
    }

    /// <summary>The wired consumer accepted the previously described DOUT
    /// suffix. Full scalar observers are deliberately incompatible with this
    /// transport; their callbacks must still see every output at its date.</summary>
    public void RepresentReadOutputs()
    {
        if (!_readOutputsDescribed || _outputChanged is not null || _deadlineChanged is not null)
            throw new InvalidOperationException("The received read has no closed output description.");
        _outputsRepresented = true;
        _representedOutputUntil = _readReleaseTime;
        PublishUnrepresentedDeadline();
    }

    public void WithdrawReadOutputs(long timeNanoseconds)
    {
        AdvanceCore(timeNanoseconds, true);
        _outputsRepresented = false;
        _representedOutputUntil = -1;
        PublishUnrepresentedDeadline();
    }

    private void RefreshResponseTime()
    {
        if (_resolvingRepresentedPrefix) return;
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
        var program = PrepareReadProgram(in cycle);
        ReceiveReadProgramCore(in program, rowData, columnData);
    }

    public Mb8118ReadProgram PrepareReadProgram(in Mb8118ReadCycle cycle)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(cycle.RowTime);
        if (cycle.Row > 127 || cycle.Column > 127 || cycle.RasFall < cycle.RowTime
            || cycle.CasFall < cycle.RasFall || cycle.RasRise < cycle.CasFall || cycle.CasRise < cycle.RasRise)
            throw new ArgumentException("Invalid multiplexed read waveform.", nameof(cycle));
        _ = checked(cycle.CasRise + _timing.MaximumOutputDelay);
        long response = Math.Max(checked(cycle.RasFall + _timing.RasAccess), checked(cycle.CasFall + _timing.CasAccess));
        long release = checked(cycle.CasRise + _timing.OutputOff);
        bool addressChanged = cycle.Row != cycle.Column || cycle.RowKnown != cycle.ColumnKnown;
        return new(_timingProfile, in cycle, response, release,
            response < cycle.CasRise && cycle.RasRise - cycle.RasFall <= MaximumPulse,
            cycle.RowKnown && cycle.ColumnKnown && (!addressChanged || cycle.CasFall - cycle.RasFall >= 15));
    }

    private void ReceiveReadProgramCore(in Mb8118ReadProgram program, bool? rowData, bool? columnData)
    {
        if (!ReferenceEquals(program.Profile, _timingProfile))
            throw new ArgumentException("The prepared electrical grid uses another memory timing profile.", nameof(program));
        ArgumentOutOfRangeException.ThrowIfLessThan(program.RowTime, _time);
        if (_publishing) throw new InvalidOperationException("Cannot replace a DRAM waveform during publication.");
        if (_hasReadCycle ? _receivedCasRise > program.RowTime : !_pins.RasHigh || !_pins.CasHigh || !_pins.WriteHigh)
            throw new InvalidOperationException("A complete read waveform requires a released RAS/CAS/WE input boundary.");
        AdvanceCore(program.RowTime, true);
        _outputsRepresented = false;
        _representedOutputUntil = -1;
        _receivedRasFall = program.RasFall; _receivedCasFall = program.CasFall;
        _receivedRasRise = program.RasRise; _receivedCasRise = program.CasRise;
        _receivedColumn = program.Column; _receivedColumnKnown = program.ColumnKnown;
        _receivedColumnData = columnData;
        _readResponseTime = program.ResponseTime;
        _readReleaseTime = program.ReleaseTime;
        _readInputsPredictable = program.Predictable;
        _readAddressKnown = program.ReadAddressKnown;
        _readAddress = program.ReadAddress;
        _readFirstResponseTime = Math.Min(_readResponseTime,_readReleaseTime);
        _readInputStep = 1;
        _hasReadCycle = true;
        _readResponseRepresented = false;
        RefreshResponseTime();
        ApplyLevels(new(program.Row, program.RowKnown, true, true, true, rowData));
        // A strictly future RAS cannot mutate a phase at RowTime. Still
        // publish the newly represented response, and retain the full resolver
        // for coincident inputs or an already due propagation.
        if (_receivedRasFall == program.RowTime || _outputDue >= 0 && _outputDue <= program.RowTime)
            AdvanceCore(program.RowTime, true);
        else PublishDeadline();
    }

    /// <summary>Receives the input and prepares its output transport in one
    /// publication boundary. Failure retains the received physical waveform
    /// and publishes its ordinary response deadline.</summary>
    public bool ReceiveReadCycle(in Mb8118ReadCycle cycle, bool? rowData, bool? columnData,
        out Mb8118ReadOutputProgram program)
    {
        var inputs = PrepareReadProgram(in cycle);
        return ReceiveReadProgram(in inputs, rowData, columnData, out program);
    }

    public bool ReceiveReadProgram(in Mb8118ReadProgram inputs, bool? rowData, bool? columnData,
        out Mb8118ReadOutputProgram program)
    {
        _preparingOutputProgram = true;
        try
        {
            ReceiveReadProgramCore(in inputs, rowData, columnData);
            bool accepted = TryDescribeReadOutputs(out program);
            if (accepted) RepresentReadOutputs();
            return accepted;
        }
        finally
        {
            _preparingOutputProgram = false;
            PublishUnrepresentedDeadline();
        }
    }

    /// <summary>Materializes the producer's already committed prefix through
    /// this date, then withdraws only the remaining future description.</summary>
    public void CancelReadCycle(long timeNanoseconds)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(timeNanoseconds, _time);
        if (_outputsRepresented) WithdrawForScalarObserver();
        AdvanceCore(timeNanoseconds, true);
        _hasReadCycle = false;
        _outputsRepresented = false;
        _representedOutputUntil = -1;
        RefreshResponseTime();
        PublishDeadline();
    }

    private void AdvanceCore(long timeNanoseconds, bool includeBoundaryInput)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(timeNanoseconds, _time);
        _readOutputsDescribed = false;
        if (_publishing)
        {
            if (timeNanoseconds != _time) throw new InvalidOperationException("Cannot advance DRAM from an output callback.");
            return;
        }
        // No unrepresented observer can run inside this accepted prefix.
        // Resolve its physical phases locally without republishing derived
        // deadlines at each RAS/CAS/output mutation, then expose one canonical
        // boundary. Pending output dates themselves are never deferred.
        if (_outputsRepresented && _time < _representedOutputUntil
            && _outputChanged is null && _deadlineChanged is null)
        {
            _resolvingRepresentedPrefix = true;
            long through = Math.Min(timeNanoseconds, _representedOutputUntil);
            try { ResolveReadPrefix(through, through < timeNanoseconds || includeBoundaryInput); }
            finally
            {
                _resolvingRepresentedPrefix = false;
                RefreshResponseTime();
                PublishDeadline();
            }
            if (through < timeNanoseconds) ResolveReadPrefix(timeNanoseconds, includeBoundaryInput);
            return;
        }
        ResolveReadPrefix(timeNanoseconds, includeBoundaryInput);
    }

    private void ResolveReadPrefix(long timeNanoseconds, bool includeBoundaryInput)
    {
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
            if (_outputChanged is null && (_outputsRepresented && _time <= _representedOutputUntil
                || UnrepresentedOutputChanged is null))
            {
                // A deadline-only scalar observer still observes this output
                // resolution before a later RAS/CAS input in the same call.
                if (!_resolvingRepresentedPrefix) PublishDeadline();
                continue;
            }
            _publishing = true;
            try
            {
                _outputChanged?.Invoke(_output);
                if (!_outputsRepresented || _time > _representedOutputUntil)
                    UnrepresentedOutputChanged?.Invoke(_output);
            }
            finally { _publishing = false; PublishDeadline(); }
        }
    }
}
