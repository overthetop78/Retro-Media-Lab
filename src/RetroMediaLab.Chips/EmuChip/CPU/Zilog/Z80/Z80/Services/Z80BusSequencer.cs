using System.Numerics;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Contracts;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Enums;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Models;
using RetroMediaLab.Chips.Logic.Common.Contracts;
using C = RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Constants.Z80PinEncoding;
using L = RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Constants.Z80BusLines;

namespace RetroMediaLab.Chips.EmuChip.CPU.Zilog.Z80.Z80.Services;

/// <summary>Séquenceur commun : Zilog 03-0027-02 pages 3–5 ; délais du profil physique explicite.
/// Les appels représentent les vrais fronts reçus ; les propagations sont résolues localement.</summary>
/// <remarks>Ce bloc n'est pas un CPU autonome : le décodeur choisira les transactions et la logique
/// CPU traitera reset, concession de bus, HALT et interruptions. Aucun périphérique n'est interrogé.</remarks>
internal sealed class Z80BusSequencer
{
    private readonly Z80BusTiming _timing;
    private readonly Z80BusWaveforms _waveforms;
    private Z80BusStep[] _steps;
    private readonly long _maximumDelay;
    private ulong _levels = C.InitialLevels, _targetLevels = C.InitialLevels;
    private readonly long[] _due = new long[L.Count];
    internal long TimeNanoseconds, RisingClocks;
    internal long NextEventTimestamp = long.MaxValue;
    private long _lastClockEdge, _lastRisingEdge;
    private long _publishedNext = long.MaxValue;
    private long _completedCycles;
    internal bool ClockHigh, IsActive;
    private bool _waiting, _waitAsserted, _publishing;
    private int _phase;
    private Z80BusCycle _cycle;
    private ushort _address, _refreshAddress;
    private byte _writeData, _inputData, _sampledData;
    private bool _hasSample, _holdWriteData;
    private bool _nextDirty;
    private uint _pending;
    private long _executionBoundary = long.MaxValue;
    private TimedZ80BusPins[]? _rangeChanges;
    private bool _capturingRange;
    private int _rangeChangeCount;
    private long _rangeStart;
    private Z80BusPins _rangeInitial;
    private Z80BusRangeHandler? _rangeReceiver;
    private bool _rangeDelivered;
    private long _waitStableBefore, _controlStableBefore, _waitOutputsSettled, _controlOutputsSettled;
    private uint _waitDependentCycles, _controlDependentCycles;
    private long _heldWaitSamples, _heldControlSamples;
    internal Z80BusRangeStatistics RangeStatistics => new(_heldWaitSamples, _heldControlSamples);

    internal void ReceiveInputWindow(Z80BusInputWindow window)
    {
        if (!_capturingRange || !_publishing)
            throw new InvalidOperationException("Input stability must be delivered with the received bus prefix.");
        ArgumentOutOfRangeException.ThrowIfLessThan(window.WaitBefore, TimeNanoseconds);
        ArgumentOutOfRangeException.ThrowIfLessThan(window.ControlBefore, TimeNanoseconds);
        if (((window.WaitDependentCycles | window.ControlDependentCycles) & ~63u) != 0)
            throw new ArgumentOutOfRangeException(nameof(window));
        _waitDependentCycles = window.WaitDependentCycles;
        _controlDependentCycles = window.ControlDependentCycles;
        _waitStableBefore = window.WaitBefore; _controlStableBefore = window.ControlBefore;
        // A promise received halfway through a dependent transaction cannot
        // cover its later strobes, even if no response is pending yet.
        if (IsActive)
        {
            uint cycle = 1u << (int)_cycle;
            if ((_waitDependentCycles & cycle) != 0) _waitOutputsSettled = long.MaxValue;
            if ((_controlDependentCycles & cycle) != 0) _controlOutputsSettled = long.MaxValue;
        }
        else if (_pending != 0)
        {
            uint cycle = 1u << (int)_cycle;
            long settled = LastPendingOutputTime();
            if ((_waitDependentCycles & cycle) != 0) _waitOutputsSettled = settled;
            if ((_controlDependentCycles & cycle) != 0) _controlOutputsSettled = settled;
        }
    }

    private long LastPendingOutputTime()
    {
        long settled = TimeNanoseconds;
        uint pending = _pending;
        while (pending != 0)
        {
            int line = BitOperations.TrailingZeroCount(pending);
            pending &= pending - 1;
            if (_due[line] > settled) settled = _due[line];
        }
        return settled;
    }

    internal void ExchangeWaitInputs()
    {
        if (_capturingRange && TimeNanoseconds < _waitStableBefore && TimeNanoseconds >= _waitOutputsSettled)
        { _heldWaitSamples++; return; }
        ExchangeRangeInputs();
    }

    internal void ExchangeControlInputs()
    {
        if (_capturingRange && TimeNanoseconds < _controlStableBefore && TimeNanoseconds >= _controlOutputsSettled)
        { _heldControlSamples++; return; }
        ExchangeRangeInputs();
    }

    internal Z80BusSequencer(Z80BusTiming timing, byte initialInputData)
    {
        ArgumentNullException.ThrowIfNull(timing);
        _maximumDelay = timing.ValidateAndGetMaximumDelay();
        _timing = timing;
        _waveforms = new Z80BusWaveforms(timing);
        _steps = _waveforms.Cycles[0];
        _inputData = initialInputData;
        Array.Fill(_due, long.MaxValue);
    }

    internal long CompletedCycles => _completedCycles;
    internal bool IsPublishing => _publishing;
    internal bool IsCapturingRange => _capturingRange;
    internal bool AtCycleStart => IsActive && _phase == 0 && TimeNanoseconds == _lastRisingEdge;
    internal bool HasSample => _hasSample;
    internal int Phase => _phase;
    internal Z80BusCycle Cycle => _cycle;
    internal bool AtLastClockStart => IsActive && !_waiting && _phase ==
        (_cycle == Z80BusCycle.InterruptAcknowledge ? 10
        : _cycle is Z80BusCycle.MemoryRead or Z80BusCycle.MemoryWrite ? 4 : 6);
    internal bool AtOpcodeT2 => IsActive && _cycle == Z80BusCycle.OpcodeFetch && _phase == 2;
    internal byte SampledData => _hasSample ? _sampledData : throw new InvalidOperationException("No data has been sampled.");
    internal long? NextEventNanoseconds => NextEventTimestamp == long.MaxValue ? null : NextEventTimestamp;
    internal Action<Z80BusPins>? PinsChanged { get; set; }
    internal Action<long?>? NextEventNanosecondsChanged { get; set; }
    internal long? PublishedNextEventNanoseconds => _publishedNext == long.MaxValue ? null : _publishedNext;
    internal Z80BusPins Pins => new(_levels);

    internal int RangeChangeCount => _rangeChangeCount;

    internal void BeginRangeCapture(Z80BusRangeHandler receiver)
    {
        if (_capturingRange || _publishing || PinsChanged is not null || NextEventNanosecondsChanged is not null)
            throw new InvalidOperationException("Range delivery requires exclusive ownership of bus output transport.");
        _rangeChanges ??= new TimedZ80BusPins[128];
        _rangeStart = TimeNanoseconds;
        _rangeInitial = Pins;
        _rangeChangeCount = 0;
        _rangeDelivered = false;
        _waitStableBefore = _controlStableBefore = 0;
        _capturingRange = true;
        _rangeReceiver = receiver;
    }

    internal void ExchangeRangeInputs()
    {
        if (!_capturingRange) return;
        if (_rangeDelivered && _rangeChangeCount == 0 && _rangeStart == TimeNanoseconds) return;
        _rangeDelivered = true;
        var result = new Z80BusRange(_rangeStart, TimeNanoseconds, _rangeInitial,
            _rangeChanges.AsSpan(0, _rangeChangeCount));
        _rangeStart = TimeNanoseconds;
        _rangeInitial = Pins;
        _rangeChangeCount = 0;
        _publishing = true;
        try { _rangeReceiver!(in result); }
        finally { _publishing = false; }
    }

    internal void EndRangeCapture() { _capturingRange = false; _rangeReceiver = null; _rangeChangeCount = 0; }

    internal void SetData(byte data) => _inputData = data;
    internal void SetWait(bool asserted) => _waitAsserted = asserted;

    internal void SetHalt(bool asserted)
    {
        EnsureClockOperation();
        Schedule(L.Halt, asserted ? 0 : 1, _timing.Halt);
        PublishDeadline();
    }

    internal void SetBusGranted(bool granted)
    {
        EnsureClockOperation();
        _waitStableBefore = _controlStableBefore = 0;
        if (granted)
        {
            if (IsActive) throw new InvalidOperationException("Cannot grant an active Z80 bus transaction.");
            Schedule(L.Address, -1, _timing.AddressFloat);
            Schedule(L.Data, -1, _timing.DataFloat);
            for (int line = L.Memory; line <= L.Write; line++) Schedule(line, -1, _timing.ControlFloat);
        }
        else
        {
            Schedule(L.Memory, 1, _timing.MemoryRelease);
            Schedule(L.Io, 1, _timing.IoReleaseFalling);
            Schedule(L.Read, 1, _timing.ReadReleaseFalling);
            Schedule(L.Write, 1, _timing.WriteRelease);
        }
        Schedule(L.BusAcknowledge, granted ? 0 : 1,
            granted ? _timing.BusAcknowledgeAssert : _timing.BusAcknowledgeRelease);
        PublishDeadline();
    }

    // Zilog UM008011-0816 et NEC Microcomputer Catalog 1982, p.393 :
    // même pilotage documenté du reset normal, aucun profil CPU de substitution.
    internal void EnterReset()
    {
        EnsureClockOperation();
        _waitStableBefore = _controlStableBefore = 0;
        _waitOutputsSettled = _controlOutputsSettled = checked(TimeNanoseconds + _maximumDelay);
        IsActive = _waiting = _hasSample = _holdWriteData = false;
        Schedule(L.Address, -1, _timing.AddressFloat);
        Schedule(L.Data, -1, _timing.DataFloat);
        Schedule(L.Memory, 1, _timing.MemoryRelease);
        Schedule(L.Io, 1, _timing.IoReleaseFalling);
        Schedule(L.Read, 1, _timing.ReadReleaseFalling);
        Schedule(L.Write, 1, _timing.WriteRelease);
        Schedule(L.M1, 1, _timing.M1Release);
        Schedule(L.Refresh, 1, _timing.RefreshRelease);
        Schedule(L.Halt, 1, _timing.Halt);
        Schedule(L.BusAcknowledge, 1, _timing.BusAcknowledgeRelease);
        PublishDeadline();
    }

    /// <summary>Commence T1 sur le front montant courant. L'adresse refresh est fournie
    /// depuis I/R du CPU, dont l'incrément n'appartient pas à ce séquenceur.</summary>
    internal void BeginCycle(Z80BusCycle cycle, ushort address, ushort refreshAddress, byte writeData = 0,
        bool holdWriteData = false)
    {
        EnsureClockOperation();
        if (IsActive || !ClockHigh || _lastRisingEdge != TimeNanoseconds)
            throw new InvalidOperationException("A transaction must begin on an available rising clock edge.");
        if (!Enum.IsDefined(cycle)) throw new ArgumentOutOfRangeException(nameof(cycle));
        if (holdWriteData && cycle != Z80BusCycle.MemoryWrite)
            throw new ArgumentException("Only a memory write can hold output data.", nameof(holdWriteData));
        if (_completedCycles == long.MaxValue) throw new OverflowException();
        _cycle = cycle;
        if ((_waitDependentCycles & (1u << (int)cycle)) != 0) _waitOutputsSettled = long.MaxValue;
        if ((_controlDependentCycles & (1u << (int)cycle)) != 0) _controlOutputsSettled = long.MaxValue;
        _steps = _waveforms.Cycles[(int)cycle];
        _address = address;
        _refreshAddress = refreshAddress;
        _writeData = writeData;
        _holdWriteData = holdWriteData;
        _phase = 0;
        IsActive = true;
        _waiting = _hasSample = false;
        ApplyStep(in _steps[0]);
        PublishDeadline();
    }

    internal void SetClock(bool high)
    {
        if (ClockHigh == high) return;
        ValidateClockEdge(TimeNanoseconds, high);
        AdvanceClockEdge(high);
    }

    internal void ValidateClockEdge(long time, bool high)
    {
        EnsureClockOperation();
        if (RisingClocks != 0)
        {
            long minimum = ClockHigh ? _timing.MinimumHigh : _timing.MinimumLow;
            if (time - _lastClockEdge < minimum)
                throw new ArgumentException("Clock pulse is shorter than the selected profile allows.");
        }
        if (high && RisingClocks != 0 && time - _lastRisingEdge < _timing.MinimumPeriod)
            throw new ArgumentException("Clock period is shorter than the selected profile allows.");
    }

    /// <summary>Front déjà qualifié : même mutation canonique dans les deux entrées.
    /// Aucune capture ni propagation du bus n'est omise.</summary>
    internal void AdvanceClockEdge(bool high)
    {
        long clocks = high ? checked(RisingClocks + 1) : RisingClocks;
        ClockHigh = high;
        _lastClockEdge = TimeNanoseconds;
        if (high) { RisingClocks = clocks; _lastRisingEdge = TimeNanoseconds; }
        if (!IsActive) return;
        if (_waiting)
        {
            if (!high)
            {
                ExchangeWaitInputs();
                _waiting = _waitAsserted;
            }
            return;
        }
        _phase++;
        ApplyStep(in _steps[_phase]);
        PublishDeadline();
    }

    /// <summary>Qualification d'un intervalle fourni par le générateur extérieur,
    /// avant toute mutation. Les niveaux du train sont strictement alternés.</summary>
    internal void ValidateClockTrain(long first, long halfPeriod, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(halfPeriod);
        ArgumentOutOfRangeException.ThrowIfLessThan(first, TimeNanoseconds);
        if (halfPeriod < _timing.MinimumHigh || halfPeriod < _timing.MinimumLow
            || halfPeriod < (_timing.MinimumPeriod / 2 + _timing.MinimumPeriod % 2))
            throw new ArgumentException("Clock train is faster than the selected profile allows.");
        long end = checked(first + (count - 1L) * halfPeriod);
        if (end >= long.MaxValue - _maximumDelay) throw new OverflowException();
        ValidateClockEdge(first, !ClockHigh);
        if (ClockHigh && count > 1 && RisingClocks != 0
            && first + halfPeriod - _lastRisingEdge < _timing.MinimumPeriod)
            throw new ArgumentException("First clock period is shorter than the selected profile allows.");
        checked { _ = RisingClocks + (count + (ClockHigh ? 0L : 1L)) / 2; }
    }

    internal void ValidateClockSource(RationalClockSource source)
    {
        long first = source.FirstEdgeNanoseconds;
        ArgumentOutOfRangeException.ThrowIfLessThan(first, TimeNanoseconds);
        if (first >= long.MaxValue - _maximumDelay) throw new OverflowException();
        long shortestHalf = source.HalfPeriodNumerator / source.HalfPeriodDenominator;
        if (source.FirstEdgeHigh == ClockHigh || shortestHalf < _timing.MinimumHigh
            || shortestHalf < _timing.MinimumLow
            || (Int128)2 * source.HalfPeriodNumerator / source.HalfPeriodDenominator < _timing.MinimumPeriod)
            throw new ArgumentException("Clock waveform does not match the selected profile or input level.");
        ValidateClockEdge(first, source.FirstEdgeHigh);
    }

    internal void SetExecutionBoundary(long boundaryExclusive) => _executionBoundary = boundaryExclusive;

    /// <summary>Préfixe interne sans effet ni propagation, qualifié par le CPU.
    /// Les compteurs et dates représentent tous les fronts réellement reçus.</summary>
    internal void AdvanceStableClockEdges(long first, long halfPeriod, int count)
    {
        bool firstHigh = !ClockHigh;
        long rising = (count + (firstHigh ? 1L : 0L)) / 2;
        long last = first + (count - 1L) * halfPeriod;
        long lastRising = first + ((firstHigh ? 0L : 1L) + 2 * (rising - 1)) * halfPeriod;
        AdvanceStableClockEdges(first, last, lastRising, count);
    }

    internal void AdvanceStableClockEdges(long first, long last, long lastRising, int count)
    {
        long rising = (count + (ClockHigh ? 0L : 1L)) / 2;
        long clocks = checked(RisingClocks + rising);
        ClockHigh = (count & 1) != 0 ? !ClockHigh : ClockHigh;
        TimeNanoseconds = _lastClockEdge = last;
        RisingClocks = clocks;
        if (rising != 0) _lastRisingEdge = lastRising;
    }

    private void ApplyStep(in Z80BusStep step)
    {
        var action = step.Action;
        if ((action & Z80BusStepAction.Sample) != 0) ExchangeRangeInputs();
        else if ((action & Z80BusStepAction.Wait) != 0) ExchangeWaitInputs();
        if ((action & Z80BusStepAction.Sample) != 0) { _sampledData = _inputData; _hasSample = true; }
        if ((action & Z80BusStepAction.Wait) != 0) _waiting = _waitAsserted;
        ulong mask = step.Mask, targets = step.Targets;
        uint outputs = step.Outputs;
        if ((action & (Z80BusStepAction.RefreshAddress | Z80BusStepAction.BeginAddress)) != 0)
            targets = (targets & ~C.AddressMask) | ((action & Z80BusStepAction.RefreshAddress) != 0
                ? _refreshAddress : _address);
        if ((action & Z80BusStepAction.WriteData) != 0)
            targets = (targets & ~C.DataMask) | ((ulong)_writeData << C.DataShift);
        if ((action & Z80BusStepAction.CompleteWrite) != 0 && _holdWriteData)
        { mask &= ~C.Masks[L.Data]; outputs &= ~(1u << L.Data); }
        if (outputs != 0) ScheduleTargets(mask, targets, outputs, step.Delays);
        if ((action & (Z80BusStepAction.Complete | Z80BusStepAction.CompleteWrite)) != 0)
        {
            IsActive = false; _completedCycles++;
            uint cycle = 1u << (int)_cycle;
            if (((_waitDependentCycles | _controlDependentCycles) & cycle) != 0)
            {
                long settled = LastPendingOutputTime();
                if ((_waitDependentCycles & cycle) != 0) _waitOutputsSettled = settled;
                if ((_controlDependentCycles & cycle) != 0) _controlOutputsSettled = settled;
            }
        }
    }

    internal void ReleaseWriteData()
    {
        EnsureClockOperation();
        if (!_holdWriteData) return;
        _holdWriteData = false;
        Schedule(L.Data, -1, _timing.DataFloat);
        PublishDeadline();
    }

    private void Schedule(int line, int target, long delay)
    {
        ulong mask = C.Masks[line];
        ulong bits;
        if (line == L.Address) bits = target < 0 ? 0 : (ulong)(ushort)target | C.AddressDriven;
        else if (line == L.Data) bits = target < 0 ? 0 : ((ulong)(byte)target << C.DataShift) | C.DataDriven;
        else bits = target < 0 ? 0 : target != 0 ? mask : C.DrivenMasks[line];
        ScheduleTargets(mask, bits, 1u << line, null, delay);
    }

    private void ScheduleTargets(ulong mask, ulong targets, uint outputs, long[]? delays, long singleDelay = 0)
    {
        ulong changed = (_targetLevels ^ targets) & mask;
        if (changed == 0) return;
        _targetLevels = (_targetLevels & ~mask) | (targets & mask);
        while (outputs != 0)
        {
            int line = BitOperations.TrailingZeroCount(outputs);
            uint bit = 1u << line;
            outputs &= ~bit;
            ulong lineMask = C.Masks[line];
            if ((changed & lineMask) == 0) continue;
            _nextDirty |= NextEventTimestamp != long.MaxValue && _due[line] == NextEventTimestamp;
            _due[line] = ((_levels ^ targets) & lineMask) == 0 ? long.MaxValue
                : checked(TimeNanoseconds + (delays is null ? singleDelay : delays[line]));
            if (_due[line] == long.MaxValue) _pending &= ~bit;
            else _pending |= bit;
            if (!_nextDirty && _due[line] < NextEventTimestamp) NextEventTimestamp = _due[line];
        }
    }

    /// <summary>Avance seulement aux propagations locales ; aucune boucle par ns ou cycle.</summary>
    internal bool AdvanceTo(long nanoseconds)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(nanoseconds, TimeNanoseconds);
        if (_publishing)
        {
            if (nanoseconds != TimeNanoseconds) throw new InvalidOperationException("Cannot advance time from a pin callback.");
            return true;
        }
        while (NextEventTimestamp != long.MaxValue && NextEventTimestamp <= nanoseconds)
        {
            if (_executionBoundary != long.MaxValue && NextEventTimestamp >= _executionBoundary
                && NextEventTimestamp > TimeNanoseconds) return false;
            TimeNanoseconds = NextEventTimestamp;
            ulong previousLevels = _levels;
            long next = long.MaxValue;
            uint pending = _pending;
            while (pending != 0)
            {
                int line = BitOperations.TrailingZeroCount(pending);
                uint bit = 1u << line;
                pending &= ~bit;
                long due = _due[line];
                if (due == TimeNanoseconds)
                {
                    _pending &= ~bit;
                    _due[line] = long.MaxValue;
                    ulong mask = C.Masks[line];
                    _levels = (_levels & ~mask) | (_targetLevels & mask);
                }
                else if (due < next) next = due;
            }
            NextEventTimestamp = next;
            _nextDirty = false;
            if (_capturingRange && _levels != previousLevels)
                _rangeChanges![_rangeChangeCount++] = new(TimeNanoseconds, new(_levels));
            if (_levels != previousLevels && PinsChanged is { } listener)
            {
                _publishing = true;
                try { listener(new Z80BusPins(_levels)); }
                finally { _publishing = false; }
            }
            PublishDeadline(recompute: false);
        }
        if (_executionBoundary != long.MaxValue && nanoseconds >= _executionBoundary) return false;
        TimeNanoseconds = nanoseconds;
        return true;
    }

    private void FindNext()
    {
        NextEventTimestamp = long.MaxValue;
        uint pending = _pending;
        while (pending != 0)
        {
            int line = BitOperations.TrailingZeroCount(pending);
            pending &= pending - 1;
            if (_due[line] < NextEventTimestamp) NextEventTimestamp = _due[line];
        }
        _nextDirty = false;
    }

    private void PublishDeadline(bool recompute = true)
    {
        if (recompute && _nextDirty) FindNext();
        if (_publishedNext == NextEventTimestamp) return;
        _publishedNext = NextEventTimestamp;
        if (NextEventNanosecondsChanged is not { } listener) return;
        _publishing = true;
        try { listener(NextEventTimestamp == long.MaxValue ? null : NextEventTimestamp); }
        finally { _publishing = false; }
    }

    private void EnsureClockOperation()
    {
        if (_publishing) throw new InvalidOperationException("Cannot change clock sequencing from a pin callback.");
        if (TimeNanoseconds >= long.MaxValue - _maximumDelay) throw new OverflowException();
    }

    internal Z80BusState CaptureState() => new(_timing, TimeNanoseconds, _lastClockEdge, _lastRisingEdge,
        RisingClocks, _completedCycles, ClockHigh, IsActive, _waiting, _waitAsserted,
        _phase, _cycle, _address, _refreshAddress, _writeData, _inputData, _sampledData,
        _hasSample, CapturePinValues(_levels), CapturePinValues(_targetLevels), (long[])_due.Clone(), _holdWriteData);

    /// <summary>Le propriétaire restaure également les autres composants ; aucune fausse
    /// transition n'est publiée pendant le remplacement de l'état local.</summary>
    internal void RestoreState(Z80BusState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (_publishing) throw new InvalidOperationException("Cannot restore state from a pin callback.");
        ValidateState(state);
        long inputSettled = state.Time;
        foreach (long due in state.Deadlines)
            if (due != long.MaxValue && due > inputSettled) inputSettled = due;
        _waitStableBefore = _controlStableBefore = 0;
        _waitDependentCycles = _controlDependentCycles = 0;
        _waitOutputsSettled = _controlOutputsSettled = inputSettled;
        TimeNanoseconds = state.Time; _lastClockEdge = state.LastClockEdge; _lastRisingEdge = state.LastRisingEdge;
        RisingClocks = state.RisingClocks; _completedCycles = state.CompletedCycles;
        ClockHigh = state.ClockHigh; IsActive = state.Active; _waiting = state.Waiting;
        _waitAsserted = state.WaitAsserted; _phase = state.Phase; _cycle = state.Cycle;
        _steps = _waveforms.Cycles[(int)_cycle];
        _address = state.Address; _refreshAddress = state.RefreshAddress;
        _writeData = state.WriteData; _inputData = state.InputData;
        _sampledData = state.SampledData; _hasSample = state.HasSample;
        _holdWriteData = state.HoldWriteData;
        Z80BusPins levels = default, targets = default;
        for (int line = 0; line < L.Count; line++)
        {
            levels = levels.WithEncodedOutput(line, state.Values[line]);
            targets = targets.WithEncodedOutput(line, state.Targets[line]);
        }
        _levels = levels.EncodedLevels; _targetLevels = targets.EncodedLevels;
        state.Deadlines.CopyTo(_due, 0);
        _pending = 0;
        for (int line = 0; line < L.Count; line++) if (_due[line] != long.MaxValue) _pending |= 1u << line;
        FindNext();
        _publishedNext = NextEventTimestamp;
    }

    private static int[] CapturePinValues(ulong levels)
    {
        var values = new int[L.Count];
        var pins = new Z80BusPins(levels);
        for (int line = 0; line < L.Count; line++) values[line] = pins.GetEncodedOutput(line);
        return values;
    }

    private void ValidateState(Z80BusState state)
    {
        if (state.Timing != _timing || state.Time < 0 || state.LastClockEdge < 0
            || state.LastClockEdge > state.Time || state.LastRisingEdge < 0 || state.LastRisingEdge > state.Time
            || state.RisingClocks < 0 || state.CompletedCycles < 0 || state.Phase is < 0 or > 12
            || !Enum.IsDefined(state.Cycle) || (state.Waiting && !state.Active)
            || (state.HoldWriteData && state.Cycle != Z80BusCycle.MemoryWrite)
            || state.Values is not { Length: L.Count } || state.Targets is not { Length: L.Count }
            || state.Deadlines is not { Length: L.Count })
            throw new ArgumentException("Invalid Z80 bus state.", nameof(state));
        int finalPhase = state.Cycle == Z80BusCycle.InterruptAcknowledge ? 12
            : state.Cycle is Z80BusCycle.MemoryRead or Z80BusCycle.MemoryWrite ? 6 : 8;
        int waitPhase = state.Cycle == Z80BusCycle.InterruptAcknowledge ? 7
            : state.Cycle is Z80BusCycle.IoRead or Z80BusCycle.IoWrite ? 5 : 3;
        if (state.Phase > finalPhase || (state.Waiting && state.Phase != waitPhase))
            throw new ArgumentException("Invalid Z80 bus cycle phase.", nameof(state));
        for (int line = 0; line < L.Count; line++)
        {
            int minimum = line < L.M1 ? -1 : 0;
            int maximum = line == L.Address ? ushort.MaxValue : line == L.Data ? byte.MaxValue : 1;
            if (state.Values[line] < minimum || state.Values[line] > maximum
                || state.Targets[line] < minimum || state.Targets[line] > maximum
                || state.Deadlines[line] < state.Time)
                throw new ArgumentException("Invalid Z80 bus propagation state.", nameof(state));
        }
    }

}
